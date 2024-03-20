using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ISignHashHandler
    {

        Task<Response> HashFiles(NetHashFilesRequestModel model, SystemLogModel systemLog, Guid userId);
        Task<Response> AttachFiles(NetAttachFileModel model, SystemLogModel systemLog, Guid userId);
        Task<Response> SignHSMFiles(SignHSMClientModel model, SystemLogModel systemLog, Guid userId);
        Task<Response> ElectronicSignFiles(ElectronicSignClientModel model, SystemLogModel systemLog, Guid userId, bool isFromSignPage = false);
        Task<Response> AutomaticSignDocument(List<string> listDocumentCode, SystemLogModel systemLog);
        Task<Response> SignADSSFiles(SignADSSClientModel model, SystemLogModel systemLog, Guid userId);
        Task<Response> RejectDocumentFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog);
        Task<Response> DeleteDocumentFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog);
        Task<Response> ConfirmSignDocumentFromESign(SignConfirmModel model, SystemLogModel systemLog);

        //bool ValidateJWTToken(string token);

        /// <summary>
        /// Demo luồng ký
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> SADConfirmSignDocumentFromApp(SADReqeustSignConfirmModel model, SystemLogModel systemLog);

        #region API cung cấp cho web app
        /// <summary>
        /// Xác nhận eForm
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> ConfirmEFormFromWebApp(ConfirmEformFromWebAppModel model, SystemLogModel systemLog);

        /// <summary>
        /// Ký hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> SignDocumentFromWebApp(SignDocumentFromWebAppModel model, SystemLogModel systemLog);

        Task<Response> RejectDocumentFromWebApp(RejectDocumentFromWebAppModel model, SystemLogModel systemLog);

        Task<Response> UploadFileSignedFromWebApp(UploadSignedDocumentFromWebAppModel model, SystemLogModel systemLog);
        #endregion

        #region Tương tác với ứng dụng bên thứ 3
        /// <summary>
        /// Ký eForm từ ứng dụng khác
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> SignEFormFrom3rd(SignEFormFrom3rdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Yêu cầu ký hợp đồng từ bên thứ 3
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> RequestSignDocumentFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog);

        Task<Response> RequestSignDocumentVkeyFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Làm mới OTP thực hiện ký hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<NetCore.Shared.Response> RenewOTPFromRequestId(RequestOTPFromRequestId model, SystemLogModel systemLog);

        /// <summary>
        /// Từ chối
        /// </summary>
        /// <param name="model">Danh sách Id hợp đồng</param>
        /// <returns>Danh sách kết quả từ chối</returns>
        Task<Response> RejectFrom3rd(DocumentApproveRejectFrom3rdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Duyệt 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> ApproveFrom3rd(DocumentApproveRejectFrom3rdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy mới OTP 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> RenewOTPFrom3rd(RenewOTPReuquestFrom3rdModel model, SystemLogModel systemLog);
        #endregion

        /// <summary>
        /// Kiểm tra workflow có ký tự động HSM và ADSS
        /// </summary>
        /// <param name="listDocumentId"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task CheckAutomaticSign(List<Guid> listDocumentId, SystemLogModel systemLog);

        /// <summary>
        /// API phê duyệt trên web
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> Approve(List<Guid> listId, SystemLogModel systemLog);

        Task<Response> HashFilesFromSinglePage(NetHashFromSinglePageModel model, SystemLogModel systemLog, Guid userId);
        Task<Response> AttachFilesFromSinglePage(NetAttachFromSinglePageModel model, SystemLogModel systemLog, Guid userId);
        
        Task SendDocumentEverifyToQueue();
        Task RequestEverify(EVerifyRequestModel model, string traceId);

    }
}
