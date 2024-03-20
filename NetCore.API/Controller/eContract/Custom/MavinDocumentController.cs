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

namespace NetCore.API
{
    /// <summary>
    /// Module hợp đồng - Mavin
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/mavin/document")]
    [ApiExplorerSettings(GroupName = "eContract - 08. Document (hợp đồng) - Custom for Mavin Group", IgnoreApi = true)]
    public class MavinDocumentController : ApiControllerBase
    {
        private readonly IDocumentHandler _handler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        public MavinDocumentController(IDocumentHandler handler, IOrganizationConfigHandler orgConfigHandler, ISystemLogHandler logHandler) : base(logHandler)
        {
            _handler = handler;
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

                #region Cập nhật lại thông tin quy trình theo đơn vị kết nối

                foreach (var item in model.ListDocument)
                {
                    if (item.ListMetaData != null)
                    {
                        var orgMapCode = item.ListMetaData.Where(x => x.MetaDataCode == "ORG_MAP_CODE").Select(x => x.MetaDataValue).FirstOrDefault();

                        if (!string.IsNullOrEmpty(orgMapCode))
                        {
                            item.WorkFlowUser.Add("mavin.vanthu." + orgMapCode.ToLower());
                        }
                    }
                }

                #endregion

                var result = await _handler.CreateMetaDataMany3rd_iText7(model, u.SystemLog);

                return result;
            });
        }

        #endregion

    }
}
