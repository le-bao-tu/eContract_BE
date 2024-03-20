using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetCore.Business;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.API
{
    /// <summary>
    /// Module biểu mẫu
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/document-template")]
    [ApiExplorerSettings(GroupName = "eContract - 04. Document Template (Biểu mẫu)")]
    public class DocumentTemplateController : ApiControllerBase
    {
        private readonly IDocumentTemplateHandler _handler;
        public DocumentTemplateController(IDocumentTemplateHandler handler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Thêm mới biểu mẫu
        /// </summary>
        /// <param name="model">Thông tin biểu mẫu</param>
        /// <returns>Id biểu mẫu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] DocumentTemplateCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TEMPLATE_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TEMPLATE_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }


        /// <summary>
        /// Nhân bản 
        /// </summary>
        /// <param name="model">Thông tin biểu mẫu cần nhân bản</param>
        /// <returns>Id biểu mẫu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("duplicate")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Duplicate([FromBody] DocumentTemplateDuplicateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TEMPLATE_DUPLICATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TEMPLATE_DUPLICATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Duplicate(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật biểu mẫu
        /// </summary> 
        /// <param name="model">Thông tin biểu mẫu cần cập nhật</param>
        /// <returns>Id biểu mẫu đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] DocumentTemplateUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TEMPLATE_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TEMPLATE_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật cấu hình biểu mẫu hợp đồng
        /// </summary>
        /// <param name="list">Danh sách file cấu hình biểu mẫu</param>
        /// <returns>Trạng thái thành công/thất bại</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("update-meta-data-config")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMetaData([FromBody] List<DocumentFileTemplateModel> list)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TEMPLATE_METADATA_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TEMPLATE_METADATA_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.UpdateMetaData(list, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin biểu mẫu theo id
        /// </summary> 
        /// <param name="id">Id biểu mẫu</param>
        /// <returns>Thông tin chi tiết biểu mẫu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<DocumentTemplateModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách biểu mẫu hợp đồng theo loại hợp đồng
        /// </summary> 
        /// <param name="id">Id loại hợp đồng</param>
        /// <returns>Danh sách biểu mẫu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("get-by-type")]
        [ProducesResponseType(typeof(ResponseObject<DocumentTemplateModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListTemplateByTypeId(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListTemplateByTypeId(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách biểu mẫu theo điều kiện lọc
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
        /// <returns>Danh sách biểu mẫu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentTemplateBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] DocumentTemplateQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                filter.OrganizationId = u.OrganizationId;
                filter.CurrentUserId = u.UserId;
                var result = await _handler.Filter(filter);

                return result;
            });
        }

        [Authorize, HttpPost, Route("get-document-template-by-group-code")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentTemplateBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListDocumentTemplateByGroupCode([FromBody] DocumentByGroupCodeModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                model.CurrentUserId = u.UserId;
                var result = await _handler.GetListDocumentTemplateByGroupCode(model);

                return result;
            });
        }

        /// <summary>
        /// Lấy tất cả danh sách biểu mẫu
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách biểu mẫu</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentTemplateBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                DocumentTemplateQueryFilter filter = new DocumentTemplateQueryFilter()
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
        /// Xóa biểu mẫu
        /// </summary> 
        /// <remarks>
        /// Sample request:
        ///
        ///     [
        ///         "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        ///     ]
        /// </remarks>
        /// <param name="listId">Danh sách Id biểu mẫu</param>
        /// <returns>Danh sách kết quả xóa</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpDelete, Route("")]
        [ProducesResponseType(typeof(ResponseObject<List<ResponeDeleteModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromBody] List<Guid> listId)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOCUMENT_TEMPLATE_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOCUMENT_TEMPLATE_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách biểu mẫu cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách biểu mẫu</returns> 
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
    }
}
