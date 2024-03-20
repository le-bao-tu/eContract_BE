using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("organization")]
    public class Organization : BaseTableWithApplication
    {
        public Organization()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [StringLength(128)]
        [Column("name")]
        public string Name { get; set; }

        [Column("short_name")]
        public string ShortName { get; set; }

        [Column("ou")]
        public string OU { get; set; }

        [Column("parent_id")]
        public Guid? ParentId { get; set; }

        [Column("tax_code")]
        public string TaxCode { get; set; }

        [Column("identify_number")]
        public string IdentifyNumber { get; set; }

        [Column("issuer_by")]
        public string IssuerBy { get; set; }

        [Column("issuer_date")]
        public DateTime? IssuerDate { get; set; }

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

        [Column("address")]
        public string Address { get; set; }

        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("path")]
        public string Path { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [ForeignKey("OrganizationType")]
        [Column("organization_type_id")]
        public Guid? OrganizationTypeId { get; set; }

        public OrganizationType OrganizationType { get; set; }

        /// <summary>
        /// Giấy phép kinh doanh
        /// </summary>
        [Column("bussiness_license_object_name")]
        public string BussinessLicenseObjectName { get; set; }

        [Column("bussiness_license_bucket_name")]
        public string BussinessLicenseBucketName { get; set; }

        #region Thông tin người đại diện
        //Chứng minh thư mặt trước
        [Column("identity_front_object_name")]
        public string IdentityFrontObjectName { get; set; }

        [Column("identity_front_bucket_name")]
        public string IdentityFrontBucketName { get; set; }

        //Chứng minh thư mặt sau
        [Column("identity_back_object_name")]
        public string IdentityBackObjectName { get; set; }

        [Column("identity_back_bucket_name")]
        public string IdentityBackBucketName { get; set; }

        //- họ và tên người đại diện
        [Column("repre_full_name")]
        public string RepresentationFullName { get; set; }

        [Column("repre_position_line1")]
        public string RepresentationPositionLine1 { get; set; }

        [Column("repre_position_line2")]
        public string RepresentationPositionLine2 { get; set; }

        //- số cmnd/cccd người đại diện
        [Column("repre_identity_number")]
        public string RepresentationIdentityNumber { get; set; }

        //- giới tính người đại diện
        [Column("repre_sex")]
        public GenderEnum? RepresentationSex { get; set; }

        //- ngày cấp cmnd người đại diện
        [Column("repre_issue_date")]
        public DateTime? RepresentationIssueDate { get; set; }

        //- ngày sinh
        [Column("repre_birthday")]
        public DateTime? RepresentationBirthday { get; set; }

        //- nơi cấp cmnd người đại diện
        [Column("repre_issueby")]
        public string RepresentationIssueBy { get; set; }

        //- địa chỉ thường trú
        [Column("repre_permanent_address")]
        public string RepresentationPermanentAddress { get; set; }

        //- địa chỉ hiện tại
        [Column("repre_current_address")]
        public string RepresentationCurentAddess { get; set; }

        //- quốc gia người đại diện
        [Column("repre_country_id")]
        public Guid? RepresentationCountryId { get; set; }

        [Column("repre_country_code")]
        public string RepresentationCountryCode { get; set; }

        [Column("repre_country_name")]
        public string RepresentationCountryName { get; set; }

        //- tỉnh thành phố người đại diện
        [Column("repre_province_id")]
        public Guid? RepresentationProvinceId { get; set; }

        [Column("repre_province_code")]
        public string RepresentationProvinceCode { get; set; }

        [Column("repre_province_name")]
        public string RepresentationProvinceName { get; set; }

        //- email người đại diện
        [Column("repre_email")]
        public string RepresentationEmail { get; set; }

        //- số điện thoại người đại diện
        [Column("repre_phone_number")]
        public string RepresentationPhoneNumber { get; set; }

        #endregion

    }
}
