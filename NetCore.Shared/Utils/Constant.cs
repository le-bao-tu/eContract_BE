using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetCore.Shared
{
    #region Systems

    public class AppConstants
    {
        //public static bool SignExprireDate = true;
        public static string EnvironmentName = "production";
        public static Guid RootAppId => new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid TestAppId => new Guid("00000000-0000-0000-0000-000000000002");
    }

    public class UserConstants
    {
        public static Guid AdministratorId => new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid UserId => new Guid("00000000-0000-0000-0000-000000000002");
    }

    public class RoleConstants
    {
        public static Guid AdministratorId => new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid UserId => new Guid("00000000-0000-0000-0000-000000000002");
        public static Guid GuestId => new Guid("00000000-0000-0000-0000-000000000002");
        public static Guid CustomerId => new Guid("00000000-0000-0000-0000-000000000003");
    }

    public class RightConstants
    {
        public static Guid AccessAppId => new Guid("00000000-0000-0000-0000-000000000001");
        public static string AccessAppCode = "TRUY_CAP_HE_THONG";

        public static Guid DefaultAppId => new Guid("00000000-0000-0000-0000-000000000002");
        public static string DefaultAppCode = "TRUY_CAP_MAC_DINH";

        public static Guid FileAdministratorId => new Guid("00000000-0000-0000-0000-000000000003");
        public static string FileAdministratorCode = "QUAN_TRI_FILE";

        public static Guid PemissionId => new Guid("00000000-0000-0000-0000-000000000004");
        public static string PemissionCode = "PHAN_QUYEN";
    }

    public static class WFConstants
    {
        public const string NEW = "SCHEDULE";
        public const string INPROCESS = "SCHEDULE";
        public const string COMPLETE = "SCHEDULE";
        public const string REJECT = "SCHEDULE";

        public const string SIGNRURL = "notification/wf-process";
    }

    public static class NameConstants
    {
        public static string GetPropName(string name)
        {
            switch (name)
            {
                case "Code":
                    return "Mã";

                default:
                    return null;
            }
        }

        public static List<string> GetPropName(List<string> names)
        {
            var result = new List<string>();
            foreach (var name in names)
            {
                result.Add(GetPropName(name));
            }
            return result;
        }
    }

    public class SelectListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }

    public class SelectItemModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
    }


    public class MessageConstants
    {
        public static string ErrorLogMessage = "An error occurred: ";
        public static string CreateSuccessMessage = "Thêm mới thành công";
        public static string CreateErrorMessage = "Thêm mới thất bại";
        public static string UpdateSuccessMessage = "Cập nhật thành công";
        public static string UpdateErrorMessage = "Cập nhật thất bại";
        public static string DeleteSuccessMessage = "Kết quả xóa";
        public static string DeleteErrorMessage = "Xóa thất bại";
        public static string DeleteItemSuccessMessage = "Xóa thành công";
        public static string DeleteItemErrorMessage = "Xóa không thành công";
        public static string DeleteItemNotFoundMessage = "Không tìm thấy đối tượng";
        public static string GetDataSuccessMessage = "Tải dữ liệu thành công";
        public static string GetDataErrorMessage = "Tải dữ liệu thất bại";
        public static string SignSuccess = "Ký thành công";
    }

    public class ClaimConstants
    {
        public const string USER_ID = "x-user-id";
        public const string USER_EMAIL = "x-user-email";
        public const string USER_NAME = "x-user-name";
        public const string FULL_NAME = "x-full-name";
        public const string AVATAR = "x-avatar";
        public const string APP_ID = "x-app-id";
        public const string ORG_ID = "x-org-id";
        public const string APPS = "x-app";
        public const string ROLES = "x-role";
        public const string RIGHTS = "x-right";
        public const string ISSUED_AT = "x-iat";
        public const string EXPIRES_AT = "x-exp";
        public const string CHANNEL = "x-channel";
        public const string DOCUMENT_ID = "x-document-id";
        public const string REQUEST_ID = "x-request-id";
        public const string SAD_REQUEST_ID = "x-sad-id";
    }

    #endregion

    #region Business

    public class CacheConstants
    {
        public const string COUNTRY = "Country";
        public const string DISTRICT = "District";
        public const string POSITION = "Position";
        public const string PROVINCE = "Province";
        public const string WARD = "Ward";

        public const string DOCUMENT = "Document";
        public const string DOCUMENT_BATCH = "DocumentBatch";
        public const string DOCUMENT_TEMPLATE = "DocumentTemplate";
        public const string DOCUMENT_TYPE = "DocumentType";
        public const string META_DATA = "MetaData";

        public const string ORG_CONFIG = "OrganizationConfig";
        public const string USER_SIGN_CONFIG = "UserSignConfig";
        public const string USER_HSM_ACCOUNT = "UserHSMAccount";
        public const string USER_ROLE = "UserRole";

        public const string EMAIL_ACCOUNT = "EmailAccount";

        public const string ORGANIZATION = "Organization";
        public const string ORGANIZATION_TYPE = "OrganizationType";
        public const string USER = "User";
        public const string TOTP = "TOTP";
        public const string SYS_APP = "SystemApplication";

        public const string CONTACT = "Contact";
        public const string WORKFLOW = "Workflow";
        public const string WORKFLOW_DOCUMENT = "WorkflowDocument";
        public const string WORKFLOW_STATE = "WorkflowState";
        public const string NOTIFYCONFIG = "NotifyConfig";

        public const string LIST_SELECT = "list-select";

        public const string WSO2_ACCESSTOKEN = "WSO2-ACCESSTOKEN";
        public const string KEYCLOAK_ACCESSTOKEN = "KEYCLOAK-ACCESSTOKEN";

        public const string ROLE = "Role";
        public const string NAVIGATION = "Navigation";
        public const string RIGHT = "Right";
    }

    public class LogConstants
    {
        #region Khởi tạo dữ liệu
        public const string ACTION_SEED_DATA = "Khởi tạo dữ liệu ứng dụng";
        #endregion

        #region Login
        public const string ACTION_LOGIN = "Đăng nhập";
        public const string ACTION_LOGOUT = "Đăng xuất";
        public const string ACTION_WSO2_LOGIN = "Truy cập hệ thống";
        #endregion

        #region Metadata
        public const string ACTION_META_DATA_CREATE = "Thêm mới thông tin metadata";
        public const string ACTION_META_DATA_CREATE_MANY = "Thêm mới danh sách thông tin metadata";
        public const string ACTION_META_DATA_UPDATE = "Cập nhập thông tin metadata";
        public const string ACTION_META_DATA_DELETE = "Xóa thông tin metadata";
        #endregion

        #region DocumentType
        public const string ACTION_DOCUMENT_TYPE_CREATE = "Thêm mới loại tài liệu";
        public const string ACTION_DOCUMENT_TYPE_CREATE_MANY = "Thêm mới danh sách loại tài liệu";
        public const string ACTION_DOCUMENT_TYPE_UPDATE = "Cập nhập loại tài liệu";
        public const string ACTION_DOCUMENT_TYPE_DELETE = "Xóa loại tài liệu";
        #endregion

        #region DocumentTemplate
        public const string ACTION_DOCUMENT_TEMPLATE_CREATE = "Thêm mới biểu mẫu";
        public const string ACTION_DOCUMENT_TEMPLATE_DUPLICATE = "Nhân bản biểu mẫu";
        public const string ACTION_DOCUMENT_TEMPLATE_UPDATE = "Cập nhập biểu mẫu";
        public const string ACTION_DOCUMENT_TEMPLATE_DELETE = "Xóa biểu mẫu";
        public const string ACTION_DOCUMENT_TEMPLATE_METADATA_UPDATE = "Cập nhập cấu hình biểu mẫu hợp đồng";
        #endregion

        #region Document
        public const string ACTION_GET_DOCTYPE = "Lấy thông tin loại hợp đồng";
        public const string ACTION_GET_METADATA = "Lấy thông tin metadata theo loại hợp đồng";
        public const string ACTION_CREATE_DOCUMENT_PDF_3RD = "Thêm mới hợp đồng từ bên thứ 3 (PDF)";
        public const string ACTION_CREATE_DOCUMENT_DOCX_3RD = "Thêm mới hợp đồng từ bên thứ 3 (Docx)";
        public const string ACTION_CREATE_DOCUMENT_DATA_3RD = "Thêm mới hợp đồng từ bên thứ 3 (Meta data)";
        public const string ACTION_GET_DOC_BY_CODE_OTP = "Lấy thông tin hợp đồng theo OTP";
        public const string ACTION_REJECT_SIGN_DOCUMENT = "Từ chối ký hợp đồng";
        public const string ACTION_GET_DOC_INFO = "Lấy thông tin hợp đồng";
        public const string ACTION_GET_BATCHDOC_INFO = "Lấy thông tin gói hợp đồng";
        public const string ACTION_GET_ACCESS_LINK = "Lấy lại link truy cập hợp đồng";
        public const string ACTION_SEND_ACCESS_LINK = "Gửi lại link truy cập hợp đồng";

        public const string ACTION_DOC_BATCH_CREATE = "Thêm mới lô hợp đồng";
        public const string ACTION_DOC_BATCH_UPDATE = "Cập nhập lô hợp đồng";
        public const string ACTION_DOC_BATCH_DELETE = "Xóa lô hợp đồng";

        public const string ACTION_CREATE_DOC = "Thêm mới hợp đồng";
        public const string ACTION_GET_DOC_DETAIL_BY_CODE = "Lấy thông tin chi tiết hợp đồng theo mã";
        public const string ACTION_GET_LATEST_DOCUMENT = "Lấy hợp đồng mới nhất của khách hàng";
        public const string ACTION_REQUEST_URL_DOWNLOAD_DOCUMENT = "Lấy đường dẫn tải xuống file hợp đồng";
        public const string ACTION_REQUEST_SIGN_DOCUMENT_3RD = "Yêu cầu ký hợp đồng từ bên thứ 3";
        public const string ACTION_REJECT_DOCUMENT_3RD = "Từ chối ký hợp đồng từ bên thứ 3";
        public const string ACTION_DELETE_DOCUMENT_3RD = "Xóa đồng từ bên thứ 3";
        public const string ACTION_RENEW_OTP_3RD = "Lấy mới OTP ký hợp đồng từ bên thứ 3";
        public const string ACTION_CONFIRM_SIGN_DOCUMENT_ESIGN = "Xác nhận ký hợp đồng từ eSign";

        public const string ACTION_UPDATE_DOCUMENT_FILE_PREVIEW = "Cập nhật file preview hợp đồng";
        public const string ACTION_CANCEL_DOCUMENT = "Hủy hợp đồng";
        public const string ACTION_UPDATE_EXPIREDATE_DOCUMENT = "Gia hạn hợp đồng";


        public const string ACTION_REQUEST_APPROVE_DOCUMENT = "Trình duyệt hợp đồng";
        public const string ACTION_LOT_APPROVE_DOCUMENT = "Trung tâm thẩm định phê duyệt hợp đồng";
        public const string ACTION_LOT_REJECT_DOCUMENT = "Trung tâm thẩm định từ chối hợp đồng";
        public const string ACTION_GET_DOCUMENT_FROM_3RD = "Lấy danh sách hợp đồng từ ứng dụng bên thứ 3";
        public const string ACTION_DOCUMENT_DELETE = "Xóa hợp đồng";
        #endregion

        #region Opt
        public const string ACTION_SEND_OTP = "Gửi lại mã OTP truy cập hợp đồng";
        public const string ACTION_GET_OTP_DOCUMENT = "Lấy OTP theo hợp đồng";
        public const string ACTION_SEND_OTP_MAIL_DOCUMENT = "Gửi OTP qua mail theo hợp đồng";
        public const string ACTION_SEND_OTP_SMS_DOCUMENT = "Gửi OTP qua sms theo hợp đồng";
        public const string ACTION_SEND_OTP_MAIL_USER = "Gửi OTP qua mail cho người dùng";
        public const string ACTION_SEND_OTP_SMS_USER = "Gửi OTP qua sms cho người dùng";
        #endregion

        #region Sign
        public const string ACTION_SIGN_DOC_DIGITAL = "Ký điện tử";
        public const string ACTION_SIGN_DOC_HSM = "Ký HSM";
        public const string ACTION_SIGN_DOC_USBTOKEN = "Ký USBTOKEN";
        public const string ACTION_SIGN_DOC_ADSS = "Ký ADSS";
        public const string ACTION_SIGN_EFORM = "Ký eForm";
        
        public const string ACTION_SIGN_TSA_ESEAL = "Ký điện tử an toàn";
        public const string ACTION_SIGN_LTV = "Ký LTV";
        public const string ACTION_SIGN_DOC_DIGITAL_NORMAL = "Ký số thường";
        #endregion

        public const string ACTION_UPDATE_IDENTIFIER_DEVICE = "Cập nhật thiết bị định danh";
        public const string ACTION_ADD_FIREBASE_TOKEN = "Thêm Firebase token";
        public const string ACTION_DELETE_FIREBASE_TOKEN = "Xóa Firebase token";

        public const string ACTION_CREATE_DOC_BATCH = "Thêm mới lô hợp đồng";

        #region Config
        public const string ACTION_GET_COORDINATE = "Lấy tọa độ vùng ký";
        public const string ACTION_CREATE_EFORM = "Tạo eForm";
        public const string ACTION_CREATE_USERSIGN_CONFIG_3RD = "Thêm mới cấu hình ký từ bên thứ 3";
        public const string ACTION_DELETE_USERSIGN_CONFIG_3RD = "Xóa cấu hình ký từ bên thứ 3";
        public const string ACTION_GET_USERSIGN_CONFIG_3RD = "Lấy danh sách cấu hình ký từ bên thứ 3";
        #endregion

        #region Ký từ web app
        public const string ACTION_GET_DOCUMENT_FROM_WEB_APP = "Truy cập ký hợp đồng từ web";
        public const string ACTION_RESEND_PASSCODE_WEB_APP = "Gửi lại passcode truy cập hợp đồng";
        public const string ACTION_CONFIRM_EFORM_FROM_WEB_APP = "Xác nhận ký eForm";
        public const string ACTION_SEND_OTP_SIGN_WEB_APP = "Gửi OTP ký hợp đồng";
        public const string ACTION_SIGN_DOCUMENT_WEB_APP = "Ký hợp đồng từ web app";
        public const string ACTION_REJECT_DOCUMENT_WEB_APP = "Từ chối ký hợp đồng từ web app";
        public const string ACTION_UPLOAD_SIGNED_DOCUMENT_WEB_APP = "Cập nhật hợp đồng đã ký từ web app";

        #endregion

        #region OrgConfig
        public const string ACTION_ORG_CONFIG_CREATE_OR_UPDATE = "Thêm mới cấu hình hiển thị";
        #endregion

        #region User
        public const string ACTION_CREATE_USER = "Thêm mới người dùng";
        public const string ACTION_CREATE_USER_3RD = "Thêm mới/cập nhật người dùng từ ứng dụng bên thứ 3";
        public const string ACTION_UPDATE_USER = "Cập nhật người dùng";
        public const string ACTION_UPDATE_PASS_USER = "Cập nhật mật khẩu người dùng";
        public const string ACTION_DELETE_USER = "Xóa người dùng";
        public const string ACTION_LOCK_USER = "Khóa người dùng";
        public const string ACTION_UNLOCK_USER = "Mở khóa người dùng";
        public const string ACTION_UPDATE_USER_PIN = "Cập nhật User PIN";

        public const string ACTION_VALIDATE_PASSWORD = "Kiểm tra mật khẩu";
        public const string ACTION_SEND_OTP_CHANGEPASS = "Gửi OTP thay đổi mật khẩu";
        public const string ACTION_SEND_OTP_USER_AUTH = "Gửi OTP xác thực người dùng";
        public const string ACTION_UPDATE_USERPIN = "Cập nhật UserPIN";
        public const string ACTION_UPDATE_USER_EFORM_CONFIG = "Cập nhật EForm Config";

        public const string ACTION_UPDATE_USER_ROLE = "Cập nhật User Role";
        public const string ACTION_INIT_USER_ROLE_ORG = "Khởi tạo nhóm người dùng cho đơn vị";
        public const string ACTION_UPDATE_LIST_USER_ROLE = "Cập nhật List User Role";
        #endregion

        #region Hash
        public const string ACTION_HASH_DOC_USBTOKEN = "Tạo chuỗi hash để ký USBTOKEN";
        public const string ACTION_ATTACH_SIGN_TO_FILE = "Gán chữ ký vào file sau khi ký chuỗi hash";
        #endregion

        #region Notify
        public const string ACTION_NOTIFYCONFIG_CREATE = "Thêm mới thông báo";
        public const string ACTION_NOTIFYCONFIG_UPDATE = "Cập nhập thông báo";
        public const string ACTION_NOTIFYCONFIG_DELETE = "Xóa thông báo";
        #endregion

        #region Device
        public const string DEVICE_ANDROID = "Android";
        public const string DEVICE_IOS = "iOS";
        public const string DEVICE_MOBILE = "Mobile";
        public const string DEVICE_WEB = "Web";
        public const string DEVICE_WEBAPP = "WebApp";
        public const string DEVICE_WEB_FROM_EMAIL = "Web SignPage";
        public const string DEVICE_3RD = "3rdApp";
        public const string DEVICE_ESIGN = "eSign";
        #endregion

        #region catalog
        public const string ACTION_COUNTRY_CREATE = "Thêm mới thông tin quốc gia";
        public const string ACTION_COUNTRY_CREATE_MANY = "Thêm mới danh sách thông tin quốc gia";
        public const string ACTION_COUNTRY_UPDATE = "Cập nhập thông tin quốc gia";
        public const string ACTION_COUNTRY_DELETE = "Xóa thông tin quốc gia";

        public const string ACTION_DISTRICT_CREATE = "Thêm mới thông tin quận huyện";
        public const string ACTION_DISTRICT_CREATE_MANY = "Thêm mới danh sách thông tin quận huyện";
        public const string ACTION_DISTRICT_UPDATE = "Cập nhập thông tin quận huyện";
        public const string ACTION_DISTRICT_DELETE = "Xóa thông tin quận huyện";

        public const string ACTION_POSITION_CREATE = "Thêm mới thông tin chức vụ";
        public const string ACTION_POSITION_CREATE_MANY = "Thêm mới danh sách thông tin chức vụ";
        public const string ACTION_POSITION_UPDATE = "Cập nhập thông tin chức vụ";
        public const string ACTION_POSITION_DELETE = "Xóa thông tin chức vụ";

        public const string ACTION_PROVINCE_CREATE = "Thêm mới thông tin tỉnh thành";
        public const string ACTION_PROVINCE_CREATE_MANY = "Thêm mới danh sách thông tin tỉnh thành";
        public const string ACTION_PROVINCE_UPDATE = "Cập nhập thông tin tỉnh thành";
        public const string ACTION_PROVINCE_DELETE = "Xóa thông tin tỉnh thành";

        public const string ACTION_WARD_CREATE = "Thêm mới thông tin phường xã";
        public const string ACTION_WARD_CREATE_MANY = "Thêm mới danh sách thông tin phường xã";
        public const string ACTION_WARD_UPDATE = "Cập nhập thông tin phường xã";
        public const string ACTION_WARD_DELETE = "Xóa thông tin phường xã";

        public const string ACTION_ROLE_CREATE = "Thêm mới thông tin nhóm người dùng";
        public const string ACTION_ROLE_CREATE_MANY = "Thêm mới danh sách thông tin nhóm người dùng";
        public const string ACTION_ROLE_UPDATE = "Cập nhập thông tin nhóm người dùng";
        public const string ACTION_ROLE_DATA_PERMISSION_UPDATE = "Cập nhập phân quyền dữ liệu nhóm người dùng";
        public const string ACTION_ROLE_DELETE = "Xóa thông tin nhóm người dùng";

        public const string ACTION_NAVIGATION_CREATE = "Thêm mới thông tin Menu";
        public const string ACTION_NAVIGATION_UPDATE = "Cập nhập thông tin Menu";
        public const string ACTION_NAVIGATION_DELETE = "Xóa thông tin Menu";

        public const string ACTION_RIGHT_CREATE = "Thêm mới thông tin phân quyền chức năng";
        public const string ACTION_RIGHT_UPDATE = "Cập nhập thông tin phân quyền chức năng";
        public const string ACTION_RIGHT_DELETE = "Xóa thông tin phân quyền chức năng";

        public const string ACTION_USER_SIGN_CONFIG_CREATE = "Thêm mới cấu hình chữ ký";
        public const string ACTION_USER_SIGN_CONFIG_UPDATE = "Cập nhập cấu hình chữ ký";
        public const string ACTION_USER_SIGN_CONFIG_DELETE = "Xóa cấu hình chữ ký";

        public const string ACTION_ORGANIZATION_CREATE = "Thêm mới phòng ban";
        public const string ACTION_ORGANIZATION_UPDATE = "Cập nhập phòng ban";
        public const string ACTION_ORGANIZATION_DELETE = "Xóa phòng ban";

        public const string ACTION_ORGANIZATION_TYPE_CREATE = "Thêm mới loại phòng ban";
        public const string ACTION_ORGANIZATION_TYPE_UPDATE = "Cập nhập loại phòng ban";
        public const string ACTION_ORGANIZATION_TYPE_DELETE = "Xóa loại phòng ban";

        public const string ACTION_HSM_ACCOUNT_CREATE = "Thêm mới tài khoản HSM";
        public const string ACTION_HSM_ACCOUNT_UPDATE = "Cập nhập tài khoản HSM";
        public const string ACTION_HSM_ACCOUNT_DELETE = "Xóa tài khoản HSM";
        #endregion

        #region workflow
        public const string ACTION_WORKFLOW_CREATE = "Thêm mới thông tin quy trình";
        public const string ACTION_WORKFLOW_CREATE_MANY = "Thêm mới danh sách thông tin quy trình";
        public const string ACTION_WORKFLOW_UPDATE = "Cập nhập thông tin quy trình";
        public const string ACTION_WORKFLOW_DELETE = "Xóa thông tin quy trình";

        public const string ACTION_WORKFLOW_STATE_CREATE = "Thêm mới thông tin trạng thái hợp đồng ";
        public const string ACTION_WORKFLOW_STATE_CREATE_MANY = "Thêm mới danh sách thông tin trạng thái hợp đồng";
        public const string ACTION_WORKFLOW_STATE_UPDATE = "Cập nhập thông tin trạng thái hợp đồng";
        public const string ACTION_WORKFLOW_STATE_DELETE = "Xóa thông tin trạng thái hợp đồng";
        #endregion
    }

    public class QueryFilter
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 20;
    }

    #endregion

    public class EmailTemplateConstanct
    {
        public static string Header = "<!DOCTYPE html><html><head><style>body{font-family:Arial,Helvetica,sans-serif}table{width:500px;text-align:center}table thead{min-width:400px;max-width:600px;height:70px;width:100%;background-color:#3598DB}table tbody{padding:30px}table tbody td{border-bottom:1px solid #ddd}table tbody:first-child td{text-align:left}table tbody:last-child td{text-align:right}table tfoot{min-width:400px;max-width:600px;height:50px;width:100%;background-color:#3598DB;color:white}table tfoot tr{height:50px}table tr td{padding:10px} a.button { -webkit-appearance: button; -moz-appearance: button; appearance: button; text-decoration: none; margin: 5px 5px; padding: 10px 20px; color: white; text-decoration: none; background-color: #3AAEE0; border: none; border-radius: 5px; cursor: pointer }</style></head>";

    }

    public enum GenderEnum
    {
        MALE = 1,
        FEMALE = 2,
        UNKNOW = 3
    }

    public enum DetailSignType
    {
        SIGN_TSA = 1,
        SIGN_HSM = 2,
        SIGN_USB_TOKEN = 3,
        SIGN_ADSS = 4
    }
    
    public enum DocumentAction
    {
        TAO_CHINH_SUA_HOP_DONG = 1,
        TAO_PHU_LUC_HOP_DONG = 2,
        HUY_VAN_BAN_LIEN_QUAN_OR_HUY_CA_HOP_DONG = 3,
        NGHIEM_THU_HOP_DONG = 4,
        NGHIEM_THU_DINH_KY_HOP_DONG = 5,
        THANH_LY_HOP_DONG = 6,
        NGHIEM_THU_VA_THANH_LY_HOP_DONG = 7
    }

    public enum ContractType
    {
        QUALIFIED_CONTRACT = 1,
        ADVANCED_CONTRACT = 2,
        BASIC_CONTRACT = 3,
        HD_2BEN_CO_KY_SO = 4
    }

    public enum SignatureGroup
    {
        CHU_KY_BO_CONG_THUONG = 1,
        CHU_KY_CECA = 2,
        CHU_KY_DOANH_NGHIEP_VA_CA_NHAN = 3
    }

    public enum SignatureType
    {
        CHU_KY_NOI_BO = 1,
        CHU_KY_DAI_DIEN = 2,
        CHU_KY_CON_DAU_CONG_TY = 3
    }

    public static class EFormDocumentConstant
    {
        public static string YEU_CAU_CAP_CTS = "DDNCCTS";
        public static string CHAP_THUAN_KY_DIEN_TU = "DDNSDCKDTAT";
        public static string WF_EFORM = "TMP_EFORM_WF";
    }


    public class MetaDataCodeConstants
    {
        public static string ID = "ID";
        public static string DOC_ID = "DOC_ID";
        public static string FULLNAME = "FULLNAME";
        public static string EMAIL = "EMAIL";
        public static string PHONENUMBER = "PHONENUMBER";
        public static string DOC_3RD_ID = "DOC_3RD_ID";
        public static string USER_CONNECT_ID = "USER_CONNECT_ID";
        public static string DOC_NAME = "DOC_NAME ";
        public static string ORG_CODE = "ORG_CODE";
        public static string CONTRACT_TYPE_ACTION = "CONTRACT_TYPE_ACTION";
    }

    public class FileBase64Model
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("fileBase64")]
        public string FileBase64 { get; set; }

        [JsonPropertyName("listData")]
        public List<KeyValueModel> ListData { get; set; }

        public Code Code { get; set; }
        public string Message { get; set; }
    }

    public class KeyValueModel
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class FileConverterResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("traceId")]
        public Guid TraceId { get; set; }

        [JsonPropertyName("data")]
        public FileBase64Model Data { get; set; }
    }

    public static class DocumentTemplateConstants
    {
        public static string KeyPrefix = "«";
        public static string KeySubfix = "»";
    }

    public static class MongoCollections
    {
        public static string SysLog = "sys_log";
        public static string NotifyLog = "notify_log";
    }

    public static class TemplatePlaceHolder
    {
        public static Dictionary<string, string> PlaceHolder = new Dictionary<string, string>()
        {
            {"userFullName" ,"{{userFullName}}"},
            {"documentCode" ,"{{documentCode}}"},
            {"documentName" ,"{{documentName}}"},
            {"expireTime" ,"{{expireTime}}"},
            {"expireDate" ,"{{expireDate}}"}
        };
    }

    public static class NotifyType
    {
        public static int ConsentXacNhanKy = 1;
        public static int KyHopDongThanhCong = 2;
        public static int NhacNhoKyHopDong = 3;
        public static int HopDongHetHanKy = 4;
        public static int HopDongKyHoanThanh = 5;
        public static int HopDongDaBiTuChoi = 6;
        public static int LamMoiHopDong = 7;
        public static int YeuCauKy = 8;
        public static int HopDongDaBiHuy = 9;
        public static int GuiThongBaoKyHopDong = 10;
    }

    public static class UserRoles
    {
        public static string SYS_ADMIN = "SYS_ADMIN";
        public static string ORG_ADMIN = "ORG_ADMIN";
        public static string USER = "USER";
    }
}