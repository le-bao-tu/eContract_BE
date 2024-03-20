using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetCore.Data;

namespace NetCore.API
{
    /// <summary>
    /// Module hợp đồng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/document")]
    [ApiExplorerSettings(GroupName = "eContract - 08. Document (hợp đồng)")]
    public class DocumentController : ApiControllerBase
    {
        private readonly ISignDocumentHandler _signHandler;
        private readonly IUserHandler _userHandler;
        private readonly IDocumentHandler _handler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        private readonly IOrganizationHandler _orgHandler;
        private readonly ISignHashHandler _signHashHandler;
        private readonly ISystemNotifyHandler _sysNotiHandler;
        public DocumentController(ISignHashHandler signHashHander, 
            IDocumentHandler handler, 
            IOrganizationConfigHandler orgConfigHandler, 
            IOrganizationHandler orgHandler, 
            IUserHandler userHandler,
            ISystemNotifyHandler sysNotiHandler,
            ISignDocumentHandler signHandler, 
            ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _signHandler = signHandler;
            _userHandler = userHandler;
            _orgHandler = orgHandler;
            _orgConfigHandler = orgConfigHandler;
            _signHashHandler = signHashHander;
            _sysNotiHandler = sysNotiHandler;
        }

        #region API kết nối ứng dụng từ bên thứ 3

        /// <summary>
        /// Tạo mới nhiều hợp đồng từ ứng dụng bên thứ 3 - PDF
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("create-many-pdf")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePDFMany3rd([FromBody] DocumentCreatePDFManyModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_DOCUMENT_PDF_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_DOCUMENT_PDF_3RD;
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

                var result = await _handler.CreatePDFMany3rd(model, false, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Tạo mới nhiều hợp đồng từ ứng dụng bên thứ 3 - Docx
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("create-many-docx")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateDocxMany3rd([FromBody] DocumentCreatePDFManyModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_DOCUMENT_DOCX_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_DOCUMENT_DOCX_3RD;
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
                var result = await _handler.CreatePDFMany3rd(model, true, u.SystemLog);

                return result;
            });
        }

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

