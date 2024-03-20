using Microsoft.AspNetCore.Http;
using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class UserBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string SubjectDN { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public bool IsLock { get; set; } = false;
        public bool Status { get; set; } = true;
        public Guid? OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public DateTime? CreatedDate { get; set; }

        [JsonIgnore]
        public string UserEFormInfoJson { get; set; }

        public UserEFormModel UserEFormInfo
        {
            get
            {
                if (!string.IsNullOrEmpty(this.UserEFormInfoJson)) return JsonSerializer.Deserialize<UserEFormModel>(this.UserEFormInfoJson);
                return null;
            }
            set
            {
            }
        }

        public bool IsInternalUser { get; set; }
        public string IdentityNumber { get; set; }
        public string ConnectId { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string ApplicationId { get; set; }
    }
    public class UserProfileUpdateModel : UserModel
    {
        public string PlaceOfOrigin { get; set; }
        public string FrontImageBucketName { get; set; }
        public string FrontImageObjectName { get; set; }
        public string BackImageBucketName { get; set; }
        public string BackImageObjectName { get; set; }
        public string FaceImageBucketName { get; set; }
        public string FaceImageObjectName { get; set; }
        public string PermanentAddress { get; set; }
        public void UpdateUserEntity(User entity)
        {
            entity.ConnectId = this.ConnectId;
            entity.Name = this.Name;
            entity.Address = this.Address;
            entity.Sex = this.Sex;
            entity.Email = this.Email;
            entity.PhoneNumber = this.PhoneNumber;
            entity.Birthday = this.Birthday;
            entity.EFormConfig = this.EFormConfig;
            entity.IdentityNumber = this.IdentityNumber;
            entity.IdentityType = this.IdentityType;
            entity.IssueDate = this.IssueDate;
            entity.IssueBy = this.IssueBy;
            entity.CountryId = this.CountryId;
            entity.CountryName = this.CountryName;
            entity.ProvinceId = this.ProvinceId;
            entity.ProvinceName = this.ProvinceName;
            entity.DistrictId = this.DistrictId;
            entity.DistrictName = this.DistrictName;
            entity.PlaceOfOrigin = this.PlaceOfOrigin;
            entity.PermanentAddress = this.PermanentAddress;
            entity.EKYCInfo.FrontImageBucketName = this.FrontImageBucketName;
            entity.EKYCInfo.FrontImageObjectName = this.FrontImageObjectName;
            entity.EKYCInfo.BackImageBucketName = this.BackImageBucketName;
            entity.EKYCInfo.BackImageObjectName = this.BackImageObjectName;
            entity.EKYCInfo.FaceImageBucketName = this.FaceImageBucketName;
            entity.EKYCInfo.FaceImageObjectName = this.FaceImageObjectName;
        }
    }
    public class UserDeviceModel
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public bool IsIdentifierDevice { get; set; }
        public DateTime CreatedDate { get; set; }

    }

    public class UserDeviceQueryFilter
    {

        public Guid Id { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public bool IsIdentifierDevice { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TextSearch { get; set; }

    }
    public class UserModel : UserBaseModel
    {
        public string UserConnectId { get; set; }
        public DateTime? Birthday { get; set; }
        public EFormConfigEnum EFormConfig { get; set; } = EFormConfigEnum.KY_DIEN_TU;
        public string Address { get; set; }
        /*
            1. Nam
            2. Nữ
            3. Không xác định
         */
        public GenderEnum? Sex { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public string IssueBy { get; set; }
        public Guid? CountryId { get; set; }
        public string CountryName { get; set; }
        public Guid? ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public Guid? DistrictId { get; set; }
        public string DistrictName { get; set; }
        public string PositionName { get; set; }
        public int Order { get; set; } = 0;
        public string Description { get; set; }
        public string ZipCode { get; set; }
        public bool HasUserPIN { get; set; }
        public string UserPIN { get; set; }
        public bool IsApproveAutoSign { get; set; }
        public bool IsNotRequirePINToSign { get; set; }
        public bool IsReceiveSystemNoti { get; set; }
        public bool IsReceiveSignFailNoti { get; set; }
        public bool IsEKYC { get; set; }

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

        public EKYCInfoModel EKYCInfo { get; set; }

        public string FrontImageCardUrl { get; set; }
        public string BackImageCardUrl { get; set; }
        public string FaceImageCardUrl { get; set; }
    }

    public class UserDetailModel : UserModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class UserLockModel
    {
        public List<Guid> ListId { get; set; }
        public bool IsLock { get; set; }
        public Guid? ModifiedUserId { get; set; }
    }

    public class UserUpdatePasswordModel
    {
        public Guid UserId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public Guid? ModifiedUserId { get; set; }
    }

    public class UserCreateModel : UserModel
    {
        //public string Password { get; set; }
        public Guid? CreatedUserId { get; set; }

        public Guid? ApplicationId { get; set; }
    }

    public class UserUpdateModel : UserModel 
    {
        public string Password { get; set; }
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(User entity)
        {
            entity.ConnectId = this.ConnectId;
            entity.Name = this.Name;
            entity.Address = this.Address;
            entity.Sex = this.Sex;
            entity.Email = this.Email;
            entity.PhoneNumber = this.PhoneNumber;
            entity.Birthday = this.Birthday;
            entity.EFormConfig = this.EFormConfig;
            entity.IdentityNumber = this.IdentityNumber;
            entity.IdentityType = this.IdentityType;
            entity.IssueDate = this.IssueDate;
            entity.IssueBy = this.IssueBy;
            entity.CountryId = this.CountryId;
            entity.CountryName = this.CountryName;
            entity.ProvinceId = this.ProvinceId;
            entity.ProvinceName = this.ProvinceName;
            entity.DistrictId = this.DistrictId;
            entity.DistrictName = this.DistrictName;
            entity.PositionName = this.PositionName;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.OrganizationId = this.OrganizationId;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
        }
    }

    public class UserQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? OrganizationCurrentId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";

        //asc - desc
        public string Ascending { get; set; } = "desc";

        public UserQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }

        public bool? StatusCert { get; set; }
        public bool? IsInternalUser { get; set; }
    }

    public class UserSelectItemModel : SelectItemModel
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string IdentityNumber { get; set; }
        public string FullName { get; set; }
        public string DisplayName { get; set; }
        public Guid? PositionId { get; set; }
        public string PositionName { get; set; }
        public Guid? OrganizationId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public EFormConfigEnum EFormConfig { get; set; }
        public bool HasUserPIN { get; set; }
        public bool IsLock { get; set; }
        public bool IsInternalUser { get; set; }
        public bool IsEKYC { get; set; }
    }

    public class UserCertificateCreateModel
    {
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public Guid? OrganizationId { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNumber { get; set; }
        public string TaxCode { get; set; }
        public string CommonName { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationUnit { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PositionName { get; set; }
        public Guid? CountryId { get; set; }
        public string CountryName { get; set; }
        public Guid? ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public string Address { get; set; }
    }

    public class UserConnectModel
    {
        public string UserConnectId { get; set; }
        public string UserFullName { get; set; }
        public DateTime? Birthday { get; set; }
        public GenderEnum? Sex { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public string IssueBy { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserEmail { get; set; }
        public string Address { get; set; }
        public string CountryName { get; set; }
        public string ProvinceName { get; set; }
        public string DistrictName { get; set; }
        public EFormConfigEnum EFormType { get; set; }
        public bool IsConfirmEformDTAT { get; set; }
        public bool IsConfirmEformCTS { get; set; }

        public void UpdateToEntity(User entity)
        {
            entity.Name = this.UserFullName;
            entity.Birthday = this.Birthday;
            entity.Sex = this.Sex;
            entity.IdentityType = this.IdentityType;
            entity.IdentityNumber = this.IdentityNumber;
            entity.IssueDate = this.IssueDate;
            entity.IssueBy = this.IssueBy;
            entity.PhoneNumber = this.UserPhoneNumber;
            entity.Email = this.UserEmail;
            entity.Address = this.Address;
            entity.CountryName = this.CountryName;
            entity.ProvinceName = this.ProvinceName;
            entity.DistrictName = this.DistrictName;

            entity.ModifiedDate = DateTime.Now;
        }
    }

    public class UpdateOrCreateUserModel
    {
        public Guid OrganizationId { get; set; }
        public List<UserConnectModel> ListUser { get; set; }
    }

    public class OrgAndUserConnectRequestModel
    {
        public Guid OrganizationId { get; set; }
        public List<string> ListUserConnectId { get; set; }
    }

    public class UserConnectResonseModel
    {
        [JsonIgnore]
        public Guid UserId { get; set; }

        public string UserConnectId { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
    }

    public class OrgAndUserConnectInfoModel
    {
        public OrganizationForServiceModel OrganizationInfo { get; set; }
        public List<UserConnectInfoModel> ListUserConnectInfo { get; set; }
    }

    public class UserConnectInfoModel
    {
        public Guid? OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string UserConnectId { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNumber { get; set; }
    }

    public class UserUpdatePIN
    {
        public Guid UserId { get; set; }
        public Guid ModifiedUserId { get; set; }
        public string UserPIN { get; set; }
        public bool IsApproveAutoSign { get; set; }
        public bool IsNotRequirePINToSign { get; set; }
        public bool IsReceiveSystemNoti { get; set; }
        public bool IsReceiveSignFailNoti { get; set; }
    }

    #region User Role

    public class ResultUserPermissionModel
    {
        public List<string> Role { get; set; }
        public List<string> Right { get; set; }
        public List<NavigationSelectItemModel> Navigation { get; set; }
        public bool IsLock { get; set; }
    }

    public class GetUserRoleModel
    {
        public Guid Id { get; set; }
    }

    public class ResultGetUserRoleModel
    {
        public List<Guid> ListRoleId { get; set; }
    }

    public class UpdateUserRoleModel
    {
        public Guid Id { get; set; }
        public List<Guid> ListRoleId { get; set; }
    }

    public class GetUserRoleByRoleModel
    {
        public Guid RoleId { get; set; }
    }

    public class ResultGetUserRoleByRoleModel
    {
        public List<Guid> ListUserId { get; set; }
    }

    public class SaveListUserRoleModel
    {
        public Guid RoleId { get; set; }
        public List<Guid> UserIds { get; set; }
    }

    public class InitUserAdminOrgModel
    {
        public string OrganizationCode { get; set; }
        public string UserName { get; set; }
    }

    #endregion User Role

    #region SCIM

    public class SICMAuthenToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("creadted_date")]
        public DateTime? CreadtedDate { get; set; }
    }

    public class SICMUserCreateRequestModel
    {
        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    public class SICMUserCreateResponseModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class SICMUserUpdateResponseModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }
    }

    public class SICMUserDeleteResponseModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("detail")]
        public string Detail { get; set; }
    }

    public class SICMUserSearchResponseModel
    {
        [JsonPropertyName("Resources")]
        public List<SICMUserSearchResourceModel> Resources { get; set; }
    }

    public class SICMUserSearchResourceModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    #endregion SCIM

    #region Device - Firebase

    public class DeviceAddRequestModel
    {
        public string UserConnectId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string FirebaseToken { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class FirebaseRequestModel
    {
        public string DeviceId { get; set; }
        public string FirebaseToken { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    public class FirebaseRequestModel3rd
    {
        public string UserConnectId { get; set; }
        public string DeviceId { get; set; }
        public string FirebaseToken { get; set; }
        public LocationSign3rdModel Location { get; set; }
        public OpratingSystemMobileModel DeviceInfo { get; set; }
    }

    #endregion Device - Firebase

    #region Mobile API

    public class ChangePasswordModel
    {
        public Guid UserId { get; set; }
        public string Password { get; set; }
        public string OTP { get; set; }
    }

    public class ChangeUserPINModel
    {
        public Guid UserId { get; set; }
        public string UserPIN { get; set; }
    }

    public class UserSCIMDetailModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    public class UpdateEFormConfigModel
    {
        public Guid UserId { get; set; }
        public EFormConfigEnum EFormConfig { get; set; }
    }

    public class UserEFormConfigModel : UserBaseModel
    {
        public EFormConfigEnum EFormConfig { get; set; }
    }

    public class AddCertificateModel
    {
        public Guid UserId { get; set; }
    }

    #endregion Mobile API

    public class SaveEKYCModel
    {
        public IFormFile IdentityFront { get; set; }
        public IFormFile IdentityBack { get; set; }
        public IFormFile UserFace { get; set; }
    }

    public class RegisterFrontCardModel
    {
        public IFormFile FrontCard { get; set; }
    }

    public class RegisterBackCardModel
    {
        public IFormFile BackCard { get; set; }
    }

    public class RegisterFaceVideoLivenessModel
    {
        public IFormFile FaceVideo { get; set; }
    }

    public class UpdateEKYCUserInfoModel
    {
        public EKYCUserInfo UserInfo { get; set; }
        public Guid UserId { get; set; }
    }

    public class EKYCUserInfo
    {
        public string Name { get; set; }
        public DateTime? Birthday { get; set; }
        public GenderEnum? Sex { get; set; }
        public string IdentityType { get; set; }
        public string IdentityNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public string IssueBy { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string CountryName { get; set; }
        public string ProvinceName { get; set; }
        public string DistrictName { get; set; }
        public EKYCInfoModel EKYCInfo { get; set; }
        public string SessionId { get; set; }
    }
}