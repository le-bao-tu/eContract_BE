using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class SignServiceModel
    {
    }

    public class SignFileModel
    {
        public string FileBucketName { get; set; }
        public string FileObjectName { get; set; }
        public string FileName { get; set; }
        public string NewHashFile { get; set; }
        public string NewXMLFile { get; set; }
    }

    public class DataInputSignPDF
    {

        /// <summary>
        /// Alias
        /// </summary>
        [JsonPropertyName("cert_alias")]
        public string CertAlias { get; set; }
        /// <summary>
        /// User Pin
        /// </summary>
        [JsonPropertyName("cert_user_pin")]
        public string CertUserPin { get; set; }
        /// <summary>
        ///  Slot label
        /// </summary>
        [JsonPropertyName("cert_slot_label")]
        public string CertSlotLabel { get; set; }
        /// <summary>
        /// User Pin
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
        /// <summary>
        ///  Slot label
        /// </summary>
        [JsonPropertyName("location")]
        public string Location { get; set; }
        /// <summary>
        /// Lower left x
        /// </summary>
        [JsonPropertyName("llx")]
        public string Llx { get; set; }
        /// <summary>
        /// Lowwer left y
        /// </summary>
        [JsonPropertyName("lly")]
        public string Lly { get; set; }
        /// <summary>
        ///  Rectangle width
        /// </summary>
        [JsonPropertyName("urx")]
        public string Urx { get; set; }
        /// <summary>
        /// Rectangle height 
        /// </summary>
        [JsonPropertyName("ury")]
        public string Ury { get; set; }
        /// <summary>
        /// Sign At Page
        /// </summary>
        [JsonPropertyName("page")]
        public string Page { get; set; }
        /// <summary>
        /// Mail
        /// </summary>
        [JsonPropertyName("mail")]
        public string Mail { get; set; }
        /// <summary>
        /// File Info
        /// </summary>
        [JsonPropertyName("file")]
        public SignFileModel FileInfo { get; set; } // sign at page
        /// <summary>
        /// Base64FileImage
        /// </summary>
        [JsonPropertyName("base64Image")]
        public string Base64Image { get; set; }
    }


    public class SigningBoxResponseModel
    {
        /// <summary>
        /// Response Code
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Response Data
        /// </summary>
        public string Data { get; set; }
    }

    public class MDMResponseModel
    {
        public MDMFileModel Data { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public int TotalTime { get; set; }
    }

    public class MDMFileModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PhysicalName { get; set; }
        public int Size { get; set; }
        public string Extension { get; set; }
        public string Path { get; set; }
        public string PhysicalPath { get; set; }
    }

    public class ElectronicSigningResponseModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("data")]
        public ElectronicSigningDataModel Data { get; set; }
    }

    public class ElectronicSigningDataModel
    {
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }
        [JsonPropertyName("crl")]
        public string Crl { get; set; }
        [JsonPropertyName("file")]
        public string File { get; set; }
    }
}
