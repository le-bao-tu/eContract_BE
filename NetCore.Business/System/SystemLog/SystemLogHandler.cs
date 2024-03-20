using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class SystemLogHandler : ISystemLogHandler
    {
        private readonly IMongoCollection<SystemLog> _logs;
        private readonly ICacheService _cacheService;
        private readonly DataContext _dataContext;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly IUserHandler _userHandler;

        public SystemLogHandler(IMongoDBDatabaseSettings settings, DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler, IUserHandler userHandler)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _logs = database.GetCollection<SystemLog>(MongoCollections.SysLog);
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
            _dataContext = dataContext;
            _userHandler = userHandler;
        }

        public async Task<Response> Create(SystemLog model)
        {
            try
            {
                model.Id = string.Empty;

                #region Load data
                // User
                if (string.IsNullOrEmpty(model.UserName))
                {
                    //var cacheKey = $"{CacheConstants.USER}-{CacheConstants.LIST_SELECT}";

                    //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                    //{
                    //    var data = (from item in _dataContext.User.Where(x => x.Status == true && x.IsDeleted == false && x.Type == UserType.USER).OrderBy(x => x.Order).ThenBy(x => x.UserName)
                    //                select new UserSelectItemModel()
                    //                {
                    //                    Id = item.Id,
                    //                    Code = item.UserName,
                    //                    DisplayName = item.Name + " - " + item.UserName,
                    //                    Name = item.UserName,
                    //                    Email = item.Email,
                    //                    PhoneNumber = item.PhoneNumber,
                    //                    PositionName = item.PositionName,
                    //                    OrganizationId = item.OrganizationId,
                    //                    Note = item.Email,
                    //                    IdentityNumber = item.IdentityNumber,
                    //                    EFormConfig = item.EFormConfig,
                    //                    HasUserPIN = !string.IsNullOrEmpty(item.UserPIN)
                    //                });

                    //    return await data.ToListAsync();
                    //});
                    var list = await _userHandler.GetListUserFromCache();

                    Guid.TryParse(model.UserId, out Guid userId);
                    var user = list.Where(x => x.Id == userId).FirstOrDefault();
                    model.UserName = user?.DisplayName;
                }

                // Org
                if (string.IsNullOrEmpty(model.OrganizationName))
                {
                    var cacheKeyOrg = $"{CacheConstants.ORGANIZATION}-{CacheConstants.LIST_SELECT}";

                    var listOrg = await _organizationHandler.GetAllListOrgFromCacheAsync();

                    //var listOrg = await _cacheService.GetOrCreate(cacheKeyOrg, async () =>
                    //{
                    //    var data = (from item in _dataContext.Organization.Where(x => x.Status == true && !x.IsDeleted).OrderBy(x => x.Order).ThenBy(x => x.Name)
                    //                select new OrganizationSelectItemModel()
                    //                {
                    //                    Id = item.Id,
                    //                    Code = item.Code,
                    //                    Name = item.Name,
                    //                    Note = "",
                    //                    ParentId = item.ParentId
                    //                });

                    //    return await data.ToListAsync();
                    //});

                    Guid.TryParse(model.OrganizationId, out Guid orgId);
                    var org = listOrg.Where(x => x.Id == orgId).FirstOrDefault();
                    model.OrganizationName = org?.Code + " - " + org?.Name;
                }
                #endregion

                await _logs.InsertOneAsync(model).ConfigureAwait(false);
                return new ResponseObject<string>(model.Id, MessageConstants.CreateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Create(SystemLogModel model)
        {
            try
            {
                if (model.ListAction.Count == 0)
                {
                    return new ResponseError(Code.NotFound, $"Không có lịch sử thao tác cần ghi");
                }
                List<UserSelectItemModel> listUser = new List<UserSelectItemModel>();
                List<OrganizationSelectItemModel> listOrg = new List<OrganizationSelectItemModel>();
                #region Load data
                // User
                if (string.IsNullOrEmpty(model.UserName))
                {
                    //var cacheKey = $"{CacheConstants.USER}-{CacheConstants.LIST_SELECT}";

                    //listUser = await _cacheService.GetOrCreate(cacheKey, async () =>
                    //{
                    //    var data = (from item in _dataContext.User.Where(x => x.Status == true && x.IsDeleted == false && x.Type == UserType.USER).OrderBy(x => x.Order).ThenBy(x => x.UserName)
                    //                select new UserSelectItemModel()
                    //                {
                    //                    Id = item.Id,
                    //                    Code = item.UserName,
                    //                    DisplayName = item.Name + " - " + item.UserName,
                    //                    Name = item.UserName,
                    //                    Email = item.Email,
                    //                    PhoneNumber = item.PhoneNumber,
                    //                    PositionName = item.PositionName,
                    //                    OrganizationId = item.OrganizationId,
                    //                    Note = item.Email,
                    //                    IdentityNumber = item.IdentityNumber,
                    //                    EFormConfig = item.EFormConfig,
                    //                    HasUserPIN = !string.IsNullOrEmpty(item.UserPIN)
                    //                });

                    //    return await data.ToListAsync();
                    //});

                    listUser = await _userHandler.GetListUserFromCache();

                    Guid.TryParse(model.UserId, out Guid userId);
                    var user = listUser.Where(x => x.Id == userId).FirstOrDefault();
                    model.UserName = user?.DisplayName;
                }

                // Org
                if (string.IsNullOrEmpty(model.OrganizationName))
                {
                    var cacheKeyOrg = $"{CacheConstants.ORGANIZATION}-{CacheConstants.LIST_SELECT}";

                    listOrg = await _organizationHandler.GetAllListOrgFromCacheAsync();
                    //listOrg = await _cacheService.GetOrCreate(cacheKeyOrg, async () =>
                    //{
                    //    var data = (from item in _dataContext.Organization.Where(x => x.Status == true && !x.IsDeleted).OrderBy(x => x.Order).ThenBy(x => x.Name)
                    //                select new OrganizationSelectItemModel()
                    //                {
                    //                    Id = item.Id,
                    //                    Code = item.Code,
                    //                    Name = item.Name,
                    //                    Note = "",
                    //                    ParentId = item.ParentId
                    //                });

                    //    return await data.ToListAsync();
                    //});

                    Guid.TryParse(model.OrganizationId, out Guid orgId);
                    var org = listOrg.Where(x => x.Id == orgId).FirstOrDefault();
                    model.OrganizationName = org?.Code + " - " + org?.Name;
                }
                #endregion

                List<SystemLog> systemLogs = new List<SystemLog>();
                foreach (var item in model.ListAction)
                {
                    var systemLog = AutoMapperUtils.AutoMap<SystemLogModel, SystemLog>(model);
                    systemLog.Id = null;
                    systemLog.ObjectCode = item.ObjectCode;
                    systemLog.ObjectId = item.ObjectId;
                    systemLog.MetaData = item.MetaData;
                    systemLog.Description = item.Description;
                    systemLog.SubActionCode = item.SubActionCode;
                    systemLog.SubActionName = item.SubActionName;
                    if (!string.IsNullOrEmpty(item.UserId))
                    {
                        systemLog.UserId = item.UserId;
                        systemLog.UserName = listUser.Where(x => x.Id.ToString() == item.UserId).FirstOrDefault()?.DisplayName;
                    }
                    if (!string.IsNullOrEmpty(item.ActionCode))
                    {
                        systemLog.ActionCode = item.ActionCode;
                    }
                    if (!string.IsNullOrEmpty(item.ActionName))
                    {
                        systemLog.ActionName = item.ActionName;
                    }
                    if (item.CreatedDate.HasValue)
                    {
                        systemLog.CreatedDate = item.CreatedDate.Value;
                    }
                    systemLogs.Add(systemLog);
                }

                await _logs.InsertManyAsync(systemLogs).ConfigureAwait(false);
                return new ResponseObject<string>(string.Empty, MessageConstants.CreateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(SystemLog model)
        {
            try
            {
                var entity = await _logs.ReplaceOneAsync(log => log.Id == model.Id, model).ConfigureAwait(false);

                return new ResponseObject<string>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Delete(List<string> listId)
        {
            try
            {
                var builder = Builders<SystemLog>.Filter.And(Builders<SystemLog>.Filter.Where(p => listId.Contains(p.Id)));

                await _logs.DeleteManyAsync(builder).ConfigureAwait(false);

                return new ResponseObject<bool>(true, MessageConstants.DeleteSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(SystemLogQueryFilter filter)
        {
            try
            {
                // set time StartDate = 0h and time EndDate = 24h
                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    filter.StartDate = filter.StartDate.Value.Date;
                    filter.EndDate = filter.EndDate.Value.AddDays(1).Date;
                    //filter.StartDate = filter.StartDate.Value.AddHours(-DateTime.Now.Hour).AddMinutes(-DateTime.Now.Minute);
                    //filter.EndDate = filter.EndDate.Value.AddHours(23 - DateTime.Now.Hour).AddMinutes(59 - DateTime.Now.Minute);
                }

                #region:Lấy các đơn vị con theo đơn vị cha
                List<string> listChildOrgID = new List<string>();
                if (!string.IsNullOrEmpty(filter.OrganizationId))
                {
                    var listGuid = _organizationHandler.GetListChildOrgByParentID(Guid.Parse(filter.OrganizationId));
                    // var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(new Guid(filter.OrganizationId));
                    // List<Guid> listGuid = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);

                    listChildOrgID.AddRange(listGuid.Select(i => i.ToString()));
                }
                #endregion
                //var builder = Builders<SystemLog>.Filter.Eq(x => x.ActionCode, filter.ActionCode);

                if (string.IsNullOrEmpty(filter.TextSearch))
                {
                    filter.TextSearch = "";
                }
                var builder = Builders<SystemLog>.Filter.And(
                    Builders<SystemLog>.Filter.Where(p => p.Device.ToLower().Contains(filter.TextSearch.ToLower())
                        || p.ActionCode.ToLower().Contains(filter.TextSearch.ToLower())
                        || p.ActionName.ToLower().Contains(filter.TextSearch.ToLower())
                        || string.IsNullOrEmpty(filter.TextSearch)
                    ),
                    Builders<SystemLog>.Filter.Where(p => p.TraceId.Equals(filter.TradeId) || string.IsNullOrEmpty(filter.TradeId)),
                    Builders<SystemLog>.Filter.Where(p => p.ActionCode.Equals(filter.ActionCode) || string.IsNullOrEmpty(filter.ActionCode)),
                    Builders<SystemLog>.Filter.Where(p => p.Device.Equals(filter.Device) || string.IsNullOrEmpty(filter.Device)),
                    Builders<SystemLog>.Filter.Where(p => p.UserId == filter.UserId || filter.UserId == null),
                    Builders<SystemLog>.Filter.Where(p => listChildOrgID.Contains(p.OrganizationId) || string.IsNullOrEmpty(filter.OrganizationId)),
                    Builders<SystemLog>.Filter.Where(p => p.ApplicationId == filter.ApplicationId || filter.ApplicationId == null),
                    Builders<SystemLog>.Filter.Where(p => (filter.StartDate.HasValue && filter.EndDate.HasValue && p.CreatedDate >= filter.StartDate && p.CreatedDate < filter.EndDate)
                        || (!filter.StartDate.HasValue && !filter.EndDate.HasValue))
                );

                IFindFluent<SystemLog, SystemLog> data = _logs.Find(builder).Sort(Builders<SystemLog>.Sort.Descending(x => x.CreatedDate));

                var totalCount = await data.CountDocumentsAsync();

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
                    data = data.Skip(excludedRows).Limit(filter.PageSize.Value);
                }
                var dataCount = await data.CountDocumentsAsync();

                var listResult = await data.ToListAsync();

                return new ResponseObject<PaginationList<SystemLog>>(new PaginationList<SystemLog>()
                {
                    DataCount = (int)dataCount,
                    TotalCount = (int)totalCount,
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

        public async Task<Response> FilterByDocument(string documentId)
        {
            try
            {
                var builder = Builders<SystemLog>.Filter.And(
                    Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT) && p.ObjectId == documentId)
                );

                IFindFluent<SystemLog, SystemLog> data = _logs.Find(builder).Sort(Builders<SystemLog>.Sort.Ascending(x => x.CreatedDate));

                var listResult = await data.ToListAsync();

                var count = listResult.Count;

                return new ResponseObject<PaginationList<SystemLog>>(new PaginationList<SystemLog>()
                {
                    DataCount = count,
                    TotalCount = count,
                    Data = listResult
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetById(string id)
        {
            try
            {
                var entity = await _logs.Find(log => log.Id == id).FirstOrDefaultAsync();

                return new ResponseObject<SystemLog>(entity, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetReportDocumentByOrgID(OrgReportFilterModel filter)
        {
            try
            {
                if (filter.ToDate.Equals(DateTime.MinValue))
                {
                    filter.ToDate = DateTime.Now.AddDays(1).AddHours(-DateTime.Now.Hour).AddMinutes(-DateTime.Now.Minute).AddSeconds(-DateTime.Now.Second);
                }

                filter.FromDate = filter.FromDate.AddHours(-filter.FromDate.Hour).AddMinutes(-filter.FromDate.Minute).AddSeconds(-filter.FromDate.Second);
                filter.ToDate = filter.ToDate.AddDays(1).AddHours(-filter.ToDate.Hour).AddMinutes(-filter.ToDate.Minute).AddSeconds(-filter.ToDate.Second);

                int totalDocType = 0;
                int totalWFL = 0;
                long totalDigitalSign = 0;
                long totalHSMSign = 0;
                long totalUSBTokenSign = 0;
                long totalADSSSign = 0;

                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(filter.OrganizationID);
                List<string> listStrChildOrgID = listChildOrgID.Select(r => r.ToString()).ToList();

                //Số lượng loại hợp đồng
                totalDocType = await _dataContext.DocumentType.AsNoTracking()
                                    .Where(r => r.OrganizationId.HasValue && listChildOrgID.Contains(r.OrganizationId.Value))
                                    .Select(r => r.Id)
                                    .CountAsync();

                //Số lượng quy trình
                totalWFL = await _dataContext.Workflow.AsNoTracking()
                                .Where(r => r.OrganizationId.HasValue && listChildOrgID.Contains(r.OrganizationId.Value))
                                .Select(r => r.Id)
                                .CountAsync();
                #region Lấy số lượng hợp đồng
                var listDoc = _dataContext.Document.Where(r => r.IsDeleted == false && r.OrganizationId.HasValue && listChildOrgID.Contains(r.OrganizationId.Value));

                int totalDoc = listDoc.Select(r => r.Id).Count();
                int totalDocDraft = await listDoc.Where(x => x.DocumentStatus == DocumentStatus.DRAFT).Select(r => r.Id).CountAsync();
                int totalDocProcessing = await listDoc.Where(x => x.DocumentStatus == DocumentStatus.PROCESSING).Select(r => r.Id).CountAsync();
                int totalDocCancel = await listDoc.Where(x => x.DocumentStatus == DocumentStatus.CANCEL).Select(r => r.Id).CountAsync();
                int totalDocComplete = await listDoc.Where(x => x.DocumentStatus == DocumentStatus.FINISH).Select(r => r.Id).CountAsync();
                #endregion

                #region Đếm số lượt ký
                string ACTION_SIGN_DOC_DIGITAL_CODE = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL);
                string ACTION_SIGN_DOC_HSM_CODE = nameof(LogConstants.ACTION_SIGN_DOC_HSM);
                string ACTION_ATTACH_SIGN_TO_FILE_CODE = nameof(LogConstants.ACTION_ATTACH_SIGN_TO_FILE);
                string ACTION_SIGN_DOC_ADSS_CODE = nameof(LogConstants.ACTION_SIGN_DOC_ADSS);

                FilterDefinition<SystemLog> filterSignDigital =
                    Builders<SystemLog>.Filter.Where(
                        p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                        && p.ActionCode.Equals(ACTION_SIGN_DOC_DIGITAL_CODE)
                        && !string.IsNullOrEmpty(p.OrganizationId)
                        && listStrChildOrgID.Contains(p.OrganizationId)
                        );

                FilterDefinition<SystemLog> filterSignHSM = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                  && p.ActionCode.Equals(ACTION_SIGN_DOC_HSM_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId));

                FilterDefinition<SystemLog> filterSignUSBToken = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                 && p.ActionCode.Equals(ACTION_ATTACH_SIGN_TO_FILE_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId));

                FilterDefinition<SystemLog> filterSignADSS = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                 && p.ActionCode.Equals(ACTION_SIGN_DOC_ADSS_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId));

                filterSignDigital = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                                   && p.ActionCode.Equals(ACTION_SIGN_DOC_DIGITAL_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId)
                                   && p.CreatedDate >= filter.FromDate && p.CreatedDate < filter.ToDate);

                filterSignHSM = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                                  && p.ActionCode.Equals(ACTION_SIGN_DOC_HSM_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId)
                                  && p.CreatedDate >= filter.FromDate && p.CreatedDate < filter.ToDate);

                filterSignUSBToken = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                                 && p.ActionCode.Equals(ACTION_ATTACH_SIGN_TO_FILE_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId)
                                 && p.CreatedDate >= filter.FromDate && p.CreatedDate < filter.ToDate);

                filterSignADSS = Builders<SystemLog>.Filter.Where(p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                                 && p.ActionCode.Equals(ACTION_SIGN_DOC_ADSS_CODE) && !string.IsNullOrEmpty(p.OrganizationId) && listStrChildOrgID.Contains(p.OrganizationId)
                                 && p.CreatedDate >= filter.FromDate && p.CreatedDate < filter.ToDate);

                var builderCountSignDigital = Builders<SystemLog>.Filter.And(filterSignDigital);
                var builderCountSignHSM = Builders<SystemLog>.Filter.And(filterSignHSM);
                var builderCountSignUSBToken = Builders<SystemLog>.Filter.And(filterSignUSBToken);
                var builderCountSignADSS = Builders<SystemLog>.Filter.And(filterSignADSS);

                totalDigitalSign = await _logs.Find(builderCountSignDigital).CountDocumentsAsync();
                totalHSMSign = await _logs.Find(builderCountSignHSM).CountDocumentsAsync();
                totalUSBTokenSign = await _logs.Find(builderCountSignUSBToken).CountDocumentsAsync();
                totalADSSSign = await _logs.Find(builderCountSignADSS).CountDocumentsAsync();
                #endregion
                //LTV = totalHSMSign + totalUSBTokenSign
                //TSA = totalDigitalSign
                return new ResponseObject<Object>(new
                {
                    totalDocType,
                    totalWFL,
                    totalDoc,
                    totalDocDraft,
                    totalDocProcessing,
                    totalDocCancel,
                    totalDocComplete,
                    totalDigitalSign,
                    totalHSMSign,
                    totalUSBTokenSign,
                    totalADSSSign
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage}");
            }
        }

        public async Task<Response> GetActionCodeForCombobox()
        {
            try
            {
                var result = new List<ActionCodeForComboboxModel>();

                Type type = typeof(LogConstants);
                var flags = BindingFlags.Static | BindingFlags.Public;
                var fields = type.GetFields(flags).Where(x => x.IsLiteral && !x.IsInitOnly).ToList();

                foreach (var field in fields)
                {
                    var actionCodeModel = new ActionCodeForComboboxModel();
                    actionCodeModel.ActionName = (string)field.GetRawConstantValue();
                    actionCodeModel.ActionCode = field.Name;

                    result.Add(actionCodeModel);
                }

                return new ResponseObject<List<ActionCodeForComboboxModel>>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage}");
            }
        }
    }
}
