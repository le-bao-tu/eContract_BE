using NetCore.Shared;
using System;
using Serilog;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using OtpNet;
using System.Net.Http.Headers;
using System.Net;
using System.Web;
using System.Text;
using System.IO;
using System.Xml;

namespace NetCore.Business
{
    public class OTPHandler : IOTPHandler
    {
        private readonly string idpUrl = Utils.GetConfig("WSO2IS:uri");
        private readonly string idpBasicUserName = Utils.GetConfig("WSO2IS:basicUserName");
        private readonly string idpBasicPassword = Utils.GetConfig("WSO2IS:basicPassword");
        private readonly int timeOTPSize = 300; // 5 phút

        private readonly string otpServiceUrl = Utils.GetConfig("OTPService:uri");

        private readonly ICacheService _cacheService;

        private const string CachePrefix = CacheConstants.TOTP;

        public OTPHandler(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<string> GenerateOTP(string account)
        {
            try
            {
                var secretKey = Base32Encoding.ToBytes(await RetrieveSecretKeyResponse(account));
                var totp = new Totp(secretKey, timeOTPSize);
                var otp = totp.ComputeTotp();
                return otp;
                //return new ResponseObject<string>(otp, MessageConstants.GetDataSuccessMessage, Code.Success);  
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw new ArgumentException("Không tạo được OTP");
                //return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<bool> ValidateOTP(ValidateOTPModel model)
        {
            try
            {
                // Using local validate
                var secretKey = Base32Encoding.ToBytes(await RetrieveSecretKeyResponse(model.UserName));
                var totp = new Totp(secretKey, timeOTPSize);
                long timeStepMatched = 0;
                var result = totp.VerifyTotp(model.OTP.ToString(), out timeStepMatched);
                //Console.WriteLine(timeStepMatched);
                //Console.WriteLine(result);

                return result;

                #region Using IDP to validate
                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient(clientHandler))
                //    {
                //        //Log.Error("ValidateOTP: " + JsonSerializer.Serialize(model));
                //        var token = await GetToken();
                //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

                //        var dict = new Dictionary<string, string>();
                //        dict.Add("username", model.UserName);
                //        dict.Add("verificationCode", model.OTP);
                //        var content = new FormUrlEncodedContent(dict);
                //        var res = await client.PostAsync(otpUrl + "totp/1.0.0/validateTOTP", content);
                //        if (res.IsSuccessStatusCode)
                //        {
                //            var responeText = res.Content.ReadAsStringAsync().Result;
                //            var rsOtp = JsonSerializer.Deserialize<ValidateOTPResponseModel>(responeText);
                //            return rsOtp.ValidateTOTPResponse.Return;
                //            //return new ResponseObject<bool>(rsOtp.ValidateTOTPResponse.Return, MessageConstants.GetDataSuccessMessage, Code.Success);
                //        }
                //        else
                //        {
                //            Log.Error(JsonSerializer.Serialize(res));
                //            throw new ArgumentException("Không xác thực được OTP");
                //        }
                //    }
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw new ArgumentException("Không validate được OTP");
            }
        }

        #region OTP Model

        public async Task<OTPResponseModel> GenerateOTP(OTPRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                //TODO: Bổ sung ghi log
                var secretKey = Base32Encoding.ToBytes(await RetrieveSecretKeyResponse(model.UserName));
                var totp = new Totp(secretKey, model.Step, OtpHashMode.Sha1, model.TOTPSize);
                var otp = totp.ComputeTotp();
                var remainingSeconds = totp.RemainingSeconds();

                OTPResponseModel rs = new OTPResponseModel()
                {
                    OTP = otp,
                    UserName = model.UserName,
                    RemainingSeconds = remainingSeconds,
                    ExpireAtUTCDate = DateTime.UtcNow.AddSeconds(remainingSeconds)
                };

                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        public async Task<bool> ValidateOTP(OTPValidateModel model, SystemLogModel systemLog)
        {
            try
            {
                //TODO: Bổ sung ghi log
                // Using local validate
                var secretKey = Base32Encoding.ToBytes(await RetrieveSecretKeyResponse(model.UserName));
                var totp = new Totp(secretKey, model.Step, OtpHashMode.Sha1, model.OTP.Length);
                long timeStepMatched = 0;
                var rs = totp.VerifyTotp(model.OTP, out timeStepMatched);
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        #endregion

        #region OTP From Service
        public async Task<HOTPResponseDetailModel> GenerateHOTPFromService(HOTPRequestModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Yêu cầu sinh HOTP: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(otpServiceUrl + "api/v1/otp/generate-hotp", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            var rs = JsonSerializer.Deserialize<HOTPResponseModel>(responeText);
                            if (rs.Code == 200)
                            {
                                return rs.Data;
                            }
                            else
                            {
                                return new HOTPResponseDetailModel()
                                {
                                    OTP = null,
                                    Message = rs.Message
                                };
                            }
                        }
                        else
                        {
                            return new HOTPResponseDetailModel()
                            {
                                OTP = null,
                                Message = "Lỗi kế nối dịch vụ, không lấy được OTP"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new HOTPResponseDetailModel()
                {
                    OTP = null,
                    Message = "Lỗi kế nối dịch vụ, không lấy được OTP: " + ex.Message
                };
            }
        }
        public async Task<HOTPValidateResponseModel> ValidateHOTPFromService(HOTPValidateModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - Yêu cầu kiểm tra HOTP: " + JsonSerializer.Serialize(model));
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(otpServiceUrl + "api/v1/otp/validate-hotp", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            var rs = JsonSerializer.Deserialize<HOTPResponseModel>(responeText);
                            if (rs.Code == 200)
                            {
                                return new HOTPValidateResponseModel()
                                {
                                    IsSuccess = true,
                                    Message = rs.Message
                                };
                            }
                            else
                            {
                                return new HOTPValidateResponseModel()
                                {
                                    IsSuccess = false,
                                    Message = rs.Message
                                };
                            }
                        }
                        else
                        {
                            return new HOTPValidateResponseModel()
                            {
                                IsSuccess = false,
                                Message = "Lỗi kết nối dịch vụ, không kiểm tra được OTP"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new HOTPValidateResponseModel()
                {
                    IsSuccess = false,
                    Message = "Lỗi kết nối dịch vụ, không kiểm tra được OTP: " + ex.Message
                };
            }
        }
        #endregion


        public string GenerateHOTP(string secretKey, long counter = 1, int totpSize = 6)
        {
            try
            {
                var hotp = new Hotp(Encoding.ASCII.GetBytes(secretKey), mode: OtpHashMode.Sha512, totpSize);
                var otp = hotp.ComputeHOTP(counter);
                return otp;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw new ArgumentException("Không tạo được OTP");
            }
        }
        public bool ValidateHOTP(string otp, string secretKey, long counter = 1, int totpSize = 6)
        {
            try
            {
                var hotp = new Hotp(Encoding.ASCII.GetBytes(secretKey), mode: OtpHashMode.Sha512, totpSize);
                return hotp.VerifyHotp(otp, counter);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw new ArgumentException("Không validate được OTP");
            }
        }

        private async Task<string> RetrieveSecretKeyResponse(string account)
        {
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{idpBasicUserName}:{idpBasicPassword}"));

                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                        var dict = new Dictionary<string, string>();
                        dict.Add("username", account);
                        var content = new FormUrlEncodedContent(dict);
                        var res = await client.PostAsync(idpUrl + "services/TOTPAdminService/retrieveSecretKey", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"RetrieveSecretKeyResponse: " + JsonSerializer.Serialize(responeText));

                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(responeText);

                            XmlNodeList elemList = doc.GetElementsByTagName("ns:return");

                            if (elemList.Count > 0) return elemList[0].InnerText;
                            else throw new ArgumentException("Không lấy được OTP");
                        }
                        else
                        {
                            Log.Error(JsonSerializer.Serialize(res));
                            throw new ArgumentException("Không lấy được RetrieveSecretKey");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
    }
}


