using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class ADSSCoreModelRequest
    {     
        public string ADSSUrl { get; set; }
        public string OriginatorId { get; set; }
        public string AppearanceId { get; set; }
        public string SignatureFieldName { get; set; }
        public string DocumentPath { get; set; }
        public string SignBy { get; set; }
        public string Alias { get; set; }
        public string Password { get; set; }
        public string ProfileId { get; set; }
        public string HandSignaturePath { get; set; }
        public string CompanyPath { get; set; }
        public string SigningReason { get; set; }
        public string SigningLocation { get; set; }
        public string ContactInfo { get; set; }
        public int? SigningPage { get; set; }
        public int? SigningArea { get; set; }      
        public int? SignatureDictionarySize { get; set; } = 51200;
        public List<string> Documents { get; set; }
    }
}
