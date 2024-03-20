using NetCore.Data;
using NetCore.Shared;
using Serilog;
//using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class EmailHandler : IEmailHandler
    {
        public EmailConfiguration mailConfig;

        public EmailHandler()
        {
            LoadMailConfiguration();
        }

        public EmailHandler(EmailConfiguration request)
        {
            LoadCustomConfiguration(request);
        }

        private void LoadMailConfiguration()
        {
            mailConfig = new EmailConfiguration();
            mailConfig.MailConfigEnable = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_ENABLED).Equals("1");
            mailConfig.MailConfigPort = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_PORT);
            mailConfig.MailConfigFrom = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_FROM);
            mailConfig.MailConfigSmtp = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_SMTP);
            mailConfig.MailConfigSsl = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_SSL).Equals("1");
            mailConfig.MailConfigPassword = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_PASSWORD);
            mailConfig.MailConfigSendType = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_SEND_TYPE);
            mailConfig.MailConfigUser = Utils.GetConfig(MailConstants.MailConfigParameter.MAIL_CONFIG_USER);
        }

        private void LoadCustomConfiguration(EmailConfiguration request)
        {
            mailConfig = new EmailConfiguration();
            mailConfig.MailConfigEnable = request.MailConfigEnable.Equals("1");
            mailConfig.MailConfigSmtp = request.MailConfigSmtp;
            mailConfig.MailConfigPort = request.MailConfigPort;
            mailConfig.MailConfigSendType = request.MailConfigSendType;
            mailConfig.MailConfigSsl = request.MailConfigSsl.Equals("1");
            mailConfig.MailConfigFrom = request.MailConfigFrom;
            mailConfig.MailConfigUser = request.MailConfigUser;
            mailConfig.MailConfigPassword = request.MailConfigPassword;
            mailConfig.MailConfigTemplate = request.MailConfigTemplate;
        }

        private void LoadMailConfiguration(EmailAccountModel accountModel)
        {
            mailConfig = new EmailConfiguration();
            mailConfig.MailConfigPort = accountModel.Port.ToString();
            mailConfig.MailConfigFrom = accountModel.From;
            mailConfig.MailConfigSmtp = accountModel.Smtp;
            mailConfig.MailConfigSsl = accountModel.Ssl;
            mailConfig.MailConfigPassword = accountModel.Password;
            mailConfig.MailConfigSendType = accountModel.SendType;
            mailConfig.MailConfigUser = accountModel.User;
        }

        public bool SendMailExchange(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, Dictionary<string, byte[]> fileAttach)
        {
            if (!mailConfig.MailConfigEnable)
                return true;
            SmtpClient mySmtpClient = new SmtpClient();
            MailMessage mail = new MailMessage();
            try
            {
                mail.BodyEncoding = System.Text.Encoding.UTF8;

                mySmtpClient.UseDefaultCredentials = false;
                mySmtpClient.EnableSsl = mailConfig.MailConfigSsl;
                // mySmtpClient.
                // Set port
                string tempPort = mailConfig.MailConfigPort;
                int port;
                if (int.TryParse(tempPort, out port))
                {
                    mySmtpClient.Port = port;
                }

                // Set host
                string tempHost = mailConfig.MailConfigSmtp;
                if (!string.IsNullOrEmpty(tempHost))
                {
                    mySmtpClient.Host = tempHost;
                }

                // Set from
                string tempFrom = mailConfig.MailConfigFrom;
                if (!string.IsNullOrEmpty(tempFrom))
                {
                    mail.From = new MailAddress(tempFrom, mailConfig.MailConfigUser);
                }

                // Set credential password
                string tempPassword = mailConfig.MailConfigPassword;
                if (!string.IsNullOrEmpty(tempPassword) && !string.IsNullOrEmpty(mailConfig.MailConfigFrom))
                {
                    mySmtpClient.Credentials = new NetworkCredential(mailConfig.MailConfigFrom, tempPassword);
                }
                if (toEmails != null)
                {
                    foreach (string email in toEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.To.Add(email);
                        }
                    }
                }

                if (ccEmails != null)
                {
                    foreach (string email in ccEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.CC.Add(email);
                        }
                    }
                }

                if (bccEmail != null)
                {
                    foreach (string email in bccEmail)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.Bcc.Add(email);
                        }
                    }
                }

                mail.Subject = title;
                mail.Body = body;

                ServicePointManager.ServerCertificateValidationCallback +=
                    delegate (
                        object sender,
                        X509Certificate certificate,
                        X509Chain chain,
                        SslPolicyErrors sslPolicyErrors)
                    {
                        return true;
                    };

                mail.IsBodyHtml = true;
                Attachment attach;
                ContentType contentType;
                MemoryStream ms = null;
                if (fileAttach != null)
                {
                    foreach (KeyValuePair<string, byte[]> keyvalue in fileAttach)
                    {
                        if (!string.IsNullOrEmpty(keyvalue.Key) && keyvalue.Value != null && keyvalue.Value.Length > 0)
                        {
                            contentType = new ContentType(GetContentTypeFromFileName(keyvalue.Key));
                            ms = new MemoryStream(keyvalue.Value);
                            ms.Flush();
                            attach = new Attachment(ms, contentType);
                            attach.Name = GetFileNameForDisplay(keyvalue.Key);
                            mail.Attachments.Add(attach);
                        }
                    }
                }

                // Log
                Log.Information("MainConfig : " + JsonSerializer.Serialize(mailConfig));
                Log.Information("toEmails : " + JsonSerializer.Serialize(toEmails));

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.Send(mail);
                }

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.SendAsync(mail, Guid.NewGuid());
                }

                if (ms != null)
                {
                    ms.Close();
                    ms.Dispose();
                }
            }
            catch (SmtpException stmpEx)
            {
                Log.Error(stmpEx, "Send Mail Error");
                //try SendMail with no attachment
                try
                {
                    mail.Attachments.Clear();
                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.Send(mail);
                    }

                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.SendAsync(mail, Guid.NewGuid());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Send Mail Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Send Mail Error");
                return false;
            }

            return true;
        }

        public bool SendMailWithConfig(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, OrganizationConfig orgConf)
        {
            if (orgConf == null)
                return false;

            var emailConf = orgConf.EmailConfig;

            if (emailConf.Type == EmailType.GMAIL)
            {
                mailConfig = new EmailConfiguration();
                mailConfig.MailConfigPort = emailConf.Port.ToString();
                mailConfig.MailConfigFrom = emailConf.From;
                mailConfig.MailConfigSmtp = emailConf.SMTP;
                mailConfig.MailConfigSsl = emailConf.SSL.Equals("1");
                mailConfig.MailConfigPassword = emailConf.Password;
                mailConfig.MailConfigSendType = emailConf.SendType;
                mailConfig.MailConfigUser = emailConf.User;

                SmtpClient mySmtpClient = new SmtpClient();
                MailMessage mail = new MailMessage();
                try
                {
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.Priority = MailPriority.Normal;

                    mySmtpClient.UseDefaultCredentials = false;
                    mySmtpClient.EnableSsl = mailConfig.MailConfigSsl;
                    mail.From = new MailAddress(mailConfig.MailConfigFrom, mailConfig.MailConfigUser);

                    mySmtpClient.Timeout = 10000;

                    // Set port
                    int port;
                    if (int.TryParse(mailConfig.MailConfigPort, out port))
                    {
                        mySmtpClient.Port = port;
                    }

                    // Set host
                    if (!string.IsNullOrEmpty(mailConfig.MailConfigSmtp))
                    {
                        mySmtpClient.Host = mailConfig.MailConfigSmtp;
                    }

                    // Set credential password
                    if (!string.IsNullOrEmpty(mailConfig.MailConfigPassword) && !string.IsNullOrEmpty(mailConfig.MailConfigFrom))
                    {
                        mySmtpClient.Credentials = new NetworkCredential(mailConfig.MailConfigFrom, mailConfig.MailConfigPassword);
                    }
                    if (toEmails != null && toEmails.Count > 0)
                    {
                        foreach (string email in toEmails)
                        {
                            if (IsValidEmail(email))
                            {
                                mail.To.Add(email);
                            }
                        }
                    }

                    if (ccEmails != null && ccEmails.Count > 0)
                    {
                        foreach (string email in ccEmails)
                        {
                            if (IsValidEmail(email))
                            {
                                mail.CC.Add(email);
                            }
                        }
                    }

                    if (bccEmail != null && bccEmail.Count > 0)
                    {
                        foreach (string email in bccEmail)
                        {
                            if (IsValidEmail(email))
                            {
                                mail.Bcc.Add(email);
                            }
                        }
                    }

                    mail.Subject = title;
                    mail.Body = body;

                    mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, new ContentType("text/html")));

                    // Log
                    Log.Information("MainConfig : " + JsonSerializer.Serialize(mailConfig));
                    Log.Information("toEmails : " + JsonSerializer.Serialize(toEmails));

                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.Send(mail);
                    }

                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.SendAsync(mail, Guid.NewGuid());
                    }
                }
                catch (SmtpException stmpEx)
                {

                    Log.Error(stmpEx, "Send Mail Error");
                    //try SendMail with no attachment
                    try
                    {
                        mail.Attachments.Clear();
                        if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                        {
                            mySmtpClient.Send(mail);
                        }

                        if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                        {
                            mySmtpClient.SendAsync(mail, Guid.NewGuid());
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Send Mail Error");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Send Mail Error");
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SendMailGoogle(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body)
        {
            if (!mailConfig.MailConfigEnable)
                return true;
            SmtpClient mySmtpClient = new SmtpClient();
            MailMessage mail = new MailMessage();
            try
            {
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.Priority = MailPriority.Normal;

                mySmtpClient.UseDefaultCredentials = false;
                mySmtpClient.EnableSsl = mailConfig.MailConfigSsl;
                mail.From = new MailAddress(mailConfig.MailConfigFrom, mailConfig.MailConfigUser);

                mySmtpClient.Timeout = 10000;

                // Set port
                int port;
                if (int.TryParse(mailConfig.MailConfigPort, out port))
                {
                    mySmtpClient.Port = port;
                }

                // Set host
                if (!string.IsNullOrEmpty(mailConfig.MailConfigSmtp))
                {
                    mySmtpClient.Host = mailConfig.MailConfigSmtp;
                }

                // Set credential password
                if (!string.IsNullOrEmpty(mailConfig.MailConfigPassword) && !string.IsNullOrEmpty(mailConfig.MailConfigFrom))
                {
                    mySmtpClient.Credentials = new NetworkCredential(mailConfig.MailConfigFrom, mailConfig.MailConfigPassword);
                }
                if (toEmails != null && toEmails.Count > 0)
                {
                    foreach (string email in toEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.To.Add(email);
                        }
                    }
                }

                if (ccEmails != null && ccEmails.Count > 0)
                {
                    foreach (string email in ccEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.CC.Add(email);
                        }
                    }
                }

                if (bccEmail != null && bccEmail.Count > 0)
                {
                    foreach (string email in bccEmail)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.Bcc.Add(email);
                        }
                    }
                }

                mail.Subject = title;
                mail.Body = body;

                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, new ContentType("text/html")));

                // Log
                Log.Information("MainConfig : " + JsonSerializer.Serialize(mailConfig));
                Log.Information("toEmails : " + JsonSerializer.Serialize(toEmails));

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.Send(mail);
                }

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.SendAsync(mail, Guid.NewGuid());
                }
            }
            catch (SmtpException stmpEx)
            {

                Log.Error(stmpEx, "Send Mail Error");
                //try SendMail with no attachment
                try
                {
                    mail.Attachments.Clear();
                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.Send(mail);
                    }

                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.SendAsync(mail, Guid.NewGuid());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Send Mail Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Send Mail Error");
                return false;
            }

            return true;
        }

        public bool SendMailGoogle(EmailAccountModel accountModel, List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body)
        {
            LoadMailConfiguration(accountModel);
            SmtpClient mySmtpClient = new SmtpClient();
            MailMessage mail = new MailMessage();
            try
            {
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.Priority = MailPriority.Normal;

                mySmtpClient.UseDefaultCredentials = false;
                mySmtpClient.EnableSsl = mailConfig.MailConfigSsl;
                mail.From = new MailAddress(mailConfig.MailConfigFrom, mailConfig.MailConfigUser);

                mySmtpClient.Timeout = 10000;

                // Set port
                int port;
                if (int.TryParse(mailConfig.MailConfigPort, out port))
                {
                    mySmtpClient.Port = port;
                }

                // Set host
                if (!string.IsNullOrEmpty(mailConfig.MailConfigSmtp))
                {
                    mySmtpClient.Host = mailConfig.MailConfigSmtp;
                }

                // Set credential password
                if (!string.IsNullOrEmpty(mailConfig.MailConfigPassword) && !string.IsNullOrEmpty(mailConfig.MailConfigFrom))
                {
                    mySmtpClient.Credentials = new NetworkCredential(mailConfig.MailConfigFrom, mailConfig.MailConfigPassword);
                }
                if (toEmails != null && toEmails.Count > 0)
                {
                    foreach (string email in toEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.To.Add(email);
                        }
                    }
                }

                if (ccEmails != null && ccEmails.Count > 0)
                {
                    foreach (string email in ccEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.CC.Add(email);
                        }
                    }
                }

                if (bccEmail != null && bccEmail.Count > 0)
                {
                    foreach (string email in bccEmail)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.Bcc.Add(email);
                        }
                    }
                }

                mail.Subject = title;
                mail.Body = body;

                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, new ContentType("text/html")));

                // Log
                Log.Information("MainConfig : " + JsonSerializer.Serialize(mailConfig));
                Log.Information("toEmails : " + JsonSerializer.Serialize(toEmails));

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.Send(mail);
                }

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.SendAsync(mail, Guid.NewGuid());
                }
            }
            catch (SmtpException stmpEx)
            {

                Log.Error(stmpEx, "Send Mail Error");
                //try SendMail with no attachment
                try
                {
                    mail.Attachments.Clear();
                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.Send(mail);
                    }

                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.SendAsync(mail, Guid.NewGuid());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Send Mail Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Send Mail Error");
                return false;
            }

            return true;
        }

        //public async Task<bool> SendMailService(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, string emailCode = null)
        //{
        //    try
        //    {
        //        emailCode = emailCode ?? defaultEmailCode;
        //        var mailModel = new SendMailServiceModel()
        //        {
        //            ToEmails = toEmails,
        //            CcEmails = ccEmails,
        //            BccEmails = bccEmail,
        //            Title = title,
        //            Body = body,
        //            EmailCode = emailCode
        //        };

        //        using (var client = new HttpClient())
        //        {

        //            var json = JsonSerializer.Serialize(mailModel);
        //            var data = new StringContent(json, Encoding.UTF8, "application/json");
        //            var url = sendMailService + "api/v1/queue-send-email/send-mail-now";

        //            var res = await client.PostAsync(url, data);
        //            var responseText = res.Content.ReadAsStringAsync().Result;
        //            var response = JsonSerializer.Deserialize<SendMailResponseModel>(responseText);
        //            if (response.Code != 200)
        //            {
        //                Log.Error(response.Message, "Send Mail Error:" + JsonSerializer.Serialize(response));
        //            }
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "Send Mail Error");
        //        return false;
        //    }
        //}

        public bool SendMailGoogleWithQRCode(List<string> toEmails, List<string> ccEmails, List<string> bccEmail, string title, string body, string base64Image)
        {
            if (!mailConfig.MailConfigEnable)
                return true;
            SmtpClient mySmtpClient = new SmtpClient();
            MailMessage mail = new MailMessage();
            try
            {
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.Priority = MailPriority.Normal;

                mySmtpClient.UseDefaultCredentials = false;
                mySmtpClient.EnableSsl = mailConfig.MailConfigSsl;
                mail.From = new MailAddress(mailConfig.MailConfigFrom, mailConfig.MailConfigUser);

                mySmtpClient.Timeout = 10000;

                // Set port
                int port;
                if (int.TryParse(mailConfig.MailConfigPort, out port))
                {
                    mySmtpClient.Port = port;
                }

                // Set host
                if (!string.IsNullOrEmpty(mailConfig.MailConfigSmtp))
                {
                    mySmtpClient.Host = mailConfig.MailConfigSmtp;
                }

                // Set credential password
                if (!string.IsNullOrEmpty(mailConfig.MailConfigPassword) && !string.IsNullOrEmpty(mailConfig.MailConfigFrom))
                {
                    mySmtpClient.Credentials = new NetworkCredential(mailConfig.MailConfigFrom, mailConfig.MailConfigPassword);
                }
                if (toEmails != null && toEmails.Count > 0)
                {
                    foreach (string email in toEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.To.Add(email);
                        }
                    }
                }

                if (ccEmails != null && ccEmails.Count > 0)
                {
                    foreach (string email in ccEmails)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.CC.Add(email);
                        }
                    }
                }

                if (bccEmail != null && bccEmail.Count > 0)
                {
                    foreach (string email in bccEmail)
                    {
                        if (IsValidEmail(email))
                        {
                            mail.Bcc.Add(email);
                        }
                    }
                }

                mail.Subject = title;
                mail.Body = body;

                if (!string.IsNullOrEmpty(base64Image))
                {
                    var imageData = Convert.FromBase64String(base64Image);
                    var contentId = "imageUniqueId";
                    var linkedResource = new LinkedResource(new MemoryStream(imageData), "image/jpeg");
                    linkedResource.ContentId = contentId;
                    linkedResource.TransferEncoding = TransferEncoding.Base64;

                    var htmlView = AlternateView.CreateAlternateViewFromString(body, null, "text/html");
                    htmlView.LinkedResources.Add(linkedResource);
                    mail.AlternateViews.Add(htmlView);
                }

                // Log
                Log.Information("MainConfig : " + JsonSerializer.Serialize(mailConfig));
                Log.Information("toEmails : " + JsonSerializer.Serialize(toEmails));

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.Send(mail);
                }

                if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                {
                    mySmtpClient.SendAsync(mail, Guid.NewGuid());
                }
            }
            catch (SmtpException stmpEx)
            {

                Log.Error(stmpEx, "Send Mail Error");
                //try SendMail with no attachment
                try
                {
                    mail.Attachments.Clear();
                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.SYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.Send(mail);
                    }

                    if (mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.ASYNC || mailConfig.MailConfigSendType == MailConstants.MailConfigSendType.BOTH)
                    {
                        mySmtpClient.SendAsync(mail, Guid.NewGuid());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Send Mail Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Send Mail Error");
                return false;
            }

            return true;
        }

        public string GetContentTypeFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            try
            {
                string[] temp = fileName.Split(new string[] { ";#" }, StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length > 0)
                {
                    return temp[1];
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetContentTypeFromFileName Error");
            }

            return string.Empty;
        }

        public string GetFileNameForDisplay(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            try
            {
                string[] temp = fileName.Split(new string[] { ";#" }, StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length > 0)
                {
                    return temp[0];
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetFileNameForDisplay Error");
            }

            return string.Empty;
        }

        private bool IsValidEmail(string emailAddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailAddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public string GenerateDocumentSignedEmailBody(GenerateEmailBodyModel model)
        {
            StringBuilder htmlBody = new StringBuilder();
            htmlBody.Append(EmailTemplateConstanct.Header);
            htmlBody.Append("<body><table style=\"text-align:center;\"><thead><tr><td colspan=\"2\"></td></tr></thead>");
            htmlBody.Append("<tbody>");
            htmlBody.Append($"<tr><td colspan=\"2\"><p>Bạn nhận được một hồ sơ đã ký hoàn chỉnh </p></td></tr>");
            htmlBody.Append($"<tr><td style=\"text-align:left;\">Tên tài liệu:</td><td style=\"text-align:right;\">{model.DocumentName}</td></tr>");
            htmlBody.Append($"<tr><td style=\"text-align:left;\">Mã tài liệu:</td><td style=\"text-align:right;\">{model.DocumentCode}</td></tr>");
            htmlBody.Append($"<tr><td colspan=\"2\"><a class=\"button\" href=\"{model.DocumentDownloadUrl}\" target=\"_blank\" >Tải tài liệu</a></td></tr></tbody>");
            htmlBody.Append("<tfoot><tr><td colspan=\"2\">[Savis Digital] e-CONTRACT</td></tr></tfoot></table></body></html>");
            return htmlBody.ToString();
        }

        public string GenerateDocumentEmailBody(GenerateEmailBodyModel model)
        {
            StringBuilder htmlBody = new StringBuilder();
            htmlBody.Append(EmailTemplateConstanct.Header);
            htmlBody.Append("<body><table style=\"text-align:center;\"><thead><tr><td colspan=\"2\"></td></tr></thead>");
            htmlBody.Append("<tbody>");
            htmlBody.Append($"<tr><td colspan=\"2\"><p><h1> Xin chào {model.UserName}</h1></p><p>Bạn có một yêu cầu ký tài liệu từ phần mềm eContract</p></td></tr>");
            htmlBody.Append($"<tr><td style=\"text-align:left;\">Tên hợp đồng:</td><td style=\"text-align:right;\">{model.DocumentName}</td></tr>");
            htmlBody.Append($"<tr><td style=\"text-align:left;\">Mã hợp đồng:</td><td style=\"text-align:right;\">{model.DocumentCode}</td></tr>");
            if (!string.IsNullOrEmpty(model.OTP))
                htmlBody.Append($"<tr><td style=\"text-align:left;\">Mã truy cập:</td><td style=\"text-align:right;\"><b>{model.OTP}</b></td></tr>");
            htmlBody.Append($"<tr><td colspan=\"2\"><a class=\"button\" href=\"{model.DocumentUrl}\" target=\"_blank\" >Xem tài liệu</a></td></tr></tbody>");
            htmlBody.Append("<tfoot><tr><td colspan=\"2\">[Savis Digital] e-CONTRACT</td></tr></tfoot></table></body></html>");
            return htmlBody.ToString();
        }

        public string GenerateDocumentOTPEmailBody(GenerateEmailBodyModel model)
        {
            StringBuilder htmlBody = new StringBuilder();
            htmlBody.Append(EmailTemplateConstanct.Header);
            htmlBody.Append("<body><table style='text-align:center;'><thead><tr><td colspan='2'></td></tr></thead>");
            htmlBody.Append("<tbody>");
            htmlBody.Append($"<tr><td><p><h1> Xin chào {model.UserName}</h1></p><p>Mã OTP xác nhận ký tài liệu</p></td></tr>");
            htmlBody.Append($"<tr><td><b>{model.OTP}</b></td></tr>");
            htmlBody.Append("<tfoot><tr><td colspan='2'>[Savis Digital] e-CONTRACT</td></tr></tfoot></table></body></html>");
            return htmlBody.ToString();
        }       
    }

    public class EmailConfiguration
    {
        public bool MailConfigEnable { get; set; }
        public string MailConfigSmtp { get; set; }
        public string MailConfigPort { get; set; }
        public bool MailConfigSsl { get; set; }
        public string MailConfigSendType { get; set; }
        public string MailConfigFrom { get; set; }
        public string MailConfigUser { get; set; }
        public string MailConfigPassword { get; set; }
        public string MailConfigTemplate { get; set; }
    }
}