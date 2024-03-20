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
    public class NotifyConfigHandler : INotifyConfigHandler
    {
        #region:Cache Config
        private const string CodePrefix = "NOTIFYCONFIG.";
        private const string CachePrefix = CacheConstants.NOTIFYCONFIG;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        #endregion

        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;

        public NotifyConfigHandler(DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
        }

        public async Task<Response> Create(NotifyConfigCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CodePrefix}: {JsonSerializer.Serialize(model)}");
                bool isExistCode = _dataContext.NotifyConfig.Any(c => c.Code == model.Code);
                if (isExistCode)
                    return new ResponseError(Code.ServerError, "Mã thông báo đã tồn tại");

                var entity = AutoMapperUtils.AutoMap<NotifyConfigCreateModel, NotifyConfig>(model);

                var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(model.OrganizationId.Value);

                entity.OrganizationId = rootOrg.Id;
                entity.Id = Guid.NewGuid();
                entity.CreatedDate = DateTime.Now;

                await _dataContext.NotifyConfig.AddAsync(entity);
                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create success: " + JsonSerializer.Serialize(entity));
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

        public async Task<Response> Update(NotifyConfigUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CodePrefix}: {JsonSerializer.Serialize(model)}");
                var exitsEntity = await _dataContext.NotifyConfig.FindAsync(model.Id);

                if (exitsEntity == null)
                    return new ResponseError(Code.ServerError, "Không tìm thấy đối tượng");

                Log.Information($"{systemLog.TraceId} - Before Update: { JsonSerializer.Serialize(exitsEntity)}");

                model.UpdateToEntity(exitsEntity);
                _dataContext.NotifyConfig.Update(exitsEntity);
                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update: { JsonSerializer.Serialize(exitsEntity)}");
                    InvalidCache(model.Id.ToString());
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
                var listResult = new List<ResponeDeleteModel>();
                string name = string.Empty;
                Log.Information($"{systemLog.TraceId} - List Delete: {JsonSerializer.Serialize(ids)}");

                var listDelete = await _dataContext.NotifyConfig.Where(r => ids.Contains(r.Id)).ToListAsync();
                foreach (var entity in listDelete)
                {
                    name = string.Empty;
                    if (entity == null)
                    {
                        listResult.Add(new ResponeDeleteModel()
                        {
                            Id = entity.Id,
                            Name = name,
                            Result = false,
                            Message = MessageConstants.DeleteItemNotFoundMessage
                        });
                    }
                    else
                    {
                        name = entity.Code;
                        _dataContext.NotifyConfig.Remove(entity);
                        try
                        {
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                InvalidCache(entity.Id.ToString());
                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = entity.Id,
                                    Name = name,
                                    Result = true,
                                    Message = MessageConstants.DeleteItemSuccessMessage
                                });
                            }
                            else
                            {
                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = entity.Id,
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
                                Id = entity.Id,
                                Name = name,
                                Result = false,
                                Message = ex.Message
                            });
                        }
                    }
                }

                Log.Information($"{systemLog.TraceId} - List Result Delete: {JsonSerializer.Serialize(listResult)}");
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(NotifyConfigQueryFilter filter)
        {
            try
            {
                var query = (from w in _dataContext.NotifyConfig.AsNoTracking()
                             select new NotifyConfigBaseModel
                             {
                                 Id = w.Id,
                                 Code = w.Code,
                                 Status = w.Status,
                                 DaySendNotiBefore = w.DaySendNotiBefore,
                                 IsRepeat = w.IsRepeat,
                                 TimeSendNotify = w.TimeSendNotify,
                                 IsSendSMS = w.IsSendSMS,
                                 IsSendEmail = w.IsSendEmail,
                                 EmailTitleTemplate = w.EmailTitleTemplate,
                                 IsSendNotification = w.IsSendNotification,
                                 NotificationTitleTemplate = w.NotificationTitleTemplate,
                                 CreatedDate = w.CreatedDate,
                                 OrganizationId = w.OrganizationId,
                                 NotifyType = w.NotifyType
                             });

                if (filter.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(filter.OrganizationId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    query = query.Where(x => x.OrganizationId.HasValue && (listChildOrgID.Contains(x.OrganizationId.Value)));
                }

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    query = query.Where(x => x.Code.ToLower().Contains(ts));
                }

                if (filter.Status.HasValue)
                    query = query.Where(x => x.Status == filter.Status.Value);

                if (filter.NotifyType.HasValue)
                {
                    query = query.Where(x => x.NotifyType == filter.NotifyType.Value);
                }

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
                return new ResponseObject<PaginationList<NotifyConfigBaseModel>>(new PaginationList<NotifyConfigBaseModel>()
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

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.NotifyConfig.FindAsync(id);

                    return AutoMapperUtils.AutoMap<NotifyConfig, NotifyConfigModel>(entity);
                });

                return new ResponseObject<NotifyConfigModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, Guid? orgID)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await GetListNotifyConfigFromCache();

                if (orgID.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(orgID.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                return new ResponseObject<List<NotifyConfigSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListComboboxByType(int type, SystemLogModel systemLog, Guid? orgID)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await GetListNotifyConfigFromCache();
                list = list.Where(x => x.NotifyType == type).ToList();

                return new ResponseObject<List<NotifyConfigSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private async Task<List<NotifyConfigSelectItemModel>> GetListNotifyConfigFromCache()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                return await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.NotifyConfig
                                where item.Status
                                orderby item.Order
                                select new NotifyConfigSelectItemModel
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    OrganizationId = item.OrganizationId,
                                    NotifyType = item.NotifyType
                                });

                    return await data.ToListAsync<NotifyConfigSelectItemModel>();
                });                               
            }
            catch (Exception ex)
            {
                throw ex;
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