                #region Add MetaData to Export Document
                if (model.ListDocument != null)
                {
                    foreach (var item in model.ListDocument)
                    {
                        if (item.ListMetaData != null)
                        {
                            // get meta data VC_EMAIL_NVTV
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
                }
                #endregion
                
                var result = await _handler.CreateMetaDataMany3rd_iText7(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách hợp đồng theo người dùng từ đơn vị thứ 3
        /// </summary> 
        /// <returns>Trạng thái tạo mới, link ký Document</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("get-documents-by-user-connect-id")]
        [ProducesResponseType(typeof(ResponseObject<CreatDocument3rdResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListDocumentByUserConnectId(DocumentRequestByUserConnectIdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOCUMENT_FROM_3RD);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOCUMENT_FROM_3RD;
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

                var result = await _handler.GetListDocumentByUserConnectId(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy url download hợp đồng
        /// </summary> 
        /// <param name="model">Model request</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpPost, Route("get-url-download-document")]
        [ProducesResponseType(typeof(ResponseObject<DocumentSignedResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestUrlDownloadDocumentFrom3rd([FromBody] RequestUrlDownloadDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, DataLog.Location>(model.Location);
                u.SystemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);

                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_URL_DOWNLOAD_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_URL_DOWNLOAD_DOCUMENT;
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

                var result = await _handler.RequestUrlDownloadDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        ///  Lấy thông thông tin hiện tại của tài liệu
        /// </summary> 
        /// <param name="documentCode">Mã tài liệu</param>
        /// <param name="fileUrlExpireSeconds">Thời gian tồn tại của url file hợp đồng</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("get-documnet-info")]
        [ProducesResponseType(typeof(ResponseObject<DocumentSignedResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentInfo(string documentCode, int fileUrlExpireSeconds = 0)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOC_INFO);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOC_INFO;
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

                var result = await _signHandler.GetDocumentInfo(documentCode, fileUrlExpireSeconds, u.SystemLog);

                return result;
            });
        }
        #endregion

        /// <summary>
        /// Lấy url download hợp đồng
        /// </summary> 
        /// <param name="model">Model request</param>
        /// <returns>Đã gửi mã OTP</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-url-download-document-from-app")]
        [ProducesResponseType(typeof(ResponseObject<DocumentSignedResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestUrlDownloadDocumentFromApp([FromBody] RequestUrlDownloadDocumentFrom3rdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_URL_DOWNLOAD_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_URL_DOWNLOAD_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_MOBILE;

                model.UserId = new Guid(u.SystemLog.UserId);

                var result = await _handler.RequestUrlDownloadDocumentFrom3rd(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Lấy thông tin hợp đồng theo id
        /// </summary> 
        /// <param name="id">Id hợp đồng</param>
        /// <returns>Thông tin chi tiết hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<DocumentModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id, u.UserId, u.SystemLog);

                return result;
            });
        }

        [HttpGet, Route("get-document-wfl-history")]
        [ProducesResponseType(typeof(ResponseObject<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentWFLHistory(Guid docId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetDocumentWFLHistory(docId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách thông tin hợp đồng theo danh sách id
        /// </summary> 
        /// <param name="listId">Danh sách id hợp đồng</param>
        /// <returns>Thông tin chi tiết hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("get-many")]
        [ProducesResponseType(typeof(ResponseObject<DocumentModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByListId(List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetByListId(listId, u.UserId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách hợp đồng theo điều kiện lọc
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "textSearch": "",
        ///         "pageSize": 20,
        ///         "pageNumber": 1
        ///     }
        /// </remarks>
        /// <param name="filter">Điều kiện lọc</param>
        /// <returns>Danh sách hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] DocumentQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.CurrentUserId = u.UserId;
                filter.OrganizationId = u.OrganizationId;
                var result = await _handler.Filter(filter, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Xóa lô hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id lô hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Trình duyệt hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id hợp đồng</param>
        /// <returns>Danh sách kết quả trình duyệt</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("send-to-wf")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendToWorkflow([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REQUEST_APPROVE_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_REQUEST_APPROVE_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.SendToWorkflow(listId, u.Email, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Từ chối hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id hợp đồng</param>
        /// <returns>Danh sách kết quả từ chối</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("reject")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reject([FromBody] DocumentRejectModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_REJECT_SIGN_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_REJECT_SIGN_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Reject(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Duyệt hợp đồng
        /// </summary>
        /// <param name="listId"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("approve")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Approve(List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_LOT_APPROVE_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_LOT_APPROVE_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                //var result = await _handler.Approve(listId, u.SystemLog);
                var result = await _signHashHandler.Approve(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhập hợp đồng về trạng thái hủy
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("update-status-to-cancel")]
        [ProducesResponseType(typeof(ResponseObject<List<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(DocumentUpdateStatusModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CANCEL_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_CANCEL_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.UpdateStatus(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Cập nhập thời gian hết hạn ký
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("update-sign-expire")]
        [ProducesResponseType(typeof(ResponseObject<List<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSignExpireAtDate(DocumentUpdateSignExpireAtDateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_EXPIREDATE_DOCUMENT);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_EXPIREDATE_DOCUMENT;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.UpdateSignExpireAtDate(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Get user in workflow
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("get-user-in-flow-by-list-document")]
        [ProducesResponseType(typeof(ResponseObject<List<WorkflowUserModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserInFlowByListDocument(GetUserInWorkflowInListDocumentIdModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetUserInWorkflowByListDocument(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gửi mail
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="model">Danh sách Id hợp đồng</param>
        /// <returns>Trạng thái gửi mail</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("send-complete-mail")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMail([FromBody] DocumentSendMailModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.SendMail(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Gủi nofify đến user ký
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("send-email-to-usersign")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMailToUserSign(Guid? docId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.SendMailToUserSign(docId, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Gủi nofify đến user ký
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("send-email-to-usersign-with-config")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMailToUserSignWithConfig([FromBody] DocumentSendNotify model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.SendMailToUserSignWithConfig(model, u.SystemLog);

                return result;
            });
        }

        [HttpGet, Route("test")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeSendToWorkflowModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendNotify(Guid documentId, Guid workflowStepRemindNotifyId, Guid userId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.SendNotify(documentId, workflowStepRemindNotifyId, userId, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thực hiện quy trình ký
        ///// </summary>
        ///// <param name="model"></param>
        ///// <returns></returns>
        //[Authorize, HttpPost, Route("processing-workflow")]
        //[ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> ProcessingWorkflow(WorkflowDocumentProcessingModel model)
        //{
        //    string authHeader = Request.Headers["Authorization"];
        //    string accessToken = Guid.NewGuid().ToString();
        //    // Get the token
        //    var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
        //    accessToken = token;

        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        var result = await _handler.ProcessingWorkflow(model, u.UserId, u.SystemLog);


        //        return result;
        //    });
        //}

        [Authorize, HttpPost, Route("get-max-expired-after-day")]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMaxExpiredAfterDayByListDocumentId(List<Guid> docIds)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetMaxExpiredAfterDayByListDocumentId(docIds, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật file preview hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost, Route("update-document-file-preview")]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDocumentFilePreview(PdfCallBackResponseModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_UPDATE_DOCUMENT_FILE_PREVIEW);
                u.SystemLog.ActionName = LogConstants.ACTION_UPDATE_DOCUMENT_FILE_PREVIEW;
                u.SystemLog.Device = LogConstants.DEVICE_3RD;

                var result = await _handler.UpdateDocumentFilePreview(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách User Id kèm hợp đồng của User Id theo danh sách User Id
        /// </summary>
        /// <param name="userIds">Danh sách Id khách hàng</param>
        /// <returns>Danh sách User Id và hợp đồng của User</returns>
        [Authorize, HttpPost, Route("get-list-document-by-list-user")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentByListUserModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListDocumentByListUser([FromBody] List<Guid> userIds)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListDocumentByListUser(userIds, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Generate ảnh Preview từ hợp đồng
        /// </summary>
        /// <param name="listDocumentId"></param>
        /// <returns></returns>
        [Authorize, HttpPost, Route("generate-image-preview")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateImagePreview([FromBody] List<Guid> listDocumentId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _handler.GenerateImagePreview(listDocumentId, u.SystemLog);
            });
        }

        #region Gửi thông báo yêu cầu ký hợp đồng cho người dùng nội bộ
        /// <summary>
        /// Gửi thông báo yêu cầu ký hợp đồng cho người dùng nội bộ
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous, HttpPost, Route("push-notification-remind-sign-document-daily")]
        [ProducesResponseType(typeof(ResponseObject<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PushNotificationRemindSignDoucmentDaily()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _sysNotiHandler.PushNotificationRemindSignDocumentDaily(u.SystemLog);

                return result;
            });
        }
        #endregion
        
        [HttpPost, Route("update-document-everify")]
        public async Task<IActionResult> UpdateDocumentEVerify([FromBody] EVerifyDocumentRequest model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                return await _handler.UpdateDocumentEVerify(model);
            });
        }
    }
}
