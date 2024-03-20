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
    public class UserSignConfigHandler : IUserSignConfigHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.USER_SIGN_CONFIG;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;

        public UserSignConfigHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(UserSignConfigCreateOrUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));

                if (!model.SignAppearanceImage)
                {
                    model.ScaleImage = 0;
                }
                if (!model.SignAppearanceLogo)
                {
                    model.ScaleLogo = 0;
                }

                var entity = AutoMapperUtils.AutoMap<UserSignConfigCreateOrUpdateModel, UserSignConfig>(model);

                entity.CreatedDate = DateTime.Now;

                var checkCode = await _dataContext.UserSignConfig.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Mã cấu hình chữ ký đã tồn tại trong hệ thống!");
                    return new ResponseError(Code.ServerError, "Mã cấu hình chữ ký đã tồn tại trong hệ thống!");
                }
                if (model.IsSignDefault)
                {
                    await _dataContext.UserSignConfig.Where(x => x.UserId == model.UserId).ForEachAsync(x => x.IsSignDefault = false);
                }
                //long identityNumber = await _dataContext.UserSignConfig.OrderByDescending(x => x.IdentityNumber).Select(x => x.IdentityNumber).FirstOrDefaultAsync();

                //entity.IdentityNumber = ++identityNumber;
                //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                entity.Id = Guid.NewGuid();
                await _dataContext.UserSignConfig.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới {CachePrefix} mã: {entity.Code}",
                        ObjectCode = CachePrefix,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(UserSignConfigCreateOrUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.UserSignConfig
                         .FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                if (model.IsSignDefault)
                {
                    await _dataContext.UserSignConfig.Where(x => x.UserId == model.UserId).ForEachAsync(x => x.IsSignDefault = false);
                }
                model.UpdateToEntity(entity);

                _dataContext.UserSignConfig.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập {CachePrefix} mã:{entity.Code}",
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
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Delete: " + JsonSerializer.Serialize(listId));

                var listResult = new List<ResponeDeleteModel>();
                var name = "";
         
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.UserSignConfig.FindAsync(item);
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
                        _dataContext.UserSignConfig.Remove(entity);
                        try
                        {
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                InvalidCache(item.ToString());

                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = item,
                                    Name = name,
                                    Result = true,
                                    Message = MessageConstants.DeleteItemSuccessMessage
                                });

                                systemLog.ListAction.Add(new ActionDetail()
                                {
                                    Description = $"Xóa {CachePrefix} mã: {entity.Code}",
                                    ObjectCode = CachePrefix,
                                    CreatedDate = DateTime.Now
                                });
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
                            Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
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
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(UserSignConfigQueryFilter filter)
        {
            try
            {
                var data = (from item in _dataContext.UserSignConfig
                            join u in _dataContext.User on item.UserId equals u.Id
                            select new UserSignConfigBaseModel()
                            {
                                Username = u.UserName,
                                Id = item.Id,
                                Code = item.Code,
                                LogoFileBase64 = item.LogoFileBase64,
                                ImageFileBase64 = item.ImageFileBase64,
                                SignAppearanceLogo = item.SignAppearanceLogo,
                                SignAppearanceImage = item.SignAppearanceImage,
                                ListSignInfoJson = item.ListSignInfoJson,
                                IsSignDefault = item.IsSignDefault,
                                AppearenceSignType = item.AppearenceSignType,
                                ScaleImage = item.ScaleImage,
                                ScaleLogo = item.ScaleLogo,
                                ScaleText = item.ScaleText,
                                CreatedDate = item.CreatedDate,
                                UserId = item.UserId,
                                BackgroundImageFileBase64 = item.BackgroundImageFileBase64,
                                MoreInfo = item.MoreInfo
                            });
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Code.ToLower().Contains(ts) || x.Username.ToLower().Contains(ts));
                }
                if (filter.UserId.HasValue)
                {
                    data = data.Where(x => x.UserId == filter.UserId);
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
                return new ResponseObject<PaginationList<UserSignConfigBaseModel>>(new PaginationList<UserSignConfigBaseModel>()
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

        public async Task<Response> GetListCombobox(int count = 0, Guid? userId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.UserSignConfig.OrderBy(x => x.Order).ThenByDescending(x => x.CreatedDate)
                                select new UserSignConfigBaseModel()
                                {
                                    Id = item.Id,
                                    LogoFileBase64 = item.LogoFileBase64,
                                    ImageFileBase64 = item.ImageFileBase64,
                                    SignAppearanceLogo = item.SignAppearanceLogo,
                                    SignAppearanceImage = item.SignAppearanceImage,
                                    Code = item.Code,
                                    AppearenceSignType = item.AppearenceSignType,
                                    UserId = item.UserId,
                                    ListSignInfoJson = item.ListSignInfoJson,
                                    IsSignDefault = item.IsSignDefault,
                                    ScaleLogo = item.ScaleLogo,
                                    ScaleImage = item.ScaleImage,
                                    ScaleText = item.ScaleText
                                });

                    return await data.ToListAsync();
                });
                if (userId.HasValue)
                {
                    list = list.Where(x => x.UserId == userId).ToList();
                }
                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<UserSignConfigBaseModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateFrom3rd(UserSign3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix} from 3rd: " + JsonSerializer.Serialize(model));

                if (string.IsNullOrEmpty(model.UserConnectId))
                {
                    Log.Information($"{systemLog.TraceId} - Tài khoản không được để trống");
                    return new ResponseError(Code.NotFound, $"Tài khoản không được để trống");
                }
                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.ConnectId == model.UserConnectId && x.OrganizationId == new Guid(systemLog.OrganizationId) && !x.IsDeleted);

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Tài khoản chưa được đăng ký");
                    return new ResponseError(Code.NotFound, $"Tài khoản chưa được đăng ký");
                }

                long identityNumber = await _dataContext.UserSignConfig.CountAsync(x => x.UserId == user.Id);

                UserSignConfig entity = new UserSignConfig()
                {
                    Id = Guid.NewGuid(),
                    ImageFileBase64 = model.ImageFileBase64,
                    ScaleImage = 1,
                    Status = true,
                    AppearenceSignType = "image",
                    Code = string.IsNullOrEmpty(model.Code) ? Utils.GenerateAutoCode(user.UserName + "-", identityNumber++) : model.Code,
                    IsSignDefault = false,
                    CreatedDate = DateTime.Now,
                    CreatedUserId = user.Id,
                    UserId = user.Id,
                };

                await _dataContext.UserSignConfig.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Add {CachePrefix} from 3rd success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateFrom3rd(UserSignUpdate3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix} from 3rd: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.UserSignConfig
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix} from 3rd: " + JsonSerializer.Serialize(entity));

                entity.ImageFileBase64 = model.ImageFileBase64;

                _dataContext.UserSignConfig.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache();

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> DeleteFrom3rd(Guid id, SystemLogModel systemLog)
        {
            try
            {
                var entity = await _dataContext.UserSignConfig.FindAsync(id);
                if (entity == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy cấu hình chữ ký");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy cấu hình chữ ký");
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Delete {CachePrefix} from 3rd: " + JsonSerializer.Serialize(entity));
                    _dataContext.UserSignConfig.Remove(entity);
                    try
                    {
                        int dbSave = await _dataContext.SaveChangesAsync();
                        if (dbSave > 0)
                        {
                            InvalidCache(id.ToString());
                            return new ResponseError(Code.Success, $"Xóa thành công");
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                            return new ResponseError(Code.ServerError, $"Có lỗi xảy ra, vui lòng thực hiện lại");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra, vui lòng liên hệ quản trị hệ thống");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetSignConfigUser3rd(string userConnectId, SystemLogModel systemLog)
        {
            try
            {
                if (string.IsNullOrEmpty(userConnectId))
                {
                    return new ResponseError(Code.NotFound, $"Tài khoản không được để trống");
                }

                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.ConnectId == userConnectId && x.OrganizationId == new Guid(systemLog.OrganizationId) && !x.IsDeleted);

                if (user == null)
                {
                    return new ResponseError(Code.NotFound, $"Tài khoản chưa được đăng ký");
                }
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.UserSignConfig.OrderBy(x => x.Order).ThenByDescending(x => x.CreatedDate)
                                select new UserSignConfigBaseModel()
                                {
                                    Id = item.Id,
                                    LogoFileBase64 = item.LogoFileBase64,
                                    ImageFileBase64 = item.ImageFileBase64,
                                    SignAppearanceLogo = item.SignAppearanceLogo,
                                    SignAppearanceImage = item.SignAppearanceImage,
                                    Code = item.Code,
                                    AppearenceSignType = item.AppearenceSignType,
                                    UserId = item.UserId,
                                    ListSignInfoJson = item.ListSignInfoJson,
                                    IsSignDefault = item.IsSignDefault,
                                    ScaleLogo = item.ScaleLogo,
                                    ScaleImage = item.ScaleImage,
                                    ScaleText = item.ScaleText,
                                    BackgroundImageFileBase64 = item.BackgroundImageFileBase64,
                                    MoreInfo = item.MoreInfo
                                });

                    return await data.ToListAsync();
                });
                List<UserSign3rdModel> listRS = new List<UserSign3rdModel>();
                listRS = list.Where(x => x.UserId == user.Id).Select(x => new UserSign3rdModel()
                {
                    Id = x.Id,
                    Code = x.Code,
                    ImageFileBase64 = x.ImageFileBase64
                }).ToList();

                return new ResponseObject<List<UserSign3rdModel>>(listRS, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<UserSignConfigModel> GetUserSignConfigForSign(Guid userId)
        {
            try
            {
                var userSignConfigDefault = await _dataContext.UserSignConfig.FirstOrDefaultAsync(x => x.Status && x.UserId == userId && x.IsSignDefault);

                if (userSignConfigDefault != null) return AutoMapperUtils.AutoMap<UserSignConfig, UserSignConfigModel>(userSignConfigDefault);

                var userSignConfig = await _dataContext.UserSignConfig
                    .Where(x => x.UserId == userId && x.Status)
                    .OrderByDescending(x => x.CreatedDate.HasValue)
                    .ThenByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                return AutoMapperUtils.AutoMap<UserSignConfig, UserSignConfigModel>(userSignConfig);
            }
            catch(Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        public async Task<UserSignConfigModel> GetById(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                return await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.UserSignConfig
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<UserSignConfig, UserSignConfigModel>(entity);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }
    }
}