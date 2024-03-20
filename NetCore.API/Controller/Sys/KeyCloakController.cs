using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Data;
using NetCore.Shared;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.API.Controller.Sys
{
    /// <summary>
    /// Tích hợp KeyCloak
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/keycloak")]
    [ApiExplorerSettings(GroupName = "System - KeyCloak", IgnoreApi = true)]
    public class KeyCloakController : ApiControllerBase
    {
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IUserHandler _userHandler;
        private readonly IRoleHandler _roleHandler;
        private readonly string _keyCloakUri = Utils.GetConfig("KeyCloak:uri");
        private readonly string _clientId = Utils.GetConfig("KeyCloak:clientId");
        private readonly string _redirectUri = Utils.GetConfig("Authentication:KeyCloak:redirectUri");

        public KeyCloakController(ICacheService cacheService, DataContext dataContext, IUserHandler userHandler, IRoleHandler roleHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _userHandler = userHandler;
            _roleHandler = roleHandler;
        }

        [AllowAnonymous, HttpGet, Route("get-access-token")]
        public async Task<IActionResult> GetAccessToken(string authorizationCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                try
                {
                    using (HttpClientHandler clientHandler = new HttpClientHandler())
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, cert, chain, sslPolicyErrors) => { return true; };

                        using (var client = new HttpClient(clientHandler))
                        {
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                            var data = new[]
                            {
                                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                                new KeyValuePair<string, string>("client_id", _clientId),
                                new KeyValuePair<string, string>("code", authorizationCode),
                                new KeyValuePair<string, string>("redirect_uri", _redirectUri),
                            };
                            HttpResponseMessage res = await client.PostAsync(_keyCloakUri + "protocol/openid-connect/token", new FormUrlEncodedContent(data));
                            var responseText = await res.Content.ReadAsStringAsync();
                            Log.Logger.Information("Request Get Token response model: " + responseText);

                            if (res.IsSuccessStatusCode)
                            {
                                var rs = System.Text.Json.JsonSerializer.Deserialize<KeyCloakResponseResult>(responseText);
                                return new ResponseObject<KeyCloakResponseResult>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
                            }
                            else
                            {
                                Log.Logger.Error("Request Get Token error: " + responseText);
                                return new ResponseError(Code.ServerError, MessageConstants.GetDataSuccessMessage);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new Response(Code.ServerError, ex.Message);
                }
            });
        }

        /// <summary>
        /// API lấy claim user dựa vào access token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Kết quả trả về</returns>
        /// <response code="200">Thành công</response>
        [HttpPost, Route("get-user-info")]
        [ProducesResponseType(typeof(ResponseObject<KeyCloakUserInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserInfoByAccessToken(string token)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {                              
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                        HttpResponseMessage res = await client.GetAsync(_keyCloakUri + "protocol/openid-connect/userinfo");
                        var responseText = await res.Content.ReadAsStringAsync();
                        Log.Logger.Information("Get User info: " + responseText);

                        if (res.IsSuccessStatusCode)
                        {
                            var userInfoRs = System.Text.Json.JsonSerializer.Deserialize<KeyCloakUserInfoResponse>(responseText);
                            string userName = userInfoRs.preferred_username;

                            if (string.IsNullOrEmpty(userName)) return new ResponseError(Code.BadRequest, "Không tìm thấy Username");

                            var rs = await GetUserInfoByUserName(userName);
                            return rs;
                        }
                        else
                        {
                            return new ResponseError(Code.ServerError, "Lỗi không thể xác thực token!");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// API làm mới access token dựa vào refresh token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Access token</returns>
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("refreshtoken")]
        [ProducesResponseType(typeof(ResponseObject<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewAccessTokenByRefreshToken(string token)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await RefreshToken(token);
            });
        }

        /// <summary>
        /// API Logout
        /// </summary>
        /// <param name="refresh token"></param>
        /// <returns>Access token</returns>
        /// <response code="200">Thành công</response>
        [HttpGet, Route("logout")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> KeyCloakLogout(string refreshToken)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await Logout(refreshToken);
            });
        }

        private async Task<Response> Logout(string refreshToken)
        {
            try
            {
                string authHeader = Request.Headers["Authorization"];
                var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                       
                        var data = new[]
                        {
                            new KeyValuePair<string, string>("client_id", _clientId),
                            new KeyValuePair<string, string>("refresh_token", refreshToken),
                        };
                        HttpResponseMessage res = await client.PostAsync(_keyCloakUri + "protocol/openid-connect/logout", new FormUrlEncodedContent(data));

                        return new ResponseObject<bool>(true, "Logout thành công.", Code.Success);
                    }
                }
            }
            catch(Exception ex)
            {
                return new ResponseError(Code.ServerError, ex.Message);
            }
        }

        private async Task<Response> RefreshToken(string refreshToken)
        {
            #region Request Keycloak get new access token
            using (HttpClientHandler clientHandler = new HttpClientHandler())
            {
                clientHandler.ServerCertificateCustomValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (var client = new HttpClient(clientHandler))
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                    var data = new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                    };
                    HttpResponseMessage res = await client.PostAsync(_keyCloakUri + "protocol/openid-connect/token", new FormUrlEncodedContent(data));
                    var responseText = await res.Content.ReadAsStringAsync();
                    Log.Logger.Information("Request Get Token response model: " + responseText);

                    if (res.IsSuccessStatusCode)
                    {
                        var rs = System.Text.Json.JsonSerializer.Deserialize<KeyCloakResponseResult>(responseText);
                        return new ResponseObject<KeyCloakResponseResult>(rs, "Làm mới token thành công.", Code.Success);
                    }
                    else
                    {
                        Log.Logger.Error("Request Get Token error: " + responseText);
                        return new ResponseError(Code.ServerError, "Lỗi trong quá trình làm mới Token");
                    }
                }
            }
            #endregion
        }

        public async Task<Response> GetUserInfoByUserName(string userName)
        {
            try
            {
                KeyCloakUserInfo keyCloakUserInfo = new KeyCloakUserInfo();

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
                        keyCloakUserInfo.is_lock = user.IsLock;
                        keyCloakUserInfo.message = "Tài khoản đã bị khóa!";
                        return new ResponseObject<KeyCloakUserInfo>(keyCloakUserInfo, "Tài khoản đã bị khóa!", Code.Forbidden);
                    }
                    keyCloakUserInfo.id = user.Id;
                    keyCloakUserInfo.user_id = user.Id;
                    keyCloakUserInfo.user_name = user.Code;
                    keyCloakUserInfo.display_name = user.Name;
                    keyCloakUserInfo.email = user.Email;
                    keyCloakUserInfo.app_id = AppConstants.RootAppId;
                    keyCloakUserInfo.organization_id = user.OrganizationId;
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
                        keyCloakUserInfo.id = userCreated.Id;
                        keyCloakUserInfo.user_id = userCreated.Id;
                        keyCloakUserInfo.user_name = userCreated.Code;
                        keyCloakUserInfo.display_name = userCreated.Name;
                        keyCloakUserInfo.email = userCreated.Email;
                        keyCloakUserInfo.app_id = AppConstants.RootAppId;
                        keyCloakUserInfo.organization_id = userCreated.OrganizationId;
                    }
                    else
                    {
                        return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi tạo người dùng.");
                    }
                }               

                if (!keyCloakUserInfo.organization_id.HasValue || keyCloakUserInfo.organization_id == null)
                {
                    keyCloakUserInfo.roles = new List<string>();
                }
                else
                {
                    var listUserRole = await _userHandler.GetUserRoleFromCacheAsync(keyCloakUserInfo.user_id);

                    var lsRole = await _roleHandler.GetListRoleFromCache(keyCloakUserInfo.organization_id);
                    var listCodeRole = lsRole.Where(x => listUserRole.Contains(x.Id)).Select(x => x.Code).ToList();
                    keyCloakUserInfo.roles = listCodeRole;
                }

                return new ResponseObject<KeyCloakUserInfo>(keyCloakUserInfo, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch(Exception ex)
            {
                return new ResponseError(Code.ServerError, ex.Message);
            }
        }
    }

    public class KeyCloakUserInfoResponse
    {
        public string sub { get; set; }
        public bool email_verified { get; set; }
        public string name { get; set; }
        public string preferred_username { get; set; }
        public string given_name { get; set; }
        public string family_name { get; set; }
        public string email { get; set; }
    }

    public class KeyCloakResponseResult
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
    }

    public class KeyCloakUserInfo
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

    public class KeyCloakAccessTokenCacheModel
    {
        public DateTime CreatedDate { get; set; }
        public string AccessToken { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
