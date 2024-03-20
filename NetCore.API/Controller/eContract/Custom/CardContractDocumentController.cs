using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using NetCore.Data;

namespace NetCore.API
{
    /// <summary>
    /// Module hợp đồng - VietCredit
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/card-contract")]
    [ApiExplorerSettings(GroupName = "eContract - 08.1 Card contract (hợp đồng thẻ vay) - Custom for VietCredit Group")]
    public class CardContractDocumentController : ApiControllerBase
    {
        private readonly IDocumentHandler _handler;
        private readonly ISignHashHandler _signHashHandler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        private readonly IOrganizationHandler _orgHandler;
        private readonly IUserHandler _userHandler;
        public CardContractDocumentController(
            IDocumentHandler handler,
            IOrganizationConfigHandler orgConfigHandler,
            ISignHashHandler signHashHandler,
            IOrganizationHandler orgHandler,
            IUserHandler userHandler,
            ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _signHashHandler = signHashHandler;
            _orgHandler = orgHandler;
            _userHandler = userHandler;
            _orgConfigHandler = orgConfigHandler;
        }

        #region API kết nối ứng dụng từ bên thứ 3
        /// <summary>
        /// Tạo mới nhiều hợp đồng từ ứng dụng bên thứ 3 - Metadata
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("create-many-meta-data")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateMetaDataMany3rd([FromBody] DocumentCreateMetaDataManyModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_DOCUMENT_DATA_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_DOCUMENT_DATA_3RD;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request); if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    model.OrganizationId = org.OrganizationId;
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }

                //if (u.OrganizationId == null || u.OrganizationId == Guid.Empty)
                //{
                //    return new ResponseError(Code.Forbidden, "X-OrganizationId không hợp lệ");
                //}
                #endregion

                #region Nếu truyền thêm thông tin đơn vị con
                if (!string.IsNullOrEmpty(model.OrganizationCode))
                {
                    var orgInfo = await _orgHandler.GetByCode(model.OrganizationCode);
                    if (orgInfo == null)
                    {
                        return new ResponseError(Code.NotFound, "Không tìm thấy đơn vị theo mã đã truyền vào.") { TraceId = u.SystemLog.TraceId };
                    }
                    model.CustomOrganizationId = orgInfo.Id;
                }
                #endregion

                #region Thêm mới người dùng
                if (model.ListDocument != null && model.ListDocument.Count > 0)
                {
                    if (model.ListUser != null && model.ListUser.Count > 0)
                    {
                        UpdateOrCreateUserModel updateOrCreateUserModelmodel = new UpdateOrCreateUserModel()
                        {
                            OrganizationId = model.CustomOrganizationId.HasValue ? model.CustomOrganizationId.Value : u.OrganizationId,
                            ListUser = model.ListUser
                        };
                        var rsUser = await _userHandler.CreateOrUpdate(updateOrCreateUserModelmodel, u.SystemLog);
                        if (rsUser.Code != Code.Success)
                        {
                            return rsUser;
                        }
                    }
                }
                #endregion

                #region Lấy thông tin đơn vị => bổ sung thêm meta-data
                var orgRootInfo = await _orgHandler.GetOrgFromCache(u.OrganizationId);
                foreach (var item in model.ListDocument)
                {
                    if (item.ListMetaData != null)
                    {
                        item.ListMetaData.Add(new MetaDataListForDocumentType()
                        {
                            MetaDataCode = "VC_DDUQ",
                            MetaDataValue = orgRootInfo.RepresentationFullName
                        });
                        item.ListMetaData.Add(new MetaDataListForDocumentType()
                        {
                            MetaDataCode = "VC_DDUQ_CHUCVU_1",
                            MetaDataValue = orgRootInfo.RepresentationPositionLine1
                        });
                        item.ListMetaData.Add(new MetaDataListForDocumentType()
                        {
                            MetaDataCode = "VC_DDUQ_CHUCVU_2",
                            MetaDataValue = orgRootInfo.RepresentationPositionLine2
                        });
                        
                        var listExportDocumentData = new List<ExportDocumentData>();
                        if (item.ListMetaData != null && item.ListMetaData.Count > 0)
                        {
                            var metaCreatedUserName = item.ListMetaData.FirstOrDefault(x => x.MetaDataCode == "VC_EMAIL_NVTV");
                            item.CreatedByUserName = !string.IsNullOrEmpty(metaCreatedUserName?.MetaDataValue) ? metaCreatedUserName?.MetaDataValue : model.ListUser?.FirstOrDefault()?.UserConnectId;

                            var metaOrgReceived = item.ListMetaData.FirstOrDefault(x => x.MetaDataCode == "VC_DONVITIEPNHAN");
                            if (metaOrgReceived != null)
                            {
                                var orgReceived = new ExportDocumentData(); 
                                orgReceived.Key = metaOrgReceived.MetaDataCode;
                                orgReceived.Value = metaOrgReceived.MetaDataValue;
                                orgReceived.Name = metaOrgReceived.MetaDataName;
                                listExportDocumentData.Add(orgReceived);
                            } 
                        
                            item.ExportDocumentData = listExportDocumentData;
                        }
                    }
                }
                #endregion

                var result = await _handler.CreateMetaDataMany3rd_iText7(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Phê duyệt hợp đồng từ ứng dụng bên thứ 3
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("lot-approve-documnet")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveFrom3rd([FromBody] DocumentApproveRejectFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_LOT_APPROVE_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_LOT_APPROVE_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request);    
                if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }
                #endregion

                var result = await _signHashHandler.ApproveFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Từ chối hợp đồng từ ứng dụng bên thứ 3
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("lot-reject-documnet")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectFrom3rd([FromBody] DocumentApproveRejectFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_LOT_REJECT_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_LOT_REJECT_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                #region Lấy thông tin đơn vị
                var consumerKey = Helper.GetConsumerKeyFromRequest(Request); if (string.IsNullOrEmpty(consumerKey)) return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };

                var lsConfig = await _orgConfigHandler.GetListCombobox(1, consumerKey);

                if (lsConfig.Code == Code.Success && lsConfig is ResponseObject<List<OrganizationConfigModel>> configList)
                {
                    var org = configList.Data.Where(x => x.ConsumerKey.Equals(consumerKey)).FirstOrDefault();

                    if (org == null)
                    {
                        Log.Information($"{u.SystemLog.TraceId} - Không tìm thấy thông tin đơn vị đang kết nối có consumerKey: {consumerKey}");

                        return new ResponseError(Code.Forbidden, "Không tìm thấy đơn vị đang kết nối") { TraceId = u.SystemLog.TraceId };
                    }

                    u.SystemLog.OrganizationId = org.OrganizationId.ToString();
                    u.OrganizationId = org.OrganizationId;
                }
                else
                {
                    Log.Information($"{u.SystemLog.TraceId} - Có lỗi xảy ra thông tin đơn vị đang kết nối có consumerKey: {consumerKey} - {lsConfig.Message}");

                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra, vui lòng thử lại sau");
                }
                #endregion

                var result = await _signHashHandler.RejectFrom3rd(model, u.SystemLog);

                return result;
            });
        }
        #endregion
    }
}
