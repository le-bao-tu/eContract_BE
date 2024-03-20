using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class SendMailServiceModel
    {
        public List<string> ToEmails { get; set; }
        public List<string> CcEmails { get; set; }
        public List<string> BccEmails { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string EmailCode { get; set; }
    }

    public class SendMailResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public Guid Data { get; set; }
    }

    public class GenerateEmailBodyModel
    {
        public string UserName { get; set; }
        public string DocumentUrl { get; set; }
        public string DocumentName { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentDownloadUrl { get; set; }
        public List<string> ListFileUrl { get; set; }
        public string OTP { get; set; }
    }
}
