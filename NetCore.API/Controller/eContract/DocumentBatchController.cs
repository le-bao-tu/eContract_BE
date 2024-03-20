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
    /// Module lô hợp đồng
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/document-batch")]
    [ApiExplorerSettings(GroupName = "eContract - 07. Document Batch (Lô hợp đồng)")]
    public class DocumentBatchController : ApiControllerBase
    {
        private readonly IDocumentBatchHandler _handler;
        private readonly ISignServiceHandler _signHandler;
        public DocumentBatchController(IDocumentBatchHandler handler, ISignServiceHandler signHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
            _signHandler = signHandler;
        }

        /// <summary>
        /// Thêm mới lô hợp đồng
        /// </summary>
        /// <param name="model">Thông tin lô hợp đồng</param>
        /// <returns>Id lô hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] DocumentBatchCreateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOC_BATCH_CREATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOC_BATCH_CREATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.ApplicationId = u.ApplicationId;
                model.OrganizationId = u.OrganizationId;
                var result = await _handler.Create(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Cập nhật lô hợp đồng
        /// </summary>
        /// <param name="model">Thông tin lô hợp đồng cần cập nhật</param>
        /// <returns>Id lô hợp đồng đã cập nhật thành công</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPut, Route("")]
        [ProducesResponseType(typeof(ResponseObject<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] DocumentBatchUpdateModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOC_BATCH_UPDATE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOC_BATCH_UPDATE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.ModifiedUserId = u.UserId;
                var result = await _handler.Update(model, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy thông tin lô hợp đồng theo id
        /// </summary> 
        /// <param name="id">Id lô hợp đồng</param>
        /// <returns>Thông tin chi tiết lô hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("")]
        [ProducesResponseType(typeof(ResponseObject<DocumentBatchModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetById(id);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách lô hợp đồng theo điều kiện lọc
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
        /// <returns>Danh sách lô hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("filter")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBatchBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] DocumentBatchQueryFilter filter)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.Filter(filter);

                return result;
            });
        }
        /// <summary>
        /// Lấy tất cả danh sách lô hợp đồng
        /// </summary> 
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách lô hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("all")]
        [ProducesResponseType(typeof(ResponseObject<List<DocumentBatchBaseModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(string ts = null)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                DocumentBatchQueryFilter filter = new DocumentBatchQueryFilter()
                {
                    TextSearch = ts,
                    PageNumber = null,
                    PageSize = null
                };
                var result = await _handler.Filter(filter);

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
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_DOC_BATCH_DELETE);
                u.SystemLog.ActionName = LogConstants.ACTION_DOC_BATCH_DELETE;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                var result = await _handler.Delete(listId, u.SystemLog);

                return result;
            });
        }

        /// <summary>
        /// Lấy danh sách lô hợp đồng cho combobox
        /// </summary> 
        /// <param name="count">số bản ghi tối đa</param>
        /// <param name="ts">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách lô hợp đồng</returns> 
        /// <response code="200">Thành công</response>
        [Authorize, HttpGet, Route("for-combobox")]
        [ProducesResponseType(typeof(ResponseObject<List<SelectItemModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListCombobox(int count = 0, string ts = "")
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = await _handler.GetListCombobox(count, ts);

                return result;
            });
        }

        /// <summary>
        /// Sinh danh sách hợp đồng
        /// </summary>
        /// <param name="model">Model tạo hợp đồng</param>
        /// <returns>Trạng thái thành công/thất bại</returns>
        /// <response code="200">Thành công</response>
        [Authorize, HttpPost, Route("generate-certificate")]
        [ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateListCertificate(DocumentBatchGenerateFileModel model)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                u.SystemLog.ActionCode = nameof(LogConstants.ACTION_CREATE_DOC);
                u.SystemLog.ActionName = LogConstants.ACTION_CREATE_DOC;
                u.SystemLog.Device = LogConstants.DEVICE_WEB;

                model.CreatedUserId = u.UserId;
                model.OrganizationId = u.OrganizationId;
                model.ApplicationId = u.ApplicationId;
                var result = await _handler.GenerateListDocument_v2(model, u.SystemLog);

                return result;
            });
        }

        ///// <summary>
        ///// Ký file nè
        ///// </summary>
        ///// <param name="model">Model tạo hợp đồng</param>
        ///// <returns>Trạng thái thành công/thất bại</returns>
        ///// <response code="200">Thành công</response>
        //[Authorize, HttpPost, Route("test-sign-file")]
        //[ProducesResponseType(typeof(ResponseObject<bool>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> SignPDFwithSigningBox(DataInputSignPDF model)
        //{
        //    return await ExecuteFunction(async (RequestUser u) =>
        //    {
        //        var result = await _signHandler.SignBySigningBox(model, u.UserId);

        //        return result;
        //    });
        //}

    }
}
