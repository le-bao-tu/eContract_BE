using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetCore.Data
{
    [Table("user_sign_config")]
    public class UserSignConfig : BaseTableWithIdentityNumber
    {
        public UserSignConfig()
        {

        }
        [Key]
        [Column("id")]
        public Guid Id { get; set; }


        [Required]
        [ForeignKey("User")]
        [Column("user_id")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [Required]
        [Column("appearance_sign_type")]
        public string AppearenceSignType { get; set; }

        [Required]
        [Column("code")]
        public string Code { get; set; }

        [Column("list_sign_info_json")]
        public string ListSignInfoJson { get; set; }

        [Column("logo_file_base64")]
        public string LogoFileBase64 { get; set; }

        [Column("image_file_base64")]
        public string ImageFileBase64 { get; set; }

        [Column("background_image_file_base64")]
        public string BackgroundImageFileBase64 { get; set; }

        [Column("sign_appearance_image")]
        public bool SignAppearanceImage { get; set; }

        [Column("sign_appearance_logo")]
        public bool SignAppearanceLogo { get; set; }

        [Column("scale_image")]
        public float ScaleImage { get; set; }

        [Column("scale_text")]
        public float ScaleText { get; set; }

        [Column("scale_logo")]
        public float ScaleLogo { get; set; }

        [Required]
        [Column("is_sign_default")]
        public bool IsSignDefault { get; set; }

        [Column("more_info")]
        public string MoreInfo { get; set; }
    }

    public class SignInfo
    {
        [JsonPropertyName("value")]
        public bool Value { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("label")]
        public string Label { get; set; }
    }
}
