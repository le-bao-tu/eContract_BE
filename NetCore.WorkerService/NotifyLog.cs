
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NetCore.WorkerService
{
    public class NotifyLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("document_id")]
        public string DocumentId { get; set; }

        [BsonElement("document_code")]
        public string DocumentCode { get; set; }

        [BsonElement("user_id")]
        public string UserId { get; set; }

        [BsonElement("user_name")]
        public string UserName { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("notify_type")]
        public string NotifyType { get; set; }

        [BsonElement("notify_config_type")]
        public string NotifyConfigType { get; set; }

        [BsonElement("created_date")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }
    }

    public static class NotifyType
    {
        public const string SMS = "SMS";
        public const string EMAIL = "EMAIL";
        public const string NOTIFY = "NOTIFY";
    }

    public static class NotifyConfigType
    {
        public const string REMIND = "REMIND";
        public const string EXPIRE = "EXPIRE";
    }
}
