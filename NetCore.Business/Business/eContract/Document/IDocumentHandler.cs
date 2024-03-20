using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý hợp đồng
    /// </summary>
    public interface IDocumentHandler
    {
        #region Kết nối từ ứng dụng bên thứ 3
        /// <summary>
        /// Tạo mới nhiều documnet sử dụng file PDF
        /// </summary>
        /// <param name="model">Model tạo mới document</param>
        /// <returns>Danh sách Trạng thái tạo mới, link ký Document</returns>
        Task<Response> CreatePDFMany3rd(DocumentCreatePDFManyModel model, bool isDocx, SystemLogModel systemLog);   

        /// <summary>
        /// Tạo mới nhiều documnet sử dụng MetaData
        /// </summary>
        /// <param name="model">Model tạo mới document</param>
        /// <returns>Danh sách Trạng thái tạo mới, link ký Document</returns>
        Task<Response> CreateMetaDataMany3rd_iText7(DocumentCreateMetaDataManyModel model, SystemLogModel systemLog);

        ///// <summary>
        ///// Tạo mới nhiều documnet sử dụng MetaData
        ///// </summary>
        ///// <param name="model">Model tạo mới document</param>
        ///// <returns>Danh sách Trạng thái tạo mới, link ký Document</returns>
        //Task<Response> CreateMetaDataMany3rd_SpirePdf(DocumentCreateMetaDataManyModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách hợp đồng theo connectId khách hàng 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetListDocumentByUserConnectId(DocumentRequestByUserConnectIdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Tạo hợp đồng eForm từ ứng dụng bên thứ 3
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> CreateEFormFrom3rd_v2(CreateEFormFrom3rdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Hàm dùng chung để fill meta data vào Pdf
        /// </summary>
        /// <param name="streamWriter"></param>
        /// <param name="listMetaDataValue"></param>
        /// <param name="fileTemplateStream"></param>
        /// <param name="systemLog"></param>
        /// <returns>File filled</returns>
        Task<MemoryStream> FillMetaDataToPdfWithIText7(List<MetaDataFileValue> listMetaDataValue, MemoryStream fileTemplateStream, SystemLogModel systemLog);

        /// <summary>
        /// Hàm lấy thông tin file template
        /// </summary>
        /// <param name="listDocumentTemplate"></param>        
        /// <param name="listFileTemplate"></param>
        /// <param name="systemLog"></param>
        /// <returns>List File template</returns>
        Task<List<FileTemplateStreamModel>> GetFileTemplateDocument(List<DocumentTemplate> listDocumentTemplate, List<DocumentFileTemplate> listFileTemplate, SystemLogModel systemLog);

        /// <summary>
        /// Lấy link download hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> RequestUrlDownloadDocumentFrom3rd(RequestUrlDownloadDocumentFrom3rdModel model, SystemLogModel systemLog);


        Task<Response> GetLatestDocumentUser(GetLatestDocumentUserFrom3rdModel model, SystemLogModel systemLog);

        Task<Response> GetDoumentDetailByCodeFrom3rd(GetDoumentDetailByCodeFrom3rdModel model, SystemLogModel systemLog);

        Task<Response> GetListDocumentFrom3rd(DocumentQueryFilterMobileApp model, SystemLogModel systemLog);

        #endregion

        #region API cung cấp cho web app

        /// <summary>
        /// Lấy thông tin hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetDocumentFromWebApp(GetDocumentFromWebAppModel model, SystemLogModel systemLog);

        /// <summary>
        /// Gửi lại mã truy cập hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> ResendEmailPassCodeWebApp(GetDocumentFromWebAppModel model, SystemLogModel systemLog);

        /// <summary>
        /// Gửi lại OTP để thực hiện ký hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> SendOTPSignDocumentFromWebApp(ResendOTPSignDocumentFromWebAppModel model, SystemLogModel systemLog);


        #endregion

        /// <summary>
        /// Lấy danh sách hợp đồng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách thông tin trong hợp đồng</returns>
        Task<Response> Filter(DocumentQueryFilter filter, SystemLogModel systemLog);

        Task<Response> GetById(Guid id, Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách thông tin hợp đồng theo danh sách Id
        /// </summary>
        /// <param name="id">Id thông tin trong hợp đồng</param>
        /// <param name="userId">Id người dùng đang đăng nhập</param>
        /// <returns>Thông tin thông tin trong hợp đồng</returns>
        Task<List<Data.Document>> InternalGetDocumentByListId(List<Guid> listId, SystemLogModel systemLog);

        Task<List<Data.Document>> InternalGetDocumentByListCode(List<string> listCode, SystemLogModel systemLog);

        /// <summary>
        /// Lấy thông tin hợp đồng theo Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Data.Document> InternalGetDocumentById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy thông tin trong hợp đồng theo Id
        /// </summary>
        /// <param name="listId">Danh sách id thông tin trong hợp đồng</param>
        /// <param name="userId">Id người dùng đang đăng nhập</param>
        /// <returns>Thông tin thông tin trong hợp đồng</returns>
        Task<Response> GetByListId(List<Guid> listId, Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Xóa hợp đồng
        /// </summary>
        /// <param name="listId">Danh sách Id hợp đồng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Trình duyệt
        /// </summary>
        /// <param name="listId">Danh sách Id hợp đồng</param>
        /// <param name="currentEmail">Email người đăng nhập</param>
        /// <returns>Danh sách kết quả trình duyệt</returns>
        Task<Response> SendToWorkflow(List<Guid> listId, string currentEmail, SystemLogModel systemLog);

        /// <summary>
        /// Từ chối
        /// </summary>
        /// <param name="listId">Danh sách Id hợp đồng</param>
        /// <returns>Danh sách kết quả từ chối</returns>
        Task<Response> Reject(DocumentRejectModel model, SystemLogModel systemLog);

        /// <summary>
        /// Duyệt 
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> Approve(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhập hợp đồng về trạng thái hủy
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        public Task<Response> UpdateStatus(DocumentUpdateStatusModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhập ngày hết hạn hợp đồng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> UpdateSignExpireAtDate(DocumentUpdateSignExpireAtDateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Get user in workflow by list document
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetUserInWorkflowByListDocument(GetUserInWorkflowInListDocumentIdModel model, SystemLogModel systemLog);

        /// <summary>
        /// Gửi mail
        /// </summary>
        /// <param name="model">Model gửi mail</param>
        /// <returns>Kết quả gửi mail</returns>
        Task<Response> SendMail(DocumentSendMailModel model, SystemLogModel systemLog);

        /// <summary>
        /// Gửi mail tới người ký
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> SendMailToUserSign(Guid? docId, SystemLogModel systemLog);

        /// <summary>
        /// Gửi notify thông báo nhắc nhở
        /// </summary>
        /// <param name="documentId"></param>
        /// <param name="WorkflowStepNotifyId"></param>
        /// <returns></returns>
        Task<Response> SendNotify(Guid documentId, Guid WorkflowStepNotifyId, Guid userId, SystemLogModel systemLog);


        ///// <summary>
        ///// Thực hiện quy trình ký
        ///// </summary>
        ///// <param name="model"></param>
        ///// <returns></returns>
        //Task<Response> ProcessingWorkflow(WorkflowDocumentProcessingModel model, Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy thông thông tin hiện tại của tài liệu
        /// </summary>
        /// <param name="code">Mã tài liệu</param>
        /// <param name="fileUrlExpireSeconds">Thời gian sống của url tải file hợp đồng</param>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> GetDocumentInfo(string code, int fileUrlExpireSeconds, SystemLogModel sysLog);

        /// <summary>
        /// Tạo lịch sử khi hợp đồng thay đổi
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        Task CreateDocumentWFLHistory(NetCore.Data.Document document, MemoryStream memoryStream = null);

        /// <summary>
        /// Lấy lịch sử thay đổi của hợp đồng
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        Task<Response> GetDocumentWFLHistory(Guid docId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy ngày nhắc ký lớn nhất theo list id hợp đồng
        /// </summary>
        /// <param name="docIds"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetMaxExpiredAfterDayByListDocumentId(List<Guid> docIds, SystemLogModel systemLog);

        Task<Response> UpdateDocumentFilePreview(PdfCallBackResponseModel model, SystemLogModel systemLog);

        Task<Response> SendMailToUserSignWithConfig(DocumentSendNotify model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách documentId, documentCode by userId
        /// </summary>
        /// <param name="userIds">Danh sách User Id</param>
        /// <param name="systemLog"></param>
        /// <returns>Danh sách User Id kèm hợp đồng của User</returns>
        Task<Response> GetListDocumentByListUser(List<Guid> userIds, SystemLogModel systemLog);

        /// <summary>
        /// Tạo lại ảnh preview từ list document id
        /// </summary>
        /// <param name="listDocumentId">List document Id</param>
        /// <returns></returns>
        Task<Response> GenerateImagePreview(List<Guid> listDocumentId, SystemLogModel systemLog);

        Task<Response> UpdateDocumentEVerify(EVerifyDocumentRequest model);
    }
}
