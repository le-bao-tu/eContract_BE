using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class UserSignConfigBaseModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public bool IsSignDefault { get; set; }
        public string ListSignInfoJson { get; set; }
        public List<SignInfo> ListSignInfo
        {
            get
            {
                return string.IsNullOrEmpty(ListSignInfoJson) ? new List<SignInfo>() : JsonSerializer.Deserialize<List<SignInfo>>(ListSignInfoJson);
            }
        }
        public string AppearenceSignType { get; set; }
        public string Code { get; set; }
        public string LogoFileBase64 { get; set; }
        public string ImageFileBase64 { get; set; }
        public bool SignAppearanceImage { get; set; }
        public bool SignAppearanceLogo { get; set; }

        public float ScaleImage { get; set; }

        public float ScaleText { get; set; }

        public float ScaleLogo { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string MoreInfo { get; set; }
        public string BackgroundImageFileBase64 { get; set; }
    }

    public class UserSignConfigModel : UserSignConfigBaseModel
    {
        public int Order { get; set; } = 0;

    }
    public class UserSignConfigQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? UserId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public UserSignConfigQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }
    public class UserSignConfigCreateOrUpdateModel : UserSignConfigModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
        public Guid? ModifiedUserId { get; set; }

        public void UpdateToEntity(UserSignConfig entity)
        {
            entity.ListSignInfoJson = ListSignInfoJson;
            entity.LogoFileBase64 = LogoFileBase64;
            entity.ImageFileBase64 = ImageFileBase64;
            entity.SignAppearanceImage = SignAppearanceImage;
            entity.SignAppearanceLogo = SignAppearanceLogo;
            entity.ScaleLogo = ScaleLogo;
            entity.ScaleImage = ScaleImage;
            entity.ScaleText = ScaleText;
            entity.IsSignDefault = IsSignDefault;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.MoreInfo = this.MoreInfo;
            entity.BackgroundImageFileBase64 = this.BackgroundImageFileBase64;
        }
    }

    #region Kết nối hệ thống bên thứ 3
    public class UserSign3rdModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string ImageFileBase64 { get; set; }
        public string UserConnectId { get; set; }
    }
    public class UserSignFilter3rdModel
    {
        public string UserConnectId { get; set; }
    }

    public class UserSignUpdate3rdModel
    {
        public Guid Id { get; set; }
        public string ImageFileBase64 { get; set; }
    }

    public class UserSignDelete3rdModel
    {
        public Guid Id { get; set; }
    }
    #endregion
}