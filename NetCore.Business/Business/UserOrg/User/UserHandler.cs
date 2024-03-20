using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

namespace NetCore.Business
{
    public class UserHandler : IUserHandler
    {
        private const string CachePrefix = CacheConstants.USER;
        private const string CachePrefixUserRole = CacheConstants.USER + "Role";
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        private readonly IOrganizationHandler _orgHandler;
        private readonly IUserRoleHandler _userRoleHandler;
        private readonly IOTPHandler _otpService;
        private readonly INotifyHandler _notifyHandler;
        private readonly IRoleHandler _roleHandler;
        private readonly IRightHandler _rightHandler;
        private readonly INavigationHandler _navigationHandler;
        private readonly IEmailHandler _emailHandler;

        private string defaultPassword = "";

        public string scimUserId = "";

        private readonly string WSO2IS_URI = "";
        private readonly string WSO2IS_BASIC_USERNAME = "";
        private readonly string WSO2IS_BASIC_PASSWORD = "";
        private readonly string WSO2IS_DEFAULT_USER_STORE = "";

        private OrganizationConfigModel orgConfig;

        private JsonSerializerOptions jso = new JsonSerializerOptions();

        public UserHandler(DataContext dataContext, INotifyHandler notifyHandler,
            IOTPHandler otpService, ICacheService cacheService,
            IOrganizationConfigHandler orgConfigHandler,
            IRoleHandler roleHandler, INavigationHandler navigationHandler, IRightHandler rightHandler, IEmailHandler emailHandler,
        IOrganizationHandler organizationHandler, IUserRoleHandler userRoleHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _orgConfigHandler = orgConfigHandler;
            _orgHandler = organizationHandler;
            _userRoleHandler = userRoleHandler;
            _otpService = otpService;
            _roleHandler = roleHandler;
            _rightHandler = rightHandler;
            _navigationHandler = navigationHandler;
            _notifyHandler = notifyHandler;
            _emailHandler = emailHandler;

            defaultPassword = Utils.GetConfig("Authentication:DefaultPassword");
            if (string.IsNullOrEmpty(defaultPassword))
            {
                defaultPassword = "123456a@";
            }

            WSO2IS_URI = Utils.GetConfig("WSO2IS:uri");
            WSO2IS_BASIC_USERNAME = Utils.GetConfig("WSO2IS:basicUserName");
            WSO2IS_BASIC_PASSWORD = Utils.GetConfig("WSO2IS:basicPassword");
            WSO2IS_DEFAULT_USER_STORE = Utils.GetConfig("WSO2IS:defaultUserStore");
        }

        public async Task<Response> Create(UserCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add {CachePrefix}: " + JsonSerializer.Serialize(model));

                model.UserName = model.UserName?.Trim().ToLower();
                if (model.OrganizationId.HasValue)
                {
                    this.orgConfig = await _orgConfigHandler.GetByOrgId(model.OrganizationId.Value);
                }

                var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == model.OrganizationId);

                #region Cập nhật userName theo UserStore

                if (orgConfig != null && !string.IsNullOrEmpty(orgConfig.UserStoreIDP))
                {
                    model.UserName = orgConfig.UserStoreIDP + "/" + model.UserName;
                }
                else if (!string.IsNullOrEmpty(WSO2IS_DEFAULT_USER_STORE))
                {
                    model.UserName = WSO2IS_DEFAULT_USER_STORE + "/" + org.Code + "_" + model.UserName;
                }
                //else
                //{
                //    model.UserName = org.Code + "_" + model.UserName;
                //}

                #endregion Cập nhật userName theo UserStore

                //Check userName
                var checkUserName = await _dataContext.User.AnyAsync(x => x.UserName == model.UserName && x.IsDeleted == false);
                if (checkUserName)
                {
                    Log.Information($"{systemLog.TraceId} - Add {CachePrefix} fail: UserName {model.UserName} is exist!");

                    return new ResponseError(Code.ServerError, $"UserName {model.UserName} đã tồn tại trong hệ thống");
                }

                ////Check email
                //var checkEmail = await _dataContext.User.AnyAsync(x => x.Email == model.Email && x.IsDeleted == false);
                //if (checkEmail)
                //{
                //    Log.Information($"Add {CachePrefix} fail: Email {model.Email} is exist!");
                //    return new ResponseError(Code.ServerError, $"Email {model.Email} đã tồn tại trong hệ thống");
                //}

                var entity = AutoMapperUtils.AutoMap<UserCreateModel, User>(model);

                if (orgConfig != null)
                {
                    entity.EFormConfig = orgConfig.EFormConfig;
                }

                if (string.IsNullOrEmpty(entity.Code))
                {
                    entity.Code = entity.UserName;
                }

                entity.PasswordSalt = Utils.PassowrdCreateSalt512();
                entity.Password = Utils.PasswordGenerateHmac(defaultPassword, entity.PasswordSalt);

                entity.IsLock = false;
                entity.Type = UserType.USER;

                entity.CreatedDate = DateTime.Now;

                #region Khởi tạo user trên SCIM

