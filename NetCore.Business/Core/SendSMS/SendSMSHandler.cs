using NetCore.Shared;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Serilog;
using System.Text;
using NetCore.Data;
using Microsoft.EntityFrameworkCore;
using System.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class SendSMSHandler : ISendSMSHandler
    {
        private readonly DataContext _dataContext;
        private readonly IOrganizationConfigHandler _organizationConfigHandler;
        private readonly string _smsServiceUrl = Utils.GetConfig("vSMS:url");
        private readonly string _smsServiceBackup = Utils.GetConfig("vSMS:urlBackup");
        private readonly string _smsType = Utils.GetConfig("vSMS:type");
        public SendSMSHandler(DataContext dataContext, IOrganizationConfigHandler organizationConfigHandler)
        {
            _dataContext = dataContext;
            _organizationConfigHandler = organizationConfigHandler;
        }

        public async Task<bool> SendSMS(SendSMSModel data, OrganizationConfig orgConf, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Gửi tin nhắn cho khách hàng: " + JsonSerializer.Serialize(data));
            try
            {
                //Lấy thông tin cấu hình đơn vị
                if (orgConf == null)
                {
                    orgConf = await _organizationConfigHandler.InternalGetByOrgId(data.OrganizationId);
                }
                if (orgConf == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin cấu hình của đơn vị {data.OrganizationId}");
                    return false;
                }

                if (orgConf.SMSSendType == SMSSendType.VSMS_GATEWAY)
                {
                    var smsConfig = orgConf.SMSConfig;
                    //Kiểm tra số điện thoại
                    Regex rx = new Regex(@"(^84|0)+(3[2-9]|5[6|8|9]|9\d(?!5)|8[1-9]|7[0|6-9])+([0-9]{7})\b");
                    if (!rx.IsMatch(data.PhoneNumber))
                    {
                        Log.Information($"{systemLog.TraceId} - Số điện thoại không đúng định dạng: {data.PhoneNumber}");
                        return false;
                    }

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = systemLog.TempObjectCode,
                        ObjectId = systemLog.TempObjectId,
                        Description = $"Gửi OTP cho khách hàng qua SMS",
                        MetaData = JsonSerializer.Serialize(data)
                    });

                    //Lưu vào SendSMSLog
                    var smsLog = new VSMSSendQueue()
                    {
                        Id = new Guid(),
                        SourceAddr = smsConfig.Brandname,
                        PhoneNumber = data.PhoneNumber,
                        Message = data.Message,
                        OrganizationId = data.OrganizationId,
                        UserId = data.UserId,
                    };
                    await _dataContext.VSMSSendQueue.AddAsync(smsLog);
                    var save = await _dataContext.SaveChangesAsync();
                    if (save < 1)
                    {
                        Log.Information($"{systemLog.TraceId} - Không thể lưu tin nhắn", $"data: {JsonSerializer.Serialize(smsLog)}");
                    }
                    var smsId = smsLog.Id;
                    return await SendSMSWorker(smsId);
                }
                else if (orgConf.SMSSendType == SMSSendType.GHTK_GATEWAY && !string.IsNullOrEmpty(orgConf.SMSUrl))
                {
                    //TODO: Kiểm tra số điện thoại
                    //Regex rx = new Regex(@"(^84|0)+(3[2-9]|5[6|8|9]|9\d(?!5)|8[1-9]|7[0|6-9])+([0-9]{7})\b");
                    //if (!rx.IsMatch(data.PhoneNumber))
                    //{
                    //    Log.Information($"{systemLog.TraceId} - Số điện thoại không đúng định dạng: {data.PhoneNumber}");
                    //    return false;
                    //}

                    if (string.IsNullOrEmpty(data.PhoneNumber))
                    {
                        Log.Information($"{systemLog.TraceId} - Số điện thoại đang để trống");
                        return false;
                    }
                    var defaultRequestCallbackHeaders = orgConf.DefaultRequestSMSHeaders;

                    GHTKSMSSendModel smsModel = new GHTKSMSSendModel()
                    {
                        Sender = "GHTK",
                        Receiver = data.PhoneNumber,
                        Msg = data.Message
                    };

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = systemLog.TempObjectCode,
                        ObjectId = systemLog.TempObjectId,
                        Description = $"Gửi OTP cho khách hàng qua SMS",
                        MetaData = JsonSerializer.Serialize(smsModel)
                    });

                    #region Code cũ
                    //using (var client = new HttpClient())
                    //{
                    //    Log.Information($"{systemLog.TraceId} - Gửi OTP cho khách hàng. Data: {JsonSerializer.Serialize(smsModel)} ");
                    //    string url = $"{orgConf.SMSUrl}";
                    //    var content = new StringContent(JsonSerializer.Serialize(smsModel), Encoding.UTF8, "application/json");
                    //    if (defaultRequestCallbackHeaders != null)
                    //    {
                    //        defaultRequestCallbackHeaders.ForEach(x => client.DefaultRequestHeaders.Add(x.Key, x.Value));
                    //    }
                    //    HttpResponseMessage res = await client.PostAsync(url, content);
                    //    if (res.IsSuccessStatusCode)
                    //    {
                    //        var responeText = res.Content.ReadAsStringAsync().Result;
                    //        var rs = JsonSerializer.Deserialize<NotifyDocumentResponseModel>(responeText);
                    //        Log.Information($"{systemLog.TraceId} - Gửi OTP cho khách hàng. DataResponse: {responeText} ");
                    //        //var message = "Gửi OTP cho người dùng thành công";
                    //        //Log.Information($"{message} {customerService} : result: {JsonSerializer.Serialize(rs)}");
                    //        return true;
                    //    }
                    //    else
                    //    {
                    //        Log.Error($"{systemLog.TraceId} - Gửi OTP cho khách hàng thất bại.");
                    //        return false;
                    //    }
                    //}
                    #endregion

                    #region Code mới

                    var defaultSMSRequestCallBackAuthorizationHeaders = orgConf.DefaultRequestSMSAuthorizationHeaders;
                    var defaultSMSRequestCallbackHeaders = orgConf.DefaultRequestSMSHeaders;

                    using (var clientAuth = new HttpClient())
                    {
                        //Lấy token 
                        var accessToken = "";
                        if (defaultSMSRequestCallBackAuthorizationHeaders != null)
                        {
                            defaultSMSRequestCallBackAuthorizationHeaders.ForEach(x => clientAuth.DefaultRequestHeaders.Add(x.Key, x.Value));
                        }
                        HttpResponseMessage resAuthen = await clientAuth.GetAsync(orgConf.SMSAuthorizationUrl);
                        if (resAuthen.IsSuccessStatusCode)
                        {
                            var responeAuthText = resAuthen.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Lấy token xác thực: result -" + responeAuthText);
                            var rsAuth = JsonSerializer.Deserialize<GHTKAuthenResponseModel>(responeAuthText);

                            if (rsAuth.Success)
                            {
                                accessToken = rsAuth.Data?.AccessToken;
                                if (string.IsNullOrEmpty(accessToken))
                                {
                                    Log.Information($"{systemLog.TraceId} - Không lấy được AccessToken");
                                    return false;
                                }

                                using (HttpClientHandler clientHandler = new HttpClientHandler())
                                {
                                    clientHandler.ServerCertificateCustomValidationCallback =
                                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                                    using (var client = new HttpClient(clientHandler))
                                    {
                                        string url = orgConf.SMSUrl;

                                        // Bổ sung thêm token
                                        client.DefaultRequestHeaders.Clear();
                                        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                                        client.DefaultRequestHeaders.Add("Authorization", accessToken);

                                        var dt = new[]
                                        {
                                            new KeyValuePair<string, string>("tel", data.PhoneNumber),
                                            new KeyValuePair<string, string>("content", data.Message),
                                        };

                                        var res = await client.PostAsync(url, new FormUrlEncodedContent(dt));

                                        if (res.IsSuccessStatusCode)
                                        {
                                            var responeText = res.Content.ReadAsStringAsync().Result;
                                            Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công cho hệ thống khách hàng {url}: result -" + responeText);
                                            //var rs = JsonSerializer.Deserialize<NotifyDocumentResponseModel>(responeText);
                                            //return rs.Code;
                                            return true;
                                        }
                                        else
                                        {
                                            Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng {url}");
                                            return false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Log.Information($"{systemLog.TraceId} - Không lấy được access_token {responeAuthText}");
                                return false;
                            }
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng");
                            return false;
                        }
                    }

                    #endregion
                }
                else if (orgConf.SMSSendType == SMSSendType.SNF_GATEWAY && !string.IsNullOrEmpty(orgConf.SMSUrl))
                {
                    //TODO: Kiểm tra số điện thoại
                    //Regex rx = new Regex(@"(^84|0)+(3[2-9]|5[6|8|9]|9\d(?!5)|8[1-9]|7[0|6-9])+([0-9]{7})\b");
                    //if (!rx.IsMatch(data.PhoneNumber))
                    //{
                    //    Log.Information($"{systemLog.TraceId} - Số điện thoại không đúng định dạng: {data.PhoneNumber}");
                    //    return false;
                    //}

                    if (string.IsNullOrEmpty(data.PhoneNumber))
                    {
                        Log.Information($"{systemLog.TraceId} - Số điện thoại đang để trống");
                        return false;
                    }

                    #region format lại số điện thoại
                    //0818038385 -> 84818038385
                    //0912184680-> 0918038385

                    if (data.PhoneNumber.StartsWith("08"))
                    {
                        data.PhoneNumber = "84" + data.PhoneNumber.Substring(1);
                    }
                    //Log.Information($"{systemLog.TraceId} - {data.PhoneNumber}");

                    #endregion
                    var defaultRequestCallbackHeaders = orgConf.DefaultRequestSMSHeaders;

                    SNFSMSSendModel smsModel = new SNFSMSSendModel()
                    {
                        To = data.PhoneNumber,
                        Text = data.Message,
                        SMSFailover = new SNFSMSFailOver()
                        {
                            Text = data.Message,
                            To = data.PhoneNumber,
                            Unicode = 0
                        }
                    };

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = systemLog.TempObjectCode,
                        ObjectId = systemLog.TempObjectId,
                        Description = $"Gửi OTP cho khách hàng qua SMS",
                        MetaData = JsonSerializer.Serialize(smsModel)
                    });

                    using (var client = new HttpClient())
                    {
                        Log.Information($"{systemLog.TraceId} - Gửi OTP cho khách hàng. Data: {JsonSerializer.Serialize(smsModel)} ");
                        string url = $"{orgConf.SMSUrl}";
                        var content = new StringContent(JsonSerializer.Serialize(smsModel), Encoding.UTF8, "application/json");
                        if (defaultRequestCallbackHeaders != null)
                        {
                            defaultRequestCallbackHeaders.ForEach(x => client.DefaultRequestHeaders.Add(x.Key, x.Value));
                        }
                        HttpResponseMessage res = await client.PostAsync(url, content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            var rs = JsonSerializer.Deserialize<NotifyDocumentResponseModel>(responeText);
                            Log.Information($"{systemLog.TraceId} - Gửi OTP cho khách hàng. DataResponse: {responeText} ");
                            //var message = "Gửi OTP cho người dùng thành công";
                            //Log.Information($"{message} {customerService} : result: {JsonSerializer.Serialize(rs)}");
                            return true;
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Gửi OTP cho khách hàng thất bại.");
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - Có lỗi khi lưu tin nhắn", ex.Message);
                return false;
            }
        }

        private async Task<bool> SendSMSWorker(Guid smsId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //Lấy thông tin gửi SMS từ SendSMS
                    var sendSMS = await _dataContext.VSMSSendQueue.FindAsync(smsId);
                    if (sendSMS == null)
                    {
                        Log.Information($"Không tìm thấy SMS id:{smsId}");
                    }
                    //Lấy thông tin cấu hình đơn vị
                    var orgConf = await _organizationConfigHandler.InternalGetByOrgId(sendSMS.OrganizationId);
                    if (orgConf == null)
                    {
                        Log.Information($"Không tìm thấy thông tin cấu hình của đơn vị {sendSMS.OrganizationId}");
                        return false;
                    }
                    var smsConfig = orgConf.SMSConfig;
                    Log.Information($"Tiến hành gửi tin nhắn cho {sendSMS.PhoneNumber}");
                    var query = new Dictionary<string, string>();
                    query["username"] = smsConfig.Username;
                    query["password"] = smsConfig.Password;
                    query["source_addr"] = sendSMS.SourceAddr;
                    query["dest_addr"] = sendSMS.PhoneNumber;
                    query["message"] = sendSMS.Message;
                    query["type"] = _smsType;
                    query["request_id"] = sendSMS.Order.ToString();
                    var uri = QueryHelpers.AddQueryString(_smsServiceUrl + "sendMulti", query);
                    HttpResponseMessage res = await client.GetAsync(uri);
                    if (res.IsSuccessStatusCode)
                    {
                        var responeText = res.Content.ReadAsStringAsync().Result;
                        var rs = JsonSerializer.Deserialize<List<SendSMSResponse>>(responeText);
                        Log.Information($"Gửi tin nhắn thành công cho {sendSMS.PhoneNumber}", $"result: {JsonSerializer.Serialize(rs)}");
                        //Cập nhật kết quả gửi tin nhắn
                        sendSMS.SendSMSResonse = rs[0];
                        sendSMS.IsPush = true;
                        sendSMS.SentDate = DateTime.Now;
                        _dataContext.VSMSSendQueue.Update(sendSMS);
                        int save = await _dataContext.SaveChangesAsync();
                        if (save < 1)
                        {
                            Log.Information($"Không thể lưu lịch sử gửi tin nhắn", $"data: {JsonSerializer.Serialize(sendSMS)}");
                        }
                        return true;
                    }
                    else
                    {
                        Log.Error($"Gửi tin nhắn không thành công cho {sendSMS.PhoneNumber}", $"StatusCode: {JsonSerializer.Serialize(res.StatusCode)}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Có lỗi khi kết nối với SMS Service", ex.Message);
                return false;
            }
        }

        //public async Task<SendSMSResponse> GetStatusSMS(Guid? orgId, string msgid)
        //{
        //    try
        //    {
        //        var orgConf = await _organizationConfigHandler.InternalGetById(orgId);
        //        if (orgConf == null)
        //        {
        //            Log.Information($"Không tìm thấy thông tin cấu hình của đơn vị {orgId}");
        //            return null;
        //        }
        //        var smsConfig = orgConf.SMSConfig;
        //        using (var client = new HttpClient())
        //        {
        //            var builder = new UriBuilder(_smsServiceUrl + "getStatus");
        //            var query = HttpUtility.ParseQueryString(string.Empty);
        //            query["username"] = smsConfig.Username;
        //            query["password"] = smsConfig.Password;
        //            query["msgid"] = msgid;
        //            builder.Query = query.ToString();
        //            HttpResponseMessage res = await client.GetAsync(builder.ToString());
        //            if (res.IsSuccessStatusCode)
        //            {
        //                var responeText = res.Content.ReadAsStringAsync().Result;
        //                var rs = JsonSerializer.Deserialize<SendSMSResponse>(responeText);
        //                return rs;
        //            }
        //            else
        //            {
        //                Log.Error($"Lất thông tin nhắn ID: {msgid} không thành công cho");
        //                return null;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"{ MessageConstants.ErrorLogMessage} {ex.Message} khi kết nối với SMS Service");
        //        return null;
        //    }
        //}
    }
}