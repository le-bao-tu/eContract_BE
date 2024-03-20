using NetCore.Data;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NetCore.Business.Business.eContract.DocumentWFLHistory
{
    public class DocumentWFLHistoryModel
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Document Document { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public string State { get; set; }
        public string ReasonReject { get; set; }
        public string Description { get; set; }
        public string ListDocumentFileJson
        {
            get
            {
                return ListDocumentFile == null ? null : JsonSerializer.Serialize(ListDocumentFile);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ListDocumentFile = null;
                else
                    ListDocumentFile = JsonSerializer.Deserialize<List<DocumentFileWFLHistoryModel>>(value);
            }
        }
        public List<DocumentFileWFLHistoryModel> ListDocumentFile { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }

    public class DocumentFileWFLHistoryModel
    {
        public Guid DocumentFileId { get; set; }
        public string BucketName { get; set; }
        public string ObjectName { get; set; }
        public string FileName { get; set; }
        public string HashSHA256 { get; set; }
    }
}
