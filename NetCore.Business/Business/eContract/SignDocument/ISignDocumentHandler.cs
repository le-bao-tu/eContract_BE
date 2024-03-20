using NetCore.DataLog;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface ký tài liệu
    /// </summary>
    public interface ISignDocumentHandler
    {

        /// <summary>
        /// Lấy thông thông tin tài liệu
        /// </summary>
        /// <param name="documentCode">Mã tài liệu</param>
        /// <param account="acc">Email/SĐT</param>
        /// <param name="otp">Mã OTP</param>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> GetDocumentByCode(string documentCode, string account, string otp, SystemLogModel sysLog);

        /// <summary>
        /// Từ chối ký tài liệu
        /// </summary>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> RejectDocument(RejectDocumentModel data, SystemLogModel sysLog);

        /// <summary>
        /// Lấy thông thông tin hiện tại của tài liệu
        /// </summary>
        /// <param name="code">Mã tài liệu</param>
        /// <param name="fileUrlExpireSeconds">Thời gian sống của url tải file hợp đồng</param>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> GetDocumentInfo(string code, int fileUrlExpireSeconds, SystemLogModel sysLog);

        /// <summary>
        /// Lấy thông thông tin hợp đồng theo lô hợp đồng
        /// </summary>
        /// <param name="documentBatchCode">Mã lô tài liệu</param>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> GetDocumentBatchInfo(string documentBatchCode, int fileUrlExpireSeconds, SystemLogModel sysLog);

        /// <summary>
        /// Lấy link truy cập hợp đồng
        /// </summary>
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> GetAccessLink(string documentCode, SystemLogModel sysLog);

        /// <summary>
        /// Gửi lại link truy cập cho người ký
        /// </summary>
        /// <param name="code">Mã tài liệu</param>
        /// <returns>Thông tin file tài liệu</returns>
        Task<Response> ResendAccessLink(string documentCode, SystemLogModel sysLog);

        /// <summary>
        /// Gửi mã OTP
        /// </summary>
        /// <param name="documentCode">Mã tài liệu</param>
        /// <returns></returns>
        Task<Response> SendOTP(string documentCode, SystemLogModel sysLog, bool isEmail = false);

        ///// <summary>
        ///// Ký bằng Chữ ký tay/Hình ảnh
        ///// </summary>
        ///// <param name="code">Mã tài liệu</param>
        ///// <param name="otp">Mã otp</param>
        ///// <param name="signatureBase64">chữ ký dưới dạng base 64</param>
        ///// <returns></returns>
        //Task<Response> SignDocumentDigital(string documentCode, string otp, string signatureBase64, SystemLogModel sysLog);

        ///// <summary>
        ///// Ký bằng Chữ ký tay/Hình ảnh (Ký nhiều)
        ///// </summary>
        ///// <param name="code">Mã tài liệu</param>
        ///// <param name="otp">Mã otp</param>
        ///// <param name="signatureBase64">chữ ký dưới dạng base 64</param>
        ///// <returns></returns>
        //Task<Response> SignMultileDocumentDigital(List<Guid> listDocumentId, string otp, string signatureBase64, SystemLogModel sysLog);

        ///// <summary>
        ///// Ký bằng Chữ ký tay/Hình ảnh cho đơn vị thứ 3 (Ký nhiều)
        ///// </summary>
        ///// <param name="userConnectId">Id người đang ký</param>
        ///// <param name="code">Mã tài liệu</param>
        ///// <param name="otp">Mã otp</param>
        ///// <param name="signatureBase64">chữ ký dưới dạng base 64</param>
        ///// <returns></returns>
        //Task<Response> SignMultileDocumentDigitalFor3rd(string userConnectId, List<string> listDocumentCode, string otp, string signatureBase64, SystemLogModel sysLog);

        /// <summary>
        /// Ký bằng Chữ ký tay/Hình ảnh cho đơn vị thứ 3 (Ký nhiều) v2
        /// </summary>
        /// <param name="model">Thông tin kye</param>
        /// <returns></returns>
        Task<Response> SignMultileDocumentDigitalFor3rdV2(SignDocumentMultileFor3rdModel model, SystemLogModel sysLog);

        /// <summary>
        /// Lấy OTP để ký hợp đồng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách mã hợp đồng</param>
        /// <param name="sysLog">Log hệ thống</param>
        /// <returns></returns>
        Task<Response> GetOTPDocumentFor3rd(List<string> listDocumentCode, SystemLogModel sysLog);

        /// <summary>
        /// Gửi OTP qua mail cho người dùng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách hợp đồng</param>
        /// <param name="sysLog">Nhật ký hệ thống</param>
        /// <returns></returns>
        Task<Response> SendMailOTPDocumentFor3rd(List<string> listDocumentCode, SystemLogModel sysLog);

        /// <summary>
        /// Gửi OTP qua SMS cho người dùng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách hợp đồng</param>
        /// <param name="sysLog">Nhật ký hệ thống</param>
        /// <returns></returns>
        Task<Response> SendSMSOTPDocumentFor3rd(List<string> listDocumentCode, SystemLogModel sysLog);

        /// <summary>
        /// Gửi OTP qua mail cho người dùng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách hợp đồng</param>
        /// <param name="sysLog">Nhật ký hệ thống</param>
        /// <returns></returns>
        Task<Response> SendMailOTPUserFor3rd(OTPUserRequestModel model, SystemLogModel sysLog);

        /// <summary>
        /// Gửi OTP qua SMS cho người dùng
        /// </summary>
        /// <param name="listDocumentCode">Danh sách hợp đồng</param>
        /// <param name="sysLog">Nhật ký hệ thống</param>
        /// <returns></returns>
        Task<Response> SendSMSOTPUserFor3rd(OTPUserRequestModel model, SystemLogModel sysLog);

        ///// <summary>
        ///// Ký HSM
        ///// </summary>
        ///// <param name="documentCode">Mã tài liệu</param>
        ///// <param name="otp">Mã otp</param>
        ///// <param name="data">Thông tin tài khoản để ký HSM</param>
        ///// <returns></returns>
        //Task<Response> SignDocumentHSM(string documentCode, string userPin, string fileBase64, SystemLogModel sysLog);

        ///// <summary>
        ///// Ký USB token
        ///// </summary>
        ///// <param name="documentCode">Mã tài liệu</param>
        ///// <param name="otp">Mã otp</param>
        ///// <param name="fileBase64">file tài liệu đã ký dưới dạng Base64</param>
        ///// <returns></returns>
        //Task<Response> SignDocumentUsbToken(string documentCode, string fileBase64, SystemLogModel sysLog);

        ///// <summary>
        ///// Ký USB token (Ký nhiều)
        ///// </summary>
        ///// <param name="documentCode">Mã tài liệu</param>
        ///// <param name="otp">Mã otp</param>
        ///// <param name="fileBase64">file tài liệu đã ký dưới dạng Base64</param>
        ///// <returns></returns>
        //Task<Response> SignMultileDocumentUsbToken(List<SignDocumentUsbTokenDataModel> model, SystemLogModel sysLog);

        /// <summary>
        /// Lấy tọa độ vùng ký
        /// </summary>
        /// <param name="documentId">Id tài liệu</param>
        /// <returns>Tọa độ vùng ký của tài liệu</returns>
        Task<Response> GetCoordinateFile(Guid documentId, SystemLogModel sysLog = null);

        /// <summary>
        /// Lấy danh sách tọa độ vùng ký dựa vào documentId
        /// </summary>
        /// <param name="listDocumentId">Danh sách Id tài liệu</param>
        /// <returns>Danh sách tọa độ vùng ký của từng tài liệu</returns>
        Task<Response> GetListCoordinate(List<Guid> listDocumentId, SystemLogModel sysLog = null);
    }
}
