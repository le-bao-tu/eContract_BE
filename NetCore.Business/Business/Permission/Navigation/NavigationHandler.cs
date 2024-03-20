using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace NetCore.Business
{
    public class NavigationHandler : INavigationHandler
    {
        private const string CachePrefix = CacheConstants.NAVIGATION;
        private const string CachePrefixRole = CacheConstants.ROLE;
        private const string CachePrefixNavigationRole = "NAV_ROLE";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IRoleHandler _roleHandler;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;

        public NavigationHandler(DataContext dataContext, ICacheService cacheService, IRoleHandler roleHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _roleHandler = roleHandler;
        }

        public async Task<Response> Create(NavigationCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));

                // Check code unique
                var checkCode = await _dataContext.Navigation.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} fail: Code {model.Code} is exist!");
                    return new ResponseError(Code.Conflict, $"Mã {model.Code} đã tồn tại trong hệ thống");
                }

                var entity = AutoMapperUtils.AutoMap<NavigationModel, Navigation>(model);
                entity.CreatedDate = DateTime.Now;
                entity.CreatedUserId = model.CreatedUserId;

                await _dataContext.Navigation.AddAsync(entity);

                if (model.RoleIds != null)
                {
                    var listNavigationMapRole = new List<NavigationMapRole>();
                    foreach (var item in model.RoleIds)
                    {
                        var navigationRole = new NavigationMapRole()
                        {
                            Id = Guid.NewGuid(),
                            NavigationId = entity.Id,
                            RoleId = item
                        };
                        listNavigationMapRole.Add(navigationRole);
                    }

                    if (listNavigationMapRole.Count > 0) await _dataContext.NavigationMapRole.AddRangeAsync(listNavigationMapRole);
                }

                var dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();
                    InvalidCache(CachePrefixNavigationRole + entity.Id.ToString());
                    foreach(var item in model.RoleIds)
                        InvalidCache(CachePrefixNavigationRole + item.ToString());

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

        public async Task<Response> Update(NavigationUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.Navigation.FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.Navigation.Update(entity);

                var ls = await _roleHandler.GetListRoleFromCache(new Guid(systemLog.OrganizationId));
                var listRoleId = ls.Select(x => x.Id).ToList();

                // xóa role cũ
                var roles = await _dataContext.NavigationMapRole.Where(x => x.NavigationId == entity.Id && listRoleId.Contains(x.RoleId)).ToListAsync();

                var lisRoleOldIs = roles.Select(x => x.RoleId).ToList();

                _dataContext.NavigationMapRole.RemoveRange(roles);

                if (model.RoleIds != null && model.RoleIds.Count > 0)
                {
                    // insert role mới
                    var listNavigationMapRole = new List<NavigationMapRole>();
                    foreach (var item in model.RoleIds)
                    {
                        var navigationRole = new NavigationMapRole()
                        {
                            Id = Guid.NewGuid(),
                            NavigationId = entity.Id,
                            RoleId = item
                        };
                        listNavigationMapRole.Add(navigationRole);
                    }

                    if (listNavigationMapRole.Count > 0) await _dataContext.NavigationMapRole.AddRangeAsync(listNavigationMapRole);
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());
                    InvalidCache(CachePrefixNavigationRole + model.Id.ToString());
                    
                    foreach (var item in lisRoleOldIs)
                        InvalidCacheRole(CachePrefixNavigationRole + item.ToString());

                    foreach (var item in model.RoleIds)
                        InvalidCacheRole(CachePrefixNavigationRole + item.ToString());

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
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
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
                    var entity = await _dataContext.Navigation.FindAsync(item);

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
                        name = entity.Name;

                        //Kiểm tra xem có con hay không
                        var checkChild = await _dataContext.Navigation.AnyAsync(x => x.ParentId == entity.Id);
                        if (checkChild)
                        {
                            Log.Error($"{systemLog.TraceId} - Menu có con, không thể xóa!");
                            listResult.Add(new ResponeDeleteModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = "Không thể xóa vì menu có con"
                            });
                        }
                        else
                        {
                            // xóa navigation role
                            var navRoles = await _dataContext.NavigationMapRole.Where(x => x.NavigationId == entity.Id).ToListAsync();
                            _dataContext.NavigationMapRole.RemoveRange(navRoles);

                            // xóa navigation
                            _dataContext.Navigation.Remove(entity);
                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    InvalidCache(item.ToString());
                                    InvalidCache(CachePrefixNavigationRole + item.ToString());
                                    foreach (var roleId in navRoles.Select(x => x.RoleId).ToList())
                                        InvalidCache(CachePrefixNavigationRole + roleId.ToString());

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
                }
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Result Delete: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteItemSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Navigation.FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Navigation, NavigationModel>(entity);
                });

                rs.RoleIds = await GetListRoleIdByNavigationId(id);

                return new ResponseObject<NavigationModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private async Task<List<Guid>> GetListRoleIdByNavigationId(Guid navId)
        {
            string cacheKey = BuildCacheKey(CachePrefixNavigationRole + navId.ToString());
            return await _cacheService.GetOrCreate(cacheKey, async () =>
            {
                var roleIds = await _dataContext.NavigationMapRole.Where(x => x.NavigationId == navId).Select(x => x.RoleId).ToListAsync();

                return roleIds;
            });
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", int status = 1)
        {
            try
            {
                var list = await GetListNavFromCacheAsync();

                if (status == 1)
                {
                    list = list.Where(x => x.Status).ToList();
                }
                else if (status == 0)
                {
                    list = list.Where(x => !x.Status).ToList();
                }
                else if(status == 2)
                {
                }
                else
                {
                    list = list.Where(x => x.Status).ToList();
                }

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<NavigationSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<NavigationSelectItemModel>> GetListNavFromCacheAsync()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Navigation.OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new NavigationSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    ParentId = item.ParentId,
                                    Description = item.Description,
                                    HideInBreadcrumb = item.HideInBreadcrumb,
                                    I18nName = item.I18nName,
                                    Icon = item.Icon,
                                    Link = item.Link,
                                    Note = "",
                                    Status = item.Status,
                                    ListRoleId = new List<Guid>()
                                });
                    var ls = await data.ToListAsync();

                    //var lsRole = await _dataContext.NavigationMapRole.Select(x => new NavigationMapRole()
                    //{
                    //    NavigationId = x.NavigationId,
                    //    RoleId = x.RoleId
                    //}).ToListAsync();

                    //foreach (var item in ls)
                    //{
                    //    item.ListRoleId = lsRole.Where(x => x.NavigationId == item.Id).Select(x => x.RoleId).ToList();
                    //}

                    return ls;
                });

                foreach (var item in list)
                {
                    item.ListRoleId = await GetListRoleIdByNavigationId(item.Id);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public async Task<Response> Filter(NavigationQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.Navigation
                            select new NavigationBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                I18nName = item.I18nName,
                                Link = item.Link,
                                ParentId = item.ParentId,
                                Icon = item.Icon,
                                Status = item.Status,
                                Description = item.Description,
                                CreatedDate = item.CreatedDate,
                                HideInBreadcrumb = item.HideInBreadcrumb
                            });

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.Status.HasValue)
                {
                    data = data.Where(x => x.Status == filter.Status);
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
                return new ResponseObject<PaginationList<NavigationBaseModel>>(new PaginationList<NavigationBaseModel>()
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
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
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

        private void InvalidCacheRole(string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                string cacheKey = BuildCacheKeyRole(id);
                _cacheService.Remove(cacheKey);
            }

            string selectItemCacheKey = BuildCacheKeyRole(SelectItemCacheSubfix);
            _cacheService.Remove(selectItemCacheKey);
        }

        private string BuildCacheKeyRole(string id)
        {
            return $"{CachePrefixRole}-{id}";
        }
    }
}
