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
    public class WorkflowHandler : IWorkflowHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.WORKFLOW;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "WF.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;

        public WorkflowHandler(DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
        }

        public async Task<Response> Create(WorkflowCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));
                var entity = AutoMapperUtils.AutoMap<WorkflowCreateModel, Workflow>(model);

                entity.CreatedDate = DateTime.Now;

                var checkCode = await _dataContext.Workflow.AnyAsync(x => x.Code == model.Code && !x.IsDeleted);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} fail: Code {model.Code} is exist!");
                    return new ResponseError(Code.ServerError, "Mã quy trình đã tồn tại trong hệ thống!");
                }

                //var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(model.OrganizationId.Value);

                //entity.OrganizationId = rootOrg.Id;
                entity.Id = Guid.NewGuid();
                entity.IsDeleted = false;

                await _dataContext.Workflow.AddAsync(entity);

                int count = 1;
                foreach (var item in model.ListUser)
                {
                    WorkflowUserSign dt = AutoMapperUtils.AutoMap<WorkflowUserModel, WorkflowUserSign>(item);
                    dt.UserReceiveNotiExpire = new List<int>();
                    if (item.UserReceiveNotiExpire != null)
                    {
                        foreach (var u in item.UserReceiveNotiExpire)
                        {
                            if (u < model.ListUser.Count)
                            {
                                dt.UserReceiveNotiExpire.Add(u);
                            }
                        }
                    }
                    dt.Id = Guid.NewGuid();
                    dt.WorkflowId = entity.Id;
                    dt.Order = count++;
                    dt.CreatedDate = DateTime.Now;
                    await _dataContext.WorkflowUserSign.AddAsync(dt);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(entity));
                    InvalidCache(entity.Id.ToString());
                    InvalidCache(entity.Code);

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

        public async Task<Response> Update(WorkflowUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var exitsEntity = await _dataContext.Workflow
                         .FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(exitsEntity));

                exitsEntity.IsDeleted = true;

                _dataContext.Workflow.Update(exitsEntity);

                // Add new
                var entity = AutoMapperUtils.AutoMap<WorkflowUpdateModel, Workflow>(model);

                entity.CreatedUserId = model.ModifiedUserId;
                entity.CreatedDate = DateTime.Now;

                entity.Id = Guid.NewGuid();
                await _dataContext.Workflow.AddAsync(entity);

                int count = 1;
                foreach (var item in model.ListUser)
                {
                    WorkflowUserSign dt = AutoMapperUtils.AutoMap<WorkflowUserModel, WorkflowUserSign>(item);
                    dt.UserReceiveNotiExpire = new List<int>();
                    if (item.UserReceiveNotiExpire != null)
                    {
                        foreach (var u in item.UserReceiveNotiExpire)
                        {
                            if (u < model.ListUser.Count)
                            {
                                dt.UserReceiveNotiExpire.Add(u);
                            }
                        }
                    }
                    dt.Id = Guid.NewGuid();
                    dt.WorkflowId = entity.Id;
                    dt.Order = count++;
                    dt.CreatedDate = DateTime.Now;
                    await _dataContext.WorkflowUserSign.AddAsync(dt);
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());
                    InvalidCache(model.Code);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập {CachePrefix} mã:{entity.Code}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
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
                    var entity = await _dataContext.Workflow.FindAsync(item);

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
                        //Kiểm tra quy trình đã được gán cho hợp đồng nào chưa
                        //Lấy danh sách id quy trình có mã tương ứng với quy trình bị xóa
                        var listWFIdByCode = await _dataContext.Workflow.Where(x => x.Code == entity.Code).Select(x => x.Id).ToListAsync();

                        var checkDocument = await _dataContext.Document.AnyAsync(x => listWFIdByCode.Contains(x.WorkflowId));

                        if (checkDocument)
                        {
                            listResult.Add(new ResponeDeleteModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = "Đã tồn tại hợp đồng áp dụng quy trình " + entity.Code
                            });
                        }
                        else
                        {
                            name = entity.Name;
                            entity.IsDeleted = true;
                            _dataContext.Workflow.Update(entity);
                            //_dataContext.Workflow.Remove(entity);
                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    InvalidCache(item.ToString());
                                    InvalidCache(entity.Code);

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
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(WorkflowQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                var workflowData = (from wf in _dataContext.Workflow
                                    join user in _dataContext.User on wf.CreatedUserId equals user.Id into gj1
                                    from user in gj1.DefaultIfEmpty()
                                    select new WorkflowBaseModel()
                                    {
                                        Id = wf.Id,
                                        Code = wf.Code,
                                        Name = wf.Name,
                                        OrganizationId = wf.OrganizationId,
                                        UserId = wf.UserId,
                                        Status = wf.Status,
                                        CreatedDate = wf.CreatedDate,
                                        CreatedUserId = wf.CreatedUserId,
                                        CreatedUserName = user.UserName,
                                        IsDeleted = wf.IsDeleted
                                    });

                var data = workflowData.Where(x => x.IsDeleted == false);

                var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(filter.CurrentOrganizationId);
                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                data = data.Where(x => (x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)));

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.Status.HasValue)
                {
                    data = data.Where(x => x.Status == filter.Status);
                }

                //WF chung: UserId = null
                //WF riêng: UserId != null
                if (!filter.UserId.HasValue)
                {
                    data = data.Where(x => !x.UserId.HasValue);
                }
                else
                {
                    data = data.Where(x => x.UserId == filter.UserId);
                }

                if (filter.OrganizationId.HasValue)
                {
                    data = data.Where(x => (x.OrganizationId.HasValue && filter.OrganizationId == x.OrganizationId.Value));
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

                var workflowHistory = await workflowData.Where(x => x.IsDeleted == true).ToListAsync();
                listResult.ForEach(x => x.ListWorkflowHistory = workflowHistory.Where(x1 => x1.Code == x.Code).OrderByDescending(x2 => x2.CreatedDate).ToList());

                return new ResponseObject<PaginationList<WorkflowBaseModel>>(new PaginationList<WorkflowBaseModel>()
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
                var rs = await GetWFInfoById(id, systemLog);
                return new ResponseObject<WorkflowModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }


        public async Task<WorkflowModel> GetWFInfoById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Workflow
                        .FirstOrDefaultAsync(x => x.Id == id);

                    var model = AutoMapperUtils.AutoMap<Workflow, WorkflowModel>(entity);
                    model.ListUser = await GetListWorkflowUserByWfId(id);

                    return model;
                });
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return null;
            }
        }

        private async Task<List<WorkflowUserModel>> GetListWorkflowUserByWfId(Guid id)
        {
            var listUse = await (from wfc in _dataContext.WorkflowUserSign.Where(x => x.WorkflowId == id)
                         join user in _dataContext.User on wfc.UserId equals user.Id into gj
                         from user in gj.DefaultIfEmpty()
                         join state in _dataContext.WorkflowState on wfc.StateId equals state.Id into gstate
                         from state in gstate.DefaultIfEmpty()
                         orderby wfc.Order
                         select new WorkflowUserModel()
                         {
                             Id = wfc.Id,
                             UserId = wfc.UserId,
                             UserConnectId = user.ConnectId,
                             UserFullName = user.Name,
                             UserName = wfc.UserName,
                             UserEmail = user.Email,
                             Name = wfc.Name,
                             StateId = state.Id,
                             State = state.Code,
                             StateName = state.Name,
                             ADSSProfileName = wfc.ADSSProfileName,
                             SignExpireAfterDay = wfc.SignExpireAfterDay,
                             SignCloseAfterDay = wfc.SignCloseAfterDay,
                             UserPhoneNumber = user.PhoneNumber,
                             UserPositionName = user.PositionName,
                             Type = wfc.Type,
                             ConsentSignConfig = wfc.ConsentSignConfig,
                             IsAutoSign = wfc.IsAutoSign,
                             IsSendMailNotiResult = wfc.IsSendMailNotiResult,
                             IsSendMailNotiSign = wfc.IsSendMailNotiSign,
                             IsSendOTPNotiSign = wfc.IsSendOTPNotiSign,
                             IsSendNotiSignedFor3rdApp = wfc.IsSendNotiSignedFor3rdApp,
                             IsSignCertify = wfc.IsSignCertify,
                             IsSignLTV = wfc.IsSignLTV,
                             IsSignTSA = wfc.IsSignTSA,
                             NotifyConfigExpireId = wfc.NotifyConfigExpireId,
                             NotifyConfigRemindId = wfc.NotifyConfigRemindId,
                             NotifyConfigUserSignCompleteId = wfc.NotifyConfigUserSignCompleteId,
                             IsAllowRenew = wfc.IsAllowRenew,
                             MaxRenewTimes = wfc.MaxRenewTimes
                         }).ToListAsync();

            return listUse;
        }

        // Vì quy trình đã tạo ra không thể thay đổi nên không cần clear cache vẫn chạy tốt
        public async Task<WorkflowUserStepDetailModel> GetDetailStepById(SystemLogModel systemLog, Guid wfId, Guid? stepId)
        {
            try
            {
                string cacheKey = "";
                //StepId == null => hợp đồng đã hoàn thành quy trình
                if (stepId.HasValue)
                {
                    cacheKey = BuildCacheKey(wfId.ToString() + stepId.ToString());
                }
                else
                {
                    cacheKey = BuildCacheKey(wfId.ToString() + wfId.ToString());
                }

                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    WorkflowUserStepDetailModel model = new WorkflowUserStepDetailModel();
                    if (stepId.HasValue)
                    {
                        var entity = await _dataContext.WorkflowUserSign
                            .FirstOrDefaultAsync(x => x.Id == stepId);

                        model = AutoMapperUtils.AutoMap<WorkflowUserSign, WorkflowUserStepDetailModel>(entity);
                    }

                    model.ListStepIdReceiveResult = await _dataContext.WorkflowUserSign.Where(x => x.WorkflowId == wfId && x.IsSendMailNotiResult).Select(x => x.Id).ToListAsync();

                    return model;
                });
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                throw new Exception("Không lấy được thông tin quy trình");
            }
        }

        public async Task<List<WorkflowUserStepDetailModel>> GetDetailWFById(Guid wfId, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(wfId.ToString() + "-ListWfUserStepDetail");

                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    List<WorkflowUserStepDetailModel> list = new List<WorkflowUserStepDetailModel>();

                    var entity = await _dataContext.WorkflowUserSign.Where(x => x.WorkflowId == wfId).ToListAsync();

                    var model = AutoMapperUtils.AutoMap<WorkflowUserSign, WorkflowUserStepDetailModel>(entity);

                    return model;
                });
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                throw new Exception("Không lấy được thông tin quy trình");
            }
        }

        public async Task<Response> GetByCode(string code, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(code);
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Workflow
                        .FirstOrDefaultAsync(x => x.Code == code && x.IsDeleted == false);

                    if (entity == null)
                    {
                        return null;
                    }

                    var model = AutoMapperUtils.AutoMap<Workflow, WorkflowModel>(entity);

                    model.ListUser = await
                        (from wfc in _dataContext.WorkflowUserSign.Where(x => x.WorkflowId == entity.Id)
                         join user in _dataContext.User on wfc.UserId equals user.Id into gj
                         from user in gj.DefaultIfEmpty()
                         join state in _dataContext.WorkflowState on wfc.StateId equals state.Id into gstate
                         from state in gstate.DefaultIfEmpty()
                         orderby wfc.Order
                         select new WorkflowUserModel()
                         {
                             Id = wfc.Id,
                             UserId = wfc.UserId,
                             UserConnectId = user.ConnectId,
                             UserFullName = user.Name,
                             UserName = wfc.UserName,
                             UserEmail = user.Email,
                             Name = wfc.Name,
                             StateId = state.Id,
                             State = state.Code,
                             StateName = state.Name,
                             SignExpireAfterDay = wfc.SignExpireAfterDay,
                             SignCloseAfterDay = wfc.SignCloseAfterDay,
                             UserPhoneNumber = user.PhoneNumber,
                             UserPositionName = user.PositionName,
                             Type = wfc.Type,
                             ConsentSignConfig = wfc.ConsentSignConfig,
                             IsAutoSign = wfc.IsAutoSign,
                             IsSendMailNotiResult = wfc.IsSendMailNotiResult,
                             IsSendMailNotiSign = wfc.IsSendMailNotiSign,
                             IsSendOTPNotiSign = wfc.IsSendOTPNotiSign,
                             IsSendNotiSignedFor3rdApp = wfc.IsSendNotiSignedFor3rdApp,
                             IsSignCertify = wfc.IsSignCertify,
                             IsSignLTV = wfc.IsSignLTV,
                             IsSignTSA = wfc.IsSignTSA,
                             IsAllowRenew = wfc.IsAllowRenew,
                             MaxRenewTimes = wfc.MaxRenewTimes
                         }).ToListAsync();

                    return model;
                });

                if (rs == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy quy trình có mã là: {code}");
                }

                return new ResponseObject<WorkflowModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? userId = null, Guid? organizationId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Workflow.Where(x => x.Status == true && x.IsDeleted == false).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new WorkflowSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    Note = item.Code,
                                    OrganizationId = item.OrganizationId,
                                    UserId = item.UserId
                                });

                    return await data.ToListAsync();
                });

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (userId == null)
                {
                    list = list.Where(x => x.UserId == null).ToList();
                }
                else
                {
                    list = list.Where(x => x.UserId == userId || x.UserId == null).ToList();
                }

                if (organizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(organizationId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                //if (organizationId == null)
                //{
                //    list = list.Where(x => x.OrganizationId == null).ToList();
                //}
                //else
                //{
                //    list = list.Where(x => x.OrganizationId == organizationId || x.OrganizationId == null).ToList();
                //}

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<WorkflowSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
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
    }
}