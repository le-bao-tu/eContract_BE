using NetCore.Data;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class NotifyDocumentModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("documentTypeCode")]
        public string DocumentTypeCode { get; set; }
        [JsonPropertyName("documentCode")]
        public string DocumentCode { get; set; }
        //-	0: Tạo thành công
        //-	1: Đang thực hiện
        //-	2: Bị từ chối
        //-	3: Đã hoàn thành
        //-	500: Lỗi trong quá trình xử lý
        [JsonPropertyName("documentWorkflowStatus")]
        public DocumentStatus DocumentWorkflowStatus { get; set; }
        [JsonPropertyName("status")]
        public DocumentStatus Status
        {
            get
            {
                return DocumentWorkflowStatus;
            }
        }
        [JsonPropertyName("note")]
        public string Note { get; set; }
    }
    public class NotifyDocumentResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }

    #region GHTK
    public class GHTKAuthenResponseModel
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public GHTKAuthenResponseDataModel Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class GHTKAuthenResponseDataModel
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
    #endregion

    #region SNF
    public class SNFAuthenResponseModel
    {
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }

    public class SNFNotifyDocumentModel
    {
        [JsonPropertyName("documentCode")]
        public string DocumentCode { get; set; }
        //-	0: Tạo thành công
        //-	1: Đang thực hiện
        //-	2: Bị từ chối
        //-	3: Đã hoàn thành
        //-	500: Lỗi trong quá trình xử lý
        [JsonPropertyName("documentWorkflowStatus")]
        public DocumentStatus DocumentWorkflowStatus { get; set; }

        [JsonPropertyName("geoLocation")]
        public string GeoLocation { get; set; }
    }

    #endregion

    #region Notification gateway model
    public class NotificationRequestModel
    {
        public string TraceId { get; set; }
        /// <summary>
        /// Đơn vị yêu cầu gửi thông báo
        /// </summary>
        public string OraganizationCode { get; set; }
        //public UserModel User { get; set; }
        public NotificationData NotificationData { get; set; }
    }

    public class NotificationData
    {
        /// <summary>
        /// Loại gửi thông báo
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// Nội dung
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Tiêu đề
        /// </summary>
        public string Title { get; set; }

        ///// <summary>
        ///// Số điện thoại
        ///// </summary>
        //public string PhoneNumber { get; set; }

        public List<string> ListPhoneNumber { get; set; }

        /// <summary>
        /// Danh sách mail
        /// </summary>
        public List<string> ListEmail { get; set; }

        /// <summary>
        /// Danh sách token của device
        /// </summary>
        public List<string> ListToken { get; set; }

        /// <summary>
        /// Data gửi cho firebase
        /// </summary>
        public Dictionary<string, string> Data { get; set; }
    }

    public class NotificationType
    {
        public const string Email = "Email";
        public const string SMS = "SMS";
        public const string Firebase = "Firebase";
    }
    #endregion

    #region Notification remind sign document
    public class NotificationRemindSignDocumentModel
    {
        public string TraceId { get; set; }

        public string OraganizationCode { get; set; }

        public NotifyUserModel User { get; set; }

        public GatewayNotifyDocumentModel Document { get; set; }
    }
    #endregion

    #region Notification OTP change password
    public class NotifyChangePasswordModel
    {
        public NotifyUserModel User { get; set; }
        public string OTP { get; set; }
        public string OraganizationCode { get; set; }
        public string TraceId { get; set; }
    }
    #endregion

    public class NotifyUserModel
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public List<string> ListToken { get; set; }
    }

    public class GatewayNotifyDocumentModel
    {
        public string Code { get; set; }
        public string DocumentTypeCode { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string OneTimePassCode { get; set; }
    }

    public class NotificationConfigModel
    {
        public string TraceId { get; set; }
        public string OraganizationCode { get; set; }
        public bool? IsSendSMS { get; set; }
        public List<string> ListPhoneNumber { get; set; }
        public string SmsContent { get; set; }
        public bool? IsSendEmail { get; set; }
        public List<string> ListEmail { get; set; }
        public string EmailTitle { get; set; }
        public string EmailContent { get; set; }
        public bool? IsSendNotification { get; set; }
        public List<string> ListToken { get; set; }
        public string NotificationTitle { get; set; }
        public string NotificationContent { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public class NotificationSendOTPAuthUserModel
    {
        public string TraceId { get; set; }
        public string OraganizationCode { get; set; }
        public List<string> ListPhoneNumber { get; set; }
        public string SmsContent { get; set; }
        public List<string> ListEmail { get; set; }
        public string EmailTitle { get; set; }
        public string EmailContent { get; set; }
        public List<string> ListToken { get; set; }
        public string NotificationTitle { get; set; }
        public string NotificationContent { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public class NotificationRemindSignDocumentDailyModel
    {
        public string TraceId { get; set; }
        public string OraganizationCode { get; set; }
        public string UserFullName { get; set; }
        public List<string> ListPhoneNumber { get; set; }
        public List<string> ListEmail { get; set; }
        public List<string> ListToken { get; set; }
        public int NumberOfDocumentExpired { get; set; }
        public int NumberOfDocumentWaitMeSign { get; set; }
    }

    public class NotificationAutoSignFailModel
    {
        public string TraceId { get; set; }
        public Guid OraganizationRootId { get; set; }
        public string OraganizationCode { get; set; }
        public List<string> ListPhoneNumber { get; set; }
        public List<string> ListEmail { get; set; }
        public List<string> ListToken { get; set; }
        public string Document3rdId { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentName { get; set; }
    }
}
