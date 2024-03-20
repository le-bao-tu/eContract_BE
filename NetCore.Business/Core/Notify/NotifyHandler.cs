using NetCore.Shared;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Serilog;
using System.Text;
using NetCore.Data;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class NotifyHandler : INotifyHandler
    {
        private string notiGateWayUrl = Utils.GetConfig("NotificationGateway:Uri");
        private readonly IOrganizationConfigHandler _organizationConfigHandler;
        public NotifyHandler(IOrganizationConfigHandler organizationConfigHandler)
        {
            _organizationConfigHandler = organizationConfigHandler;
        }

        public async Task<Response> SendNotificationRemindSignDocumentByGateway(NotificationRemindSignDocumentModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Gửi thông báo yêu cầu ký hợp đồng: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-remind-sign-document", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công: " + responeText);
                            return new Response(Code.Success, "Gửi thông báo thành công");
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không gửi được thông báo");
                            return new Response(Code.ServerError, "Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi gửi thông báo");
                return new Response(Code.ServerError, $"Lỗi khi gửi thông báo - {ex.Message}");
            }
        }

        public async Task SendNotificationFirebaseByGateway(NotificationRequestModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Gửi thông báo cho ứng dụng: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-firebase", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công: " + responeText);
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Response: " + JsonSerializer.Serialize(res));
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi gửi thông báo");
            }
        }

        public async Task SendSMSOTPByGateway(NotificationRequestModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Gửi OTP Sign qua SMS by Gateway: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-sms-otp-sign", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Gửi sms otp sign thành công: " + responeText);
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Response: " + JsonSerializer.Serialize(res));
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi gửi thông báo");
            }
        }

        public async Task SendOTPChangePasswordByGateway(NotifyChangePasswordModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Gửi thông báo OTP cho ứng dụng: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-otp-change-password", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công: " + responeText);
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không gửi được thông báo");
                            throw new Exception("Gửi thông báo thất bại");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi gửi thông báo");
                throw new Exception($"Gửi thông báo thất bại - {ex.Message}");
            }
        }

        public async Task<int> SendNotifyDocumentStatus(Guid orgId, NotifyDocumentModel data, SystemLogModel systemLog, OrganizationConfig orgConf = null)
        {
            try
            {
                if (orgConf == null)
                {
                    orgConf = await _organizationConfigHandler.InternalGetByOrgId(orgId);
                }
                if (orgConf == null || string.IsNullOrEmpty(orgConf.CallbackUrl))
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin cấu hình của đơn vị {orgId}");
                    return -1;
                }
                var customerService = orgConf.CallbackUrl;
                if (string.IsNullOrEmpty(customerService))
                {
                    Log.Information($"{systemLog.TraceId} - Đơn vị {orgId} chưa được cấu hình callback url để nhận thông báo");
                    return -1;
                }
                Log.Information($"{systemLog.TraceId} - Gửi thông báo cho hệ thống khách hàng {customerService}. Data: {JsonSerializer.Serialize(data)} ");
                if (orgConf.NotifySendType == NotifySendType.MAVIN_GATEWAY)
                {
                    var defaultRequestCallbackHeaders = orgConf.DefaultRequestCallbackHeaders;
                    using (var client = new HttpClient())
                    {
                        string url = customerService;
                        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                        if (defaultRequestCallbackHeaders != null)
                        {
                            defaultRequestCallbackHeaders.ForEach(x => client.DefaultRequestHeaders.Add(x.Key, x.Value));
                        }
                        HttpResponseMessage res = await client.PostAsync(url, content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công cho hệ thống khách hàng {customerService}: result -" + responeText);
                            var rs = JsonSerializer.Deserialize<NotifyDocumentResponseModel>(responeText);
                            var message = rs.Code == 200 ? "Gửi thông báo thành công cho hệ thống khách hàng" : "Gửi thông báo không thành công cho hệ thống khách hàng";
                            return rs.Code;
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng {customerService}");
                            return -1;
                        }
                    }
                }
                else if (orgConf.NotifySendType == NotifySendType.GHTK_GATEWAY)
                {
                    var defaultRequestCallBackAuthorizationHeaders = orgConf.DefaultRequestCallBackAuthorizationHeaders;
                    var defaultRequestCallbackHeaders = orgConf.DefaultRequestCallbackHeaders;

                    using (var clientAuth = new HttpClient())
                    {
                        //Lấy token 
                        var accessToken = "";
                        if (defaultRequestCallBackAuthorizationHeaders != null)
                        {
                            defaultRequestCallBackAuthorizationHeaders.ForEach(x => clientAuth.DefaultRequestHeaders.Add(x.Key, x.Value));
                        }
                        HttpResponseMessage resAuthen = await clientAuth.GetAsync(orgConf.CallbackAuthorizationUrl);
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
                                    return -1;
                                }
                                using (var client = new HttpClient())
                                {
                                    string url = customerService;
                                    var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                                    if (defaultRequestCallbackHeaders != null)
                                    {
                                        defaultRequestCallbackHeaders.ForEach(x => client.DefaultRequestHeaders.Add(x.Key, x.Value));
                                    }
                                    // Bổ sung thêm token
                                    client.DefaultRequestHeaders.Add("Authorization", accessToken);

                                    HttpResponseMessage res = await client.PostAsync(url, content);
                                    if (res.IsSuccessStatusCode)
                                    {
                                        var responeText = res.Content.ReadAsStringAsync().Result;
                                        Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công cho hệ thống khách hàng {customerService}: result -" + responeText);
                                        //var rs = JsonSerializer.Deserialize<NotifyDocumentResponseModel>(responeText);
                                        //return rs.Code;
                                        return 1;
                                    }
                                    else
                                    {
                                        Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng {customerService}");
                                        return -1;
                                    }
                                }
                            }
                            else
                            {
                                Log.Information($"{systemLog.TraceId} - Không lấy được access_token {responeAuthText}");
                                return -1;
                            }
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng {orgConf.CallbackAuthorizationUrl} - {JsonSerializer.Serialize(defaultRequestCallBackAuthorizationHeaders)}");
                            return -1;
                        }
                    }
                }
                else if (orgConf.NotifySendType == NotifySendType.SNF_GATEWAY)
                {
                    var defaultRequestCallBackAuthorizationHeaders = orgConf.DefaultRequestCallBackAuthorizationHeaders;
                    var defaultRequestCallbackHeaders = orgConf.DefaultRequestCallbackHeaders;

                    using (var clientAuth = new HttpClient())
                    {
                        //Lấy token 
                        var accessToken = "";
                        var dict = new Dictionary<string, string>();

                        if (defaultRequestCallBackAuthorizationHeaders != null)
                        {
                            defaultRequestCallBackAuthorizationHeaders.ForEach(x => dict.Add(x.Key, x.Value));
                        }
                        var contentAuth = new FormUrlEncodedContent(dict);

                        HttpResponseMessage resAuthen = await clientAuth.PostAsync(orgConf.CallbackAuthorizationUrl, contentAuth);
                        if (resAuthen.IsSuccessStatusCode)
                        {
                            var responeAuthText = resAuthen.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Lấy token xác thực: result -" + responeAuthText);
                            var rsAuth = JsonSerializer.Deserialize<SNFAuthenResponseModel>(responeAuthText);

                            if (!string.IsNullOrEmpty(rsAuth.AccessToken))
                            {
                                accessToken = rsAuth.AccessToken;
                                using (var client = new HttpClient())
                                {
                                    string url = customerService;

                                    var dt = AutoMapperUtils.AutoMap<NotifyDocumentModel, SNFNotifyDocumentModel>(data);
                                    dt.GeoLocation = systemLog.Location?.GeoLocation;

                                    var content = new StringContent(JsonSerializer.Serialize(dt), Encoding.UTF8, "application/json");
                                    if (defaultRequestCallbackHeaders != null)
                                    {
                                        defaultRequestCallbackHeaders.ForEach(x => client.DefaultRequestHeaders.Add(x.Key, x.Value));
                                    }

                                    // Bổ sung thêm token
                                    client.DefaultRequestHeaders.Add("Authorization", "Bearer  " + accessToken);
                                    Log.Information($"{systemLog.TraceId} - body gửi thông báo cho SNF: " + JsonSerializer.Serialize(dt));

                                    HttpResponseMessage res = await client.PutAsync(url, content);
                                    if (res.IsSuccessStatusCode)
                                    {
                                        var responeText = res.Content.ReadAsStringAsync().Result;
                                        Log.Information($"{systemLog.TraceId} - Gửi thông báo thành công cho hệ thống khách hàng {customerService}: result -" + responeText);
                                        //var rs = JsonSerializer.Deserialize<NotifyDocumentResponseModel>(responeText);
                                        //return rs.Code;
                                        return 1;
                                    }
                                    else
                                    {
                                        Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng {customerService}");
                                        return -1;
                                    }
                                }
                            }
                            else
                            {
                                Log.Information($"{systemLog.TraceId} - Không lấy được AccessToken");
                                return -1;
                            }
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - Gửi thông báo không thành công cho hệ thống khách hàng {orgConf.CallbackAuthorizationUrl} - {JsonSerializer.Serialize(defaultRequestCallBackAuthorizationHeaders)}");
                            return -1;
                        }
                    }
                }
                {
                    return 0;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Có lỗi xảy ra khi gửi thông báo cho đơn vị có Id: {orgId}");
                return -1;
            }
        }

        public async Task<Response> SendNotificationFromNotifyConfig(NotificationConfigModel model)
        {
            Log.Information($"Gửi thông báo theo cấu hình: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-from-notify-config", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"Gửi thông báo thành công: " + responeText);
                            return new Response(Code.Success, "Gửi thông báo thành công");
                        }
                        else
                        {
                            Log.Error($"Lỗi kế nối dịch vụ, không gửi được thông báo");
                            return new Response(Code.ServerError, "Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi gửi thông báo từ cấu hình");
                return new Response(Code.ServerError, $"Lỗi khi gửi thông báo - {ex.Message}");
            }
        }

        public async Task<Response> SendOTPAuthUserByGateway(NotificationSendOTPAuthUserModel model)
        {
            Log.Information($"Gửi thông báo OTP cho người dùng " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-otp-user-auth", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"Gửi thông báo thành công: " + responeText);
                            return new Response(Code.Success, "Gửi thông báo thành công");
                        }
                        else
                        {
                            Log.Error($"Lỗi kế nối dịch vụ, không gửi được thông báo");
                            return new Response(Code.ServerError, "Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi gửi thông báo từ cấu hình");
                return new Response(Code.ServerError, $"Lỗi khi gửi thông báo - {ex.Message}");
            }
        }

        public async Task<Response> PushNotificationRemindSignDoucmentDaily(NotificationRemindSignDocumentDailyModel model)
        {
            Log.Information($"Gửi thông báo nhắc nhở ký hợp đồng hàng ngày: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-remind-sign-document-daily", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"Gửi thông báo thành công: " + responeText);
                            return new Response(Code.Success, "Gửi thông báo thành công");
                        }
                        else
                        {
                            Log.Error($"Lỗi kế nối dịch vụ, không gửi được thông báo");
                            return new Response(Code.ServerError, "Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi gửi thông báo từ cấu hình");
                return new Response(Code.ServerError, $"Lỗi khi gửi thông báo - {ex.Message}");
            }
        }

        public async Task<Response> PushNotificationAutoSignFail(NotificationAutoSignFailModel model)
        {
            Log.Information($"Gửi thông báo ký tự động bị lỗi: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-auto-sign-fail", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"Gửi thông báo thành công: " + responeText);
                            return new Response(Code.Success, "Gửi thông báo thành công");
                        }
                        else
                        {
                            Log.Error($"Lỗi kế nối dịch vụ, không gửi được thông báo");
                            return new Response(Code.ServerError, "Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi gửi thông báo từ cấu hình");
                return new Response(Code.ServerError, $"Lỗi khi gửi thông báo - {ex.Message}");
            }
        }

        public async Task<Response> SendSystemNotification(NotificationRemindSignDocumentDailyModel model)
        {
            Log.Information($"Gửi thông báo hệ thống: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-system-notification", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"Gửi thông báo thành công: " + responeText);
                            return new Response(Code.Success, "Gửi thông báo thành công");
                        }
                        else
                        {
                            Log.Error($"Lỗi kế nối dịch vụ, không gửi được thông báo");
                            return new Response(Code.ServerError, "Lỗi kế nối dịch vụ, không gửi được thông báo");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Có lỗi xảy ra khi gửi thông báo từ cấu hình");
                return new Response(Code.ServerError, $"Lỗi khi gửi thông báo - {ex.Message}");
            }
        }

        public Task<Response> SendSystemNotification(NotificationConfigModel model)
        {
            throw new NotImplementedException();
        }
    }
}