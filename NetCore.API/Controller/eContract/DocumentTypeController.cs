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

namespace NetCore.API
{
    /// <summary>
    /// Module loại hợp đồng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/document-type")]
    [ApiExplorerSettings(GroupName = "eContract - 02. Document Type (Loại hợp đồng)")]
    public class DocumentTypeController : ApiControllerBase
    {
        private readonly IDocumentTypeHandler _handler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        public DocumentTypeController(IDocumentTypeHandler handler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _orgConfigHandler = orgConfigHandler;
        }

        /// <summary>
        /// Thêm mới loại hợp đồng
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }
        /// </remarks>
        /// <param name="model">Thông tin loại hợp đồng</param>
        /// <returns>Id loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] DocumentTypeCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TYPE_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TYPE_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model,u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Thêm mới loại hợp đồng theo danh sách
        ///// </summary>
        ///// <remarks>
        ///// Sample request:
        /////
        /////     [
        /////         {
        /////             "code": "Code",
        /////             "name": "Name",
        /////             "status": true,
        /////             "description": "Description",
        /////             "order": 1
        /////         }   
        /////     ]
        ///// </remarks>
        ///// <param name="list">Danh sách thông tin loại hợp đồng</param>
        ///// <returns>Danh sách kết quả thêm mới</returns> 
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("create-many")]
        //[ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> CreateMany([FromBody]List<DocumentTypeCreateModel> list)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        foreach (var item in list)
        //        {
        //            item.CreatedUserId = u.UserId;
        //            item.ApplicationId = u.ApplicationId;
        //        }
        //        var result = await _handler.CreateMany(list);
        //        return result;
        //    });
        //}
        /// <summary>
        /// Cập nhật loại hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     {
        ///         "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///         "code": "Code",
        ///         "name": "Name",
        ///         "status": true,
        ///         "description": "Description",
        ///         "order": 1
        ///     }   
        /// </remarks>
        /// <param name="model">Thông tin loại hợp đồng cần cập nhật</param>
        /// <returns>Id loại hợp đồng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] DocumentTypeUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TYPE_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TYPE_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin loại hợp đồng theo id
        /// </summary> 
        /// <param name="id">Id loại hợp đồng</param>
        /// <returns>Thông tin chi tiết loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<DocumentTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách loại hợp đồng theo điều kiện lọc
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
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentTypeBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] DocumentTypeQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                filter.CurrentUserId = u.UserId;
                var result = await _handler.Filter(filter);

                return result;
            });
        }

        /// <summary>
        /// Lấy tất cả danh sách loại hợp đồng
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentTypeBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                DocumentTypeQueryFilter filter = new DocumentTypeQueryFilter()
                {
                    TextSearch = ts,
                    PageNumber = null,
                    PageSize = null
                };
                filter.CurrentUserId = u.UserId;
                var result = await _handler.Filter(filter);

                return result;
            });
        }
        /// <summary>
        /// Xóa loại hợp đồng
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id loại hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TYPE_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TYPE_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách loại hợp đồng cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(count, ts, u.OrganizationId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách loại hợp đồng cho combobox bao gồm cả đơn chấp thuận điện tử an toàn
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all-for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetAllListCombobox(count, ts, u.OrganizationId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách loại hợp đồng cho combobox bao gồm cả trạng thái inactive
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox-all-status")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListComboboxAllStatus(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListComboboxAllStatus(count, ts, u.OrganizationId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách loại hợp đồng cho đơn vị kết nối
        /// </summary> 
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("for-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListFor3rd()
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_DOCTYPE);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_DOCTYPE;
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

                //if (u.OrganizationId == null || u.OrganizationId == Guid.Empty)
                //{
                //    return new ResponseError(Code.Forbidden, "X-OrganizationId không hợp lệ");
                //}
                #endregion

                u.SystemLog.ListAction.Add(new ActionDetail("Lấy danh sách loại hợp đồng theo đơn vị"));

                var result = await _handler.GetListComboboxFor3rd(0, "", u.OrganizationId);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách meta data theo loại hợp đồng cho đơn vị kết nối
        /// </summary> 
        /// <param name="documentTypeCode">Mã loại hợp đồng</param>
        /// <returns>Danh sách loại hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [AllowAnonymous, HttpGet, Route("meta-data-by-code-for-3rd")]
        [ProducesResponseType(typeof(ResponseObject<List<MetaDataListForDocumentType>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListMetaDataByDocumentType(string documentTypeCode)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_GET_METADATA);
                u.SystemLog.ActionName = LogConstants.ACTION_GET_METADATA;
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

                //if (u.OrganizationId == null || u.OrganizationId == Guid.Empty)
                //{
                //    return new ResponseError(Code.Forbidden, "X-OrganizationId không hợp lệ");
                //}
                #endregion

                u.SystemLog.ListAction.Add(new ActionDetail($"Lấy danh sách Metadata theo loại hợp đồng {documentTypeCode}"));

                var result = await _handler.GetListMetaDataByDocumentType(documentTypeCode, u.OrganizationId);

                return result;
            });
        }
    }
}