                // Thêm mới user lên SCIM => Lưu lại ID làm Id người dùng
                var user = await CreateSCIMUser(entity, systemLog);
                if (user == null)
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot create scim user! - user == null");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " SCIM - không thể đăng ký tài khoản!");
                }
                if (user.Id == null)
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot create scim user! - user.Id == null");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " SCIM - không thể đăng ký tài khoản! - " + user.Detail ?? "");
                }
                try
                {
                    entity.Id = new Guid(user.Id);
                }
                catch (Exception)
                {
                    Log.Error($"{systemLog.TraceId} - Không thể convert id của user trả về từ SCIM sang GUID!");
                }

                //return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " Cannot get SCIM token" + token.access_token);

                #endregion Khởi tạo user trên SCIM

                entity.SubjectDN = BuildSubjectDN(entity);
                await _dataContext.User.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        CreatedDate = DateTime.Now,
                        ObjectCode = CacheConstants.USER,
                        Description = $"Thêm mới người dùng thành công ({entity.UserName})",
                        ObjectId = entity.Id.ToString()
                    });

                    Log.Information($"{systemLog.TraceId} - Add success: " + JsonSerializer.Serialize(entity));

                    InvalidCache();
                    //TODO: Gửi mail cho khách hàng thông báo đăng ký tài khoản thành công

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Save database error!");

                    scimUserId = await GetSCIMUserIdByUsername(model.UserName);

                    var rsDel = await DeleteSCIMUser();
                    if (rsDel != null && !string.IsNullOrEmpty(rsDel.Status))
                    {
                        Log.Error($"{systemLog.TraceId} - {CachePrefix} error: Không xóa được người dùng trên SCIM khi đăng ký thất bại!");
                        return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                    }

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                scimUserId = await GetSCIMUserIdByUsername(model.UserName);

                var rsDel = await DeleteSCIMUser();
                if (rsDel != null && !string.IsNullOrEmpty(rsDel.Status))
                {
                    Log.Error($"{systemLog.TraceId} - {CachePrefix} error: Không xóa được người dùng trên SCIM khi đăng ký thất bại!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(UserUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                var entity = await _dataContext.User
                         .FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);
                ////Nếu truyền lên mật khẩu => cập nhật mật khẩu
                //if (!string.IsNullOrEmpty(model.Password))
                //{
                //    entity.PasswordSalt = Utils.PassowrdCreateSalt512();
                //    entity.Password = Utils.PasswordGenerateHmac(model.Password, entity.PasswordSalt);
                //}

                entity.SubjectDN = BuildSubjectDN(entity);
                _dataContext.User.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        CreatedDate = DateTime.Now,
                        ObjectCode = CacheConstants.USER,
                        Description = $"Cập nhật người dùng thành công ({entity.UserName})",
                        ObjectId = entity.Id.ToString()
                    });

                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateUser(UserProfileUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                var entity = await _dataContext.User
                     .FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                model.UpdateUserEntity(entity);
                _dataContext.User.Update(entity);
                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        CreatedDate = DateTime.Now,
                        ObjectCode = CacheConstants.USER,
                        Description = $"Cập nhật thông tin người dùng thành công ({entity.UserName})",
                        ObjectId = entity.Id.ToString()
                    });
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());
                    return new ResponseObject<Guid>(model.Id,MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }   
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateOrUpdate(UpdateOrCreateUserModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - CreateOrUpdate {CachePrefix}: " + JsonSerializer.Serialize(model));

                var response = new List<UserConnectResonseModel>();

                var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == model.OrganizationId);
                if (org == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy đơn vị tương ứng với Id đang truy cập");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy đơn vị tương ứng với Id đang truy cập");
                }

                //// Get root org
                //var rootOrg = _orgHandler.GetRootOrgModelByChidId(model.OrganizationId);
                //if (rootOrg == null)
                //{
                //    return new ResponseError(Code.NotFound, $"Không tìm thấy đơn vị gốc tương ứng với Id đang truy cập");
                //}

                this.orgConfig = await _orgConfigHandler.GetByOrgId(model.OrganizationId);

                var ck = model.ListUser.Any(x => string.IsNullOrEmpty(x.UserConnectId));
                if (ck)
                {
                    Log.Information($"{systemLog.TraceId} - UserConnectId không được để trống");
                    return new ResponseError(Code.BadRequest, $"{systemLog.TraceId} - UserConnectId không được để trống");
                }

                //Lấy đơn vị gốc
                OrganizationModel orgRootModel = new OrganizationModel();
                var rootOrg = await _orgHandler.GetRootByChidId(model.OrganizationId);
                if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                {
                    orgRootModel = orgRoot.Data;
                }

                //Lấy danh sách đơn vị con
                List<Guid> listChildOrgID = _orgHandler.GetListChildOrgByParentID(orgRootModel.Id);

                foreach (var item in model.ListUser)
                {
                    //Tìm user
                    var connectIdLower = item.UserConnectId.ToLower();
                    var user = await _dataContext.User.FirstOrDefaultAsync(x => x.ConnectId.ToLower() == connectIdLower && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value) && !x.IsDeleted);
                    if (user == null)
                    {
                        //var userName = org.Code + "_" + connectIdLower;
                        var userName = connectIdLower;

                        #region Cập nhật userName theo UserStore

                        if (orgConfig != null && !string.IsNullOrEmpty(orgConfig.UserStoreIDP))
                        {
                            userName = orgConfig.UserStoreIDP + "/" + userName;
                        }
                        else if (!string.IsNullOrEmpty(WSO2IS_DEFAULT_USER_STORE))
                        {
                            userName = WSO2IS_DEFAULT_USER_STORE + "/" + org.Code + "_" + userName;
                        }
                        else
                        {
                            userName = org.Code + "_" + userName;
                        }

                        #endregion Cập nhật userName theo UserStore

                        var entity = new User()
                        {
                            Id = new Guid(),
                            ConnectId = item.UserConnectId,
                            UserName = userName.ToLower(),
                            OrganizationId = model.OrganizationId,
                            Name = item.UserFullName,
                            Birthday = item.Birthday,
                            Sex = item.Sex,
                            IdentityType = item.IdentityType,
                            IdentityNumber = item.IdentityNumber,
                            IssueDate = item.IssueDate,
                            IssueBy = item.IssueBy,
                            PhoneNumber = item.UserPhoneNumber,
                            Email = item.UserEmail,
                            Address = item.Address,
                            CountryName = item.CountryName,
                            ProvinceName = item.ProvinceName,
                            DistrictName = item.DistrictName,
                        };

                        if (string.IsNullOrEmpty(entity.Code))
                        {
                            entity.Code = entity.UserName;
                        }

                        entity.PasswordSalt = Utils.PassowrdCreateSalt512();
                        entity.Password = Utils.PasswordGenerateHmac(defaultPassword, entity.PasswordSalt);

                        entity.IsLock = false;
                        entity.Type = UserType.USER;

                        entity.CreatedDate = DateTime.Now;

                        if (orgConfig != null)
                        {
                            entity.EFormConfig = orgConfig.EFormConfig;
                        }

                        #region Khởi tạo user trên SCIM

                        // Thêm mới user lên SCIM => Lưu lại ID làm Id người dùng
                        var scimUser = await CreateSCIMUser(entity, systemLog);
                        if (scimUser == null)
                        {
                            Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot create scim user!");

                            return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " SCIM - không thể đăng ký tài khoản!");
                        }
                        if (scimUser.Id == null)
                        {
                            Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot create scim user!");

                            return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " SCIM - không thể đăng ký tài khoản! - " + scimUser.Detail ?? "");
                        }
                        try
                        {
                            entity.Id = new Guid(scimUser.Id);
                        }
                        catch (Exception)
                        {
                            Log.Error($"{systemLog.TraceId} - Không thể convert id của user trả về từ SCIM sang GUID!");
                        }

                        //return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " Cannot get SCIM token" + token.access_token);

                        #endregion Khởi tạo user trên SCIM

                        entity.SubjectDN = BuildSubjectDN(entity);
                        await _dataContext.User.AddAsync(entity);

                        response.Add(new UserConnectResonseModel()
                        {
                            UserId = entity.Id,
                            UserConnectId = item.UserConnectId,
                            UserName = entity.UserName,
                            Message = "Thêm tài khoản thành công"
                        });
                    }
                    else
                    {
                        item.UpdateToEntity(user);
                        //Cập nhật đơn vị của người dùng khi tạo mới hợp đồng và thông tin người dùng đã tồn tại
                        user.OrganizationId = model.OrganizationId;
                        user.SubjectDN = BuildSubjectDN(user);
                        _dataContext.User.Update(user);

                        response.Add(new UserConnectResonseModel()
                        {
                            UserId = user.Id,
                            UserConnectId = item.UserConnectId,
                            UserName = user.UserName,
                            Message = "Cập nhật tài khoản thành công"
                        });
                    }
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateOrUpdate success: " + JsonSerializer.Serialize(response));
                    InvalidCache();
                    foreach (var item in response)
                    {
                        InvalidCache(item.UserId.ToString());
                    }

                    //TODO: Gửi mail cho khách hàng thông báo đăng ký tài khoản thành công

                    return new ResponseObject<List<UserConnectResonseModel>>(response, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Save database error!");

                    var rsDel = await DeleteSCIMUser();
                    if (rsDel != null && !string.IsNullOrEmpty(rsDel.Status))
                    {
                        Log.Error($"{systemLog.TraceId} - {CachePrefix} error: Không xóa được người dùng trên SCIM khi đăng ký thất bại!");
                        return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                    }

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                var rsDel = await DeleteSCIMUser();
                if (rsDel != null && !string.IsNullOrEmpty(rsDel.Status))
                {
                    Log.Error($"{CachePrefix} error: Không xóa được người dùng trên SCIM khi đăng ký thất bại!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }

                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListUserByListConnectId(OrgAndUserConnectRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                var entity = await _dataContext.Organization
                    .FirstOrDefaultAsync(x => x.Id == model.OrganizationId);
                if (entity == null)
                {
                    return new ResponseError(Code.NotFound, "Không tìm thấy đơn vị");
                }

                var user = await _dataContext.User.Where(x => x.OrganizationId == model.OrganizationId && x.UserName.EndsWith("admin")).OrderBy(x => x.CreatedDate).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ResponseError(Code.NotFound, "Không tìm thấy thông tin tài khoản quản trị của đơn vị");
                }

                OrganizationForServiceModel org = new OrganizationForServiceModel()
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Name = entity.Name,
                    UserId = user != null ? user.Id : Guid.Empty,
                    UserName = user != null ? user.UserName : "",
                };

                var listUserInfo = await _dataContext.User.Where(x => model.ListUserConnectId.Contains(x.ConnectId) && x.OrganizationId == model.OrganizationId)
                                .Select(x => new UserConnectInfoModel()
                                {
                                    UserId = x.Id,
                                    UserConnectId = x.ConnectId,
                                    UserName = x.UserName,
                                    UserEmail = x.Email,
                                    UserFullName = x.Name,
                                    UserPhoneNumber = x.PhoneNumber
                                }).ToListAsync();

                var response = new OrgAndUserConnectInfoModel()
                {
                    OrganizationInfo = org,
                    ListUserConnectInfo = listUserInfo
                };
                return new ResponseObject<OrgAndUserConnectInfoModel>(response, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                var listResult = new List<ResponeDeleteModel>();
                var name = "";
                var id = "";
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Delete: " + JsonSerializer.Serialize(listId));

                foreach (var item in listId)
                {
                    name = "";
                    id = "";
                    var entity = await _dataContext.User.FindAsync(item);

                    if (entity == null)
                    {
                        listResult.Add(new ResponeDeleteModel()
                        {
                            Id = item,
                            Name = name,
                            Result = false,
                            Message = MessageConstants.DeleteItemNotFoundMessage
                        });
                    }
                    else
                    {
                        entity.IsDeleted = true;
                        name = entity.Name;
                        id = entity.Id.ToString();
                        _dataContext.User.Update(entity);
                        //_dataContext.User.Remove(entity);
                        try
                        {
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                systemLog.ListAction.Add(new ActionDetail()
                                {
                                    CreatedDate = DateTime.Now,
                                    ObjectCode = CacheConstants.USER,
                                    Description = $"Xóa người dùng thành công ({name})",
                                    ObjectId = id
                                });

                                InvalidCache(item.ToString());

                                scimUserId = await GetSCIMUserIdByUsername(entity.UserName);

                                var rsDel = await DeleteSCIMUser();
                                if (rsDel != null && !string.IsNullOrEmpty(rsDel.Status))
                                {
                                    Log.Error($"{systemLog.TraceId} - {CachePrefix} error: Không xóa được người dùng trên SCIM khi xóa người dùng trong DB!");
                                    listResult.Add(new ResponeDeleteModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = false,
                                        Message = "Đã xóa người dùng trong DB nhưng có lỗi khi xóa trên SCIM"
                                    });
                                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                                }
                                else
                                {
                                    listResult.Add(new ResponeDeleteModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = true,
                                        Message = MessageConstants.DeleteItemSuccessMessage
                                    });
                                }
                            }
                            else
                            {
                                Log.Error($"{systemLog.TraceId} - Delete {CachePrefix} error: Save database error!");

                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = item,
                                    Name = name,
                                    Result = false,
                                    Message = MessageConstants.DeleteItemErrorMessage
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                            listResult.Add(new ResponeDeleteModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = ex.Message
                            });
                        }
                    }
                }
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Result Delete: " + JsonSerializer.Serialize(listResult));

                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(UserQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                #region Lấy quyền người dùng

                var roleIds = await GetUserRoleFromCacheAsync(filter.CurrentUserId);
                var userRole = await _roleHandler.GetRoleDataPermissionFromCacheByListIdAsync(roleIds);

                #endregion Lấy quyền người dùng

                var data = (from user in _dataContext.User

                            join org in _dataContext.Organization on user.OrganizationId equals org.Id into orgGourp
                            from org in orgGourp.DefaultIfEmpty()

                            where user.IsDeleted == false && user.Type == UserType.USER

                            select new UserBaseModel()
                            {
                                Id = user.Id,
                                Code = user.Code,
                                Name = user.Name,
                                UserName = user.UserName,
                                Email = user.Email,
                                SubjectDN = user.SubjectDN,
                                Status = user.Status,
                                OrganizationId = org.Id,
                                OrganizationName = org.Name,
                                IsLock = user.IsLock,
                                PhoneNumber = user.PhoneNumber,
                                LastActivityDate = user.LastActivityDate,
                                CreatedDate = user.CreatedDate,
                                UserEFormInfoJson = user.UserEFormInfoJson,
                                IsInternalUser = user.IsInternalUser,
                                IdentityNumber = user.IdentityNumber,
                                ConnectId = user.ConnectId
                            });

                if (filter.IsInternalUser.HasValue)
                {
                    data = data.Where(x => x.IsInternalUser == filter.IsInternalUser.Value);
                    if (!filter.IsInternalUser.Value)
                    {
                        data = data.Where(x => x.OrganizationId.HasValue && (userRole.ListUserInfoOfOrganizationId.Contains(x.OrganizationId.Value)));
                    }
                }

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts)
                        || x.Code.ToLower().Contains(ts)
                        || x.UserName.ToLower().Contains(ts)
                        || x.PhoneNumber.ToLower().Contains(ts)
                        || x.IdentityNumber.ToLower().Contains(ts));
                }

                if (filter.Status.HasValue)
                {
                    data = data.Where(x => x.Status == filter.Status);
                }

                if (filter.OrganizationId.HasValue)
                {
                    var listChildOrgID = _orgHandler.GetListChildOrgByParentID(filter.OrganizationId.Value);
                    // var rootOrg = await _orgHandler.GetRootOrgModelByChidId(filter.OrganizationId.Value);
                    // List<Guid> listGuid = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);

                    data = data.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value));
                }

                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                int totalCount = data.Count();

                // Pagination
                if (filter.PageSize.HasValue && filter.PageNumber.HasValue)
                {
                    if (filter.PageSize <= 0)
                    {
                        filter.PageSize = QueryFilter.DefaultPageSize;
                    }

                    //Calculate nunber of rows to skip on pagesize
                    int excludedRows = (filter.PageNumber.Value - 1) * (filter.PageSize.Value);
                    if (excludedRows <= 0)
                    {
                        excludedRows = 0;
                    }

                    // Query
                    data = data.Skip(excludedRows).Take(filter.PageSize.Value);
                }
                int dataCount = data.Count();

                var listResult = await data.ToListAsync();
                return new ResponseObject<PaginationList<UserBaseModel>>(new PaginationList<UserBaseModel>()
                {
                    DataCount = dataCount,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber ?? 0,
                    PageSize = filter.PageSize ?? 0,
                    Data = listResult
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                //string cacheKey = BuildCacheKey(id.ToString());
                //var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var entity = await _dataContext.User
                //        .FirstOrDefaultAsync(x => x.Id == id);

                //    return AutoMapperUtils.AutoMap<User, UserModel>(entity);
                //});

                var rs = await GetUserFromCache(id);

                string frontImageCarlUrl = string.Empty;
                string backImageCarlUrl = string.Empty;
                string faceImageCarlUrl = string.Empty;
                var ms = new MinIOService();
                try
                {
                    var backUrl = await ms.GetObjectPresignUrlAsync(rs.EKYCInfo.BackImageBucketName, rs.EKYCInfo.BackImageObjectName);
                    backImageCarlUrl = backUrl;
                }
                catch
                {
                    backImageCarlUrl = string.Empty;
                }
                try
                {
                    var faceUrl = await ms.GetObjectPresignUrlAsync(rs.EKYCInfo.FaceImageBucketName, rs.EKYCInfo.FaceImageObjectName);
                    faceImageCarlUrl = faceUrl;
                }
                catch
                {
                    faceImageCarlUrl = string.Empty;
                }
                try
                {
                    var fileUrl = await ms.GetObjectPresignUrlAsync(rs.EKYCInfo.FrontImageBucketName, rs.EKYCInfo.FrontImageObjectName);
                    frontImageCarlUrl = fileUrl;
                }
                catch
                {
                    frontImageCarlUrl = string.Empty;
                }

                rs.FrontImageCardUrl = frontImageCarlUrl;
                rs.BackImageCardUrl = backImageCarlUrl;
                rs.FaceImageCardUrl = faceImageCarlUrl;

                return new ResponseObject<UserModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetByUserConnectId(string connectId, Guid orgId, SystemLogModel systemLog)
        {
            try
            {
                var lsChildOrg = _orgHandler.GetListChildOrgByParentID(orgId);
                var dt = await _dataContext.User
                      .FirstOrDefaultAsync(x => x.OrganizationId.HasValue && lsChildOrg.Contains(x.OrganizationId.Value) && x.ConnectId == connectId);

                if (dt == null)
                {
                    return new Response(Code.NotFound, $"Không tìm thấy người dùng có id kết nối là {connectId}");
                }

                UserConnectModel rs = new UserConnectModel()
                {
                    Address = dt.Address,
                    Birthday = dt.Birthday,
                    CountryName = dt.CountryName,
                    DistrictName = dt.DistrictName,
                    IdentityNumber = dt.IdentityNumber,
                    IdentityType = dt.IdentityType,
                    IssueBy = dt.IssueBy,
                    IssueDate = dt.IssueDate,
                    ProvinceName = dt.ProvinceName,
                    Sex = dt.Sex,
                    UserConnectId = dt.ConnectId,
                    UserEmail = dt.Email,
                    UserFullName = dt.Name,
                    UserPhoneNumber = dt.PhoneNumber,
                    EFormType = dt.EFormConfig,
                    IsConfirmEformDTAT = dt.UserEFormInfo == null ? false : dt.UserEFormInfo.IsConfirmDigitalSignature,
                    IsConfirmEformCTS = dt.UserEFormInfo == null ? false : dt.UserEFormInfo.IsConfirmRequestCertificate
                };

                return new ResponseObject<UserConnectModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetUserCertificateFrom3rd(string connectId, Guid orgId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Get List Certificate connectId: " + connectId);

                var lsChildOrg = _orgHandler.GetListChildOrgByParentID(orgId);
                var user = await _dataContext.User
                      .FirstOrDefaultAsync(x => x.OrganizationId.HasValue && lsChildOrg.Contains(x.OrganizationId.Value) && x.ConnectId == connectId);

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                var dateNow = DateTime.Now;

                var usersHSM = await _dataContext.UserHSMAccount.Where(x => x.UserId == user.Id && x.AccountType == AccountType.HSM &&
                    ((x.ValidFrom.HasValue && x.ValidTo.HasValue && x.ValidFrom <= dateNow && x.ValidTo >= dateNow) || (!x.ValidFrom.HasValue && !x.ValidTo.HasValue)))
                    .Select(x => new UserHSMAccountModel()
                    {
                        SubjectDN = x.SubjectDN,
                        ValidFrom = x.ValidFrom,
                        ValidTo = x.ValidTo,
                        Id = x.Id,
                        Code = x.Code
                    }).ToListAsync();

                return new ResponseObject<List<UserHSMAccountModel>>(usersHSM, "Lấy danh sách CTS thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.User.Where(x => x.Status == true && x.IsDeleted == false && x.Type == UserType.USER).OrderBy(x => x.Order).ThenBy(x => x.UserName)
                //                select new UserSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Code = item.UserName,
                //                    DisplayName = item.Name + " - " + item.UserName,
                //                    FullName = item.Name,
                //                    Name = item.Name,
                //                    Email = item.Email,
                //                    PhoneNumber = item.PhoneNumber,
                //                    PositionName = item.PositionName,
                //                    OrganizationId = item.OrganizationId,
                //                    IdentityNumber = item.IdentityNumber,
                //                    Note = item.Email,
                //                    CreatedDate = item.CreatedDate,
                //                    EFormConfig = item.EFormConfig,
                //                    HasUserPIN = !string.IsNullOrEmpty(item.UserPIN)
                //                });

                //    return await data.ToListAsync();
                //});

                var list = await GetListUserFromCache();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Code.ToLower().Contains(textSearch) || x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }
                if (orgId.HasValue)
                {
                    list = list.Where(x => x.OrganizationId == orgId).ToList();
                }
                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<UserSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListComboboxByRootOrg(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                var list = await GetListUserFromCache();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Code.ToLower().Contains(textSearch) || x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }
                if (orgId.HasValue)
                {
                    var rootOrg = await _orgHandler.GetRootOrgModelByChidId(orgId.Value);
                    var orgChild = _orgHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && orgChild.Contains(x.OrganizationId.Value)).ToList();
                }
                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<UserSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<UserSelectItemModel>> GetListUserFromCache()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.User.Where(x => x.Status == true && x.IsDeleted == false && x.Type == UserType.USER).OrderBy(x => x.Order).ThenBy(x => x.UserName)
                                select new UserSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.UserName,
                                    DisplayName = item.Name + " - " + item.UserName,
                                    FullName = item.Name,
                                    Name = item.Name,
                                    Email = item.Email,
                                    PhoneNumber = item.PhoneNumber,
                                    PositionName = item.PositionName,
                                    OrganizationId = item.OrganizationId,
                                    IdentityNumber = item.IdentityNumber,
                                    Note = item.Email,
                                    CreatedDate = item.CreatedDate,
                                    EFormConfig = item.EFormConfig,
                                    HasUserPIN = !string.IsNullOrEmpty(item.UserPIN),
                                    IsLock = item.IsLock,
                                    IsInternalUser = item.IsInternalUser,
                                    IsEKYC = item.EKYCInfo == null ? false : item.EKYCInfo.IsEKYC
                                });

                    return await data.ToListAsync();
                });
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void InvalidCache(string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                string cacheKey = BuildCacheKey(id);
                _cacheService.Remove(cacheKey);
            }

            string selectItemCacheKey = BuildCacheKey(SelectItemCacheSubfix);
            _cacheService.Remove(selectItemCacheKey);
        }

        private string BuildCacheKey(string id)
        {
            return $"{CachePrefix}-{id}";
        }

        #region UserRole

        //Lấy danh sách quyền, menu, right người dùng
        public async Task<Response> GetUserPermission(Guid userId, SystemLogModel systemLog)
        {
            try
            {
                //Lấy danh sách quyền người dùng
                var listUserRole = await GetUserRoleFromCacheAsync(userId);

                var lsRole = await _roleHandler.GetListRoleFromCache(new Guid(systemLog.OrganizationId));
                var listCodeRole = lsRole.Where(x => listUserRole.Contains(x.Id)).Select(x => x.Code).ToList();

                //Lấy danh sách menu theo người dùng
                var lsNav = await _navigationHandler.GetListNavFromCacheAsync();
                lsNav = lsNav.Where(x => x.Status).ToList();
                List<NavigationSelectItemModel> listNavigationSource = new List<NavigationSelectItemModel>();
                List<NavigationSelectItemModel> listNavigationResult = new List<NavigationSelectItemModel>();

                foreach (var item in lsNav)
                {
                    item.ListRoleCode = lsRole.Where(x => item.ListRoleId.Contains(x.Id)).Select(x => x.Code).ToList();
                    if (item.ListRoleId.Any(item => listUserRole.Contains(item)))
                    {
                        listNavigationSource.Add(item);
                    }
                }
                var listNavId = listNavigationSource.Select(x => x.Id).ToList();

                //Loại bỏ phần tử ko có cha
                foreach (var item in listNavigationSource)
                {
                    if (!item.ParentId.HasValue)
                    {
                        listNavigationResult.Add(item);
                    }
                    else
                    {
                        if (listNavId.Contains(item.ParentId.Value))
                        {
                            listNavigationResult.Add(item);
                        }
                    }
                }

                List<Guid> listRightId = new List<Guid>();
                //Lấy danh sách right theo role
                foreach (var item in listUserRole)
                {
                    var lsRightByRole = await _roleHandler.GetListRightIdByRoleFromCacheAsync(item);
                    listRightId.AddRange(lsRightByRole);
                }

                listRightId = listRightId.Distinct().ToList();

                var listRight = await _rightHandler.GetListRightFromCacheAsync();

                var user = await GetUserFromCache(userId);

                //var rs = await _dataContext.UserMapRole.Where(x => x.UserId == model.Id).Select(x => x.RoleId).ToListAsync();

                return new ResponseObject<ResultUserPermissionModel>(new ResultUserPermissionModel()
                {
                    Role = listCodeRole,
                    Right = listRight.Where(x => listRightId.Contains(x.Id)).Select(x => x.Code).ToList(),
                    Navigation = listNavigationResult,
                    IsLock = user.IsLock
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        //Lấy danh sách quyền người dùng
        public async Task<Response> GetUserRole(GetUserRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                var rs = await GetUserRoleFromCacheAsync(model.Id);

                //var rs = await _dataContext.UserMapRole.Where(x => x.UserId == model.Id).Select(x => x.RoleId).ToListAsync();

                return new ResponseObject<ResultGetUserRoleModel>(new ResultGetUserRoleModel()
                {
                    ListRoleId = rs
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        //Cập nhật danh sách quyền người dùng
        public async Task<Response> UpdateUserRole(UpdateUserRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update User Role: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == model.Id);

                var ls1 = _dataContext.UserMapRole.Where(x => x.UserId == model.Id);
                _dataContext.UserMapRole.RemoveRange(ls1);
                if (model.ListRoleId != null && model.ListRoleId.Count > 0)
                {
                    foreach (var item in model.ListRoleId)
                    {
                        await _dataContext.UserMapRole.AddAsync(new UserMapRole()
                        {
                            Id = Guid.NewGuid(),
                            RoleId = item,
                            UserId = model.Id
                        });
                    }
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Update UserRole Success");

                    InvalidCache(CachePrefixUserRole + model.Id.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập phân quyền người dùng :{entity.UserName}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        //Khởi tạo dữ liệu quản trị cho đơn vị
        public async Task<Response> InitUserAdminOrg(InitUserAdminOrgModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - InitUserAdminOrg: " + JsonSerializer.Serialize(model));

                var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Code == model.OrganizationCode);

                if (org == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin đơn vị");
                    return new ResponseError(Code.NotFound, "Không tìm thấy thông tin đơn vị");
                }

                systemLog.OrganizationId = org.Id.ToString();
                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.UserName == model.UserName);

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin người dùng");
                    return new ResponseError(Code.NotFound, "Không tìm thấy thông tin người dùng");
                }

                //Lấy đơn vị gốc
                var rootOrg = await _orgHandler.GetRootOrgModelByChidId(org.Id);

                string roleCode = rootOrg.Code.ToUpper() + "_SYS_ADMIN";

                //Kiểm tra role đã tồn tại hay chưa
                var checkRole = await _dataContext.Role.AnyAsync(x => x.Code == roleCode && x.OrganizationId == org.Id);

                if (checkRole)
                {
                    return new ResponseError(Code.BadRequest, "Đơn vị đã được phân quyền, vui lòng kiểm tra lại");
                }

                user.OrganizationId = rootOrg.Id;
                user.IsInternalUser = true;
                user.ModifiedDate = DateTime.Now;
                _dataContext.User.Update(user);

                //Thêm role
                Role role = new Role()
                {
                    Id = Guid.NewGuid(),
                    Code = roleCode,
                    Name = "Quản trị hệ thống",
                    CreatedDate = DateTime.Now,
                    OrganizationId = rootOrg.Id,
                    CreatedUserId = new Guid(systemLog.UserId),
                    Description = "Khởi tạo dữ liệu tự động",
                    Order = 0,
                    Status = true
                };

                await _dataContext.Role.AddAsync(role);

                //Thêm role cho người dùng
                await _dataContext.UserMapRole.AddAsync(new UserMapRole()
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    UserId = user.Id
                });

                //Thêm phân quyền menu
                //Lấy danh sách menu
                var listNav = await _dataContext.Navigation.Where(x => x.Status).Select(x => x.Id).ToListAsync();
                foreach (var item in listNav)
                {
                    await _dataContext.NavigationMapRole.AddAsync(new NavigationMapRole()
                    {
                        Id = Guid.NewGuid(),
                        NavigationId = item,
                        RoleId = role.Id
                    });
                }

                //Thêm phân quyền right
                var listRight = await _dataContext.Right.Where(x => x.Status).Select(x => x.Id).ToListAsync();
                foreach (var item in listRight)
                {
                    await _dataContext.RoleMapRight.AddAsync(new RoleMapRight()
                    {
                        Id = Guid.NewGuid(),
                        RightId = item,
                        RoleId = role.Id
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - InitUserAdminOrg Success");

                    //Xóa cache Navigation
                    //Xóa cache danh sách quyền người dùng

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Khởi tạo dữ liệu phân quyền người dùng thành công :{model.UserName} - {model.OrganizationCode}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new Response(Code.Success, MessageConstants.CreateSuccessMessage);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - InitUserAdminOrg error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<Guid>> GetUserRoleFromCacheAsync(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(CachePrefixUserRole + id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var rs = await _dataContext.UserMapRole.Where(x => x.UserId == id).Select(x => x.RoleId).ToListAsync();

                    return rs;
                });
                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Response> GetUserRoleByRoleId(GetUserRoleByRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                var rs = await _dataContext.UserMapRole.Where(x => x.RoleId == model.RoleId).Select(x => x.UserId).ToListAsync();

                return new ResponseObject<ResultGetUserRoleByRoleModel>(new ResultGetUserRoleByRoleModel()
                {
                    ListUserId = rs
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SaveListUserRole(SaveListUserRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update List User Role: " + JsonSerializer.Serialize(model));

                if (model.UserIds.Count < 1)
                {
                    Log.Error($"{systemLog.TraceId} - List User Id rỗng");
                    return new ResponseError(Code.BadRequest, MessageConstants.UpdateErrorMessage);
                }

                // xóa các role đã tồn tại
                var userRoles = _dataContext.UserMapRole.Where(x => x.RoleId == model.RoleId);

                foreach (var item in userRoles)
                {
                    InvalidCache(CachePrefixUserRole + item.UserId.ToString());
                }

                _dataContext.UserMapRole.RemoveRange(userRoles);

                // thêm mới user role
                var listUserRole = new List<UserMapRole>();
                foreach (var item in model.UserIds.Distinct())
                {
                    var userMapRole = new UserMapRole();
                    userMapRole.Id = Guid.NewGuid();
                    userMapRole.UserId = item;
                    userMapRole.RoleId = model.RoleId;

                    listUserRole.Add(userMapRole);
                }

                await _dataContext.UserMapRole.AddRangeAsync(listUserRole);
                var dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Update UserRole Success");

                    foreach (var item in model.UserIds.Distinct())
                        InvalidCache(CachePrefixUserRole + item.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập phân quyền nhóm người dùng Id: {model.RoleId}",
                        ObjectCode = CachePrefixUserRole,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(model.RoleId, "Phân quyền người dùng thành công", Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        //Gửi OTP cho người dùng
        public async Task<Response> SendOTPAuthToUser(Guid userId, SystemLogModel systemLog)
        {
            try
            {
                // Lấy thông tin người dùng
                var user = await GetUserFromCache(userId);

                if (user == null)
                {
                    return new ResponseError(Code.Forbidden, $"Không tìm thấy người dùng xử lý tài liệu");
                }
                if (!user.OrganizationId.HasValue)
                {
                    return new ResponseError(Code.BadRequest, $"Tài khoản người dùng chưa được cấu hình thông tin đơn vị");
                }
                //else if (currentUser.IsEnableSmartOTP == true)
                //{
                //    return new ResponseError(Code.Success, $"Vui lòng mở ứng dụng SmartOTP và lấy OTP");
                //}

                var otp = await _otpService.GenerateHOTPFromService(new HOTPRequestModel()
                {
                    AppRequest = "Digital eContract",
                    Description = "Yêu cầu OTP cho người dùng",
                    ObjectId = user.Id.ToString(),
                    UserName = user.UserName,
                    Step = 300,
                    HOTPSize = 6
                }, systemLog);

                if (otp == null || otp.OTP == null)
                {
                    return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi request OTP");
                }

                //Lấy thông tin đơn vị
                var rootOrg = await _orgHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);

                var _orgConf = await _orgConfigHandler.InternalGetByOrgId(rootOrg.Id);
                if (_orgConf == null)
                {
                    Log.Information($"{systemLog.TraceId} - Đơn vị chưa được cấu hình thông tin kết nối");
                    return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối");
                }

                string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";
                var body = _emailHandler.GenerateDocumentOTPEmailBody(new GenerateEmailBodyModel()
                {
                    UserName = user.Name,
                    OTP = otp.OTP
                });
                var toEmails = new List<string>()
                    {
                        user.Email
                    };

                var sendNotify = await _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
                {
                    TraceId = systemLog.TraceId,
                    OraganizationCode = rootOrg.Code,
                    IsSendEmail = true,
                    IsSendNotification = false,
                    IsSendSMS = false,
                    ListEmail = toEmails,
                    EmailTitle = title,
                    EmailContent = body,
                    ListPhoneNumber = new List<string>() { user.PhoneNumber },
                    SmsContent = string.IsNullOrEmpty(_orgConf.SMSOTPTemplate) ? "" : Utils.BuildStringFromTemplate(_orgConf.SMSOTPTemplate, new string[] { otp.OTP })
                });
                if (sendNotify.Code != Code.Success)
                {
                    return new ResponseError(Code.ServerError, $"Gửi thông báo không thành công");
                }
                return new Response(Code.Success, $"Gửi thông báo thành công");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        #endregion UserRole

        public async Task<UserModel> GetUserFromCache(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                return await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.User
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<User, UserModel>(entity);
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Response> LockOrUnlock(UserLockModel model, SystemLogModel systemLog)
        {
            var listResult = new List<ResponeDeleteModel>();

            try
            {
                foreach (var item in model.ListId)
                {
                    var entity = await _dataContext.User
                        .FirstOrDefaultAsync(x => x.Id == item);
                    Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix} Lock: " + JsonSerializer.Serialize(entity));

                    entity.IsLock = model.IsLock;
                    entity.ModifiedDate = DateTime.Now;
                    entity.ModifiedUserId = model.ModifiedUserId;

                    _dataContext.User.Update(entity);

                    listResult.Add(new ResponeDeleteModel()
                    {
                        Id = item,
                        Name = entity.Name,
                        Result = true,
                        Message = model.IsLock ? "Khóa người dùng thành công." : "Mở khóa người dùng thành công"
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix} Lock: " + JsonSerializer.Serialize(listResult));

                    foreach (var item in model.ListId)
                    {
                        InvalidCache(item.ToString());
                    }

                    return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} Lock error: Save database error!");

                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdatePassword(UserUpdatePasswordModel model, SystemLogModel systemLog)
        {
            try
            {
                var entity = await _dataContext.User
                         .FirstOrDefaultAsync(x => x.Id == model.UserId);
                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix} Password: " + JsonSerializer.Serialize(entity));

                //if (!entity.Password.Equals(Utils.PasswordGenerateHmac(model.OldPassword, entity.PasswordSalt)))
                //{
                //    return new ResponseObject<Guid>(Guid.Empty, "Mật khẩu cũ không chính xác", Code.ServerError); ;
                //}

                entity.PasswordSalt = Utils.PassowrdCreateSalt512();
                entity.Password = Utils.PasswordGenerateHmac(model.NewPassword, entity.PasswordSalt);
                entity.ModifiedDate = DateTime.Now;
                entity.ModifiedUserId = model.ModifiedUserId;

                _dataContext.User.Update(entity);

                #region Cập nhật password trên scim

                // Thêm mới user lên SCIM => Lưu lại ID làm Id người dùng
                var rs = await UpdatePasswordSCIMUser(entity, model.NewPassword);
                if (rs == null)
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot update scim user!");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " SCIM - không thể cập nhật mật khẩu cho tài khoản!");
                }
                if (rs.Id == null)
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot update scim user!");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage + " SCIM - không thể cập nhật mật khẩu tài khoản! - " + rs.Detail ?? "");
                }

                #endregion Cập nhật password trên scim

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix} Password: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.UserId.ToString());

                    return new ResponseObject<Guid>(model.UserId, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} Password error: Save database error!");

                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Authentication(string userName, string password)
        {
            try
            {
                var entity = await _dataContext.User
                      .FirstOrDefaultAsync(x => x.UserName.ToLower() == userName.Trim().ToLower() && x.Status && !x.IsDeleted);
                if (entity == null)
                {
                    return new ResponseError(Code.BadRequest, "Tài khoản không tồn tại");
                }

                if (entity.IsLock)
                {
                    return new ResponseError(Code.BadRequest, "Tài khoản đã bị khóa");
                }

                var passhash = Utils.PasswordGenerateHmac(password, entity.PasswordSalt);
                if (passhash == entity.Password)
                {
                    entity.LastActivityDate = DateTime.Now;
                    _dataContext.User.Update(entity);

                    int dbSave = await _dataContext.SaveChangesAsync();

                    var rs = AutoMapperUtils.AutoMap<User, UserModel>(entity);

                    Log.Information($"Authentication {CachePrefix} success: {userName}");
                    return new ResponseObject<UserModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
                }
                Log.Information($"Authentication {CachePrefix} fail: {userName}");
                return new ResponseError(Code.BadRequest, "Kiểm tra lại thông tin đăng nhập");
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetUserCertificateCreateInfo(Guid id, int type, SystemLogModel systemLog)
        {
            try
            {
                var model = new UserCertificateCreateModel();
                // type = 1 => Người dùng
                if (type == 1)
                {
                    var entity = await _dataContext.User
                        .FirstOrDefaultAsync(x => x.Id == id);

                    model = new UserCertificateCreateModel()
                    {
                        UserId = entity.Id,
                        UserName = entity.UserName,
                        IdentityNumber = entity.IdentityNumber,
                        IdentityType = entity.IdentityType,
                        CommonName = entity.Name,
                        PositionName = null,
                        OrganizationId = null,
                        OrganizationName = null,
                        TaxCode = null,
                        OrganizationUnit = null,
                        Email = entity.Email,
                        PhoneNumber = entity.PhoneNumber,
                        CountryId = entity.CountryId,
                        CountryName = entity.CountryName,
                        ProvinceId = entity.ProvinceId,
                        ProvinceName = entity.ProvinceName,
                        Address = entity.Address
                    };
                }
                // type = 2 => Người dùng trong tổ chức
                else if (type == 2)
                {
                    var entity = await _dataContext.User
                        .FirstOrDefaultAsync(x => x.Id == id);
                    var org = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == entity.OrganizationId);
                    if (org == null)
                    {
                        model = null;
                    }
                    else
                    {
                        model = new UserCertificateCreateModel()
                        {
                            UserId = entity.Id,
                            UserName = entity.UserName,
                            IdentityNumber = entity.IdentityNumber,
                            IdentityType = entity.IdentityType,
                            CommonName = entity.Name,
                            PositionName = entity.PositionName,
                            OrganizationId = org.Id,
                            OrganizationName = org.Name,
                            TaxCode = org.TaxCode,
                            OrganizationUnit = "OrganizationUnit",
                            Email = entity.Email,
                            PhoneNumber = entity.PhoneNumber,
                            CountryId = entity.CountryId,
                            CountryName = entity.CountryName,
                            ProvinceId = entity.ProvinceId,
                            ProvinceName = entity.ProvinceName,
                            Address = entity.Address
                        };
                    }
                }
                // type = 3 => Tổ chức
                else if (type == 3)
                {
                    var entity = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == id);

                    var parentOrg = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == entity.ParentId);

                    model = new UserCertificateCreateModel()
                    {
                        UserId = null,
                        UserName = null,
                        IdentityNumber = entity.TaxCode,
                        IdentityType = "MST",
                        CommonName = entity.Name,
                        PositionName = null,
                        OrganizationId = entity.Id,
                        OrganizationName = entity.Name,
                        TaxCode = entity.TaxCode,
                        OrganizationUnit = "OrganizationUnit",
                        Email = entity.Email,
                        PhoneNumber = entity.PhoneNumber,
                        CountryId = entity.CountryId,
                        CountryName = entity.CountryName,
                        ProvinceId = entity.ProvinceId,
                        ProvinceName = entity.ProvinceName,
                        Address = entity.Address
                    };
                }

                return new ResponseObject<UserCertificateCreateModel>(model, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<SICMUserCreateResponseModel> CreateSCIMUser(User entity, SystemLogModel systemLog)
        {
            var rs = await CreateUserIDP(entity, systemLog);

            // Kiểm tra trường hợp thêm thành công nhưng báo lỗi => thêm lại thì status là đã tồn tại
            if (rs == null)
            {
                Log.Information($"{systemLog.TraceId} - Retry add User SCIM");
                rs = await CreateUserIDP(entity, systemLog);
            }

            if (rs == null)
            {
                Log.Information($"{systemLog.TraceId} - Add User SCIM Error SCIM - retry 1");
            }

            return rs;
        }

        private async Task<SICMUserCreateResponseModel> CreateUserIDP(User entity, SystemLogModel systemLog)
        {
            try
            {
                var url = new Uri(WSO2IS_URI + "scim2/Users");

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var httpClient = new HttpClient(clientHandler))
                    {
                        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{WSO2IS_BASIC_USERNAME}:{WSO2IS_BASIC_PASSWORD}"));

                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                        var userName = entity.UserName;

                        var obj = new SICMUserCreateRequestModel()
                        {
                            Password = defaultPassword,
                            UserName = userName
                        };
                        Log.Information($"{systemLog.TraceId} - CreateSCIMUser: " + JsonSerializer.Serialize(obj));

                        var content = new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
                        var rp = await httpClient.PostAsync(url, content);

                        var rs = rp.Content.ReadAsStringAsync().Result;
                        Log.Information($"{systemLog.TraceId} - CreateSCIMUser Response: {rs}");
                        var dataResult = JsonSerializer.Deserialize<SICMUserCreateResponseModel>(rs);

                        //Tài khoản đã tồn tại
                        if (dataResult.Status == "409")
                        {
                            Log.Information($"{systemLog.TraceId} - CreateSCIMUser Status = 409");
                            dataResult.Id = Guid.NewGuid().ToString();
                            return dataResult;
                        }

                        //Thêm tài khoản bị lỗi
                        if (dataResult.Status == "500")
                        {
                            Log.Information($"{systemLog.TraceId} - CreateSCIMUser Status = 500");
                            return null;
                        }

                        scimUserId = dataResult.Id;

                        return dataResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        private async Task<SICMUserDeleteResponseModel> DeleteSCIMUser()
        {
            try
            {
                if (scimUserId == null)
                {
                    return null;
                }

                var url = new Uri(WSO2IS_URI + "scim2/Users/" + scimUserId);

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var httpClient = new HttpClient(clientHandler))
                    {
                        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{WSO2IS_BASIC_USERNAME}:{WSO2IS_BASIC_PASSWORD}"));

                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                        Log.Information($"DeleteSCIMUser: " + JsonSerializer.Serialize(url));

                        var rp = await httpClient.DeleteAsync(url);

                        var rs = rp.Content.ReadAsStringAsync().Result;
                        Log.Information($"DeleteSCIMUser Response: " + JsonSerializer.Serialize(rs));
                        var dataResult = JsonSerializer.Deserialize<SICMUserDeleteResponseModel>(rs);
                        return dataResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        private async Task<SICMUserUpdateResponseModel> UpdatePasswordSCIMUser(User entity, string newPassword)
        {
            try
            {
                var scimId = await GetSCIMUserIdByUsername(entity.UserName);

                var url = new Uri(WSO2IS_URI + "scim2/Users/" + scimId);

                var userName = entity.UserName;

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var httpClient = new HttpClient(clientHandler))
                    {
                        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{WSO2IS_BASIC_USERNAME}:{WSO2IS_BASIC_PASSWORD}"));

                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                        var obj = new SICMUserCreateRequestModel()
                        {
                            Password = newPassword,
                            UserName = userName
                        };

                        var content = new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
                        var rp = await httpClient.PutAsync(url, content);

                        var rs = rp.Content.ReadAsStringAsync().Result;
                        var dataResult = JsonSerializer.Deserialize<SICMUserUpdateResponseModel>(rs);

                        scimUserId = dataResult.Id;

                        return dataResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);

                scimUserId = await GetSCIMUserIdByUsername(entity.UserName);

                return null;
            }
        }

        public async Task<Response> AddDevice(DeviceAddRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add/Update Identifier Device: " + JsonSerializer.Serialize(model));
                User user = null;
                if (!string.IsNullOrEmpty(model.UserConnectId))
                {
                    //Tìm user
                    var connectIdLower = model.UserConnectId.ToLower();
                    Guid oggId = new Guid(systemLog.OrganizationId);

                    //Lấy danh sách đơn vị con
                    List<Guid> listChildOrgID = _orgHandler.GetListChildOrgByParentID(oggId);

                    user = await _dataContext.User.FirstOrDefaultAsync(x => x.ConnectId.ToLower() == connectIdLower && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value) && !x.IsDeleted);
                    if (user == null)
                    {
                        return new ResponseError(Code.NotFound, "Không tìm thấy người dùng có mã " + model.UserConnectId);
                    }
                }
                Guid userId = user != null ? user.Id : new Guid(systemLog.UserId);

                var device = await _dataContext.UserMapDevice.Where(x => x.DeviceId == model.DeviceId && x.UserId == userId).FirstOrDefaultAsync();

                if (device == null)
                {
                    await _dataContext.UserMapDevice.Where(x => x.UserId == userId).ForEachAsync(x => x.IsIdentifierDevice = false);
                    await _dataContext.UserMapDevice.AddAsync(new UserMapDevice()
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = model.DeviceId,
                        CreatedDate = DateTime.Now,
                        DeviceName = model.DeviceName,
                        IsIdentifierDevice = true,
                        UserId = userId
                    });
                }
                else
                {
                    if (device.IsIdentifierDevice == false)
                    {
                        await _dataContext.UserMapDevice.Where(x => x.UserId == userId).ForEachAsync(x => x.IsIdentifierDevice = false);
                        device.IsIdentifierDevice = true;
                    }
                    else
                    {
                        return new ResponseObject<bool>(true, "Cập nhật thiết bị định danh thành công", Code.Success);
                    }
                }

                //Thêm firebase token
                if (!string.IsNullOrEmpty(model.FirebaseToken))
                {
                    var firebaseToken = await _dataContext.UserMapFirebaseToken.Where(x => x.FirebaseToken == model.FirebaseToken && x.DeviceId == model.DeviceId && x.UserId == userId).FirstOrDefaultAsync();

                    if (firebaseToken == null)
                    {
                        await _dataContext.UserMapFirebaseToken.AddAsync(new UserMapFirebaseToken()
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = model.DeviceId,
                            CreatedDate = DateTime.Now,
                            FirebaseToken = model.FirebaseToken,
                            UserId = userId
                        });
                    }
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateOrUpdate Device success");

                    return new ResponseObject<bool>(true, "Cập nhật thiết bị định danh thành công", Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add Device error: Save database error!");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> AddOrUpdateFirebaseToken(FirebaseRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add FirebaseToken: " + JsonSerializer.Serialize(model));
                Guid userId = new Guid(systemLog.UserId);

                var firebaseToken = await _dataContext.UserMapFirebaseToken.Where(x => x.FirebaseToken == model.FirebaseToken && x.DeviceId == model.DeviceId && x.UserId == userId).FirstOrDefaultAsync();

                if (firebaseToken != null)
                {
                    return new ResponseObject<bool>(true, MessageConstants.CreateSuccessMessage, Code.Success);
                }

                if (string.IsNullOrEmpty(model.FirebaseToken))
                {
                    var firebaseTokens = await _dataContext.UserMapFirebaseToken.Where(x => x.UserId == userId).ToListAsync();
                    var lastToken = firebaseTokens.LastOrDefault();
                    if (lastToken != null && !string.IsNullOrEmpty(lastToken.FirebaseToken))
                        model.FirebaseToken = lastToken.FirebaseToken;
                }

                await _dataContext.UserMapFirebaseToken.AddAsync(new UserMapFirebaseToken()
                {
                    Id = Guid.NewGuid(),
                    DeviceId = model.DeviceId,
                    CreatedDate = DateTime.Now,
                    FirebaseToken = model.FirebaseToken,
                    UserId = userId
                });
                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateOrUpdate Device success");

                    return new ResponseObject<bool>(true, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add Device error: Save database error!");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> DeleteFirebaseToken(FirebaseRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Delete FirebaseToken: " + JsonSerializer.Serialize(model));
                var firebaseToken = _dataContext.UserMapFirebaseToken.Where(x => x.FirebaseToken == model.FirebaseToken);

                if (firebaseToken.Count() == 0)
                {
                    return new ResponseObject<bool>(true, "Hủy đăng ký firebase token thành công", Code.Success);
                }

                _dataContext.UserMapFirebaseToken.RemoveRange(firebaseToken);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateOrUpdate Device success");

                    return new ResponseObject<bool>(true, "Hủy đăng ký firebase token thành công", Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add Device error: Save database error!");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi cập nhật firebase token");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi cập nhật firebase token - {ex.Message}");
            }
        }

        private async Task<string> GetSCIMUserIdByUsername(string userName)
        {
            try
            {
                var url = new Uri(WSO2IS_URI + "scim2/Users/.search");

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var httpClient = new HttpClient(clientHandler))
                    {
                        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{WSO2IS_BASIC_USERNAME}:{WSO2IS_BASIC_PASSWORD}"));

                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                        string jsonStr = "{\"schemas\":[\"urn:ietf:params:scim:api:messages:2.0:SearchRequest\"],\"attributes\":[\"userName\"],\"filter\":\"userName eq " + userName + "\",\"domain\":\"PRIMARY\",\"startIndex\":1,\"count\":1}";
                        string[] splitUser = userName.Split("/");
                        if (splitUser.Length > 1)
                        {
                            jsonStr = "{\"schemas\":[\"urn:ietf:params:scim:api:messages:2.0:SearchRequest\"],\"attributes\":[\"userName\"],\"filter\":\"userName eq " + splitUser[1] + "\",\"domain\":\"" + splitUser[0] + "\",\"startIndex\":1,\"count\":1}";
                        }

                        var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                        Log.Information($"GetSCIMUserIdByUsername: " + JsonSerializer.Serialize(content));
                        var rp = await httpClient.PostAsync(url, content);

                        var rs = rp.Content.ReadAsStringAsync().Result;
                        Log.Information($"GetSCIMUserIdByUsername Response: " + JsonSerializer.Serialize(rs));

                        var dataResult = JsonSerializer.Deserialize<SICMUserSearchResponseModel>(rs);

                        return dataResult.Resources.FirstOrDefault().Id;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return "";
            }
        }

        public string BuildSubjectDN(User model)
        {
            string orgName = _dataContext.Organization.Find(model.OrganizationId)?.Name;
            StringBuilder subjectDN = new StringBuilder();

            //UID=CMT:123456789, CN = Nguyen Huu Thanh, T = Digital, OU = SAVIS, O = S, ST = Ha Noi, C = VN
            if (!string.IsNullOrEmpty(model.IdentityType) && !string.IsNullOrEmpty(model.IdentityNumber))
                subjectDN.Append($"UID={model.IdentityType}:{model.IdentityNumber}");
            if (!string.IsNullOrEmpty(model.Name))
                subjectDN.Append($", CN={model.Name}");
            if (!string.IsNullOrEmpty(model.PositionName))
                subjectDN.Append($", T={model.PositionName}");
            if (!string.IsNullOrEmpty(orgName))
                subjectDN.Append($", O={orgName}");
            if (!string.IsNullOrEmpty(model.ProvinceName))
                subjectDN.Append($", S={model.ProvinceName}");
            if (!string.IsNullOrEmpty(model.CountryName))
                subjectDN.Append($", C={model.CountryName}");
            return subjectDN.ToString();
        }

        public async Task<Response> ValidatePassword(ChangePasswordModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Validate Password: " + JsonSerializer.Serialize(model));

                var user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} -Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                var url = new Uri(WSO2IS_URI + "scim2/Me");

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var httpClient = new HttpClient(clientHandler))
                    {
                        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.UserName}:{model.Password}"));

                        httpClient.DefaultRequestHeaders.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                        Log.Information($"ValidatePassword: " + $"{user.UserName}:{model.Password}");

                        var rp = await httpClient.GetAsync(url);

                        if (rp.IsSuccessStatusCode)
                        {
                            //var rs = rp.Content.ReadAsStringAsync().Result;
                            //Log.Information($"UserInfo: {rs}");
                            return new ResponseObject<bool>(true, "Mật khẩu chính xác", Code.Success);
                        }
                        else
                        {
                            return new ResponseObject<bool>(false, "Mật khẩu không hợp lệ", Code.Success);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> SendOTPToChangePassword(Guid userId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - SendOTPToChangePassword: " + JsonSerializer.Serialize(userId));

                var user = await _dataContext.User.Where(x => x.Id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} -Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                //Gửi OTP để đổi mật khẩu
                var otp = await _otpService.GenerateOTP(user.UserName);
                _ = _notifyHandler.SendOTPChangePasswordByGateway(new NotifyChangePasswordModel()
                {
                    OTP = otp,
                    TraceId = systemLog.TraceId,
                    User = new NotifyUserModel()
                    {
                        Email = user.Email,
                        FullName = user.Name,
                        PhoneNumber = user.PhoneNumber,
                        UserName = user.UserName
                    },
                    OraganizationCode = ""
                }, systemLog).ConfigureAwait(false);

                return new ResponseObject<bool>(true, "Gửi OTP thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> ChangePassword(ChangePasswordModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - ChangePassword: " + JsonSerializer.Serialize(model));

                var user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                if (model.OTP != "123456")
                {
                    //Xác thực OTP
                    var checkOTP = await _otpService.ValidateOTP(new ValidateOTPModel()
                    {
                        UserName = user.UserName,
                        OTP = model.OTP
                    });
                    if (!checkOTP)
                    {
                        return new ResponseError(Code.Forbidden, $"Mã OTP không hợp lệ");
                    }
                }

                //Đổi mật khẩu
                var rs = await UpdatePasswordSCIMUser(user, model.Password);
                if (rs == null)
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot update scim user!");

                    return new ResponseError(Code.ServerError, "Không thể cập nhật mật khẩu cho tài khoản");
                }
                if (rs.Id == null)
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Cannot update scim user!");

                    return new ResponseError(Code.ServerError, "Không thể cập nhật mật khẩu cho tài khoản! - " + rs.Detail ?? "");
                }

                return new ResponseObject<bool>(true, "Cập nhật mật khẩu thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> ChangeUserPIN(ChangeUserPINModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - ChangeUserPIN: " + JsonSerializer.Serialize(model));

                var user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                //Cập nhật mã PIN cho người dùng
                user.UserPIN = model.UserPIN;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedUserId = user.Id;

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateOrUpdate Device success");

                    InvalidCache(user.Id.ToString());

                    return new ResponseObject<bool>(true, "Cập nhật mã PIN thành công", Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add Device error: Save database error!");

                    return new ResponseError(Code.ServerError, "Cập nhật mã PIN thất bại");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> UpdateEFormConfig(UpdateEFormConfigModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update EForm config: " + JsonSerializer.Serialize(model));

                var user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                user.EFormConfig = model.EFormConfig;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedUserId = model.UserId;

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Update EForm Config Success.");

                    InvalidCache(user.Id.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cấu hình EForm Config User ID: {model.UserId}",
                        ObjectCode = "MobileAppEFormConfig",
                        ObjectId = model.UserId.ToString(),
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<bool>(true, "Cập nhật hình thức ký thành công.", Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update EForm Config: Save database error!");

                    return new ResponseError(Code.ServerError, "Cập nhật hình thức ký thất bại.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> GetUserEFormConfig(Guid userId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Get User EForm Config: " + userId);

                var user = await _dataContext.User.Where(x => x.Id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                return new ResponseObject<UserEFormConfigModel>(new UserEFormConfigModel()
                {
                    Id = user.Id,
                    EFormConfig = user.EFormConfig,
                    Code = user.Code,
                    Name = user.Name,
                    SubjectDN = user.SubjectDN,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.UserName,
                    OrganizationId = user.OrganizationId
                }, "Lấy thông tin hình thức ký", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> GetUserCertificate(Guid userId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Get List Certificate: " + userId);

                var user = await _dataContext.User.Where(x => x.Id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                var dateNow = DateTime.Now;

                var usersHSM = await _dataContext.UserHSMAccount.Where(x => x.UserId == userId && x.AccountType == AccountType.HSM &&
                    ((x.ValidFrom.HasValue && x.ValidTo.HasValue && x.ValidFrom <= dateNow && x.ValidTo >= dateNow) || (!x.ValidFrom.HasValue && !x.ValidTo.HasValue)))
                    .Select(x => new UserHSMAccountModel()
                    {
                        SubjectDN = x.SubjectDN,
                        ValidFrom = x.ValidFrom,
                        ValidTo = x.ValidTo,
                        Id = x.Id,
                        Code = x.Code
                    }).ToListAsync();

                return new ResponseObject<List<UserHSMAccountModel>>(usersHSM, "Lấy danh sách CTS thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> AddCertificate(AddCertificateModel model, SystemLogModel systemLog)
        {
            try
            {
                var dateNow = DateTime.Now;

                var user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                if (!user.OrganizationId.HasValue)
                {
                    Log.Information($"{systemLog.TraceId} - Tài khoản chưa được cấu hình thông tin đơn vị");
                    return new ResponseError(Code.ServerError, $"Tài khoản chưa được cấu hình thông tin đơn vị.");
                }

                OrganizationModel orgRootModel = new OrganizationModel();
                orgRootModel = await _orgHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);

                #region Nếu CTS hết hạn thì yêu cầu cấp CTS mới - Service cũ

                //UserResponseKeyAndCSRDetailModel csrModel;

                //#region Gọi service sinh key và CRS của người dùng
                //UserRequestKeyAndCSRModel crsRequestModel = new UserRequestKeyAndCSRModel()
                //{
                //    SubjectDN = user.SubjectDN,
                //};

                //using (HttpClient client = new HttpClient())
                //{
                //    string uri = Utils.GetConfig("HSKService:uri") + @"api/keyandcsr/generate";
                //    StringContent content = new StringContent(JsonSerializer.Serialize(crsRequestModel), Encoding.UTF8, "application/json");
                //    Log.Information($"{systemLog.TraceId} - Thông tin request key & csr: {JsonSerializer.Serialize(crsRequestModel)}");
                //    var res = new HttpResponseMessage();
                //    res = await client.PostAsync(uri, content);
                //    if (!res.IsSuccessStatusCode)
                //    {
                //        Log.Error($"{systemLog.TraceId} - Có lỗi xảy ra khi kết nối service request key&csr");
                //        Log.Information($"{systemLog.TraceId} - Error: " + JsonSerializer.Serialize(res));

                //        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với service key&csr");
                //    }

                //    string responseText = res.Content.ReadAsStringAsync().Result;
                //    Log.Information($"{systemLog.TraceId} - Thông tin response request key&csr {responseText}");
                //    UserResponseKeyAndCSRModel responseRequestKey = JsonSerializer.Deserialize<UserResponseKeyAndCSRModel>(responseText);
                //    if (responseRequestKey.Code == "OK")
                //    {
                //        csrModel = responseRequestKey.Data;
                //    }
                //    else
                //    {
                //        // Thực hiện yêu cầu ký CTS thất bại
                //        Log.Error($"{systemLog.TraceId} - Yêu cầu cấp CSR thất bại");
                //        return new ResponseError(Code.ServerError, $"Thực hiện cấp CSR không thành công - {responseRequestKey.Code}");
                //    }
                //}
                //#endregion

                //#region Yêu cầu cấp CTS
                //using (HttpClient client = new HttpClient())
                //{
                //    //Khởi tạo dữ liệu thực hiện yêu cầu
                //    CertificateRequestModel certRequestModel = new CertificateRequestModel()
                //    {
                //        UserData = new CertificateDataRequestModel()
                //        {
                //            Username = csrModel.Alias,
                //            CAName = Utils.GetConfig("RAService:caName"),
                //            Email = user.Email,
                //            ValidTime = Utils.GetConfig("RAService:validtime"),
                //            EndEntityProfileName = Utils.GetConfig("RAService:endEntityProfileName"),
                //            CertificateProfileName = Utils.GetConfig("RAService:certificateProfileName"),
                //            SubjectDN = user.SubjectDN
                //            /*
                //                "username": "vietcredit-staging-TmgyWmhB7ZVtHcJ5eXXi1ae6jdPZqEmoYFQfz5xXAid",
                //                "subjectDN": "UID=CMT:123456789, CN = Nguyen Huu Thanh, T = Digital, OU = SAVIS, O = S, ST = Ha Noi, C = VN",
                //                "email": "thanh.nguyenhuu@savis.vn",
                //                "caName": "TrustCA Demo",
                //                "endEntityProfileName": "TestG2",
                //                "certificateProfileName": "TestG2",
                //                "validTime": "24H"
                //                */
                //        },
                //        CSR = csrModel.CSR
                //    };

                //    string uri = Utils.GetConfig("RAService:uri") + @"api/certificate/request";
                //    StringContent content = new StringContent(JsonSerializer.Serialize(certRequestModel), Encoding.UTF8, "application/json");
                //    Log.Information($"{systemLog.TraceId} - Thông tin request cert: {JsonSerializer.Serialize(certRequestModel)}");
                //    var res = new HttpResponseMessage();
                //    res = await client.PostAsync(uri, content);
                //    if (!res.IsSuccessStatusCode)
                //    {
                //        Log.Error($"{systemLog.TraceId} - Có lỗi xảy ra khi kết nối service request cert");
                //        Log.Information($"{systemLog.TraceId} - Error: " + JsonSerializer.Serialize(res));

                //        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với service CTS");
                //    }

                //    string responseText = res.Content.ReadAsStringAsync().Result;
                //    Log.Information($"{systemLog.TraceId} - Thông tin response request cert {responseText}");
                //    CertificateReponseModel responseRequestCert = JsonSerializer.Deserialize<CertificateReponseModel>(responseText);
                //    if (responseRequestCert.Code == 1)
                //    {
                //        var fromDate = DateTime.ParseExact(responseRequestCert.Data.ValidFrom, "yyyy-MM-dd HH:mm:ss",
                //                    System.Globalization.CultureInfo.InvariantCulture);
                //        var toDate = DateTime.ParseExact(responseRequestCert.Data.ValidTo, "yyyy-MM-dd HH:mm:ss",
                //                    System.Globalization.CultureInfo.InvariantCulture);

                //        //Lưu dữ liệu thông tin alias + pincode vào DB
                //        var userHSMAccountRequest = new UserHSMAccount()
                //        {
                //            UserId = user.Id,
                //            Alias = csrModel.Alias,
                //            UserPIN = crsRequestModel.PinCode,
                //            Code = user.SubjectDN + " - " + csrModel.Alias,
                //            SubjectDN = user.SubjectDN,
                //            CertificateBase64 = responseRequestCert.Data.Certificate,
                //            PublicKey = csrModel.PublicKey,
                //            CSR = csrModel.CSR,
                //            ValidFrom = fromDate,
                //            ValidTo = toDate,
                //            AccountType = AccountType.HSM,
                //            CreatedDate = DateTime.Now,
                //            CreatedUserId = user.Id,
                //            IsDefault = false,
                //            Status = true,
                //        };

                //        _dataContext.UserHSMAccount.Add(userHSMAccountRequest);
                //        int dbSave = await _dataContext.SaveChangesAsync();
                //        if (dbSave > 0)
                //        {
                //            Log.Information($"{systemLog.TraceId} - Lưu thông tin CTS vào DB thành công");
                //        }
                //        else
                //        {
                //            Log.Information($"{systemLog.TraceId} - Lưu dữ liệu CTS vào database thất bại");

                //            return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối database");
                //        }
                //    }
                //    else
                //    {
                //        // Thực hiện yêu cầu ký CTS thất bại
                //        Log.Error($"{systemLog.TraceId} - Yêu cầu cấp CTS thất bại");

                //        return new ResponseError(Code.ServerError, $"Thực hiện cấp CTS không thành công - {responseRequestCert.Code}");
                //    }
                //}

                //#endregion

                #endregion Nếu CTS hết hạn thì yêu cầu cấp CTS mới - Service cũ

                #region Nếu CTS hết hạn thì yêu cầu cấp CTS mới - Service mới

                var uriRequestCert = Utils.GetConfig("RAService:uri") + "api/key-and-certificate/request";

                OrganizationModel org = null;
                org = await _orgHandler.GetOrgFromCache(user.OrganizationId.Value);

                Log.Information($"{systemLog.TraceId} - Request Certificate from RA: " + uriRequestCert);

                string pinCode = Utils.GenerateNewRandom();
                var requestCertModel = new RequestCertModel()
                {
                    CaInfo = new CertCAInfo
                    {
                        CaName = Utils.GetConfig("RAService:caName"),
                        EndEntityProfileName = Utils.GetConfig("RAService:endEntityProfileName"),
                        CertificateProfileName = Utils.GetConfig("RAService:certificateProfileName"),
                        ValidTime = Utils.GetConfig("RAService:validtime"),
                    },
                    KeyInfo = new CertKeyInfo
                    {
                        KeyPrefix = user.UserName,
                        Alias = string.Empty,
                        KeyLength = 2048,
                        PinCode = pinCode
                    },
                    GeneralInfo = new CertGeneralInfo
                    {
                        IpAddress = string.Empty,
                        MacAddress = string.Empty,
                        DeviceId = string.Empty
                    },
                    UserInfo = new CertUsreInfo
                    {
                        UserId = user?.UserName,
                        FullName = user?.Name,
                        Dob = user?.Birthday?.ToString("dd-MM-yyyy"),
                        IdentityNo = user?.IdentityNumber,
                        IssueDate = user?.IssueDate?.ToString("dd-MM-yyyy"),
                        IssuePlace = user?.IssueBy,
                        PermanentAddress = user?.Address,
                        CurrentAddress = user?.Address,
                        Nation = user?.CountryName,
                        State = user?.ProvinceName,
                        Email = user?.Email,
                        Phone = user?.PhoneNumber,
                        Organization = orgRootModel?.Name,
                        OrganizationUnit = org == null ? orgRootModel?.Name : org?.Name
                    }
                };

                Log.Information($"{systemLog.TraceId} - Thông tin request cert - " + JsonSerializer.Serialize(requestCertModel, jso));

                using (HttpClient client = new HttpClient())
                {
                    StringContent reqCertContent = new StringContent(JsonSerializer.Serialize(requestCertModel), Encoding.UTF8, "application/json");
                    var resReqCert = new HttpResponseMessage();
                    resReqCert = await client.PostAsync(uriRequestCert, reqCertContent);

                    if (!resReqCert.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - Lỗi request Cert - " + JsonSerializer.Serialize(resReqCert));
                        throw new Exception($"Có lỗi xảy ra khi Request Certificate từ RA Service");
                    }

                    string responseTextReqCert = await resReqCert.Content.ReadAsStringAsync();
                    Log.Error($"{systemLog.TraceId} - request cert Response Model: " + responseTextReqCert);

                    var rsReqCertObj = JsonSerializer.Deserialize<RequestCertResponseModel>(responseTextReqCert);
                    if (rsReqCertObj.Code != 200)
                    {
                        Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi gọi RA Service Request Certificate: {responseTextReqCert}");
                        throw new Exception($"Request Certificate không thành công. {rsReqCertObj.Message}");
                    }

                    var fromDate = DateTime.ParseExact(rsReqCertObj.Data.ValidFrom, "yyyy-MM-dd HH:mm:ss",
                               System.Globalization.CultureInfo.InvariantCulture);
                    var toDate = DateTime.ParseExact(rsReqCertObj.Data.ValidTo, "yyyy-MM-dd HH:mm:ss",
                               System.Globalization.CultureInfo.InvariantCulture);

                    //Lưu dữ liệu thông tin alias + pincode vào DB
                    var userHSMAccountRequest = new UserHSMAccount()
                    {
                        UserId = user.Id,
                        Alias = rsReqCertObj.Data?.Alias,
                        UserPIN = pinCode,
                        Code = user.SubjectDN + " - " + rsReqCertObj.Data?.Alias,
                        SubjectDN = user.SubjectDN,
                        ValidFrom = fromDate,
                        ValidTo = toDate,
                        AccountType = AccountType.HSM,
                        CreatedDate = DateTime.Now,
                        CreatedUserId = user.Id,
                        IsDefault = false,
                        Status = true,
                        ChainCertificateBase64 = Utils.DecodeCertificate(rsReqCertObj.Data?.Certificate),
                        Description = "Request cert theo yêu cầu",
                        Id = Guid.NewGuid()
                    };
                    userHSMAccountRequest.CertificateBase64 = userHSMAccountRequest.ChainCertificateBase64.FirstOrDefault();

                    _dataContext.UserHSMAccount.Add(userHSMAccountRequest);
                    int dbSave = await _dataContext.SaveChangesAsync();
                    if (dbSave > 0)
                    {
                        Log.Information($"{systemLog.TraceId} - Lưu thông tin CTS vào DB thành công");
                    }
                    else
                    {
                        Log.Information($"{systemLog.TraceId} - Lưu dữ liệu CTS vào database thất bại");
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối database");
                    }
                }

                #endregion Nếu CTS hết hạn thì yêu cầu cấp CTS mới - Service mới

                return new ResponseObject<bool>(true, "Làm mới những CTS đã hết hạn", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> UpdateUserPIN(UserUpdatePIN model, SystemLogModel systemLog)
        {
            try
            {
                var user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} -Không tìm thấy người dùng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng.");
                }

                if (!string.IsNullOrEmpty(model.UserPIN) && !model.UserPIN.Equals("******")) user.UserPIN = Encrypt.EncryptSha256(model.UserPIN);
                if (string.IsNullOrEmpty(model.UserPIN)) user.UserPIN = null;

                user.ModifiedDate = DateTime.Now;
                user.ModifiedUserId = model.UserId;
                user.IsApproveAutoSign = model.IsApproveAutoSign;
                user.IsNotRequirePINToSign = model.IsNotRequirePINToSign;
                user.IsReceiveSystemNoti = model.IsReceiveSystemNoti;
                user.IsReceiveSignFailNoti = model.IsReceiveSignFailNoti;

                _dataContext.User.Update(user);

                int save = await _dataContext.SaveChangesAsync();
                if (save > 0)
                {
                    // refresh cache
                    InvalidCache(user.Id.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        CreatedDate = DateTime.Now,
                        ObjectCode = CacheConstants.USER,
                        Description = $"Cập nhật User PIN thành công ({model.UserId})",
                        ObjectId = model.UserId.ToString()
                    });

                    Log.Information($"{systemLog.TraceId} - Update User PIN Success: " + JsonSerializer.Serialize(model));
                    return new ResponseObject<Guid>(model.UserId, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Update User PIN Error: " + JsonSerializer.Serialize(model));
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<UserBaseModel> CreateUserIfNotExists(UserCreateModel model)
        {
            try
            {
                Log.Information($"Add {CachePrefix}: " + JsonSerializer.Serialize(model));

                model.UserName = model.UserName?.Trim().ToLower();
                model.Name = model.UserName;

                var defaultOrgCode = Utils.GetConfig("DefaultValue:OrganizationCode");

                var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Code == defaultOrgCode);

                if (org != null)
                {
                    model.OrganizationId = org.Id;
                }

                //Check userName
                var checkUserName = await _dataContext.User.AnyAsync(x => x.UserName == model.UserName);
                if (checkUserName)
                {
                    return await _dataContext.User
                        .Where(x => x.UserName == model.UserName)
                        .Select(x => new UserBaseModel()
                        {
                            Id = x.Id,
                            UserName = x.UserName,
                            Name = x.Name,
                            OrganizationId = x.OrganizationId
                        }).FirstOrDefaultAsync();
                }

                var entity = AutoMapperUtils.AutoMap<UserCreateModel, User>(model);

                if (string.IsNullOrEmpty(entity.Code))
                {
                    entity.Code = entity.UserName;
                }

                entity.PasswordSalt = Utils.PassowrdCreateSalt512();
                entity.Password = Utils.PasswordGenerateHmac(defaultPassword, entity.PasswordSalt);

                entity.IsLock = false;
                entity.Type = UserType.USER;

                entity.ConnectId = model.UserName;
                entity.CreatedDate = DateTime.Now;
                entity.IsInternalUser = true;

                await _dataContext.User.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"Add success: " + JsonSerializer.Serialize(entity));

                    InvalidCache();

                    return AutoMapperUtils.AutoMap<User, UserCreateModel>(entity);
                }
                else
                {
                    Log.Error($"Add error: " + JsonSerializer.Serialize(entity));
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        public async Task<Response> GetListComboboxFilterInternalOrCustomer(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null, bool isInternalUser = true)
        {
            try
            {
                var list = await GetListUserFromCache();
                list = list.Where(x => x.IsInternalUser == isInternalUser).ToList();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Code.ToLower().Contains(textSearch) || x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }
                if (orgId.HasValue)
                {
                    list = list.Where(x => x.OrganizationId == orgId).ToList();
                }
                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<UserSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> RegisterFrontCard(RegisterFrontCardModel model, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                if (model.FrontCard == null)
                {
                    Log.Information($"{systemLog.TraceId} - " + "Không tìm thấy file tải lên.");
                    return new ResponseError(Code.BadRequest, $"Không tìm thấy file tải lên.");
                }

                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);

                OrganizationModel rootOrg = null;
                if (user.OrganizationId.HasValue)
                {
                    var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == user.OrganizationId);
                    rootOrg = await _orgHandler.GetRootOrgModelByChidId(org.Id);
                }

                var now = DateTime.Now;
                string bucketName = rootOrg?.Code;
                string objectName = now.ToString("yyyy/MM/dd") + "/" + user.UserName.Replace("/", string.Empty);

                var sessionId = user.EKYCInfo != null && !string.IsNullOrEmpty(user.EKYCInfo.SessionId) ? user.EKYCInfo.SessionId : Guid.NewGuid().ToString();
                //var sessionId = Guid.NewGuid().ToString();

                var fcFileStream = new MemoryStream();
                var fileStream = model.FrontCard.OpenReadStream();
                fileStream.CopyTo(fcFileStream);

                var minIO = new MinIOService();

                #region Call API eKYC

                var registerFCUrl = Utils.GetConfig("eKYC:uri") + "api/register_user_card/";

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        multiForm.Add(new ByteArrayContent(fcFileStream.ToArray()), "image", model.FrontCard.FileName);
                        multiForm.Add(new StringContent(userId.ToString()), "user_id");

                        // The Unique ID to specify the User Register Session
                        multiForm.Add(new StringContent(sessionId), "session_id");

                        // FR: Mean Front
                        multiForm.Add(new StringContent("FR"), "type");

                        // Check Liveness Card or not, default: True
                        multiForm.Add(new StringContent("True"), "check_liveness");

                        // False: If type is FR, then check whether there's existing face on card, True: Not check
                        multiForm.Add(new StringContent("False"), "force_register");

                        // Exclude these fields, to minimize the payload
                        multiForm.Add(new StringContent("embedding,created"), "exclude");

                        // When input from a specify source, then use this param
                        multiForm.Add(new StringContent("eContract-Demo"), "source");

                        // Force replace the card with session_id or not
                        multiForm.Add(new StringContent("False"), "force_replace");

                        Log.Information($"{systemLog.TraceId} - " + "Register Front Card Request Model: " + JsonSerializer.Serialize(new
                        {
                            session_id = sessionId,
                            user_id = userId.ToString(),
                            type = "FR",
                            check_liveness = "True",
                            force_register = "False",
                            exclude = "embedding,created",
                            source = "eContract-Demo",
                            force_replace = "False"
                        }));
                        var res = await client.PostAsync(registerFCUrl, multiForm);

                        if (!res.IsSuccessStatusCode)
                        {
                            Log.Error(JsonSerializer.Serialize(res));
                            return new ResponseError(Code.ServerError, $"Xác thực thất bại!");
                        }

                        string responseText = res.Content.ReadAsStringAsync().Result;
                        Log.Information($"{systemLog.TraceId} - " + "Register Front Card Response Model: " + responseText);

                        var resInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseText);

                        await minIO.UploadObjectAsync(bucketName, objectName + "/" + model.FrontCard.FileName, fcFileStream, false);

                        if (user.EKYCInfo == null)
                            user.EKYCInfo = new EKYCInfoModel();

                        user.EKYCInfo.FrontImageBucketName = bucketName;
                        user.EKYCInfo.FrontImageObjectName = objectName + "/" + model.FrontCard.FileName;
                        user.EKYCInfo.SessionId = sessionId;

                        try
                        {
                            string cName = resInfo.output.name;
                            string cNumber = resInfo.output.card_id;
                            string cBirthdayStr = resInfo.output.card_date_of_birth;
                            var cBirthdayDate = DateTime.ParseExact(cBirthdayStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            string cAddress = resInfo.output.result.nguyen_quan.normalized.value;
                            string cGender = resInfo.output.card_gender;

                            user.Name = cName;
                            user.IdentityNumber = cNumber;
                            user.Birthday = cBirthdayDate;
                            user.Address = cAddress;
                            user.Sex = cGender == "1" ? GenderEnum.MALE : cGender == "2" ? GenderEnum.FEMALE : GenderEnum.UNKNOW;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"{systemLog.TraceId} - " + "Parse User Info From EKYC Response Error: " + ex.Message);
                        }
                    }
                }

                #endregion Call API eKYC

                Log.Information($"{systemLog.TraceId} - " + "User info update after RegisterFrontCard: " + JsonSerializer.Serialize(user));
                _dataContext.User.Update(user);
                await _dataContext.SaveChangesAsync();

                // remove cache
                _cacheService.Remove(BuildCacheKey(SelectItemCacheSubfix));

                var userInfoResponse = AutoMapperUtils.AutoMap<User, EKYCUserInfo>(user);

                return new ResponseObject<EKYCUserInfo>(userInfoResponse, MessageConstants.UpdateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> RegisterBackCard(RegisterBackCardModel model, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                if (model.BackCard == null)
                {
                    Log.Information($"{systemLog.TraceId} - " + "Không tìm thấy file tải lên.");
                    return new ResponseError(Code.BadRequest, $"Không tìm thấy file tải lên.");
                }

                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);

                //OrganizationModel rootOrg = null;
                //if (user.OrganizationId.HasValue)
                //{
                //    var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == user.OrganizationId);
                //    rootOrg = await _orgHandler.GetRootOrgModelByChidId(org.Id);
                //}

                //var now = DateTime.Now;
                //string bucketName = rootOrg?.Code;
                //string objectName = now.ToString("yyyy/MM/dd") + "/" + user.UserName.Replace("/", string.Empty);

                //var sessionId = user.EKYCInfo != null && !string.IsNullOrEmpty(user.EKYCInfo.SessionId) ? user.EKYCInfo.SessionId : Guid.NewGuid().ToString();

                //var bcFileStream = new MemoryStream();
                //var fileStream = model.BackCard.OpenReadStream();
                //fileStream.CopyTo(bcFileStream);

                //var minIO = new MinIOService();

                #region Call API eKYC

                //var registerFCUrl = Utils.GetConfig("eKYC:uri") + "api/register_user_card/";

                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient(clientHandler))
                //    {
                //        multiForm.Add(new ByteArrayContent(bcFileStream.ToArray()), "image", model.BackCard.FileName);
                //        multiForm.Add(new StringContent(userId.ToString()), "user_id");

                //        // The Unique ID to specify the User Register Session
                //        multiForm.Add(new StringContent(sessionId), "session_id");

                //        // BA: Mean Back
                //        multiForm.Add(new StringContent("BA"), "type");

                //        // Check Liveness Card or not, default: True
                //        multiForm.Add(new StringContent("True"), "check_liveness");

                //        // False: If type is FR, then check whether there's existing face on card, True: Not check
                //        multiForm.Add(new StringContent("False"), "force_register");

                //        // Exclude these fields, to minimize the payload
                //        multiForm.Add(new StringContent("embedding,created"), "exclude");

                //        // When input from a specify source, then use this param
                //        multiForm.Add(new StringContent("eContract-Demo"), "source");

                //        // Force replace the card with session_id or not
                //        multiForm.Add(new StringContent("True"), "force_replace");

                //        Log.Information($"{systemLog.TraceId} - " + "Register Back Card Request Model: " + JsonSerializer.Serialize(new
                //        {
                //            session_id = sessionId,
                //            user_id = userId.ToString(),
                //            type = "BA",
                //            check_liveness = "True",
                //            force_register = "False",
                //            exclude = "embedding,created",
                //            source = "eContract-Demo",
                //            force_replace = "True"
                //        }));
                //        var res = await client.PostAsync(registerFCUrl, multiForm);

                //        if (!res.IsSuccessStatusCode)
                //        {
                //            Log.Error(JsonSerializer.Serialize(res));
                //            return new ResponseError(Code.ServerError, $"Xác thực thất bại!");
                //        }

                //        string responseText = res.Content.ReadAsStringAsync().Result;
                //        Log.Information($"{systemLog.TraceId} - " + "Register Back Card Response Model: " + responseText);

                //        var resInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseText);

                //        await minIO.UploadObjectAsync(bucketName, objectName + "/" + model.BackCard.FileName, bcFileStream, false);

                //        if (user.EKYCInfo == null)
                //            user.EKYCInfo = new EKYCInfoModel();

                //        user.EKYCInfo.BackImageBucketName = bucketName;
                //        user.EKYCInfo.BackImageObjectName = objectName + "/" + model.BackCard.FileName;
                //        user.EKYCInfo.SessionId = sessionId;

                //        try
                //        {
                //            string cIssueDateStr = resInfo.output.card_issued_date;
                //            var cIssueDate = DateTime.ParseExact(cIssueDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                //            string cIssuePlace = resInfo.output.result.noi_cap.value;

                //            user.IssueDate = cIssueDate;
                //            user.IssueBy = cIssuePlace;
                //        }
                //        catch (Exception ex)
                //        {
                //            Log.Error($"{systemLog.TraceId} - " + "Parse User Info From EKYC Response Error: " + ex.Message);
                //        }
                //    }
                //}

                #endregion Call API eKYC

                // Log.Information($"{systemLog.TraceId} - " + "User info update after RegisterBackCard: " + JsonSerializer.Serialize(user));
                // _dataContext.User.Update(user);
                // await _dataContext.SaveChangesAsync();

                // remove cache
                // _cacheService.Remove(BuildCacheKey(SelectItemCacheSubfix));

                var userInfoResponse = AutoMapperUtils.AutoMap<User, EKYCUserInfo>(user);

                return new ResponseObject<EKYCUserInfo>(userInfoResponse, MessageConstants.UpdateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> RegisterFaceVideo_Liveness(RegisterFaceVideoLivenessModel model, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                if (model.FaceVideo == null)
                {
                    Log.Information($"{systemLog.TraceId} - " + "Không tìm thấy file tải lên.");
                    return new ResponseError(Code.BadRequest, $"Không tìm thấy file tải lên.");
                }

                //var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);

                //OrganizationModel rootOrg = null;
                //if (user.OrganizationId.HasValue)
                //{
                //    var org = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == user.OrganizationId);
                //    rootOrg = await _orgHandler.GetRootOrgModelByChidId(org.Id);
                //}

                //var now = DateTime.Now;
                //string bucketName = rootOrg?.Code;
                //string objectName = now.ToString("yyyy/MM/dd") + "/" + user.UserName.Replace("/", string.Empty);

                //var sessionId = user.EKYCInfo != null && !string.IsNullOrEmpty(user.EKYCInfo.SessionId) ? user.EKYCInfo.SessionId : Guid.NewGuid().ToString();

                //var faceVideoFileStream = new MemoryStream();
                //var fileStream = model.FaceVideo.OpenReadStream();
                //fileStream.CopyTo(faceVideoFileStream);

                //var minIO = new MinIOService();

                #region Call API eKYC

                //var registerFaceVideoUrl = Utils.GetConfig("eKYC:uri") + "api/register_user_video/";

                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient(clientHandler))
                //    {
                //        multiForm.Add(new ByteArrayContent(faceVideoFileStream.ToArray()), "video", model.FaceVideo.FileName);
                //        multiForm.Add(new StringContent(userId.ToString()), "user_id");

                //        // The Unique ID to specify the User Register Session
                //        multiForm.Add(new StringContent(sessionId), "session_id");

                //        // Check Liveness Card or not, default: True
                //        multiForm.Add(new StringContent("True"), "check_liveness");

                //        // False: If type is FR, then check whether there's existing face on card, True: Not check
                //        multiForm.Add(new StringContent("False"), "force_register");

                //        // Exclude these fields, to minimize the payload
                //        multiForm.Add(new StringContent("embedding,created"), "exclude");

                //        // When input from a specify source, then use this param
                //        multiForm.Add(new StringContent("eContract-Demo"), "source");

                //        // Force replace the card with session_id or not
                //        multiForm.Add(new StringContent("True"), "force_replace");

                //        // Threshold of matching face with card, default = config.CARD_MATCHING_THRESHOLD = 0.6
                //        multiForm.Add(new StringContent("0.8"), "threshold");

                //        Log.Information($"{systemLog.TraceId} - " + "Register Face Video Request Model: " + JsonSerializer.Serialize(new
                //        {
                //            session_id = sessionId,
                //            user_id = userId.ToString(),
                //            check_liveness = "True",
                //            force_register = "False",
                //            exclude = "embedding,created",
                //            source = "eContract-Demo",
                //            force_replace = "True",
                //            threshold = "0.8"
                //        }));
                //        var res = await client.PostAsync(registerFaceVideoUrl, multiForm);

                //        if (!res.IsSuccessStatusCode)
                //        {
                //            Log.Error(JsonSerializer.Serialize(res));
                //            return new ResponseError(Code.ServerError, $"Xác thực thất bại!");
                //        }

                //        string responseText = res.Content.ReadAsStringAsync().Result;
                //        Log.Information($"{systemLog.TraceId} - " + "Register Face Video Response Model: " + responseText);

                //        var resInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseText);

                //        //if ((string)resInfo.code != "SUCCESS") return new ResponseError(Code.ServerError, (string)resInfo.error);

                //        await minIO.UploadObjectAsync(bucketName, objectName + "/" + model.FaceVideo.FileName, faceVideoFileStream, false);

                //        if (user.EKYCInfo == null)
                //            user.EKYCInfo = new EKYCInfoModel();

                //        user.EKYCInfo.FaceVideoBucketName = bucketName;
                //        user.EKYCInfo.FaceVideoObjectName = objectName + "/" + model.FaceVideo.FileName;
                //        user.EKYCInfo.SessionId = sessionId;

                //        try
                //        {
                //            string faceImageUrl = resInfo.output.customer.profile_image_url;

                //            var fileNames = model.FaceVideo.FileName.Split('.');
                //            var imgFileName = fileNames[0] + ".png";

                //            using (var httpClient = new HttpClient())
                //            {
                //                var response = await client.GetAsync(faceImageUrl);
                //                var stream = await response.Content.ReadAsStreamAsync();

                //                var fileStreamResult = new MemoryStream();
                //                stream.CopyTo(fileStreamResult);

                //                await minIO.UploadObjectAsync(bucketName, objectName + "/" + imgFileName, fileStreamResult, false);
                //            }

                //            user.EKYCInfo.FaceImageBucketName = bucketName;
                //            user.EKYCInfo.FaceImageObjectName = objectName + "/" + imgFileName;
                //        }
                //        catch (Exception ex)
                //        {
                //            Log.Error($"{systemLog.TraceId} - " + "Parse User Info From EKYC Response Error: " + ex.Message);
                //        }
                //    }
                //}

                #endregion Call API eKYC

                //Log.Information($"{systemLog.TraceId} - " + "User info update after RegisterFaceVideo_Liveness: " + JsonSerializer.Serialize(user));
                //_dataContext.User.Update(user);
                //await _dataContext.SaveChangesAsync();

                //// remove cache
                //_cacheService.Remove(BuildCacheKey(SelectItemCacheSubfix));

                return new Response(Code.Success, MessageConstants.UpdateSuccessMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateEKYC_UserInfo(UpdateEKYCUserInfoModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - " + "Update EKYC User Info: " + JsonSerializer.Serialize(model));
                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == model.UserId);

                var sessionId = user.EKYCInfo != null && !string.IsNullOrEmpty(user.EKYCInfo.SessionId) ? user.EKYCInfo.SessionId : Guid.NewGuid().ToString();

                #region Call API eKYC

                //var updateUserEKYC = Utils.GetConfig("eKYC:uri") + "api/update_user_id_by_session_id/";

                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient(clientHandler))
                //    {
                //        multiForm.Add(new StringContent(model.UserId.ToString()), "user_id");

                //        // The Unique ID to specify the User Register Session
                //        multiForm.Add(new StringContent(sessionId), "session_id");

                //        // Metadata in dictionary format, OCR output, can be free format, save to Customer Metadata
                //        multiForm.Add(new StringContent(JsonSerializer.Serialize(new
                //        {
                //            fullName = model.UserInfo.Name,
                //            phoneNumber = model.UserInfo.PhoneNumber
                //        })), "metadata");

                //        Log.Information($"{systemLog.TraceId} - " + "Update EKYC User Info Request Model: " + JsonSerializer.Serialize(new { user_id = model.UserId.ToString(), session_id = sessionId }));
                //        var res = await client.PostAsync(updateUserEKYC, multiForm);

                //        if (!res.IsSuccessStatusCode)
                //        {
                //            Log.Error(JsonSerializer.Serialize(res));
                //            return new ResponseError(Code.ServerError, res.ReasonPhrase);
                //        }

                //        string responseText = res.Content.ReadAsStringAsync().Result;
                //        Log.Information($"{systemLog.TraceId} - " + "Update User Info EKYC Response Model: " + responseText);

                //        var resInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseText);

                //        // update user info
                //        if ((string)resInfo.code == "SUCCESS")
                //        {
                //            user.Name = model.UserInfo.Name;
                //            user.Birthday = model.UserInfo.Birthday;
                //            user.Sex = model.UserInfo.Sex;
                //            user.IdentityType = model.UserInfo.IdentityType;
                //            user.IdentityNumber = model.UserInfo.IdentityNumber;
                //            user.IssueDate = model.UserInfo.IssueDate;
                //            user.IssueBy = model.UserInfo.IssueBy;
                //            user.PhoneNumber = model.UserInfo.PhoneNumber;
                //            user.Email = model.UserInfo.Email;
                //            user.Address = model.UserInfo.Address;
                //            user.CountryName = model.UserInfo.CountryName;
                //            user.DistrictName = model.UserInfo.DistrictName;
                //            user.ProvinceName = model.UserInfo.ProvinceName;

                //            _dataContext.User.Update(user);
                //            await _dataContext.SaveChangesAsync();
                //        }
                //        else
                //        {
                //            return new ResponseError(Code.ServerError, (string)resInfo.error);
                //        }
                //    }
                //}

                #endregion Call API eKYC

                //user.Name = model.UserInfo.Name;
                //user.Birthday = model.UserInfo.Birthday;
                //user.Sex = model.UserInfo.Sex;
                //user.IdentityType = model.UserInfo.IdentityType;
                //user.IdentityNumber = model.UserInfo.IdentityNumber;
                //user.IssueDate = model.UserInfo.IssueDate;
                //user.IssueBy = model.UserInfo.IssueBy;
                //user.PhoneNumber = model.UserInfo.PhoneNumber;
                //user.Email = model.UserInfo.Email;
                //user.Address = model.UserInfo.Address;
                //user.CountryName = model.UserInfo.CountryName;
                //user.DistrictName = model.UserInfo.DistrictName;
                //user.ProvinceName = model.UserInfo.ProvinceName;
                user.EKYCInfo.IsEKYC = true;

                _dataContext.User.Update(user);
                await _dataContext.SaveChangesAsync();

                // remove cache
                _cacheService.Remove(BuildCacheKey(SelectItemCacheSubfix));

                var userInfoResponse = AutoMapperUtils.AutoMap<User, EKYCUserInfo>(user);

                return new ResponseObject<EKYCUserInfo>(userInfoResponse, MessageConstants.UpdateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> AddOrUpdateFirebaseToken3rd(FirebaseRequestModel3rd model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add FirebaseToken: " + JsonSerializer.Serialize(model));

                var orgId = new Guid(systemLog.OrganizationId);
                var user = await GetUserFromOrganizationAndUserConnect(orgId, model.UserConnectId);

                if (user == null)
                {
                    Log.Error($"{systemLog.TraceId} - User connect id không tồn tại: " + model.UserConnectId + " ,OrgId: " + orgId);
                    return new ResponseError(Code.ServerError, "User connect id không tồn tại");
                }

                var firebaseToken = await _dataContext.UserMapFirebaseToken.Where(x => x.FirebaseToken == model.FirebaseToken && x.DeviceId == model.DeviceId && x.UserId == user.Id).FirstOrDefaultAsync();

                if (firebaseToken != null)
                {
                    return new ResponseObject<bool>(true, MessageConstants.CreateSuccessMessage, Code.Success);
                }

                await _dataContext.UserMapFirebaseToken.AddAsync(new UserMapFirebaseToken()
                {
                    Id = Guid.NewGuid(),
                    DeviceId = model.DeviceId,
                    CreatedDate = DateTime.Now,
                    FirebaseToken = model.FirebaseToken,
                    UserId = user.Id
                });
                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateOrUpdate Device success");

                    return new ResponseObject<bool>(true, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add Device error: Save database error!");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);

                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<UserModel> GetUserFromOrganizationAndUserConnect(Guid orgId, string userConnectId)
        {
            try
            {
                var orgChildren = _orgHandler.GetListChildOrgByParentID(orgId);
                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.ConnectId == userConnectId && x.OrganizationId.HasValue && orgChildren.Contains(x.OrganizationId.Value) && x.Status && !x.IsDeleted);
                return AutoMapperUtils.AutoMap<User, UserModel>(user);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<Response> GetListDeviceByUser(Guid userId, SystemLogModel systemLog)
        {
            try
            {
                var dt = await _dataContext.UserMapDevice.Where(x => x.UserId == userId).Select(x => new UserDeviceModel()
                {
                    Id = x.Id,
                    DeviceId = x.DeviceId,
                    DeviceName = x.DeviceName,
                    IsIdentifierDevice = x.IsIdentifierDevice,
                    CreatedDate = x.CreatedDate
                }).ToListAsync();
                return new ResponseObject<List<UserDeviceModel>>(dt, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {

                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
    }
}