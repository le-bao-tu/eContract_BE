// -------------------------------------------------------------------------------------------------
// Copyright (c) Johan Boström. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetCore.API.Controller.Sys;
using NetCore.Business;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.API
{
    public static class CustomAuthExtensions
    {
        public static AuthenticationBuilder AddCustomAuth(this AuthenticationBuilder builder, Action<CustomAuthOptions> configureOptions)
        {
            return builder.AddScheme<CustomAuthOptions, CustomAuthHandler>("Custom Scheme", "Custom Auth", configureOptions);
        }
    }
    public class CustomAuthOptions : AuthenticationSchemeOptions
    {
        public CustomAuthOptions()
        {

        }
    }
    internal class CustomAuthHandler : AuthenticationHandler<CustomAuthOptions>
    {
        private readonly string _serviceValidate = Utils.GetConfig("Authentication:WSO2:Uri");
        private readonly int _wso2CacheLiveMinutes = Int32.Parse(Utils.GetConfig("Authentication:WSO2:CacheLiveMinutes"));
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IUserHandler _userHandler;
        private readonly string _keyCloakUri = Utils.GetConfig("KeyCloak:uri");

        public CustomAuthHandler(
            IOptionsMonitor<CustomAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            DataContext dataContext,
            ICacheService cacheService,
            IUserHandler userHandler) : base(options, logger, encoder, clock)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _userHandler = userHandler;
            // store custom services here...
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                string authHeader = Request.Headers["Authorization"];
                AuthenticateResult result = null;
                #region KeyCloak
                if (Utils.GetConfig("Authentication:KeyCloak:enabled") == "true")
                {
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        string[] tokenParts = token.Split(".");

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
                                    var rs = System.Text.Json.JsonSerializer.Deserialize<KeyCloakUserInfoResponse>(responseText);
                                    string userName = rs.preferred_username;

                                    var keycloakCache = $"{CacheConstants.KEYCLOAK_ACCESSTOKEN}-{token}";
                                    KeyCloakAccessTokenCacheModel accessTokenCache = new KeyCloakAccessTokenCacheModel
                                    {
                                        CreatedDate = DateTime.Now,
                                        AccessToken = token,
                                        ExpireAt = DateTime.Now.AddHours(Convert.ToDouble(Utils.GetConfig("Authentication:KeyCloak:expireIn")))
                                    };

                                    //Cập nhật cache mới
                                    var dtCache = await _cacheService.GetOrCreate(keycloakCache, () =>
                                    {
                                        return accessTokenCache;
                                    });

                                    var claimsKeycloak = new List<Claim>();
                                    claimsKeycloak.Add(new Claim(ClaimTypes.NameIdentifier, UserConstants.AdministratorId.ToString()));
                                    ClaimsIdentity claimsIdentityWSO2 = new ClaimsIdentity(claimsKeycloak, "Bear");
                                    ClaimsPrincipal claimsPrincipalWSO2 = new ClaimsPrincipal(claimsIdentityWSO2);

                                    var users = await _userHandler.GetListUserFromCache();
                                    if (!string.IsNullOrEmpty(userName))
                                    {
                                        //TODO: Hiện tại sub chỉ trả về userName, chưa có UserStore => cần phải xử lý thêm case này
                                        var user = users.Where(x => x.Code.ToLower() == userName.ToLower()).FirstOrDefault();

                                        //Nếu không tìm thấy người dùng có tên chính xác thì tìm trong userStore xem có người dùng phù hợp hay không
                                        if (user == null)
                                        {
                                            user = users.Where(x => x.Code.ToLower().EndsWith("/" + userName.ToLower())).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                                        }

                                        if (user != null)
                                        {
                                            Request.Headers["UserId"] = user?.Id.ToString();
                                            Request.Headers["X-OrganizationId"] = user?.OrganizationId?.ToString();
                                        }
                                    }

                                    result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalWSO2,
                                            new AuthenticationProperties(), "Bear"));

                                    return result;
                                }
                                else
                                {
                                    Log.Logger.Error("Request Get User info error: " + responseText);
                                    return AuthenticateResult.Fail("Không xác thực");
                                }
                            }
                        }                                              
                    }
                }
                #endregion
                #region WSO2
                if (Utils.GetConfig("Authentication:WSO2:Enable") == "true")
                {
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        // Get the token
                        var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        WSO2UserInfo wso2Result = new WSO2UserInfo();
                        var dateNow = DateTime.Now;

                        //Lấy thông tin access token từ cache
                        var wso2CacheKey = $"{CacheConstants.WSO2_ACCESSTOKEN}-{token}";
                        WSO2AccessTokenCacheModel accessTokenCache = new WSO2AccessTokenCacheModel();

                        accessTokenCache = await _cacheService.GetOrCreate(wso2CacheKey, () =>
                        {
                            return accessTokenCache;
                        });

                        //Nếu access token tồn tại thì kiểm tra thời gian hết hạn
                        if (accessTokenCache != null && accessTokenCache.ExpireAt > dateNow)
                        {
                            wso2Result.sub = Helper.GetValueFromJWTTokenByKey(accessTokenCache.AccessToken, "sub");
                        }
                        //Nếu không tồn tại thì gọi server để kiểm tra
                        else
                        {
                            Uri validTokenUri = new Uri(_serviceValidate + "oauth2/userinfo")
                                .AddQuery("schema", "openid");

                            #region Code HttpClient
                            //using (HttpClientHandler clientHandler = new HttpClientHandler())
                            //{
                            //    clientHandler.ServerCertificateCustomValidationCallback =
                            //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                            //    using (var client = new HttpClient(clientHandler))
                            //    {
                            //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            //        var dict = new Dictionary<string, string>();
                            //        dict.Add("Content-Type", "application/x-www-form-urlencoded");
                            //        var content = new FormUrlEncodedContent(dict);
                            //        var res = await client.PostAsync(validTokenUri, content);
                            //        if (res.IsSuccessStatusCode)
                            //        {
                            //            var responseBody = res.Content.ReadAsStringAsync().Result;
                            //            wso2Result = JsonSerializer.Deserialize<WSO2UserInfo>(responseBody);
                            //        }
                            //        else
                            //        {
                            //            Log.Error($"WSO2 Error: " + JsonSerializer.Serialize(res));
                            //            return AuthenticateResult.Fail("Không xác thực");
                            //        }
                            //    }
                            //}
                            #endregion

                            #region Code WebRequest
                            var webRequest = (System.Net.HttpWebRequest)WebRequest.Create(validTokenUri);
                            webRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                            webRequest.Method = "POST";
                            webRequest.ContentType = "application/x-www-form-urlencoded";
                            webRequest.Accept = "application/json, text/javascript, */*";
                            webRequest.Headers.Add("Authorization", "Bearer " + token);


                            using (var jsonResponse = (HttpWebResponse)webRequest.GetResponse())
                            {
                                var jsonStream = jsonResponse.GetResponseStream();

                                MemoryStream ms = new MemoryStream();
                                jsonStream.CopyTo(ms);
                                ms.Position = 0;
                                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                                response.Content = new StreamContent(ms);
                                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                                var responseBody = await response.Content.ReadAsStringAsync();
                                wso2Result = JsonSerializer.Deserialize<WSO2UserInfo>(responseBody);
                            }
                            #endregion

                            #region Lưu access_token vào cache với thời gian validate là 10p
                            accessTokenCache = new WSO2AccessTokenCacheModel()
                            {
                                CreatedDate = dateNow,
                                ExpireAt = dateNow.AddMinutes(_wso2CacheLiveMinutes),
                                AccessToken = token
                            };

                            //Xóa cache
                            _cacheService.Remove(wso2CacheKey);

                            //Cập nhật cache mới
                            var dtCache = await _cacheService.GetOrCreate(wso2CacheKey, () =>
                            {
                                return accessTokenCache;
                            });
                            #endregion
                        }

                        if (wso2Result.sub != null || wso2Result.sub != "")
                        {
                            var claimsWSO2 = new List<Claim>();
                            claimsWSO2.Add(new Claim(ClaimTypes.NameIdentifier, UserConstants.AdministratorId.ToString()));
                            ClaimsIdentity claimsIdentityWSO2 = new ClaimsIdentity(claimsWSO2, "Bear");
                            ClaimsPrincipal claimsPrincipalWSO2 = new ClaimsPrincipal(claimsIdentityWSO2);

                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalWSO2,
                                    new AuthenticationProperties(), "Bear"));
                            //var cacheKey = $"{CacheConstants.USER}-{CacheConstants.LIST_SELECT}";

                            //var rs = await _cacheService.GetOrCreate(cacheKey, () =>
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

                            //TODO: Hiện tại sub chỉ trả về userName, chưa có UserStore => cần phải xử lý thêm case này
                            var user = rs.Where(x => x.Code.ToLower() == wso2Result.sub.ToLower()).FirstOrDefault();

                            //Nếu không tìm thấy người dùng có tên chính xác thì tìm trong userStore xem có người dùng phù hợp hay không
                            if (user == null)
                            {
                                user = rs.Where(x => x.Code.ToLower().EndsWith("/" + wso2Result.sub.ToLower())).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                            }

                            if (user.IsLock)
                            {
                                return AuthenticateResult.Fail("Tài khoản đã bị khóa");
                                //Log.Information("Tài khoản đã bị khóa");
                            }

                            if (user != null)
                            {
                                Request.Headers["UserId"] = user?.Id.ToString();
                                Request.Headers["X-OrganizationId"] = user?.OrganizationId?.ToString();
                                return result;
                            }
                            else
                            {
                                return AuthenticateResult.Fail("Không xác thực");
                            }
                        }
                        else
                        {
                            return AuthenticateResult.Fail("Không xác thực");
                        }
                    }
                }
                #endregion
                #region APIKEY
                if (Utils.GetConfig("Authentication:apikey:Enable") == "true")
                {
                    if (authHeader != null && authHeader.StartsWith("APIKEY "))
                    {
                        // Get the token
                        var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        // validatetoken

                        // var dateTimeExpired = TokenHelpers.GetDateTimeExpired(token, Utils.GetConfig("Authentication:apikey:Key"));
                        // if (dateTimeExpired.HasValue && dateTimeExpired.Value.CompareTo(DateTime.Now) != -1)
                        // {
                        var objectId = TokenHelpers.GetKeyFromBasicToken(token);
                        if (objectId != null)
                        {
                            var claimsAPIKEY = new List<Claim>();
                            // claimsAPIKEY.Add(new Claim(ClaimTypes.Sid, objectId.ToString()));
                            ClaimsIdentity claimsIdentityAPIKEY = new ClaimsIdentity(claimsAPIKEY, "Bear");
                            ClaimsPrincipal claimsPrincipalAPIKEY = new ClaimsPrincipal(claimsIdentityAPIKEY);
                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalAPIKEY, new AuthenticationProperties(), "APIKEY"));
                        }

                        // }

                        return result;
                    }
                }
                #endregion
                #region Basic
                if (Utils.GetConfig("Authentication:Basic:Enable") == "true")
                {
                    if (authHeader != null && authHeader.StartsWith("Basic "))
                    {
                        // Get the encoded username and password
                        var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                        // Decode from Base64 to string
                        var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                        // Split username and password
                        var username = decodedUsernamePassword.Split(':', 2)[0];
                        var password = decodedUsernamePassword.Split(':', 2)[1];
                        // Check if login is correct
                        if (username == Utils.GetConfig("Authentication:AdminUser") && password == Utils.GetConfig("Authentication:AdminPassWord"))
                        {

                            var claimsBasic = new List<Claim>();
                            claimsBasic.Add(new Claim(ClaimTypes.NameIdentifier, UserConstants.AdministratorId.ToString()));
                            ClaimsIdentity claimsIdentityBasic = new ClaimsIdentity(claimsBasic, "Basic");
                            ClaimsPrincipal claimsPrincipalBasic = new ClaimsPrincipal(claimsIdentityBasic);

                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalBasic,
                                                    new AuthenticationProperties(), "Basic"));
                            return result;
                        }
                    }
                }
                #endregion
                #region NoAuth
                if (Utils.GetConfig("Authentication:NoAuth:Enable") == "true")
                {
                    var claims = new List<Claim>();
                    // claims.Add(new Claim(ClaimTypes.NameIdentifier, UserConstants.AdministratorId.ToString()));
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "NONE_AUTH");
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal,
                                        new AuthenticationProperties(), "NONE_AUTH"));
                    return result;
                }
                #endregion
                #region JWT
                if (Utils.GetConfig("Authentication:Jwt:Enable") == "true")
                {
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        // Get the token
                        var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
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
                        if (validatedToken != null
                            && validatedToken.Issuer == Utils.GetConfig("Authentication:Jwt:Issuer")
                            && validatedToken.ValidFrom.CompareTo(DateTime.UtcNow) < 0
                            && validatedToken.ValidTo.CompareTo(DateTime.UtcNow) > 0)
                        {
                            var claimsJwt = new List<Claim>();
                            var userId = tokenInfo.Claims.Where(x => x.Type == "nameid").FirstOrDefault().Value;

                            //Check phân quyền API
                            //bool isAcceptAPI = UserHandler.IsApiPermission(userId, Request.Path.ToString());
                            //if (!isAcceptAPI)
                            //return AuthenticateResult.Fail("You not accepted API: " + Request.Path.ToString());

                            claimsJwt.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
                            claimsJwt.AddRange(tokenInfo.Claims);
                            // claims.Add(new Claim(ClaimTypes.NameIdentifier, UserConstants.AdministratorId.ToString()));
                            ClaimsIdentity claimsIdentityJwt = new ClaimsIdentity(claimsJwt, "Jwt");
                            ClaimsPrincipal claimsPrincipalJwt = new ClaimsPrincipal(claimsIdentityJwt);

                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalJwt,
                                                new AuthenticationProperties(), "Jwt"));
                            return result;
                        }
                        else
                        {
                            Log.Information(authHeader);
                            if (validatedToken == null)
                            {
                                Log.Information("validatedToken = null");
                            }
                            Log.Information("Issuer = " + validatedToken.Issuer);
                            Log.Information("ValidFrom = " + validatedToken.ValidFrom);
                            Log.Information("ValidTo = " + validatedToken.ValidTo);
                        }
                    }
                }
                #endregion
                #region AD JWT
                if (Utils.GetConfig("Authentication:AD:Enable") == "true")
                {
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        // Get the token
                        var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
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
                            ValidIssuer = Utils.GetConfig("Authentication:AD:Issuer"),
                            ValidAudience = Utils.GetConfig("Authentication:AD:Issuer"),
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Utils.GetConfig(("Authentication:AD:Key"))))
                        }, out validatedToken);
                        if (validatedToken != null
                            && validatedToken.Issuer == Utils.GetConfig("Authentication:AD:Issuer")
                            && validatedToken.ValidFrom.CompareTo(DateTime.UtcNow) < 0
                            && validatedToken.ValidTo.CompareTo(DateTime.UtcNow) > 0)
                        {
                            var claimsJwt = new List<Claim>();
                            var userId = tokenInfo.Claims.Where(x => x.Type == "nameid").FirstOrDefault().Value;

                            //Check phân quyền API
                            //bool isAcceptAPI = UserHandler.IsApiPermission(userId, Request.Path.ToString());
                            //if (!isAcceptAPI)
                            //return AuthenticateResult.Fail("You not accepted API: " + Request.Path.ToString());

                            claimsJwt.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
                            claimsJwt.AddRange(tokenInfo.Claims);
                            // claims.Add(new Claim(ClaimTypes.NameIdentifier, UserConstants.AdministratorId.ToString()));
                            ClaimsIdentity claimsIdentityJwt = new ClaimsIdentity(claimsJwt, "Jwt");
                            ClaimsPrincipal claimsPrincipalJwt = new ClaimsPrincipal(claimsIdentityJwt);

                            result = AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipalJwt,
                                                new AuthenticationProperties(), "Jwt"));
                            return result;
                        }
                        else
                        {
                            Log.Information(authHeader);
                            if (validatedToken == null)
                            {
                                Log.Information("validatedToken = null");
                            }
                            Log.Information("Issuer = " + validatedToken.Issuer);
                            Log.Information("ValidFrom = " + validatedToken.ValidFrom);
                            Log.Information("ValidTo = " + validatedToken.ValidTo);
                        }
                    }
                }
                #endregion

                //Log.Error(authHeader);
                return AuthenticateResult.Fail("Không xác thực");
            }
            catch (Exception ex)
            {
                //Log.Error(ex, MessageConstants.ErrorLogMessage);
                return AuthenticateResult.Fail("Không xác thực");
            }
        }       
    }
}