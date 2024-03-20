using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class DashboardHandler : IDashboardHandler
    {
        #region Message
        #endregion

        private readonly IMongoCollection<SystemLog> _logs;
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IUserRoleHandler _userRoleHandler;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly IUserHandler _userHandler;
        private readonly IRoleHandler _roleHandler;

        public DashboardHandler(IMongoDBDatabaseSettings settings, IOrganizationHandler organizationHandler, IUserRoleHandler userRoleHandler, IUserHandler userHandler, IRoleHandler roleHandler, DataContext dataContext, ICacheService cacheService)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _logs = database.GetCollection<SystemLog>(MongoCollections.SysLog);
            _organizationHandler = organizationHandler;
            _userRoleHandler = userRoleHandler;
            _dataContext = dataContext;
            _cacheService = cacheService;
            _userHandler = userHandler;
            _roleHandler = roleHandler;
        }

        public async Task<Response> GetDashboardInfo(DashboardRequest requestModel, Guid userId, Guid organizationId, SystemLogModel systemLog)
        {
            try
            {
                #region Lấy quyền người dùng
                var roleIds = await _userHandler.GetUserRoleFromCacheAsync(requestModel.CurrentUserId);
                var userRole = await _roleHandler.GetRoleDataPermissionFromCacheByListIdAsync(roleIds);
                #endregion

                var dateNow = DateTime.Now;
                // lấy cấu hình ngày sắp hết hạn
                int incommingExpiredDate = Convert.ToInt16(Utils.GetConfig("DefaultValue:IncommingExpirationDate"));
                IQueryable<Document> data;

                organizationId = requestModel.OrganizationId.HasValue ? requestModel.OrganizationId.Value : organizationId;
                var org = await _organizationHandler.GetById(organizationId);

                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(organizationId);
                List<string> listStrChildOrgID = listChildOrgID.Select(r => r.ToString()).ToList();

                //Lấy dữ liệu từ báo cáo đơn vị
                if (requestModel.OrganizationId.HasValue)
                {
                    // lấy các phòng ban con hoặc all nếu là quản trị hệ thống
                    //var userRoleFix = await _userRoleHandler.GetByUserId(userId);
                    //bool isOrgAdmin = false;
                    //if (userRoleFix != null && userRoleFix.GetPropValue("Data") != null)
                    //{
                    //    isOrgAdmin = (bool)userRoleFix?.GetPropValue("Data")?.GetPropValue("IsOrgAdmin");
                    //}

                    //Nếu không phải admin đơn vị thì chỉ xem được:
                    //hợp đồng mình tạo
                    //hợp đồng liên quan đến mình
                    //hợp đồng ở các đơn vị cấp dưới
                    //if (!isOrgAdmin)
                    //{
                    //    listChildOrgID.Remove(organizationId);
                    //    listStrChildOrgID.Remove(organizationId.ToString());
                    //}

                    data = (from doc in _dataContext.Document.AsNoTracking()
                            join docType in _dataContext.DocumentType.AsNoTracking() on doc.DocumentTypeId equals docType.Id into gj1
                            from docType in gj1.DefaultIfEmpty()
                            where !doc.IsDeleted
                               && (doc.OrganizationId.HasValue && listChildOrgID.Contains(doc.OrganizationId.Value))
                               && docType.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS
                               && docType.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                            select new Document()
                            {
                                Id = doc.Id,
                                DocumentStatus = doc.DocumentStatus,
                                NextStepUserId = doc.NextStepUserId,
                                SignExpireAtDate = doc.SignExpireAtDate,
                                SignCompleteAtDate = doc.SignCompleteAtDate,
                                SignCloseAtDate = doc.SignCloseAtDate
                            });
                }
                //Lấy dữ liệu từ dashboard
                else
                {
                    listStrChildOrgID = userRole.ListDocumentOfOrganizationId.Select(x => x.ToString()).ToList();
                    data = (from doc in _dataContext.Document.AsNoTracking()
                            join docType in _dataContext.DocumentType.AsNoTracking() on doc.DocumentTypeId equals docType.Id into gj1
                            from docType in gj1.DefaultIfEmpty()
                            where !doc.IsDeleted
                                && (doc.CreatedUserId == userId
                                   || doc.WorkFlowUserJson.Contains(userId.ToString())
                                   //Lọc theo dữ liệu đơn vị
                                   || (doc.OrganizationId.HasValue && userRole.ListDocumentOfOrganizationId.Contains(doc.OrganizationId.Value)))
                                   //Lọc theo dữ liệu loại hợp đồng
                                && userRole.ListDocumentTypeId.Contains(docType.Id)
                                && docType.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS
                                && docType.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                            select new Document()
                            {
                                Id = doc.Id,
                                DocumentStatus = doc.DocumentStatus,
                                NextStepUserId = doc.NextStepUserId,
                                SignExpireAtDate = doc.SignExpireAtDate,
                                SignCompleteAtDate = doc.SignCompleteAtDate,
                                SignCloseAtDate = doc.SignCloseAtDate
                            });
                }
              
                var complete = await data.Where(x => x.DocumentStatus == DocumentStatus.FINISH).CountAsync();
                var draft = await data.Where(x => x.DocumentStatus == DocumentStatus.CANCEL).CountAsync();
                var waitSignMe = await data.Where(x => x.DocumentStatus == DocumentStatus.PROCESSING 
                    && x.NextStepUserId == userId 
                    && !(x.DocumentStatus.Equals(DocumentStatus.PROCESSING) && x.SignExpireAtDate.HasValue && x.SignExpireAtDate.Value < dateNow)
                    && !(x.DocumentStatus.Equals(DocumentStatus.PROCESSING) && x.SignCloseAtDate.HasValue && x.SignCloseAtDate.Value < dateNow))
                    .CountAsync();
                //var error = await data.Where(x => x.DocumentStatus == DocumentStatus.ERROR).CountAsync();
                var expired = await data.Where(x => x.DocumentStatus.Equals(DocumentStatus.PROCESSING)
                    && x.SignExpireAtDate.HasValue 
                    && x.SignExpireAtDate.Value < dateNow
                    && !(x.SignCloseAtDate.HasValue && x.SignCloseAtDate.Value < dateNow)).CountAsync();

                var listIncommingExpired = data.Where(x => x.SignExpireAtDate.HasValue
                    && x.SignExpireAtDate.Value > dateNow
                    && x.SignExpireAtDate.Value <= dateNow.AddDays(incommingExpiredDate)
                    && x.DocumentStatus == DocumentStatus.PROCESSING
                    && !(x.SignCloseAtDate.HasValue && x.SignCloseAtDate.Value < dateNow));
                var incommingExpired = await listIncommingExpired.CountAsync();

                var totalDocument = await data.CountAsync();

                // bảng thống kê số lượng hợp đồng đã ký trong 7 ngày, 30 ngày, 90 ngày
                var listSignCompleted = new List<DocumentDashboardTableSignCompleted>();
                var listSignCompletedDays = new List<int>() { 7, 30, 90 };
                foreach (var days in listSignCompletedDays)
                {                   
                    var documentCountDay = await data.Where(x => x.DocumentStatus == DocumentStatus.FINISH
                        && x.SignCompleteAtDate.HasValue
                        && x.SignCompleteAtDate.Value.Date <= dateNow.Date
                        && x.SignCompleteAtDate.Value.Date >= dateNow.AddDays(-days + 1).Date)
                        .CountAsync();
             
                    listSignCompleted.Add(new DocumentDashboardTableSignCompleted()
                    {
                        DateTimeLabel = $"{days}",
                        DocumentCount = documentCountDay,
                        CompletedRate = complete != 0 ? (float)Math.Round(((float)documentCountDay / (float)complete * 100), 2) : 0
                    });
                }

                // bảng thống kê số lượng hợp đồng sắp hết hạn trong 1 ngày, 7 ngày, 30 ngày
                var listSignIncommingExpired = new List<DocumentDashboardTableSignIncommingExpired>();
                var listSignIncommingExpiredDays = new List<int>() { 1, 7, 30 };
                foreach (var days in listSignIncommingExpiredDays)
                {
                    var documentCountDay = await data.Where(x => x.DocumentStatus == DocumentStatus.PROCESSING
                        && x.SignExpireAtDate.HasValue
                        && x.SignExpireAtDate.Value > dateNow
                        && x.SignExpireAtDate.Value <= dateNow.Date.AddDays(days)
                        && !(x.SignCloseAtDate.HasValue && x.SignCloseAtDate.Value < dateNow))
                        .CountAsync();
                    listSignIncommingExpired.Add(new DocumentDashboardTableSignIncommingExpired()
                    {
                        DateTimeLabel = $"{days}",
                        DocumentCount = documentCountDay,
                        IncommingExpiredRate = incommingExpired != 0 ? (float)Math.Round(((float)documentCountDay / (float)incommingExpired * 100), 2) : 0
                    });
                }

                #region Gói dịch vụ
                //string ACTION_SIGN_DOC_LTV_CODE = nameof(LogConstants.ACTION_SIGN_LTV);
                //string ACTION_SIGN_DOC_TSA_SEAL = nameof(LogConstants.ACTION_SIGN_TSA_ESEAL);
                //string ACTION_SIGN_DOC_DIGITAL_NORMAL = nameof(LogConstants.ACTION_SIGN_DOC_DIGITAL_NORMAL);

                //// ký LTV
                //FilterDefinition<SystemLog> filterSignLTV =
                //    Builders<SystemLog>.Filter.Where(
                //        p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)                        
                //        && !string.IsNullOrEmpty(p.OrganizationId)
                //        && listStrChildOrgID.Contains(p.OrganizationId)
                //        && p.SubActionCode.Equals(ACTION_SIGN_DOC_LTV_CODE));           

                //var builderCountSignLTV = Builders<SystemLog>.Filter.And(filterSignLTV);

                //if (requestModel.FromDate.HasValue && requestModel.ToDate.HasValue)
                //{
                //    requestModel.FromDate = requestModel.FromDate.Value.Date;
                //    requestModel.ToDate = requestModel.ToDate.Value.AddDays(1).Date;

                //    FilterDefinition<SystemLog> filterSignLTVFilter =
                //        Builders<SystemLog>.Filter.Where(p =>
                //            p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                //            && !string.IsNullOrEmpty(p.OrganizationId)
                //            && listStrChildOrgID.Contains(p.OrganizationId)
                //            && p.SubActionCode.Equals(ACTION_SIGN_DOC_LTV_CODE)
                //            && p.CreatedDate >= requestModel.FromDate && p.CreatedDate < requestModel.ToDate);

                //    builderCountSignLTV = Builders<SystemLog>.Filter.And(filterSignLTVFilter);
                //}

                //long totalLTV = await _logs.Find(builderCountSignLTV).CountDocumentsAsync();

                //// ký TSA ESEAL
                //FilterDefinition<SystemLog> filterSignTSA_ESEAL = 
                //    Builders<SystemLog>.Filter.Where(
                //        p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                //        && !string.IsNullOrEmpty(p.OrganizationId)
                //        && listStrChildOrgID.Contains(p.OrganizationId)
                //        && p.SubActionCode.Equals(ACTION_SIGN_DOC_TSA_SEAL));

                //var builderCountSignTSA_ESEAL = Builders<SystemLog>.Filter.And(filterSignTSA_ESEAL);

                //if (requestModel.FromDate.HasValue && requestModel.ToDate.HasValue)
                //{
                //    FilterDefinition<SystemLog> builderCountSignTSA_ESEALFilter =
                //        Builders<SystemLog>.Filter.Where(p =>
                //            p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                //            && !string.IsNullOrEmpty(p.OrganizationId)
                //            && listStrChildOrgID.Contains(p.OrganizationId)
                //            && p.SubActionCode.Equals(ACTION_SIGN_DOC_TSA_SEAL)
                //            && p.CreatedDate >= requestModel.FromDate && p.CreatedDate < requestModel.ToDate);

                //    builderCountSignTSA_ESEAL = Builders<SystemLog>.Filter.And(builderCountSignTSA_ESEALFilter);
                //}

                //long totalTSA_ESEAL = await _logs.Find(builderCountSignTSA_ESEAL).CountDocumentsAsync();

                //// ký điện tử thường
                //FilterDefinition<SystemLog> filterSignDIGNormal =
                //    Builders<SystemLog>.Filter.Where(
                //        p => p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                //        && !string.IsNullOrEmpty(p.OrganizationId)
                //        && listStrChildOrgID.Contains(p.OrganizationId)
                //        && p.SubActionCode.Equals(ACTION_SIGN_DOC_DIGITAL_NORMAL));

                //var builderCountSignDigitalNormal = Builders<SystemLog>.Filter.And(filterSignDIGNormal);

                //if (requestModel.FromDate.HasValue && requestModel.ToDate.HasValue)
                //{
                //    FilterDefinition<SystemLog> filterSignDIGNormalFilter =
                //        Builders<SystemLog>.Filter.Where(p =>
                //            p.ObjectCode.Equals(CacheConstants.DOCUMENT)
                //            && !string.IsNullOrEmpty(p.OrganizationId)
                //            && listStrChildOrgID.Contains(p.OrganizationId)
                //            && p.SubActionCode.Equals(ACTION_SIGN_DOC_DIGITAL_NORMAL)
                //            && p.CreatedDate >= requestModel.FromDate && p.CreatedDate < requestModel.ToDate);

                //    builderCountSignDigitalNormal = Builders<SystemLog>.Filter.And(filterSignDIGNormalFilter);
                //}

                //long totalDigitalNormal = await _logs.Find(builderCountSignDigitalNormal).CountDocumentsAsync();
                #endregion

                return new ResponseObject<DashboardModel>(new DashboardModel()
                {
                    TotalDocument = totalDocument,
                    Completed = complete,
                    Draft = draft,
                    WaitMeSign = waitSignMe,
                    //Error = error,
                    Expired = expired,
                    IncommingExpired = incommingExpired,
                    ListSignCompleted = listSignCompleted,
                    ListSignIncommingExpired = listSignIncommingExpired,
                    //SignLTV = totalLTV,
                    //SignTSA_ESEAL = totalTSA_ESEAL,
                    //SignDIG_NORMAL = totalDigitalNormal,
                    OrganizationName = org.Code == Code.Success && org is ResponseObject<OrganizationModel> orgRootOfConnectTemp ? orgRootOfConnectTemp.Data.Name : ""
                });
            }
            catch(Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
            
        }

        public async Task<Response> GetNumberDocumentStatus(Guid userId, Guid organizationId)
        {
            try
            {
                var userRole = await _userRoleHandler.GetByUserId(userId);
                bool isOrgAdmin = false;
                if (userRole != null && userRole.GetPropValue("Data") != null)
                {
                    isOrgAdmin = (bool)userRole?.GetPropValue("Data")?.GetPropValue("IsOrgAdmin");
                }
                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(organizationId);

                //Nếu không phải admin đơn vị thì chỉ xem được:
                //hợp đồng mình tạo
                //hợp đồng liên quan đến mình
                //hợp đồng ở các đơn vị cấp dưới
                if (!isOrgAdmin)
                    listChildOrgID.Remove(organizationId);  

                var data = (from doc in _dataContext.Document.AsNoTracking()
                            join docType in _dataContext.DocumentType.AsNoTracking() on doc.DocumentTypeId equals docType.Id into gj1
                            from docType in gj1.DefaultIfEmpty()
                            where !doc.IsDeleted
                               && (doc.CreatedUserId == userId || doc.WorkFlowUserJson.Contains(userId.ToString()) || (doc.OrganizationId.HasValue && listChildOrgID.Contains(doc.OrganizationId.Value)))
                               && docType.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS
                               && docType.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                            select new Document()
                            {
                                DocumentStatus = doc.DocumentStatus,
                                NextStepUserId = doc.NextStepUserId,
                                SignExpireAtDate = doc.SignExpireAtDate,
                                SignCompleteAtDate = doc.SignCompleteAtDate,
                            });

                var complete = await data.Where(x => x.DocumentStatus == DocumentStatus.FINISH).CountAsync();
                var draft = await data.Where(x => x.DocumentStatus == DocumentStatus.DRAFT).CountAsync();
                var processing = await data.Where(x => x.DocumentStatus == DocumentStatus.PROCESSING).CountAsync();
                var waitMeSign = await data.Where(x => x.DocumentStatus == DocumentStatus.PROCESSING && x.NextStepUserId == userId && !(x.DocumentStatus.Equals(DocumentStatus.PROCESSING) && x.SignExpireAtDate.HasValue && x.SignExpireAtDate.Value < DateTime.Now)).CountAsync();

                return new ResponseObject<DocumentStatusModel>(new DocumentStatusModel()
                {
                    Completed = complete,
                    Draft = draft,
                    Processing = processing,
                    WaitMeSign = waitMeSign
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage}");
            }
        }

        //private void InvalidCache(string id = "")
        //{
        //    if (!string.IsNullOrEmpty(id))
        //    {
        //        string cacheKey = BuildCacheKey(id);
        //        _cacheService.Remove(cacheKey);
        //    }

        //    string selectItemCacheKey = BuildCacheKey(SelectItemCacheSubfix);
        //    _cacheService.Remove(selectItemCacheKey);
        //}

        //private string BuildCacheKey(string id)
        //{
        //    return $"{CachePrefix}-{id}";
        //}
    }
}