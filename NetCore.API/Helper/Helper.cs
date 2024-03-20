using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using NetCore.Business;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetCore.API
{
    public static class Helper
    {
        public const string ErrorMessage_TokenExpired = "TokenExpired";
        public const string ErrorMessage_Unauthorized = "Unauthorized";
        public const string ErrorMessage_IncorrectIssuer = "IncorrectIssuer";
        public const string ErrorMessage_IncorrectInput = "IncorrectInput";
        public static bool ValidAuthen(HttpRequest request)
        {
            try
            {
                string authHeader = request.Headers["Authorization"];
                var handerJwt = new JwtSecurityTokenHandler();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    string accessToken = Guid.NewGuid().ToString();
                    // Get the token
                    var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                    var tokenInfo = handerJwt.ReadJwtToken(token);
                    var userId = tokenInfo.Claims.Where(x => x.Type == "nameid").FirstOrDefault().Value;
                    //bool rt = IdmHelper.ValidTokenFromDB(new Guid(userId), token);
                    //return rt;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetConsumerKeyFromRequest(HttpRequest request)
        {
            try
            {
                var headers = request.Headers;

                //foreach (var item in headers)
                //{
                //    Console.WriteLine($"----{item.Key}: " + item.Value);
                //}

                string token = request.Headers["X-OrganizationToken"];
                var handerJwt = new JwtSecurityTokenHandler();
                if (token != null)
                {
                    // Get the token
                    token = token.Trim();
                    var tokenInfo = handerJwt.ReadJwtToken((string)token);
                    var consumerKey = tokenInfo.Claims.Where(x => x.Type == "consumerKey" || x.Type == "azp").FirstOrDefault().Value;
                    return consumerKey;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetValueFromJWTTokenByKey(string token, string key)
        {
            try
            {
                var handerJwt = new JwtSecurityTokenHandler();
                if (token != null)
                {
                    // Get the token
                    token = token.Trim();
                    var tokenInfo = handerJwt.ReadJwtToken((string)token);
                    var consumerKey = tokenInfo.Claims.Where(x => x.Type == key).FirstOrDefault().Value;
                    return consumerKey;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Transform data to http response
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ActionResult TransformData(Response data)
        {
            var result = new ObjectResult(data) { StatusCode = (int)data.Code };
            return result;
        }

        /// <summary>
        /// Get user info in token and headder
        /// </summary>
        /// <param name="request"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        public static RequestUser GetRequestInfo(HttpRequest request, ClaimsPrincipal currentUser)
        {

            var result = new RequestUser
            {
                UserId = Guid.Empty,
                ApplicationId = Guid.Empty
            };


            //UserId
            if (currentUser.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
            {
                var userId = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && Utils.IsGuid(userId))
                {
                    result.UserId = new Guid(userId);
                }
            }
            else
            {
                request.Headers.TryGetValue("X-UserId", out StringValues userId);
                if (!string.IsNullOrEmpty(userId) && Utils.IsGuid(userId))
                {
                    result.ApplicationId = new Guid(userId);
                }
            }
            //AppId
            if (currentUser.HasClaim(c => c.Type == ClaimTypes.Sid))
            {
                var appId = currentUser.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;
                if (!string.IsNullOrEmpty(appId) && Utils.IsGuid(appId))
                {
                    result.UserId = new Guid(appId);
                }
            }
            else
            {
                request.Headers.TryGetValue("X-ApplicationId", out StringValues applicationId);
                if (!string.IsNullOrEmpty(applicationId) && Utils.IsGuid(applicationId))
                {
                    result.ApplicationId = new Guid(applicationId);
                }
            }
            return result;
        }

        public static async Task<RequestUser> GetRequestInfo(HttpRequest request)
        {
            var claims = request.HttpContext.User.Claims;

            string userID = request.Headers["UserId"];
            Guid userGuidId = Guid.Empty;
            Guid.TryParse(userID, out userGuidId);

            string appId = request.Headers["X-ApplicationId"];
            Guid appGuidId = Guid.Empty;
            Guid.TryParse(appId, out appGuidId);

            string orgId = request.Headers["X-OrganizationId"];
            Guid orgGuidId = Guid.Empty;
            Guid.TryParse(orgId, out orgGuidId);

            var requestUser = new RequestUser()
            {
                UserId = userID == null ? Guid.Empty : userGuidId,
                ApplicationId = appId == null ? Guid.Empty : appGuidId,
                OrganizationId = orgId == null ? Guid.Empty : orgGuidId,
            };
            if (claims == null || claims.Count() == 0)
            {
                var authenticateResult = await CustomAuthentication(request);
                if (authenticateResult.Succeeded)
                {
                    claims = authenticateResult.Principal.Claims;
                }
                else
                {
                    //throw new AuthenticationException(authenticateResult.Failure.Message);
                }
            }

            #region SystemLog
            //OperatingSystem
            DataLog.OperatingSystem osObj = new DataLog.OperatingSystem();
            string osBase64 = request.Headers["operating-system"];
            if (!string.IsNullOrEmpty(osBase64))
            {
                try
                {
                    string os = Utils.Base64Decode(osBase64);
                    osObj = JsonSerializer.Deserialize<DataLog.OperatingSystem>(os);
                }
                catch (Exception) { }
            }

            //location
            Location locationObj = new Location();
            string locationBase64 = request.Headers["location"];
            if (!string.IsNullOrEmpty(locationBase64))
            {
                try
                {
                    string location = Utils.Base64Decode(locationBase64);
                    locationObj = JsonSerializer.Deserialize<Location>(location);
                }
                catch (Exception) { }
            }

            var ip = GetIPAddress(request);
            SystemLogModel sysLog = new SystemLogModel()
            {
                TraceId = Guid.NewGuid().ToString(),
                IP = ip,
                Location = new DataLog.Location()
                {
                    Latitude = locationObj.Latitude,
                    Longitude = locationObj.Longitude,
                },
                OperatingSystem = osObj,
                CreatedDate = DateTime.Now,
                Device = request.Headers["X-Device"],
                //UserId = userGuidId.ToString(),
                //OrganizationId = orgGuidId.ToString(),
                ApplicationId = appGuidId.ToString(),
                ListAction = new List<ActionDetail>()
            };
            requestUser.SystemLog = sysLog;
            #endregion

            return GetRequestInfo(claims, requestUser);
        }

        public class Location
        {
            [JsonPropertyName("latitude")]
            public decimal? Latitude { get; set; }
            [JsonPropertyName("longitude")]
            public decimal? Longitude { get; set; }
        }

        public static RequestUser GetRequestInfo(IEnumerable<Claim> claims, RequestUser request = null)
        {
            var result = new RequestUser()
            {
                SystemLog = request.SystemLog ?? null,
                UserId = claims.GetGuidClaim(ClaimConstants.USER_ID) ?? (request != null ? request.UserId : Guid.Empty),
                OrganizationId = claims.GetGuidClaim(ClaimConstants.ORG_ID) ?? (request != null ? request.OrganizationId : Guid.Empty),
                //UserName = claims.GetStringClaim(ClaimConstants.USER_NAME),
                //FullName = claims.GetStringClaim(ClaimConstants.FULL_NAME),
                //Avatar = claims.GetStringClaim(ClaimConstants.AVATAR),
                ApplicationId = claims.GetGuidClaim(ClaimConstants.APP_ID) ?? (request != null ? request.ApplicationId : Guid.Empty),
                //ListApps = claims.GetListStringClaim(ClaimConstants.APPS),
                //ListRoles = claims.GetListStringClaim(ClaimConstants.ROLES),
                //ListRights = claims.GetListStringClaim(ClaimConstants.RIGHTS),
            };

            result.SystemLog.UserId = result.UserId.ToString();
            result.SystemLog.OrganizationId = result.OrganizationId.ToString();

            return result;
        }

        public static string GetIPAddress(HttpRequest request)
        {
            //var remoteIpAddress = request.HttpContext.Connection.RemoteIpAddress;
            //return remoteIpAddress.ToString();
            string ipAddress = request.Headers["X-Real-IP"];
            return ipAddress;
        }

        public static async Task<AuthenticateResult> CustomAuthentication(HttpRequest request)
        {
            AuthenticateResult result = null;
            try
            {
                #region NoAuth
                if (Utils.GetConfig("Authentication:NoAuth:Enable") == "true")
                {
                    var claims = new List<Claim>();
                    claims.Add(new Claim(ClaimConstants.USER_ID, UserConstants.AdministratorId.ToString()));
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "NONE_AUTH");
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal,
                                        new AuthenticationProperties(), "NONE_AUTH"));
                    return result;
                }
                #endregion
                #region Basic
                if (Utils.GetConfig("Authentication:Basic:Enable") == "true")
                {
                    string tokenString = request.Headers["Authorization"];
                    if (tokenString != null && tokenString.StartsWith("Basic "))
                    {
                        // Get the encoded username and password
                        var encodedUsernamePassword = tokenString.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        // Decode from Base64 to string
                        var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                        // Split username and password
                        var username = decodedUsernamePassword.Split(':', 2)[0];
                        var password = decodedUsernamePassword.Split(':', 2)[1];
                        // Check if login is correct
                        if (username == Utils.GetConfig("Authentication:AdminUser") && password == Utils.GetConfig("Authentication:AdminPassWord"))
                        {

                            var claimsBasic = new List<Claim>();
                            claimsBasic.Add(new Claim(ClaimConstants.USER_ID, UserConstants.AdministratorId.ToString()));
                            ClaimsIdentity claimsIdentityBasic = new ClaimsIdentity(claimsBasic, "Basic");
                            ClaimsPrincipal claimsPrincipalBasic = new ClaimsPrincipal(claimsIdentityBasic);

                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalBasic,
                                                    new AuthenticationProperties(), "Basic"));
                            return result;
                        }
                    }
                }
                #endregion
                #region JWT - More
                if (Utils.GetConfig("Authentication:Jwt:Enable") == "true")
                {
                    string tokenString = request.Headers["Authorization"];

                    if (Utils.GetConfig("Authentication:Jwt:XPermission") == "true" && !string.IsNullOrWhiteSpace(request.Headers["X-Permission"]))
                    {
                        tokenString = request.Headers["X-Permission"];
                    }
                    if (Utils.GetConfig("Authentication:Jwt:Cookie") == "true" && !string.IsNullOrWhiteSpace(request.HttpContext.Request.Cookies["access_token"]))
                    {
                        tokenString = request.HttpContext.Request.Cookies["access_token"];
                    }

                    if (tokenString == null)
                    {
                        return AuthenticateResult.Fail(ErrorMessage_Unauthorized);
                    }

                    if (tokenString == Utils.GetConfig("Authentication:Jwt:Anonymous"))
                    {
                        var claimsJwt = new List<Claim>(){
                                new Claim(ClaimConstants.USER_NAME, "Anonymous"),
                                new Claim(ClaimConstants.FULL_NAME, "Anonymous"),
                                new Claim(ClaimConstants.AVATAR, ""),
                                new Claim(ClaimConstants.USER_ID, ""),
                                new Claim(ClaimConstants.APP_ID,""),
                                new Claim(ClaimConstants.APPS,""),
                                new Claim(ClaimConstants.ROLES,""),
                                new Claim(ClaimConstants.RIGHTS,""),
                                new Claim(ClaimConstants.APPS,""),
                                new Claim(ClaimConstants.EXPIRES_AT,""),
                                new Claim(ClaimConstants.ISSUED_AT,"")
                             };
                        ClaimsIdentity claimsIdentityJwt = new ClaimsIdentity(claimsJwt, "Jwt");
                        ClaimsPrincipal claimsPrincipalJwt = new ClaimsPrincipal(claimsIdentityJwt);

                        result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalJwt,
                                            new AuthenticationProperties(), "Jwt"));
                        return result;
                    }

                    // Get the token
                    var token = tokenString.Replace("Bearer", "").Trim();
                    // validatetoken
                    var handerJwt = new JwtSecurityTokenHandler();
                    var tokenInfo = handerJwt.ReadJwtToken(token);
                    SecurityToken validatedToken;
                    handerJwt.ValidateToken(token, new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Utils.GetConfig("Authentication:Jwt:Issuer"),
                        ValidAudience = Utils.GetConfig("Authentication:Jwt:Issuer"),
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Utils.GetConfig(("Authentication:Jwt:Key"))))
                    }, out validatedToken);

                    if (validatedToken == null)
                    {
                        return AuthenticateResult.Fail(ErrorMessage_Unauthorized);
                    }
                    else if (validatedToken.Issuer != Utils.GetConfig("Authentication:Jwt:Issuer"))
                    {
                        return AuthenticateResult.Fail(ErrorMessage_IncorrectIssuer);
                    }
                    else if (validatedToken.ValidFrom > DateTime.Now || validatedToken.ValidTo < DateTime.Now)
                    {
                        return AuthenticateResult.Fail(ErrorMessage_TokenExpired);
                    }
                    else if (validatedToken.ValidFrom < DateTime.Now && validatedToken.ValidTo > DateTime.Now)
                    {
                        var claimsJwt = new List<Claim>();
                        var userId = tokenInfo.Claims.Where(x => x.Type == ClaimConstants.USER_ID).FirstOrDefault().Value;
                        claimsJwt.Add(new Claim(ClaimConstants.USER_ID, userId.ToString()));
                        claimsJwt.AddRange(tokenInfo.Claims);
                        ClaimsIdentity claimsIdentityJwt = new ClaimsIdentity(claimsJwt, "Jwt");
                        ClaimsPrincipal claimsPrincipalJwt = new ClaimsPrincipal(claimsIdentityJwt);

                        result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalJwt, new AuthenticationProperties(), "Jwt"));
                        return result;
                    }

                    return AuthenticateResult.Fail(ErrorMessage_Unauthorized);
                }
                #endregion
                #region WSO2
                if (Utils.GetConfig("Authentication:WSO2:Enable") == "true")
                {
                    string tokenString = request.Headers["Authorization"];
                    string _serviceValidate = Utils.GetConfig("Authentication:WSO2:Uri");
                    string _clientId = Utils.GetConfig("Authentication:WSO2:Clientid");
                    string _clientSecret = Utils.GetConfig("Authentication:WSO2:Secret");
                    string _redirectUri = Utils.GetConfig("Authentication:WSO2:Redirecturi");
                    if (tokenString != null && tokenString.StartsWith("Bearer "))
                    {
                        string accessToken = Guid.NewGuid().ToString();
                        // Get the token
                        var token = tokenString.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        accessToken = token;
                        Uri refreshTokenUri = new Uri(_serviceValidate + "oauth2/userinfo")
                        .AddQuery("schema", "openid");

                        var webRequest = (System.Net.HttpWebRequest)WebRequest.Create(refreshTokenUri);
                        webRequest.Method = "POST";
                        webRequest.ContentType = "application/x-www-form-urlencoded";
                        webRequest.Accept = "application/json, text/javascript, */*";
                        webRequest.Headers.Add("Authorization", "Bearer " + accessToken);

                        using (var jsonResponse = (HttpWebResponse)webRequest.GetResponse())
                        {
                            var jsonStream = jsonResponse.GetResponseStream();

                            MemoryStream ms = new MemoryStream();
                            jsonStream.CopyTo(ms);
                            ms.Position = 0;
                            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                            response.Content = new StreamContent(ms);
                            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                            await response.Content.ReadAsStringAsync();

                            var claimsWSO2 = new List<Claim>();
                            claimsWSO2.Add(new Claim(ClaimConstants.USER_ID, UserConstants.AdministratorId.ToString()));
                            ClaimsIdentity claimsIdentityWSO2 = new ClaimsIdentity(claimsWSO2, "Bear");
                            ClaimsPrincipal claimsPrincipalWSO2 = new ClaimsPrincipal(claimsIdentityWSO2);

                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalWSO2,
                                    new AuthenticationProperties(), "Bear"));
                            return result;
                        }
                    }
                }
                #endregion
                #region APIKEY
                if (Utils.GetConfig("Authentication:APIKey:Enable") == "true")
                {
                    string tokenString = request.Headers["Authorization"];
                    if (tokenString != null && tokenString.StartsWith("APIKEY "))
                    {
                        string accessToken = Guid.NewGuid().ToString();
                        // Get the token
                        var token = tokenString.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        // validatetoken

                        // var dateTimeExpired = TokenHelpers.GetDateTimeExpired(token, Utils.GetConfig("Authentication:APIKey:Key"));
                        // if (dateTimeExpired.HasValue && dateTimeExpired.Value.CompareTo(DateTime.Now) != -1)
                        // {
                        var objectId = TokenHelpers.GetKeyFromBasicToken(token);
                        if (objectId != null)
                        {
                            var claimsAPIKEY = new List<Claim>();
                            // claimsAPIKEY.Add(new Claim(ClaimConstants.APP_ID, objectId.ToString()));
                            ClaimsIdentity claimsIdentityAPIKEY = new ClaimsIdentity(claimsAPIKEY, "Bear");
                            ClaimsPrincipal claimsPrincipalAPIKEY = new ClaimsPrincipal(claimsIdentityAPIKEY);
                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalAPIKEY, new AuthenticationProperties(), "APIKEY"));
                        }

                        // }

                        return result;
                    }
                }

                #endregion
                return AuthenticateResult.Fail(ErrorMessage_Unauthorized);

            }
            catch (Exception ex)
            {
                //Log.Error(ex, "");
                return AuthenticateResult.Fail(ErrorMessage_Unauthorized);
            }
        }

        public static RequestUser GetRequestInfo(ClaimsPrincipal user)
        {

            var claims = user.Claims;
            return GetRequestInfo(claims);
        }

        private static string GetStringClaim(this IEnumerable<Claim> claims, string key)
        {
            var obj = claims.FirstOrDefault(x => x.Type == key);
            if (obj != null)
                return obj.Value;
            return null;
        }

        private static Guid? GetGuidClaim(this IEnumerable<Claim> claims, string key)
        {

            var obj = claims.FirstOrDefault(x => x.Type == key);
            if (obj != null && !string.IsNullOrEmpty(obj.Value) && Utils.IsGuid(obj.Value))
                return new Guid(obj.Value);
            return null;
        }

        private static DateTime? GetDateClaim(this IEnumerable<Claim> claims, string key)
        {
            var obj = claims.FirstOrDefault(x => x.Type == key);
            if (obj != null && !string.IsNullOrEmpty(obj.Value))
                return DateTime.Parse(obj.Value);
            return null;
        }

        private static List<string> GetListStringClaim(this IEnumerable<Claim> claims, string key)
        {
            var obj = claims.FirstOrDefault(x => x.Type == key);
            return obj != null && !string.IsNullOrEmpty(obj.Value) ? JsonSerializer.Deserialize<List<string>>(obj.Value) : null;
        }

        private static List<Guid> GetListGuidClaim(this IEnumerable<Claim> claims, string key)
        {
            var obj = claims.FirstOrDefault(x => x.Type == key);
            return obj != null && !string.IsNullOrEmpty(obj.Value) ? JsonSerializer.Deserialize<List<Guid>>(obj.Value) : null;
        }
    }

    public static class CoreHelper
    {
        public static string MakePrefix(Guid? appId)
        {
            if (appId.HasValue)
            {
                //var app = ApplicationCollection.Instance.GetModel(appId.Value);
                //if (app != null)
                return "_" + "Demo code";
            }

            return null;
        }
        public static string GetTrueName(string name)
        {
            switch (name)
            {
                case "Extension": return "Định dạng";
                case "Path": return "Đường dẫn";
                case "Status": return "Trạng thái";
                case "Level": return "Định dạng";
                case "Content": return "Nội dung";
                case "Tags": return "Tag";
            }
            if (name.Contains("Metadata.")) name = name.Replace("Metadata.", "");

            return name;
        }
    }

    public class RequestUser
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid OrganizationId { get; set; }
        public SystemLogModel SystemLog { get; set; }
    }
}
