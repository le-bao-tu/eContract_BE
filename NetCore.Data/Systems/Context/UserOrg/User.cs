using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("user")]
    public class User : BaseTableWithApplication
    {
        public User()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [StringLength(64)]
        [Column("code")]
        public string Code { get; set; }

        [StringLength(128)]
        [Column("name")]
        public string Name { get; set; }

        [StringLength(256)]
        [Column("connect_id")]
        public string ConnectId { get; set; }

        [StringLength(128)]
        [Column("email")]
        public string Email { get; set; }

        [StringLength(128)]
        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(128)]
        [Column("user_name")]
        public string UserName { get; set; }

        [Column("birthday")]
        public DateTime? Birthday { get; set; }

        /*
            1. Nam
            2. Nữ
            3. Không xác định
         */
        [Column("sex")]
        public GenderEnum? Sex { get; set; }

        [Column("identity_type")]
        public string IdentityType { get; set; }

        [Column("identity_number")]
        public string IdentityNumber { get; set; }

        #region Phần thông tin này viết sai chính tả, sau sẽ bỏ đi sau
        [Column("issuer_date")]
        public DateTime? IssuerDate { get; set; }

        [Column("issuer_by")]
        public string IssuerBy { get; set; }
        #endregion

        [Column("issue_date")]
        public DateTime? IssueDate { get; set; }

        [Column("issue_by")]
        public string IssueBy { get; set; }

        [Column("country_id")]
        public Guid? CountryId { get; set; }

        [Column("country_name")]
        public string CountryName { get; set; }

        [Column("province_id")]
        public Guid? ProvinceId { get; set; }

        [Column("province_name")]
        public string ProvinceName { get; set; }

        [Column("zip_code")]
        public string ZipCode { get; set; }

        [Column("district_id")]
        public Guid? DistrictId { get; set; }

        [Column("district_name")]
        public string DistrictName { get; set; }

        [Column("subject_dn")]
        public string SubjectDN { get; set; }

        [Column("position_name")]
        public string PositionName { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("password_salt")]
        public string PasswordSalt { get; set; }

        [Column("last_activity_date")]
        public DateTime? LastActivityDate { get; set; }

        [ForeignKey("Organization")]
        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        public Organization Organization { get; set; }

        [Column("is_enable_smart_otp")]
        public bool IsEnableSmartOTP { get; set; } = false;

        [Column("is_lock")]
        public bool IsLock { get; set; } = false;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        public UserType Type { get; set; }

        // Cấu hình sử dụng ký mặc định là sử dụng CTS hay ký điện tử => để tạo eForm cho đúng
        [Column("eform_config")]
        public EFormConfigEnum EFormConfig { get; set; } = EFormConfigEnum.KY_DIEN_TU;

        [Column("user_eform_info_json")]
        public string UserEFormInfoJson
        {

            get
            {
                return UserEFormInfo == null ? null : JsonSerializer.Serialize(UserEFormInfo);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    UserEFormInfo = null;
                else
                    UserEFormInfo = JsonSerializer.Deserialize<UserEFormModel>(value);
            }
        }

        [NotMapped]
        public UserEFormModel UserEFormInfo { get; set; }

        [Column("ou")]
        public string OU { get; set; }

        [Column("user_pin")]
        public string UserPIN { get; set; }
        
        [Column("place_of_origin")]
        public string PlaceOfOrigin { get; set; }
        
        [Column("permanent_address")]
        public string PermanentAddress { get; set; }

        [Column("is_approve_auto_sign")]
        public bool IsApproveAutoSign { get; set; } = false;

        [Column("is_not_require_pin_to_sign")]
        public bool IsNotRequirePINToSign { get; set; } = false;

        [Column("is_receive_system_noti")]
        public bool IsReceiveSystemNoti { get; set; } = false;

        [Column("is_receive_sign_fail_noti")]
        public bool IsReceiveSignFailNoti { get; set; } = false;

        [Column("is_internal_user")]
        public bool IsInternalUser { get; set; }

        // Thông tin eKYC
        [Column("ekyc_info_json")]
        public string EKYCInfoJson
        {

            get
            {
                return EKYCInfo == null ? null : JsonSerializer.Serialize(EKYCInfo);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    EKYCInfo = null;
                else
                    EKYCInfo = JsonSerializer.Deserialize<EKYCInfoModel>(value);
            }
        }

        [NotMapped]
        public EKYCInfoModel EKYCInfo { get; set; }
    }

    //BucketName mặc định mà mã đơn vị gốc
    //ObjectName rootpath là năm tháng ngày rồi tiếp theo là user name (nhớ bỏ dấu / nếu có)
    public class EKYCInfoModel
    {
        public string SessionId { get; set; }
        public bool IsEKYC { get; set; }
        public string FrontImageBucketName { get; set; }
        public string FrontImageObjectName { get; set; }
        public string BackImageBucketName { get; set; }
        public string BackImageObjectName { get; set; }
        public string FaceImageBucketName { get; set; }
        public string FaceImageObjectName { get; set; }
        public string FaceVideoBucketName { get; set; }
        public string FaceVideoObjectName { get; set; }
    }

    public class UserEFormModel
    {
        public Guid? ConfirmDigitalSignatureDocumentId { get; set; }
        public Guid? RequestCertificateDocumentId { get; set; }
        public bool IsConfirmDigitalSignature { get; set; } = false;
        public bool IsConfirmRequestCertificate { get; set; } = false;
        public string RequestCertificateDocumentBucketName { get; set; }
        public string RequestCertificateDocumentObjectName { get; set; }
    }

    public enum UserType
    {
        USER = 1,
        ORG = 2
    }
}
