using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class UserRoleHandler : IUserRoleHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.USER_ROLE;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "UR.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;

        public UserRoleHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> CreateOrUpdate(UserRoleCreateOrUpdateModel model)
        {
            try
            {
                string msgSuccess = "";
                string msgErr = "";
                var userRole = await _dataContext.UserRole.Where(x => x.UserId == model.UserId).FirstOrDefaultAsync();

                if (userRole == null)
                {
                    userRole = AutoMapperUtils.AutoMap<UserRoleCreateOrUpdateModel, UserRole>(model);
                    userRole.CreatedDate = DateTime.Now;
                    msgSuccess = "Thêm mới quyền thành công";
                    msgErr = "Thêm mới quyền thất bại";
                    await _dataContext.UserRole.AddAsync(userRole);
                }
                else
                {
                    userRole.ModifiedDate = DateTime.Now;
                    userRole.ModifiedUserId = model.ModifiedUserId;
                    userRole.IsOrgAdmin = model.IsOrgAdmin;
                    userRole.IsSystemAdmin = model.IsSystemAdmin;
                    userRole.IsUser = model.IsUser;
                    msgSuccess = "Cập nhật quyền thành công";
                    msgErr = "Cập nhật quyền thất bại";
                    _dataContext.UserRole.Update(userRole);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information(msgSuccess + JsonSerializer.Serialize(userRole));
                    InvalidCache(userRole.UserId.ToString());

                    return new ResponseObject<Guid>(userRole.Id, msgSuccess, Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, msgErr);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> GetByUserId(Guid id)
        {
            try
            {
                var rs = await GetDataFromCache(id);
                var user = await _dataContext.User
                      .FirstOrDefaultAsync(x => x.Id == id);
                //var user = await _userHandler.GetUserFromCache(id);
                rs.IsLock = user.IsLock;

                return new ResponseObject<UserRoleModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
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

        public async Task<UserRoleModel> GetUserRoleById(Guid id)
        {
            try
            {
                return await GetDataFromCache(id);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<UserRoleModel> GetDataFromCache(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                return await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.UserRole
                        .FirstOrDefaultAsync(x => x.UserId == id);
                    if (entity == null)
                    {
                        return new UserRoleModel()
                        {
                            UserId = id
                        };
                    }
                    return AutoMapperUtils.AutoMap<UserRole, UserRoleModel>(entity);
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}