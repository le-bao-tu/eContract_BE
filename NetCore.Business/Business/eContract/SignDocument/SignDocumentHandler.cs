using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NetCore.DataLog;
using Spire.Pdf;
using Spire.Pdf.General.Find;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NetCore.Business
{
    public class SignDocumentHandler : ISignDocumentHandler
    {
        private string portalWebUrl = Utils.GetConfig("Web:PortalUrl");

        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IEmailHandler _emailHandler;
        private readonly ISendSMSHandler _smsHandler;
        private readonly ISignServiceHandler _signServiceHandler;
        private readonly IDocumentHandler _documentHandler;
        private readonly IOTPHandler _otpService;
        private readonly INotifyHandler _notifyService;
        private readonly IOTPHandler _otpHandler;
        private readonly ISignHashHandler _signHashHandler;
        private readonly IWorkflowHandler _workflowHandler;
        private readonly IUserHSMAccountHandler _hsmAccountHandler;
        private readonly IUserSignConfigHandler _signConfigHandler;
        private readonly IOrganizationConfigHandler _organizationConfigHandler;
        private readonly IOrganizationHandler _organizationHandler;

        private OrganizationConfig _orgConf = null;
        MinIOService ms;
        public SignDocumentHandler
            (
            DataContext dataContext,
            ICacheService cacheService,
            IEmailHandler emailHandler,
            ISendSMSHandler smsHandler,
            ISignServiceHandler signServiceHandler,
            IDocumentHandler documentHandler,
            IOTPHandler otpService,
            INotifyHandler notifyService,
            IOTPHandler otpHandler,
            ISignHashHandler signHashHandler,
            IWorkflowHandler workflowHandler,
            IUserHSMAccountHandler hsmAccountHandler,
            IUserSignConfigHandler signConfigHandler,
            IOrganizationConfigHandler organizationConfigHandler,
            IOrganizationHandler organizationHandler
            )
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _emailHandler = emailHandler;
            _smsHandler = smsHandler;
            _signServiceHandler = signServiceHandler;
            _documentHandler = documentHandler;
            _otpService = otpService;
            _notifyService = notifyService;
            _otpHandler = otpHandler;
            _signHashHandler = signHashHandler;
            _workflowHandler = workflowHandler;
            _hsmAccountHandler = hsmAccountHandler;
            _signConfigHandler = signConfigHandler;
            _organizationConfigHandler = organizationConfigHandler;
            _organizationHandler = organizationHandler;

            ms = new MinIOService();
        }

        public async Task<Response> GetDocumentByCode(string documentCode, string account, string otp, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Lấy thông tin hợp đồng theo mã");
                //1. Lấy thông tin tài liệu
                var document = _dataContext.Document.FirstOrDefault(x => x.Code == documentCode && x.Status);
                if (document == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin tài liệu cần ký");
                }
                //Kiểm tra trạng thái hợp đồng
                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    return new ResponseError(Code.Forbidden, $"Tài liệu đã hoàn thành quy trình ký");
                }
                //Kiểm tra email, sđt
                var documentMail = document.NextStepUserEmail?.ToLower();
                var documentPhonenumber = document.NextStepUserPhoneNumber?.ToLower();
                account = account.ToLower();
                if (documentMail != account && documentPhonenumber != account)
                {
                    return new ResponseError(Code.Forbidden, $"Thông tin email/sdt không hợp lệ");
                }
                // Hoặc số điện thoại
                //var account = document.NextStepUserPhoneNumber;
                //2. Kiểm tra mã OTP
                var checkOTP = await _otpService.ValidateOTP(new ValidateOTPModel()
                {
                    UserName = document.NextStepUserName,
                    OTP = otp
                });
                if (!checkOTP)
                {
                    return new ResponseError(Code.Forbidden, $"Mã OTP không hợp lệ");
                }
                //Thông tin file tài liệu
                var docFiles = _dataContext.DocumentFile.Where(x => x.DocumentId == document.Id);
                if (docFiles == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy file tài liệu");
                }
                var listFileResult = await docFiles.Select(x => new DocumentFileResponseModel()
                {
                    DocumentFileId = x.Id,
                    FileName = x.FileName,
                    Buckename = x.FileBucketName,
                    ObjectName = x.FileObjectName
                }).ToListAsync();
                var result = new DocumentResponseModel()
                {
                    DocumentId = document.Id,
                    UserId = document.NextStepUserId,
                    UserName = document.NextStepUserName,
                    DocumentName = document.Name,
                    DocumentCode = document.Code,
                    SignType = document.NextStepSignType,
                    ListDocumentFile = listFileResult
                };
                systemLog.OrganizationId = document.OrganizationId?.ToString();
                systemLog.UserId = document.NextStepUserId?.ToString();
                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Truy cập link ký hợp đồng (SignPage): {document.Code}",
                    MetaData = ""
                });

                return new ResponseObject<DocumentResponseModel>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> RejectDocument(RejectDocumentModel data, SystemLogModel systemLog)
        {
            try
            {
                Log.Information("Từ chối ký tài liệu: " + JsonSerializer.Serialize(data));
                //1. Lấy thông tin tài liệu
                var document = await _dataContext.Document.FindAsync(data.DocumentId);
                if (document == null)
                {
                    Log.Error($"Không tìm thấy tài liệu {data.DocumentId}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin tài liệu");
                }

                if (document.NextStepUserId != data.UserId)
                {
                    Log.Error($"Thông tin tài khoản không hợp lệ userId:{data.UserId}");
                    return new ResponseError(Code.Forbidden, $"{MessageConstants.GetDataErrorMessage} - Thông tin tài khoản không hợp lệ");
                }
                //var user = await _dataContext.User.FindAsync(document.NextStepUserId);
                //if (user == null)
                //{
                //    Log.Information("Không tìm thông tin người dùng " + JsonSerializer.Serialize(document));
                //    return new ResponseError(Code.NotFound, "Không tìm thông tin người dùng");
                //}
                //Thông tin workflow
                var curUser = document.WorkFlowUser.FirstOrDefault(x => x.UserId == document.NextStepUserId);
                //Lưu thông tin
                document.Status = false;
                document.DocumentStatus = DocumentStatus.CANCEL;
                curUser.RejectReason = data.RejectReason;
                curUser.RejectAtDate = DateTime.Now;
                var index = document.WorkFlowUser.IndexOf(curUser);
                document.WorkFlowUser[index] = curUser;
                _dataContext.Document.Update(document);

                #region Lưu lịch sử khi hợp đồng thay đổi
                await _documentHandler.CreateDocumentWFLHistory(document);
                #endregion

                int save = await _dataContext.SaveChangesAsync();
                if (save == 0)
                {
                    Log.Information("Từ chối ký tài liệu không thành công " + JsonSerializer.Serialize(document));
                    return new ResponseError(Code.ServerError, "Từ chối ký tài liệu không thành công");
                }
                //Gửi thông báo cho hệ thống Khách hàng
                var document3rdId = 0;
                int.TryParse(document.Document3rdId, out document3rdId);
                var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);
                if (documentType == null)
                {
                    Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(document));
                    return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
                }
                var notify = new NotifyDocumentModel()
                {
                    Id = document3rdId,
                    DocumentTypeCode = documentType.Code,
                    DocumentCode = document.Code,
                    DocumentWorkflowStatus = DocumentStatus.CANCEL,
                    Note = $"Tài liệu có mã {document.Code} đã bị từ chối ký bởi {curUser.UserFullName}"
                };

                systemLog.OrganizationId = document.OrganizationId?.ToString();

                if (document.OrganizationId.HasValue)
                {
                    var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog);
                }
                Log.Information("Từ chối ký tài liệu thành công: " + JsonSerializer.Serialize(document));
                return new ResponseObject<string>($"Từ chối ký hợp đồng {document.Code} thành công", MessageConstants.UpdateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetDocumentBatchInfo(string documentBatchCode, int fileUrlExpireSeconds, SystemLogModel systemLog)
        {
            try
            {
                //00. Lấy thông tin lô hợp đồng
                var documentBatch = await _dataContext.DocumentBatch.FirstOrDefaultAsync(x => x.Code == documentBatchCode && x.OrganizationId == new Guid(systemLog.OrganizationId));
                if (documentBatch == null)
                {
                    Log.Error($"Không tìm thấy lô hợp đồng với mã {documentBatchCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy lô hợp đồng với mã {documentBatchCode}");
                }

                //1. Lấy thông tin tài liệu
                var listDocument = await _dataContext.Document.Where(x => x.DocumentBatchId == documentBatch.Id).ToListAsync();
                if (listDocument == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy lô hợp đồng với mã {documentBatchCode}");
                }

                var listRS = new List<DocumentSignedResponseModel>();
                var ms = new MinIOService();

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT_BATCH,
                    ObjectId = documentBatch.Id.ToString(),
                    Description = $"Lấy thông tin lô hợp đồng từ bên thứ 3: {documentBatch.Code}",
                    MetaData = ""
                });

                foreach (var document in listDocument)
                {
                    //Kiểm tra quy trình kys
                    var workflow = document.WorkFlowUser;
                    var checkSign = workflow.Any(x => x.SignAtDate != null);
                    var docStatus = document.DocumentStatus;
                    var documentWorkflowStatus = !checkSign ? DocumentStatus.DRAFT : docStatus;

                    //Thông tin file tài liệu
                    var docFile = _dataContext.DocumentFile.OrderByDescending(x => x.CreatedDate).FirstOrDefault(x => x.DocumentId == document.Id);
                    if (docFile == null)
                    {
                        Log.Error($"Không tìm thấy file tài liệu {document.Code}");
                        return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file tài liệu");
                    }

                    var fileUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName, fileUrlExpireSeconds);

                    var result = new DocumentSignedResponseModel()
                    {
                        Id = document.Document3rdId,
                        Name = document.Name,
                        DocumentCode = document.Code,
                        DocumentWorkflowStatus = documentWorkflowStatus,
                        FileUrl = fileUrl,
                        WorkFlowUser = AutoMapperUtils.AutoMap<WorkFlowUserDocumentModel, DocumentSignedWorkFlowUser>(workflow)
                    };
                    listRS.Add(result);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = document.Id.ToString(),
                        Description = $"Lấy thông tin hợp đồng từ bên thứ 3: {document.Code}",
                        MetaData = JsonSerializer.Serialize(result)
                    });
                }

                return new ResponseObject<List<DocumentSignedResponseModel>>(listRS, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetDocumentInfo(string documentCode, int fileUrlExpireSeconds, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Lấy thông tin hợp đồng từ 3rd App DocumentCode: {documentCode} với thời gian sống của file là {fileUrlExpireSeconds}");
                //1. Lấy thông tin tài liệu
                var document = await _dataContext.Document.AsNoTracking().FirstOrDefaultAsync(x => x.Code == documentCode && x.OrganizationId == new Guid(systemLog.OrganizationId));
                if (document == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy tài liệu {documentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin tài liệu có mã {documentCode}");
                }
                //Kiểm tra quy trình kys
                var workflow = document.WorkFlowUser;
                //var checkSign = workflow.Any(x => x.SignAtDate != null);
                //var docStatus = document.DocumentStatus;
                //var documentWorkflowStatus = !checkSign ? DocumentStatus.DRAFT : docStatus;

                var docType =    _dataContext.DocumentType.AsNoTracking().FirstOrDefault(x => x.Id == document.DocumentTypeId);

                //Thông tin file tài liệu
                var docFile = _dataContext.DocumentFile.OrderByDescending(x => x.CreatedDate).FirstOrDefault(x => x.DocumentId == document.Id);
                if (docFile == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy file tài liệu {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file tài liệu");
                }
                var ms = new MinIOService();
                var fileUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName, fileUrlExpireSeconds);

                var result = new DocumentSignedResponseModel()
                {
                    Id = document.Document3rdId,
                    Document3rdId = document.Document3rdId,
                    Name = document.Name,
                    //DocumentName = document.Name,
                    DocumentTypeCode = docType.Code,
                    DocumentTypeName = docType.Name,
                    DocumentCode = document.Code,
                    DocumentWorkflowStatus = document.DocumentStatus,
                    FileUrl = fileUrl,
                    State = document.State,
                    WorkFlowUser = AutoMapperUtils.AutoMap<WorkFlowUserDocumentModel, DocumentSignedWorkFlowUser>(workflow)
                };

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Lấy thông tin hợp đồng từ bên thứ 3: {document.Code}",
                    MetaData = JsonSerializer.Serialize(result)
                });

                return new ResponseObject<DocumentSignedResponseModel>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetAccessLink(string documentCode, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Cấp lại link truy cập tài liệu {documentCode}");
                //1. Lấy thông tin tài liệu
                var document = await _dataContext.Document.FirstOrDefaultAsync(x => x.Code == documentCode);
                if (document == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy tài liệu {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin tài liệu");
                }
                //Kiểm tra tài liệu đã hoàn thành quy trình hay chưa
                var workFlow = document.WorkFlowUser;
                var checkWF = workFlow.Where(x => x.SignAtDate != null);
                if (checkWF.Count() > 0)
                {
                    Log.Error($"{systemLog.TraceId} - Tài liệu đang trong quy trình ký: {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Tài liệu đang trong quy trình ký");
                }
                var otp = await _otpService.GenerateOTP(document.NextStepUserName);
                var result = new ResponseAccessLinkModel()
                {
                    DocumentCode = document.Code,
                    Url = $"{portalWebUrl}validate-otp?code={document.Code}&email={document.NextStepUserEmail}&otp={otp}"
                };

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Lấy lại link truy cập hợp đồng để ký từ bên thứ 3: {document.Code}",
                    MetaData = JsonSerializer.Serialize(result)
                });

                return new ResponseObject<ResponseAccessLinkModel>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> ResendAccessLink(string documentCode, SystemLogModel systemLog)
        {
            try
            {
                // Nếu chưa có thì tiến hành bổ sung thông tin
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    Log.Information($"{systemLog.TraceId} - Đơn vị chưa được cấu hình thông tin kết nối");
                    return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối");
                }

                Log.Information($"{systemLog.TraceId} - Gửi lại link truy cập tài liệu {documentCode}");
                //1. Lấy thông tin tài liệu
                var document = _dataContext.Document.FirstOrDefault(x => x.Code == documentCode);
                if (document == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy tài liệu {documentCode}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.ErrorLogMessage} - Không tìm thấy thông tin tài liệu");
                }
                //Kiểm tra trạng thái hợp đồng
                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    return new ResponseError(Code.Forbidden, $"- Tài liệu đã hoàn thành quy trình ký");
                }
                //OTP
                var otp = await _otpService.GenerateOTP(document.NextStepUserName);
                //Tạo URL
                var url = $"{portalWebUrl}validate-otp?code={document.Code}&email={document.NextStepUserEmail}";
                //Gửi link tài liệu vừa tạo và mã OTP qua mail
                var toEmails = new List<string>()
                                {
                                    document.NextStepUserEmail
                                };
                string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

                var curUser = document.WorkFlowUser.FirstOrDefault(x => x.UserId == document.NextStepUserId);

                var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
                {
                    UserName = curUser.UserFullName,
                    DocumentName = document.Name,
                    DocumentCode = document.Code,
                    DocumentUrl = url,
                    OTP = otp
                });
                var sendMail = _emailHandler.SendMailWithConfig(toEmails, null, null, title, body, _orgConf);
                var result = new ResponseAccessLinkModel
                {
                    Url = url,
                    DocumentCode = document.Code
                };

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Gửi lại link truy cập hợp đồng qua email từ bên thứ 3: {document.Code}",
                    MetaData = JsonSerializer.Serialize(result)
                });

                return new ResponseObject<ResponseAccessLinkModel>(result, $"Gửi link truy cập thành công cho email/sđt: {document.NextStepUserEmail}", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SendOTP(string documentCode, SystemLogModel systemLog, bool isEmail = false)
        {
            try
            {
                //1. Lấy thông tin tài liệu
                var document = _dataContext.Document.FirstOrDefault(x => x.Code == documentCode);
                if (document == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy thông tin tài liệu {documentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin tài liệu");
                }
                //Kiểm tra trạng thái hợp đồng
                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    return new ResponseError(Code.Forbidden, $"Tài liệu đã hoàn thành quy trình ký");
                }
                if (document.DocumentStatus == DocumentStatus.CANCEL)
                {
                    return new ResponseError(Code.Forbidden, $"Tài liệu đã bị hủy");
                }

                // Kiểm tra người dùng có đăng ký smart OTP chưa
                var currentUser = await _dataContext.User.Where(x => x.Id == document.NextStepUserId && !x.IsDeleted).FirstOrDefaultAsync();

                if (currentUser == null)
                {
                    return new ResponseError(Code.Forbidden, $"Không tìm thấy người dùng xử lý tài liệu");
                }
                else if (currentUser.IsEnableSmartOTP == true)
                {
                    return new ResponseError(Code.Success, $"Vui lòng mở ứng dụng SmartOTP và lấy OTP");
                }

                //2. Lấy mã OTP
                var otp = await _otpService.GenerateOTP(currentUser.UserName);

                systemLog.OrganizationId = document.OrganizationId?.ToString();
                systemLog.TempObjectCode = CacheConstants.DOCUMENT;
                systemLog.TempObjectId = document.Id.ToString();

                //Lấy cấu hình gửi đơn vị
                if (!document.OrganizationId.HasValue)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin đơn vị đang thực hiện ký");
                }
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(document.OrganizationId.Value);
                }
                if (_orgConf == null)
                {
                    return new ResponseError(Code.NotFound, $"Đơn vị chưa được cấu hình thông tin");
                }
                if (!isEmail && _orgConf.SMSConfig != null && _orgConf.SMSConfig.Service == SMSService.VSMS && !string.IsNullOrEmpty(currentUser.PhoneNumber))
                {
                    var smsData = new SendSMSModel()
                    {
                        OrganizationId = document.OrganizationId ?? Guid.Empty,
                        UserId = document.NextStepUserId ?? Guid.Empty,
                        PhoneNumber = currentUser.PhoneNumber,
                        Message = Utils.BuildStringFromTemplate(_orgConf.SMSOTPTemplate, new string[] { otp })
                    };
                    var sendSMS = await _smsHandler.SendSMS(smsData, _orgConf, systemLog);
                    if (!sendSMS)
                    {
                        return new ResponseError(Code.ServerError, $"Gửi sms không thành công cho sdt: { document.NextStepUserPhoneNumber }");
                    }
                    return new ResponseObject<bool>(true, $"Gửi mã OTP thành công cho sđt: { document.NextStepUserPhoneNumber }", Code.Success);
                }
                else
                {
                    if (string.IsNullOrEmpty(currentUser.Email))
                    {
                        return new ResponseError(Code.ServerError, $"Tài khoản {currentUser.Name} không có thông tin email");
                    }
                    string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";
                    var body = _emailHandler.GenerateDocumentOTPEmailBody(new GenerateEmailBodyModel()
                    {
                        UserName = currentUser.Name,
                        DocumentName = document.Name,
                        DocumentCode = document.Code,
                        OTP = otp
                    });
                    var toEmails = new List<string>()
                    {
                        document.NextStepUserEmail
                    };

                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(new Guid(systemLog.OrganizationId));
                    var sendNotify = await _notifyService.SendNotificationFromNotifyConfig(new NotificationConfigModel() 
                    { 
                        TraceId = systemLog.TraceId,
                        OraganizationCode = rootOrg.Code,
                        IsSendEmail = true,
                        IsSendNotification = false,
                        IsSendSMS = false,
                        ListEmail = toEmails,
                        EmailTitle = title,
                        EmailContent = body
                    });
                    if (sendNotify.Code != Code.Success)
                    {
                        return new ResponseError(Code.ServerError, $"Gửi email không thành công");
                    }
                    //var sendMail = await _emailHandler.SendMailWithConfig(toEmails, null, null, title, body, _orgConf);
                    //if (!sendMail)
                    //{
                    //    return new ResponseError(Code.ServerError, $"Gửi email không thành công");
                    //}
                    return new ResponseObject<bool>(true, $"Gửi mã OTP thành công cho email: { document.NextStepUserEmail }", Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage, ex);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        //public async Task<Response> SignDocumentDigital(string code, string otp, string signatureBase64, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        //1. Lấy thông tin tài liệu
        //        var document = await _dataContext.Document.FirstOrDefaultAsync(x => x.Code == code);
        //        if (document == null)
        //        {
        //            Log.Error($"{systemLog.TraceId} - Không tìm thấy tài liệu mã {code}");
        //            return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin tài liệu");
        //        }
        //        //Kiểm tra trạng thái hợp đồng
        //        if (document.DocumentStatus == DocumentStatus.FINISH)
        //        {
        //            return new ResponseError(Code.Forbidden, $"{MessageConstants.GetDataErrorMessage} - Tài liệu đã hoàn thành quy trình ký");
        //        }
        //        //2. Kiểm tra mã OTP
        //        var documentMail = document.NextStepUserEmail;
        //        var documentPhonenumber = document.NextStepUserPhoneNumber;
        //        var checkOTP = await _otpService.ValidateOTP(
        //            new ValidateOTPModel()
        //            {
        //                UserName = document.NextStepUserName,
        //                OTP = otp
        //            });
        //        if (!checkOTP)
        //        {
        //            Log.Information($"{systemLog.TraceId} - Mã OTP không hợp lệ {code}");
        //            return new ResponseError(Code.ServerError, $"Mã OTP không hợp lệ");
        //        }
        //        //Ký Hình ảnh
        //        var workFlowDocumentProcessing = new WorkflowDocumentProcessingModel()
        //        {
        //            ListDocumentId = new List<Guid>() { document.Id },
        //            Base64Image = signatureBase64
        //        };
        //        var signResult = await ProcessingWorkflow(workFlowDocumentProcessing);

        //        systemLog.UserId = document.NextStepUserId?.ToString();
        //        systemLog.OrganizationId = document.OrganizationId?.ToString();

        //        //return new ResponseObject<bool>(true, $"Ký thành công tài liệu {code }", Code.Success);
        //        return signResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
        //    }
        //}

        //public async Task<Response> SignMultileDocumentDigital(List<Guid> listDocumentId, string otp, string signatureBase64, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        //TODO: Check UserId
        //        if (listDocumentId.Count() == 0)
        //        {
        //            return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
        //        }
        //        var document = await _dataContext.Document.FindAsync(listDocumentId[0]);
        //        //1. Kiểm tra mã OTP
        //        var curUser = document.NextStepUserName;
        //        var checkOTP = await _otpService.ValidateOTP(
        //            new ValidateOTPModel()
        //            {
        //                UserName = curUser,
        //                OTP = otp
        //            });
        //        if (!checkOTP)
        //        {
        //            return new ResponseError(Code.ServerError, $"Mã OTP không hợp lệ");
        //        }
        //        //Ký Hình ảnh
        //        var workFlowDocumentProcessing = new WorkflowDocumentProcessingModel()
        //        {
        //            ListDocumentId = listDocumentId,
        //            Base64Image = signatureBase64
        //        };
        //        var signResult = await ProcessingWorkflow(workFlowDocumentProcessing);

        //        systemLog.UserId = document.NextStepUserId?.ToString();
        //        systemLog.OrganizationId = document.OrganizationId?.ToString();

        //        return signResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
        //    }
        //}

        //public async Task<Response> SignMultileDocumentDigitalFor3rd(string userConnectId, List<string> listDocumentCode, string otp, string signatureBase64, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        if (listDocumentCode.Count() == 0)
        //        {
        //            return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
        //        }

        //        var orgId = new Guid(systemLog.OrganizationId);
        //        var user = await _dataContext.User.Where(x => x.ConnectId == userConnectId && x.OrganizationId == orgId).FirstOrDefaultAsync();

        //        if (user == null)
        //        {
        //            return new ResponseError(Code.NotFound, $"UserConnectId không tồn tại.");
        //        }

        //        //1. Kiểm tra mã OTP
        //        var checkOTP = await _otpService.ValidateOTP(
        //            new ValidateOTPModel()
        //            {
        //                UserName = user.UserName,
        //                OTP = otp
        //            });

        //        if (!checkOTP)
        //        {
        //            return new ResponseError(Code.Forbidden, $"Mã OTP không hợp lệ");
        //        }

        //        var listDocument = await _dataContext.Document.Where(x => listDocumentCode.Contains(x.Code) && x.NextStepUserId == user.Id).ToListAsync();

        //        if (listDocument.Count != listDocumentCode.Count)
        //        {
        //            return new ResponseError(Code.Forbidden, $"Người dùng không có quyền truy cập các hợp đồng.");
        //        }

        //        //Ký Hình ảnh
        //        var workFlowDocumentProcessing = new WorkflowDocumentProcessingModel()
        //        {
        //            ListDocumentId = listDocument.Select(x => x.Id).ToList(),
        //            Base64Image = signatureBase64
        //        };
        //        var signResult = await ProcessingWorkflow(workFlowDocumentProcessing);

        //        systemLog.UserId = user.Id.ToString();
        //        if (signResult.Code == Code.Success && signResult is ResponseObject<List<WorkflowDocumentSignReponseModel>> resultData)
        //        {
        //            var rs = new List<WorkflowDocumentSignFor3rdReponseModel>();
        //            if (resultData.Data != null)
        //            {
        //                foreach (var item in resultData.Data)
        //                {
        //                    rs.Add(new WorkflowDocumentSignFor3rdReponseModel()
        //                    {
        //                        DocumentCode = item.DocumentCode,
        //                        IsSuccess = item.IsSuccess,
        //                        Message = item.Message
        //                    });
        //                }

        //                return new ResponseObject<List<WorkflowDocumentSignFor3rdReponseModel>>(rs, signResult.Message, Code.Success);
        //            }
        //            else
        //            {
        //                return new ResponseError(Code.ServerError, $"Không thể ký file");
        //            }
        //        }
        //        else
        //        {
        //            return signResult;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
        //    }
        //}

        public async Task<Response> SignMultileDocumentDigitalFor3rdV2(SignDocumentMultileFor3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Ký hợp đồng từ 3rd App: " + JsonSerializer.Serialize(model));
                if (model.ListDocumentCode.Count() == 0)
                {
                    Log.Information($"{systemLog.TraceId} - Danh sách hợp đồng trống");
                    return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                }

                if (string.IsNullOrEmpty(model.UserConnectId))
                {
                    Log.Information($"{systemLog.TraceId} - Thiếu thông tin người dùng UserConnectId");
                    return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                }

                var orgId = new Guid(systemLog.OrganizationId);
                var user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower() && x.OrganizationId == orgId).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                    return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                }
                var listDocument = await _dataContext.Document.Where(x => model.ListDocumentCode.Contains(x.Code) && x.NextStepUserId == user.Id).ToListAsync();
                if (listDocument.Count != model.ListDocumentCode.Count)
                {
                    Log.Information($"{systemLog.TraceId} - Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                    return new ResponseError(Code.Forbidden, $"Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                }
                var listDocumentId = listDocument.Select(x => x.Id).ToList();

                #region Resize image ekyc
                if (model.EKYCMemoryStream != null)
                {
                    Image image = System.Drawing.Image.FromStream(model.EKYCMemoryStream, true);

                    var height = image.Height;
                    var width = image.Width;
                    if (height < width)
                    {
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }

                    Bitmap b = new Bitmap(image);

                    Image i = resizeImage(b, new Size(200, 200));

                    model.EKYCImageBase64 = this.ImageToBase64(i);
                    Log.Information($"{systemLog.TraceId} - Ảnh EKYC Base64 sau resize: {model.EKYCImageBase64}");
                }
                else if (!string.IsNullOrEmpty(model.EKYCImageBase64))
                {
                    Image image = this.Base64ToImage(model.EKYCImageBase64);

                    var height = image.Height;
                    var width = image.Width;
                    if (height < width)
                    {
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);

                    }

                    Bitmap b = new Bitmap(image);

                    Image i = resizeImage(b, new Size(200, 200));

                    model.EKYCImageBase64 = this.ImageToBase64(i);
                }
                #endregion

                var appearance = new SignAppearanceModel()
                {
                    ImageData = model.SignatureBase64,
                    Logo = model.EKYCImageBase64,
                    Detail = "6,7,",
                    ScaleImage = string.IsNullOrEmpty(model.SignatureBase64) ? 0 : (float)0.45,
                    ScaleLogo = string.IsNullOrEmpty(model.EKYCImageBase64) ? 0 : (float)1,
                    ScaleText = (float)1,
                    Reason = model.SignatureDetail.Reason
                };
                if (model.SignatureDetail != null)
                {
                    appearance.Detail = "";
                    appearance.Reason += $" (CCCD/CMND: {user.IdentityNumber}, OTP: {model.OTP})";
                    //vào lúc {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}
                    if (model.SignatureDetail.IsShowUserName)
                    {
                        appearance.Detail += $"1:{user.Name},";
                    }
                    if (model.SignatureDetail.IsShowEmail)
                    {
                        appearance.Detail += $"4,";
                        appearance.Mail = user.Email;
                    }
                    if (model.SignatureDetail.IsShowPhoneNumber)
                    {
                        appearance.Detail += $"5,";
                        appearance.Phone = user.PhoneNumber;
                    }
                    appearance.Detail += $"6,7,";
                    appearance.SignLocation = model.Location?.GeoLocation;
                    //appearance.ScaleImage = (float)0.45;
                    //appearance.ScaleText = (float)1;
                }

                var signModel = new ElectronicSignClientModel()
                {
                    ListDocumentId = listDocumentId,
                    OTP = model.OTP,
                    IsFileUrlReturn = model.IsFileUrlReturn,
                    Appearance = appearance
                };

                if (model.Location != null)
                {
                    systemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, DataLog.Location>(model.Location);
                }
                if (model.DeviceInfo != null)
                {
                    systemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);
                }

                var signResult = await _signHashHandler.ElectronicSignFiles(signModel, systemLog, user.Id);
                if (signResult.Code == Code.Success && signResult is ResponseObject<List<DocumentSignedResult>> resultData)
                {
                    var rs = new List<WorkflowDocumentSignFor3rdReponseModel>();
                    if (resultData.Data != null)
                    {
                        foreach (var item in resultData.Data)
                        {
                            var documentCode = listDocument.FirstOrDefault(x => x.Id == item.DocumentId)?.Code;

                            var dt = new WorkflowDocumentSignFor3rdReponseModel()
                            {
                                DocumentCode = documentCode,
                                IsSuccess = item.Message == MessageConstants.SignSuccess,
                                Message = item.Message
                            };

                            if (model.IsFileUrlReturn)
                            {
                                //TODO: Tạm lấy file đầu tiên
                                var docFile = item.ListFileSignedResult.FirstOrDefault();
                                if (docFile != null)
                                {
                                    dt.FileUrl = await ms.GetObjectPresignUrlAsync(docFile.BucketNameSigned, docFile.ObjectNameSigned); ;
                                }
                            }
                            rs.Add(dt);
                        }

                        return new ResponseObject<List<WorkflowDocumentSignFor3rdReponseModel>>(rs, signResult.Message, Code.Success);
                    }
                    else
                    {
                        Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi ký tài liệu - ElectronicSignFiles - Data ký = null");
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi ký tài liệu");
                    }
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi ký tài liệu - {signResult.Message}");
                    return new ResponseError(Code.ServerError, signResult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }

        public async Task<Response> GetOTPDocumentFor3rd(List<string> listDocumentCode, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Lấy OTP theo hợp đồng {JsonSerializer.Serialize(listDocumentCode)}");
                if (listDocumentCode.Count() == 0)
                {
                    Log.Information($"{systemLog.TraceId} - Danh sách hợp đồng đang trống");
                    return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                }

                var listDocument = await _dataContext.Document.Where(x => listDocumentCode.Contains(x.Code)).ToListAsync();

                if (listDocument.Count != listDocumentCode.Count)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng theo danh sách yêu cầu.");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy hợp đồng theo danh sách yêu cầu.");
                }

                var listOTP = new List<OTPByDocumentModel>();
                var temp = new OTPByDocumentModel();

                foreach (var item in listDocument)
                {
                    temp.DocumentCode = item.Code;

                    if (!string.IsNullOrEmpty(item.NextStepUserName))
                    {
                        temp.OTP = await _otpHandler.GenerateOTP(item.NextStepUserName);
                    }

                    listOTP.Add(temp);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = item.Id.ToString(),
                        Description = $"Lấy OTP truy cập hợp đồng từ hệ thống thứ 3 cho người dùng {item.NextStepUserName}",
                        MetaData = temp.OTP
                    });
                }

                return new ResponseObject<List<OTPByDocumentModel>>(listOTP, "Lấy OTP theo hợp đồng thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SendMailOTPDocumentFor3rd(List<string> listDocumentCode, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Send mail OTP theo hợp đồng {JsonSerializer.Serialize(listDocumentCode)}");
                if (listDocumentCode.Count() == 0)
                {
                    return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                }

                var listDocument = await _dataContext.Document.Where(x => listDocumentCode.Contains(x.Code) && x.OrganizationId == new Guid(systemLog.OrganizationId)).ToListAsync();

                if (listDocument.Count != listDocumentCode.Count)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng theo danh sách yêu cầu.");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy hợp đồng theo danh sách yêu cầu.");
                }

                //Lấy cấu hình gửi đơn vị
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    Log.Information($"{systemLog.TraceId} - Đơn vị chưa được cấu hình thông tin kết nối.");
                    return new ResponseError(Code.NotFound, $"Đơn vị chưa được cấu hình thông tin kết nối");
                }

                var listOTP = new List<OTPByDocumentModel>();
                var temp = new OTPByDocumentModel();

                foreach (var item in listDocument)
                {
                    if (!string.IsNullOrEmpty(item.NextStepUserEmail) && !string.IsNullOrEmpty(item.NextStepUserName))
                    {
                        temp.DocumentCode = item.Code;

                        temp.OTP = await _otpHandler.GenerateOTP(item.NextStepUserName);

                        listOTP.Add(temp);

                        string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";
                        var body = _emailHandler.GenerateDocumentOTPEmailBody(new GenerateEmailBodyModel()
                        {
                            UserName = item.NextStepUserName,
                            DocumentName = item.Name,
                            DocumentCode = item.Code,
                            OTP = temp.OTP
                        });
                        var toEmails = new List<string>()
                            {
                                item.NextStepUserEmail
                            };
                        var sendMail = _emailHandler.SendMailWithConfig(toEmails, null, null, title, body, _orgConf);

                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.Id.ToString(),
                            Description = $"Gửi OTP truy cập hợp đồng cho người dùng {item.NextStepUserName} qua email {item.NextStepUserEmail}",
                            MetaData = temp.OTP
                        });
                    }
                }

                // Gửi mail cho người dùng

                return new ResponseObject<bool>(true, "Gửi OTP cho người dùng qua mail thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SendSMSOTPDocumentFor3rd(List<string> listDocumentCode, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Gửi SMS OTP theo hợp đồng {JsonSerializer.Serialize(listDocumentCode)}");
                if (listDocumentCode.Count() == 0)
                {
                    return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                }

                //Lấy cấu hình gửi đơn vị
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    return new ResponseError(Code.NotFound, $"Đơn vị chưa được cấu hình thông tin");
                }

                var listDocument = await _dataContext.Document.Where(x => listDocumentCode.Contains(x.Code) && x.OrganizationId == new Guid(systemLog.OrganizationId)).ToListAsync();

                if (listDocument.Count != listDocumentCode.Count)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng theo danh sách yêu cầu.");
                    return new ResponseError(Code.Forbidden, $"Không tìm thấy hợp đồng theo danh sách yêu cầu.");
                }

                var temp = new OTPByDocumentModel();

                var listUserId = listDocument.Select(x => x.NextStepUserId);
                var listUser = await _dataContext.User.Where(x => listUserId.Contains(x.Id)).Select(x => new { UserId = x.Id, PhoneNumber = x.PhoneNumber }).ToListAsync();

                foreach (var item in listDocument)
                {
                    systemLog.TempObjectCode = CacheConstants.DOCUMENT;
                    systemLog.TempObjectId = item.Id.ToString();
                    if (!string.IsNullOrEmpty(item.NextStepUserName))
                    {
                        var currentUser = listUser.Where(x => x.UserId == item.NextStepUserId).FirstOrDefault();

                        if (currentUser == null)
                        {
                            continue;
                        }

                        temp.DocumentCode = item.Code;

                        //var otp = await _otpHandler.GenerateOTP(item.NextStepUserName);

                        var otp = await _otpHandler.GenerateHOTPFromService(new HOTPRequestModel()
                        {
                            AppRequest = "eContract",
                            ObjectId = item.NextStepUserId.ToString(),
                            UserName = item.NextStepUserName,
                            HOTPSize = 6,
                            Step = 300
                        }, systemLog);

                        if (otp == null)
                        {
                            Log.Error($"{systemLog.TraceId} - OTP GenerateHOTPFromService is null");
                            return new ResponseError(Code.ServerError, $"Không tạo được OTP");
                        }

                        var smsData = new SendSMSModel()
                        {
                            OrganizationId = item.OrganizationId ?? Guid.Empty,
                            UserId = item.NextStepUserId ?? Guid.Empty,
                            PhoneNumber = currentUser.PhoneNumber,
                            Message = Utils.BuildStringFromTemplate(_orgConf.SMSOTPTemplate, new string[] { otp.OTP })
                        };
                        var sendSMS = await _smsHandler.SendSMS(smsData, _orgConf, systemLog);
                        if (!sendSMS)
                        {
                            return new ResponseObject<bool>(false, "Gửi OTP cho người dùng qua sms thất bại do không kết nối được service", Code.Success);
                        }

                    }
                }

                return new ResponseObject<bool>(true, "Gửi OTP cho người dùng qua sms thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SendMailOTPUserFor3rd(OTPUserRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Send mail OTP theo người dùng qua email {JsonSerializer.Serialize(model)}");
                if (model == null && string.IsNullOrEmpty(model.UserConnectId))
                {
                    return new ResponseError(Code.NotFound, $"Thông tin người dùng đang để trống.");
                }

                var user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower() && x.OrganizationId == new Guid(systemLog.OrganizationId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng.");
                    return new ResponseError(Code.NotFound, $"Tài khoản với thông tin {model.UserConnectId} chưa được đăng ký.");
                }

                //Lấy cấu hình gửi đơn vị
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    Log.Information($"{systemLog.TraceId} - Đơn vị chưa được cấu hình thông tin kết nối.");
                    return new ResponseError(Code.NotFound, $"Đơn vị chưa được cấu hình thông tin kết nối");
                }

                if (!string.IsNullOrEmpty(user.Email))
                {
                    var otp = await _otpHandler.GenerateOTP(user.UserName);

                    string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";
                    var body = _emailHandler.GenerateDocumentOTPEmailBody(new GenerateEmailBodyModel()
                    {
                        UserName = user.Name,
                        DocumentName = "",
                        DocumentCode = "",
                        OTP = otp
                    });
                    var toEmails = new List<string>()
                            {
                                user.Email
                            };
                    var sendMail = _emailHandler.SendMailWithConfig(toEmails, null, null, title, body, _orgConf);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = CacheConstants.USER,
                        ObjectId = user.Id.ToString(),
                        Description = $"Gửi OTP cho người dùng {user.Name} qua email {user.Email}",
                        MetaData = otp
                    });
                    return new ResponseObject<bool>(true, "Gửi OTP cho người dùng qua mail thành công", Code.Success);
                }
                else
                {
                    return new ResponseObject<bool>(false, "Tài khoản người dùng chưa được cấu hình thông tin email", Code.ServerError);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SendSMSOTPUserFor3rd(OTPUserRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Send mail OTP theo người dùng qua SMS {JsonSerializer.Serialize(model)}");
                if (model == null && string.IsNullOrEmpty(model.UserConnectId))
                {
                    return new ResponseError(Code.NotFound, $"Thông tin người dùng đang để trống.");
                }

                var user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower() && x.OrganizationId == new Guid(systemLog.OrganizationId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng.");
                    return new ResponseError(Code.NotFound, $"Tài khoản với thông tin {model.UserConnectId} chưa được đăng ký.");
                }

                //Lấy cấu hình gửi đơn vị
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    Log.Information($"{systemLog.TraceId} - Đơn vị chưa được cấu hình thông tin kết nối.");
                    return new ResponseError(Code.NotFound, $"Đơn vị chưa được cấu hình thông tin kết nối");
                }

                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var otpModel = await _otpHandler.GenerateHOTPFromService(new HOTPRequestModel()
                    {
                        AppRequest = "eContract",
                        ObjectId = user.Id.ToString(),
                        UserName = user.UserName,
                        HOTPSize = 6,
                        Step = 300
                    }, systemLog);

                    var otp = otpModel.OTP;

                    var smsData = new SendSMSModel()
                    {
                        OrganizationId = user.OrganizationId ?? Guid.Empty,
                        UserId = user.Id,
                        PhoneNumber = user.PhoneNumber,
                        Message = Utils.BuildStringFromTemplate(_orgConf.SMSOTPTemplate, new string[] { otp })
                    };
                    var sendSMS = await _smsHandler.SendSMS(smsData, _orgConf, systemLog);
                    if (sendSMS)
                    {
                        return new ResponseObject<bool>(true, "Gửi OTP cho người dùng qua sms thành công", Code.Success);
                    }
                    else
                    {
                        return new ResponseObject<bool>(false, "Gửi OTP cho người dùng qua sms thất bại", Code.ServerError);
                    }
                }
                else
                {
                    return new ResponseObject<bool>(false, "Tài khoản khách hàng chưa có thông tin số điện thoại", Code.ServerError);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        //public async Task<Response> SignDocumentHSM(string documentCode, string userPin, string base64Image, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        //1. Lấy thông tin tài liệu
        //        var document = await _dataContext.Document.FirstOrDefaultAsync(x => x.Code == documentCode);
        //        if (document == null)
        //        {
        //            Log.Error($"{systemLog.TraceId} - Không tìm thấy tài liệu mã {documentCode}");
        //            return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin tài liệu");
        //        }
        //        //Kiểm tra trạng thái hợp đồng
        //        if (document.DocumentStatus == DocumentStatus.FINISH)
        //        {
        //            return new ResponseError(Code.Forbidden, $"{MessageConstants.GetDataErrorMessage} - Tài liệu đã hoàn thành quy trình ký");
        //        }
        //        //Ký HSM
        //        var workFlowDocumentProcessing = new WorkflowDocumentProcessingModel()
        //        {
        //            ListDocumentId = new List<Guid>() { document.Id },
        //            Base64Image = base64Image
        //        };
        //        var signResult = await ProcessingWorkflow(workFlowDocumentProcessing, document.NextStepUserId, userPin, true);

        //        systemLog.UserId = document.NextStepUserId?.ToString();
        //        systemLog.OrganizationId = document.OrganizationId?.ToString();

        //        return signResult;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
        //    }
        //}

        //public async Task<Response> SignDocumentUsbToken(string documentCode, string fileBase64, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        //1. Lấy thông tin tài liệu
        //        var document = _dataContext.Document.FirstOrDefault(x => x.Code == documentCode);
        //        if (document == null)
        //        {
        //            Log.Error($"{systemLog.TraceId} - Không tìm thấy tài liệu mã {documentCode}");
        //            return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin tài liệu");
        //        }
        //        //Kiểm tra trạng thái hợp đồng
        //        if (document.DocumentStatus == DocumentStatus.FINISH)
        //        {
        //            return new ResponseError(Code.Forbidden, $"{MessageConstants.GetDataErrorMessage} - Tài liệu đã hoàn thành quy trình ký");
        //        }
        //        //TODO kiểm tra chữ ký trong file
        //        //Convert Base64 to MemoryStream
        //        if (fileBase64.IndexOf(',') >= 0)
        //            fileBase64 = fileBase64.Substring(fileBase64.IndexOf(',') + 1);
        //        var bytes = Convert.FromBase64String(fileBase64);
        //        var memory = new MemoryStream(bytes);
        //        //Xử lý quy trình
        //        var pWF = await ProcessingWorkflowSignUsbToken(document, memory);

        //        systemLog.UserId = document.NextStepUserId?.ToString();
        //        systemLog.OrganizationId = document.OrganizationId?.ToString();

        //        return pWF;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
        //    }
        //}

        //public async Task<Response> SignMultileDocumentUsbToken(List<SignDocumentUsbTokenDataModel> model, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        //TODO: Check User
        //        if (model.Count() == 0)
        //        {
        //            return new ResponseError(Code.Forbidden, $"Dữ liệu hợp đồng trống");
        //        }
        //        //TODO kiểm tra chữ ký trong file
        //        var pWF = await ProcessingWorkflowSignUsbTokenV2(model, systemLog);
        //        return pWF;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
        //    }
        //}

        //private async Task<Response> ProcessingWorkflow(WorkflowDocumentProcessingModel model, Guid? userId = null, string userPin = "", bool isSignHSM = false)
        //{
        //    try
        //    {
        //        #region Lấy thông tin ký 
        //        var certAlias = string.Empty;
        //        var certUserPin = string.Empty;
        //        var certSlotLabel = string.Empty;
        //        // #TODO:Lấy Alias 
        //        if (isSignHSM)
        //        {
        //            //certAlias = await GetAliasDefault(userId.Value);
        //            //if (string.IsNullOrEmpty(certAlias))
        //            //{
        //            //    return new ResponseError(Code.ServerError, $"Chưa cấu hình thông tin chứ ký");
        //            //}
        //            certUserPin = "123";
        //            certAlias = "ecertify";
        //        }

        //        #endregion

        //        var responseSignModel = new List<WorkflowDocumentSignReponseModel>();

        //        foreach (var documentId in model.ListDocumentId)
        //        {
        //            var document = await _dataContext.Document.Where(c => c.Id == documentId).FirstOrDefaultAsync();

        //            var listFile = await _dataContext.DocumentFile.Where(c => c.DocumentId == documentId).ToListAsync();

        //            // Lấy danh sách liên hệ
        //            var lstUser = document.WorkFlowUser;

        //            // Lấy thông tin người ký tại bước hiện tại
        //            var currentStepUser = lstUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();

        //            // #TODO: Check valid user
        //            //------------------------
        //            //------------------------

        //            // Gắn cờ flag cho ký cả document
        //            // Ký document thành công khi tất cả file trong document đều thành công
        //            bool flag = false;
        //            var fileResult = new SignFileModel();
        //            foreach (var file in listFile)
        //            {
        //                var templateId = file.DocumentFileTemplateId;

        //                // Lấy template của document
        //                var documentTemplate = await _dataContext.DocumentFileTemplate.Where(c => c.Id == templateId).FirstOrDefaultAsync();

        //                if (documentTemplate == null)
        //                    return new ResponseObject<bool>(false, "Không tìm thấy template", Code.ServerError);

        //                // Lấy danh sách vùng ký 
        //                var lstMetaDataConfig = documentTemplate.MetaDataConfig.Where(c => !string.IsNullOrEmpty(c.FixCode)).ToList();

        //                // Lấy thứ tự ký
        //                var stepOrder = lstUser.FindIndex(c => c.Id == document.NextStepId);

        //                // Lấy vùng ký tại bước hiện tại
        //                if (lstMetaDataConfig.Count == 0)
        //                {
        //                    flag = true;

        //                    responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                    {
        //                        DocumentId = documentId,
        //                        DocumentCode = document.Code,
        //                        DocumentName = document.Name,
        //                        IsSuccess = false,
        //                        Message = $"Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký"
        //                    });

        //                    break;
        //                }

        //                // #TODO: MAP USER VS METADATACONFIG SIGN
        //                //------------------------
        //                //------------------------

        //                // Danh sách loại ký theo quy trình
        //                var lstSignTypeOfUser = lstUser.Select(c => c.Type).ToArray();

        //                // Kiểm tra xem đã ký qua loại gì rồi :)))
        //                var lstSignedType = lstSignTypeOfUser.Skip(0).Take(stepOrder).ToArray();

        //                // Danh sách Meta Data Config còn lại
        //                var lstMetaDataConfigRemain = new List<MetaDataConfig>();
        //                foreach (var item in lstMetaDataConfig)
        //                {
        //                    lstMetaDataConfigRemain.Add(item);
        //                }

        //                // Danh sách thứ tự vùng ký loại bỏ
        //                var lstOrderToRemove = new List<int>();

        //                // Loại bỏ những vùng đã ký trong biểu mẫu
        //                if (lstSignedType.Length > 0)
        //                {
        //                    foreach (var signType in lstSignedType)
        //                    {
        //                        for (int i = 0; i < lstMetaDataConfig.Count; i++)
        //                        {
        //                            if (lstMetaDataConfig[i].SignType == signType)
        //                            {
        //                                // Nếu tồn tại số đó trong lstOrderToRemove rồi thì continue
        //                                if (lstOrderToRemove.Contains(i))
        //                                    continue;
        //                                // Nếu chưa thì add vào lstRemove
        //                                lstOrderToRemove.Add(i);
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }

        //                // Loại bỏ
        //                if (lstOrderToRemove.Count > 0)
        //                {
        //                    foreach (int indice in lstOrderToRemove.OrderByDescending(v => v))
        //                    {
        //                        lstMetaDataConfigRemain.RemoveAt(indice);
        //                    }
        //                }

        //                // Kiểu ký tại bước hiện tại
        //                var currentSignType = lstUser[stepOrder].Type;

        //                // Lấy vùng ký giống kiểu ký hiện tại được cấu hình trước
        //                var metaDataConfig = lstMetaDataConfigRemain.Where(c => c.SignType == currentSignType).FirstOrDefault();

        //                // Nếu không tìm được kiểu ký phù hợp
        //                if (metaDataConfig == null)
        //                {
        //                    flag = true;

        //                    responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                    {
        //                        DocumentId = documentId,
        //                        DocumentCode = document.Code,
        //                        DocumentName = document.Name,
        //                        IsSuccess = false,
        //                        Message = $"Kiểm tra lại cấu hình vùng ký"
        //                    });

        //                    break;
        //                }

        //                #region Convert tọa độ và ký file
        //                // Convert tọa độ từ vùng ký trong biểu mẫu
        //                var convertResult = await ConvertCoordinateFile(metaDataConfig, file, model.Base64Image, null);

        //                if (convertResult.Code == Code.Success && convertResult is ResponseObject<DataInputSignPDF> resultData)
        //                {
        //                    // Thực hiện ký
        //                    // Gán thông tin ký
        //                    resultData.Data.CertAlias = certAlias;
        //                    resultData.Data.CertUserPin = certUserPin;
        //                    resultData.Data.CertSlotLabel = certSlotLabel;

        //                    var resultSigned = new ResponseObject<SignFileModel>(null);
        //                    if (isSignHSM)
        //                    {
        //                        resultSigned = await _signServiceHandler.SignBySigningBox(resultData.Data, userId, model.SignType);
        //                    }
        //                    else
        //                    {
        //                        resultData.Data.Mail = document.Email;
        //                        resultSigned = await _signServiceHandler.ElectronicSigning(resultData.Data);
        //                    }

        //                    if (resultSigned.Code == Code.Success && resultSigned is ResponseObject<SignFileModel> resultDataSigned)
        //                    {
        //                        var fileAfterSigned = resultDataSigned.Data;
        //                        // Lưu log vào bảng document sign history
        //                        await _dataContext.DocumentSignHistory.AddAsync(new DocumentSignHistory()
        //                        {
        //                            Id = Guid.NewGuid(),
        //                            CreatedDate = DateTime.Now,
        //                            Description = "",
        //                            DocumentId = document.Id,
        //                            DocumentFileId = file.Id,
        //                            FileType = file.FileType,
        //                            OldFileBucketName = file.FileBucketName,
        //                            OldFileName = file.FileName,
        //                            OldFileObjectName = file.FileObjectName,
        //                            OldHashFile = file.HashFile,
        //                            OldXMLFile = file.XMLFile,
        //                            NewFileBucketName = fileAfterSigned.FileBucketName,
        //                            NewFileName = fileAfterSigned.FileName,
        //                            NewFileObjectName = fileAfterSigned.FileObjectName,
        //                            NewHashFile = fileAfterSigned.NewHashFile,
        //                            NewXMLFile = fileAfterSigned.NewXMLFile,
        //                        }); ;

        //                        // Cập nhật document-file
        //                        file.FileBucketName = fileAfterSigned.FileBucketName;
        //                        // file.FileName = fileAfterSigned.FileName;
        //                        file.FileObjectName = fileAfterSigned.FileObjectName;

        //                        _dataContext.DocumentFile.Update(file);
        //                        //Thêm vào danh sách file đã ký
        //                        fileResult = fileAfterSigned;
        //                    }
        //                    else
        //                    {
        //                        flag = true;
        //                        responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                        {
        //                            DocumentId = documentId,
        //                            DocumentCode = document.Code,
        //                            DocumentName = document.Name,
        //                            IsSuccess = false,
        //                            Message = $"Sai thông tin cấu hình chữ ký hoặc service ký không hoạt động"
        //                        });
        //                        Log.Information($"SignDocument: Sai thông tin cấu hình chữ ký hoặc service ký không hoạt động {document.Code}");
        //                        Log.Information($"resultSigned: {JsonSerializer.Serialize(resultSigned)}");
        //                        break;
        //                    }
        //                }
        //                #endregion
        //            }

        //            // Nếu flag = true (Ký thất bại) thì nhảy sang document khác ký
        //            if (flag) continue;


        //            #region Cập nhật bảng Document
        //            // Lấy số thứ tự bước hiện tại
        //            var stepOrderUpdate = lstUser.FindIndex(c => c.Id == document.NextStepId);

        //            // Cập nhật nextstep
        //            if (lstUser.Count - 1 > stepOrderUpdate)
        //            {
        //                document.NextStepId = lstUser[stepOrderUpdate + 1].Id;
        //                document.NextStepUserId = lstUser[stepOrderUpdate + 1].UserId;
        //                document.NextStepUserName = lstUser[stepOrderUpdate + 1].UserName;
        //                document.NextStepUserEmail = lstUser[stepOrderUpdate + 1].UserEmail;
        //                document.NextStepSignType = lstUser[stepOrderUpdate + 1].Type;
        //            }
        //            else
        //            {
        //                document.DocumentStatus = DocumentStatus.FINISH;
        //                document.NextStepId = null;
        //                document.NextStepUserId = null;
        //                document.NextStepUserName = null;
        //                document.NextStepUserEmail = null;
        //            }

        //            // Cập nhật signed Date
        //            document.WorkFlowUser.Where(c => c.Id == currentStepUser.Id).FirstOrDefault().SignAtDate = DateTime.Now;

        //            // Cập nhật lại ngày xử lý file
        //            document.ModifiedDate = DateTime.Now;

        //            // Lưu 
        //            _dataContext.Document.Update(document);
        //            #endregion  

        //            // Save vào database
        //            int dbSave = await _dataContext.SaveChangesAsync();
        //            if (dbSave > 0)
        //            {
        //                Log.Information("Ký document" + document.Id + " thành công");

        //                responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                {
        //                    DocumentId = documentId,
        //                    DocumentCode = document.Code,
        //                    DocumentName = document.Name,
        //                    SignFile = fileResult,
        //                    IsSuccess = true,
        //                    Message = $"Ký thành công"
        //                });

        //                // Gửi mail cho người kế tiếp
        //                if (lstUser.Count - 1 > stepOrderUpdate)
        //                {
        //                    //SendMailRemindSign(currentStepUser.UserEmail, lstuser[stepOrderUpdate + 1].UserEmail, lstUser[stepOrderUpdate + 1].UserName);
        //                    string url = null;
        //                    //Lấy mã OTP
        //                    var account = document.NextStepUserName;
        //                    //TODO: Hoặc số điện thoại
        //                    var otp = await _otpService.GenerateOTP(account);
        //                    //Tạo URL
        //                    url = $"{portalWebUrl}validate-otp?code={document.Code}&email={document.NextStepUserEmail}";
        //                    //Gửi link tài liệu vừa tạo và mã OTP qua mail
        //                    var toEmails = new List<string>()
        //                        {
        //                            document.NextStepUserEmail
        //                        };

        //                    string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

        //                    var curUser = document.WorkFlowUser.FirstOrDefault(x => x.UserId == document.NextStepUserId);

        //                    var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
        //                    {
        //                        UserName = curUser.UserFullName,
        //                        DocumentName = document.Name,
        //                        DocuemtCode = document.Code,
        //                        DocumentUrl = url,
        //                        OTP = otp
        //                    });
        //                    var sendMail = _emailHandler.SendMailGoogle(toEmails, null, null, title, body);
        //                    if (!sendMail)
        //                    {
        //                        Log.Error($"Xử lý quy trình: Gửi email không thành công: {document.Code}");
        //                    }
        //                    //Gửi thông báo cho hệ thống Khách hàng
        //                    var document3rdId = 0;
        //                    int.TryParse(document.Document3rdId, out document3rdId);
        //                    var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);
        //                    if (documentType == null)
        //                    {
        //                        Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(document));
        //                        return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
        //                    }
        //                    var notify = new NotifyDocumentModel()
        //                    {
        //                        Id = document3rdId,
        //                        DocumentTypeCode = documentType.Code,
        //                        DocumentCode = document.Code,
        //                        DocumentWorkflowStatus = DocumentStatus.PROCESSING,
        //                        Note = $"Tài liệu có mã {document.Code} đã được ký bởi {currentStepUser.UserFullName}"
        //                    };
        //                    var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId, notify);
        //                }
        //                else
        //                {
        //                    //Gửi link tài liệu vừa tạo và mã OTP qua mai
        //                    //TODO: lấy link donwload theo thời gian
        //                    var ms = new MinIOService();
        //                    var fileDownloadUrl = await ms.GetObjectPresignUrlAsync(listFile[0].FileBucketName, listFile[0].FileObjectName);
        //                    //var fileDownloadUrl = $"{minio_service_url}api/v1/core/minio/download-object?bucketName={listFile[0].FileBucketName}&objectName={listFile[0].FileObjectName}";
        //                    var toEmails = lstUser.Select(x => x.UserEmail).ToList();

        //                    string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

        //                    var body = _emailHandler.GenerateDocumentSignedEmailBody(new GenerateEmailBodyModel()
        //                    {
        //                        UserName = currentStepUser.UserFullName,
        //                        DocumentName = document.Name,
        //                        DocuemtCode = document.Code,
        //                        DocumentDownloadUrl = fileDownloadUrl,
        //                    });
        //                    var sendMail = _emailHandler.SendMailGoogle(toEmails, null, null, title, body);
        //                    if (!sendMail)
        //                    {
        //                        Log.Error($"Xử lý quy trình: Gửi email không thành công: {document.Code}");
        //                    }
        //                    //Gửi thông báo cho hệ thống Khách hàng
        //                    var document3rdId = 0;
        //                    int.TryParse(document.Document3rdId, out document3rdId);
        //                    var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);
        //                    if (documentType == null)
        //                    {
        //                        Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(document));
        //                        return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
        //                    }
        //                    var notify = new NotifyDocumentModel()
        //                    {
        //                        Id = document3rdId,
        //                        DocumentTypeCode = documentType.Code,
        //                        DocumentCode = document.Code,
        //                        DocumentWorkflowStatus = DocumentStatus.FINISH,
        //                        Note = $"Tài liệu có mã {document.Code} đã hoàn thành quy trình ký"
        //                    };
        //                    var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId, notify);
        //                }
        //            }
        //            else
        //            {
        //                Log.Information($"SignDocument: Không cập nhật được Database {document.Code}");
        //                responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                {
        //                    DocumentId = documentId,
        //                    DocumentCode = document.Code,
        //                    DocumentName = document.Name,
        //                    IsSuccess = true,
        //                    Message = $"Không cập nhật được Database"
        //                });
        //            }
        //        }

        //        Log.Information($"SignDocument: {JsonSerializer.Serialize(responseSignModel)}");
        //        return new ResponseObject<List<WorkflowDocumentSignReponseModel>>(responseSignModel, "Ký thành công", Code.Success);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{ex.Message}");
        //    }
        //}

        private async Task<Response> ConvertCoordinateFile(MetaDataConfig metaDataConfig, DocumentFile file, string base64Image, SystemLogModel systemLog)
        {
            try
            {
                // Model File ký 
                var signFile = new SignFileModel();
                signFile.FileBucketName = file.FileBucketName;
                signFile.FileName = file.FileName;
                signFile.FileObjectName = file.FileObjectName;

                //// Tải file từ MDM để tính kích thước tran
                //using (HttpClientHandler clientHandler = new HttpClientHandler())
                //{
                //    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                //    clientHandler.ServerCertificateCustomValidationCallback =
                //        (sender, cert, chain, sslPolicyErrors) => { return true; };

                //    using (var client = new HttpClient())
                //    {
                MemoryStream memoryStream;
                try
                {
                    var ms = new MinIOService();
                    memoryStream = await ms.DownloadObjectAsync(file.FileBucketName, file.FileObjectName);
                    memoryStream.Position = 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Không thể xác định được file đã lưu trữ");
                    return new ResponseError(Code.ServerError, "Không thể xác định được file đã lưu trữ");
                }


                // SpirePDF load from memory stream
                Spire.Pdf.PdfDocument pdf = new Spire.Pdf.PdfDocument();
                //Load the PDF file
                pdf.LoadFromStream(memoryStream);

                PdfPageBase page1 = pdf.Pages[metaDataConfig.Page - 1];

                // Tính lại tọa độ
                // Tính kích thước trên iTextSharp
                var pageHeight = page1.Size.Height; // Chiều cao PDF
                var pageWidth = page1.Size.Width; // Chiều rộng PDF

                // Tỷ lệ trên Frontend
                var temp = (float)pageHeight / (float)metaDataConfig.PageHeight;

                var itemHeight = (float)metaDataConfig.Height * temp; // Độ cao object
                var itemWidth = (float)metaDataConfig.Width * temp;  // Độ rộng object
                var itemLLY = (float)(0 - metaDataConfig.LLY - 9) * temp - itemHeight;  // Tọa độ LLY 
                var itemLLX = (float)(metaDataConfig.LLX - 9) * temp; // Tọa độ LLX
                var itemURX = itemLLX + itemWidth;
                var itemURY = itemLLY + itemHeight;

                //Check lại tọa độ (LLX, LLY) phải nhỏ hơn (URX, URY)
                if (itemLLX > itemURX)
                {
                    var temp1 = itemLLX;
                    itemLLX = itemURX;
                    itemURX = temp1;
                }
                if (itemLLY > itemURY)
                {
                    var temp1 = itemURY;
                    itemURY = itemLLY;
                    itemLLY = temp1;
                }

                // Model thông tin ký
                var dataInputSignPDF = new DataInputSignPDF();
                dataInputSignPDF.FileInfo = signFile;
                dataInputSignPDF.Llx = itemLLX.ToString();
                dataInputSignPDF.Lly = itemLLY.ToString();
                dataInputSignPDF.Location = "Việt Nam";
                dataInputSignPDF.Page = metaDataConfig.Page.ToString();
                dataInputSignPDF.Reason = "Đồng ý phê duyệt";
                dataInputSignPDF.Urx = itemURX.ToString();
                dataInputSignPDF.Ury = itemURY.ToString();
                dataInputSignPDF.Base64Image = base64Image.Replace("data:image/png;base64,", string.Empty);

                // Kiểm tra vùng ký động và có text để lấy ra tọa độ
                if (metaDataConfig.IsDynamicPosition && !string.IsNullOrEmpty(metaDataConfig.TextAnchor))
                {
                    if (metaDataConfig.FromPage == 0)
                    {
                        metaDataConfig.FromPage = metaDataConfig.Page;
                    }
                    // Tọa độ ký
                    PdfTextFind[] results = null;
                    int pageNumber = 0;
                    int textFindedPosition = 0;
                    foreach (PdfPageBase page in pdf.Pages)
                    {
                        pageNumber++;
                        // Nếu chưa phải đến trang chỉ định thì bỏ qua
                        if (pageNumber >= metaDataConfig.FromPage)
                        {
                            results = page.FindText(metaDataConfig.TextAnchor, TextFindParameter.IgnoreCase).Finds;
                            foreach (PdfTextFind text in results)
                            {
                                textFindedPosition++;
                                if (textFindedPosition == metaDataConfig.TextFindedPosition)
                                {
                                    System.Drawing.PointF p = text.Position;

                                    // Lấy được tọa độ là tọa độ ngay trên đỉnh của chữ
                                    // => LLX = p.X; URY = p.Y
                                    // DynamicFromAnchorLLX phải < 0 thì dữ liệu mới đẩy sang trái
                                    itemLLX = (float)p.X + (float)metaDataConfig.DynamicFromAnchorLLX;

                                    // DynamicFromAnchorLLX phải > 0 thì dữ liệu mới đẩy lên trên
                                    itemURY = page.Size.Height - (float)p.Y + (float)metaDataConfig.DynamicFromAnchorLLY;


                                    // DynamicFromAnchorLLX phải > 0 thì dữ liệu mới đẩy lên trên
                                    itemLLY = itemURY - itemHeight;  // Tọa độ LLY 
                                    itemURX = itemLLX + itemWidth;

                                    dataInputSignPDF.Page = pageNumber.ToString();

                                    dataInputSignPDF.Llx = itemLLX.ToString();
                                    dataInputSignPDF.Lly = itemLLY.ToString();
                                    dataInputSignPDF.Urx = itemURX.ToString();
                                    dataInputSignPDF.Ury = itemURY.ToString();
                                    break;
                                }
                            }
                        }
                    }
                }


                return new ResponseObject<DataInputSignPDF>(dataInputSignPDF, MessageConstants.GetDataSuccessMessage, Code.Success);
                //    }
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw new Exception("Không lấy được tọa độ vùng ký của biểu mẫu");
            }
        }

        //private async Task<Response> ProcessingWorkflowSignUsbToken(Document doc, MemoryStream data)
        //{
        //    try
        //    {
        //        //Lưu file
        //        var ms = new MinIOService();
        //        var fileName = doc.Code;
        //        var fileAfterSigned = await ms.UploadObjectAsync(null, fileName, data);
        //        //Cập nhật thông tin file
        //        var file = _dataContext.DocumentFile.FirstOrDefault(x => x.DocumentId == doc.Id);

        //        // Lưu log vào bảng document sign history
        //        await _dataContext.DocumentSignHistory.AddAsync(new DocumentSignHistory()
        //        {
        //            Id = Guid.NewGuid(),
        //            CreatedDate = DateTime.Now,
        //            Description = "",
        //            DocumentId = doc.Id,
        //            DocumentFileId = file.Id,
        //            FileType = file.FileType,
        //            OldFileBucketName = file.FileBucketName,
        //            OldFileName = file.FileName,
        //            OldFileObjectName = file.FileObjectName,
        //            OldXMLFile = file.XMLFile,
        //            OldHashFile = file.HashFile,
        //            NewFileBucketName = fileAfterSigned.BucketName,
        //            NewFileName = fileAfterSigned.FileName,
        //            NewFileObjectName = fileAfterSigned.ObjectName,
        //            NewHashFile = "",
        //            NewXMLFile = "",
        //        });
        //        //Lưu thông tin File tài liệu
        //        file.FileBucketName = fileAfterSigned.BucketName;
        //        file.FileObjectName = fileAfterSigned.ObjectName;
        //        file.FileName = fileAfterSigned.FileName;
        //        _dataContext.DocumentFile.Update(file);

        //        //Cập nhật tài liệu
        //        var wfUser = doc.WorkFlowUser;
        //        //Người ký hiện tại
        //        var curUser = wfUser.FirstOrDefault(x => x.Id == doc.NextStepId);
        //        var index = wfUser.IndexOf(curUser);
        //        wfUser[index].SignAtDate = DateTime.Now;
        //        //
        //        bool isFinish = index == wfUser.Count() - 1;
        //        if (isFinish)
        //        {
        //            //Cập nhật tài liệu
        //            doc.NextStepId = null;
        //            doc.NextStepUserId = null;
        //            doc.NextStepUserName = null;
        //            doc.NextStepUserEmail = null;
        //            doc.NextStepUserPhoneNumber = null;
        //            doc.DocumentStatus = DocumentStatus.FINISH;
        //            doc.ModifiedDate = DateTime.Now;
        //            doc.WorkFlowUser = wfUser;
        //            _dataContext.Document.Update(doc);
        //        }
        //        else  //Tài liệu đã hoàn thành quy trình ký
        //        {
        //            //Cập nhật tài liệu
        //            doc.NextStepId = wfUser[index + 1].Id;
        //            doc.NextStepUserId = wfUser[index + 1].UserId;
        //            doc.NextStepUserName = wfUser[index + 1].UserName;
        //            doc.NextStepUserEmail = wfUser[index + 1].UserEmail;
        //            doc.NextStepUserPhoneNumber = wfUser[index + 1].UserPhoneNumber;
        //            doc.NextStepSignType = wfUser[index + 1].Type;
        //            doc.WorkFlowUser = wfUser;
        //            doc.ModifiedDate = DateTime.Now;
        //            _dataContext.Document.Update(doc);
        //        }
        //        //TODO: Lưu lịch sử ký
        //        var responseSignModel = new List<WorkflowDocumentSignReponseModel>();
        //        int dbSave = await _dataContext.SaveChangesAsync();
        //        if (dbSave > 0)
        //        {
        //            Log.Information($"Xử lý quy trình: Cập nhật thông tin tài liệu thành công");
        //            responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //            {
        //                DocumentId = doc.Id,
        //                DocumentCode = doc.Code,
        //                DocumentName = doc.Name,
        //                IsSuccess = true,
        //                Message = $"Ký thành công"
        //            });
        //        }
        //        else
        //        {
        //            Log.Error($"Xử lý quy trình: Cập nhật thông tin tài liệu không thành công");
        //            responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //            {
        //                DocumentId = doc.Id,
        //                DocumentCode = doc.Code,
        //                DocumentName = doc.Name,
        //                IsSuccess = true,
        //                Message = $"Không cập nhật được Database"
        //            });
        //        }
        //        //TODO: Gửi mail
        //        if (isFinish)
        //        {
        //            string url = null;

        //            //Gửi link tài liệu vừa tạo và mã OTP qua mail
        //            var fileDownloadUrl = await ms.GetObjectPresignUrlAsync(file.FileBucketName, file.FileObjectName);
        //            //var fileDownloadUrl = $"{minio_service_url}api/v1/core/minio/download-object?bucketName={file.FileBucketName}&objectName={file.FileObjectName}";
        //            var toEmails = wfUser.Select(x => x.UserEmail).ToList();
        //            string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

        //            var body = _emailHandler.GenerateDocumentSignedEmailBody(new GenerateEmailBodyModel()
        //            {
        //                UserName = curUser.UserFullName,
        //                DocumentName = doc.Name,
        //                DocuemtCode = doc.Code,
        //                DocumentDownloadUrl = fileDownloadUrl,
        //            });
        //            var sendMail = _emailHandler.SendMailGoogle(toEmails, null, null, title, body);
        //            if (!sendMail)
        //            {
        //                Log.Error($"Xử lý quy trình: Gửi email không thành công: {doc.Code}");
        //            }
        //            //TODO: Gửi link tài liệu vừa tạo và mã OTP qua số điện thoại
        //            //-Gọi service gửi SMS
        //            //Gửi thông báo cho hệ thống Khách hàng  
        //            var document3rdId = 0;
        //            int.TryParse(doc.Document3rdId, out document3rdId);
        //            var documentType = await _dataContext.DocumentType.FindAsync(doc.DocumentTypeId);
        //            if (documentType == null)
        //            {
        //                Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(doc));
        //                return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
        //            }
        //            var notify = new NotifyDocumentModel()
        //            {
        //                Id = document3rdId,
        //                DocumentTypeCode = documentType.Code,
        //                DocumentCode = doc.Code,
        //                DocumentWorkflowStatus = DocumentStatus.FINISH,
        //                Note = $"Tài liệu có mã {doc.Code} đã hoàn thành quy trình ký"
        //            };
        //            var sendNotify = await _notifyService.SendNotifyDocumentStatus(doc.OrganizationId, notify);
        //        }
        //        else
        //        {
        //            string url = null;
        //            //Lấy mã OTP
        //            var account = doc.NextStepUserName;
        //            //var account = document.NextStepUserPhoneNumber;
        //            var otp = await _otpService.GenerateOTP(account);
        //            //Tạo URL
        //            url = $"{portalWebUrl}validate-otp?code={doc.Code}&email={doc.NextStepUserEmail}";
        //            //Gửi link tài liệu vừa tạo và mã OTP qua mail
        //            var toEmails = new List<string>()
        //            {
        //                doc.NextStepUserEmail
        //            };

        //            string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

        //            var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
        //            {
        //                UserName = curUser.UserFullName,
        //                DocumentName = doc.Name,
        //                DocuemtCode = doc.Code,
        //                DocumentUrl = url,
        //                OTP = otp
        //            });
        //            var sendMail = _emailHandler.SendMailGoogle(toEmails, null, null, title, body);
        //            if (!sendMail)
        //            {
        //                Log.Error($"Xử lý quy trình: Gửi email không thành công: {doc.Code}");
        //            }
        //            var document3rdId = 0;
        //            int.TryParse(doc.Document3rdId, out document3rdId);
        //            var documentType = await _dataContext.DocumentType.FindAsync(doc.DocumentTypeId);
        //            if (documentType == null)
        //            {
        //                Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(doc));
        //                return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
        //            }
        //            var notify = new NotifyDocumentModel()
        //            {
        //                Id = document3rdId,
        //                DocumentTypeCode = documentType.Code,
        //                DocumentCode = doc.Code,
        //                DocumentWorkflowStatus = DocumentStatus.PROCESSING,
        //                Note = $"Tài liệu có mã {doc.Code} đã được ký bởi {curUser.UserFullName}"
        //            };
        //            var sendNotify = await _notifyService.SendNotifyDocumentStatus(doc.OrganizationId, notify);
        //        }
        //        return new ResponseObject<List<WorkflowDocumentSignReponseModel>>(responseSignModel, MessageConstants.CreateSuccessMessage, Code.Success);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{ex.Message}");
        //    }
        //}

        //private async Task<Response> ProcessingWorkflowSignUsbTokenV2(List<SignDocumentUsbTokenDataModel> model, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        var responseSignModel = new List<WorkflowDocumentSignReponseModel>();
        //        foreach (var data in model)
        //        {
        //            var doc = await _dataContext.Document.FindAsync(data.DocumentId);
        //            //Lưu file
        //            var fileAfterSigned = new MinIOFileUploadResult();
        //            try
        //            {
        //                var ms = new MinIOService();
        //                var fileName = doc.Code;
        //                var fileBase64 = data.FileBase64;
        //                if (fileBase64.IndexOf(',') >= 0)
        //                    fileBase64 = fileBase64.Substring(fileBase64.IndexOf(',') + 1);
        //                var bytes = Convert.FromBase64String(fileBase64);
        //                var memory = new MemoryStream(bytes);
        //                fileAfterSigned = await ms.UploadObjectAsync(null, fileName, memory);
        //            }
        //            catch (Exception ex)
        //            {
        //                Log.Error("Lưu file đã ký không thành công", ex);
        //                responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                {
        //                    DocumentId = doc.Id,
        //                    DocumentCode = doc.Code,
        //                    DocumentName = doc.Name,
        //                    IsSuccess = false,
        //                    Message = $"Lưu file đã ký không thành công"
        //                });
        //                continue;
        //            }
        //            //Cập nhật thông tin file
        //            var file = _dataContext.DocumentFile.FirstOrDefault(x => x.DocumentId == doc.Id);

        //            // Lưu log vào bảng document sign history
        //            await _dataContext.DocumentSignHistory.AddAsync(new DocumentSignHistory()
        //            {
        //                Id = Guid.NewGuid(),
        //                CreatedDate = DateTime.Now,
        //                Description = "",
        //                DocumentId = doc.Id,
        //                DocumentFileId = file.Id,
        //                FileType = file.FileType,
        //                OldFileBucketName = file.FileBucketName,
        //                OldFileName = file.FileName,
        //                OldFileObjectName = file.FileObjectName,
        //                OldXMLFile = file.XMLFile,
        //                OldHashFile = file.HashFile,
        //                NewFileBucketName = fileAfterSigned.BucketName,
        //                NewFileName = fileAfterSigned.FileName,
        //                NewFileObjectName = fileAfterSigned.ObjectName,
        //                NewHashFile = "",
        //                NewXMLFile = "",
        //            });
        //            //Lưu thông tin File tài liệu
        //            file.FileBucketName = fileAfterSigned.BucketName;
        //            file.FileObjectName = fileAfterSigned.ObjectName;
        //            file.FileName = fileAfterSigned.FileName;
        //            _dataContext.DocumentFile.Update(file);

        //            //Cập nhật tài liệu
        //            var wfUser = doc.WorkFlowUser;
        //            //Người ký hiện tại
        //            var curUser = wfUser.FirstOrDefault(x => x.Id == doc.NextStepId);
        //            var index = wfUser.IndexOf(curUser);
        //            wfUser[index].SignAtDate = DateTime.Now;
        //            //
        //            bool isFinish = index == wfUser.Count() - 1;
        //            if (isFinish)
        //            {
        //                //Cập nhật tài liệu
        //                doc.NextStepId = null;
        //                doc.NextStepUserId = null;
        //                doc.NextStepUserName = null;
        //                doc.NextStepUserEmail = null;
        //                doc.NextStepUserPhoneNumber = null;
        //                doc.DocumentStatus = DocumentStatus.FINISH;
        //                doc.ModifiedDate = DateTime.Now;
        //                doc.WorkFlowUser = wfUser;
        //                _dataContext.Document.Update(doc);
        //            }
        //            else  //Tài liệu đã hoàn thành quy trình ký
        //            {
        //                //Cập nhật tài liệu
        //                doc.NextStepId = wfUser[index + 1].Id;
        //                doc.NextStepUserId = wfUser[index + 1].UserId;
        //                doc.NextStepUserName = wfUser[index + 1].UserName;
        //                doc.NextStepUserEmail = wfUser[index + 1].UserEmail;
        //                doc.NextStepUserPhoneNumber = wfUser[index + 1].UserPhoneNumber;
        //                doc.NextStepSignType = wfUser[index + 1].Type;
        //                doc.WorkFlowUser = wfUser;
        //                doc.ModifiedDate = DateTime.Now;
        //                _dataContext.Document.Update(doc);
        //            }
        //            //TODO: Lưu lịch sử ký
        //            int dbSave = await _dataContext.SaveChangesAsync();
        //            if (dbSave > 0)
        //            {
        //                systemLog.UserId = doc.NextStepUserId != null ? doc.NextStepUserId.Value.ToString() : "";
        //                // systemLog.Description = $"Mã hợp đồng {doc.Code}";
        //                systemLog.OrganizationId = doc.OrganizationId != null ? doc.OrganizationId.Value.ToString() : "";
        //                // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);

        //                Log.Information($"Xử lý quy trình: Cập nhật thông tin tài liệu thành công");
        //                var signFile = new SignFileModel()
        //                {
        //                    FileObjectName = fileAfterSigned.ObjectName,
        //                    FileBucketName = fileAfterSigned.BucketName,
        //                    FileName = fileAfterSigned.FileName
        //                };
        //                responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                {
        //                    DocumentId = doc.Id,
        //                    DocumentCode = doc.Code,
        //                    DocumentName = doc.Name,
        //                    SignFile = signFile,
        //                    IsSuccess = true,
        //                    Message = $"Ký thành công"
        //                });

        //                #region Gmail
        //                //TODO: Gửi mail
        //                if (isFinish)
        //                {
        //                    //Gửi link tài liệu vừa tạo và mã OTP qua mail
        //                    var ms = new MinIOService();
        //                    var fileDownloadUrl = await ms.GetObjectPresignUrlAsync(file.FileBucketName, file.FileObjectName);
        //                    //var fileDownloadUrl = $"{minio_service_url}api/v1/core/minio/download-object?bucketName={file.FileBucketName}&objectName={file.FileObjectName}";
        //                    var toEmails = wfUser.Select(x => x.UserEmail).ToList();

        //                    string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

        //                    var body = _emailHandler.GenerateDocumentSignedEmailBody(new GenerateEmailBodyModel()
        //                    {
        //                        UserName = curUser.UserFullName,
        //                        DocumentName = doc.Name,
        //                        DocuemtCode = doc.Code,
        //                        DocumentDownloadUrl = fileDownloadUrl,
        //                    });
        //                    var sendMail = _emailHandler.SendMailGoogle(toEmails, null, null, title, body);
        //                    if (!sendMail)
        //                    {
        //                        Log.Error($"Xử lý quy trình: Gửi email không thành công: {doc.Code}");
        //                    }
        //                    //TODO: Gửi link tài liệu vừa tạo và mã OTP qua số điện thoại
        //                    //-Gọi service gửi SMS
        //                    //Gửi thông báo cho hệ thống Khách hàng
        //                    var document3rdId = 0;
        //                    int.TryParse(doc.Document3rdId, out document3rdId);
        //                    var documentType = await _dataContext.DocumentType.FindAsync(doc.DocumentTypeId);
        //                    if (documentType == null)
        //                    {
        //                        Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(doc));
        //                        return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
        //                    }
        //                    var notify = new NotifyDocumentModel()
        //                    {
        //                        Id = document3rdId,
        //                        DocumentTypeCode = documentType.Code,
        //                        DocumentCode = doc.Code,
        //                        DocumentWorkflowStatus = DocumentStatus.FINISH,
        //                        Note = $"Tài liệu có mã {doc.Code} đã hoàn thành quy trình ký"
        //                    };
        //                    var sendNotify = await _notifyService.SendNotifyDocumentStatus(doc.OrganizationId, notify);
        //                }
        //                else
        //                {
        //                    string url = null;
        //                    //Tạo URL
        //                    url = $"{portalWebUrl}validate-otp?code={doc.Code}&email={doc.NextStepUserEmail}";
        //                    //Gửi link tài liệu vừa tạo và mã OTP qua mail
        //                    var toEmails = new List<string>()
        //                    {
        //                        doc.NextStepUserEmail
        //                    };

        //                    string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";

        //                    var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
        //                    {
        //                        UserName = curUser.UserFullName,
        //                        DocumentName = doc.Name,
        //                        DocuemtCode = doc.Code,
        //                        DocumentUrl = url
        //                    });
        //                    var sendMail = _emailHandler.SendMailGoogle(toEmails, null, null, title, body);
        //                    if (!sendMail)
        //                    {
        //                        Log.Error($"Xử lý quy trình: Gửi email không thành công: {doc.Code}");
        //                    }
        //                    //TODO:Gửi link tài liệu vừa tạo và mã OTP qua số điện thoại
        //                    //Gửi thông báo cho hệ thống Khách hàng
        //                    var document3rdId = 0;
        //                    int.TryParse(doc.Document3rdId, out document3rdId);
        //                    var documentType = await _dataContext.DocumentType.FindAsync(doc.DocumentTypeId);
        //                    if (documentType == null)
        //                    {
        //                        Log.Information("Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(doc));
        //                        return new ResponseError(Code.NotFound, "Không tìm thấy loại tài liệu");
        //                    }
        //                    var notify = new NotifyDocumentModel()
        //                    {
        //                        Id = document3rdId,
        //                        DocumentTypeCode = documentType.Code,
        //                        DocumentCode = doc.Code,
        //                        DocumentWorkflowStatus = DocumentStatus.PROCESSING,
        //                        Note = $"Tài liệu có mã {doc.Code} đã được ký bởi {curUser.UserFullName}"
        //                    };
        //                    var sendNotify = await _notifyService.SendNotifyDocumentStatus(doc.OrganizationId, notify);
        //                }
        //                #endregion

        //            }
        //            else
        //            {
        //                Log.Error($"Xử lý quy trình: Cập nhật thông tin tài liệu không thành công");
        //                responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                {
        //                    DocumentId = doc.Id,
        //                    DocumentCode = doc.Code,
        //                    DocumentName = doc.Name,
        //                    IsSuccess = true,
        //                    Message = $"Không cập nhật được Database"
        //                });
        //            }
        //        }

        //        return new ResponseObject<List<WorkflowDocumentSignReponseModel>>(responseSignModel, MessageConstants.CreateSuccessMessage, Code.Success);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{ex.Message}");
        //    }
        //}

        public async Task<Response> GetCoordinateFile(Guid documentId, SystemLogModel systemLog = null)
        {
            try
            {
                var document = await _dataContext.Document.FindAsync(documentId);
                if (document == null)
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin tài liệu");
                var docFile = await _dataContext.DocumentFile.Where(c => c.DocumentId == document.Id).FirstOrDefaultAsync();
                if (docFile == null)
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy thông tin file của tài liệu");
                // Lấy danh sách liên hệ
                var lstUser = document.WorkFlowUser;
                // Lấy thông tin người ký tại bước hiện tại
                var currentStepUser = lstUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();

                var templateId = docFile.DocumentFileTemplateId;

                // Lấy template của document
                var documentTemplate = await _dataContext.DocumentFileTemplate.Where(c => c.Id == templateId).FirstOrDefaultAsync();

                if (documentTemplate == null)
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file template");

                // Lấy danh sách vùng ký 
                var lstMetaDataConfig = documentTemplate.MetaDataConfig.Where(c => !string.IsNullOrEmpty(c.FixCode)).ToList();

                // Lấy thứ tự ký
                var stepOrder = lstUser.FindIndex(c => c.Id == document.NextStepId);

                // Lấy vùng ký tại bước hiện tại
                if (lstMetaDataConfig.Count == 0)
                    return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký");

                // #TODO: MAP USER VS METADATACONFIG SIGN
                //------------------------
                //------------------------

                // Danh sách loại ký theo quy trình
                var lstSignTypeOfUser = lstUser.Select(c => c.Type).ToArray();

                // Kiểm tra xem đã ký qua loại gì rồi :)))
                var lstSignedType = lstSignTypeOfUser.Skip(0).Take(stepOrder).ToArray();

                // Danh sách Meta Data Config còn lại
                var lstMetaDataConfigRemain = new List<MetaDataConfig>();
                foreach (var item in lstMetaDataConfig)
                {
                    lstMetaDataConfigRemain.Add(item);
                }

                // Danh sách thứ tự vùng ký loại bỏ
                var lstOrderToRemove = new List<int>();

                // Loại bỏ những vùng đã ký trong biểu mẫu
                if (lstSignedType.Length > 0)
                {
                    foreach (var signType in lstSignedType)
                    {
                        for (int i = 0; i < lstMetaDataConfig.Count; i++)
                        {
                            if (lstMetaDataConfig[i].SignType == signType)
                            {
                                // Nếu tồn tại số đó trong lstOrderToRemove rồi thì continue
                                if (lstOrderToRemove.Contains(i))
                                    continue;
                                // Nếu chưa thì add vào lstRemove
                                lstOrderToRemove.Add(i);
                                break;
                            }
                        }
                    }
                }

                // Loại bỏ
                if (lstOrderToRemove.Count > 0)
                {
                    foreach (int indice in lstOrderToRemove.OrderByDescending(v => v))
                    {
                        lstMetaDataConfigRemain.RemoveAt(indice);
                    }
                }
                // Kiểu ký tại bước hiện tại
                var currentSignType = lstUser[stepOrder].Type;

                // Lấy vùng ký giống kiểu ký hiện tại được cấu hình trước
                var metaDataConfig = lstMetaDataConfigRemain.Where(c => c.SignType == currentSignType).FirstOrDefault();

                // Nếu không tìm được kiểu ký phù hợp
                if (metaDataConfig == null)
                    return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} -Kiểm tra lại cấu hình vùng ký");
                // Convert tọa độ từ vùng ký trong biểu mẫu
                var convertCoordinate = (ResponseObject<DataInputSignPDF>)await ConvertCoordinateFile(metaDataConfig, docFile, "", null);
                if (convertCoordinate.Code == Code.Success && convertCoordinate is ResponseObject<DataInputSignPDF> convertResult)
                {
                    var result = AutoMapperUtils.AutoMap<DataInputSignPDF, CoordinateFileModel>(convertResult.Data);
                    result.DocumentId = documentId;

                    systemLog.UserId = document.NextStepUserId?.ToString();
                    // systemLog.Description = $"Lấy tọa độ vùng ký hợp đồng {document.Code}";
                    systemLog.OrganizationId = document.OrganizationId?.ToString();
                    // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);

                    return new ResponseObject<CoordinateFileModel>(result, "Tải tọa độ ký thành công", Code.Success);
                }
                return new ResponseError(Code.ServerError, "Không convert được tọa độ vùng ký");
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCoordinate(List<Guid> listDocument, SystemLogModel systemLog = null)
        {
            try
            {
                var result = new List<CoordinateFileModel>();

                Guid userId = Guid.Empty;
                Guid orgId = Guid.Empty;
                string docCode = "";

                foreach (var documentId in listDocument)
                {
                    var document = await _dataContext.Document.FindAsync(documentId);
                    if (document == null)
                    {
                        result.Add(new CoordinateFileModel()
                        {
                            DocumentId = documentId,
                            Message = "Không tìm thấy thông tin tài liệu"
                        });
                        continue;
                    }

                    userId = document.NextStepUserId ?? Guid.Empty;
                    orgId = document.OrganizationId ?? Guid.Empty;
                    docCode += document.Code + ", ";

                    var docFile = await _dataContext.DocumentFile.Where(c => c.DocumentId == document.Id).FirstOrDefaultAsync();
                    if (docFile == null)
                    {
                        result.Add(new CoordinateFileModel()
                        {
                            DocumentId = documentId,
                            Message = "Không tìm thấy thông tin file của tài liệu"
                        });
                        continue;
                    }
                    // Lấy danh sách liên hệ
                    var lstUser = document.WorkFlowUser;
                    // Lấy thông tin người ký tại bước hiện tại
                    var currentStepUser = lstUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();

                    var templateId = docFile.DocumentFileTemplateId;

                    // Lấy template của document
                    var documentTemplate = await _dataContext.DocumentFileTemplate.Where(c => c.Id == templateId).FirstOrDefaultAsync();

                    if (documentTemplate == null)
                    {
                        result.Add(new CoordinateFileModel()
                        {
                            DocumentId = documentId,
                            Message = "Không tìm thấy file template"
                        });
                        continue;
                    }

                    // Lấy danh sách vùng ký 
                    var lstMetaDataConfig = documentTemplate.MetaDataConfig.Where(c => !string.IsNullOrEmpty(c.FixCode)).ToList();

                    // Lấy thứ tự ký
                    var stepOrder = lstUser.FindIndex(c => c.Id == document.NextStepId);

                    // Lấy vùng ký tại bước hiện tại
                    if (lstMetaDataConfig.Count == 0)
                    {
                        result.Add(new CoordinateFileModel()
                        {
                            DocumentId = documentId,
                            Message = "Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký"
                        });
                        continue;
                    }

                    // #TODO: MAP USER VS METADATACONFIG SIGN
                    //------------------------
                    //------------------------

                    // Danh sách loại ký theo quy trình
                    var lstSignTypeOfUser = lstUser.Select(c => c.Type).ToArray();

                    // Kiểm tra xem đã ký qua loại gì rồi :)))
                    var lstSignedType = lstSignTypeOfUser.Skip(0).Take(stepOrder).ToArray();

                    // Danh sách Meta Data Config còn lại
                    var lstMetaDataConfigRemain = new List<MetaDataConfig>();
                    foreach (var item in lstMetaDataConfig)
                    {
                        lstMetaDataConfigRemain.Add(item);
                    }

                    // Danh sách thứ tự vùng ký loại bỏ
                    var lstOrderToRemove = new List<int>();

                    // Loại bỏ những vùng đã ký trong biểu mẫu
                    if (lstSignedType.Length > 0)
                    {
                        foreach (var signType in lstSignedType)
                        {
                            for (int i = 0; i < lstMetaDataConfig.Count; i++)
                            {
                             // Nếu tồn tại số đó trong lstOrderToRemove rồi thì continue
                             if (lstOrderToRemove.Contains(i))
                             continue;
                             // Nếu chưa thì add vào lstRemove
                             lstOrderToRemove.Add(i);
                             break;
                            }
                        }
                    }

                    // Loại bỏ
                    if (lstOrderToRemove.Count > 0)
                    {
                        foreach (int indice in lstOrderToRemove.OrderByDescending(v => v))
                        {
                            lstMetaDataConfigRemain.RemoveAt(indice);
                        }
                    }
                    // Kiểu ký tại bước hiện tại
                    var currentSignType = lstUser[stepOrder].Type;

                    // Lấy vùng ký giống kiểu ký hiện tại được cấu hình trước
                    var metaDataConfig = lstMetaDataConfigRemain.FirstOrDefault();

                    // Nếu không tìm được kiểu ký phù hợp
                    if (metaDataConfig == null)
                    {
                        result.Add(new CoordinateFileModel()
                        {
                            DocumentId = documentId,
                            Message = "Kiểm tra lại cấu hình vùng ký"
                        });
                        continue;
                    }
                    // Convert tọa độ từ vùng ký trong biểu mẫu
                    var convertCoordinate = await ConvertCoordinateFile(metaDataConfig, docFile, "", null);
                    if (convertCoordinate.Code == Code.Success && convertCoordinate is ResponseObject<DataInputSignPDF> convertResult)
                    {
                        var coordinate = AutoMapperUtils.AutoMap<DataInputSignPDF, CoordinateFileModel>(convertResult.Data);
                        coordinate.DocumentId = documentId;
                        coordinate.Message = "Lấy tọa độ ký thành công";
                        result.Add(coordinate);
                        continue;
                    }
                    result.Add(new CoordinateFileModel()
                    {
                        DocumentId = documentId,
                        Message = "Không convert được tọa độ vùng ký"
                    });
                }

                systemLog.UserId = userId.ToString();
                // systemLog.Description = $"Lấy tọa độ vùng ký hợp đồng {docCode}";
                systemLog.OrganizationId = orgId.ToString();
                // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);

                return new ResponseObject<List<CoordinateFileModel>>(result, "Lấy dữ liệu thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        //public Task<string> GetAliasDefault(Guid userId)
        //{
        //    // Lấy thông tin alias theo người dùng
        //    try
        //    {
        //        return null;
        //        //using (var client = new HttpClient())
        //        //{
        //        //    Log.Information("GetAliaDefault userId: " + userId);
        //        //    string url = $"{certificateService}api/v1/certificate/user/alias-default?userId={userId}";
        //        //    HttpResponseMessage res = await client.GetAsync(url);
        //        //    if (res.IsSuccessStatusCode)
        //        //    {
        //        //        var responeText = res.Content.ReadAsStringAsync().Result;
        //        //        var rsAlias = JsonSerializer.Deserialize<ResponseObject<string>>(responeText);
        //        //        if (rsAlias.Code != Code.Success)
        //        //        {
        //        //            Log.Error(rsAlias.Message, JsonSerializer.Serialize(rsAlias));
        //        //        }
        //        //        return rsAlias.Data;
        //        //    }
        //        //    else
        //        //    {
        //        //        Log.Error("Lỗi kết nối với certificateService");
        //        //        return null;
        //        //    }
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"{ MessageConstants.ErrorLogMessage} {ex.Message}");
        //        throw ex;
        //    }
        //}

        private static System.Drawing.Image resizeImage(System.Drawing.Image imgToResize, Size size)
        {
            //Get the image current width  
            int sourceWidth = imgToResize.Width;
            //Get the image current height  
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            //Calulate  width with new desired size  
            nPercentW = ((float)size.Width / (float)sourceWidth);
            //Calculate height with new desired size  
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            //New Width  
            int destWidth = (int)(sourceWidth * nPercent);
            //New Height  
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height  
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (System.Drawing.Image)b;
        }

        private Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true);
            return image;
        }
        
        private Image MemoryStreamToImage(MemoryStream memoryStream)
        {
            //byte[] imageBytes = Convert.FromBase64String(base64String);
            //MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            //memoryStream.Write(imageBytes, 0, imageBytes.Length);
            System.Drawing.Image image = System.Drawing.Image.FromStream(memoryStream, true);
            return image;
        }

        private string ImageToBase64(Image image)
        {
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, ImageFormat.Jpeg);
                byte[] imageBytes = m.ToArray();
                var base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }
    }
}
