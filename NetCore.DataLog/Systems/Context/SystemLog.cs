using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NetCore.DataLog
{
    public class SystemLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("trace_id")]
        public string TraceId { get; set; }

        [BsonElement("action_code")]
        public string ActionCode { get; set; }

        [BsonElement("action_name")]
        public string ActionName { get; set; }

        [BsonElement("sub_action_code")]
        public string SubActionCode { get; set; }

        [BsonElement("sub_action_name")]
        public string SubActionName { get; set; }

        [BsonElement("ip")]
        public string IP { get; set; }

        [BsonElement("created_date")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [BsonElement("parent_id")]
        public string ParentId { get; set; }

        [BsonElement("object_code")]
        public string ObjectCode { get; set; }

        [BsonElement("object_id")]
        public string ObjectId { get; set; }

        [BsonElement("device")]
        public string Device { get; set; }

        [BsonElement("operating_system")]
        public OperatingSystem OperatingSystem { get; set; }

        [BsonElement("location")]
        public Location Location { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("meta_data")]
        [JsonIgnore]
        public string MetaData { get; set; }

        [BsonElement("user_id")]
        public string UserId { get; set; }

        [BsonElement("user_name")]
        public string UserName { get; set; }

        [BsonElement("organization_id")]
        public string OrganizationId { get; set; }

        [BsonElement("organization_name")]
        public string OrganizationName { get; set; }

        [BsonElement("application_id")]
        public string ApplicationId { get; set; }
    }

    public class Location
    {
        [BsonElement("latitude")]
        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [BsonElement("longitude")]
        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }

        [BsonElement("geo_location")]
        [JsonPropertyName("geoLocation")]
        public string GeoLocation { get; set; }
    }

    public class OperatingSystem
    {
        [BsonElement("app_code_name")]
        [JsonPropertyName("appCodeName")]
        public string AppCodeName { get; set; }

        [BsonElement("app_name")]
        [JsonPropertyName("appName")]
        public string AppName { get; set; }

        [BsonElement("app_version")]
        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; }

        [BsonElement("app_type")]
        [JsonPropertyName("appType")]
        public string AppType { get; set; }

        [BsonElement("user_agent")]
        [JsonPropertyName("userAgent")]
        public string UserAgent { get; set; }

        [BsonElement("language")]
        [JsonPropertyName("language")]
        public string Language { get; set; }

        [BsonElement("oscpu")]
        [JsonPropertyName("oscpu")]
        public string OSCPU { get; set; }

        [BsonElement("device_memory")]
        [JsonPropertyName("deviceMemory")]
        public float? DeviceMemory { get; set; }

        [BsonElement("platform")]
        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [BsonElement("vendor")]
        [JsonPropertyName("vendor")]
        public string Vendor { get; set; }

        [BsonElement("vendor_sub")]
        [JsonPropertyName("vendorSub")]
        public string VendorSub { get; set; }

        [BsonElement("product")]
        [JsonPropertyName("product")]
        public string Product { get; set; }

        [BsonElement("product_sub")]
        [JsonPropertyName("productSub")]
        public string ProductSub { get; set; }

        [BsonElement("cookie_enable")]
        [JsonPropertyName("cookieEnable")]
        public bool? CookieEnable { get; set; }

        [BsonElement("device_type")]
        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; }

        [BsonElement("device_id")]
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [BsonElement("device_name")]
        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }
    }
}
