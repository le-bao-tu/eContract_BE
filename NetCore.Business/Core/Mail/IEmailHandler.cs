using NetCore.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface IEmailHandler
    {
        bool SendMailExchange(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, Dictionary<string, byte[]> fileAttach);

        bool SendMailGoogle(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body);

        bool SendMailGoogle(EmailAccountModel accountModel, List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body);

        bool SendMailGoogleWithQRCode(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, string base64Image);

        bool SendMailWithConfig(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, OrganizationConfig orgConf);

        //Task<bool> SendMailService(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, string emailCode = null);

        string GenerateDocumentSignedEmailBody(GenerateEmailBodyModel model);

        string GenerateDocumentEmailBody(GenerateEmailBodyModel model);

        string GenerateDocumentOTPEmailBody(GenerateEmailBodyModel model);
    }
}
