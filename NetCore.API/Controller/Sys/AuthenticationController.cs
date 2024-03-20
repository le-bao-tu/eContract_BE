using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NetCore.Business;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// JWT cho hệ thống
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/authentication")]
    [ApiExplorerSettings(GroupName = "00. Authentication")]
    public class AuthenticationController : ApiControllerBase
    {
        private readonly IUserHandler _userHandler;
        private readonly IConfiguration _config;
        private readonly IActiveDirectoryHandler _adHandler;
        private readonly ICacheService _cacheService;
        private readonly DataContext _dataContext;

        private static readonly HttpClient Client = new HttpClient();

        public AuthenticationController(IConfiguration config, IUserHandler userHandler, IActiveDirectoryHandler adHandler, DataContext dataContext, ICacheService cacheService, ISystemLogHandler logHandler) : base(logHandler)
        {
            _dataContext = dataContext;
            _config = config;
            _userHandler = userHandler;
            _adHandler = adHandler;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Đăng nhập trả về token JWT
        /// </summary>
        /// <param name="login">Model đăng nhập</param>
        /// <returns></returns>
        [Route("jwt/login")]
        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> SignInJwt([FromBody] LoginModel login)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_LOGIN);
                u.SystemLog.ActionName = LogConstants.ACTION_LOGIN;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                IActionResult response = Unauthorized();
                double timeToLive = Convert.ToDouble(_config["Authentication:Jwt:TimeToLive"]);
                if (login.Username == _config["Authentication:AdminUser"] && login.Password == _config["Authentication:AdminPassWord"])
                {
                    u.SystemLog.ListAction.Add(new ActionDetail("Đăng nhập thành công bằng tài khoản Admin (fix)"));
                    //Lấy về danh sách quyền của người dùng
                    var tokenString = AuthenticationHelper.BuildToken(new UserLoginModel()
                    {
                        UserName = _config["Authentication:AdminUser"],
                        Email = "admin@admin.com",
                        Id = UserConstants.AdministratorId,
                        ApplicationId = AppConstants.RootAppId,
                        ListRole = new List<string>(),
                        ListRight = new List<string>()
                    }, login.RememberMe, timeToLive);
                    response = Helper.TransformData(new ResponseObject<LoginResponse>(new LoginResponse()
                    {
                        TokenString = tokenString,
                        UserId = UserConstants.AdministratorId,
                        ApplicationId = AppConstants.RootAppId,
                        TimeExpride = DateTime.UtcNow.AddSeconds(timeToLive),
                        ListRight = new List<string>(),
                        ListRole = new List<string>(),
                        UserModel = new BaseUserModel()
                        {
                            Id = UserConstants.AdministratorId,
                            UserName = _config["Authentication:AdminUser"],
                            Email = "admin@admin.com",
                            Name = "Administrator",
                            OrganizationId = null
                        }
                    }));
                    return response;
                }
                if (login.Username == _config["Authentication:GuestUser"] && login.Password == _config["Authentication:GuestPassWord"])
                {
                    u.SystemLog.ListAction.Add(new ActionDetail("Đăng nhập thành công bằng tài khoản GuestUser (fix)"));
                    var tokenString = AuthenticationHelper.BuildToken(new UserLoginModel()
                    {
                        UserName = _config["Authentication:GuestUser"],
                        Email = "guest@admin.com",
                        Id = UserConstants.UserId,
                        ApplicationId = AppConstants.RootAppId
                    }, login.RememberMe, timeToLive);
                    response = Helper.TransformData(new ResponseObject<LoginResponse>(new LoginResponse()
                    {
                        TokenString = tokenString,
                        UserId = UserConstants.UserId,
                        ApplicationId = AppConstants.RootAppId,
                        TimeExpride = DateTime.UtcNow.AddSeconds(timeToLive),
                        UserModel = new BaseUserModel()
                        {
                            Id = UserConstants.UserId,
                            UserName = _config["Authentication:GuestUser"],
                            Email = "guest@admin.com"
                        }
                    }));
                    return response;
                }
                var user = await _userHandler.Authentication(login.Username, login.Password);
                if (user.Code == Code.Success && user is ResponseObject<UserModel> userData)
                {
                    u.SystemLog.OrganizationId = userData.Data.OrganizationId?.ToString();
                    u.SystemLog.ListAction.Add(new ActionDetail(CacheConstants.USER, userData.Data.Id.ToString(), "Đăng nhập thành công"));
                    //Lấy về danh sách quyền của người dùng
                    var tokenString = AuthenticationHelper.BuildToken(new UserLoginModel()
                    {
                        UserName = userData.Data.Name,
                        Email = userData.Data.Email,
                        Id = userData.Data.Id,
                        OrganizationId = userData.Data.OrganizationId,
                        ApplicationId = AppConstants.RootAppId,
                        ListRole = new List<string>(),
                        ListRight = new List<string>()
                    }, login.RememberMe, timeToLive);
                    response = Helper.TransformData(new ResponseObject<LoginResponse>(new LoginResponse()
                    {
                        TokenString = tokenString,
                        UserId = userData.Data.Id,
                        ApplicationId = AppConstants.RootAppId,
                        TimeExpride = DateTime.UtcNow.AddSeconds(timeToLive),
                        ListRight = new List<string>(),
                        ListRole = new List<string>(),
                        UserModel = new BaseUserModel()
                        {
                            Id = userData.Data.Id,
                            UserName = userData.Data.UserName,
                            Email = userData.Data.Email,
                            Name = userData.Data.Name,
                            OrganizationId = userData.Data.OrganizationId,
                            IdentityNumber = userData.Data.IdentityNumber,
                            PhoneNumber = userData.Data.PhoneNumber
                        }
                    }));
                    return response;
                }

                return response;
            });
        }

        /// <summary>
        /// Đăng nhập trả về token JWT sử dụng thông tin xác thực AD
        /// </summary>
        /// <param name="login">Model đăng nhập</param>
        /// <returns></returns>
        [Route("ad/login")]
        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> SignInADJwt([FromBody] LoginModel login)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_LOGIN);
                u.SystemLog.ActionName = LogConstants.ACTION_LOGIN;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var authenCheck = _adHandler.ValidateCredentials(login.Username, login.Password);

                if (login.Username == _config["Authentication:AD:AdminUser"] && login.Password == _config["Authentication:AD:AdminPassWord"])
                {
                    authenCheck = new ResponseObject<bool>(true, "Credentials valid.", Code.Success);
                }

                IActionResult response = Unauthorized();
                double timeToLive = Convert.ToDouble(_config["Authentication:AD:TimeToLive"]);
                if (authenCheck.Code == Code.Success && authenCheck is ResponseObject<bool> authenCheckData)
                {
                    if (authenCheckData.Data == true)
                    {
                        UserModel userModel = new UserModel();
                        //var cacheKey = $"{CacheConstants.USER}-{CacheConstants.LIST_SELECT}";

                        //var listUser = await _cacheService.GetOrCreate(cacheKey, () =>
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
                        //                          IdentityNumber = item.IdentityNumber,
                        //                          CreatedDate = item.CreatedDate,
                        //                          EFormConfig = item.EFormConfig,
                        //                          HasUserPIN = !string.IsNullOrEmpty(item.UserPIN)
                        //                      }).ToList();
                        //    return userResult;
                        //});
                        var listUser = await _userHandler.GetListUserFromCache();

                        //TODO: Hiện tại sub chỉ trả về userName, chưa có UserStore => cần phải xử lý thêm case này
                        var user = listUser.Where(x => x.Code.ToLower() == login.Username.ToLower()).FirstOrDefault();

                        //Nếu không tìm thấy người dùng có tên chính xác thì tìm trong userStore xem có người dùng phù hợp hay không
                        if (user == null)
                        {
                            user = listUser.Where(x => x.Code.ToLower().EndsWith("/" + login.Username.ToLower())).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
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
                            };

                            u.SystemLog.OrganizationId = user.OrganizationId?.ToString();
                            u.SystemLog.UserId = user.Id.ToString();
                            u.SystemLog.ListAction.Add(new ActionDetail(CacheConstants.USER, user.Id.ToString(), "Đăng nhập thành công"));

                            //Lấy về danh sách quyền của người dùng
                            var tokenString = AuthenticationHelper.BuildToken(new UserLoginModel()
                            {
                                UserName = user.Name,
                                Email = user.Email,
                                Id = user.Id,
                                OrganizationId = user.OrganizationId,
                                ApplicationId = AppConstants.RootAppId,
                                ListRole = new List<string>(),
                                ListRight = new List<string>()
                            }, login.RememberMe, timeToLive);

                            response = Helper.TransformData(new ResponseObject<LoginResponse>(new LoginResponse()
                            {
                                TokenString = tokenString,
                                UserId = user.Id,
                                ApplicationId = AppConstants.RootAppId,
                                TimeExpride = DateTime.UtcNow.AddSeconds(timeToLive),
                                ListRight = new List<string>(),
                                ListRole = new List<string>(),
                                UserModel = new BaseUserModel()
                                {
                                    Id = user.Id,
                                    UserName = user.Code,
                                    Email = user.Email,
                                    Name = user.Name,
                                    OrganizationId = user.OrganizationId,
                                    IdentityNumber = user.IdentityNumber,
                                    PhoneNumber = user.PhoneNumber
                                }
                            }));
                            return response;
                        }
                        else
                        {
                            response = Unauthorized("Tài khoản chưa được cấu hình truy cập hệ thống");
                        }
                    }
                }

                return response;
            });
        }
    }
}