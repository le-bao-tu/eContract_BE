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
    public class RoleHandler : IRoleHandler
    {
        private const string CachePrefix = CacheConstants.ROLE;
        private const string CachePrefixNav = CacheConstants.NAVIGATION;
        private const string CachePrefixRoleData = "DATA";
        private const string CachePrefixRightRole = "RIGHT_ROLE";
        private const string CachePrefixNavigationRole = "NAV_ROLE";
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;

        public RoleHandler(DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
        }

        public async Task<Response> Create(RoleCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));
                // Check code unique
                var checkCode = await _dataContext.Role.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} fail: Code {model.Code} is exist!");
                    return new ResponseError(Code.Conflict, $"Mã {model.Code} đã tồn tại trong hệ thống");
                }

                var entity = AutoMapperUtils.AutoMap<RoleModel, Role>(model);
                entity.CreatedDate = DateTime.Now;

                OrganizationModel orgRootModel = new OrganizationModel();
                orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(model.OrganizationId);

                entity.OrganizationId = orgRootModel.Id;

                await _dataContext.Role.AddAsync(entity);

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

        public async Task<Response> Update(RoleUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.Role.FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.Role.Update(entity);

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
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateDataPermission(UpdateDataPermissionModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update Data Permission {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.Role.FirstOrDefaultAsync(x => x.Id == model.Id);

                entity.ModifiedDate = DateTime.Now;
                entity.ModifiedUserId = model.ModifiedUserId;
                _dataContext.Role.Update(entity);

                var ls1 = _dataContext.RoleMapDocumentType.Where(x => x.RoleId == model.Id);
                _dataContext.RoleMapDocumentType.RemoveRange(ls1);
                if (model.ListDocumentTypeId != null && model.ListDocumentTypeId.Count > 0)
                {
                    foreach (var item in model.ListDocumentTypeId)
                    {
                        await _dataContext.RoleMapDocumentType.AddAsync(new RoleMapDocumentType()
                        {
                            Id = Guid.NewGuid(),
                            RoleId = model.Id,
                            DocumentTypeId = item
                        });
                    }
                }

                var ls2 = _dataContext.RoleMapDocumentOfOrganization.Where(x => x.RoleId == model.Id);
                _dataContext.RoleMapDocumentOfOrganization.RemoveRange(ls2);
                if (model.ListDocumentOfOrganizationId != null && model.ListDocumentOfOrganizationId.Count > 0)
                {
                    foreach (var item in model.ListDocumentOfOrganizationId)
                    {
                        await _dataContext.RoleMapDocumentOfOrganization.AddAsync(new RoleMapDocumentOfOrganization()
                        {
                            Id = Guid.NewGuid(),
                            RoleId = model.Id,
                            OrganizationId = item
                        });
                    }
                }

                var ls3 = _dataContext.RoleMapUserInfoOfOrganization.Where(x => x.RoleId == model.Id);
                _dataContext.RoleMapUserInfoOfOrganization.RemoveRange(ls3);
                if (model.ListUserInfoOfOrganizationId != null && model.ListUserInfoOfOrganizationId.Count > 0)
                {
                    foreach (var item in model.ListUserInfoOfOrganizationId)
                    {
                        await _dataContext.RoleMapUserInfoOfOrganization.AddAsync(new RoleMapUserInfoOfOrganization()
                        {
                            Id = Guid.NewGuid(),
                            RoleId = model.Id,
                            OrganizationId = item
                        });
                    }
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(CachePrefixRoleData + model.Id.ToString());

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

        public async Task<Response> GetDataPermission(GetDataPermissionModel model, SystemLogModel systemLog)
        {
            try
            {
                //var lsRoleMapDocumentType = await _dataContext.RoleMapDocumentType.Where(x => x.RoleId == model.Id).Select(x => x.DocumentTypeId).ToListAsync();

                //var lsRoleMapDocumentOfOrganization = await _dataContext.RoleMapDocumentOfOrganization.Where(x => x.RoleId == model.Id).Select(x => x.OrganizationId).ToListAsync();

                //var lsRoleMapUserInfoOfOrganization = await _dataContext.RoleMapUserInfoOfOrganization.Where(x => x.RoleId == model.Id).Select(x => x.OrganizationId).ToListAsync();

                var rs = await GetRoleDataPermissionFromCacheAsync(model.Id);

                return new ResponseObject<ResultGetDataPermissionModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
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
                    var entity = await _dataContext.Role.FindAsync(item);

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
                        _dataContext.Role.Remove(entity);
                        try
                        {
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                InvalidCache(item.ToString());
                                InvalidCache(CachePrefixRoleData + item.ToString());

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

        public async Task<Response> Filter(RoleQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                OrganizationModel orgRootModel = new OrganizationModel();
                orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(filter.OrganizationId);

                var data = (from item in _dataContext.Role
                            where item.OrganizationId == orgRootModel.Id
                            select new RoleBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                Status = item.Status,
                                Description = item.Description,
                                CreatedDate = item.CreatedDate
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
                return new ResponseObject<PaginationList<RoleBaseModel>>(new PaginationList<RoleBaseModel>()
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

        public async Task<Response> GetById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Role.FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Role, RoleModel>(entity);
                });
                return new ResponseObject<RoleModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "")
        {
            try
            {
                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.Role.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                //                select new RoleSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Code = item.Code,
                //                    Name = item.Name,
                //                    OrganizationId = item.OrganizationId,
                //                    Note = ""
                //                });

                //    return await data.ToListAsync();
                //});

                var list = await GetListRoleFromCache();

                #region Lấy ra đơn vị gốc
                OrganizationModel orgRootModel = new OrganizationModel();
                orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(new Guid(systemLog.OrganizationId));
                #endregion
                list = list.Where(x => x.OrganizationId == orgRootModel.Id).ToList();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<RoleSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<RoleSelectItemModel>> GetListRoleFromCache(Guid? orgId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Role.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new RoleSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    OrganizationId = item.OrganizationId,
                                    Note = ""
                                });

                    return await data.ToListAsync();
                });

                if (orgId.HasValue)
                {
                    #region Lấy ra đơn vị gốc
                    OrganizationModel orgRootModel = new OrganizationModel();
                    orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(orgId.Value);
                    #endregion
                    if (orgRootModel != null)
                    {
                        list = list.Where(x => x.OrganizationId == orgRootModel.Id).ToList();
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Có lỗi xảy ra");
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
        
        private void InvalidCacheNav(string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                string cacheKey = BuildCacheKeyNav(id);
                _cacheService.Remove(cacheKey);
            }

            string selectItemCacheKey = BuildCacheKeyNav(SelectItemCacheSubfix);
            _cacheService.Remove(selectItemCacheKey);
        }

        private string BuildCacheKeyNav(string id)
        {
            return $"{CachePrefixNav}-{id}";
        }

        public async Task<ResultGetDataPermissionModel> GetRoleDataPermissionFromCacheByListIdAsync(List<Guid> listId)
        {
            try
            {
                ResultGetDataPermissionModel rs = new ResultGetDataPermissionModel()
                {
                    ListDocumentOfOrganizationId = new List<Guid>(),
                    ListDocumentTypeId = new List<Guid>(),
                    ListUserInfoOfOrganizationId = new List<Guid>()
                };
                foreach (var id in listId)
                {
                    var dt = await this.GetRoleDataPermissionFromCacheAsync(id);
                    if (dt != null)
                    {
                        if (dt.ListUserInfoOfOrganizationId != null && dt.ListUserInfoOfOrganizationId.Count > 0)
                        {
                            rs.ListUserInfoOfOrganizationId.AddRange(dt.ListUserInfoOfOrganizationId);
                        }
                        if (dt.ListDocumentTypeId != null && dt.ListDocumentTypeId.Count > 0)
                        {
                            rs.ListDocumentTypeId.AddRange(dt.ListDocumentTypeId);
                        }
                        if (dt.ListDocumentOfOrganizationId != null && dt.ListDocumentOfOrganizationId.Count > 0)
                        {
                            rs.ListDocumentOfOrganizationId.AddRange(dt.ListDocumentOfOrganizationId);
                        }
                    }
                }

                return new ResultGetDataPermissionModel()
                {
                    ListDocumentOfOrganizationId = rs.ListDocumentOfOrganizationId.Distinct().ToList(),
                    ListDocumentTypeId = rs.ListDocumentTypeId.Distinct().ToList(),
                    ListUserInfoOfOrganizationId = rs.ListUserInfoOfOrganizationId.Distinct().ToList()
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<ResultGetDataPermissionModel> GetRoleDataPermissionFromCacheAsync(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(CachePrefixRoleData + id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var lsRoleMapDocumentType = await _dataContext.RoleMapDocumentType.Where(x => x.RoleId == id).Select(x => x.DocumentTypeId).ToListAsync();

                    var lsRoleMapDocumentOfOrganization = await _dataContext.RoleMapDocumentOfOrganization.Where(x => x.RoleId == id).Select(x => x.OrganizationId).ToListAsync();

                    var lsRoleMapUserInfoOfOrganization = await _dataContext.RoleMapUserInfoOfOrganization.Where(x => x.RoleId == id).Select(x => x.OrganizationId).ToListAsync();

                    return new ResultGetDataPermissionModel()
                    {
                        ListDocumentOfOrganizationId = lsRoleMapDocumentOfOrganization,
                        ListDocumentTypeId = lsRoleMapDocumentType,
                        ListUserInfoOfOrganizationId = lsRoleMapUserInfoOfOrganization
                    };
                });
                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Response> UpdateRightByRole(UpdateRightByRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update Right by {CachePrefix}: " + JsonSerializer.Serialize(model));

                // xóa các right đã tồn tại
                var rightRoles = await _dataContext.RoleMapRight.Where(x => x.RoleId == model.RoleId).ToListAsync();
                _dataContext.RoleMapRight.RemoveRange(rightRoles);

                // thêm mới right
                var listRoleMapRight = new List<RoleMapRight>();
                foreach (var item in model.RightIds)
                {
                    var roleMapRight = new RoleMapRight()
                    {
                        Id = Guid.NewGuid(),
                        RightId = item,
                        RoleId = model.RoleId
                    };
                    listRoleMapRight.Add(roleMapRight);
                }

                await _dataContext.RoleMapRight.AddRangeAsync(listRoleMapRight);

                var dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Update Right by {CachePrefix}: " + JsonSerializer.Serialize(listRoleMapRight));
                    InvalidCache(CachePrefixRightRole + model.RoleId.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập Right {CachePrefix} mã: {model.RoleId}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(model.RoleId, MessageConstants.UpdateSuccessMessage, Code.Success);
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

        public async Task<Response> GetListRightIdByRole(GetListRightIdByRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                var rs = await GetListRightIdByRoleFromCacheAsync(model.RoleId);
                return new ResponseObject<ResultGetListRightIdByRoleModel>(new ResultGetListRightIdByRoleModel()
                {
                    RightIds = rs
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<Guid>> GetListRightIdByRoleFromCacheAsync(Guid roleId)
        {
            try
            {
                string cacheKey = BuildCacheKey(CachePrefixRightRole + roleId.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var listRightIds = await _dataContext.RoleMapRight.Where(x => x.RoleId == roleId).Select(x => x.RightId).ToListAsync();
                    return listRightIds;
                });
                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Response> GetListNavigationByRole(GetListNavigationByRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                var rs = await GetListNavigationIdByRoleFromCache(model.RoleId);
                return new ResponseObject<ResultGetListNavigationByRoleModel>(new ResultGetListNavigationByRoleModel()
                {
                    NavigationIds = rs
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private async Task<List<Guid>> GetListNavigationIdByRoleFromCache(Guid roleId)
        {
            try
            {
                string cacheKey = BuildCacheKey(CachePrefixNavigationRole + roleId.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var listNavigationIds = await _dataContext.NavigationMapRole.Where(x => x.RoleId == roleId).Select(x => x.NavigationId).ToListAsync();
                    return listNavigationIds;
                });
                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Response> UpdateNavigationByRole(UpdateNavigationByRoleModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update Navigation by {CachePrefix}: " + JsonSerializer.Serialize(model));

                if (model.NavigationIds == null || model.NavigationIds.Count < 1)
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} error: List navigation is empty!");
                    return new ResponseError(Code.BadRequest, "Danh sách Menu trống!");
                }

                // xóa các navigation đã tồn tại
                var navigationRoles = await _dataContext.NavigationMapRole.Where(x => x.RoleId == model.RoleId).ToListAsync();

                var listNavOldId = navigationRoles.Select(x=>x.NavigationId).ToList();

                _dataContext.NavigationMapRole.RemoveRange(navigationRoles);

                // thêm mới các navigation
                var listNavigaiton = new List<NavigationMapRole>();
                foreach(var item in model.NavigationIds)
                {
                    listNavigaiton.Add(new NavigationMapRole()
                    {
                        Id = Guid.NewGuid(),
                        NavigationId = item,
                        RoleId = model.RoleId
                    });
                }
                await _dataContext.NavigationMapRole.AddRangeAsync(listNavigaiton);

                var dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Update Navigation by {CachePrefix}: " + JsonSerializer.Serialize(listNavigaiton));
                    InvalidCache(CachePrefixNavigationRole + model.RoleId.ToString());
                    
                    foreach(var item in listNavOldId)
                        InvalidCacheNav(CachePrefixNavigationRole + item.ToString());

                    foreach(var item in model.NavigationIds)
                        InvalidCacheNav(CachePrefixNavigationRole + item.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập Navigation - {CachePrefix} mã: {model.RoleId}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(model.RoleId, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update Navigation - {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }
    }
}
