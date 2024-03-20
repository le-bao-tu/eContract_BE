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
    public class WorkflowStateHandler : IWorkflowStateHandler
    {
        #region:Cache Config
        private const string CodePrefix = "WFLS.";
        private const string CachePrefix = CacheConstants.WORKFLOW_STATE;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        #endregion

        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;

        public WorkflowStateHandler(DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
        }

        public async Task<Response> Create(WorkflowStateCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));

                var isExistCode = _dataContext.WorkflowState.Any(c => c.Code == model.Code);
                if (isExistCode)
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} Mã trạng thái đã tồn tại!");
                    return new ResponseError(Code.NotFound, "Mã trạng thái đã tồn tại");
                }

                var entity = AutoMapperUtils.AutoMap<WorkflowStateCreateModel, WorkflowState>(model);

                var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(model.OrganizationId.Value);

                entity.OrganizationId = rootOrg.Id;
                entity.Id = Guid.NewGuid();
                entity.CreatedDate = DateTime.Now;

                await _dataContext.WorkflowState.AddAsync(entity);
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

        public async Task<Response> Update(WorkflowStateUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var exitsEntity = await _dataContext.WorkflowState.FindAsync(model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(exitsEntity));

                model.UpdateToEntity(exitsEntity);
                _dataContext.WorkflowState.Update(exitsEntity);
                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(exitsEntity));
                    InvalidCache(model.Id.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập {CachePrefix} mã:{exitsEntity.Code}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(exitsEntity.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
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

        public async Task<Response> Delete(List<Guid> ids, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Delete: " + JsonSerializer.Serialize(ids));

                var listResult = new List<ResponeDeleteModel>();
                string name = string.Empty;

                foreach (Guid id in ids)
                {
                    name = string.Empty;
                    var entity = await _dataContext.WorkflowState.FindAsync(id);

                    if (entity == null)
                    {
                        listResult.Add(new ResponeDeleteModel()
                        {
                            Id = id,
                            Name = name,
                            Result = false,
                            Message = MessageConstants.DeleteItemNotFoundMessage
                        });
                    }
                    else
                    {
                        name = entity.Name;
                        _dataContext.WorkflowState.Remove(entity);
                        try
                        {
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                InvalidCache(id.ToString());
                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = id,
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
                                    Id = id,
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
                                Id = id,
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

        public async Task<Response> Filter(WorkflowStateQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                var query = (from w in _dataContext.WorkflowState
                             select new WorkflowStateBaseModel
                             {
                                 Id = w.Id,
                                 Code = w.Code,
                                 Name = w.Name,
                                 NameForReject = w.NameForReject,
                                 Description = w.Description,
                                 Status = w.Status,
                                 CreatedDate = w.CreatedDate,
                                 OrganizationId = w.OrganizationId
                             });

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x => x.Name.ToLower().Contains(ts) || x.NameForReject.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(filter.OrganizationId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    query = query.Where(x => x.OrganizationId.HasValue && (listChildOrgID.Contains(x.OrganizationId.Value)));
                }

                if (filter.Status.HasValue)
                    query = query.Where(x => x.Status == filter.Status.Value);

                query = query.OrderByField(filter.PropertyName, filter.Ascending);

                int totalCount = query.Select(r => r.Id).Count();

                if (filter.PageSize.HasValue && filter.PageNumber.HasValue)
                {
                    if (filter.PageSize <= 0)
                        filter.PageSize = QueryFilter.DefaultPageSize;

                    //Calculate nunber of rows to skip on pagesize
                    int excludedRows = (filter.PageNumber.Value - 1) * (filter.PageSize.Value);
                    if (excludedRows <= 0)
                        excludedRows = 0;

                    query = query.Skip(excludedRows).Take(filter.PageSize.Value);
                }

                int dataCount = query.Count();

                var listResult = await query.ToListAsync();
                return new ResponseObject<PaginationList<WorkflowStateBaseModel>>(new PaginationList<WorkflowStateBaseModel>()
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
                    var entity = await _dataContext.WorkflowState.FindAsync(id);

                    return AutoMapperUtils.AutoMap<WorkflowState, WorkflowStateModel>(entity);
                });

                return new ResponseObject<WorkflowStateModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, Guid? orgId)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.WorkflowState
                                where item.Status
                                orderby item.Order
                                select new WorkflowStateSelectItemModel
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    NameForReject = item.NameForReject,
                                    DisplayName = item.Code + " - " + item.Name,
                                    OrganizationId = item.OrganizationId
                                });

                    return await data.ToListAsync<WorkflowStateSelectItemModel>();
                });

                if (orgId.HasValue && orgId != Guid.Empty)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(orgId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                return new ResponseObject<List<WorkflowStateSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        #region:Cache Handler
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
        #endregion
    }
}
