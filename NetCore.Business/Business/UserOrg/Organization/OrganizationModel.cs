using NetCore.Data;
using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public class OrganizationBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public bool Status { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
    }

    public class OrganizationModel : OrganizationBaseModel
    {
        public Guid? ParentId { get; set; }
        public string TaxCode { get; set; }
        public string IdentifyNumber { get; set; }
        public string IssuerBy { get; set; }
        public DateTime? IssuerDate { get; set; }
        public Guid? CountryId { get; set; }
        public string CountryName { get; set; }
        public Guid? ProvinceId { get; set; }
        public string ProvinceName { get; set; }
        public Guid? DistrictId { get; set; }
        public string DistrictName { get; set; }
        public string ZipCode { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public int Order { get; set; } = 0;
        public string Description { get; set; }
        public Guid? OrganizationTypeId { get; set; }
        public string BussinessLicenseObjectName { get; set; }
        public string BussinessLicenseBucketName { get; set; }
        public string BussinessLicenseFilePath { get; set; }

        #region người đại diện
        public string IdentityFrontObjectName { get; set; }
        public string IdentityFrontBucketName { get; set; }
        public string IdentityFrontFilePath { get; set; }
        public string IdentityBackObjectName { get; set; }
        public string IdentityBackBucketName { get; set; }
        public string IdentityBackFilePath { get; set; }
        public string RepresentationFullName { get; set; }
        public string RepresentationPositionLine1 { get; set; }
        public string RepresentationPositionLine2 { get; set; }
        public string RepresentationIdentityNumber { get; set; }        
        public DateTime? RepresentationIssueDate { get; set; }
        public DateTime? RepresentationBirthday { get; set; }
        public string RepresentationIssueBy { get; set; }
        public string RepresentationPermanentAddress { get; set; }
        public string RepresentationCurentAddess { get; set; }
        public Guid? RepresentationCountryId { get; set; }
        public string RepresentationCountryCode { get; set; }
        public string RepresentationCountryName { get; set; }
        public Guid? RepresentationProvinceId { get; set; }
        public string RepresentationProvinceCode { get; set; }
        public string RepresentationProvinceName { get; set; }
        public string RepresentationEmail { get; set; }
        public string RepresentationPhoneNumber { get; set; }
        public GenderEnum? RepresentationSex { get; set; }
        #endregion
    }

    public class OrganizationDetailModel : OrganizationModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class OrganizationCreateModel : OrganizationModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class OrganizationUpdateModel : OrganizationModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Organization entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.ShortName = this.ShortName;
            entity.Status = this.Status;
            entity.Order = this.Order;
            entity.Description = this.Description;
            entity.ParentId = this.ParentId;
            entity.CountryId = this.CountryId;
            entity.CountryName = this.CountryName;
            entity.ProvinceId = this.ProvinceId;
            entity.ProvinceName = this.ProvinceName;
            entity.DistrictId = this.DistrictId;
            entity.DistrictName = this.DistrictName;
            entity.ZipCode = this.ZipCode;
            entity.PhoneNumber = this.PhoneNumber;
            entity.IdentifyNumber = this.IdentifyNumber;
            entity.Email = this.Email;
            entity.Address = this.Address;
            entity.IssuerBy = this.IssuerBy;
            entity.IssuerDate = this.IssuerDate;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.OrganizationTypeId = this.OrganizationTypeId;
            entity.BussinessLicenseObjectName = this.BussinessLicenseObjectName;
            entity.BussinessLicenseBucketName = this.BussinessLicenseBucketName;

            #region thông tin người đại diện
            entity.IdentityFrontObjectName = this.IdentityFrontObjectName;
            entity.IdentityFrontBucketName = this.IdentityFrontBucketName;
            entity.IdentityBackObjectName = this.IdentityBackObjectName;
            entity.IdentityBackBucketName = this.IdentityBackBucketName;
            entity.RepresentationFullName = this.RepresentationFullName;
            entity.RepresentationPositionLine1 = this.RepresentationPositionLine1;
            entity.RepresentationPositionLine2 = this.RepresentationPositionLine2;
            entity.RepresentationIdentityNumber = this.RepresentationIdentityNumber;
            entity.RepresentationSex = this.RepresentationSex;
            entity.RepresentationIssueDate = this.RepresentationIssueDate;
            entity.RepresentationBirthday = this.RepresentationBirthday;
            entity.RepresentationIssueBy = this.RepresentationIssueBy;
            entity.RepresentationPermanentAddress = this.RepresentationPermanentAddress;
            entity.RepresentationCurentAddess = this.RepresentationCurentAddess;
            entity.RepresentationCountryId = this.RepresentationCountryId;
            entity.RepresentationCountryCode = this.RepresentationCountryCode;
            entity.RepresentationCountryName = this.RepresentationCountryName;
            entity.RepresentationProvinceId = this.RepresentationProvinceId;
            entity.RepresentationProvinceCode = this.RepresentationProvinceCode;
            entity.RepresentationProvinceName = this.RepresentationProvinceName;
            entity.RepresentationEmail = this.RepresentationEmail;
            entity.RepresentationPhoneNumber = this.RepresentationPhoneNumber;
            #endregion
        }
    }

    public class OrganizationQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public OrganizationQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class OrganizationSelectItemModel : SelectItemModel
    {
        //public string Path { get; set; }
        public Guid? ParentId { get; set; }
        public bool Status { get; set; }
    }

    public class OrganizationForServiceModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
    }

    public class OrgLayoutModel
    {
        public Guid Id { get; set; }
        public string OrgName { get; set; }
        public string DisplayName { get; set; }
        public string LogoBase64 { get; set; }
    }
}