using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Tích hợp WSO2
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/wso2")]
    [ApiExplorerSettings(GroupName = "System - Wso2", IgnoreApi = true)]
    public class AdminWso2Controller : ApiControllerBase
    {
        private readonly string _serviceValidate = Utils.GetConfig("Authentication:WSO2:Uri");
        private readonly string _clientId = Utils.GetConfig("Authentication:WSO2:Clientid");
        private readonly string _clientSecret = Utils.GetConfig("Authentication:WSO2:Secret");
        private readonly string _redirectUri = Utils.GetConfig("Authentication:WSO2:Redirecturi");
        private readonly int _wso2CacheLiveMinutes = Int32.Parse(Utils.GetConfig("Authentication:WSO2:CacheLiveMinutes"));

        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IUserHandler _userHandler;
        private readonly IRoleHandler _roleHandler;

        public AdminWso2Controller(ICacheService cacheService, DataContext dataContext, IUserHandler userHandler, IRoleHandler roleHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _userHandler = userHandler;
            _roleHandler = roleHandler;
            // store custom services here...
        }

        /// <summary>
        /// API lấy về các thông tin xác thực lấy từ Identity Server theo Authentication Code
        /// </summary>
        /// <param name="authorizationCode"></param>
        /// <returns>Kết quả trả về</returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("{authorizationCode}/authenticationinfo")]
        [ProducesResponseType(typeof(ResponseObject<WSO2Result>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuthenticationInfoByCode(string authorizationCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var refreshTokenUri = new Uri(_serviceValidate + "oauth2/token")
                    .AddQuery("grant_type", "authorization_code")
                    .AddQuery("redirect_uri", _redirectUri)
                    .AddQuery("code", authorizationCode);

                #region Code HttpClient
                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient(clientHandler))
                //    {
                //        var encodedAuthenHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_clientId + ":" + _clientSecret));

                //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedAuthenHeader);
                //        var dict = new Dictionary<string, string>();
                //        dict.Add("Content-Type", "application/x-www-form-urlencoded");
                //        var content = new FormUrlEncodedContent(dict);
                //        var res = await client.PostAsync(refreshTokenUri, content);
                //        if (res.IsSuccessStatusCode)
                //        {
                //            var responseBody = res.Content.ReadAsStringAsync().Result;
                //            var wso2Result = JsonSerializer.Deserialize<WSO2Result>(responseBody);

                //            // Caculate expires time
                //            var dt = DateTime.Now;
                //            wso2Result.start_time = dt.ToString(CultureInfo.InvariantCulture);
                //            wso2Result.expires_time = dt.AddSeconds(wso2Result.expires_in).ToString(CultureInfo.InvariantCulture);

                //            //Lưu access_token vào cache

                //            // Get user info
                //            if (wso2Result.access_token != null)
                //            {
                //                //#region Lưu access_token vào cache với thời gian validate là 10p
                //                //var wso2CacheKey = $"{CacheConstants.WSO2_ACCESSTOKEN}-{wso2Result.access_token}";

                //                //WSO2AccessTokenCacheModel accessTokenCacheModel = new WSO2AccessTokenCacheModel()
                //                //{
                //                //    CreatedDate = DateTime.Now,
                //                //    ExpireAt = DateTime.Now.AddMinutes(_wso2CacheLiveMinutes),
                //                //    AccessToken = wso2Result.access_token
                //                //};

                //                //await _cacheService.GetOrCreate(wso2CacheKey, () =>
                //                //{
                //                //    return accessTokenCacheModel;
                //                //});
                //                //#endregion

                //                #region Đọc dữ liệu từ access token
                //                wso2Result.UserInfo = await this.GetUserInfoByJWTAccessToken(wso2Result.access_token);

                //                #endregion

                //                #region Đọc dữ liệu từ server
                //                //var getUserInfo = await GetUserInfoByAccessToken(wso2Result.access_token);

                //                //if (getUserInfo.Code == Code.Success)
                //                //{
                //                //    if (getUserInfo is ResponseObject<WSO2UserInfo> getUserInfoData) wso2Result.UserInfo = getUserInfoData.Data;
                //                //}
                //                #endregion
                //            }
                //            var result = new ResponseObject<WSO2Result>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                //            return Helper.TransformData(result);
                //        }
                //        else
                //        {
                //            Log.Error($"WSO2 Error: " + JsonSerializer.Serialize(res));
                //            //return null;
                //            throw new Exception("Lỗi kế nối dịch vụ, không lấy được Token");
                //        }
                //    }
                //}
                #endregion

                #region Code WebRequest
                var webRequest = (HttpWebRequest)WebRequest.Create(refreshTokenUri);
                webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest.Method = "POST";
                var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_clientId + ":" + _clientSecret));
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Accept = "application/json, text/javascript, */*";
                webRequest.Headers.Add("Authorization", "Basic " + encoded);
                try
                {
                    using (var jsonResponse = (HttpWebResponse)webRequest.GetResponse())
                    {
                        var jsonStream = jsonResponse.GetResponseStream();

                        var ms = new MemoryStream();
                        jsonStream?.CopyTo(ms);
                        ms.Position = 0;
                        var response = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StreamContent(ms)
                        };
                        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var wso2Result = JsonSerializer.Deserialize<WSO2Result>(responseBody);
                        //Log.Information($"WSO2 Response body with authorizationCode: {authorizationCode} - " + responseBody);

                        // Caculate expires time
                        var dt = DateTime.Now;
                        wso2Result.start_time = dt.ToString(CultureInfo.InvariantCulture);
                        wso2Result.expires_time = dt.AddSeconds(wso2Result.expires_in).ToString(CultureInfo.InvariantCulture);

                        // Get user info
                        if (wso2Result.access_token != null)
                        {
                            //#region Lưu access_token vào cache với thời gian validate là 10p
                            //var wso2CacheKey = $"{CacheConstants.WSO2_ACCESSTOKEN}-{wso2Result.access_token}";

                            //WSO2AccessTokenCacheModel accessTokenCacheModel = new WSO2AccessTokenCacheModel()
                            //{
                            //    CreatedDate = DateTime.Now,
                            //    ExpireAt = DateTime.Now.AddMinutes(_wso2CacheLiveMinutes),
                            //    AccessToken = wso2Result.access_token
                            //};
                            //await _cacheService.GetOrCreate(wso2CacheKey, () =>

                            //{
                            //    return accessTokenCacheModel;
                            //});
                            //#endregion

                            #region Đọc dữ liệu từ access token
                            wso2Result.UserInfo = await this.GetUserInfoByJWTAccessToken(wso2Result.access_token);

                            if (wso2Result.UserInfo != null && wso2Result.UserInfo.user_id != Guid.Empty)
                            {
                                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_WSO2_LOGIN);
                                u.SystemLog.ActionName = LogConstants.ACTION_WSO2_LOGIN;
                                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                                u.SystemLog.UserId = wso2Result.UserInfo.user_id.ToString();

                                u.SystemLog.OrganizationId = wso2Result.UserInfo.organization_id?.ToString();
                                u.SystemLog.ListAction.Add(new ActionDetail()
                                {
                                    Description = $"{wso2Result.UserInfo.user_name} truy cập hệ thống thành công",
                                    ObjectCode = CacheConstants.USER,
                                    ObjectId = wso2Result.UserInfo.user_id.ToString()
                                });
                            }

                            #endregion

                            #region Đọc dữ liệu từ server
                            //var getUserInfo = await GetUserInfoByAccessToken(wso2Result.access_token);

                            //if (getUserInfo.Code == Code.Success)
                            //{
                            //    if (getUserInfo is ResponseObject<WSO2UserInfo> getUserInfoData) wso2Result.UserInfo = getUserInfoData.Data;
                            //}
                            #endregion

                            var result = new ResponseObject<WSO2Result>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                            return result;
                        }
                        else
                        {
                            //Log.Information("Không lấy được access_token: authorizationCode " + authorizationCode);
                            var result = new Response(Code.Unauthorized, "Đăng nhập không thành công, không lấy được access_token");
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Log.Information("Có lỗi xảy ra: authorizationCode " + authorizationCode + " - " + ex.Message);
                    var result = new Response(Code.ServerError, "Có lỗi trong quá trình xử lý: " + ex.Message);
                    return result;
                }
                #endregion
            });
        }

        [AllowAnonymous, HttpPost, Route("authentication/login")]
        [ProducesResponseType(typeof(ResponseObject<WSO2Result>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Authenticate([FromBody] LoginModel login)
        {
            Log.Information($"User {login.Username} try login from implicit login");
            UriBuilder uriBuilder = new UriBuilder(_serviceValidate + "oauth2/token?grant_type=password&username=" + login.Username
                                        + "&password=" + login.Password + "&scope=openid");

            var webRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
            webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            webRequest.Method = "POST";
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(_clientId + ":" + _clientSecret));
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Accept = "application/json, text/javascript, */*";
            webRequest.Headers.Add("Authorization", "Basic " + encoded);
            try
            {
                using (var jsonResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    var jsonStream = jsonResponse.GetResponseStream();

                    MemoryStream ms = new MemoryStream();
                    jsonStream.CopyTo(ms);
                    ms.Position = 0;
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StreamContent(ms);
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var wso2Result = JsonSerializer.Deserialize<WSO2Result>(responseBody);

                    // Caculate expires time
                    var dt = DateTime.Now;
                    wso2Result.start_time = dt.ToString(CultureInfo.InvariantCulture);
                    wso2Result.expires_time = dt.AddSeconds(wso2Result.expires_in).ToString(CultureInfo.InvariantCulture);

                    // Get user info
                    if (wso2Result.access_token != null)
                    {
                        #region Đọc dữ liệu từ access token
                        //wso2Result.UserInfo = await this.GetUserInfoByJWTAccessToken(wso2Result.access_token);
                        wso2Result.UserModel = await this.GetUserModelByJWTAccessToken(wso2Result.access_token);
                        #endregion

                        #region Đọc dữ liệu từ server
                        //var getUserInfo = await GetUserInfoByAccessToken(wso2Result.access_token);

                        //if (getUserInfo.Code == Code.Success)
                        //{
                        //    if (getUserInfo is ResponseObject<WSO2UserInfo> getUserInfoData) wso2Result.UserInfo = getUserInfoData.Data;
                        //}
                        #endregion
                    }
                    var result = new ResponseObject<WSO2Result>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                    return Helper.TransformData(result);
                }
            }
            catch (Exception e)
            {
                var result = new Response(Code.Unauthorized, "Đăng nhập thất bại, kiểm tra lại thông tin tài khoản và mật khẩu.");
                return Helper.TransformData(result);
            }
        }

        /// <summary>
        /// API làm mới access token dựa vào refresh token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Kết quả trả về</returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("{token}/refreshtoken")]
        [ProducesResponseType(typeof(ResponseObject<WSO2Result>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewAccessTokenByRefreshToken(string token)
        {
            try
            {
                var refreshTokenUri = new Uri(_serviceValidate + "oauth2/token")
                .AddQuery("grant_type", "refresh_token")
                .AddQuery("refresh_token", token);

                #region Code HttpClient
                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient(clientHandler))
                //    {
                //        var encodedAuthenHeader = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_clientId + ":" + _clientSecret));

                //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedAuthenHeader);
                //        var dict = new Dictionary<string, string>();
                //        dict.Add("Content-Type", "application/x-www-form-urlencoded");
                //        var content = new FormUrlEncodedContent(dict);
                //        var res = await client.PostAsync(refreshTokenUri, content);
                //        if (res.IsSuccessStatusCode)
                //        {
                //            var responseBody = res.Content.ReadAsStringAsync().Result;
                //            var wso2Result = JsonSerializer.Deserialize<WSO2Result>(responseBody);

                //            // Caculate expires time
                //            var dt = DateTime.Now;
                //            wso2Result.start_time = dt.ToString(CultureInfo.InvariantCulture);
                //            wso2Result.expires_time = dt.AddSeconds(wso2Result.expires_in).ToString(CultureInfo.InvariantCulture);

                //            var result = new ResponseObject<WSO2Result>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                //            return Helper.TransformData(result);
                //        }
                //        else
                //        {
                //            Log.Error($"WSO2 Error: " + JsonSerializer.Serialize(res));
                //            //return null;
                //            throw new Exception("Lỗi kế nối dịch vụ, không lấy được Token");
                //        }
                //    }
                //}
                #endregion

                #region Code WebRequest
                var webRequest = (HttpWebRequest)WebRequest.Create(refreshTokenUri);
                webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                webRequest.Method = "POST";
                var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_clientId + ":" + _clientSecret));
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Accept = "application/json, text/javascript, */*";
                webRequest.Headers.Add("Authorization", "Basic " + encoded);

                using (var jsonResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    var jsonStream = jsonResponse.GetResponseStream();

                    var ms = new MemoryStream();
                    jsonStream?.CopyTo(ms);
                    ms.Position = 0;
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(ms)
                    };
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var wso2Result = JsonSerializer.Deserialize<WSO2Result>(responseBody);

                    // Caculate expires time
                    var dt = DateTime.Now;
                    wso2Result.start_time = dt.ToString(CultureInfo.InvariantCulture);
                    wso2Result.expires_time = dt.AddSeconds(wso2Result.expires_in).ToString(CultureInfo.InvariantCulture);

                    //#region Lưu access_token vào cache với thời gian validate là 10p
                    //var wso2CacheKey = $"{CacheConstants.WSO2_ACCESSTOKEN}-{wso2Result.access_token}";

                    //WSO2AccessTokenCacheModel accessTokenCacheModel = new WSO2AccessTokenCacheModel()
                    //{
                    //    CreatedDate = DateTime.Now,
                    //    ExpireAt = DateTime.Now.AddMinutes(_wso2CacheLiveMinutes),
                    //    AccessToken = wso2Result.access_token
                    //};

                    //await _cacheService.GetOrCreate(wso2CacheKey, () =>
                    //{
                    //    return accessTokenCacheModel;
                    //});
                    //#endregion

                    var result = new ResponseObject<WSO2Result>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                    return Helper.TransformData(result);
                }
                #endregion
            }
            catch (Exception ex)
            {
                var result = new Response(Code.ServerError, "Có lỗi trong quá trình xử lý: " + ex.Message);
                return Helper.TransformData(result);
            }
        }

        /// <summary>
        /// API lấy claim user dựa vào access token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Kết quả trả về</returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("{token}")]
        [ProducesResponseType(typeof(ResponseObject<WSO2UserInfo>), StatusCodes.Status200OK)]
        public async Task<Response> GetUserInfoByAccessToken(string token)
        {

            var userInfoUri = new Uri(_serviceValidate + "oauth2/userinfo")
            .AddQuery("schema", "openid");

            var webRequest = (HttpWebRequest)WebRequest.Create(userInfoUri);
            webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Accept = "application/json, text/javascript, */*";
            webRequest.Headers.Add("Authorization", "Bearer " + token);
            try
            {
                using (var jsonResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    var jsonStream = jsonResponse.GetResponseStream();

                    var ms = new MemoryStream();
                    jsonStream?.CopyTo(ms);
                    ms.Position = 0;
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(ms)
                    };
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var wso2Result = JsonSerializer.Deserialize<WSO2UserInfo>(responseBody);

                    //Log.Information("Get User Info ResponseBody: " + System.Text.Json.JsonSerializer.Serialize(responseBody));
                    if (wso2Result.sub != null && wso2Result.sub != "")
                    {
                        ResponseObject<WSO2UserInfo> result = null;
                        //var cacheKey = $"{CacheConstants.USER}-{CacheConstants.LIST_SELECT}";

                        //var rs = _cacheService.GetOrCreate(cacheKey, () =>
                        //{
                        //    var userResult = (from item in _dataContext.User.Where(x => x.Status == true && x.IsDeleted == false && x.Type == UserType.USER).OrderBy(x => x.Order).ThenBy(x => x.UserName)
                        //                      select new UserSelectItemModel()
                        //                      {
                        //                          Id = item.Id,
                        //                          Code = item.UserName,
                        //                          DisplayName = item.Name + " - " + item.UserName,
                        //                          Name = item.Name,
                        //                          Email = item.Email,
                        //                          PhoneNumber = item.PhoneNumber,
                        //                          PositionName = item.PositionName,
                        //                          OrganizationId = item.OrganizationId,
                        //                          Note = item.Email,
                        //                          CreatedDate = item.CreatedDate,
                        //                          EFormConfig = item.EFormConfig,
                        //                          HasUserPIN = !string.IsNullOrEmpty(item.UserPIN)
                        //                      }).ToList();
                        //    return userResult;
                        //});

                        var rs = await _userHandler.GetListUserFromCache();
                        var user = rs.Where(x => x.Code.ToLower().Trim() == wso2Result.sub.ToLower().Trim()).FirstOrDefault();

                        if (user != null)
                        {
                            wso2Result.id = user.Id;
                            wso2Result.user_id = user.Id;
                            wso2Result.user_name = user.Code;
                            wso2Result.display_name = user.Name;
                            wso2Result.email = user.Email;
                            wso2Result.app_id = AppConstants.RootAppId;
                            wso2Result.organization_id = user.OrganizationId;
                            result = new ResponseObject<WSO2UserInfo>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                            return result;
                        }

                        result = new ResponseObject<WSO2UserInfo>(null, MessageConstants.GetDataErrorMessage, Code.NotFound);
                        return result;

                    }
                    else
                    {
                        var result = new ResponseObject<WSO2UserInfo>(null, MessageConstants.GetDataErrorMessage, Code.NotFound);
                        return result;
                    }

                    //var result = new ResponseObject<WSO2UserInfo>(wso2Result, MessageConstants.GetDataSuccessMessage, Code.Success);
                    //return result;
                }
            }
            catch (Exception ex)
            {
                var result = new Response(Code.ServerError, "Có lỗi trong quá trình xử lý: " + ex.Message);
                return result;
            }
        }

        private async Task<WSO2UserInfo> GetUserInfoByJWTAccessToken(string token)
        {
            var userName = Helper.GetValueFromJWTTokenByKey(token, "sub");
            //Log.Information("Get User Info ResponseBody: " + System.Text.Json.JsonSerializer.Serialize(responseBody));
            if (!string.IsNullOrEmpty(userName))
            {
                WSO2UserInfo wso2Result = new WSO2UserInfo();

                var listUser = await _userHandler.GetListUserFromCache();

                //TODO: Hiện tại sub chỉ trả về userName, chưa có UserStore => cần phải xử lý thêm case này
                var user = listUser.Where(x => x.Code.ToLower() == userName.ToLower()).FirstOrDefault();

                //Nếu không tìm thấy người dùng có tên chính xác thì tìm trong userStore xem có người dùng phù hợp hay không
                if (user == null)
                {
                    user = listUser.Where(x => x.Code.ToLower().EndsWith("/" + userName.ToLower())).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                }

                UserRoleModel userRole = null;

                if (user != null)
                {
                    // kiểm tra người dùng có bị khóa hay không
                    if (user.IsLock)
                    {
                        wso2Result.is_lock = user.IsLock;
                        wso2Result.message = "Tài khoản đã bị khóa!";
                        return wso2Result;
                    }
                    wso2Result.id = user.Id;
                    wso2Result.user_id = user.Id;
                    wso2Result.user_name = user.Code;
                    wso2Result.display_name = user.Name;
                    wso2Result.email = user.Email;
                    wso2Result.app_id = AppConstants.RootAppId;
                    wso2Result.organization_id = user.OrganizationId;
                }
                else
                {
                    var userModel = new UserCreateModel()
                    {
                        UserName = userName
                    };
                    var userCreated = await _userHandler.CreateUserIfNotExists(userModel);

                    if (userCreated != null)
                    {
                        wso2Result.id = userCreated.Id;
                        wso2Result.user_id = userCreated.Id;
                        wso2Result.user_name = userCreated.Code;
                        wso2Result.display_name = userCreated.Name;
                        wso2Result.email = userCreated.Email;
                        wso2Result.app_id = AppConstants.RootAppId;
                        wso2Result.organization_id = userCreated.OrganizationId;
                    }
                    else
                    {
                        return null;
                    }
                }

                //// kiểm tra người dùng đã được phân quyền chưa     
                //if (userRole == null || (userRole != null && !userRole.IsOrgAdmin && !userRole.IsSystemAdmin && !userRole.IsUser))
                //{
                //    wso2Result.roles = new List<string>();
                //    return wso2Result;
                //}

                //var roles = new List<string>();
                //if (userRole.IsOrgAdmin)
                //    roles.Add(UserRoles.ORG_ADMIN);
                //if (userRole.IsSystemAdmin)
                //    roles.Add(UserRoles.SYS_ADMIN);
                //if (userRole.IsUser)
                //    roles.Add(UserRoles.USER);

                //wso2Result.roles = roles;

                //Lấy danh sách quyền người dùng
                if (!wso2Result.organization_id.HasValue || wso2Result.organization_id == null)
                {
                    wso2Result.roles = new List<string>();
                }
                else
                {
                    var listUserRole = await _userHandler.GetUserRoleFromCacheAsync(wso2Result.user_id);

                    var lsRole = await _roleHandler.GetListRoleFromCache(wso2Result.organization_id);
                    var listCodeRole = lsRole.Where(x => listUserRole.Contains(x.Id)).Select(x => x.Code).ToList();
                    wso2Result.roles = listCodeRole;
                }

                return wso2Result;
            }
            else
            {
                return null;
            }
        }

        private async Task<UserModel> GetUserModelByJWTAccessToken(string token)
        {
            var userName = Helper.GetValueFromJWTTokenByKey(token, "sub");
            //Log.Information("Get User Info ResponseBody: " + System.Text.Json.JsonSerializer.Serialize(responseBody));
            if (!string.IsNullOrEmpty(userName))
            {
                UserModel userModel = new UserModel();
                var listUser = await _userHandler.GetListUserFromCache();

                //TODO: Hiện tại sub chỉ trả về userName, chưa có UserStore => cần phải xử lý thêm case này
                var user = listUser.Where(x => x.Code.ToLower() == userName.ToLower()).FirstOrDefault();

                //Nếu không tìm thấy người dùng có tên chính xác thì tìm trong userStore xem có người dùng phù hợp hay không
                if (user == null)
                {
                    user = listUser.Where(x => x.Code.ToLower().EndsWith("/" + userName.ToLower())).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                }

                if (user != null)
                {
                    userModel = new UserModel()
                    {
                        Id = user.Id,
                        UserName = user.Code,
                        Email = user.Email,
                        Name = user.Name,
                        OrganizationId = user.OrganizationId,
                        IdentityNumber = user.IdentityNumber,
                        PhoneNumber = user.PhoneNumber,
                        EFormConfig = user.EFormConfig,
                        HasUserPIN = user.HasUserPIN,
                        IsInternalUser = user.IsInternalUser,
                        IsEKYC = user.IsEKYC
                    };
                    return userModel;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    #region WSO2Result
    public class WSO2Result
    {
#pragma warning disable IDE1006 // Naming Styles
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
        public int expires_in { get; set; }
        public string start_time { get; set; }
        public string expires_time { get; set; }
        public string token_type { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public WSO2UserInfo UserInfo { get; set; }
        public UserModel UserModel { get; set; }
    }

    public class WSO2UserInfo
    {
#pragma warning disable IDE1006 // Naming Styles
        public Guid user_id { get; set; }
        public Guid app_id { get; set; }
        public Guid? organization_id { get; set; }
        public Guid id { get; set; }
        public List<string> rights { get; set; }
        public List<string> roles { get; set; }
        public string user_name { get; set; }
        public string display_name { get; set; }
        public string? email { get; set; }
        public string sub { get; set; }
        public string message { get; set; }
        public bool is_lock { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
    #endregion

    #region Cache model
    public class WSO2AccessTokenCacheModel
    {
        public DateTime CreatedDate { get; set; }
        public string AccessToken { get; set; }
        public DateTime ExpireAt { get; set; }
    }
    #endregion
}



