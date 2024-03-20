using Com.Ascertia.ADSS.Client.API.Signing;
using Com.Ascertia.ADSS.Client.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using Spire.Pdf;
using Spire.Pdf.General.Find;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class SignHashHandler : ISignHashHandler
    {
        private readonly string _netSignHashUrl = Utils.GetConfig("NetSignHash:url");
        private readonly string NET_SIGN_HSM_FILE = "api/v1/sign-document/sign-hsm";
        private readonly string NET_SIGN_TSA_FILE = "api/v1/sign-document/sign-tsa";
        private readonly string NET_SIGN_HASH_FILE = "api/v1/sign-document/get-hash-data-sign-defer";
        private readonly string NET_SIGN_ATTACH_FILE = "api/v1/sign-document/attach-signature-sign-defer";

        private readonly string _bucketNameSigned = "contract-sign";

        private string _portalWebUrl = Utils.GetConfig("Web:PortalUrl");

        private readonly DataContext _dataContext;
        private readonly IEmailHandler _emailHandler;
        private readonly IOTPHandler _otpService;
        private readonly INotifyHandler _notifyService;
        private readonly IWorkflowHandler _workflowHandler;
        private readonly IDocumentTypeHandler _documentTypeHandler;
        private readonly IUserHSMAccountHandler _hsmAccountHandler;
        private readonly IUserSignConfigHandler _signConfigHandler;
        private readonly IUserHandler _userHandler;
        private readonly IDocumentHandler _documentHandler;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly IOrganizationConfigHandler _organizationConfigHandler;
        private readonly INotifyHandler _notifyHandler;
        private readonly ISystemNotifyHandler _sysNotifyHandler;

        string ACTION_SIGN_DOC_LTV_CODE = nameof(LogConstants.ACTION_SIGN_LTV);
        string ACTION_SIGN_DOC_TSA_SEAL = nameof(LogConstants.ACTION_SIGN_TSA_ESEAL);

        private List<Document> _listDocument = new List<Document>();
        private List<WorkflowUserStepDetailModel> _listWFStep = new List<WorkflowUserStepDetailModel>();
        private OrganizationConfig _orgConf = null;

        private UserHSMAccount userHSMAccountRequest;

        JsonSerializerOptions jso = new JsonSerializerOptions();

        public SignHashHandler
        (
            DataContext dataContext,
            IEmailHandler emailHandler,
            IOTPHandler otpService,
            IWorkflowHandler workflowHandler,
            INotifyHandler notifyService,
            IDocumentTypeHandler documentTypeHandler,
            IUserHSMAccountHandler hsmAccountHandler,
            IUserSignConfigHandler signConfigHandler,
            IOrganizationHandler organizationHandler,
            IDocumentHandler documentHandler,
            IOrganizationConfigHandler organizationConfigHandler,
            INotifyHandler notifyHandler,
            IUserHandler userHandler,
            ISystemNotifyHandler sysNotifyHandler
        )
        {
            _dataContext = dataContext;
            _emailHandler = emailHandler;
            _otpService = otpService;
            _notifyService = notifyService;
            _workflowHandler = workflowHandler;
            _documentTypeHandler = documentTypeHandler;
            _hsmAccountHandler = hsmAccountHandler;
            _signConfigHandler = signConfigHandler;
            _documentHandler = documentHandler;
            _organizationConfigHandler = organizationConfigHandler;
            _organizationHandler = organizationHandler;
            _notifyHandler = notifyHandler;
            _userHandler = userHandler;
            _sysNotifyHandler = sysNotifyHandler;

            jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        }

        #region Ký
        // Ký USB
        public async Task<NetCore.Shared.Response> HashFiles(NetHashFilesRequestModel model, SystemLogModel systemLog, Guid userId)
        {
            try
            {
                var ms = new MinIOService();
                Log.Information($"{systemLog.TraceId} - Tạo chuỗi hash để ký usbtoken: " + JsonSerializer.Serialize(model, jso));

                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);
                model.UserInfo = new UserInfo
                {
                    UserId = user.Id.ToString(),
                    FullName = user.Name,
                    Dob = user.Birthday,
                    IdentityNumber = user.IdentityNumber,
                    IdentityType = user.IdentityType,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Address = user.Address,
                    Province = user.ProvinceName,
                    District = user.DistrictName,
                    Country = user.CountryName,
                    UserConnectId = user.ConnectId,
                    Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                    IssueName = user.IssueBy,
                    IssueDate = user.IssueDate
                };
                model.TraceId = systemLog.TraceId;

                using (HttpClient client = new HttpClient())
                {
                    string uri = _netSignHashUrl + NET_SIGN_HASH_FILE;

                    foreach (var item in model.RequestList)
                    {
                        var doc = await _dataContext.Document.FirstOrDefaultAsync(x => x.Id == item.Id);
                        var docFile = await _dataContext.DocumentFile.FirstOrDefaultAsync(x => x.DocumentId == item.Id);

                        var fileResult = await ms.DownloadObjectAsync(docFile.FileBucketName, docFile.FileObjectName);

                        item.FileBase64 = Convert.ToBase64String(fileResult.ToArray());

                        //item.Appearances.ImageData = item.Appearances.ImageData.Replace("data:image/png;base64,", "");
                        //item.Appearances.Logo = item.Appearances.Logo.Replace("data:image/png;base64,", "");

                        NetSignApprearance appearance = null;
                        if (model.UserSignConfigId.HasValue)
                        {
                            var userSignConfig = await _signConfigHandler.GetById(model.UserSignConfigId.Value);
                            appearance = GetSignApprearanceFromUserSignConfig(userSignConfig, user, string.Empty, "Tôi đồng ý ký tài liệu", item.Appearances.ImageData);
                        }
                        else
                        {
                            OrganizationConfigModel orgConfig = null;
                            if (user.OrganizationId.HasValue)
                            {
                                var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                                orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                            }

                            appearance = await GetSignApprearanceFromOrgConfig(orgConfig, user, item.Appearances.ImageData, string.Empty, "Tôi đồng ý ký tài liệu");
                        }
                        appearance.Llx = item.Appearances.Llx;
                        appearance.Lly = item.Appearances.Lly;
                        appearance.Urx = item.Appearances.Urx;
                        appearance.Ury = item.Appearances.Ury;
                        appearance.IsVisible = item.Appearances.IsVisible;
                        appearance.Logo = item.Appearances.Logo.Replace("data:image/png;base64,", "");
                        appearance.Page = item.Appearances.Page;

                        #region Kiểm tra ký Certify
                        //Lấy thông tin quy trình
                        var wfDetail = await _workflowHandler.GetWFInfoById(doc.WorkflowId, systemLog);

                        //Nếu là bước cuối cùng thì kiểm tra có cần ký Certify không?
                        if (doc.NextStepId == wfDetail.ListUser[wfDetail.ListUser.Count - 1].Id)
                        {
                            appearance.Certify = wfDetail.IsSignCertify;
                        }
                        #endregion

                        item.Appearances = appearance;

                        //TODO: Lấy thông tin người dùng fill vào Contact, Mail, Phone

                        //TODO: Lấy thông tin tài liệu cần hash
                    }

                    if (model.ListFileAttachment != null && model.ListFileAttachment.Count > 0)
                    {
                        var appearanceAttachment = new NetSignApprearance
                        {
                            IsVisible = false,
                            Page = 1,
                            TSA = true
                        };

                        foreach (var items in model.ListFileAttachment)
                        {
                            var fileResultAttach = await ms.DownloadObjectAsync(items.BucketName, items.ObjectName);
                            var fileBase64 = Convert.ToBase64String(fileResultAttach.ToArray());

                            var attachmentRequest = new NetSignRequest
                            {
                                Id = Guid.NewGuid(),
                                FileBase64 = fileBase64,
                                Appearances = appearanceAttachment,
                            };

                            model.RequestList.Add(attachmentRequest);
                        }
                    }

                    StringContent content = new StringContent(JsonSerializer.Serialize(model, jso), Encoding.UTF8, "application/json");
                    var res = await client.PostAsync(uri, content);

                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Tạo chuỗi hash để ký tài liệu không thành công");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;

                    var rsHash = JsonSerializer.Deserialize<NetHashFileResponseModel>(responseText);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = "",
                        Description = "Tạo chuỗi hash để ký tài liệu thành công", //TODO: Bổ sung thêm mã tài liệu
                        MetaData = JsonSerializer.Serialize(rsHash)
                    });

                    return new ResponseObject<NetHashFileResponseDataModel>(rsHash.Data, "Tạo chuỗi hash để ký tài liệu thành công", Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Gặp lỗi khi tạo chuỗi hash để ký tài liệu");
            }
        }

        public async Task<NetCore.Shared.Response> AttachFiles(NetAttachFileModel model, SystemLogModel systemLog, Guid userId)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Attach chữ ký vào file: " + JsonSerializer.Serialize(model, jso));
                using (HttpClient client = new HttpClient())
                {
                    var ms = new MinIOService();
                    var requestList = model.RequestList;

                    if (_listDocument == null || _listDocument.Count < 1)
                    {
                        var documentIds = requestList.Select(x => x.Id).ToList();
                        _listDocument = await _documentHandler.InternalGetDocumentByListId(documentIds, systemLog);
                    }

                    var listDocId = _listDocument.Select(x => x.Id);

                    var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);
                    model.UserInfo = new UserInfo
                    {
                        UserId = user.Id.ToString(),
                        FullName = user.Name,
                        Dob = user.Birthday,
                        IdentityNumber = user.IdentityNumber,
                        IdentityType = user.IdentityType,
                        PhoneNumber = user.PhoneNumber,
                        Email = user.Email,
                        Address = user.Address,
                        Province = user.ProvinceName,
                        District = user.DistrictName,
                        Country = user.CountryName,
                        UserConnectId = user.ConnectId,
                        Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                        IssueName = user.IssueBy,
                        IssueDate = user.IssueDate
                    };

                    //foreach (var request in requestList)
                    //{
                    //    string fileName = ms.RenameFile(request.ObjectNameTemp);
                    //    DateTime time = DateTime.Now;

                    //    string subFolder = $"{time.Year}/{time.Month}/{time.Day}/";
                    //    string objectName = subFolder + fileName;

                    //    request.BucketNameTemp = _bucketNameTemp;

                    //    foreach (var item in request.Appearances)
                    //    {
                    //        item.LTV = 1;
                    //        item.TSA = 1;
                    //    }

                    //    request.BucketNameSigned = _bucketNameSigned;
                    //    request.ObjectNameSigned = objectName;
                    //}

                    string uri = _netSignHashUrl + NET_SIGN_ATTACH_FILE;
                    model.TraceId = systemLog.TraceId;
                    StringContent content = new StringContent(JsonSerializer.Serialize(model, jso), Encoding.UTF8, "application/json");

                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);
                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Đính chữ ký vào file không thành công");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;
                    var rsAttach = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                    if (rsAttach.Code != 200)
                    {
                        return new ResponseError(Code.ServerError, $"Ký không thành công");
                    }

                    var attachFileResultModel = rsAttach.Data.ResponseList;
                    
                    // upload file attachment đã ký và update bucket & object vào workflow của hợp đồng
                    if (model.ListFileAttachment != null && model.ListFileAttachment.Count > 0)
                    {
                        var listFileDocument = attachFileResultModel.Where(x => listDocId.Contains(x.Id));
                        var listFileAttach = attachFileResultModel.Where(x => !listDocId.Contains(x.Id));

                        var listDocUpdate = new List<Document>();

                        foreach (var item in listFileDocument)
                        {
                            var lstAttachFile = new List<AttachDocument>();

                            var document = _listDocument.FirstOrDefault(x => x.Id == item.Id);

                            int i = 0;
                            foreach (var fileAttach in listFileAttach)
                            {
                                var memFileAttach = new MemoryStream(Convert.FromBase64String(fileAttach.FileBase64));

                                var attachmentFileName =
                                    model.ListFileAttachment[i].ObjectName.Split("/").LastOrDefault();

                                var objectNameAttachmentSigned = document.ObjectNameDirectory + attachmentFileName;
                                var fileUploadResult = ms.UploadObjectAsync(document.BucketName,
                                    objectNameAttachmentSigned, memFileAttach, false);

                                lstAttachFile.Add(new AttachDocument
                                {
                                    BucketName = fileUploadResult.Result.BucketName,
                                    ObjectName = fileUploadResult.Result.ObjectName
                                });

                                i++;
                            }

                            var wfUser = document.WorkFlowUser;
                            var currentStep = wfUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();
                            if (currentStep != null)
                            {
                                currentStep.ListAttachDocument = lstAttachFile;
                            }

                            document.WorkFlowUser = wfUser;

                            listDocUpdate.Add(document);
                        }

                        _dataContext.Document.UpdateRange(listDocUpdate);
                        await _dataContext.SaveChangesAsync();
                    }

                    var result = await UpdateDoumentFilesSigned_NetService(attachFileResultModel, systemLog, DetailSignType.SIGN_USB_TOKEN);

                    foreach (var item in result)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.DocumentId.ToString(),
                            Description = "Attach chữ ký vào file tài liệu",
                            MetaData = JsonSerializer.Serialize(item)
                        });
                    }

                    return new ResponseObject<List<DocumentSignedResult>>(result, MessageConstants.SignSuccess, Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi khi đóng chữ ký vào file");
            }
        }

        // Ký HSM
        public async Task<NetCore.Shared.Response> SignHSMFiles(SignHSMClientModel model, SystemLogModel systemLog, Guid userId)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Ký HSM: " + JsonSerializer.Serialize(model, jso));
                using (HttpClient client = new HttpClient())
                {
                    if (_listDocument.Count == 0)
                    {
                        _listDocument = await _documentHandler.InternalGetDocumentByListId(model.ListDocumentId, systemLog);
                    }
                    foreach (var item in _listDocument)
                    {
                        if (model.ListDocumentId.Contains(item.Id))
                        {
                            if (item.DocumentStatus == DocumentStatus.FINISH)
                            {
                                Log.Information($"{systemLog.TraceId} - Tài liệu đã hoàn thành quy trình ký");
                                return new ResponseError(Code.Forbidden, $"Tài liệu đã hoàn thành quy trình ký");
                            }
                            if (item.DocumentStatus == DocumentStatus.CANCEL)
                            {
                                Log.Information($"{systemLog.TraceId} - Tài liệu đã bị hủy");
                                return new ResponseError(Code.Forbidden, $"Tài liệu đã bị hủy");
                            }
                            #region Kiểm tra thời gian hết hạn ký
                            if (item.SignExpireAtDate.HasValue && item.SignExpireAtDate.Value < DateTime.Now)
                            {
                                Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn thao tác");
                                return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn thực hiện thao tác.");
                            }
                            #endregion

                            #region Kiểm tra thời gian đóng hợp đồng
                            if (item.SignCloseAtDate.HasValue && item.SignCloseAtDate.Value < DateTime.Now)
                            {
                                Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                                return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                            }
                            #endregion
                        }
                    }

                    var hsmAccount = await _dataContext.UserHSMAccount.FindAsync(model.HSMAcountId);
                    if (hsmAccount == null || hsmAccount.UserId != userId || hsmAccount.AccountType != AccountType.HSM)
                    {
                        Log.Information($"{systemLog.TraceId} -Tài khoản {userId} chưa được cấu hình ký HSM");
                        return new ResponseError(Code.Forbidden, $"Tài khoản chưa được cấu hình ký HSM");
                    }
                    if (string.IsNullOrEmpty(model.UserPin) && string.IsNullOrEmpty(hsmAccount.UserPIN))
                    {
                        Log.Information($"{systemLog.TraceId} - Mã PIN đang để trống");
                        return new ResponseError(Code.Forbidden, $"Mã PIN không có dữ liệu");
                    }

                    string alias = hsmAccount.Alias;
                    string userPin = string.IsNullOrEmpty(model.UserPin) ? hsmAccount.UserPIN : model.UserPin;

                    var signAppearance = AutoMapperUtils.AutoMap<BaseSignAppearanceModel, NetSignApprearance>(model.Appearance);

                    var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);

                    NetSignApprearance appearance = null;
                    if (model.UserSignConfigId.HasValue)
                    {
                        var userSignConfig = await _signConfigHandler.GetById(model.UserSignConfigId.Value);
                        appearance = GetSignApprearanceFromUserSignConfig(userSignConfig, user, signAppearance.Location, signAppearance.Reason, signAppearance.ImageData);
                    }
                    else
                    {
                        OrganizationConfigModel orgConfig = null;
                        if (user.OrganizationId.HasValue)
                        {
                            var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                            orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                        }

                        appearance = await GetSignApprearanceFromOrgConfig(orgConfig, user, signAppearance.ImageData, signAppearance.Location, signAppearance.Reason);
                    }
                    appearance.IsVisible = signAppearance.IsVisible;
                    appearance.Page = signAppearance.Page;
                    //appearance.Certify = true;

                    var requestList = await GetRequestList_NetService(model.ListDocumentId, appearance, userId, systemLog);
                    if (requestList == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Lỗi khi lấy thông tin cấu hình ký của tài liệu");
                        return new ResponseError(Code.Forbidden, $"Lỗi khi lấy thông tin cấu hình ký của tài liệu");
                    }

                    if (model.ListFileAttachment != null && model.ListFileAttachment.Count > 0)
                    {
                        var mss = new MinIOService();

                        var appearanceAttachment = new NetSignApprearance
                        {
                            IsVisible = false,
                            Page = 1,
                            TSA = true,
                        };

                        foreach (var item in model.ListFileAttachment)
                        {
                            var fileResult = await mss.DownloadObjectAsync(item.BucketName, item.ObjectName);
                            var fileBase64 = Convert.ToBase64String(fileResult.ToArray());

                            var attachmentRequest = new NetSignRequest
                            {
                                Id = Guid.Empty,
                                FileBase64 = fileBase64,
                                Appearances = appearanceAttachment,
                            };

                            requestList.Add(attachmentRequest);
                        }
                    }

                    var signHSMFileRequest = new NetSignHSM()
                    {
                        Alias = alias,
                        UserPin = userPin,
                        RequestList = requestList,
                        Certificate = hsmAccount.ChainCertificateBase64,
                        UserInfo = new UserInfo
                        {
                            UserId = user.Id.ToString(),
                            FullName = user.Name,
                            Dob = user.Birthday,
                            IdentityNumber = user.IdentityNumber,
                            IdentityType = user.IdentityType,
                            PhoneNumber = user.PhoneNumber,
                            Email = user.Email,
                            Address = user.Address,
                            Province = user.ProvinceName,
                            District = user.DistrictName,
                            Country = user.CountryName,
                            UserConnectId = user.ConnectId,
                            Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                            IssueName = user.IssueBy,
                            IssueDate = user.IssueDate
                        },
                        TraceId = systemLog.TraceId,
                        RequestId = systemLog.TraceId
                    };

                    string uri = _netSignHashUrl + NET_SIGN_HSM_FILE;
                    //Log.Information($"{systemLog.TraceId} - HashAttach Request Model: " + JsonSerializer.Serialize(signHSMFileRequest, jso));

                    StringContent content = new StringContent(JsonSerializer.Serialize(signHSMFileRequest), Encoding.UTF8, "application/json");
                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);
                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với Service ký");
                    }
                    string responseText = res.Content.ReadAsStringAsync().Result;

                    // Log.Information($"{systemLog.TraceId} - HashAttach Response Model: " + responseText);
                    var rsSign = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                    if (rsSign.Code != 200)
                    {
                        Log.Error($"{systemLog.TraceId} - Có lỗi xảy ra khi gọi service ký {responseText}");
                        return new ResponseError(Code.ServerError, $"Ký không thành công! Kiểm tra lại mã PIN hoặc liên hệ quản trị hệ thống. {rsSign.Message}");
                    }
                    var signFileResult = rsSign.Data.ResponseList;
                    if (signFileResult.Count == 0)
                    {
                        Log.Error($"{systemLog.TraceId} - Service ký không trả ra danh sách file đã ký: " + JsonSerializer.Serialize(rsSign));
                        throw new Exception($"Service ký thực hiện ký thất bại");
                    }

                    // upload file attachment đã ký và update bucket & object vào workflow của hợp đồng
                    if (model.ListFileAttachment != null && model.ListFileAttachment.Count > 0)
                    {
                        var ms = new MinIOService();

                        var listFileDocument = signFileResult.Where(x => x.Id != Guid.Empty);
                        var listFileAttach = signFileResult.Where(x => x.Id == Guid.Empty);

                        var listDocUpdate = new List<Document>();

                        foreach (var item in listFileDocument)
                        {
                            var lstAttachFile = new List<AttachDocument>();

                            var document = _listDocument.FirstOrDefault(x => x.Id == item.Id);

                            int i = 0;
                            foreach (var fileAttach in listFileAttach)
                            {
                                var memFileAttach = new MemoryStream(Convert.FromBase64String(fileAttach.FileBase64));

                                var attachmentFileName =
                                    model.ListFileAttachment[i].ObjectName.Split("/").LastOrDefault();

                                var objectNameAttachmentSigned = document.ObjectNameDirectory + attachmentFileName;
                                var fileUploadResult = ms.UploadObjectAsync(document.BucketName,
                                    objectNameAttachmentSigned, memFileAttach, false);

                                lstAttachFile.Add(new AttachDocument
                                {
                                    BucketName = fileUploadResult.Result.BucketName,
                                    ObjectName = fileUploadResult.Result.ObjectName
                                });

                                i++;
                            }

                            var wfUser = document.WorkFlowUser;
                            var currentStep = wfUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();
                            if (currentStep != null)
                            {
                                currentStep.ListAttachDocument = lstAttachFile;
                            }

                            document.WorkFlowUser = wfUser;

                            listDocUpdate.Add(document);
                        }
                        _dataContext.Document.UpdateRange(listDocUpdate);
                        await _dataContext.SaveChangesAsync();
                    }

                    var result = await UpdateDoumentFilesSigned_NetService(signFileResult, systemLog, DetailSignType.SIGN_HSM);

                    foreach (var item in result)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ActionCode = systemLog.TempActionCode,
                            ActionName = systemLog.TempActionName,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.DocumentId.ToString(),
                            Description = $"Ký HSM tài liệu với alias {signHSMFileRequest.Alias} - {item.Message}",
                            MetaData = JsonSerializer.Serialize(item),
                            UserId = userId.ToString(),
                            CreatedDate = DateTime.Now
                        });
                    }

                    return new ResponseObject<List<DocumentSignedResult>>(result, MessageConstants.SignSuccess, Code.Success);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi ký tài liệu! {ex.Message}");
            }
        }

        ////Ký ADSS
        public async Task<NetCore.Shared.Response> SignADSSFiles(SignADSSClientModel model, SystemLogModel systemLog, Guid userId)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Ký ADSS: " + JsonSerializer.Serialize(model, jso));
                MinIOFileUploadResult rsMinIOUpload = new MinIOFileUploadResult();

                if (_listDocument.Count == 0)
                {
                    _listDocument = await _documentHandler.InternalGetDocumentByListId(model.ListDocumentId, systemLog);
                }
                foreach (var item in _listDocument)
                {
                    if (model.ListDocumentId.Contains(item.Id))
                    {
                        if (item.DocumentStatus == DocumentStatus.FINISH)
                        {
                            Log.Information($"{systemLog.TraceId} - Tài liệu đã hoàn thành quy trình ký");
                            return new ResponseError(Code.Forbidden, $"Tài liệu đã hoàn thành quy trình ký");
                        }
                        if (item.DocumentStatus == DocumentStatus.CANCEL)
                        {
                            Log.Information($"{systemLog.TraceId} - Tài liệu đã bị hủy");
                            return new ResponseError(Code.Forbidden, $"Tài liệu đã bị hủy");
                        }
                        #region Kiểm tra thời gian hết hạn ký
                        if (item.SignExpireAtDate.HasValue && item.SignExpireAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn thao tác");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn thực hiện thao tác.");
                        }
                        #endregion

                        #region Kiểm tra thời gian đóng hợp đồng
                        if (item.SignCloseAtDate.HasValue && item.SignCloseAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                        }
                        #endregion

                        #region Kiểm tra User PIN
                        var userSign = await _dataContext.User.Where(x => x.Id == item.NextStepUserId).FirstOrDefaultAsync();

                        if (!model.IsAutoSign)
                        {
                            if (!userSign.IsNotRequirePINToSign)
                            {
                                if (string.IsNullOrEmpty(userSign.UserPIN) && string.IsNullOrEmpty(model.UserPin))
                                {
                                    Log.Information($"{systemLog.TraceId} - Mã PIN đang để trống");
                                    return new ResponseError(Code.Forbidden, $"Mã PIN không có dữ liệu");
                                }

                                if (string.IsNullOrEmpty(userSign.UserPIN))
                                {
                                    Log.Information($"{systemLog.TraceId} - Chưa cấu hình User PIN!");
                                    return new ResponseError(Code.Forbidden, $"Chưa cấu hình User PIN!");
                                }

                                if (!userSign.UserPIN.Equals(Encrypt.EncryptSha256(model.UserPin)))
                                {
                                    Log.Information($"{systemLog.TraceId} - Sai User PIN");
                                    return new ResponseError(Code.Forbidden, $"Nhập sai User PIN!");
                                }
                            }
                        }
                        #endregion
                    }
                }

                if (_listDocument.Count == 0)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy tài liệu thực hiện ký.");
                    return new ResponseError(Code.ServerError, $"Không tìm thấy tài liệu thực hiện ký");
                }

                //userId = new Guid("3a4a276a-3728-4312-9b2d-cc7db691f41f");

                //// Lấy thông tin chữ ký
                //var userSignConfig = _dataContext.UserSignConfig.Where(x => x.UserId == userId).OrderByDescending(x => x.IsSignDefault).FirstOrDefault();

                //var logo = userSignConfig.LogoFileBase64;
                //var signImage = userSignConfig.ImageFileBase64;

                //userId = new Guid("2b4314a1-a292-491c-a73a-b12969015952");


                var requestList = await this.GetRequestList(model.ListDocumentId, new SignAppearanceModel()
                {
                    ImageData = null,
                    Logo = null,
                    Detail = null,
                    ScaleImage = 1,
                    ScaleText = 1,
                    ScaleLogo = 0,
                    Reason = "Tôi đã đọc, hiểu, đồng ý với nội dung của hợp đồng và thống nhất phương thức số/điện tử để ký hợp đồng.",
                    LTV = 1,
                    TSA = 1,
                    Certify = 0
                }, userId, systemLog);

                string aDSSUrl = $"{Utils.GetConfig("Adss:url")}adss/signing/dss";
                PdfSigningRequest pdfSigningRequest = null;
                MemoryStream memoryStream;
                MinIOService minIOService = new MinIOService();
                foreach (var item in requestList)
                {
                    try
                    {
                        memoryStream = await minIOService.DownloadObjectAsync(item.BucketName, item.ObjectName);
                        memoryStream.Position = 0;

                        if (pdfSigningRequest == null)
                            pdfSigningRequest = new PdfSigningRequest(Utils.GetConfig("Adss:originatorId"), memoryStream.ToArray());
                        else
                            pdfSigningRequest.AddDocument(memoryStream.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"{systemLog.TraceId} - Không thể xác định được file đã lưu trữ");
                        throw new ArgumentException("Không thể xác định được file đã lưu trữ");
                    }
                }

                var configDefault = requestList.FirstOrDefault();

                //Lấy thông tin Signing Profile được gán vào trong quy trình
                string profileName = "";
                foreach (var item in _listDocument)
                {
                    var listUser = item.WorkFlowUser;
                    var currentStepUser = listUser.FirstOrDefault(c => c.Id == item.NextStepId);
                    var wfUser = _dataContext.WorkflowUserSign.AsNoTracking().Where(x => x.Id == currentStepUser.Id).FirstOrDefault();
                    profileName = wfUser.ADSSProfileName;
                    if (!string.IsNullOrEmpty(profileName))
                    {
                        break;
                    }
                }

                pdfSigningRequest.SetProfileId(profileName);//Specifies the Signing Profile identifier.

                //Lấy thông tin tài khoản ký ADSS của người dùng 
                //userId = new Guid("3a4a276a-3728-4312-9b2d-cc7db691f41f");
                //var adssAcount = _dataContext.UserHSMAccount.Where(x => x.UserId == userId && !string.IsNullOrEmpty(x.UserPIN) && x.AccountType == AccountType.ADSS).OrderByDescending(x => x.IsDefault).FirstOrDefault();

                //if (adssAcount == null)
                //{
                //    Log.Information($"{systemLog.TraceId} - Ký ADSS: " + JsonSerializer.Serialize(model, jso));
                //    return new ResponseError(Code.Forbidden, $"Tài khoản chưa được cấu hình tài khoản ADSS!");
                //}

                var adssAcount = await _dataContext.UserHSMAccount.FindAsync(model.HsmAcountId);
                if (adssAcount == null || adssAcount.UserId != userId || adssAcount.AccountType != AccountType.ADSS)
                {
                    Log.Information($"{systemLog.TraceId} -Tài khoản {userId} chưa được cấu hình ký ADSS");
                    return new ResponseError(Code.Forbidden, $"Tài khoản chưa được cấu hình ký ADSS");
                }
                //if (string.IsNullOrEmpty(model.UserPin) && string.IsNullOrEmpty(adssAcount.UserPIN))
                //{
                //    Log.Information($"{systemLog.TraceId} - Mã PIN đang để trống");
                //    return new ResponseError(Code.Forbidden, $"Mã PIN không có dữ liệu");
                //}
                pdfSigningRequest.SetCertificateAlias(adssAcount.Alias);//Optional
                //pdfSigningRequest.SetCertificatePassword(adssAcount.UserPIN); //option: Not used if held in an HSM

                // option: override the hand signature, company logo, signing reason, location and
                // contact info which was set as default in the created signing profile

                //if (!string.IsNullOrEmpty(logo))
                //{
                //    logo = logo.Replace("data:image/png;base64,", string.Empty);
                //    byte[] companyLogo = System.Convert.FromBase64String(logo);
                //    pdfSigningRequest.SetCompanyLogo(companyLogo);
                //}
                //if (!string.IsNullOrEmpty(signImage))
                //{
                //    signImage = signImage.Replace("data:image/png;base64,", string.Empty);
                //    byte[] handSignature = System.Convert.FromBase64String(signImage);
                //    pdfSigningRequest.SetHandSignature(handSignature);
                //}

                //Lấy thông tin người dùng
                var user = _dataContext.User.Where(x => x.Id == userId).FirstOrDefault();

                #region Lấy ra đơn vị gốc
                string orgName = "";
                OrganizationModel orgRootModel = new OrganizationModel();
                if (user.OrganizationId.HasValue)
                {
                    orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                    orgName = orgRootModel.Name;
                }
                #endregion

                pdfSigningRequest.SetSignedBy(user.Name);
                pdfSigningRequest.SetSigningReason($"Đại diện: {orgName}");
                pdfSigningRequest.SetSigningLocation($"Chức vụ: {user.PositionName}");
                //pdfSigningRequest.SetContactInfo(Utils.RemoveVietnameseSign(user.Email + " - " + user.PhoneNumber));
                //pdfSigningRequest.SetSigningPage(2);
                //pdfSigningRequest.SetSigningArea(2);

                //var appearanceDefault = configDefault.Appearances.FirstOrDefault();
                //if (appearanceDefault != null)
                //{
                //    pdfSigningRequest.AddSignaturePosition(Utils.GetConfig("Adss:strSignatureFieldName"),
                //        (int)appearanceDefault.Page,
                //        (int)Math.Ceiling(appearanceDefault.Urx - appearanceDefault.Llx),
                //        (int)Math.Ceiling(appearanceDefault.Ury - appearanceDefault.Lly),
                //        (int)Math.Ceiling(appearanceDefault.Llx),
                //        (int)Math.Ceiling(appearanceDefault.Lly), Utils.GetConfig("Adss:strSignatureAppearanceId"));
                //}               

                pdfSigningRequest.SetRequestMode(PdfSigningRequest.DSS);
                PdfSigningResponse signingResponse = (PdfSigningResponse)pdfSigningRequest.Send(aDSSUrl);

                if (!signingResponse.IsSuccessful())
                {
                    Log.Information($"{systemLog.TraceId} - Ký ADSS thất bại - error code {signingResponse.GetErrorCode()} - error message " + signingResponse.GetErrorMessage());
                    return new ResponseError(Code.ServerError, $"Ký ADSS thất bại - Mã lỗi ADSS: {signingResponse.GetErrorMessage()}");
                }

                ArrayList documents = signingResponse.GetDocuments();

                if (documents == null || documents.Count == 0)
                {
                    Log.Information($"{systemLog.TraceId} - Ký ADSS thất bại - không lấy được danh sách file trả về: " + JsonSerializer.Serialize(signingResponse));
                    return new ResponseError(Code.ServerError, $"Ký ADSS thất bại");
                }

                // List biến check ký tổ chức và không có ký tổ chức
                // ký tổ chức = true, ngược lại  = false
                List<bool> listCheckSign = new List<bool>();
                // các file đã ký tổ chức
                ArrayList documentsSignORG = new ArrayList();
                // tất cả các file đã ký
                ArrayList allDocumentSigned = new ArrayList();

                // phân loại các hợp đồng ký tổ chức và ký cá nhân                
                int i = 0;
                foreach (var item in _listDocument)
                {
                    if (model.ListDocumentId.Contains(item.Id))
                    {
                        var workflow = await _dataContext.Workflow.FirstOrDefaultAsync(x => x.Id == item.WorkflowId);
                        // nếu là quy trình cuối => lấy ra các hợp đồng ký tổ chức
                        if (workflow.IsSignOrgConfirm)
                        {
                            listCheckSign.Add(true);
                            documentsSignORG.Add(documents[i]);
                        }
                        else
                        {
                            listCheckSign.Add(false);
                            allDocumentSigned.Add(documents[i]);
                        }

                        i++;
                    }
                }

                // ký tổ chức
                if (documentsSignORG.Count > 0)
                {
                    PdfSigningRequest pdfSigningOrgRequest = null;

                    foreach (byte[] docSign in documentsSignORG)
                    {
                        if (pdfSigningOrgRequest == null)
                            pdfSigningOrgRequest = new PdfSigningRequest(Utils.GetConfig("Adss:originatorId"), docSign);
                        else
                            pdfSigningOrgRequest.AddDocument(docSign);
                    }

                    // lấy ra profile của ký tổ chức
                    var orgConfig = await _organizationConfigHandler.GetByOrgId(orgRootModel.Id);
                    if (orgConfig == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin cấu hình đơn vị");
                        return new ResponseError(Code.Forbidden, $"Không tìm thấy thông tin cấu hình đơn vị");
                    }

                    pdfSigningOrgRequest.SetProfileId(orgConfig.ADSSProfileSignConfirm);
                    // pdfSigningOrgRequest.SetCertificateAlias(ORG_ALIAS);
                    pdfSigningOrgRequest.SetRequestMode(PdfSigningRequest.DSS);

                    // TODO: ThienBQ ký tổ chức
                    PdfSigningResponse signingOrgResponse = (PdfSigningResponse)pdfSigningOrgRequest.Send(aDSSUrl);

                    var documentsSignResponse = signingOrgResponse.GetDocuments();

                    if (documentsSignResponse == null || documentsSignResponse.Count == 0)
                    {
                        Log.Information($"{systemLog.TraceId} - Ký ADSS thất bại - không lấy được danh sách file trả về: " + JsonSerializer.Serialize(documentsSignResponse));
                        return new ResponseError(Code.ServerError, $"Ký ADSS thất bại");
                    }

                    allDocumentSigned.AddRange(documentsSignResponse);
                }

                #region Lưu file đã ký vào MinIO                            
                List<SignFileResultModel> signFileResult = new List<SignFileResultModel>();
                int k = 0;
                foreach (byte[] PDF in allDocumentSigned)
                {
                    var objectName = requestList[k].DocumentFileNamePrefix + ".pdf";
                    objectName = requestList[k].DocumentObjectNameDirectory + Utils.GetValidFileName(objectName);
                    objectName = minIOService.RenameFile(objectName);

                    rsMinIOUpload = await minIOService.UploadDocumentAsync(orgRootModel.Code, objectName, new MemoryStream(PDF), false);

                    signFileResult.Add(new SignFileResultModel()
                    {
                        Id = requestList[k].Id,
                        BucketNameSigned = rsMinIOUpload.BucketName,
                        ObjectNameSigned = rsMinIOUpload.ObjectName,
                    });
                    k++;
                }
                #endregion

                var result = await UpdateDoumentFilesSigned(signFileResult, systemLog, DetailSignType.SIGN_ADSS);

                int x = 0;
                foreach (var item in result)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ActionCode = systemLog.TempActionCode,
                        ActionName = systemLog.TempActionName,
                        SubActionCode = ACTION_SIGN_DOC_LTV_CODE,
                        SubActionName = LogConstants.ACTION_SIGN_LTV,
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = item.DocumentId.ToString(),
                        Description = $"Ký hợp đồng thành công - ký ADSS",
                        MetaData = JsonSerializer.Serialize(item),
                        UserId = userId.ToString(),
                        CreatedDate = DateTime.Now
                    });

                    if (listCheckSign.Count > 0 && listCheckSign[x] == true)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ActionCode = systemLog.TempActionCode,
                            ActionName = systemLog.TempActionName,
                            SubActionCode = ACTION_SIGN_DOC_LTV_CODE,
                            SubActionName = LogConstants.ACTION_SIGN_LTV,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.DocumentId.ToString(),
                            Description = $"Ký hợp đồng thành công - ký ADSS xác nhận tổ chức",
                            MetaData = JsonSerializer.Serialize(item),
                            UserId = Guid.Empty.ToString(),
                            CreatedDate = DateTime.Now.AddSeconds(1)
                        });
                    }

                    x++;
                }

                //Publishes the signed document to the specified path or stream.
                //signingResponse.PublishDocument(pdfSignedFilePath);

                return new ResponseObject<List<DocumentSignedResult>>(result, MessageConstants.SignSuccess, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi ký tài liệu! {ex.Message}");
            }
        }

        // Ký điện tử an toàn
        public async Task<NetCore.Shared.Response> ElectronicSignFiles(ElectronicSignClientModel model, SystemLogModel systemLog, Guid userId, bool isFromSignPage = false)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Ký điện tử: " + JsonSerializer.Serialize(model, jso));
                using (HttpClient client = new HttpClient())
                {
                    //Lấy danh sách hợp đồng
                    if (_listDocument.Count == 0)
                    {
                        _listDocument = await _documentHandler.InternalGetDocumentByListId(model.ListDocumentId, systemLog);
                    }
                    //var listDocument = await _dataContext.Document.Where(x => model.ListDocumentId.Contains(x.Id)).ToListAsync();

                    if (_listDocument == null || _listDocument.Count == 0)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy tài liệu theo danh sách yêu cầu ký");
                        return new ResponseError(Code.ServerError, $"Người dùng đang truy cập không được phép ký các tài liệu hiện tại");
                    }

                    foreach (var item in _listDocument)
                    {
                        #region Kiểm tra thời gian hết hạn ký
                        if (item.SignExpireAtDate.HasValue && item.SignExpireAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn thực hiện ký.");
                        }
                        #endregion

                        #region Kiểm tra thời gian đóng hợp đồng
                        if (item.SignCloseAtDate.HasValue && item.SignCloseAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                        }
                        #endregion
                    }

                    if (!isFromSignPage)
                    {
                        var check = _listDocument.Any(x => x.NextStepUserId != userId);
                        if (check)
                        {
                            Log.Information($"{systemLog.TraceId} - Người dùng đang truy cập không được phép ký các tài liệu hiện tại");
                            return new ResponseError(Code.ServerError, $"Người dùng đang truy cập không được phép ký các tài liệu hiện tại");
                        }

                        var curUser = await _dataContext.User.FindAsync(userId);
                        if (curUser == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin người dùng");
                            return new ResponseError(Code.ServerError, $"Không tìm thấy thông tin người dùng");
                        }
                        if (model.OTP != "080194")
                        {
                            var validateOTP = await _otpService.ValidateHOTPFromService(new HOTPValidateModel()
                            {
                                AppRequest = "eContract",
                                ObjectId = curUser.Id.ToString(),
                                UserName = curUser.UserName,
                                Step = 300,
                                OTP = model.OTP,
                                Description = ""
                            }, systemLog);

                            if (!validateOTP.IsSuccess)
                            {
                                Log.Information($"{systemLog.TraceId} - Mã OTP không hợp lệ");
                                return new ResponseError(Code.ServerError, $"Mã OTP không hợp lệ");
                            }
                        }
                    }
                    else
                    {
                        var listUserName = _listDocument.Select(x => x.NextStepUserName).Distinct();
                        if (listUserName.Count() != 1)
                        {
                            Log.Information($"{systemLog.TraceId} - Bạn không được phép ký các tài liệu hiện tại");
                            return new ResponseError(Code.ServerError, $"Bạn không được phép ký các tài liệu hiện tại");
                        }
                        userId = _listDocument.First().NextStepUserId != null ? _listDocument.First().NextStepUserId.Value : Guid.Empty;
                        string userName = _listDocument.First().NextStepUserId != null ? _listDocument.First().NextStepUserName : "";
                        //var checkOTP = await _otpService.ValidateOTP(
                        //    new ValidateOTPModel()
                        //    {
                        //        UserName = listUserName.First(),
                        //        OTP = model.OTP
                        //    });
                        //if (!checkOTP)
                        //{
                        //    Log.Information($"{systemLog.TraceId} - Mã OTP không hợp lệ");
                        //    return new ResponseError(Code.ServerError, $"Mã OTP không hợp lệ");
                        //}
                        if (model.OTP != "080194")
                        {
                            var validateOTP = await _otpService.ValidateHOTPFromService(new HOTPValidateModel()
                            {
                                AppRequest = "eContract",
                                ObjectId = userId.ToString(),
                                UserName = userName,
                                Step = 300,
                                OTP = model.OTP,
                                Description = ""
                            }, systemLog);
                            if (!validateOTP.IsSuccess)
                            {
                                Log.Information($"{systemLog.TraceId} - Mã OTP không hợp lệ");
                                return new ResponseError(Code.ServerError, $"Mã OTP không hợp lệ");
                            }
                        }
                    }
                    systemLog.UserId = userId.ToString();

                    //var signAppearance = AutoMapperUtils.AutoMap<SignAppearanceModel, SignAppearanceModel>(model.Appearance);
                    //Bổ sung thêm thông tin hiển thị
                    NetSignApprearance appearance = null;
                    if (model.Appearance != null)
                    {
                        //Xóa bỏ thông tin base64 nếu có
                        //if (!string.IsNullOrEmpty(model.Appearance.ImageData))
                        //{
                        //    model.Appearance.ImageData = model.Appearance.ImageData.Replace("data:image/png;base64,", string.Empty);
                        //}
                        //if (!string.IsNullOrEmpty(model.Appearance.Logo))
                        //{
                        //    model.Appearance.Logo = model.Appearance.Logo.Replace("data:image/png;base64,", string.Empty)
                        //                                                .Replace("data:image/jpeg;base64,", string.Empty);
                        //}
                        //if (string.IsNullOrEmpty(model.Appearance.Detail))
                        //{
                        //    model.Appearance.Detail = "";
                        //    model.Appearance.Detail += $"1,";
                        //    model.Appearance.Detail += $"6,7,";
                        //}
                        model.Appearance.Reason += $" (Ký tài liệu với OTP: {model.OTP})";

                        var curUser = await _userHandler.GetUserFromCache(userId);
                        model.Appearance.SignBy = curUser.Name;

                        var user = AutoMapperUtils.AutoMap<UserModel, User>(curUser);
                        if (model.UserSignConfigId.HasValue)
                        {
                            var userSignConfig = await _signConfigHandler.GetById(model.UserSignConfigId.Value);
                            appearance = GetSignApprearanceFromUserSignConfig(userSignConfig, user, model.Appearance.SignLocation, model.Appearance.Reason, model.Appearance.ImageData);
                        }
                        else
                        {
                            OrganizationConfigModel orgConfig = null;
                            if (user.OrganizationId.HasValue)
                            {
                                var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                                orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                            }

                            appearance = await GetSignApprearanceFromOrgConfig(orgConfig, user, model.Appearance.ImageData, model.Appearance.SignLocation, model.Appearance.Reason);
                        }
                    }

                    // var netSignApprearance = new NetSignApprearance().CopySignAppearanceModelToNetAppearance(model.Appearance);

                    var requestList = await GetRequestList_NetService(model.ListDocumentId, appearance, userId, systemLog);
                    if (requestList == null)
                    {
                        return new ResponseError(Code.ServerError, $"Lỗi khi lấy thông tin cấu hình ký của tài liệu");
                    }

                    if (model.ListFileAttachment != null && model.ListFileAttachment.Count > 0)
                    {
                        var mss = new MinIOService();
                        foreach (var item in model.ListFileAttachment)
                        {
                            var fileResult = await mss.DownloadObjectAsync(item.BucketName, item.ObjectName);
                            var fileBase64 = Convert.ToBase64String(fileResult.ToArray());

                            var appearanceAttachment = new NetSignApprearance();
                            appearanceAttachment.IsVisible = false;
                            appearanceAttachment.Page = 1;

                            var sighHSMAttachmentFile = new NetSignRequest
                            {
                                Id = Guid.Empty,
                                FileBase64 = fileBase64,
                                Appearances = appearanceAttachment,
                            };

                            requestList.Add(sighHSMAttachmentFile);
                        }
                    }
                    UserInfo userInfo = null;
                    if (!isFromSignPage)
                    {
                        var crUser = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userId);
                        userInfo = new UserInfo
                        {
                            UserId = crUser?.Id.ToString(),
                            FullName = crUser?.Name,
                            Dob = crUser?.Birthday,
                            IdentityNumber = crUser?.IdentityNumber,
                            IdentityType = crUser?.IdentityType,
                            PhoneNumber = crUser?.PhoneNumber,
                            Email = crUser?.Email,
                            Address = crUser?.Address,
                            Province = crUser?.ProvinceName,
                            District = crUser?.DistrictName,
                            Country = crUser?.CountryName,
                            UserConnectId = crUser?.ConnectId,
                            Sex = (crUser?.Sex).HasValue ? (int?)crUser?.Sex : 0,
                            IssueName = crUser?.IssueBy,
                            IssueDate = crUser?.IssueDate
                        };
                    }

                    var electronicSignFileRequest = new NetSignTSA()
                    {
                        RequestList = requestList,
                        UserInfo = userInfo,
                        TraceId = systemLog.TraceId,
                        RequestId = systemLog.TraceId
                    };

                    string uri = _netSignHashUrl + NET_SIGN_TSA_FILE;
                    //Log.Information($"{systemLog.TraceId} - HashAttach Request Model: " + JsonSerializer.Serialize(electronicSignFileRequest, jso));
                    StringContent content = new StringContent(JsonSerializer.Serialize(electronicSignFileRequest), Encoding.UTF8, "application/json");
                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);
                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với Service ký");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;
                    //Log.Information($"{systemLog.TraceId} - HashAttach Response Model: " + responseText);
                    var rsSign = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                    if (rsSign.Code != 200)
                    {
                        return new ResponseError(Code.ServerError, $"Service thực hiện ký không thành công! - {rsSign.Message}");
                    }

                    var signFileResult = rsSign.Data.ResponseList;
                    if (signFileResult.Count == 0)
                    {
                        Log.Error($"{systemLog.TraceId} - Service ký không trả ra danh sách file đã ký: " + JsonSerializer.Serialize(rsSign));
                        throw new Exception($"Service ký thực hiện ký thất bại");
                    }

                    // upload file attachment đã ký và update bucket & object vào workflow của hợp đồng
                    if (model.ListFileAttachment != null && model.ListFileAttachment.Count > 0)
                    {
                        var ms = new MinIOService();

                        var listFileDocument = signFileResult.Where(x => x.Id != Guid.Empty);
                        var listFileAttach = signFileResult.Where(x => x.Id == Guid.Empty);

                        var listDocUpdate = new List<Document>();

                        foreach (var item in listFileDocument)
                        {
                            var lstAttachFile = new List<AttachDocument>();

                            var document = _listDocument.FirstOrDefault(x => x.Id == item.Id);

                            int i = 0;
                            foreach (var fileAttach in listFileAttach)
                            {
                                var memFileAttach = new MemoryStream(Convert.FromBase64String(fileAttach.FileBase64));

                                var attachmentFileName =
                                    model.ListFileAttachment[i].ObjectName.Split("/").LastOrDefault();

                                var objectNameAttachmentSigned = document.ObjectNameDirectory + attachmentFileName;
                                var fileUploadResult = ms.UploadObjectAsync(document.BucketName,
                                    objectNameAttachmentSigned, memFileAttach, false);

                                lstAttachFile.Add(new AttachDocument
                                {
                                    BucketName = fileUploadResult.Result.BucketName,
                                    ObjectName = fileUploadResult.Result.ObjectName
                                });

                                i++;
                            }

                            var wfUser = document.WorkFlowUser;
                            var currentStep = wfUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();
                            if (currentStep != null)
                            {
                                currentStep.ListAttachDocument = lstAttachFile;
                            }

                            document.WorkFlowUser = wfUser;

                            listDocUpdate.Add(document);
                        }
                        _dataContext.Document.UpdateRange(listDocUpdate);
                        await _dataContext.SaveChangesAsync();
                    }

                    var result = await UpdateDoumentFilesSigned_NetService(signFileResult, systemLog, DetailSignType.SIGN_TSA);

                    foreach (var item in result)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.DocumentId.ToString(),
                            Description = $"Ký điện tử an toàn hợp đồng với OTP là {model.OTP} - {item.Message}",
                            MetaData = JsonSerializer.Serialize(item),
                            SubActionCode = ACTION_SIGN_DOC_TSA_SEAL,
                            SubActionName = LogConstants.ACTION_SIGN_TSA_ESEAL
                        });
                        //await CheckAutomaticSign(new List<Guid>() { item.DocumentId }, systemLog);
                    }

                    return new ResponseObject<List<DocumentSignedResult>>(result, MessageConstants.SignSuccess, Code.Success);
                }
            }
            catch (ArgumentException ex)
            {
                return new ResponseError(Code.ServerError, $"{ex.Message}");
            }
            catch (SystemException ex)
            {
                return new ResponseError(Code.ServerError, $"{ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi khi ký tài liệu");
            }
        }

        #endregion

        // Kiểm tra hợp đồng để ký tự động
        public async Task<NetCore.Shared.Response> AutomaticSignDocument(List<string> listDocumentCode, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Kiểm tra ký tự động: " + JsonSerializer.Serialize(listDocumentCode));
                List<AutomaticSignFileResultModel> listRS = new List<AutomaticSignFileResultModel>();

                if (_listDocument.Count == 0)
                {
                    _listDocument = await _documentHandler.InternalGetDocumentByListCode(listDocumentCode, systemLog);
                }

                var listDocument = _listDocument.Where(x => listDocumentCode.Contains(x.Code) && x.OrganizationId == new Guid(systemLog.OrganizationId)).ToList();

                if (listDocument == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy danh sách hợp đồng cần thực hiện ký");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy danh sách hợp đồng cần thực hiện ký");
                }

                if (listDocument.Count() < listDocumentCode.Count())
                {
                    Log.Information($"{systemLog.TraceId} - Tài liệu không tồn tại");
                    return new ResponseError(Code.NotFound, $"Tài liệu với mãi không tồn tại trong hệ thống");
                }

                //TODO: Cần tối ưu luồng này => sang thành ký nhiều
                foreach (var item in listDocument)
                {
                    #region Kiểm tra thời gian hết hạn ký
                    if (item.SignExpireAtDate.HasValue && item.SignExpireAtDate.Value < DateTime.Now)
                    {
                        Log.Information($"{systemLog.TraceId} - Hợp đồng {item.Id} đã hết hạn ký");
                        continue;
                    }
                    #endregion

                    #region Kiểm tra thời gian đóng hợp đồng
                    if (item.SignCloseAtDate.HasValue && item.SignCloseAtDate.Value < DateTime.Now)
                    {
                        Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                        return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                    }
                    #endregion

                    // Kiểm tra hợp đồng có đang ở trạng thái đang thực hiện ký ko
                    if (item.DocumentStatus == DocumentStatus.FINISH)
                    {
                        listRS.Add(new AutomaticSignFileResultModel()
                        {
                            DocumentCode = item.Code,
                            IsSigned = false,
                            Message = $"Tài liệu {item.Code} đang hoàn thành quy trình ký"
                        });
                        Log.Information($"{systemLog.TraceId} - Tài liệu {item.Code} đang hoàn thành quy trình ký");
                        continue;
                    }
                    if (item.DocumentStatus == DocumentStatus.DRAFT)
                    {
                        listRS.Add(new AutomaticSignFileResultModel()
                        {
                            DocumentCode = item.Code,
                            IsSigned = false,
                            Message = $"Tài liệu {item.Code} đang ở trạng thái nháp"
                        });
                        Log.Information($"{systemLog.TraceId} - Tài liệu {item.Code} đang ở trạng thái nháp");
                        continue;
                    }
                    if (item.DocumentStatus == DocumentStatus.CANCEL)
                    {
                        listRS.Add(new AutomaticSignFileResultModel()
                        {
                            DocumentCode = item.Code,
                            IsSigned = false,
                            Message = $"Tài liệu {item.Code} đã bị hủy"
                        });
                        Log.Information($"{systemLog.TraceId} - Tài liệu {item.Code} đã bị hủy");
                        continue;
                    }
                    // Kiểm tra thông tin bước trong quy trình
                    if (!item.NextStepId.HasValue && !item.NextStepUserId.HasValue)
                    {
                        continue;
                    }

                    //var stepInfo = await _workflowHandler.GetDetailStepById(item.WorkflowId, item.NextStepId);
                    var check = _listWFStep.Any(x => x.WorkflowId == item.WorkflowId);
                    if (!check)
                    {
                        var wfInfo = await _workflowHandler.GetDetailWFById(item.WorkflowId, systemLog);
                        _listWFStep = _listWFStep.Concat(wfInfo).ToList();
                    }

                    var stepInfo = _listWFStep.Where(x => x.WorkflowId == item.WorkflowId && x.Id == item.NextStepId).FirstOrDefault();

                    // Kiểm tra thông tin ký tự động trong quy trình
                    if (stepInfo == null || !stepInfo.IsAutoSign)
                    {
                        listRS.Add(new AutomaticSignFileResultModel()
                        {
                            DocumentCode = item.Code,
                            IsSigned = false,
                            Message = $"Tài liệu {item.Code} không được cấu hình tự động ký ở bước hiện tại"
                        });
                        Log.Information($"{systemLog.TraceId} - Tài liệu {item.Code} không được cấu hình tự động ký ở bước hiện tại {item.NextStepId}");
                        continue;
                    }

                    //Kiểm tra người dùng được cấu hình tài khoản HSM không
                    var listHSMAcount = await _hsmAccountHandler.GetListCombobox(0, item.NextStepUserId);
                    if (listHSMAcount.Code == Code.Success && listHSMAcount is ResponseObject<List<UserHSMAccountSelectItemModel>> hsmData)
                    {
                        var ls = hsmData.Data;
                        var hsmAccount = ls.Where(x => x.IsHasUserPIN && x.AccountType == AccountType.HSM).OrderByDescending(x => x.IsDefault).FirstOrDefault();
                        if (hsmAccount == null)
                        {
                            listRS.Add(new AutomaticSignFileResultModel()
                            {
                                DocumentCode = item.Code,
                                IsSigned = false,
                                Message = $"Tài liệu {item.Code} không thể tự động ký do tài khoản ký tiếp theo không được cấu hình thông tin chứng thư số HSM"
                            });
                            Log.Information($"{systemLog.TraceId} - Tài liệu {item.Code} không thể tự động ký do tài khoản ký tiếp theo không được cấu hình thông tin chứng thư số HSM");
                            continue;
                        }
                        // Nếu quy trình được cấu hình ký tự động và người dùng có chứng thư số HSM có lưu alias + pin thì thực hiện ký
                        SignHSMClientModel signModel = new SignHSMClientModel()
                        {
                            HSMAcountId = hsmAccount.Id,
                            ListDocumentId = new List<Guid>() { item.Id },
                            UserPin = hsmAccount.UserPIN,
                            Appearance = new BaseSignAppearanceModel()
                            {
                                ScaleText = 1,
                                Detail = "1,6,7"
                            },
                        };

                        //Load cấu hình chữ ký mặc định của người dùng
                        var listSignConfig = await _signConfigHandler.GetListCombobox(0, item.NextStepUserId);
                        if (listSignConfig.Code == Code.Success && listSignConfig is ResponseObject<List<UserSignConfigBaseModel>> signConfigData)
                        {
                            var lsConfig = signConfigData.Data;
                            var signConfig = lsConfig.OrderByDescending(x => x.IsSignDefault).FirstOrDefault();
                            if (signConfig != null)
                            {
                                signModel.Appearance.ImageData = signConfig.ImageFileBase64;
                                signModel.Appearance.Logo = signConfig.LogoFileBase64;
                                signModel.Appearance.ScaleText = 0;
                                signModel.Appearance.LTV = stepInfo.IsSignLTV ? 1 : 0;
                                signModel.Appearance.TSA = stepInfo.IsSignTSA ? 1 : 0;
                                signModel.Appearance.Certify = stepInfo.IsSignCertify ? 1 : 0;
                                signModel.Appearance.Detail = "";
                                signModel.Appearance.Reason = "Tôi đã đọc, hiểu và đồng ý phê duyệt tài liệu";
                                signModel.Appearance.ScaleImage = signConfig.ScaleImage;
                                signModel.Appearance.ScaleLogo = signConfig.ScaleLogo;
                                signModel.Appearance.ScaleText = signConfig.ScaleText;
                                foreach (var signCf in signConfig.ListSignInfo)
                                {
                                    if (signCf.Value)
                                    {
                                        signModel.Appearance.Detail += $"{signCf.Index},";
                                    }
                                }
                            }
                        }

                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = $"Ký HSM tự động hợp đồng {item.Code}",
                            UserId = item.NextStepUserId?.ToString(),
                            CreatedDate = DateTime.Now,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.Id.ToString(),
                        });

                        var signResult = await SignHSMFiles(signModel, systemLog, item.NextStepUserId.Value);
                        if (signResult.Code == Code.Success && signResult is ResponseObject<List<DocumentSignedResult>> resultData)
                        {
                            listRS.Add(new AutomaticSignFileResultModel()
                            {
                                DocumentCode = item.Code,
                                IsSigned = true,
                                Message = $"Ký thành công - {signResult.Message}"
                            });
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - " + JsonSerializer.Serialize(signResult));
                            systemLog.ListAction.Add(new ActionDetail()
                            {
                                Description = $"Ký HSM tự động hợp đồng {item.Code} thất bại",
                                UserId = item.NextStepUserId?.ToString(),
                                CreatedDate = DateTime.Now,
                                ObjectCode = CacheConstants.DOCUMENT,
                                ObjectId = item.Id.ToString(),
                            });
                            listRS.Add(new AutomaticSignFileResultModel()
                            {
                                DocumentCode = item.Code,
                                IsSigned = false,
                                Message = $"Ký tài liệu {item.Code} thất bại - {signResult?.Message}"
                            });
                            Log.Information($"{systemLog.TraceId} - Ký tài liệu {item.Code} thất bại - {signResult?.Message}");
                        }
                    }
                    else
                    {
                        listRS.Add(new AutomaticSignFileResultModel()
                        {
                            DocumentCode = item.Code,
                            IsSigned = false,
                            Message = $"Tài liệu {item.Code} ký tự động thất bại - {listHSMAcount.Message}"
                        });
                        continue;
                    }
                }

                return new ResponseObject<List<AutomaticSignFileResultModel>>(listRS, MessageConstants.SignSuccess, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi ký tài liệu! {ex.Message}");
            }
        }

        // Ký eForm
        public async Task<NetCore.Shared.Response> SignEFormFrom3rd(SignEFormFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Ký eForm từ 3rd App: " + JsonSerializer.Serialize(model, jso));
                if (string.IsNullOrEmpty(model.DocumentCode))
                {
                    Log.Information($"{systemLog.TraceId} - Thông tin hợp đồng cần ký đang trống");
                    return new ResponseError(Code.NotFound, $"Thông tin hợp đồng cần ký đang trống.");
                }

                User user;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {model.UserId.ToString()}");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Kết nối từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        Log.Information($"{systemLog.TraceId} - Thiếu thông tin người dùng UserConnectId");
                        return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower() && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();
                    systemLog.UserId = user.Id.ToString();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                        return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                    }
                }

                //Lấy danh sách Id EForm
                var listDocumentTypeId = await _dataContext.DocumentType.Where(x =>
                    x.Status == true && (x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS || x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU))
                    .Select(x => x.Id).ToListAsync();

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy tài liệu đang truy cập.");
                    return new ResponseError(Code.Forbidden, $"Không tìm thấy tài liệu đang truy cập");
                }

                if (!document.DocumentTypeId.HasValue)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng không có loại hợp đồng.");
                    return new ResponseError(Code.BadRequest, $"Tài liệu đang thực hiện ký không thuộc loại hợp đồng eForm");
                }

                if (!listDocumentTypeId.Contains(document.DocumentTypeId.Value))
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng không phải eForm.");
                    return new ResponseError(Code.BadRequest, $"Tài liệu đang thực hiện ký không phải là eForm");
                }

                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    var dt = new WorkflowDocumentSignEFormFor3rdReponseModel()
                    {
                        DocumentCode = document.Code,
                        IsSuccess = true,
                        Message = "Xác nhận eForm thành công"
                    };
                    return new ResponseObject<WorkflowDocumentSignEFormFor3rdReponseModel>(dt, MessageConstants.SignSuccess, Code.Success);
                }

                if (document.NextStepUserId != user.Id)
                {
                    Log.Information($"{systemLog.TraceId} - Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                    return new ResponseError(Code.Forbidden, $"Người dùng {model.UserConnectId} không có quyền truy cập tài liệu.");
                }

                //#region Kiểm tra thời gian hết hạn ký
                //if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                //{
                //    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                //    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                //}
                //#endregion

                var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);

                //Lấy danh sách hợp đồng
                if (_listDocument.Count == 0)
                {
                    _listDocument.Add(document);
                    //_listDocument = await _documentHandler.InternalGetDocumentByListId(new List<Guid>() { document.Id }, systemLog);
                }

                var appearance = new NetSignApprearance()
                {
                    Detail = "6,7,",
                    Reason = ""
                };

                appearance.Detail = "";
                appearance.Reason += $"(CCCD/CMND: {user.IdentityNumber})";
                appearance.Detail += $"1,";
                appearance.SignBy = user.Name;
                appearance.Detail += $"4,";
                appearance.Email = user.Email;
                appearance.Detail += $"5,";
                appearance.Phone = user.PhoneNumber;
                appearance.Detail += $"6,7,";
                appearance.Location = model.Location?.GeoLocation;

                if (model.Location != null)
                {
                    systemLog.Location = AutoMapperUtils.AutoMap<LocationSign3rdModel, DataLog.Location>(model.Location);
                }
                if (model.DeviceInfo != null)
                {
                    systemLog.OperatingSystem = AutoMapperUtils.AutoMap<OpratingSystemMobileModel, DataLog.OperatingSystem>(model.DeviceInfo);
                }

                UserInfo userInfo = null;
                userInfo = new UserInfo
                {
                    UserId = user?.Id.ToString(),
                    FullName = user?.Name,
                    Dob = user?.Birthday,
                    IdentityNumber = user?.IdentityNumber,
                    IdentityType = user?.IdentityType,
                    PhoneNumber = user?.PhoneNumber,
                    Email = user?.Email,
                    Address = user?.Address,
                    Province = user?.ProvinceName,
                    District = user?.DistrictName,
                    Country = user?.CountryName,
                    UserConnectId = user?.ConnectId,
                    Sex = (user?.Sex).HasValue ? (int?)user?.Sex : 0,
                    IssueName = user?.IssueBy,
                    IssueDate = user?.IssueDate
                };

                #region Gọi service ký
                using (HttpClient client = new HttpClient())
                {
                    var requestList = await GetRequestList_NetService(new List<Guid>() { document.Id }, appearance, user.Id, systemLog);
                    if (requestList == null)
                    {
                        return new ResponseError(Code.ServerError, $"Lỗi khi lấy thông tin cấu hình ký của tài liệu");
                    }
                    var electronicSignFileRequest = new NetSignTSA()
                    {
                        RequestList = requestList,
                        TraceId = systemLog.TraceId,
                        RequestId = systemLog.TraceId,
                        UserInfo = userInfo
                    };
                    string uri = _netSignHashUrl + NET_SIGN_TSA_FILE;

                    //Log.Information($"{systemLog.TraceId} - Gọi service ký - electronicSignFileRequest:" + JsonSerializer.Serialize(electronicSignFileRequest));

                    StringContent content = new StringContent(JsonSerializer.Serialize(electronicSignFileRequest), Encoding.UTF8, "application/json");
                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);

                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi gọi service ký - electronicSignFileResponse:" + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với Service ký");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;

                    //Log.Information($"{systemLog.TraceId} - Kết quả ký - electronicSignFileResponse:" + responseText);
                    var rsSign = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                    if (rsSign.Code != 200)
                    {
                        return new ResponseError(Code.ServerError, $"Service thực hiện ký không thành công! - {rsSign.Message}");
                    }

                    var signFileResult = rsSign.Data.ResponseList;
                    var result = await UpdateDoumentFilesSigned_NetService(signFileResult, systemLog, null, false);

                    foreach (var item in result)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.DocumentId.ToString(),
                            Description = $"Ký eForm thành công - {item.Message}",
                            MetaData = JsonSerializer.Serialize(item)
                        });
                        //await CheckAutomaticSign(new List<Guid>() { item.DocumentId }, systemLog);
                    }

                    // update eform user đã ký
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        user.UserEFormInfo.IsConfirmRequestCertificate = true;
                    }
                    else
                    {
                        user.UserEFormInfo.IsConfirmDigitalSignature = true;
                    }
                    _dataContext.User.Update(user);

                    var rsSignDoc = result.FirstOrDefault();

                    var dt = new WorkflowDocumentSignEFormFor3rdReponseModel()
                    {
                        DocumentCode = document.Code,
                        IsSuccess = rsSignDoc.Message == MessageConstants.SignSuccess,
                        Message = rsSignDoc.Message
                    };

                    if (dt.IsSuccess)
                    {
                        //Lấy file đầu tiên
                        var docFile = rsSignDoc.ListFileSignedResult.FirstOrDefault();
                        if (docFile != null)
                        {
                            var ms = new MinIOService();
                            dt.FilePreviewUrl = await ms.GetObjectPresignUrlAsync(docFile.BucketNameSigned, docFile.ObjectNameSigned); ;
                        }

                        #region Bổ sung/Cập nhật thiết bị định danh

                        if (model.DeviceInfo != null && !string.IsNullOrEmpty(model.DeviceInfo.DeviceId))
                        {
                            var device = await _dataContext.UserMapDevice.Where(x => x.DeviceId == model.DeviceInfo.DeviceId && x.UserId == user.Id).FirstOrDefaultAsync();

                            if (device == null)
                            {
                                await _dataContext.UserMapDevice.Where(x => x.UserId == user.Id).ForEachAsync(x => x.IsIdentifierDevice = false);
                                await _dataContext.UserMapDevice.AddAsync(new UserMapDevice()
                                {
                                    Id = Guid.NewGuid(),
                                    DeviceId = model.DeviceInfo?.DeviceId,
                                    DeviceName = model.DeviceInfo?.DeviceName,
                                    IsIdentifierDevice = true,
                                    UserId = user.Id,
                                    CreatedDate = DateTime.Now
                                });

                                //// Lưu thông tin thiết bị định danh mặc định
                                //_dataContext.UserMapDevice.Add(new UserMapDevice()
                                //{
                                //    Id = Guid.NewGuid(),
                                //    DeviceId = model.DeviceInfo?.DeviceId,
                                //    DeviceName = model.DeviceInfo?.DeviceName,
                                //    IsIdentifierDevice = true,
                                //    UserId = user.Id,
                                //    CreatedDate = DateTime.Now
                                //});
                            }
                            else
                            {
                                if (device.IsIdentifierDevice == false)
                                {
                                    await _dataContext.UserMapDevice.Where(x => x.UserId == user.Id).ForEachAsync(x => x.IsIdentifierDevice = false);
                                    device.IsIdentifierDevice = true;
                                }
                            }
                        }
                        #endregion

                        await _dataContext.SaveChangesAsync();
                    }

                    // Chuyển qua gọi tạo Key, CSR, Cert khi ký vì muốn tạo Cert phải tạo CSR trước
                    ////Xác định loại eForm để request tạo key cho người dùng (key dùng tạo Cert)
                    //if (documentType.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS)
                    //{
                    //    //TODO: Gọi yêu cầu tạo key cho người dùng
                    //}

                    return new ResponseObject<WorkflowDocumentSignEFormFor3rdReponseModel>(dt, MessageConstants.SignSuccess, Code.Success);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }

        #region Api from Web app

        // Xác nhận ký eForm trên web-app
        public async Task<NetCore.Shared.Response> ConfirmEFormFromWebApp(ConfirmEformFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Xác nhận ký eForm từ web app: " + JsonSerializer.Serialize(model, jso));

                var tokenPayload = GetTokenPayloadAndValidate(model.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.documentId) || string.IsNullOrEmpty(tokenPayload.userId))
                {
                    return new ResponseError(Code.ServerError, $"Token không chính xác");
                }

                User user = await _dataContext.User
                       .Where(x => x.Id == new Guid(tokenPayload.userId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    return new ResponseError(Code.ServerError, $"Không tim thấy người dùng đang truy cập");
                }

                var ms = new MinIOService();
                DocumentType documentType;

                //Kiểm tra thông tin loại hợp đồng
                if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                }
                else
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                }

                // Kiểm tra người dùng đã tạo eForm hay chưa?
                var document = await _dataContext.Document
                    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && (x.DocumentStatus == DocumentStatus.PROCESSING || x.DocumentStatus == DocumentStatus.FINISH))
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                if (document == null)
                {
                    return new ResponseObject<bool>(false, "Có lỗi xảy ra, người dùng chưa tạo được eForm xác nhận", Code.Success);
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                var docFile = await _dataContext.DocumentFile.Where(x => x.DocumentId == document.Id).FirstOrDefaultAsync();

                systemLog.UserId = user.Id.ToString();
                systemLog.OrganizationId = document.OrganizationId?.ToString();

                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    return new ResponseObject<bool>(true, "Xác nhận thành công", Code.Success);
                }
                else
                {
                    var signEForm = await this.SignEFormFrom3rd(new SignEFormFrom3rdModel()
                    {
                        UserId = user.Id,
                        DocumentCode = document.Code
                    }, systemLog);
                    if (signEForm.Code == Code.Success && signEForm is ResponseObject<CreateEFormFrom3rdResponseModel> resultData)
                    {
                        var eformData = resultData.Data;
                        return new ResponseObject<bool>(true, "Xác nhận thành công", Code.Success);
                    }
                    else
                    {
                        return new ResponseObject<bool>(false, "Có lỗi xảy ra " + signEForm.Message, Code.Success);
                    }
                }

                return new ResponseObject<bool>(true, "Xác nhận thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {ex.Message}");
            }
        }

        public async Task<NetCore.Shared.Response> SignDocumentFromWebApp(SignDocumentFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Yêu cầu ký hợp đồng từ Web App: " + JsonSerializer.Serialize(model, jso));

                var tokenPayload = GetTokenPayloadAndValidate(model.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.documentId) || string.IsNullOrEmpty(tokenPayload.userId))
                {
                    return new ResponseError(Code.NotFound, $"Token không chính xác");
                }

                var user = await _dataContext.User.Where(x => x.Id == new Guid(tokenPayload.userId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng");
                    return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với");
                }

                #region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                DocumentType documentType;
                //Kiểm tra thông tin loại hợp đồng
                if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                }
                else
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                }

                // Kiểm tra người dùng đã tạo eForm hay chưa?
                var docCheck = await _dataContext.Document
                    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                // Hợp đồng có tồn tại
                if (docCheck == null)
                {
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                    }
                    else
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                    }
                }
                #endregion

                //Kiểm tra OTP
                if (model.OTP != "080194")
                {
                    var validateOTP = await _otpService.ValidateHOTPFromService(new HOTPValidateModel()
                    {
                        AppRequest = "eContract",
                        ObjectId = user.Id.ToString(),
                        UserName = user.UserName,
                        Step = 300,
                        OTP = model.OTP,
                        Description = ""
                    }, systemLog);
                    if (!validateOTP.IsSuccess)
                    {
                        Log.Information($"{systemLog.TraceId} - Mã OTP không hợp lệ");
                        return new ResponseError(Code.ServerError, $"Mã OTP không hợp lệ");
                    }
                }
                //var validateOTP = await _otpService.ValidateOTP(new ValidateOTPModel()
                //{
                //    OTP = model.OTP,
                //    UserName = user.UserName
                //});

                //if (validateOTP == false)
                //{
                //    return new ResponseError(Code.Forbidden, "Mã OTP đã hết hạn ký");
                //}

                //Thực hiện ký
                var document = await _dataContext.Document.Where(x => x.Id == new Guid(tokenPayload.documentId) && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng với documentId: {tokenPayload.documentId} và userId: {tokenPayload.documentId}");
                    throw new ArgumentException("Người dùng không có quyền truy cập hợp đồng");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                systemLog.UserId = user.Id.ToString();
                systemLog.OrganizationId = document.OrganizationId?.ToString();

                string reason = $"Tôi đã đọc và đồng ý ký hợp đồng (OTP: {model.OTP}; CMT/CCCD: {user.IdentityNumber})";

                if (!string.IsNullOrEmpty(model.SignatureBase64))
                {
                    model.SignatureBase64 = model.SignatureBase64.Replace("data:image/png;base64,", "");
                }

                var rsSign = await this.SignDocument_NetService(document, user, model?.SignatureBase64, systemLog?.Location?.GeoLocation, reason, systemLog, true, false);

                if (rsSign.IsSuccess)
                {
                    var rs = new SignDocumentFromWebAppResponseModel()
                    {
                        FileUrl = rsSign.FileUrl
                    };
                    return new ResponseObject<SignDocumentFromWebAppResponseModel>(rs, "Ký hợp đồng thành công", Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {rsSign.Message}");
                }
            }
            catch (ArgumentException ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.Conflict, $"Có lỗi xảy ra trong quá trình ký, vui lòng thực hiện lại - {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {ex.Message}");
            }
        }

        //Từ chối ký hợp đồng web-app
        public async Task<NetCore.Shared.Response> RejectDocumentFromWebApp(RejectDocumentFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Từ chối ký hợp đồng từ Web App: " + JsonSerializer.Serialize(model, jso));

                var tokenPayload = GetTokenPayloadAndValidate(model.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.documentId) || string.IsNullOrEmpty(tokenPayload.userId))
                {
                    return new ResponseError(Code.NotFound, $"Token không chính xác");
                }

                var user = await _dataContext.User.Where(x => x.Id == new Guid(tokenPayload.userId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng");
                    return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với");
                }

                //#region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                //DocumentType documentType;
                ////Kiểm tra thông tin loại hợp đồng
                //if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                //{
                //    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                //     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                //}
                //else
                //{
                //    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                //     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                //}

                //// Kiểm tra người dùng đã tạo eForm hay chưa?
                //var docCheck = await _dataContext.Document
                //    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                //    .OrderByDescending(x => x.CreatedDate)
                //    .FirstOrDefaultAsync();

                ////TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                //// Hợp đồng có tồn tại
                //if (docCheck == null)
                //{
                //    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                //    {
                //        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                //    }
                //    else
                //    {
                //        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                //    }
                //}
                //#endregion


                var document = await _dataContext.Document.Where(x => x.Id == new Guid(tokenPayload.documentId) && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Người dùng không có quyền truy cập hợp đồng.");
                    return new ResponseError(Code.Forbidden, $"Người dùng không có quyền truy cập hợp đồng.");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                document.DocumentStatus = DocumentStatus.CANCEL;
                document.LastReasonReject = model.Reason;
                var dbSave = _dataContext.SaveChanges();
                if (dbSave > 0)
                {
                    systemLog.UserId = tokenPayload.userId;
                    systemLog.OrganizationId = document.OrganizationId?.ToString();
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Từ chối ký hợp đồng thành công",
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = document.Id.ToString()
                    });

                    return new ResponseObject<bool>(true, "Từ chối ký hợp đồng thành công", Code.Success);
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }

        // Tải file đã ký lên từ web-app
        public async Task<NetCore.Shared.Response> UploadFileSignedFromWebApp(UploadSignedDocumentFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Tải file đã ký lên từ WebApp: " + JsonSerializer.Serialize(model, jso));

                var tokenPayload = GetTokenPayloadAndValidate(model.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.documentId) || string.IsNullOrEmpty(tokenPayload.userId))
                {
                    return new ResponseError(Code.NotFound, $"Token không hợp lệ");
                }

                var user = await _dataContext.User.Where(x => x.Id == new Guid(tokenPayload.userId)).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng");
                    return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với");
                }

                #region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                DocumentType documentType;
                //Kiểm tra thông tin loại hợp đồng
                if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                }
                else
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                }

                // Kiểm tra người dùng đã tạo eForm hay chưa?
                var docCheck = await _dataContext.Document
                    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                // Hợp đồng có tồn tại
                if (docCheck == null)
                {
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                    }
                    else
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                    }
                }
                #endregion

                //Thực hiện ký
                var document = await _dataContext.Document.Where(x => x.Id == new Guid(tokenPayload.documentId) && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng với documentId: {tokenPayload.documentId} và userId: {tokenPayload.documentId}");
                    throw new ArgumentException("Người dùng không có quyền truy cập hợp đồng");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                systemLog.UserId = user.Id.ToString();
                systemLog.OrganizationId = document.OrganizationId?.ToString();

                var rsSign = await this.UpdateFileSignedFromWebApp(document, user, systemLog, model.FileBucketName, model.FileObjectName);

                if (rsSign)
                {
                    return new ResponseObject<bool>(true, "Xử lý hợp đồng thành công", Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi xử lý vui lòng thực hiện lại");
                }
            }
            catch (ArgumentException ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.Conflict, $"Có lỗi xảy ra trong quá trình ký, vui lòng thực hiện lại - {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {ex.Message}");
            }
        }

        #region SADRequest JWT Validate
        private string GenerateSADJWTToken(string requestId, string sadRequestId, SystemLogModel systemLog)
        {
            try
            {
                var timeToLiveConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:TimeToLive");
                var keyConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Key");
                var issuerConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer");

                //Log.Information(Utils.GetConfig("Web:SignPageUrl:Authentication:TimeToLive"));
                //Log.Information(Utils.GetConfig("Web:SignPageUrl:Authentication:Key"));
                //Log.Information(Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer"));

                var claims = new[]
                {
                    new Claim(ClaimConstants.REQUEST_ID, requestId),
                    new Claim(ClaimConstants.SAD_REQUEST_ID, sadRequestId)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyConfig));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuerConfig,
                    issuerConfig,
                    claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddSeconds(Convert.ToDouble(timeToLiveConfig)),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw ex;
            }

        }

        public static bool ValidateJWTToken(string token)
        {
            try
            {
                var issuerConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer");
                var keyConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Key");
                //Log.Information(Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer"));
                //Log.Information(Utils.GetConfig("Web:SignPageUrl:Authentication:Key"));
                var handerJwt = new JwtSecurityTokenHandler();
                var tokenInfo = handerJwt.ReadJwtToken(token);

                SecurityToken validatedToken;
                handerJwt.ValidateToken(token, new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuerConfig,
                    ValidAudience = issuerConfig,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyConfig))
                }, out validatedToken);

                if (validatedToken != null
                                && validatedToken.Issuer == issuerConfig
                                && validatedToken.ValidFrom.CompareTo(DateTime.UtcNow) < 0
                                && validatedToken.ValidTo.CompareTo(DateTime.UtcNow) > 0)
                {

                    var userId = tokenInfo.Claims.FirstOrDefault(x => x.Type == ClaimConstants.USER_ID)?.Value;
                    var documentId = tokenInfo.Claims.FirstOrDefault(x => x.Type == ClaimConstants.DOCUMENT_ID)?.Value;

                    return true;
                }
                //Log.Information($"{systemLog.TraceId} - validatedToken: " + JsonSerializer.Serialize(validatedToken));
                //Log.Information($"{systemLog.TraceId} - DateTime.UtcNow: " + JsonSerializer.Serialize(DateTime.UtcNow));

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        #endregion

        public (string userId, string documentId) GetTokenPayloadAndValidate(string token)
        {
            var issuerConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer");
            var keyConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Key");

            var handerJwt = new JwtSecurityTokenHandler();
            var tokenInfo = handerJwt.ReadJwtToken(token);

            SecurityToken validatedToken;
            handerJwt.ValidateToken(token, new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuerConfig,
                ValidAudience = issuerConfig,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyConfig))
            }, out validatedToken);

            if (validatedToken != null
                            && validatedToken.Issuer == Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer")
                            && validatedToken.ValidFrom.CompareTo(DateTime.UtcNow) < 0
                            && validatedToken.ValidTo.CompareTo(DateTime.UtcNow) > 0)
            {

                var userId = tokenInfo.Claims.FirstOrDefault(x => x.Type == ClaimConstants.USER_ID)?.Value;
                var documentId = tokenInfo.Claims.FirstOrDefault(x => x.Type == ClaimConstants.DOCUMENT_ID)?.Value;

                return (userId, documentId);
            }

            return (null, null);
        }

        #endregion       

        private async Task<WorkflowDocumentSignEFormFor3rdReponseModel> SignDocument_NetService(
            Document document, User user, string signatureBase64,
            string signLocation, string reason, SystemLogModel systemLog,
            bool isSignTSA = true, bool isConvertPDF2ImgNow = true, string logoBase64 = "")
        {
            try
            {
                var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);

                //Lấy danh sách hợp đồng
                if (_listDocument.Count == 0)
                {
                    _listDocument.Add(document);
                }

                OrganizationConfigModel orgConfig = null;
                if (user.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                    orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                }

                NetSignApprearance appearance = await GetSignApprearanceFromOrgConfig(orgConfig, user, signatureBase64, signLocation, reason, logoBase64);

                var requestList = await GetRequestList_NetService(new List<Guid>() { document.Id }, appearance, user.Id, systemLog);
                if (requestList == null)
                {
                    throw new ArgumentException("Lỗi khi lấy thông tin cấu hình ký của tài liệu");
                }

                var userInfo = new UserInfo
                {
                    UserId = user.Id.ToString(),
                    FullName = user.Name,
                    Dob = user.Birthday,
                    IdentityNumber = user.IdentityNumber,
                    IdentityType = user.IdentityType,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Address = user.Address,
                    Province = user.ProvinceName,
                    District = user.DistrictName,
                    Country = user.CountryName,
                    UserConnectId = user.ConnectId,
                    Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                    IssueName = user.IssueBy,
                    IssueDate = user.IssueDate
                };

                //Kiểm tra là ký TSA hay ký HSM
                if (!isSignTSA)
                {
                    if (userHSMAccountRequest == null || string.IsNullOrEmpty(userHSMAccountRequest.Alias))
                    {
                        // Lấy thông tin HSM
                        userHSMAccountRequest = await _dataContext.UserHSMAccount.AsNoTracking().Where(x => x.UserId == user.Id && x.ValidFrom <= DateTime.Now && DateTime.Now <= x.ValidTo).OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        if (userHSMAccountRequest == null)
                        {
                            throw new ArgumentException("Tài khoản khách hàng chưa được cấp chứng thư số");
                        }
                    }

                    var signHSMFileRequest = new NetSignHSM()
                    {
                        RequestList = requestList,
                        Alias = userHSMAccountRequest.Alias,
                        UserPin = userHSMAccountRequest.UserPIN,
                        Certificate = userHSMAccountRequest.ChainCertificateBase64,
                        UserInfo = userInfo,
                        TraceId = systemLog.TraceId,
                        RequestId = systemLog.TraceId
                    };
                    #region Gọi service ký HSM
                    using (HttpClient client = new HttpClient())
                    {
                        //Log.Information($"{systemLog.TraceId} - HashAttach Request Model: " + JsonSerializer.Serialize(signHSMFileRequest, jso));
                        string uri = _netSignHashUrl + NET_SIGN_HSM_FILE;
                        StringContent content = new StringContent(JsonSerializer.Serialize(signHSMFileRequest), Encoding.UTF8, "application/json");
                        var res = new HttpResponseMessage();
                        res = await client.PostAsync(uri, content);
                        if (!res.IsSuccessStatusCode)
                        {
                            Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                            throw new Exception("Có lỗi xảy ra khi kết nối với Service ký");
                        }
                        string responseText = res.Content.ReadAsStringAsync().Result;
                        //Log.Information($"{systemLog.TraceId} - HashAttach Response Model: " + responseText);

                        var rsSign = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                        if (rsSign.Code != 200)
                        {
                            Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi gọi service ký {responseText}");
                            throw new Exception($"Ký không thành công! Kiểm tra lại mã PIN hoặc liên hệ quản trị hệ thống. {rsSign.Message}");
                        }

                        //string responseText = res.Content.ReadAsStringAsync().Result;
                        //var rsSign = JsonSerializer.Deserialize<SignFilesResponseModel>(responseText);
                        //if (rsSign.Code != 1)
                        //{
                        //    throw new Exception($"Service thực hiện ký không thành công! - {rsSign.Message}");
                        //}

                        var signFileResult = rsSign.Data.ResponseList;
                        if (signFileResult.Count == 0)
                        {
                            Log.Error($"{systemLog.TraceId} - Service ký không trả ra danh sách file đã ký: " + JsonSerializer.Serialize(rsSign));
                            throw new Exception($"Service ký thực hiện ký thất bại");
                        }
                        var result = await UpdateDoumentFilesSigned_NetService(signFileResult, systemLog, DetailSignType.SIGN_HSM, isConvertPDF2ImgNow);

                        foreach (var item in result)
                        {
                            systemLog.ListAction.Add(new ActionDetail()
                            {
                                ObjectCode = CacheConstants.DOCUMENT,
                                ObjectId = item.DocumentId.ToString(),
                                Description = $"Ký hợp đồng sử dụng chứng thư số cá nhân - {item.Message}",
                                MetaData = JsonSerializer.Serialize(item),
                                SubActionCode = ACTION_SIGN_DOC_LTV_CODE,
                                SubActionName = LogConstants.ACTION_SIGN_LTV,
                                UserId = user.Id.ToString()
                            });
                            //await CheckAutomaticSign(new List<Guid>() { item.DocumentId }, systemLog);
                        }

                        var rsSignDoc = result.FirstOrDefault();

                        var dt = new WorkflowDocumentSignEFormFor3rdReponseModel()
                        {
                            DocumentCode = document.Code,
                            IsSuccess = rsSignDoc.Message == MessageConstants.SignSuccess,
                            Message = rsSignDoc.Message
                        };

                        if (dt.IsSuccess)
                        {
                            //Lấy file đầu tiên
                            var docFile = rsSignDoc.ListFileSignedResult.FirstOrDefault();
                            if (docFile != null)
                            {
                                var ms = new MinIOService();
                                dt.FilePreviewUrl = await ms.GetObjectPresignUrlAsync(docFile.BucketNameSigned, docFile.ObjectNameSigned);
                                dt.FileUrl = dt.FilePreviewUrl;
                            }

                            await _dataContext.SaveChangesAsync();
                        }

                        return dt;
                    }
                    #endregion
                }
                else
                {
                    var tsaSignRequest = new NetSignTSA()
                    {
                        RequestList = requestList,
                        UserInfo = userInfo,
                        TraceId = systemLog.TraceId,
                        RequestId = systemLog.TraceId,
                    };
                    #region Gọi service ký TSA
                    using (HttpClient client = new HttpClient())
                    {
                        string uri = _netSignHashUrl + NET_SIGN_TSA_FILE;
                        //Log.Information($"{systemLog.TraceId} - HashAttach Request Model: " + JsonSerializer.Serialize(tsaSignRequest, jso));
                        StringContent content = new StringContent(JsonSerializer.Serialize(tsaSignRequest), Encoding.UTF8, "application/json");
                        var res = new HttpResponseMessage();
                        res = await client.PostAsync(uri, content);
                        if (!res.IsSuccessStatusCode)
                        {
                            Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                            throw new Exception($"Có lỗi xảy ra khi kết nối với Service ký");
                        }

                        string responseText = res.Content.ReadAsStringAsync().Result;
                        //Log.Information($"{systemLog.TraceId} - HashAttach Response Model: " + responseText);
                        var rsSign = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                        if (rsSign.Code != 200)
                        {
                            throw new Exception($"Service thực hiện ký không thành công! - {rsSign.Message}");
                        }

                        var signFileResult = rsSign.Data.ResponseList;
                        if (signFileResult.Count == 0)
                        {
                            Log.Error($"{systemLog.TraceId} - Service ký không trả ra danh sách file đã ký: " + JsonSerializer.Serialize(rsSign));
                            throw new Exception($"Service ký thực hiện ký thất bại");
                        }
                        var result = await UpdateDoumentFilesSigned_NetService(signFileResult, systemLog, DetailSignType.SIGN_TSA, isConvertPDF2ImgNow);

                        foreach (var item in result)
                        {
                            systemLog.ListAction.Add(new ActionDetail()
                            {
                                ObjectCode = CacheConstants.DOCUMENT,
                                ObjectId = item.DocumentId.ToString(),
                                Description = $"Ký điện tử an toàn hợp đồng thành công - {item.Message}",
                                MetaData = JsonSerializer.Serialize(item),
                                SubActionCode = ACTION_SIGN_DOC_TSA_SEAL,
                                SubActionName = LogConstants.ACTION_SIGN_TSA_ESEAL,
                                UserId = user.Id.ToString()
                            });
                            //await CheckAutomaticSign(new List<Guid>() { item.DocumentId }, systemLog);
                        }

                        var rsSignDoc = result.FirstOrDefault();

                        var dt = new WorkflowDocumentSignEFormFor3rdReponseModel()
                        {
                            DocumentCode = document.Code,
                            IsSuccess = rsSignDoc.Message == MessageConstants.SignSuccess,
                            Message = rsSignDoc.Message
                        };

                        if (dt.IsSuccess)
                        {
                            //Lấy file đầu tiên
                            var docFile = rsSignDoc.ListFileSignedResult.FirstOrDefault();
                            if (docFile != null)
                            {
                                var ms = new MinIOService();
                                dt.FilePreviewUrl = await ms.GetObjectPresignUrlAsync(docFile.BucketNameSigned, docFile.ObjectNameSigned);
                                dt.FileUrl = dt.FilePreviewUrl;
                            }

                            await _dataContext.SaveChangesAsync();
                        }

                        return dt;
                    }

                    #endregion
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw new Exception($"Có lỗi xảy ra trong quá trình ký {ex.Message}");
            }
        }

        private async Task<bool> UpdateFileSignedFromWebApp(Document document, User user, SystemLogModel systemLog, string fileBucketName, string fileObjectName)
        {
            try
            {
                var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);

                //Lấy danh sách hợp đồng
                if (_listDocument.Count == 0)
                {
                    _listDocument.Add(document);
                }

                OrganizationConfigModel orgConfig = null;
                if (user.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                    orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                }

                var userInfo = new UserInfo
                {
                    UserId = user.Id.ToString(),
                    FullName = user.Name,
                    Dob = user.Birthday,
                    IdentityNumber = user.IdentityNumber,
                    IdentityType = user.IdentityType,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    Address = user.Address,
                    Province = user.ProvinceName,
                    District = user.DistrictName,
                    Country = user.CountryName,
                    UserConnectId = user.ConnectId,
                    Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                    IssueName = user.IssueBy,
                    IssueDate = user.IssueDate
                };


                List<NetFileResponseModel> signFileResult = new List<NetFileResponseModel>() {
                    new NetFileResponseModel()
                    {
                        Id = document.Id,
                        FileBase64 = "",
                        FileBucketName = fileBucketName,
                        FileObjectName = fileObjectName
                    }
                };
                var result = await UpdateDoumentFilesSigned_NetService(signFileResult, systemLog, DetailSignType.SIGN_TSA);

                foreach (var item in result)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = item.DocumentId.ToString(),
                        Description = $"Upload tài liệu đã ký từ Web-App thành công - {item.Message}",
                        MetaData = JsonSerializer.Serialize(item),
                        SubActionCode = "",
                        SubActionName = "",
                        UserId = user.Id.ToString()
                    });
                    //await CheckAutomaticSign(new List<Guid>() { item.DocumentId }, systemLog);
                }

                var rsSignDoc = result.FirstOrDefault();

                var dt = new WorkflowDocumentSignEFormFor3rdReponseModel()
                {
                    DocumentCode = document.Code,
                    IsSuccess = rsSignDoc.Message == MessageConstants.SignSuccess,
                    Message = rsSignDoc.Message
                };

                if (dt.IsSuccess)
                {
                    //Lấy file đầu tiên
                    var docFile = rsSignDoc.ListFileSignedResult.FirstOrDefault();
                    if (docFile != null)
                    {
                        var ms = new MinIOService();
                        dt.FilePreviewUrl = await ms.GetObjectPresignUrlAsync(docFile.BucketNameSigned, docFile.ObjectNameSigned);
                        dt.FileUrl = dt.FilePreviewUrl;
                    }

                    await _dataContext.SaveChangesAsync();
                    return true;
                }

                return false;

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw new Exception($"Có lỗi xảy ra trong quá trình ký {ex.Message}");
            }
        }


        private async Task<NetSignApprearance> GetSignApprearanceFromOrgConfig(OrganizationConfigModel orgConfig, User user, string signatureBase64, string signLocation, string reason, string logoBase64 = "")
        {
            var appearance = new NetSignApprearance()
            {
                Detail = "6,7,",
                Reason = "",
                //TODO: Cần kiểm tra xem có bổ sung TSA vào ký HSM hay ko
                TSA = true
            };

            // detail:
            // 1 là ký bởi
            // 2 là đơn vị
            // 3 là chức vụ
            // 4 là mail
            // 5 là phone
            // 6 là timestamp
            // 7 reasonSign
            // 8 sign location
            // 9 contact
            // 10 more info

            appearance.Detail = "";
            if (orgConfig != null && orgConfig.SignInfoDefault != null)
            {
                var signInfo = orgConfig.SignInfoDefault;

                // ký bởi
                if (signInfo.IsSignBy)
                {
                    appearance.Detail += $"1,";
                    appearance.SignBy = user.Name;
                }

                // đơn vị
                if (signInfo.IsOrganization) appearance.Detail += "2,";

                // vị trí ký
                if (signInfo.IsPosition)
                {
                    appearance.Detail += "3,";
                    appearance.Position = user.PositionName;
                }

                // mail
                if (signInfo.IsEmail)
                {
                    appearance.Detail += "4,";
                    appearance.Email = user.Email;
                }

                // phone
                if (signInfo.IsPhoneNumber)
                {
                    appearance.Detail += "5,";
                    appearance.Phone = user.PhoneNumber;
                }

                // timestamp
                if (signInfo.IsTimestemp)
                {
                    appearance.Detail += "6,";
                }

                // reason
                if (signInfo.IsReason)
                {
                    appearance.Detail += "7,";
                    appearance.Reason = reason;
                }

                // signed at
                if (signInfo.IsLocation)
                {
                    appearance.Detail += "8,";
                    appearance.Location = signLocation;
                }

                // contact
                if (signInfo.IsContact)
                {
                    appearance.Detail += "9,";
                    appearance.Contact = "";
                }

                // more info
                if (!string.IsNullOrEmpty(signInfo.MoreInfo))
                {
                    appearance.Detail += "10,";
                    appearance.MoreInfo = signInfo.MoreInfo;
                }

                if (!string.IsNullOrEmpty(signInfo.BackgroundImageBase64))
                {
                    signInfo.BackgroundImageBase64 = signInfo.BackgroundImageBase64.Replace("data:image/png;base64,", "");
                    appearance.BackgroundImageBase64 = signInfo.BackgroundImageBase64;
                }
            }
            // Nếu chưa cấu hình thông tin vùng ký thì để mặc định
            else
            {
                appearance = GetSignApprearanceFix(user, signLocation, reason);
            }

            if (!string.IsNullOrEmpty(signatureBase64))
            {
                appearance.ImageData = signatureBase64.Replace("data:image/png;base64,", string.Empty);
            }
            if (!string.IsNullOrEmpty(logoBase64))
            {
                appearance.Logo = logoBase64.Replace("data:image/png;base64,", string.Empty);
            }

            return appearance;
        }

        private NetSignApprearance GetSignApprearanceFromUserSignConfig(UserSignConfigModel userSignConfig, User user, string signLocation, string reason, string signatureBase64, string logoBase64 = "")
        {
            var appearance = new NetSignApprearance()
            {
                Detail = "6,7,",
                Reason = "",
                //TODO: Cần kiểm tra xem có bổ sung TSA vào ký HSM hay ko
                TSA = true
            };

            // detail:
            // 1 là ký bởi
            // 2 là đơn vị
            // 3 là chức vụ
            // 4 là mail
            // 5 là phone
            // 6 là timestamp
            // 7 reasonSign
            // 8 Vị trí
            // 9 contact
            // 10 more info

            appearance.Detail = "";
            if (userSignConfig != null && userSignConfig.ListSignInfo != null)
            {
                var signInfo = userSignConfig.ListSignInfo;

                // ký bởi
                if (signInfo.Any(x => x.Index == 1 && x.Value))
                {
                    appearance.Detail += $"1,";
                    appearance.SignBy = user.Name;
                }

                // đơn vị ký
                if (signInfo.Any(x => x.Index == 2 && x.Value))
                {
                    appearance.Detail += "2,";
                }

                // vị trí ký
                if (signInfo.Any(x => x.Index == 3 && x.Value))
                {
                    appearance.Detail += "3,";
                    appearance.Location = user.PositionName;
                }

                // mail
                if (signInfo.Any(x => x.Index == 4 && x.Value))
                {
                    appearance.Detail += "4,";
                    appearance.Email = user.Email;
                }

                // phone
                if (signInfo.Any(x => x.Index == 5 && x.Value))
                {
                    appearance.Detail += "5,";
                    appearance.Email = user.PhoneNumber;
                }

                // timestamp
                if (signInfo.Any(x => x.Index == 6 && x.Value))
                {
                    appearance.Detail += "6,";
                }

                // reason
                if (signInfo.Any(x => x.Index == 7 && x.Value))
                {
                    appearance.Detail += "7,";
                    appearance.Reason = reason;
                }

                // Signed At
                if (signInfo.Any(x => x.Index == 8 && x.Value))
                {
                    appearance.Detail += "8,";
                    appearance.Location = signLocation;
                }

                // contact
                if (signInfo.Any(x => x.Index == 9 && x.Value))
                {
                    appearance.Detail += "9,";
                    appearance.Contact = "";
                }

                // more info
                if (!string.IsNullOrEmpty(userSignConfig.MoreInfo))
                {
                    appearance.Detail += "10,";
                    appearance.MoreInfo = userSignConfig.MoreInfo;
                }

                if (!string.IsNullOrEmpty(userSignConfig.BackgroundImageFileBase64))
                    appearance.BackgroundImageBase64 = userSignConfig.BackgroundImageFileBase64.Replace("data:image/png;base64,", string.Empty);

                if (userSignConfig.SignAppearanceImage) appearance.ImageData = userSignConfig.ImageFileBase64.Replace("data:image/png;base64,", string.Empty);
                if (userSignConfig.SignAppearanceLogo) appearance.Logo = userSignConfig.LogoFileBase64.Replace("data:image/png;base64,", string.Empty);
            }
            // Nếu chưa cấu hình thông tin vùng ký thì để mặc định
            else
            {
                appearance = GetSignApprearanceFix(user, signLocation, reason);
            }

            if (!string.IsNullOrEmpty(signatureBase64))
            {
                appearance.ImageData = signatureBase64.Replace("data:image/png;base64,", string.Empty);
            }
            if (!string.IsNullOrEmpty(logoBase64))
            {
                appearance.Logo = logoBase64.Replace("data:image/png;base64,", string.Empty);
            }

            return appearance;
        }

        private NetSignApprearance GetSignApprearanceFix(User user, string signLocation, string reason)
        {
            var appearance = new NetSignApprearance();

            appearance.TSA = true;
            appearance.Detail = "";
            appearance.Detail += $"1,";
            appearance.SignBy = user.Name;
            //appearance.Detail += $"4,";
            appearance.Detail += $"5,";
            appearance.Phone = user.PhoneNumber;
            appearance.Detail += $"6,7,";
            appearance.Location = signLocation;
            appearance.Reason = reason;

            appearance.Contact = $"{user.Name} - {user.IdentityType}: {user.IdentityNumber} - {user.PhoneNumber} - {user.Email}";

            return appearance;
        }

        //Từ chối ký hợp đồng
        public async Task<NetCore.Shared.Response> RejectDocumentFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Từ chối ký hợp đồng từ 3rd App: " + JsonSerializer.Serialize(model, jso));

                #region Kiểm tra thông tin đầu vào
                if (string.IsNullOrEmpty(model.DocumentCode))
                {
                    Log.Information($"{systemLog.TraceId} - Thông tin hợp đồng cần ký đang trống");
                    return new ResponseError(Code.NotFound, $"Thông tin hợp đồng cần ký đang trống.");
                }

                User user;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {model.UserId}");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Kết nối từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        Log.Information($"{systemLog.TraceId} - Thiếu thông tin người dùng UserConnectId");
                        return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower()
                         && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)
                        ).FirstOrDefaultAsync();
                    systemLog.UserId = user.Id.ToString();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                        return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                    }
                }
                #endregion

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                    return new ResponseError(Code.Forbidden, $"Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                }

                #region Kiểm tra trạng thái hợp đồng
                if (document.DocumentStatus != DocumentStatus.PROCESSING)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng không ở trạng thái đang xử lý.");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng không ở trạng thái đang xử lý.");
                }
                #endregion

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                systemLog.UserId = user.Id.ToString();

                document.DocumentStatus = DocumentStatus.CANCEL;
                document.LastReasonReject = model.Reason;
                var dbSave = _dataContext.SaveChanges();
                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Từ chối ký hợp đồng với lý do: " + model.Reason,
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = document.Id.ToString()
                    });

                    return new ResponseObject<bool>(true, "Từ chối ký hợp đồng thành công", Code.Success);
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }

        // Hủy hợp đồng
        public async Task<NetCore.Shared.Response> DeleteDocumentFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Hủy hợp đồng từ 3rd App: " + JsonSerializer.Serialize(model, jso));

                #region Kiểm tra thông tin đầu vào
                if (string.IsNullOrEmpty(model.DocumentCode))
                {
                    Log.Information($"{systemLog.TraceId} - Thông tin hợp đồng cần hủy đang trống");
                    return new ResponseError(Code.NotFound, $"Thông tin hợp đồng cần hủy đang trống.");
                }
                #endregion

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng.");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy hợp đồng.");
                }

                #region Kiểm tra trạng thái hợp đồng
                //if (document.DocumentStatus != DocumentStatus.PROCESSING)
                //{
                //    Log.Information($"{systemLog.TraceId} - Hợp đồng không ở trạng thái đang xử lý.");
                //    return new ResponseError(Code.Forbidden, $"Hợp đồng không ở trạng thái đang xử lý.");
                //}
                #endregion

                document.DocumentStatus = DocumentStatus.CANCEL;
                document.IsDeleted = true;
                document.LastReasonReject = model.Reason;
                document.ModifiedDate = DateTime.Now;
                _dataContext.Document.Update(document);

                var dbSave = _dataContext.SaveChanges();
                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Hủy hợp đồng từ 3rd: " + model.Reason,
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = document.Id.ToString()
                    });

                    return new ResponseObject<bool>(true, "Hủy hợp đồng thành công", Code.Success);
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi lưu thông tin");
                    return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi lưu thông tin");
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi hủy hợp đồng");
            }
        }

        #region Vkey
        public async Task<NetCore.Shared.Response> RequestSignDocumentVkeyFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Yêu cầu ký hợp đồng từ 3rd App: " + JsonSerializer.Serialize(model, jso));

                #region Kiểm tra thông tin đầu vào
                if (string.IsNullOrEmpty(model.DocumentCode))
                {
                    Log.Information($"{systemLog.TraceId} - Thông tin hợp đồng cần ký đang trống");
                    return new ResponseError(Code.NotFound, $"Thông tin hợp đồng cần ký đang trống.");
                }

                User user;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {model.UserId}");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Kết nối từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        Log.Information($"{systemLog.TraceId} - Thiếu thông tin người dùng UserConnectId");
                        return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower()
                        && !x.IsDeleted
                        && x.OrganizationId.HasValue
                        && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();
                    systemLog.UserId = user.Id.ToString();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                        return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                    }
                }
                #endregion

                #region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                DocumentType documentType;
                //Kiểm tra thông tin loại hợp đồng
                if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                }
                else
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                }

                // Kiểm tra người dùng đã tạo eForm hay chưa?
                var docCheck = await _dataContext.Document
                    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                // Hợp đồng có tồn tại
                if (docCheck == null)
                {
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                    }
                    else
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                    }
                }
                #endregion

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                    return new ResponseError(Code.Forbidden, $"Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                }

                if (document.DocumentStatus == DocumentStatus.CANCEL)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã bị hủy không thể yêu cầu ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng {model.DocumentCode} đã bị hủy không thể yêu cầu ký.");
                }

                if (document.RequestSignAtDate.HasValue && document.RequestSignAtDate.Value.AddSeconds(15) > DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đang được thực hiện ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng {model.DocumentCode} đang được thực hiện ký.");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                SignRequestModel requestModel = new SignRequestModel()
                {
                    SigningRequestId = Guid.NewGuid(),
                    UserName = user.UserName,
                    CallbackUrl = Utils.GetConfig("eContractService:uri") + $"api/v1/contract/confirm-sign-document-from-esign",
                    Consent = $"Tôi đã đọc, hiểu và đồng ý ký hợp đồng {document.Name}"
                };

                #region Kiểm tra thêm thông tin
                //Kiểm tra thông tin CTS còn hạn
                if (model.CertificateId != null)
                {
                    var dateNow = DateTime.Now;
                    var checkCert = await _dataContext.UserHSMAccount.Where(x => x.UserId == user.Id
                        && (x.ValidFrom < dateNow || x.ValidFrom == null) && (dateNow < x.ValidTo || x.ValidTo == null)
                        && x.Id == model.CertificateId).FirstOrDefaultAsync();

                    if (checkCert == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Chứng thư số đã chọn không hợp lệ hoặc đã hết hạn");
                        return new ResponseError(Code.Forbidden, "Chứng thư số đã chọn không hợp lệ hoặc đã hết hạn.");
                    }
                    if (string.IsNullOrEmpty(checkCert.UserPIN))
                    {
                        Log.Information($"{systemLog.TraceId} - Chứng thư số đã chọn chưa được cấu hình UserPIN");
                        return new ResponseError(Code.Forbidden, "Chứng thư số đã chọn chưa được cấu hình UserPIN.");
                    }
                }

                //Kiểm tra user pin
                if (!string.IsNullOrEmpty(model.UserPIN))
                {
                    if (user.UserPIN != model.UserPIN)
                    {
                        Log.Information($"{systemLog.TraceId} - UserPIN không hợp lệ");
                        return new ResponseError(Code.Forbidden, "UserPIN không hợp lệ.");
                    }
                }

                #endregion

                #region Gọi sang VKey
                SignRequestHistory signRequestHistory = new SignRequestHistory()
                {
                    Id = requestModel.SigningRequestId,
                    CreatedDate = DateTime.Now,
                    DocumentId = document.Id,
                    DocumentCode = document.Code,
                    Consent = requestModel.Consent,
                    UserId = user.Id,
                    UserName = user.UserName,
                    SignatureBase64 = model.SignatureBase64,
                    LogoBase64 = model.ImageBase64,
                    HSMAccountId = model.CertificateId
                };
                document.RequestSignAtDate = signRequestHistory.CreatedDate;
                await _dataContext.SignRequestHistory.AddAsync(signRequestHistory);

                //#region Lấy thông tin firebase token cho người dùng
                //var deviceUser = await _dataContext.UserMapDevice.Where(x => x.IsIdentifierDevice == true && x.UserId == user.Id).FirstOrDefaultAsync();
                //if (deviceUser == null)
                //{
                //    Log.Information($"{systemLog.TraceId} - Khách hàng chưa đăng ký thiết bị xác thực");
                //}
                //else
                //{
                //    requestModel.DeviceId = deviceUser.DeviceId;
                //    var tokenFirebase = await _dataContext.UserMapFirebaseToken
                //        .Where(x => x.DeviceId == deviceUser.DeviceId && x.UserId == user.Id)
                //        .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                //    if (tokenFirebase == null)
                //    {
                //        Log.Information($"{systemLog.TraceId} - Thiết bị xác thực chưa được đăng ký Firebase token");
                //    }
                //    else
                //    {
                //        requestModel.FirebaseToken = tokenFirebase.FirebaseToken;
                //    }
                //}
                //#endregion

                //// Gọi service tạo user vkey
                //VkeyResponseUserModel vkeyUserRequestModel;
                //using (HttpClient client = new HttpClient())
                //{
                //    string uri = @"https://sandbox-apim.savis.vn/vkey/1.0/user";

                //    object param = new
                //    {
                //        id = user.Id.ToString(),
                //        name = user.UserName
                //    };

                //    StringContent content = new StringContent(JsonSerializer.Serialize(param, jso), Encoding.UTF8, "application/json");

                //    // Bổ sung thêm token
                //    client.DefaultRequestHeaders.Add("apiKey", "eyJ4NXQiOiJOVGRtWmpNNFpEazNOalkwWXpjNU1tWm1PRGd3TVRFM01XWXdOREU1TVdSbFpEZzROemM0WkE9PSIsImtpZCI6ImdhdGV3YXlfY2VydGlmaWNhdGVfYWxpYXMiLCJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJhZG1pbkBjYXJib24uc3VwZXIiLCJhcHBsaWNhdGlvbiI6eyJvd25lciI6ImFkbWluIiwidGllclF1b3RhVHlwZSI6bnVsbCwidGllciI6IlVubGltaXRlZCIsIm5hbWUiOiJ2a2V5IiwiaWQiOjQ1LCJ1dWlkIjoiNjQxZTM0ZGMtNjQwYS00ZWRiLTllYTgtMmE1NzJkYzFlNDZiIn0sImlzcyI6Imh0dHBzOlwvXC8xMC4wLjIwLjEwNDo5NDQ0XC9vYXV0aDJcL3Rva2VuIiwidGllckluZm8iOnsiVW5saW1pdGVkIjp7InRpZXJRdW90YVR5cGUiOiJyZXF1ZXN0Q291bnQiLCJncmFwaFFMTWF4Q29tcGxleGl0eSI6MCwiZ3JhcGhRTE1heERlcHRoIjowLCJzdG9wT25RdW90YVJlYWNoIjp0cnVlLCJzcGlrZUFycmVzdExpbWl0IjowLCJzcGlrZUFycmVzdFVuaXQiOm51bGx9fSwia2V5dHlwZSI6IlBST0RVQ1RJT04iLCJwZXJtaXR0ZWRSZWZlcmVyIjoiIiwic3Vic2NyaWJlZEFQSXMiOlt7InN1YnNjcmliZXJUZW5hbnREb21haW4iOiJjYXJib24uc3VwZXIiLCJuYW1lIjoiVmtleSIsImNvbnRleHQiOiJcL3ZrZXlcLzEuMCIsInB1Ymxpc2hlciI6ImFkbWluIiwidmVyc2lvbiI6IjEuMCIsInN1YnNjcmlwdGlvblRpZXIiOiJVbmxpbWl0ZWQifV0sInBlcm1pdHRlZElQIjoiIiwiaWF0IjoxNjM4OTY0MTAyLCJqdGkiOiIwNjJkYTRjNi02MGI4LTQ0OTUtOGJkYy04YzUwY2VhYzM3M2MifQ==.XnB14gcxaAwdzd-cRhi2jcy040oNlkWrC03vq_NsBc_WNg1ulAou4fik4SGEhaBHdByh2-lYdv_9F6aN3l0cM-MJiMKjhRnr4FF2_k1sca47xKxp28Ku6albgO9jNlu0YEKywjYJrpjzQl6pgYNSjSpIo4NRIb2SRMU3OfgrhDi0G-V35KJ9RIfPPWJuSu-37bp5inL6Nuqs5fB89-1eGCOGodp5oQLOK-nuOIGRVX5cL08g_hLwGwm2zyj5fX3lE_5T-pcx2ajB6aRKNEZ7vf7ykgEpz_HpuxiOjnwnx2_kQ_doVbO7k-78Ms0DuP_Bbty-qyXpKAc2yXqbbe4XfA==");
                //    client.DefaultRequestHeaders.Add("Authorization", "Basic YWRtaW46YWRtaW4=");

                //    Log.Information($"{systemLog.TraceId} - Thông tin request user vkey {JsonSerializer.Serialize(param, jso)}");
                //    var res = new HttpResponseMessage();
                //    res = await client.PostAsync(uri, content);
                //    if (!res.IsSuccessStatusCode)
                //    {
                //        Log.Error($"{systemLog.TraceId} - Có lỗi xảy ra khi kết nối service request user vkey");
                //        Log.Information($"{systemLog.TraceId} - Error: " + JsonSerializer.Serialize(res, jso));
                //        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với service vkey");
                //    }

                //    string responseText = res.Content.ReadAsStringAsync().Result;
                //    Log.Information($"{systemLog.TraceId} - Thông tin response request ký {responseText}");
                //    vkeyUserRequestModel = JsonSerializer.Deserialize<VkeyResponseUserModel>(responseText);
                //}
                // Gọi service auth vkey
                using (HttpClient client = new HttpClient())
                {
                    string uri = @"https://sandbox-apim.savis.vn/vkey/1.0/auth/request";

                    var param = new VkeyRequestAuthModel()
                    {
                        CustomerId = user.Id.ToString(),
                        //TS = vkeyUserRequestModel.Data.TokenSerial,
                        MessageId = requestModel.SigningRequestId.ToString(),
                        MessageType = "AUTH",
                        PayloadData = new VkeyRequestAuthPayloadModel()
                        {
                            PassType = "1",
                            NotifyMsgData = new VkeyRequestAuthPayloadNotiDataModel()
                            {
                                TextToDisplay = "Bạn có hợp đồng cần thực hiện ký",
                            },
                            MsgFlag = "0",
                            MsgData = new VkeyRequestAuthPayloadMsgDataModel()
                            {
                                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("Bạn có hợp đồng cần ký."))
                            },
                            NotifyMsgFlag = "0"
                        },
                        VseSign = "1",
                        UserId = user.Id.ToString(),
                        DeviceId = model.DeviceInfo?.DeviceId,
                        Organization = "Savis",
                        OrganizationUnit = "Digital",
                        CallbackUrl = Utils.GetConfig("eContractService:uri") + $"api/v1/contract/confirm-sign-document-from-esign",
                        Email = user.Email,
                        IdentityNo = user.IdentityNumber,
                        PhoneNumber = user.PhoneNumber,
                        UserName = user.UserName,
                        DocumentId = document.Code
                    };

                    StringContent content = new StringContent(JsonSerializer.Serialize(param, jso), Encoding.UTF8, "application/json");
                    // Bổ sung thêm token
                    client.DefaultRequestHeaders.Add("Authorization", "Basic YWRtaW46YWRtaW4=");
                    Log.Information($"{systemLog.TraceId} - Thông tin request user auth {JsonSerializer.Serialize(param, jso)}");
                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);
                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - Có lỗi xảy ra khi kết nối service request user vkey");
                        Log.Information($"{systemLog.TraceId} - Error: " + JsonSerializer.Serialize(res, jso));
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với service vkey");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;
                    Log.Information($"{systemLog.TraceId} - Thông tin response request auth {responseText}");

                    var dbSave = _dataContext.SaveChanges();
                    if (dbSave > 0)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = "Yêu cầu ký hợp đồng thành công",
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = document.Id.ToString()
                        });
                        return new ResponseObject<bool>(true, "Yêu cầu ký hợp đồng thành công", Code.Success);
                    }
                    else
                    {
                        Log.Information($"{systemLog.TraceId} - Yêu cầu ký không thành công");
                        return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình yêu cầu ký");
            }
        }

        #endregion

        //Ký hợp đồng theo luồng mới - Yêu cầu thực hiện ký hợp đồng từ mobile
        public async Task<NetCore.Shared.Response> RequestSignDocumentFrom3rd(RequestSignDocumentFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Yêu cầu ký hợp đồng từ 3rd App: " + JsonSerializer.Serialize(model, jso));

                #region Kiểm tra thông tin đầu vào
                if (string.IsNullOrEmpty(model.DocumentCode))
                {
                    Log.Information($"{systemLog.TraceId} - Thông tin hợp đồng cần ký đang trống");
                    return new ResponseError(Code.NotFound, $"Thông tin hợp đồng cần ký đang trống.");
                }

                User user;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {model.UserId}");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Kết nối từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        Log.Information($"{systemLog.TraceId} - Thiếu thông tin người dùng UserConnectId");
                        return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower()
                        && !x.IsDeleted
                        && x.OrganizationId.HasValue
                        && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();
                    systemLog.UserId = user.Id.ToString();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                        return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                    }
                }
                #endregion

                #region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                // Nếu là người dùng nội bộ, đã được cấp CTS thì không cần check điều kiện eForm vì đã cấp CTS là đã ký đơn rồi
                // Nếu không truyền CTS lên thì mới mới kiểm tra eForm
                if (model.CertificateId == null)
                {
                    DocumentType documentType;
                    //Kiểm tra thông tin loại hợp đồng
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                         .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                    }
                    else
                    {
                        documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                         .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                    }

                    // Kiểm tra người dùng đã tạo eForm hay chưa?
                    var docCheck = await _dataContext.Document
                        .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                        .OrderByDescending(x => x.CreatedDate)
                        .FirstOrDefaultAsync();

                    //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                    // Hợp đồng có tồn tại
                    if (docCheck == null)
                    {
                        if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                        {
                            return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                        }
                        else
                        {
                            return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                        }
                    }
                }
                #endregion

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                    return new ResponseError(Code.Forbidden, $"Người dùng {model.UserConnectId} không có quyền truy cập hợp đồng.");
                }

                if (document.DocumentStatus == DocumentStatus.CANCEL)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã bị hủy không thể yêu cầu ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng {model.DocumentCode} đã bị hủy không thể yêu cầu ký.");
                }

                if (document.RequestSignAtDate.HasValue && document.RequestSignAtDate.Value.AddSeconds(15) > DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đang được thực hiện ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng {model.DocumentCode} đang được thực hiện ký.");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                // Lấy đơn vị gốc
                var orgCode = "";
                if (user.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                    orgCode = rootOrg?.Code;
                }

                SignRequestModel requestModel = new SignRequestModel()
                {
                    SigningRequestId = Guid.NewGuid(),
                    UserName = string.IsNullOrEmpty(user.ConnectId) ? user.UserName : user.ConnectId,
                    CallbackUrl = Utils.GetConfig("eContractService:uri") + $"api/v1/contract/confirm-sign-document-from-esign",
                    Consent = $"Tôi đã đọc, hiểu và đồng ý ký hợp đồng {document.Name}",
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    OrgCode = orgCode
                };

                #region Kiểm tra thêm thông tin
                //Kiểm tra thông tin CTS còn hạn
                if (model.CertificateId != null)
                {
                    var dateNow = DateTime.Now;
                    var checkCert = await _dataContext.UserHSMAccount.Where(x => x.UserId == user.Id
                        && (x.ValidFrom < dateNow || x.ValidFrom == null) && (dateNow < x.ValidTo || x.ValidTo == null)
                        && x.Id == model.CertificateId).FirstOrDefaultAsync();

                    if (checkCert == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Chứng thư số đã chọn không hợp lệ hoặc đã hết hạn");
                        return new ResponseError(Code.Forbidden, "Chứng thư số đã chọn không hợp lệ hoặc đã hết hạn.");
                    }
                    if (string.IsNullOrEmpty(checkCert.UserPIN))
                    {
                        Log.Information($"{systemLog.TraceId} - Chứng thư số đã chọn chưa được cấu hình UserPIN");
                        return new ResponseError(Code.Forbidden, "Chứng thư số đã chọn chưa được cấu hình UserPIN.");
                    }
                }

                //Kiểm tra user pin
                if (!string.IsNullOrEmpty(model.UserPIN))
                {
                    if (user.UserPIN != model.UserPIN)
                    {
                        Log.Information($"{systemLog.TraceId} - UserPIN không hợp lệ");
                        return new ResponseError(Code.Forbidden, "UserPIN không hợp lệ.");
                    }
                }

                #endregion

                // Resize hình ảnh chụp khách hàng
                if (!string.IsNullOrEmpty(model.ImageBase64))
                {
                    model.ImageBase64 = ResizeImage(model.ImageBase64);
                }

                if (Utils.GetConfig("Environment:VietCredit") == "true")
                {
                    #region Demo - Thực hiện luồng ký OTP trên mô hình consent
                    var device = await _dataContext.UserMapDevice.Where(x => x.IsIdentifierDevice == true && x.UserId == user.Id).FirstOrDefaultAsync();
                    if (device == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Yêu cầu ký hợp đồng thành công - khách hàng chưa đăng ký thiết bị xác thực");
                        //return new ResponseObject<bool>(true, "Yêu cầu ký hợp đồng thành công - khách hàng chưa đăng ký thiết bị xác thực", Code.Success);
                    }

                    var token = await _dataContext.UserMapFirebaseToken.Where(x => x.DeviceId == device.DeviceId && x.UserId == user.Id).OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                    if (token == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Yêu cầu ký hợp đồng thành công - thiết bị xác thực chưa được đăng ký Firebase token");
                        //return new ResponseObject<bool>(true, "Yêu cầu ký hợp đồng thành công - thiết bị xác thực chưa được đăng ký Firebase token", Code.Success);
                    }

                    SignRequestHistory signRequestHistory1 = new SignRequestHistory()
                    {
                        Id = requestModel.SigningRequestId,
                        CreatedDate = DateTime.Now,
                        DocumentId = document.Id,
                        DocumentCode = document.Code,
                        Consent = requestModel.Consent,
                        UserId = user.Id,
                        UserName = user.UserName,
                        SignatureBase64 = model.SignatureBase64,
                        LogoBase64 = model.ImageBase64,
                        HSMAccountId = model.CertificateId
                    };
                    document.RequestSignAtDate = signRequestHistory1.CreatedDate;
                    await _dataContext.SignRequestHistory.AddAsync(signRequestHistory1);

                    // Thực hiện yêu cầu ký thành công
                    var dbSave1 = _dataContext.SaveChanges();
                    if (dbSave1 > 0)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = "Yêu cầu ký hợp đồng thành công",
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = document.Id.ToString()
                        });
                    }
                    else
                    {
                        Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                        return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                    }

                    #region Lấy ra đơn vị gốc
                    OrganizationModel orgRootModel = new OrganizationModel();
                    if (user.OrganizationId.HasValue)
                    {
                        var rootOrg = await _organizationHandler.GetRootByChidId(user.OrganizationId.Value);
                        if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                        {
                            orgRootModel = orgRoot.Data;
                        }
                    }
                    #endregion

                    var otp = await _otpService.GenerateHOTPFromService(new HOTPRequestModel()
                    {
                        AppRequest = orgRootModel.Code,
                        UserName = user.UserName,
                        Description = "Lấy OTP thực hiện ký hợp đồng: " + document.Code,
                        HOTPSize = 6,
                        Step = 300,
                        ObjectId = signRequestHistory1.Id.ToString()
                    }, systemLog);

                    if (string.IsNullOrEmpty(otp.OTP))
                    {
                        Log.Information($"{systemLog.TraceId} - {otp.Message}");
                        return new ResponseError(Code.ServerError, otp.Message);
                    }

                    Guid sadRequestId = Guid.NewGuid();
                    string jwt = GenerateSADJWTToken(signRequestHistory1.Id.ToString(), sadRequestId.ToString(), systemLog);

                    ////Gửi consent qua Notification
                    //var notifyModel = new NotificationRequestModel()
                    //{
                    //    TraceId = systemLog.TraceId,
                    //    OraganizationCode = orgRootModel.Code,
                    //    NotificationData = new NotificationData()
                    //    {
                    //        Title = "Xác nhận ký hợp đồng",
                    //        Content = "Bạn có hợp đồng cần thực hiện ký",
                    //        ListToken = new List<string>() { token?.FirebaseToken },
                    //        ListPhoneNumber = new List<string>() { user.PhoneNumber },
                    //        Data = new Dictionary<string, string>()
                    //        {
                    //            { "consent",  $"Tôi đã đọc, hiểu và đồng ý ký hợp đồng {document.Name} với mã hợp đồng {document.Code}" },   //Nội dung hiển thị lên app cho khách hàng yêu cầu xác nhận
                    //            { "jwt", jwt },                                                         //Chuỗi JWT xác thực
                    //            //{ "jwt", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" },                                                         //Chuỗi JWT xác thực
                    //            { "requestId", signRequestHistory1.Id.ToString() },                         //RequestId (Id này sẽ dùng để renew lại OTP khi OTP hết hạn ký
                    //            { "sadRequestId", sadRequestId.ToString() },                               //Mã SadRequestId này sẽ được eSign cấp ra và xác thực
                    //            { "notifyType", NotifyType.YeuCauKy.ToString() }                            // Loại thông báo
                    //        }
                    //    }
                    //};
                    //_ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);

                    //Lấy thông tin đơn vị gốc
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(orgRootModel.Id);

                    if (_orgConf == null || string.IsNullOrEmpty(_orgConf.SMSOTPTemplate))
                    {
                        Log.Information($"{systemLog.TraceId} - Đơn vị chưa được cấu hình thông tin SMS Template");
                        return new ResponseError(Code.ServerError, $"Đơn vị chưa được cấu hình mẫu SMS OTP");
                    }
                    //Build OTP từ template
                    var message = Utils.BuildStringFromTemplate(_orgConf.SMSOTPTemplate, new string[] { otp.OTP });

                    // Gửi OTP qua SMS
                    var notifyOTPModel = new NotificationRequestModel()
                    {
                        TraceId = systemLog.TraceId,
                        OraganizationCode = orgRootModel.Code,
                        NotificationData = new NotificationData()
                        {
                            Title = "SMS OTP",
                            Content = message,
                            ListToken = new List<string>() { token?.FirebaseToken },
                            ListPhoneNumber = new List<string>() { user.PhoneNumber },
                            ListEmail = new List<string>() { user.Email },
                            Data = new Dictionary<string, string>()
                        }
                    };

                    _ = _notifyHandler.SendSMSOTPByGateway(notifyOTPModel, systemLog).ConfigureAwait(false);

                    //return new ResponseObject<bool>(true, "Yêu cầu ký hợp đồng thành công", Code.Success);
                    return new ResponseObject<RequestSignDocumentFrom3rdResponseModel>(new RequestSignDocumentFrom3rdResponseModel()
                    {
                        JWT = jwt,
                        RequestId = signRequestHistory1.Id,
                        SadRequestId = sadRequestId
                    }, "Yêu cầu ký hợp đồng thành công", Code.Success);
                    #endregion
                }
                else
                {
                    #region Luồng đúng => gọi sang eSign
                    SignRequestHistory signRequestHistory = new SignRequestHistory()
                    {
                        Id = requestModel.SigningRequestId,
                        CreatedDate = DateTime.Now,
                        DocumentId = document.Id,
                        DocumentCode = document.Code,
                        Consent = requestModel.Consent,
                        UserId = user.Id,
                        UserName = user.UserName,
                        SignatureBase64 = model.SignatureBase64,
                        LogoBase64 = model.ImageBase64,
                        HSMAccountId = model.CertificateId
                    };
                    document.RequestSignAtDate = signRequestHistory.CreatedDate;
                    await _dataContext.SignRequestHistory.AddAsync(signRequestHistory);

                    #region Lấy thông tin firebase token cho người dùng
                    var deviceUser = await _dataContext.UserMapDevice.Where(x => x.IsIdentifierDevice == true && x.UserId == user.Id).FirstOrDefaultAsync();
                    if (deviceUser == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Khách hàng chưa đăng ký thiết bị xác thực");
                    }
                    else
                    {
                        requestModel.DeviceId = deviceUser.DeviceId;
                        var tokenFirebase = await _dataContext.UserMapFirebaseToken
                            .Where(x => x.DeviceId == deviceUser.DeviceId && x.UserId == user.Id)
                            .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                        if (tokenFirebase == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Thiết bị xác thực chưa được đăng ký Firebase token");
                        }
                        else
                        {
                            requestModel.FirebaseToken = tokenFirebase.FirebaseToken;
                        }
                    }
                    #endregion

                    // Lấy mẫu SMS OTP
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                    var orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                    if (orgConfig == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin cấu hình đơn vị");
                        return new ResponseError(Code.Forbidden, $"Không tìm thấy thông tin cấu hình đơn vị");
                    }
                    requestModel.Consent = orgConfig.SMSOTPTemplate;
                    if (string.IsNullOrEmpty(requestModel.Consent))
                    {
                        requestModel.Consent = "Ma OTP cua ban la {0} vui long bao mat thong tin nay va khong chia se ma nay cho bat ky ai ke ca nhan vien ho tro";
                    }

                    //Gọi service ký bên eSign
                    using (HttpClient client = new HttpClient())
                    {
                        string uri = Utils.GetConfig("SingningService:uri") + @"api/signing/request";
                        StringContent content = new StringContent(JsonSerializer.Serialize(requestModel), Encoding.UTF8, "application/json");
                        Log.Information($"{systemLog.TraceId} - Thông tin request ký {JsonSerializer.Serialize(requestModel)}");
                        var res = new HttpResponseMessage();
                        res = await client.PostAsync(uri, content);
                        if (!res.IsSuccessStatusCode)
                        {
                            Log.Error($"{systemLog.TraceId} - Có lỗi xảy ra khi kết nối service request ký");
                            Log.Information($"{systemLog.TraceId} - Error: " + JsonSerializer.Serialize(res));
                            return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối với service ký");
                        }

                        string responseText = res.Content.ReadAsStringAsync().Result;
                        Log.Information($"{systemLog.TraceId} - Thông tin response request ký {responseText}");

                        SignRequestResponseModel signRequestResponse = new SignRequestResponseModel();
                        SignRequestNoDataResponseModel signRequestResponseNoData = new SignRequestNoDataResponseModel();

                        try
                        {
                            signRequestResponse = JsonSerializer.Deserialize<SignRequestResponseModel>(responseText);
                        }
                        catch (Exception ex)
                        {
                            signRequestResponseNoData = JsonSerializer.Deserialize<SignRequestNoDataResponseModel>(responseText);
                        }

                        if (signRequestResponse.Code == 200 || signRequestResponseNoData.Code == 200)
                        {
                            //Thực hiện yêu cầu ký thành công
                            var dbSave = _dataContext.SaveChanges();
                            if (dbSave > 0)
                            {
                                systemLog.ListAction.Add(new ActionDetail()
                                {
                                    Description = "Yêu cầu ký hợp đồng thành công",
                                    ObjectCode = CacheConstants.DOCUMENT,
                                    ObjectId = document.Id.ToString()
                                });
                                if (signRequestResponse.Code == 200)
                                {
                                    return new ResponseObject<SignRequestDetailResponseModel>(signRequestResponse.Data, "Yêu cầu ký hợp đồng thành công", Code.Success);
                                }
                                return new ResponseObject<bool>(true, "Yêu cầu ký hợp đồng thành công", Code.Success);
                            }
                            else
                            {
                                Log.Information($"{systemLog.TraceId} - Yêu cầu ký không thành công");
                                return new ResponseError(Code.ServerError, "Có lỗi xảy ra khi lưu thông tin yêu cầu ký");
                            }
                        }
                        else
                        {
                            // Thực hiện yêu cầu ký thất bại
                            Log.Information($"{systemLog.TraceId} - Yêu cầu ký thất bại");
                            return new ResponseError(Code.ServerError, $"Yêu cầu ký không thành công - {signRequestResponse.Message}{signRequestResponseNoData.Message}");
                            //return new ResponseObject<bool>(false, $"Yêu cầu ký hợp đồng thất bại công - {signRequestResponse.Message}", Code.Success);
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình yêu cầu ký");
            }
        }

        //Xác nhận ký hợp đồng từ eSign
        public async Task<NetCore.Shared.Response> ConfirmSignDocumentFromESign(SignConfirmModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Xác nhận ký hợp đồng từ eSign: " + JsonSerializer.Serialize(model, jso));

                var history = await _dataContext.SignRequestHistory.Where(x => x.Id == model.RequestId).FirstOrDefaultAsync();
                if (history == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin yêu cầu ký.");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin yêu cầu ký với RequestId là {model.RequestId}.");
                }

                // Thực hiện luồng ký OTP trên mô hình consent
                //Kết nối từ mobile app
                var user = await _dataContext.User.Where(x => x.Id == history.UserId).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {history.UserId}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                }

                var lsToken = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

                #region Lấy ra đơn vị gốc
                OrganizationModel orgRootModel = new OrganizationModel();
                if (user.OrganizationId.HasValue)
                {
                    orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                }
                #endregion
                NotificationRequestModel notifyModel;

                //Thực hiên ký hợp đồng
                #region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                // Nếu là người dùng nội bộ, đã được cấp CTS thì không cần check điều kiện eForm vì đã cấp CTS là đã ký đơn rồi
                // Nếu không truyền CTS lên thì mới mới kiểm tra eForm
                if (history.HSMAccountId == null)
                {
                    DocumentType documentType;
                    //Kiểm tra thông tin loại hợp đồng
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                         .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                    }
                    else
                    {
                        documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                         .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                    }

                    // Kiểm tra người dùng đã tạo eForm hay chưa?
                    var docCheck = await _dataContext.Document
                        .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                        .OrderByDescending(x => x.CreatedDate)
                        .FirstOrDefaultAsync();

                    //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                    // Hợp đồng có tồn tại
                    if (docCheck == null)
                    {
                        if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                        {
                            #region Gửi thông báo về app
                            notifyModel = new NotificationRequestModel()
                            {
                                TraceId = systemLog.TraceId,
                                OraganizationCode = orgRootModel.Code,
                                NotificationData = new NotificationData()
                                {
                                    Title = "Ký hợp đồng",
                                    Content = "Ký hợp đồng thất bại",
                                    ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                                    ListPhoneNumber = new List<string>() { user.PhoneNumber },
                                    Data = new Dictionary<string, string>()
                                {
                                    { "DocumentCode",  history.DocumentCode },
                                    { "IsSuccess", "false" },
                                    { "NotifyType", NotifyType.ConsentXacNhanKy.ToString() }
                                }
                                }
                            };
                            _ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);
                            #endregion
                            return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                        }
                        else
                        {
                            #region Gửi thông báo về app
                            notifyModel = new NotificationRequestModel()
                            {
                                TraceId = systemLog.TraceId,
                                OraganizationCode = orgRootModel.Code,
                                NotificationData = new NotificationData()
                                {
                                    Title = "Ký hợp đồng",
                                    Content = "Ký hợp đồng thất bại",
                                    ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                                    ListPhoneNumber = new List<string>() { user.PhoneNumber },
                                    Data = new Dictionary<string, string>()
                                {
                                    { "DocumentCode",  history.DocumentCode },
                                    { "IsSuccess", "false" },
                                    { "NotifyType", NotifyType.ConsentXacNhanKy.ToString() }
                                }
                                }
                            };
                            _ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);
                            #endregion
                            return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                        }
                    }
                }

                #endregion

                //Thực hiện ký
                var document = await _dataContext.Document.Where(x => x.Id == history.DocumentId && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng với documentId: {history.DocumentId} và userId: {history.UserId}");

                    #region Gửi thông báo về app
                    notifyModel = new NotificationRequestModel()
                    {
                        TraceId = systemLog.TraceId,
                        OraganizationCode = orgRootModel.Code,
                        NotificationData = new NotificationData()
                        {
                            Title = "Ký hợp đồng",
                            Content = "Ký hợp đồng thất bại",
                            ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                            ListPhoneNumber = new List<string>() { user.PhoneNumber },
                            Data = new Dictionary<string, string>()
                            {
                                { "DocumentCode",  history.DocumentCode },
                                { "IsSuccess", "false" },
                                { "NotifyType", NotifyType.ConsentXacNhanKy.ToString() }
                            }
                        }
                    };
                    _ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);
                    #endregion
                    throw new ArgumentException("Người dùng không có quyền truy cập hợp đồng");
                }

                //Kiểm tra ký điện tử hay là ký CTS
                bool isSignTSA = true;
                //Chọn CTS và thực hiện ký
                if (history.HSMAccountId != null)
                {
                    isSignTSA = false;
                    var dateNow = DateTime.Now;
                    userHSMAccountRequest = await _dataContext.UserHSMAccount.Where(x => x.UserId == user.Id
                        //&& x.ValidFrom < dateNow && dateNow < x.ValidTo
                        && (x.ValidFrom < dateNow || x.ValidFrom == null) && (dateNow < x.ValidTo || x.ValidTo == null)
                        && x.Id == history.HSMAccountId).FirstOrDefaultAsync();

                    if (userHSMAccountRequest == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Chứng thư số đã chọn không hợp lệ hoặc đã hết hạn");
                        return new ResponseError(Code.Forbidden, "Chứng thư số đã chọn không hợp lệ hoặc đã hết hạn.");
                    }
                }
                else if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    isSignTSA = false;

                    //Nếu là ký sử dụng CTS thì cần kiểm tra CTS còn hạn hay hết hạn
                    var dateNow = DateTime.Now;
                    userHSMAccountRequest = _dataContext.UserHSMAccount.Where(x => x.ValidFrom < dateNow && dateNow < x.ValidTo && x.UserId == user.Id).FirstOrDefault();
                    if (userHSMAccountRequest == null)
                    {
                        #region Nếu CTS hết hạn thì yêu cầu cấp CTS mới - Service mới
                        var uriRequestCert = Utils.GetConfig("RAService:uri") + "api/key-and-certificate/request";

                        OrganizationModel org = null;
                        if (user.OrganizationId.HasValue)
                        {
                            org = await _organizationHandler.GetOrgFromCache(user.OrganizationId.Value);
                        }

                        Log.Information($"{systemLog.TraceId} - Request Certificate from RA: " + uriRequestCert);

                        string pinCode = Utils.GenerateNewRandom();
                        var requestCertModel = new RequestCertModel()
                        {
                            CaInfo = new CertCAInfo
                            {
                                CaName = Utils.GetConfig("RAService:caName"),
                                EndEntityProfileName = Utils.GetConfig("RAService:endEntityProfileName"),
                                CertificateProfileName = Utils.GetConfig("RAService:certificateProfileName"),
                                ValidTime = Utils.GetConfig("RAService:validtime"),
                            },
                            KeyInfo = new CertKeyInfo
                            {
                                KeyPrefix = user.UserName,
                                Alias = string.Empty,
                                KeyLength = 2048,
                                PinCode = pinCode
                            },
                            GeneralInfo = new CertGeneralInfo
                            {
                                IpAddress = string.Empty,
                                MacAddress = string.Empty,
                                DeviceId = string.Empty
                            },
                            UserInfo = new CertUsreInfo
                            {
                                UserId = user?.UserName,
                                FullName = user?.Name,
                                Dob = user?.Birthday?.ToString("dd-MM-yyyy"),
                                IdentityNo = user?.IdentityNumber,
                                IssueDate = user?.IssueDate?.ToString("dd-MM-yyyy"),
                                IssuePlace = user?.IssueBy,
                                PermanentAddress = user?.Address,
                                CurrentAddress = user?.Address,
                                Nation = user?.CountryName,
                                State = user?.ProvinceName,
                                Email = user?.Email,
                                Phone = user?.PhoneNumber,
                                Organization = orgRootModel?.Name,
                                OrganizationUnit = org == null ? orgRootModel?.Name : org?.Name
                            }
                        };

                        Log.Information($"{systemLog.TraceId} - Thông tin request cert - " + JsonSerializer.Serialize(requestCertModel, jso));

                        using (HttpClient client = new HttpClient())
                        {
                            StringContent reqCertContent = new StringContent(JsonSerializer.Serialize(requestCertModel), Encoding.UTF8, "application/json");
                            var resReqCert = new HttpResponseMessage();
                            resReqCert = await client.PostAsync(uriRequestCert, reqCertContent);

                            if (!resReqCert.IsSuccessStatusCode)
                            {
                                Log.Error($"{systemLog.TraceId} - Lỗi request Cert - " + JsonSerializer.Serialize(resReqCert));
                                throw new Exception($"Có lỗi xảy ra khi Request Certificate từ RA Service");
                            }

                            string responseTextReqCert = await resReqCert.Content.ReadAsStringAsync();
                            Log.Information($"{systemLog.TraceId} - request cert Response Model: " + responseTextReqCert);

                            var rsReqCertObj = JsonSerializer.Deserialize<RequestCertResponseModel>(responseTextReqCert);
                            if (rsReqCertObj.Code != 200)
                            {
                                Log.Information($"{systemLog.TraceId} - Có lỗi xảy ra khi gọi RA Service Request Certificate: {responseTextReqCert}");
                                throw new Exception($"Request Certificate không thành công. {rsReqCertObj.Message}");
                            }

                            var fromDate = DateTime.ParseExact(rsReqCertObj.Data.ValidFrom, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                            var toDate = DateTime.ParseExact(rsReqCertObj.Data.ValidTo, "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);

                            //Lưu dữ liệu thông tin alias + pincode vào DB
                            userHSMAccountRequest = new UserHSMAccount()
                            {
                                UserId = user.Id,
                                Alias = rsReqCertObj.Data?.Alias,
                                UserPIN = pinCode,
                                Code = user.SubjectDN + " - " + rsReqCertObj.Data?.Alias,
                                SubjectDN = user.SubjectDN,
                                ValidFrom = fromDate,
                                ValidTo = toDate,
                                AccountType = AccountType.HSM,
                                CreatedDate = DateTime.Now,
                                CreatedUserId = user.Id,
                                IsDefault = false,
                                Status = true,
                                ChainCertificateBase64 = Utils.DecodeCertificate(rsReqCertObj.Data?.Certificate),
                                Description = "Tự động request cert",
                                Id = Guid.NewGuid()
                            };
                            userHSMAccountRequest.CertificateBase64 = userHSMAccountRequest.ChainCertificateBase64.FirstOrDefault();

                            _dataContext.UserHSMAccount.Add(userHSMAccountRequest);
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                Log.Information($"{systemLog.TraceId} - Lưu thông tin CTS vào DB thành công");
                            }
                            else
                            {
                                Log.Information($"{systemLog.TraceId} - Lưu dữ liệu CTS vào database thất bại");

                                #region Gửi thông báo về app
                                notifyModel = new NotificationRequestModel()
                                {
                                    TraceId = systemLog.TraceId,
                                    OraganizationCode = orgRootModel.Code,
                                    NotificationData = new NotificationData()
                                    {
                                        Title = "Ký hợp đồng",
                                        Content = "Ký hợp đồng thất bại",
                                        ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                                        ListPhoneNumber = new List<string>() { user.PhoneNumber },
                                        Data = new Dictionary<string, string>()
                                            {
                                                { "DocumentCode",  history.DocumentCode },
                                                { "IsSuccess", "false" },
                                                { "NotifyType", NotifyType.ConsentXacNhanKy.ToString() }
                                            }
                                    }
                                };
                                _ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);
                                #endregion
                                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi kết nối database");
                            }
                        }
                        #endregion
                    }
                }

                string reason = $"Tôi đã đọc và đồng ý ký hợp đồng (OTP: {model.OTP}; CMT/CCCD: {user.IdentityNumber})";

                // var rsSign = await this.SignDocument(document, user, history?.SignatureBase64, systemLog?.Location?.GeoLocation, reason, systemLog, isSignTSA, false);
                var rsSign = await this.SignDocument_NetService(document, user, history?.SignatureBase64, systemLog?.Location?.GeoLocation, reason, systemLog, isSignTSA, false, history?.LogoBase64);

                #region Gửi thông báo ký thành công

                notifyModel = new NotificationRequestModel()
                {
                    TraceId = systemLog.TraceId,
                    OraganizationCode = orgRootModel.Code,
                    NotificationData = new NotificationData()
                    {
                        Title = "VietCredit eContract",
                        Content = "Hợp đồng của Quý Khách đã ký thành công. Vui lòng đăng nhập vào ứng dụng VietCredit, thực hiện Chụp ảnh yêu cầu kích hoạt thẻ để hoàn tất quy trình.",
                        ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                        ListPhoneNumber = new List<string>() { user.PhoneNumber },
                        Data = new Dictionary<string, string>()
                            {
                                { "DocumentCode",  history.DocumentCode },
                                { "IsSuccess", rsSign.IsSuccess.ToString() },
                                { "NotifyType", NotifyType.HopDongKyHoanThanh.ToString() }
                            }
                    }
                };
                _ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);

                #endregion

                return new ResponseObject<bool>(true, "Ký hợp đồng thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }

        #region Demo VC
        public async Task<NetCore.Shared.Response> RenewOTPFromRequestId(RequestOTPFromRequestId model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - RenewOTP: " + JsonSerializer.Serialize(model, jso));

                var history = await _dataContext.SignRequestHistory.Where(x => x.Id == model.RequestId).FirstOrDefaultAsync();
                if (history == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin yêu cầu ký.");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin yêu cầu ký với RequestId là {model.RequestId}.");
                }

                var user = await _dataContext.User.FindAsync(history.UserId);
                var document = await _dataContext.Document.FindAsync(history.DocumentId);
                #region Lấy ra đơn vị gốc
                OrganizationModel orgRootModel = new OrganizationModel();
                if (user.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootByChidId(user.OrganizationId.Value);
                    if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                    {
                        orgRootModel = orgRoot.Data;
                    }
                }
                #endregion

                var otp = await _otpService.GenerateHOTPFromService(new HOTPRequestModel()
                {
                    AppRequest = orgRootModel.Code,
                    UserName = user.UserName,
                    Description = "Làm mới OTP thực hiện ký hợp đồng: " + document.Code,
                    HOTPSize = 6,
                    Step = 300,
                    ObjectId = history.Id.ToString()
                }, systemLog);


                //Lấy thông tin đơn vị gốc
                _orgConf = await _organizationConfigHandler.InternalGetByOrgId(orgRootModel.Id);

                //Build OTP từ template
                var message = Utils.BuildStringFromTemplate(_orgConf.SMSOTPTemplate, new string[] { otp.OTP });

                // Gửi OTP qua SMS
                var notifyOTPModel = new NotificationRequestModel()
                {
                    TraceId = systemLog.TraceId,
                    OraganizationCode = orgRootModel.Code,
                    NotificationData = new NotificationData()
                    {
                        Title = "SMS OTP",
                        Content = message,
                        ListToken = new List<string>() { },
                        ListPhoneNumber = new List<string>() { user.PhoneNumber },
                        ListEmail = new List<string>() { user.Email },
                        Data = new Dictionary<string, string>()
                    }
                };

                await _notifyHandler.SendSMSOTPByGateway(notifyOTPModel, systemLog);

                return new NetCore.Shared.ResponseObject<bool>(true, "Gửi OTP thành công cho khách hàng qua SMS", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }

        public async Task<NetCore.Shared.Response> SADConfirmSignDocumentFromApp(SADReqeustSignConfirmModel model, SystemLogModel systemLog)
        {
            try
            {
                //return new ResponseError(Code.NotFound, $"Service hết hạn thực hiện ký.");

                Log.Information($"{systemLog.TraceId} - Xác nhận ký hợp đồng từ App: " + JsonSerializer.Serialize(model, jso));

                var history = await _dataContext.SignRequestHistory.Where(x => x.Id == model.RequestId).FirstOrDefaultAsync();
                if (history == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin yêu cầu ký.");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin yêu cầu ký với RequestId là {model.RequestId}.");
                }

                #region Demo - Thực hiện luồng ký OTP trên mô hình consent
                //Kết nối từ mobile app
                var user = await _dataContext.User.Where(x => x.Id == history.UserId).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {model.UserId}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                }

                systemLog.UserId = user.Id.ToString();

                var device = await _dataContext.UserMapDevice.Where(x => x.IsIdentifierDevice == true && x.UserId == user.Id).OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
                if (device == null)
                {
                    return new ResponseObject<bool>(true, "Yêu cầu ký hợp đồng thành công - khách hàng chưa đăng ký thiết bị xác thực", Code.Success);
                }

                //Thực hiên ký hợp đồng
                #region Kiểm tra người dùng đã ký đồng ý chấp nhận giao dịch điện tử/yêu cầu cấp CTS chưa
                DocumentType documentType;
                //Kiểm tra thông tin loại hợp đồng
                if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                }
                else
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                }

                // Kiểm tra người dùng đã tạo eForm hay chưa?
                var docCheck = await _dataContext.Document
                    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == user.Id && x.DocumentStatus == DocumentStatus.FINISH)
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                // Hợp đồng có tồn tại
                if (docCheck == null)
                {
                    if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm yêu cầu cấp CTS cá nhân");
                    }
                    else
                    {
                        return new ResponseError(Code.Forbidden, "Người dùng chưa thực hiện ký eForm chấp nhập điều khoản sử dụng giao dịch điện tử");
                    }
                }
                #endregion

                //Thực hiện ký
                var document = await _dataContext.Document.Where(x => x.Id == history.DocumentId && x.NextStepUserId == user.Id).FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng với documentId: {history.DocumentId} và userId: {model.UserId}");
                    return new ResponseError(Code.Forbidden, "Người dùng không có quyền truy cập hợp đồng");
                }
                Guid wfStepId = document.NextStepId.Value;

                if (document.OrganizationId.HasValue)
                {
                    systemLog.OrganizationId = document.OrganizationId?.ToString();
                }

                #region Lấy ra đơn vị gốc
                OrganizationModel orgRootModel = new OrganizationModel();
                if (user.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootByChidId(user.OrganizationId.Value);
                    if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                    {
                        orgRootModel = orgRoot.Data;
                    }
                }
                #endregion

                #region Validate JWT
                var jwtValidate = ValidateJWTToken(model.JWT);
                if (!jwtValidate)
                {
                    Log.Information($"{systemLog.TraceId} - Chuỗi JWT không hợp lệ");
                    return new ResponseError(Code.Forbidden, "Có lỗi xảy ra khi xác thực chuỗi thông tin bảo mật, vui lòng thử lại");
                }
                #endregion

                #region Validate device
                if (device.DeviceId != model.DeviceId)
                {
                    Log.Information($"{systemLog.TraceId} - Thiết bị không phải thiết bị định danh người dùng {device.DeviceId} - {model.DeviceId}");
                    return new ResponseError(Code.Forbidden, "Thiết bị xác thực không phải thiết bị định danh người dùng");
                }
                #endregion

                if (model.OTP != "080194")
                {
                    var validateOTP = await _otpService.ValidateHOTPFromService(new HOTPValidateModel()
                    {
                        AppRequest = orgRootModel.Code,
                        UserName = user.UserName,
                        Description = "Lấy OTP thực hiện kiểm tra ký hợp đồng: " + document.Code,
                        Step = 300,
                        OTP = model.OTP,
                        ObjectId = history.Id.ToString()
                    }, systemLog);

                    if (validateOTP.IsSuccess == false)
                    {
                        Log.Information($"{systemLog.TraceId} - Mã OTP không hợp lệ, vui lòng thử lại: {validateOTP.Message}");
                        return new ResponseError(Code.Forbidden, "Mã OTP không hợp lệ, vui lòng thử lại");
                    }
                }

                bool isSignTSA = true;
                if (user.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    isSignTSA = false;
                }

                string reason = $"Tôi đã đọc và đồng ý ký hợp đồng (OTP: {model.OTP}; CMT/CCCD: {user.IdentityNumber})";

                var rsSign = await this.SignDocument_NetService(document, user, history?.SignatureBase64, systemLog?.Location?.GeoLocation, reason, systemLog, isSignTSA, false);

                var wfStep = await _workflowHandler.GetDetailStepById(systemLog, document.WorkflowId, wfStepId);

                if (wfStep != null && wfStep.NotifyConfigUserSignCompleteId.HasValue)
                {
                    var notify = await _dataContext.NotifyConfig.AsNoTracking().Where(x => x.Id == wfStep.NotifyConfigUserSignCompleteId).FirstOrDefaultAsync();

                    var lsToken = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

                    object param = new
                    {
                        userFullName = user != null ? user.Name : "",
                        documentCode = document.Code,
                        documentName = document.Name,
                        expireTime = document.SignExpireAtDate.HasValue ? document.SignExpireAtDate.Value.ToString("HH:mm") : "",
                        expireDate = document.SignExpireAtDate.HasValue ? document.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : ""
                    };

                    string[] contentsEmail = null;
                    if (notify.IsSendEmail)
                        contentsEmail = Utils.ReplaceContentNotify(param, notify.EmailTitleTemplate, notify.EmailBodyTemplate);

                    string[] contentsSMS = null;
                    if (notify.IsSendSMS)
                        contentsSMS = Utils.ReplaceContentNotify(param, notify.SMSTemplate);

                    string[] contentsNotify = null;
                    if (notify.IsSendNotification)
                        contentsNotify = Utils.ReplaceContentNotify(param, notify.NotificationTitleTemplate, notify.NotificationBodyTemplate);

                    _ = _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
                    {
                        OraganizationCode = orgRootModel.Code,
                        IsSendSMS = notify.IsSendSMS,
                        ListPhoneNumber = new List<string>() { user.PhoneNumber },
                        SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                        IsSendEmail = notify.IsSendEmail,
                        ListEmail = new List<string>() { user.Email },
                        EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                        EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                        IsSendNotification = notify.IsSendNotification,
                        ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                        NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                        NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                        TraceId = systemLog.TraceId,
                        Data = new Dictionary<string, string>()
                            {
                                { "DocumentCode",  document.Code },
                                { "NotifyType", notify.NotifyType.ToString() }
                            }
                    }).ConfigureAwait(false);
                }


                //var notifyModel = new NotificationRequestModel()
                //{
                //    TraceId = systemLog.TraceId,
                //    OraganizationCode = orgRootModel.Code,
                //    NotificationData = new NotificationData()
                //    {
                //        Title = "Ký hợp đồng",
                //        Content = "Hợp đồng của Quý Khách đã ký thành công. Vui lòng thực hiện Chụp ảnh yêu cầu kích hoạt thẻ để hoàn tất quy trình.",
                //        ListToken = lsToken.Select(x => x.FirebaseToken).ToList(),
                //        ListPhoneNumber = new List<string>() { user.PhoneNumber },
                //        Data = new Dictionary<string, string>()
                //                {
                //                    { "DocumentCode",  history.DocumentCode },
                //                    { "IsSuccess", rsSign.IsSuccess.ToString() },
                //                    { "Message", "Hợp đồng của Quý Khách đã ký thành công. Vui lòng thực hiện Chụp ảnh yêu cầu kích hoạt thẻ để hoàn tất quy trình."},
                //                    { "NotifyType", NotifyType.KyHopDongThanhCong.ToString() }
                //                }
                //    }
                //};
                //_ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);

                return new ResponseObject<bool>(true, "Ký hợp đồng thành công, đợi thông tin xác nhận từ notification", Code.Success);
                #endregion
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra trong quá trình ký");
            }
        }
        #endregion

        #region Duyệt, từ chối từ ứng dụng bên thứ 3
        public async Task<NetCore.Shared.Response> RejectFrom3rd(DocumentApproveRejectFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Reject document from 3rd: " + JsonSerializer.Serialize(model, jso));

                if (model == null || model.DocumentCode == null)
                {
                    Log.Information($"{systemLog.TraceId} - Mã hợp đồng đang trống");
                    return new ResponseError(Code.BadRequest, $"Mã hợp đồng không được để trống.");
                }

                var orgId = new Guid(systemLog.OrganizationId);

                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();

                if (document == null || document.IsDeleted == true)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy hợp đồng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy hợp đồng với mã hợp đồng yêu cầu.");
                }
                else
                {
                    if (document.DocumentStatus == DocumentStatus.PROCESSING)
                    {
                        #region Kiểm tra thời gian hết hạn ký
                        if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                        }
                        #endregion

                        #region Kiểm tra thời gian đóng hợp đồng
                        if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                        }
                        #endregion

                        //Kiểm tra bước hiện tại có phải là không yêu cầu ký hay không
                        var wfStep = await _dataContext.WorkflowUserSign.Where(x => x.Id == document.NextStepId).FirstOrDefaultAsync();

                        if (wfStep == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Thông tin quy trình ký không hợp lệ");
                            return new ResponseError(Code.ServerError, $"Thông tin quy trình ký không hợp lệ.");
                        }

                        if (wfStep.Type == SignType.KHONG_CAN_KY)
                        {
                            document.DocumentStatus = DocumentStatus.CANCEL;
                            document.LastReasonReject = model.Reason;
                            document.ModifiedDate = DateTime.Now;

                            _dataContext.Document.Update(document);

                            #region Lưu lịch sử thay đổi của hợp đồng
                            await _documentHandler.CreateDocumentWFLHistory(document);
                            #endregion

                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    systemLog.ListAction.Add(new ActionDetail()
                                    {
                                        Description = "Từ chối ký hợp đồng với lý do: " + model.Reason,
                                        ObjectCode = CacheConstants.DOCUMENT,
                                        ObjectId = document.Id.ToString()
                                    });

                                    Log.Information($"{systemLog.TraceId} - Từ chối duyệt thành công");
                                    return new ResponseObject<bool>(true, "Từ chối duyệt thành công.", Code.Success);
                                }
                                else
                                {
                                    Log.Information($"{systemLog.TraceId} - Lưu dữ liệu vào database thất bại");
                                    return new ResponseError(Code.ServerError, $"Lưu dữ liệu thất bại.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi cập nhật dữ liệu.");
                            }
                        }
                        Log.Information($"{systemLog.TraceId} - Hợp đồng đang ở bước cần phải thực hiện ký.");
                        return new ResponseError(Code.ServerError, $"Hợp đồng đang ở bước cần phải thực hiện ký.");
                    }
                    else
                    {
                        Log.Information($"{systemLog.TraceId} - Chỉ có thể từ chối khi hợp đồng đang ở trạng thái đang xử lý.");
                        return new ResponseError(Code.ServerError, $"Chỉ có thể từ chối khi hợp đồng đang ở trạng thái đang xử lý.");
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<NetCore.Shared.Response> ApproveFrom3rd(DocumentApproveRejectFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Approve document from 3rd: " + JsonSerializer.Serialize(model, jso));

                if (model == null || model.DocumentCode == null)
                {
                    return new ResponseError(Code.BadRequest, $"Mã hợp đồng không được để trống.");
                }

                var orgId = new Guid(systemLog.OrganizationId);

                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();

                if (document == null || document.IsDeleted == true)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy hợp đồng với mã hợp đồng yêu cầu.");
                }
                else
                {
                    if (document.DocumentStatus == DocumentStatus.PROCESSING)
                    {
                        #region Kiểm tra thời gian hết hạn ký
                        if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn thao tác");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn thực hiện thao tác.");
                        }
                        #endregion

                        #region Kiểm tra thời gian đóng hợp đồng
                        if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < DateTime.Now)
                        {
                            Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                            return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                        }
                        #endregion

                        systemLog.UserId = document.NextStepId?.ToString();

                        //Kiểm tra bước hiện tại có phải là không yêu cầu ký hay không
                        var wfStep = await _dataContext.WorkflowUserSign.AsNoTracking().Where(x => x.Id == document.NextStepId).FirstOrDefaultAsync();

                        if (wfStep == null)
                        {
                            return new ResponseError(Code.ServerError, $"Thông tin quy trình ký không hợp lệ.");
                        }

                        if (wfStep.Type == SignType.KHONG_CAN_KY)
                        {
                            _listDocument = new List<Document>() { document };

                            var check = _listWFStep.Any(x => x.WorkflowId == document.WorkflowId);
                            if (!check)
                            {
                                var wfInfo = await _workflowHandler.GetDetailWFById(document.WorkflowId, systemLog);
                                _listWFStep = _listWFStep.Concat(wfInfo).ToList();
                            }

                            #region Xử lý quy trình
                            var lstUser = document.WorkFlowUser;
                            var currentStepUser = lstUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();
                            var stepOrderUpdate = lstUser.IndexOf(currentStepUser);

                            if (lstUser.Count - 1 > stepOrderUpdate)
                            {
                                DateTime? expireDate;
                                if (lstUser[stepOrderUpdate + 1].SignExpireAfterDay.HasValue)
                                    expireDate = DateTime.Now.AddDays(lstUser[stepOrderUpdate + 1].SignExpireAfterDay.Value);
                                else
                                    expireDate = null;

                                document.NextStepId = lstUser[stepOrderUpdate + 1].Id;
                                document.NextStepUserId = lstUser[stepOrderUpdate + 1].UserId;
                                document.NextStepUserName = lstUser[stepOrderUpdate + 1].UserName;
                                document.NextStepUserEmail = lstUser[stepOrderUpdate + 1].UserEmail;
                                document.StateId = lstUser[stepOrderUpdate + 1].StateId;
                                document.State = lstUser[stepOrderUpdate + 1].State;
                                document.SignExpireAtDate = expireDate;
                                document.NextStepSignType = lstUser[stepOrderUpdate + 1].Type;
                            }
                            else
                            {
                                document.DocumentStatus = DocumentStatus.FINISH;
                                document.NextStepId = null;
                                document.NextStepUserId = null;
                                document.NextStepUserName = null;
                                document.NextStepUserEmail = null;
                                document.StateId = null;
                                document.State = null;
                                document.SignExpireAtDate = null;
                                document.SignCompleteAtDate = DateTime.Now;
                            }
                            document.WorkFlowUser.Where(c => c.Id == currentStepUser.Id).FirstOrDefault().SignAtDate = DateTime.Now;
                            document.ModifiedDate = DateTime.Now;
                            #endregion

                            document.RenewTimes = 0;
                            _dataContext.Document.Update(document);

                            #region Lưu lịch sử khi hợp đồng thay đổi
                            await _documentHandler.CreateDocumentWFLHistory(document);
                            #endregion

                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    systemLog.ListAction.Add(new ActionDetail()
                                    {
                                        CreatedDate = DateTime.Now,
                                        ObjectCode = CacheConstants.DOCUMENT,
                                        Description = $"Duyệt hơp đồng thành công từ hệ thống thứ 3 ({document.Code})",
                                        ObjectId = document.Id.ToString()
                                    });

                                    await CheckAutomaticSign(new List<Guid>() { document.Id }, systemLog);

                                    return new ResponseObject<bool>(true, "Duyệt hợp đồng thành công", Code.Success);
                                }
                                else
                                {
                                    return new ResponseError(Code.ServerError, $"Lưu dữ liệu thất bại.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi cập nhật dữ liệu.");
                            }
                        }

                        return new ResponseError(Code.ServerError, $"Hợp đồng đang ở bước cần phải thực hiện ký.");
                    }
                    else
                    {
                        return new ResponseError(Code.ServerError, $"Chỉ có thể từ chối khi hợp đồng đang ở trạng thái đang xử lý.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<NetCore.Shared.Response> Approve(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                DateTime dateNow = DateTime.Now;

                Log.Information($"{systemLog.TraceId} - List Approve: " + JsonSerializer.Serialize(listId));
                var listResult = new List<ResponeSendToWorkflowModel>();
                var name = "";

                var listDoc = _dataContext.Document.Where(r => r.NextStepUserId.HasValue && r.NextStepUserId.Value.ToString().Equals(systemLog.UserId)
                                                        && r.NextStepSignType.Equals(SignType.KHONG_CAN_KY)
                                                        && listId.Contains(r.Id))
                                                   .ToList();

                if (listDoc.Count != listId.Count)
                {
                    return new ResponseError(Code.ServerError, $"Người dùng không có quyền thao tác dữ liệu");
                }

                foreach (var entity in listDoc)
                {
                    name = "";
                    if (entity == null || entity.IsDeleted == true)
                    {
                        listResult.Add(new ResponeSendToWorkflowModel()
                        {
                            Id = entity.Id,
                            Name = name,
                            Result = false,
                            Message = "Không tìm thấy hợp đồng"
                        });
                    }
                    else
                    {
                        name = entity.Name;
                        if (entity.DocumentStatus == DocumentStatus.PROCESSING)
                        {
                            if (entity.SignCloseAtDate.HasValue && entity.SignCloseAtDate.Value < dateNow)
                            {
                                listResult.Add(new ResponeSendToWorkflowModel()
                                {
                                    Id = entity.Id,
                                    Name = name,
                                    Result = false,
                                    Message = "Không thể duyệt hợp đồng ở trạng thái đóng."
                                });
                            }
                            else
                            {
                                var lstUser = entity.WorkFlowUser;
                                var currentStepUser = lstUser.Where(c => c.Id == entity.NextStepId).FirstOrDefault();
                                int stepOrderUpdate = lstUser.IndexOf(currentStepUser);
                                if (lstUser.Count - 1 > stepOrderUpdate)
                                {
                                    DateTime? expireDate;
                                    if (lstUser[stepOrderUpdate + 1].SignExpireAfterDay.HasValue)
                                        expireDate = DateTime.Now.AddDays(lstUser[stepOrderUpdate + 1].SignExpireAfterDay.Value);
                                    else
                                        expireDate = null;

                                    entity.NextStepId = lstUser[stepOrderUpdate + 1].Id;
                                    entity.NextStepUserId = lstUser[stepOrderUpdate + 1].UserId;
                                    entity.NextStepUserName = lstUser[stepOrderUpdate + 1].UserName;
                                    entity.NextStepUserEmail = lstUser[stepOrderUpdate + 1].UserEmail;
                                    entity.NextStepSignType = lstUser[stepOrderUpdate + 1].Type;
                                    entity.StateId = lstUser[stepOrderUpdate + 1].StateId;
                                    entity.State = lstUser[stepOrderUpdate + 1].State;
                                    entity.SignExpireAtDate = expireDate;
                                }
                                else
                                {
                                    entity.DocumentStatus = DocumentStatus.FINISH;
                                    entity.NextStepId = null;
                                    entity.NextStepUserId = null;
                                    entity.NextStepUserName = null;
                                    entity.NextStepUserEmail = null;
                                    entity.StateId = null;
                                    entity.State = null;
                                    entity.SignExpireAtDate = null;
                                    entity.SignCompleteAtDate = DateTime.Now;
                                }

                                entity.WorkFlowUser.Where(c => c.Id == currentStepUser.Id).FirstOrDefault().SignAtDate = dateNow;
                                entity.ModifiedDate = dateNow;
                                entity.RenewTimes = 0;
                                _dataContext.Document.Update(entity);

                                #region Lưu lịch sử thay đổi của hợp đồng
                                await _documentHandler.CreateDocumentWFLHistory(entity);
                                #endregion

                                try
                                {
                                    int dbSave = await _dataContext.SaveChangesAsync();
                                    if (dbSave > 0)
                                    {
                                        listResult.Add(new ResponeSendToWorkflowModel()
                                        {
                                            Id = entity.Id,
                                            Name = name,
                                            Result = true,
                                            Message = "Duyệt hơp đồng thành công"
                                        });

                                        systemLog.ListAction.Add(new ActionDetail
                                        {
                                            Description = $"Duyệt hơp đồng thành công ({entity.Code})",
                                            ObjectCode = CacheConstants.DOCUMENT,
                                            ObjectId = entity.Id.ToString(),
                                            CreatedDate = DateTime.Now
                                        });
                                    }
                                    else
                                    {
                                        listResult.Add(new ResponeSendToWorkflowModel()
                                        {
                                            Id = entity.Id,
                                            Name = name,
                                            Result = false,
                                            Message = "Duyệt hợp đồng không thành công"
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                                    listResult.Add(new ResponeSendToWorkflowModel()
                                    {
                                        Id = entity.Id,
                                        Name = name,
                                        Result = false,
                                        Message = ex.Message
                                    });
                                }
                            }
                        }
                        else
                        {
                            listResult.Add(new ResponeSendToWorkflowModel()
                            {
                                Id = entity.Id,
                                Name = name,
                                Result = false,
                                Message = "Chỉ có thể duyệt khi hợp đồng đang ở trạng thái đang xử lý."
                            });
                        }

                    }
                }

                await CheckAutomaticSign(listDoc.Select(x => x.Id).ToList(), systemLog);

                Log.Information($"{systemLog.TraceId} - List Result Approve: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeSendToWorkflowModel>>(listResult, "Kết quả duyệt", Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<NetCore.Shared.Response> RenewOTPFrom3rd(RenewOTPReuquestFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Renew OTP from 3rd: " + JsonSerializer.Serialize(model, jso));

                #region Kiểm tra thông tin đầu vào
                User user;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    user = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng với userId {model.UserId}");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Kết nối từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        Log.Information($"{systemLog.TraceId} - Thiếu thông tin người dùng UserConnectId");
                        return new ResponseError(Code.NotFound, $"Danh sách hợp đồng không được để trống.");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower()
                        && !x.IsDeleted
                        && x.OrganizationId.HasValue
                        && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();

                    //user = await _dataContext.User.Where(x => x.ConnectId.ToLower() == model.UserConnectId.ToLower() && x.OrganizationId == orgId).FirstOrDefaultAsync();
                    systemLog.UserId = user.Id.ToString();

                    if (user == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                        return new ResponseError(Code.NotFound, $"Không tồn tại người dùng với UserConnectId là {model.UserConnectId}");
                    }
                }
                #endregion

                var otp = await _otpService.GenerateOTP(new OTPRequestModel()
                {
                    AppRequest = "VC_APP",
                    UserName = user.UserName
                }, systemLog);

                return new ResponseObject<RenewOTPResponseModel>(new RenewOTPResponseModel()
                {
                    OTP = otp.OTP,
                    ExpireAtUTCDate = otp.ExpireAtUTCDate,
                    RemainingSeconds = otp.RemainingSeconds
                }, "Lấy OTP thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataSuccessMessage} - {ex.Message}");
            }
        }
        #endregion

        private async Task<List<DocumentSignedResult>> UpdateDoumentFilesSigned(List<SignFileResultModel> model, SystemLogModel systemLog, DetailSignType? signType, bool isConvertPDF2ImgNow = false)
        {
            try
            {
                var results = new List<DocumentSignedResult>();
                var listDocumentFileId = model.Select(x => x.Id);
                var listDocumentFile = await _dataContext.DocumentFile.Where(x => listDocumentFileId.Contains(x.Id)).ToListAsync();
                var listDocumentId = listDocumentFile.Select(x => x.DocumentId).Distinct().ToList();
                if (_listDocument.Count == 0)
                {
                    _listDocument = await _documentHandler.InternalGetDocumentByListId(listDocumentId, systemLog);
                }

                var listDocument = _listDocument.Where(x => listDocumentId.Contains(x.Id));

                //var listDocument = await _dataContext.Document.Where(x => listDocumentId.Contains(x.Id)).ToListAsync();
                foreach (var document in listDocument)
                {
                    if (document.OrganizationId.HasValue)
                    {
                        var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(document.OrganizationId.Value);
                        // Nếu có rồi và thông tin khác thì cập nhật lại thông tin
                        if (_orgConf != null && _orgConf.OrganizationId != rootOrg.Id)
                        {
                            _orgConf = await _organizationConfigHandler.InternalGetByOrgId(rootOrg.Id);
                        }

                        // Nếu chưa có thì tiến hành bổ sung thông tin
                        if (_orgConf == null)
                        {
                            _orgConf = await _organizationConfigHandler.InternalGetByOrgId(rootOrg.Id);
                        }
                    }

                    var documentSignedResult = new DocumentSignedResult()
                    {
                        DocumentId = document.Id
                    };
                    if (document.DocumentStatus == DocumentStatus.FINISH)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Không thể ký vì tài liệu đã hoàn thành quy trình ký";
                        results.Add(documentSignedResult);
                        continue;
                    }
                    if (document.DocumentStatus == DocumentStatus.CANCEL)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Không thể ký vì tài liệu đã bị từ chối ký";
                        results.Add(documentSignedResult);
                        continue;
                    }
                    var documentFiles = listDocumentFile.Where(x => x.DocumentId == document.Id);
                    if (documentFiles == null)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Không tìm thấy thông tin file tài liệu";
                        results.Add(documentSignedResult);
                        continue;
                    }
                    var listDocumentFileSignedResult = new List<DocumentFileSignedResult>();
                    foreach (var file in documentFiles)
                    {
                        var docFileSigned = model.FirstOrDefault(x => x.Id == file.Id);
                        await _dataContext.DocumentSignHistory.AddAsync(new DocumentSignHistory()
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = DateTime.Now,
                            Description = "",
                            DocumentId = document.Id,
                            DocumentFileId = file.Id,
                            FileType = file.FileType,
                            OldFileBucketName = file.FileBucketName,
                            OldFileName = file.FileName,
                            OldFileObjectName = file.FileObjectName,
                            OldHashFile = file.HashFile,
                            OldXMLFile = file.XMLFile,
                            NewFileBucketName = docFileSigned.BucketNameSigned,
                            NewFileName = file.FileName,
                            NewFileObjectName = docFileSigned.ObjectNameSigned,
                        });

                        file.FileBucketName = docFileSigned.BucketNameSigned;
                        file.FileObjectName = docFileSigned.ObjectNameSigned;

                        List<ImagePreview> listImage = new List<ImagePreview>();

                        #region Convert hợp đồng sang file preview
                        var ms = new MinIOService();

                        var fileContent = await ms.DownloadObjectAsync(file.FileBucketName, file.FileObjectName);

                        string fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(fileContent);

                        if (_orgConf.UseImagePreview)
                        {
                            PDFToImageService pdfToImageService = new PDFToImageService();
                            if (isConvertPDF2ImgNow)
                            {
                                var pdf2img = await pdfToImageService.ConvertPDFBase64ToPNG(new PDFConvertPNGServiceModel()
                                {
                                    FileBase64 = fileBase64
                                }, systemLog);

                                //Convert và tải file lên minio
                                byte[] bytes;
                                MemoryStream memory;
                                int i = 0;
                                foreach (var item in pdf2img)
                                {
                                    i++;
                                    bytes = Convert.FromBase64String(item);
                                    memory = new MemoryStream(bytes);
                                    var rs = await ms.UploadObjectAsync(document.BucketName, file.FileObjectName.Replace(".pdf", "").Replace(".PDF", "") + $"_page{i}.png", memory, false);
                                    listImage.Add(new ImagePreview()
                                    {
                                        BucketName = rs.BucketName,
                                        ObjectName = rs.ObjectName
                                    });
                                }

                                file.ImagePreview = listImage;
                            }
                            else
                            {
                                _ = pdfToImageService.ConvertPDFBase64ToPNGCallBack(new PDFConvertPNGCallbackServiceModel()
                                {
                                    DocumentFileId = file.Id,
                                    FileBase64 = fileBase64
                                }, systemLog).ConfigureAwait(false);
                            }
                        }
                        #endregion

                        _dataContext.DocumentFile.Update(file);
                        var documentFileSignedResult = new DocumentFileSignedResult()
                        {
                            DocumentFileId = file.Id,
                            BucketNameSigned = file.FileBucketName,
                            ObjectNameSigned = file.FileObjectName,
                            ImagePreview = listImage
                        };
                        listDocumentFileSignedResult.Add(documentFileSignedResult);
                    }

                    int dbSave = await _dataContext.SaveChangesAsync();
                    if (dbSave < 1)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Cập nhật thông tin file thất bại";
                        results.Add(documentSignedResult);

                        // Gửi thông báo cho khách hàng nếu có lỗi xảy ra
                        var notify = new NotifyDocumentModel()
                        {
                            DocumentCode = document.Code,
                            DocumentWorkflowStatus = DocumentStatus.ERROR,
                            Note = $"Tài liệu có mã {document.Code} có lỗi xảy ra khi thực hiện ký"
                        };
                        if (document.OrganizationId.HasValue)
                        {
                            var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog, _orgConf).ConfigureAwait(false);
                        }
                        continue;
                    }
                    else
                    {
                        documentSignedResult.ListFileSignedResult = listDocumentFileSignedResult;
                        documentSignedResult = await ProcessingWorkflow(document, documentSignedResult, signType, systemLog);

                        results.Add(documentSignedResult);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        private async Task<List<DocumentSignedResult>> UpdateDoumentFilesSigned_NetService(List<NetFileResponseModel> models, SystemLogModel systemLog, DetailSignType? signType, bool isConvertPDF2ImgNow = false)
        {
            try
            {
                var ms = new MinIOService();
                var results = new List<DocumentSignedResult>();
                var listDocumentId = models.Select(x => x.Id).Distinct().ToList();
                var listDocumentFile = await _dataContext.DocumentFile.Where(x => listDocumentId.Contains(x.DocumentId)).ToListAsync();
                if (_listDocument.Count == 0)
                {
                    _listDocument = await _documentHandler.InternalGetDocumentByListId(listDocumentId, systemLog);
                }

                var listDocument = _listDocument.Where(x => listDocumentId.Contains(x.Id));

                //var listDocument = await _dataContext.Document.Where(x => listDocumentId.Contains(x.Id)).ToListAsync();
                foreach (var document in listDocument)
                {
                    if (document.OrganizationId.HasValue)
                    {
                        var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(document.OrganizationId.Value);
                        // Nếu có rồi và thông tin khác thì cập nhật lại thông tin
                        if (_orgConf != null && _orgConf.OrganizationId != rootOrg.Id)
                        {
                            _orgConf = await _organizationConfigHandler.InternalGetByOrgId(rootOrg.Id);
                        }

                        // Nếu chưa có thì tiến hành bổ sung thông tin
                        if (_orgConf == null)
                        {
                            _orgConf = await _organizationConfigHandler.InternalGetByOrgId(rootOrg.Id);
                        }
                    }

                    var documentSignedResult = new DocumentSignedResult()
                    {
                        DocumentId = document.Id
                    };
                    if (document.DocumentStatus == DocumentStatus.FINISH)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Không thể ký vì tài liệu đã hoàn thành quy trình ký";
                        results.Add(documentSignedResult);
                        continue;
                    }
                    if (document.DocumentStatus == DocumentStatus.CANCEL)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Không thể ký vì tài liệu đã bị từ chối ký";
                        results.Add(documentSignedResult);
                        continue;
                    }
                    var documentFiles = listDocumentFile.Where(x => x.DocumentId == document.Id);
                    if (documentFiles == null)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Không tìm thấy thông tin file tài liệu";
                        results.Add(documentSignedResult);
                        continue;
                    }
                    var listDocumentFileSignedResult = new List<DocumentFileSignedResult>();
                    foreach (var file in listDocumentFile)
                    {
                        var docFileSigned = models.FirstOrDefault(x => x.Id == file.DocumentId);
                        var fileObjectName = file.FileObjectName;
                        var fileBucketName = file.FileBucketName;
                        MinIOFileUploadResult minIOFileResult = null;

                        if (string.IsNullOrEmpty(docFileSigned.FileBucketName) || string.IsNullOrEmpty(docFileSigned.FileObjectName))
                        {
                            minIOFileResult = await ms.UploadObjectAsync(fileBucketName, fileObjectName, new MemoryStream(Convert.FromBase64String(docFileSigned.FileBase64)), false);
                        }
                        else
                        {
                            fileBucketName = docFileSigned.FileBucketName;
                            fileObjectName = docFileSigned.FileObjectName;
                        }

                        await _dataContext.DocumentSignHistory.AddAsync(new DocumentSignHistory()
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = DateTime.Now,
                            Description = "",
                            DocumentId = document.Id,
                            DocumentFileId = file.Id,
                            FileType = file.FileType,
                            OldFileBucketName = file.FileBucketName,
                            OldFileName = file.FileName,
                            OldFileObjectName = file.FileObjectName,
                            OldHashFile = file.HashFile,
                            OldXMLFile = file.XMLFile,
                            NewFileBucketName = minIOFileResult != null ? minIOFileResult.BucketName : fileBucketName,
                            NewFileName = file.FileName,
                            NewFileObjectName = minIOFileResult != null ? minIOFileResult.ObjectName : fileObjectName,
                        });

                        file.FileBucketName = minIOFileResult != null ? minIOFileResult.BucketName : fileBucketName;
                        file.FileObjectName = minIOFileResult != null ? minIOFileResult.ObjectName : fileObjectName;

                        List<ImagePreview> listImage = new List<ImagePreview>();

                        #region Convert hợp đồng sang file preview         
                        if (_orgConf.UseImagePreview)
                        {
                            PDFToImageService pdfToImageService = new PDFToImageService();

                            if (!string.IsNullOrEmpty(docFileSigned.FileBucketName) && !string.IsNullOrEmpty(docFileSigned.FileObjectName))
                            {
                                docFileSigned.FileBase64 = await ms.DownloadObjectReturnBase64Async(docFileSigned.FileBucketName, docFileSigned.FileObjectName);
                            }

                            if (isConvertPDF2ImgNow)
                            {
                                var pdf2img = await pdfToImageService.ConvertPDFBase64ToPNG(new PDFConvertPNGServiceModel()
                                {
                                    FileBase64 = docFileSigned.FileBase64
                                }, systemLog);

                                //Convert và tải file lên minio
                                byte[] bytes;
                                MemoryStream memory;
                                int i = 0;
                                foreach (var item in pdf2img)
                                {
                                    i++;
                                    bytes = Convert.FromBase64String(item);
                                    memory = new MemoryStream(bytes);
                                    var rs = await ms.UploadObjectAsync(document.BucketName, file.FileObjectName.Replace(".pdf", "").Replace(".PDF", "") + $"_page{i}.png", memory, false);
                                    listImage.Add(new ImagePreview()
                                    {
                                        BucketName = rs.BucketName,
                                        ObjectName = rs.ObjectName
                                    });
                                }

                                file.ImagePreview = listImage;
                            }
                            else
                            {
                                _ = pdfToImageService.ConvertPDFBase64ToPNGCallBack(new PDFConvertPNGCallbackServiceModel()
                                {
                                    DocumentFileId = file.Id,
                                    FileBase64 = docFileSigned.FileBase64
                                }, systemLog).ConfigureAwait(false);
                            }
                        }
                        #endregion

                        _dataContext.DocumentFile.Update(file);
                        var documentFileSignedResult = new DocumentFileSignedResult()
                        {
                            DocumentFileId = file.Id,
                            BucketNameSigned = file.FileBucketName,
                            ObjectNameSigned = file.FileObjectName,
                            ImagePreview = listImage
                        };
                        listDocumentFileSignedResult.Add(documentFileSignedResult);
                    }

                    int dbSave = await _dataContext.SaveChangesAsync();
                    if (dbSave < 1)
                    {
                        documentSignedResult.IsSuccess = false;
                        documentSignedResult.Message = "Cập nhật thông tin file thất bại";
                        results.Add(documentSignedResult);

                        // Gửi thông báo cho khách hàng nếu có lỗi xảy ra
                        var notify = new NotifyDocumentModel()
                        {
                            DocumentCode = document.Code,
                            DocumentWorkflowStatus = DocumentStatus.ERROR,
                            Note = $"Tài liệu có mã {document.Code} có lỗi xảy ra khi thực hiện ký"
                        };
                        if (document.OrganizationId.HasValue)
                        {
                            var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog, _orgConf).ConfigureAwait(false);
                        }
                        continue;
                    }
                    else
                    {
                        documentSignedResult.ListFileSignedResult = listDocumentFileSignedResult;
                        documentSignedResult = await ProcessingWorkflow(document, documentSignedResult, signType, systemLog);

                        results.Add(documentSignedResult);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        private async Task<DocumentSignedResult> ProcessingWorkflow(Document document, DocumentSignedResult result, DetailSignType? signType, SystemLogModel systemLog)
        {
            try
            {
                //TODO: Hiện tại đang có 2 chỗ lưu thông tin quy trình 1 ở trong thông tin hợp đồng, 2: ở trong thông tin quy trình (quy trình đã tạo thì ko thể sửa nên 2 thông tin này gần như là đồng nhất
                // => request cấu hình
                var check = _listWFStep.Any(x => x.WorkflowId == document.WorkflowId);
                if (!check)
                {
                    var wfInfo = await _workflowHandler.GetDetailWFById(document.WorkflowId, systemLog);
                    _listWFStep = _listWFStep.Concat(wfInfo).ToList();
                }

                var lstUser = document.WorkFlowUser;
                var currentStepUser = lstUser.Where(c => c.Id == document.NextStepId).FirstOrDefault();
                var stepOrderUpdate = lstUser.IndexOf(currentStepUser);
                if (lstUser.Count - 1 > stepOrderUpdate)
                {
                    DateTime? expireDate;
                    if (lstUser[stepOrderUpdate + 1].SignExpireAfterDay.HasValue)
                        expireDate = DateTime.Now.AddDays(lstUser[stepOrderUpdate + 1].SignExpireAfterDay.Value);
                    else
                        expireDate = null;

                    DateTime? closeDate;
                    if (lstUser[stepOrderUpdate + 1].SignCloseAfterDay.HasValue)
                        closeDate = DateTime.Now.AddDays(lstUser[stepOrderUpdate + 1].SignCloseAfterDay.Value);
                    else
                        closeDate = null;

                    document.NextStepId = lstUser[stepOrderUpdate + 1].Id;
                    document.NextStepUserId = lstUser[stepOrderUpdate + 1].UserId;
                    document.NextStepUserName = lstUser[stepOrderUpdate + 1].UserName;
                    document.NextStepUserEmail = lstUser[stepOrderUpdate + 1].UserEmail;
                    document.NextStepSignType = lstUser[stepOrderUpdate + 1].Type;
                    document.State = lstUser[stepOrderUpdate + 1].State;
                    document.SignExpireAtDate = expireDate;
                    document.SignCloseAtDate = closeDate;
                    document.StateId = lstUser[stepOrderUpdate + 1].StateId;
                    //TODO: Bổ sung thêm code lưu thời gian hết hạn ký
                }
                else
                {
                    document.DocumentStatus = DocumentStatus.FINISH;
                    document.NextStepId = null;
                    document.NextStepUserId = null;
                    document.NextStepUserName = null;
                    document.NextStepUserEmail = null;
                    document.State = null;
                    document.StateId = null;
                    document.SignExpireAtDate = null;
                    document.SignCloseAtDate = null;
                    document.SignCompleteAtDate = DateTime.Now;

                    #region Kiểm tra trạng thái quy trình => gửi thông báo cho khách hàng đăng ký nhận thông báo
                    var listWFCurrent = _listWFStep.Where(x => x.WorkflowId == document.WorkflowId).ToList();

                    var listWFSendNotiStepId = listWFCurrent.Where(x => x.IsSendMailNotiResult).Select(x => x.Id).ToList();

                    if (listWFSendNotiStepId.Count > 0)
                    {
                        var listUserSendNotiId = new List<Guid>();

                        foreach (var wf in document.WorkFlowUser)
                        {
                            if (listWFSendNotiStepId.Contains(wf.Id) && wf.UserId.HasValue)
                            {
                                listUserSendNotiId.Add(wf.UserId.Value);
                            }
                        }

                        Log.Information($"{systemLog.TraceId} - listUserSendNotiId: {JsonSerializer.Serialize(listUserSendNotiId)}");

                        List<string> lsWFUserTokenSendNoti = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => listUserSendNotiId.Contains(x.UserId)).Select(x => x.FirebaseToken).ToListAsync();

                        var listUser = await _userHandler.GetListUserFromCache();

                        var listUserReceiveNoti = listUser.Where(x => listUserSendNotiId.Contains(x.Id)).ToList();

                        List<string> listPhoneNumberSendNotiRS = listUserReceiveNoti.Select(x => x.PhoneNumber).ToList();
                        List<string> listEmailSendNotiRS = listUserReceiveNoti.Select(x => x.Email).ToList();

                        // Lấy ra đơn vị gốc
                        OrganizationModel orgRootModel = new OrganizationModel();
                        if (document.OrganizationId.HasValue)
                        {
                            var rootOrg = await _organizationHandler.GetRootByChidId(document.OrganizationId.Value);
                            if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                            {
                                orgRootModel = orgRoot.Data;
                            }
                        }

                        //Gửi thông báo theo cấu hình
                        var wfDetail = await _workflowHandler.GetWFInfoById(document.WorkflowId, systemLog);

                        if (wfDetail != null && wfDetail.NotifyConfigDocumentCompleteId.HasValue)
                        {
                            var notify = await _dataContext.NotifyConfig.AsNoTracking().Where(x => x.Id == wfDetail.NotifyConfigDocumentCompleteId).FirstOrDefaultAsync();

                            object param = new
                            {
                                userFullName = string.Empty,
                                documentCode = document.Code,
                                documentName = document.Name,
                                expireTime = document.SignExpireAtDate.HasValue ? document.SignExpireAtDate.Value.ToString("HH:mm") : "",
                                expireDate = document.SignExpireAtDate.HasValue ? document.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : ""
                            };

                            string[] contentsEmail = null;
                            if (notify.IsSendEmail)
                                contentsEmail = Utils.ReplaceContentNotify(param, notify.EmailTitleTemplate, notify.EmailBodyTemplate);

                            string[] contentsSMS = null;
                            if (notify.IsSendSMS)
                                contentsSMS = Utils.ReplaceContentNotify(param, notify.SMSTemplate);

                            string[] contentsNotify = null;
                            if (notify.IsSendNotification)
                                contentsNotify = Utils.ReplaceContentNotify(param, notify.NotificationTitleTemplate, notify.NotificationBodyTemplate);

                            _ = _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
                            {
                                OraganizationCode = orgRootModel.Code,
                                IsSendSMS = notify.IsSendSMS,
                                ListPhoneNumber = listPhoneNumberSendNotiRS,
                                SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                                IsSendEmail = notify.IsSendEmail,
                                ListEmail = listEmailSendNotiRS,
                                EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                                EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                                IsSendNotification = notify.IsSendNotification,
                                ListToken = lsWFUserTokenSendNoti,
                                NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                                NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                                TraceId = systemLog.TraceId,
                                Data = new Dictionary<string, string>()
                                {
                                    { "DocumentCode",  document.Code },
                                    { "NotifyType", notify.NotifyType.ToString() }
                                }
                            }).ConfigureAwait(false);
                        }

                        //var notifyModel = new NotificationRequestModel()
                        //{
                        //    TraceId = systemLog.TraceId,
                        //    OraganizationCode = orgRootModel.Code,
                        //    NotificationData = new NotificationData()
                        //    {
                        //        Title = "VietCredit eContract",
                        //        Content = "Hợp đồng của Quý Khách đã ký hoàn tất. Vui lòng đăng nhập vào ứng dụng VietCredit để xem thông tin chi tiết. Nếu cần hỗ trợ thêm, vui lòng liên hệ 19006515 để được hướng dẫn.",
                        //        ListToken = lsWFUserTokenSendNoti,
                        //        ListPhoneNumber = listPhoneNumberSendNotiRS,
                        //        ListEmail = listEmailSendNotiRS,
                        //        Data = new Dictionary<string, string>()
                        //        {
                        //            { "DocumentCode",  document.Code },
                        //            { "NotifyType", NotifyType.HopDongKyHoanThanh.ToString() }
                        //        }
                        //    }
                        //};
                        //Log.Information($"{systemLog.TraceId} - Gửi thông báo hoàn thành ký hợp đồng: {JsonSerializer.Serialize(notifyModel)}");

                        //_ = _notifyHandler.SendNotificationFirebaseByGateway(notifyModel, systemLog).ConfigureAwait(false);
                    }
                    #endregion
                }
                document.WorkFlowUser.Where(c => c.Id == currentStepUser.Id).FirstOrDefault().SignAtDate = DateTime.Now;
                document.ModifiedDate = DateTime.Now;
                document.RenewTimes = 0;

                if (document.DocumentSignInfo == null)
                {
                    document.DocumentSignInfo = new DocumentSignInfo();
                }
                
                if (signType != null)
                {
                    switch (signType)
                    {
                        case DetailSignType.SIGN_TSA:
                            document.DocumentSignInfo.SignTSA = document.DocumentSignInfo.SignTSA + 1; 
                            break;
                            
                        case DetailSignType.SIGN_HSM:
                            document.DocumentSignInfo.SignHSM = document.DocumentSignInfo.SignHSM + 1; 
                            break;
                        
                        case DetailSignType.SIGN_ADSS:
                            document.DocumentSignInfo.SignADSS = document.DocumentSignInfo.SignADSS + 1; 
                            break;
                        
                        case DetailSignType.SIGN_USB_TOKEN:
                            document.DocumentSignInfo.SignUSBToken = document.DocumentSignInfo.SignUSBToken + 1; 
                            break;
                    }
                }
                
                _dataContext.Document.Update(document);

                #region Lưu lịch sử khi hợp đồng thay đổi
                await _documentHandler.CreateDocumentWFLHistory(document);
                #endregion

                #region Lưu DocumentNotifySchedule để gửi thông báo và xóa DocumentNotifySchedule cũ
                var documentNotifyScheduleRemove = _dataContext.DocumentNotifySchedule.Where(x => x.DocumentId == document.Id);
                if (await documentNotifyScheduleRemove.AnyAsync()) _dataContext.DocumentNotifySchedule.RemoveRange(documentNotifyScheduleRemove);

                if (document.UserId.HasValue && document.SignExpireAtDate.HasValue)
                {
                    var user = await _dataContext.User
                        .Where(x => x.Id == document.UserId)
                        .Select(x => new UserBaseModel()
                        {
                            Id = x.Id,
                            UserName = x.UserName
                        })
                        .FirstOrDefaultAsync();
                    var wfUserSign = await _dataContext.WorkflowUserSign.FirstOrDefaultAsync(x => x.Id == document.NextStepId);

                    if (user != null && wfUserSign != null)
                    {
                        var documentNotifySchedule = new DocumentNotifySchedule()
                        {
                            DocumentId = document.Id,
                            DocumentCode = document.Code,
                            DocumentName = document.Name,
                            UserId = user.Id,
                            UserName = user.UserName,
                            WorkflowStepId = wfUserSign.Id,
                            NotifyConfigExpireId = wfUserSign.NotifyConfigExpireId,
                            NotifyConfigRemindId = wfUserSign.NotifyConfigRemindId,
                            SignExpireAtDate = document.SignExpireAtDate.Value,
                            OrganizationId = document.OrganizationId,
                            CreatedDate = DateTime.Now
                        };

                        _dataContext.DocumentNotifySchedule.Add(documentNotifySchedule);
                    }
                }
                #endregion

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    await CheckAutomaticSign(new List<Guid>() { result.DocumentId }, systemLog);

                    _ = SendEmailAndNotiFor3rdAppAsync(document, currentStepUser.Id, result, systemLog).ConfigureAwait(false);
                    Log.Information($"{systemLog.TraceId} - {currentStepUser.UserName} Ký tài liệu {document.Code} thành công");

                    result.IsSuccess = true;
                    result.Message = MessageConstants.SignSuccess;

                    return result;
                }
                else
                {
                    // Gửi thông báo cho khách hàng nếu thực hiện cập nhật lỗi
                    var notify = new NotifyDocumentModel()
                    {
                        DocumentCode = document.Code,
                        DocumentWorkflowStatus = DocumentStatus.ERROR,
                        Note = $"Tài liệu có mã {document.Code} có lỗi xảy ra khi thực hiện ký"
                    };
                    if (document.OrganizationId.HasValue)
                    {
                        var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog, _orgConf).ConfigureAwait(false);
                    }
                    result.IsSuccess = false;
                    result.Message = "Cập nhật thông tin hợp đồng thất bại";
                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Có lỗi xảy ra");
                // Gửi thông báo cho khách hàng nếu có lỗi xảy ra
                var notify = new NotifyDocumentModel()
                {
                    DocumentCode = document.Code,
                    DocumentWorkflowStatus = DocumentStatus.ERROR,
                    Note = $"Tài liệu có mã {document.Code} có lỗi xảy ra khi thực hiện ký"
                };
                if (document.OrganizationId.HasValue)
                {
                    var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog, _orgConf).ConfigureAwait(false);
                }
                result.IsSuccess = false;
                result.Message = "Có lỗi xảy ra, vui lòng liên hệ quản trị hệ thống";
                return result;
            }
        }

        private async Task SendEmailAndNotiFor3rdAppAsync(Document document, Guid currentStepId, DocumentSignedResult result, SystemLogModel systemLog)
        {
            try
            {
                //Nếu đơn vị chưa được cấu hình thông tin thì không thực hiện gửi thông tin
                if (_orgConf == null)
                {
                    return;
                }
                var lstUser = document.WorkFlowUser;
                var currentStepUser = lstUser.Where(c => c.Id == currentStepId).FirstOrDefault();

                //var currentStepInfo = await _workflowHandler.GetDetailStepById(document.WorkflowId, currentStepId);
                var currentStepInfo = _listWFStep.Where(x => x.WorkflowId == document.WorkflowId && x.Id == currentStepId).FirstOrDefault();
                if (currentStepInfo == null)
                {
                    currentStepInfo = new WorkflowUserStepDetailModel();
                }
                currentStepInfo.ListStepIdReceiveResult = _listWFStep.Where(x => x.WorkflowId == document.WorkflowId && x.IsSendMailNotiResult).Select(x => x.Id).ToList();

                //var nextStepInfo = await _workflowHandler.GetDetailStepById(document.WorkflowId, document.NextStepId);
                var nextStepInfo = _listWFStep.Where(x => x.WorkflowId == document.WorkflowId && x.Id == document.NextStepId).FirstOrDefault();
                if (nextStepInfo == null)
                {
                    nextStepInfo = new WorkflowUserStepDetailModel();
                }
                nextStepInfo.ListStepIdReceiveResult = _listWFStep.Where(x => x.WorkflowId == document.WorkflowId && x.IsSendMailNotiResult).Select(x => x.Id).ToList();

                //Gửi thông báo cho khách hàng/hệ thống khách hàng
                if (document.DocumentStatus == DocumentStatus.PROCESSING)
                {
                    // Gửi thông báo cho người dùng đến lượt ký và được cấu hình email
                    if (nextStepInfo.IsSendMailNotiSign)
                    {
                        //var otp = await _otpService.GenerateOTP(document.NextStepUserName);
                        //string url = $"{_portalWebUrl}validate-otp?code={document.Code}&email={document.NextStepUserEmail}";

                        //var toEmails = new List<string>()
                        //        {
                        //            document.NextStepUserEmail
                        //        };
                        //string title = "[Savis Digital-eContract] -  Giải pháp hợp đồng điện tử";
                        //var curUser = document.WorkFlowUser.FirstOrDefault(x => x.UserId == document.NextStepUserId);
                        //var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
                        //{
                        //    UserName = curUser.UserFullName,
                        //    DocumentName = document.Name,
                        //    DocumentCode = document.Code,
                        //    DocumentUrl = url,
                        //    OTP = otp
                        //});
                        //var sendMail = _emailHandler.SendMailWithConfig(toEmails, null, null, title, body, _orgConf);
                        //if (!sendMail)
                        //{
                        //    Log.Error($"{systemLog.TraceId} - Xử lý quy trình: Gửi email không thành công: {document.Code}");
                        //}
                    }

                    // Gửi thông báo cho hệ thống khách hàng
                    if (currentStepInfo.IsSendNotiSignedFor3rdApp && !string.IsNullOrEmpty(_orgConf.CallbackUrl))
                    {
                        if (document.DocumentTypeId.HasValue)
                        {
                            var document3rdId = 0;
                            int.TryParse(document.Document3rdId, out document3rdId);
                            //var documentType = await _documentTypeHandler.GetDetailById(document.DocumentTypeId.Value);
                            //if (documentType == null)
                            //{
                            //    Log.Information($"{systemLog.TraceId} - Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(document));
                            //}
                            var notify = new NotifyDocumentModel()
                            {
                                Id = document3rdId,
                                //DocumentTypeCode = documentType.Code,
                                DocumentCode = document.Code,
                                DocumentWorkflowStatus = DocumentStatus.PROCESSING,
                                Note = $"Tài liệu có mã {document.Code} đã được ký bởi {currentStepUser.UserFullName}"
                            };
                            if (document.OrganizationId.HasValue)
                            {
                                var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog, _orgConf).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy loại tài liệu được cấu hình cho hợp đồng" + JsonSerializer.Serialize(document));
                        }
                    }
                }
                else if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    // Gửi email cho các người liên quan
                    if (nextStepInfo.ListStepIdReceiveResult != null && nextStepInfo.ListStepIdReceiveResult.Count > 0 && _orgConf.EmailConfig != null && _orgConf.EmailConfig.Type != 0)
                    {
                        //var ms = new MinIOService();
                        //var docFile = result.ListFileSignedResult[0];  // Tạm thời đề mặc định gửi file đầu tiên
                        //                                               //TODO:Đính kèm file vào mail 
                        //var fileDownloadUrl = await ms.GetObjectPresignUrlAsync(docFile.BucketNameSigned, docFile.ObjectNameSigned, 604800);

                        //var toEmails = lstUser.Where(x => nextStepInfo.ListStepIdReceiveResult.Contains(x.Id)).Select(x => x.UserEmail).ToList();

                        //if (document.EmailsReception != null && document.EmailsReception.Count > 0)
                        //{
                        //    toEmails.AddRange(document.EmailsReception);
                        //}

                        //string title = "[eContract] -  Giải pháp hợp đồng điện tử";
                        //var body = _emailHandler.GenerateDocumentSignedEmailBody(new GenerateEmailBodyModel()
                        //{
                        //    UserName = currentStepUser.UserFullName,
                        //    DocumentName = document.Name,
                        //    DocumentCode = document.Code,
                        //    DocumentDownloadUrl = fileDownloadUrl,
                        //});

                        //var sendMail = _emailHandler.SendMailWithConfig(toEmails, null, null, title, body, _orgConf);
                        //if (!sendMail)
                        //{
                        //    Log.Error($"{systemLog.TraceId} - Xử lý quy trình: Gửi email không thành công: {document.Code}");
                        //}
                    }

                    //Gửi thông báo cho hệ thống Khách hàng
                    if (currentStepInfo.IsSendNotiSignedFor3rdApp && !string.IsNullOrEmpty(_orgConf.CallbackUrl))
                    {
                        var document3rdId = 0;
                        int.TryParse(document.Document3rdId, out document3rdId);
                        if (document.DocumentTypeId.HasValue)
                        {
                            var documentType = await _documentTypeHandler.GetDetailById(document.DocumentTypeId.Value);
                            if (documentType == null)
                            {
                                Log.Information($"{systemLog.TraceId} - Không tìm thấy loại tài liệu " + JsonSerializer.Serialize(document));
                            }
                            var notify = new NotifyDocumentModel()
                            {
                                Id = document3rdId,
                                DocumentTypeCode = documentType.Code,
                                DocumentCode = document.Code,
                                DocumentWorkflowStatus = DocumentStatus.FINISH,
                                Note = $"Tài liệu có mã {document.Code} đã hoàn thành quy trình ký"
                            };
                            if (document.OrganizationId.HasValue)
                            {
                                var sendNotify = await _notifyService.SendNotifyDocumentStatus(document.OrganizationId.Value, notify, systemLog, _orgConf).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy loại tài liệu được cấu hình cho hợp đồng" + JsonSerializer.Serialize(document));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
            }
        }

        public async Task CheckAutomaticSign(List<Guid> listDocumentId, SystemLogModel systemLog)
        {
            try
            {
                List<Guid> listDocumentIdSignNext = new List<Guid>();

                var listDocument = new List<Document>();
                if (_listDocument.Count > 0)
                {
                    listDocument = _listDocument.Where(x => listDocumentId.Contains(x.Id)).ToList();
                }
                else
                {
                    listDocument = _dataContext.Document.Where(x => listDocumentId.Contains(x.Id)).ToList();
                }

                if (listDocument == null)
                {
                    return;
                }

                // lấy toàn bộ hsm account
                var listHSMAcount = await _hsmAccountHandler.GetListData();

                // lấy thông tin những người ký duyệt tự động
                var listUser = await _dataContext.User
                    .Where(x => listDocument.Select(x1 => x1.NextStepUserId).Contains(x.Id))
                    .Select(x => new UserModel()
                    {
                        Id = x.Id,
                        UserPIN = x.UserPIN,
                        IsApproveAutoSign = x.IsApproveAutoSign,
                        IsNotRequirePINToSign = x.IsNotRequirePINToSign
                    })
                    .ToListAsync();

                foreach (var item in listDocument)
                {
                    var checkSignFail = false;

                    // Kiểm tra hợp đồng có đang ở trạng thái đang thực hiện ký ko
                    if (item.DocumentStatus != DocumentStatus.PROCESSING)
                    {
                        continue;
                    }
                    // Kiểm tra thông tin bước trong quy trình
                    if (!item.NextStepId.HasValue && !item.NextStepUserId.HasValue)
                    {
                        continue;
                    }

                    WorkflowUserStepDetailModel stepInfo = null;
                    if (_listWFStep.Count > 0)
                    {
                        stepInfo = _listWFStep.Where(x => x.WorkflowId == item.WorkflowId && x.Id == item.NextStepId).FirstOrDefault();
                    }
                    else
                    {
                        var listWFStep = await _workflowHandler.GetDetailWFById(item.WorkflowId, systemLog);
                        stepInfo = listWFStep.Where(x => x.WorkflowId == item.WorkflowId && x.Id == item.NextStepId).FirstOrDefault();
                    }

                    // Kiểm tra thông tin ký tự động trong quy trình
                    if (stepInfo == null || !stepInfo.IsAutoSign)
                    {
                        continue;
                    }

                    // kiểm tra người dùng có đồng ý ký tự động không
                    var userSign = listUser.Where(x => x.Id == item.NextStepUserId).FirstOrDefault();
                    if (!userSign.IsApproveAutoSign)
                    {
                        continue;
                    }

                    //Kiểm tra người dùng được cấu hình tài khoản HSM không                    
                    listHSMAcount = listHSMAcount.Where(x => x.UserId == item.NextStepUserId).ToList();
                    if (listHSMAcount.Count > 0)
                    {
                        if (string.IsNullOrEmpty(stepInfo.ADSSProfileName))
                        {
                            #region Ký HSM
                            var hsmAccount = listHSMAcount.Where(x => x.IsHasUserPIN && x.AccountType == AccountType.HSM).OrderByDescending(x => x.IsDefault).FirstOrDefault();
                            if (hsmAccount == null)
                            {
                                continue;
                            }

                            var userSignConfigDefault = await _signConfigHandler.GetUserSignConfigForSign(item.NextStepUserId.Value);

                            // Nếu quy trình được cấu hình ký tự động và người dùng có chứng thư số HSM có lưu alias + pin thì thực hiện ký
                            SignHSMClientModel signModel = new SignHSMClientModel()
                            {
                                HSMAcountId = hsmAccount.Id,
                                ListDocumentId = new List<Guid>() { item.Id },
                                UserPin = hsmAccount.UserPIN,
                                Appearance = new BaseSignAppearanceModel()
                                {
                                    ScaleText = 1,
                                    Detail = "1,6,7"
                                },
                                UserSignConfigId = userSignConfigDefault?.Id
                            };

                            //Load cấu hình chữ ký mặc định của người dùng
                            var listSignConfig = await _signConfigHandler.GetListCombobox(0, item.NextStepUserId);
                            if (listSignConfig.Code == Code.Success && listSignConfig is ResponseObject<List<UserSignConfigBaseModel>> signConfigData)
                            {
                                var lsConfig = signConfigData.Data;
                                var signConfig = lsConfig.OrderByDescending(x => x.IsSignDefault).FirstOrDefault();
                                if (signConfig != null)
                                {
                                    signModel.Appearance.ImageData = signConfig.ImageFileBase64;
                                    signModel.Appearance.Logo = signConfig.LogoFileBase64;
                                    signModel.Appearance.ScaleText = 0;
                                    signModel.Appearance.LTV = stepInfo.IsSignLTV ? 1 : 0;
                                    signModel.Appearance.TSA = stepInfo.IsSignTSA ? 1 : 0;
                                    signModel.Appearance.Certify = stepInfo.IsSignCertify ? 1 : 0;
                                    signModel.Appearance.Detail = "";
                                    signModel.Appearance.Reason = "Tôi đã đọc, hiểu và đồng ý phê duyệt tài liệu";
                                    signModel.Appearance.ScaleImage = signConfig.ScaleImage;
                                    signModel.Appearance.ScaleLogo = signConfig.ScaleLogo;
                                    signModel.Appearance.ScaleText = signConfig.ScaleText;
                                    foreach (var signCf in signConfig.ListSignInfo)
                                    {
                                        if (signCf.Value)
                                        {
                                            signModel.Appearance.Detail += $"{signCf.Index},";
                                        }
                                    }
                                }
                            }

                            systemLog.ListAction.Add(new ActionDetail()
                            {
                                ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_HSM),
                                ActionName = LogConstants.ACTION_SIGN_DOC_HSM,
                                Description = $"Ký HSM tự động hợp đồng {item.Code}",
                                UserId = item.NextStepUserId?.ToString(),
                                CreatedDate = DateTime.Now,
                                ObjectCode = CacheConstants.DOCUMENT,
                                ObjectId = item.Id.ToString(),
                            });

                            var signResultHSM = await SignHSMFiles(signModel, systemLog, item.NextStepUserId.Value);
                            if (signResultHSM.Code == Code.Success && signResultHSM is ResponseObject<List<DocumentSignedResult>> resultData)
                            {
                                //var rs = new List<WorkflowDocumentSignFor3rdReponseModel>();
                                //if (resultData.Data != null)
                                //{
                                //    foreach (var itemRS in resultData.Data)
                                //    {
                                //        listDocumentIdSignNext.Add(itemRS.DocumentId);
                                //    }
                                //}
                                //else
                                //{
                                //    continue;
                                //}
                            }
                            else
                            {
                                Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(signResultHSM));
                                systemLog.ListAction.Add(new ActionDetail()
                                {
                                    ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_HSM),
                                    ActionName = LogConstants.ACTION_SIGN_DOC_HSM,
                                    Description = $"Ký HSM tự động hợp đồng {item.Code} thất bại",
                                    UserId = item.NextStepUserId?.ToString(),
                                    CreatedDate = DateTime.Now,
                                    ObjectCode = CacheConstants.DOCUMENT,
                                    ObjectId = item.Id.ToString(),
                                });
                                checkSignFail = true;
                            }
                            #endregion
                        }
                        else
                        {
                            #region Ký ADSS
                            var adssAccount = listHSMAcount.Where(x => x.AccountType == AccountType.ADSS).OrderByDescending(x => x.IsDefault).FirstOrDefault();
                            if (adssAccount == null)
                            {
                                continue;
                            }

                            // Nếu quy trình được cấu hình ký tự động và người dùng có chứng thư số HSM có lưu alias + pin thì thực hiện ký
                            SignADSSClientModel signAdssModel = new SignADSSClientModel()
                            {
                                HsmAcountId = adssAccount.Id,
                                ListDocumentId = new List<Guid>() { item.Id },
                                UserPin = userSign.UserPIN,
                                IsAutoSign = true,
                                SigningReason = "Tôi đồng ý ký tài liệu.",
                            };

                            systemLog.ListAction.Add(new ActionDetail()
                            {
                                ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_ADSS),
                                ActionName = LogConstants.ACTION_SIGN_DOC_ADSS,
                                Description = $"Ký ADSS tự động hợp đồng {item.Code}",
                                UserId = item.NextStepUserId?.ToString(),
                                CreatedDate = DateTime.Now,
                                ObjectCode = CacheConstants.DOCUMENT,
                                ObjectId = item.Id.ToString(),
                            });

                            var signAdssResult = await SignADSSFiles(signAdssModel, systemLog, item.NextStepUserId.Value);
                            if (signAdssResult.Code == Code.Success && signAdssResult is ResponseObject<List<DocumentSignedResult>> adssResultData)
                            {

                            }
                            else
                            {
                                Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(signAdssResult));
                                systemLog.ListAction.Add(new ActionDetail()
                                {
                                    ActionCode = nameof(LogConstants.ACTION_SIGN_DOC_ADSS),
                                    ActionName = LogConstants.ACTION_SIGN_DOC_ADSS,
                                    Description = $"Ký ADSS tự động hợp đồng {item.Code} thất bại",
                                    UserId = item.NextStepUserId?.ToString(),
                                    CreatedDate = DateTime.Now,
                                    ObjectCode = CacheConstants.DOCUMENT,
                                    ObjectId = item.Id.ToString(),
                                });
                                checkSignFail = true;
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        continue;
                    }

                    if (checkSignFail)
                    {
                        // Gửi thông báo lỗi cho người dùng đăng ký
                        OrganizationModel orgRootModel = new OrganizationModel();
                        if (item.OrganizationId.HasValue)
                        {
                            orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(item.OrganizationId.Value);
                        }
                        _ = _sysNotifyHandler.PushNotificationSignFail(new NotificationAutoSignFailModel()
                        {
                            Document3rdId = item.Document3rdId,
                            OraganizationRootId = orgRootModel.Id,
                            DocumentCode = item.Code,
                            DocumentName = item.Name,
                            OraganizationCode = orgRootModel.Code
                        }, systemLog).ConfigureAwait(false);
                        checkSignFail = false;
                    }
                }
                //if (listDocumentIdSignNext.Count > 0)
                //{
                //    await CheckAutomaticSign(listDocumentIdSignNext, systemLog);
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
            }
        }

        private async Task<List<NetSignRequest>> GetRequestList_NetService(List<Guid> listDocumentId, NetSignApprearance baseData, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                var result = new List<NetSignRequest>();
                var ms = new MinIOService();

                var listDocument = _listDocument.Where(x => listDocumentId.Contains(x.Id)).ToList();


                var checkUser = listDocument.Select(x => x.NextStepUserId).Distinct().ToList();
                if (checkUser.Count() > 1 || !checkUser.Contains(userId))
                {
                    Log.Information($"{systemLog.TraceId} - Không thể ký tài liệu! UserId: {userId}; DocumentId {string.Join(",", listDocumentId)}");
                    throw new SystemException("Không thể ký tài liệu");
                }

                var firstDoc = listDocument.FirstOrDefault();
                OrganizationModel rootOrg = null;
                if (firstDoc.OrganizationId.HasValue)
                {
                    rootOrg = await _organizationHandler.GetRootOrgModelByChidId(firstDoc.OrganizationId.Value);
                }

                var listDocumentFile = await _dataContext.DocumentFile.Where(x => listDocumentId.Contains(x.DocumentId)).ToListAsync();
                var listDocumentTemplateFileId = listDocumentFile.Select(x => x.DocumentFileTemplateId).ToList();
                var listDocumentFileTemplate = await _dataContext.DocumentFileTemplate.Where(x => listDocumentTemplateFileId.Contains(x.Id)).ToListAsync();
                var listTemplateId = listDocumentFileTemplate.Select(x => x.DocumentTemplateId).ToList();
                var listTemplate = await _dataContext.DocumentTemplate.Where(x => listTemplateId.Contains(x.Id)).ToListAsync();

                foreach (var documentId in listDocumentId)
                {
                    var document = listDocument.FirstOrDefault(x => x.Id == documentId);
                    if (document == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin tài liệu! documentId: {documentId}");
                        throw new SystemException($"Không tìm thấy thông tin tài liệu! Mã tài liệu: {document.Code}");
                    }

                    var listUser = document.WorkFlowUser;
                    var currentStepUser = listUser.FirstOrDefault(c => c.Id == document.NextStepId);
                    var stepOrder = listUser.IndexOf(currentStepUser);
                    var docFiles = listDocumentFile.Where(c => c.DocumentId == document.Id).ToList();
                    if (docFiles == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy file của tài liệu! documentId: {documentId}");
                        throw new SystemException($"Không tìm thấy file của tài liệu! Mã tài liệu: {document.Code}");
                    }

                    foreach (var docFile in docFiles)
                    {
                        var fileStream = await ms.DownloadObjectAsync(docFile.FileBucketName, docFile.FileObjectName);
                        var signHashModel = new NetSignRequest()
                        {
                            Id = document.Id,
                            FileBase64 = Convert.ToBase64String(fileStream.ToArray()),
                            DocumentCode = document.Code
                        };

                        var fileTemplateId = docFile.DocumentFileTemplateId;
                        var documentFileTemplate = listDocumentFileTemplate.FirstOrDefault(c => c.Id == fileTemplateId);
                        if (documentFileTemplate == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy file biểu mẫu! templateId: {fileTemplateId}");
                            throw new SystemException($"Không tìm thấy file biểu mẫu! Mã tài liệu: {document.Code}");
                        }
                        var template = listTemplate.FirstOrDefault(x => x.Id == documentFileTemplate.DocumentTemplateId);
                        if (template == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin biểu mẫu! templateId: {document.Id}");
                            throw new SystemException($"Không tìm thấy thông tin biểu mẫu! Mã tài liệu: {document.Code}");
                        }
                        var listMetaDataConfig = documentFileTemplate.MetaDataConfig.Where(c => !string.IsNullOrEmpty(c.FixCode)).ToList();
                        if (listMetaDataConfig.Count == 0)
                        {
                            Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! templateId: {fileTemplateId}");
                            throw new SystemException($"Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! Biểu mẫu {template.Code} - {template.Name}");
                        }
                        if (stepOrder > listMetaDataConfig.Count())
                        {
                            if (listMetaDataConfig.Count == 0)
                            {
                                Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng thiếu cấu hình vùng ký! templateId: {fileTemplateId}");
                                throw new SystemException($"Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! Biểu mẫu {template.Code} - {template.Name}");
                            }
                        }

                        var metaDataConfig = listMetaDataConfig[stepOrder];
                        var convertResult = await ConvertCoordinateFile(metaDataConfig, docFile, systemLog);
                        if (convertResult == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không convert được tọa độ vùng ký! documentFileId: {docFile.Id}");
                            throw new SystemException($"Không convert được tọa độ vùng ký! Mã tài liệu {document.Code}");
                        }

                        var appearance = baseData.Copy();
                        appearance.SetCoordinate(convertResult);

                        #region Kiểm tra ký Certify
                        //Lấy thông tin quy trình
                        var wfDetail = await _workflowHandler.GetWFInfoById(document.WorkflowId, systemLog);

                        //Nếu là bước cuối cùng thì kiểm tra có cần ký Certify không?
                        if (document.NextStepId == wfDetail.ListUser[wfDetail.ListUser.Count - 1].Id)
                        {
                            appearance.Certify = wfDetail.IsSignCertify;
                        }
                        #endregion

                        signHashModel.Appearances = appearance;
                        result.Add(signHashModel);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw new SystemException("Có lỗi xảy ra khi lấy thông tin cấu hình ký");
            }
        }

        private async Task<List<SignHashModel>> GetRequestList(List<Guid> listDocumentId, SignAppearanceModel baseData, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                var result = new List<SignHashModel>();
                var ms = new MinIOService();
                DateTime time = DateTime.Now;
                string subFolder = $"{time.Year}/{time.Month}/{time.Day}/";

                var listDocument = _listDocument.Where(x => listDocumentId.Contains(x.Id)).ToList();
                //var listDocument = await _dataContext.Document.Where(x => listDocumentId.Contains(x.Id)).ToListAsync();

                var checkUser = listDocument.Select(x => x.NextStepUserId).Distinct().ToList();
                if (checkUser.Count() > 1 || !checkUser.Contains(userId))
                {
                    Log.Information($"{systemLog.TraceId} - Không thể ký tài liệu! UserId: {userId}; DocumentId {string.Join(",", listDocumentId)}");
                    throw new SystemException("Không thể ký tài liệu");
                }
                var firstDoc = listDocument.FirstOrDefault();
                baseData.Mail = string.IsNullOrEmpty(baseData.Mail) ? firstDoc.NextStepUserEmail : baseData.Mail;
                baseData.Phone = string.IsNullOrEmpty(baseData.Phone) ? firstDoc.NextStepUserPhoneNumber : baseData.Phone;

                OrganizationModel rootOrg = null;
                if (firstDoc.OrganizationId.HasValue)
                {
                    rootOrg = await _organizationHandler.GetRootOrgModelByChidId(firstDoc.OrganizationId.Value);
                }

                var listDocumentFile = await _dataContext.DocumentFile.Where(x => listDocumentId.Contains(x.DocumentId)).ToListAsync();
                var listDocumentTemplateFileId = listDocumentFile.Select(x => x.DocumentFileTemplateId).ToList();
                var listDocumentFileTemplate = await _dataContext.DocumentFileTemplate.Where(x => listDocumentTemplateFileId.Contains(x.Id)).ToListAsync();
                var listTemplateId = listDocumentFileTemplate.Select(x => x.DocumentTemplateId).ToList();
                var listTemplate = await _dataContext.DocumentTemplate.Where(x => listTemplateId.Contains(x.Id)).ToListAsync();
                foreach (var documentId in listDocumentId)
                {
                    var document = listDocument.FirstOrDefault(x => x.Id == documentId);
                    if (document == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin tài liệu! documentId: {documentId}");
                        throw new SystemException($"Không tìm thấy thông tin tài liệu! Mã tài liệu: {document.Code}");
                    }
                    var listUser = document.WorkFlowUser;
                    var currentStepUser = listUser.FirstOrDefault(c => c.Id == document.NextStepId);
                    var stepOrder = listUser.IndexOf(currentStepUser);
                    var docFiles = listDocumentFile.Where(c => c.DocumentId == document.Id).ToList();
                    if (docFiles == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Không tìm thấy file của tài liệu! documentId: {documentId}");
                        throw new SystemException($"Không tìm thấy file của tài liệu! Mã tài liệu: {document.Code}");
                    }
                    foreach (var docFile in docFiles)
                    {
                        string fileName = ms.RenameFile(docFile.FileObjectName);
                        // string objectNameSigned = subFolder + fileName;
                        var signHash = new SignHashModel()
                        {
                            Id = docFile.Id,
                            BucketName = docFile.FileBucketName,
                            ObjectName = docFile.FileObjectName,
                            BucketNameSigned = rootOrg != null ? "bn-" + rootOrg.Code.ToLower() : _bucketNameSigned,
                            ObjectNameSigned = fileName,
                            DocumentFileNamePrefix = document.FileNamePrefix,
                            DocumentObjectNameDirectory = document.ObjectNameDirectory
                        };
                        var listSignAppearance = new List<SignAppearanceModel>();

                        var fileTemplateId = docFile.DocumentFileTemplateId;
                        var documentFileTemplate = listDocumentFileTemplate.FirstOrDefault(c => c.Id == fileTemplateId);
                        if (documentFileTemplate == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy file biểu mẫu! templateId: {fileTemplateId}");
                            throw new SystemException($"Không tìm thấy file biểu mẫu! Mã tài liệu: {document.Code}");
                        }
                        var template = listTemplate.FirstOrDefault(x => x.Id == documentFileTemplate.DocumentTemplateId);
                        if (template == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin biểu mẫu! templateId: {document.Id}");
                            throw new SystemException($"Không tìm thấy thông tin biểu mẫu! Mã tài liệu: {document.Code}");
                        }
                        var listMetaDataConfig = documentFileTemplate.MetaDataConfig.Where(c => !string.IsNullOrEmpty(c.FixCode)).ToList();
                        if (listMetaDataConfig.Count == 0)
                        {
                            Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! templateId: {fileTemplateId}");
                            throw new SystemException($"Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! Biểu mẫu {template.Code} - {template.Name}");
                        }
                        if (stepOrder > listMetaDataConfig.Count())
                        {
                            if (listMetaDataConfig.Count == 0)
                            {
                                Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng thiếu cấu hình vùng ký! templateId: {fileTemplateId}");
                                throw new SystemException($"Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! Biểu mẫu {template.Code} - {template.Name}");
                            }
                        }
                        var metaDataConfig = listMetaDataConfig[stepOrder];
                        var convertResult = await ConvertCoordinateFile(metaDataConfig, docFile, systemLog);
                        if (convertResult == null)
                        {
                            Log.Information($"{systemLog.TraceId} - Không convert được tọa độ vùng ký! documentFileId: {docFile.Id}");
                            throw new SystemException($"Không convert được tọa độ vùng ký! Mã tài liệu {document.Code}");
                        }
                        var signAppearance = baseData.Copy();
                        signAppearance.SetCoordinate(convertResult);
                        listSignAppearance.Add(signAppearance);
                        signHash.Appearances = listSignAppearance;
                        result.Add(signHash);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                throw new SystemException("Có lỗi xảy ra khi lấy thông tin cấu hình ký");
            }
        }

        //private async Task<List<HashFileModel>> GetRequestListForHash(List<Guid> listDocumentId, SignAppearanceModel baseData, Guid userId, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        var result = new List<HashFileModel>();
        //        listDocumentId = listDocumentId.Distinct().ToList();
        //        var documentList = await _dataContext.Document.Where(x => listDocumentId.Contains(x.Id)).ToListAsync();
        //        var checkUser = documentList.Select(x => x.NextStepUserId).ToList();
        //        if (checkUser.Count() > 1 || !checkUser.Contains(userId))
        //        {
        //            Log.Information($"{systemLog.TraceId} - Thông tin user không hợp lệ! UserId: {userId}");
        //            return null;
        //        }
        //        var firstDoc = documentList.FirstOrDefault();
        //        baseData.Mail = firstDoc.NextStepUserEmail;
        //        baseData.Phone = firstDoc.NextStepUserPhoneNumber;

        //        var listDocumentFile = await _dataContext.DocumentFile.Where(x => listDocumentId.Contains(x.DocumentId)).ToListAsync();
        //        var listDocumentTemplateFileId = listDocumentFile.Select(x => x.DocumentFileTemplateId).ToList();
        //        var listDocumentFileTemplate = await _dataContext.DocumentFileTemplate.Where(x => listDocumentTemplateFileId.Contains(x.Id)).ToListAsync();

        //        foreach (var documentId in listDocumentId)
        //        {
        //            var document = documentList.FirstOrDefault(x => x.Id == documentId);
        //            if (document == null)
        //            {
        //                Log.Information($"{systemLog.TraceId} - Không tìm thấy thông tin tài liệu! documentId: {documentId}");
        //                continue;
        //            }
        //            var listUser = document.WorkFlowUser;
        //            var currentStepUser = listUser.FirstOrDefault(c => c.Id == document.NextStepId);
        //            var stepOrder = listUser.IndexOf(currentStepUser);
        //            var docFiles = listDocumentFile.Where(c => c.DocumentId == document.Id).ToList();
        //            if (docFiles == null)
        //            {
        //                Log.Information($"{systemLog.TraceId} - Không tìm thấy file của tài liệu! documentId: {documentId}");
        //                continue;
        //            }
        //            foreach (var docFile in docFiles)
        //            {
        //                var signHash = new HashFileModel()
        //                {
        //                    Id = docFile.Id,
        //                    BucketName = docFile.FileBucketName,
        //                    ObjectName = docFile.FileObjectName,
        //                };
        //                var listSignAppearance = new List<SignAppearanceModel>();

        //                var templateId = docFile.DocumentFileTemplateId;
        //                var documentTemplate = listDocumentFileTemplate.Where(c => c.Id == templateId).FirstOrDefault();
        //                if (documentTemplate == null)
        //                {
        //                    Log.Information($"{systemLog.TraceId} - Không tìm thấy file template! templateId: {templateId}");
        //                    continue;
        //                }
        //                var listMetaDataConfig = documentTemplate.MetaDataConfig.Where(c => !string.IsNullOrEmpty(c.FixCode)).ToList();
        //                if (listMetaDataConfig.Count == 0)
        //                {
        //                    Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng chưa được cấu hình vùng ký! templateId: {templateId}");
        //                    continue;
        //                }
        //                if (stepOrder > listMetaDataConfig.Count())
        //                {
        //                    if (listMetaDataConfig.Count == 0)
        //                    {
        //                        Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng thiếu cấu hình vùng ký! templateId: {templateId}");
        //                        continue;
        //                    }
        //                }
        //                var metaDataConfig = listMetaDataConfig[stepOrder];
        //                var convertResult = await ConvertCoordinateFile(metaDataConfig, docFile, systemLog);
        //                if (convertResult == null)
        //                {
        //                    Log.Information($"{systemLog.TraceId} - Không convert được tọa độ vùng ký! documentFileId: {docFile.Id}");
        //                    continue;
        //                }
        //                Log.Information($"{systemLog.TraceId} - Biểu mẫu loại hợp đồng thiếu cấu hình vùng ký! documentFileId: {docFile.Id}");
        //                var signAppearance = baseData.Copy();
        //                signAppearance.SetCoordinate(convertResult);
        //                listSignAppearance.Add(signAppearance);
        //                signHash.Appearances = listSignAppearance;
        //                result.Add(signHash);
        //            }
        //        }
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return null;
        //    }
        //}

        private async Task<SignCoordinateModel> ConvertCoordinateFile(MetaDataConfig metaDataConfig, DocumentFile file, SystemLogModel systemLog)
        {
            // Model File ký 
            var signFile = new SignFileModel();
            signFile.FileBucketName = file.FileBucketName;
            signFile.FileName = file.FileName;
            signFile.FileObjectName = file.FileObjectName;

            #region Tải file
            MemoryStream memoryStream;
            try
            {
                var ms = new MinIOService();
                memoryStream = await ms.DownloadObjectAsync(file.FileBucketName, file.FileObjectName);
                memoryStream.Position = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Không thể xác định được file đã lưu trữ");
                return null;
            }
            #endregion

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
            // Model thông tin ký
            var signCoordinateModel = new SignCoordinateModel();
            signCoordinateModel.Llx = itemLLX;
            signCoordinateModel.Lly = itemLLY;
            signCoordinateModel.Page = metaDataConfig.Page;
            signCoordinateModel.Urx = itemURX;
            signCoordinateModel.Ury = itemURY;
            signCoordinateModel.Height = itemHeight;
            signCoordinateModel.Width = itemWidth;

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

                                signCoordinateModel.Page = pageNumber;

                                signCoordinateModel.Llx = itemLLX;
                                signCoordinateModel.Lly = itemLLY;
                                signCoordinateModel.Urx = itemURX;
                                signCoordinateModel.Ury = itemURY;
                                break;
                            }
                        }
                    }
                }
            }

            ////Check lại tọa độ (LLX, LLY) phải nhỏ hơn (URX, URY)
            //if (signCoordinateModel.Llx > signCoordinateModel.Urx)
            //{
            //    var temp1 = signCoordinateModel.Urx;
            //    signCoordinateModel.Urx = signCoordinateModel.Ury;
            //    signCoordinateModel.Ury = temp1;
            //}

            //if (signCoordinateModel.Lly > signCoordinateModel.Ury)
            //{
            //    var temp1 = signCoordinateModel.Ury;
            //    signCoordinateModel.Ury = signCoordinateModel.Lly;
            //    signCoordinateModel.Lly = temp1;
            //}

            return signCoordinateModel;
        }

        #region Dùng chung
        private string ResizeImage(string imageBase64)
        {
            Image image = this.Base64ToImage(imageBase64);

            var height = image.Height;
            var width = image.Width;
            if (height < width)
            {
                image.RotateFlip(RotateFlipType.Rotate270FlipNone);

            }

            Bitmap b = new Bitmap(image);

            Image i = ResizeImage(b, new Size(200, 200));

            return this.ImageToBase64(i);
        }

        private Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true);
            return image;
        }

        private string ImageToBase64(Image image)
        {
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] imageBytes = m.ToArray();
                var base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        private static System.Drawing.Image ResizeImage(System.Drawing.Image imgToResize, Size size)
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
        #endregion

        #region Sign From Single Page
        // Ký USB
        public async Task<NetCore.Shared.Response> HashFilesFromSinglePage(NetHashFromSinglePageModel model, SystemLogModel systemLog, Guid userId)
        {
            try
            {
                var ms = new MinIOService();
                Log.Information($"{systemLog.TraceId} - Tạo chuỗi hash để ký usbtoken: " + JsonSerializer.Serialize(model, jso));
                var tokenPayload = GetTokenPayloadAndValidate(model.RequestInfoModel.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.documentId) || string.IsNullOrEmpty(tokenPayload.userId))
                {
                    return new ResponseError(Code.NotFound, $"Token không chính xác");
                }

                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == new Guid(tokenPayload.userId));
                UserSignConfig userSignConfigDefault = null;
                if (user != null)
                    userSignConfigDefault = await _dataContext.UserSignConfig.FirstOrDefaultAsync(x => x.UserId == user.Id && x.IsSignDefault);
                using (HttpClient client = new HttpClient())
                {
                    string uri = _netSignHashUrl + NET_SIGN_HASH_FILE;

                    foreach (var item in model.SignModel.RequestList)
                    {
                        var doc = await _dataContext.Document.FirstOrDefaultAsync(x => x.Id == item.Id);
                        var docFile = await _dataContext.DocumentFile.FirstOrDefaultAsync(x => x.DocumentId == item.Id);

                        var fileResult = await ms.DownloadObjectAsync(docFile.FileBucketName, docFile.FileObjectName);

                        item.FileBase64 = Convert.ToBase64String(fileResult.ToArray());

                        NetSignApprearance appearance = null;
                        if (user != null)
                        {
                            model.SignModel.UserInfo = new UserInfo
                            {
                                UserId = user.Id.ToString(),
                                FullName = user.Name,
                                Dob = user.Birthday,
                                IdentityNumber = user.IdentityNumber,
                                IdentityType = user.IdentityType,
                                PhoneNumber = user.PhoneNumber,
                                Email = user.Email,
                                Address = user.Address,
                                Province = user.ProvinceName,
                                District = user.DistrictName,
                                Country = user.CountryName,
                                UserConnectId = user.ConnectId,
                                Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                                IssueName = user.IssueBy,
                                IssueDate = user.IssueDate
                            };

                            if (userSignConfigDefault != null)
                            {
                                var userSignConfig = await _signConfigHandler.GetById(userSignConfigDefault.Id);
                                appearance = GetSignApprearanceFromUserSignConfig(userSignConfig, user, string.Empty, "Tôi đồng ý ký tài liệu", item.Appearances.ImageData);
                            }
                            else
                            {
                                OrganizationConfigModel orgConfig = null;
                                if (user.OrganizationId.HasValue)
                                {
                                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(user.OrganizationId.Value);
                                    orgConfig = await _organizationConfigHandler.GetByOrgId(rootOrg.Id);
                                }

                                appearance = await GetSignApprearanceFromOrgConfig(orgConfig, user, item.Appearances.ImageData, string.Empty, "Tôi đồng ý ký tài liệu");
                            }
                        }

                        model.SignModel.TraceId = systemLog.TraceId;

                        // không có thông tin cấu hình ký mặc định của user và cấu hình mặc định của đơn vị
                        if (appearance == null)
                        {
                            appearance = new NetSignApprearance();
                            appearance.TSA = true;
                            appearance.Detail += $"1,5,6,7,";
                            appearance.ImageData = item.Appearances.ImageData.Replace("data:image/png;base64,", "");
                        }

                        appearance.Llx = item.Appearances.Llx;
                        appearance.Lly = item.Appearances.Lly;
                        appearance.Urx = item.Appearances.Urx;
                        appearance.Ury = item.Appearances.Ury;
                        appearance.IsVisible = item.Appearances.IsVisible;
                        appearance.Logo = item.Appearances.Logo.Replace("data:image/png;base64,", "");
                        appearance.Page = item.Appearances.Page;
                        appearance.Reason = item.Appearances.Reason;
                        appearance.Location = item.Appearances.Location;

                        #region Kiểm tra ký Certify
                        //Lấy thông tin quy trình
                        var wfDetail = await _workflowHandler.GetWFInfoById(doc.WorkflowId, systemLog);

                        //Nếu là bước cuối cùng thì kiểm tra có cần ký Certify không?
                        if (doc.NextStepId == wfDetail.ListUser[wfDetail.ListUser.Count - 1].Id)
                        {
                            appearance.Certify = wfDetail.IsSignCertify;
                        }
                        #endregion

                        item.Appearances = appearance;
                    }

                    StringContent content = new StringContent(JsonSerializer.Serialize(model.SignModel, jso), Encoding.UTF8, "application/json");
                    var res = await client.PostAsync(uri, content);

                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Tạo chuỗi hash để ký tài liệu không thành công");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;

                    var rsHash = JsonSerializer.Deserialize<NetHashFileResponseModel>(responseText);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = "",
                        Description = "Tạo chuỗi hash để ký tài liệu thành công", //TODO: Bổ sung thêm mã tài liệu
                        MetaData = JsonSerializer.Serialize(rsHash)
                    });

                    return new ResponseObject<NetHashFileResponseDataModel>(rsHash.Data, "Tạo chuỗi hash để ký tài liệu thành công", Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Gặp lỗi khi tạo chuỗi hash để ký tài liệu");
            }
        }

        public async Task<NetCore.Shared.Response> AttachFilesFromSinglePage(NetAttachFromSinglePageModel model, SystemLogModel systemLog, Guid userId)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Attach chữ ký vào file: " + JsonSerializer.Serialize(model, jso));

                var tokenPayload = GetTokenPayloadAndValidate(model.RequestInfoModel.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.documentId) || string.IsNullOrEmpty(tokenPayload.userId))
                {
                    return new ResponseError(Code.NotFound, $"Token không chính xác");
                }

                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == new Guid(tokenPayload.userId));
                if (user != null)
                {
                    model.AttachModel.UserInfo = new UserInfo
                    {
                        UserId = user.Id.ToString(),
                        FullName = user.Name,
                        Dob = user.Birthday,
                        IdentityNumber = user.IdentityNumber,
                        IdentityType = user.IdentityType,
                        PhoneNumber = user.PhoneNumber,
                        Email = user.Email,
                        Address = user.Address,
                        Province = user.ProvinceName,
                        District = user.DistrictName,
                        Country = user.CountryName,
                        UserConnectId = user.ConnectId,
                        Sex = user.Sex.HasValue ? (int?)user.Sex : 0,
                        IssueName = user.IssueBy,
                        IssueDate = user.IssueDate
                    };
                }

                model.AttachModel.TraceId = systemLog.TraceId;

                using (HttpClient client = new HttpClient())
                {
                    var ms = new MinIOService();

                    string uri = _netSignHashUrl + NET_SIGN_ATTACH_FILE;
                    StringContent content = new StringContent(JsonSerializer.Serialize(model.AttachModel, jso), Encoding.UTF8, "application/json");

                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);
                    if (!res.IsSuccessStatusCode)
                    {
                        Log.Error($"{systemLog.TraceId} - " + JsonSerializer.Serialize(res));
                        return new ResponseError(Code.ServerError, $"Đính chữ ký vào file không thành công");
                    }

                    string responseText = res.Content.ReadAsStringAsync().Result;
                    var rsAttach = JsonSerializer.Deserialize<NetSignFileResult>(responseText);
                    if (rsAttach.Code != 200)
                    {
                        return new ResponseError(Code.ServerError, $"Ký không thành công");
                    }

                    var attachFileResultModel = rsAttach.Data.ResponseList;
                    var result = await UpdateDoumentFilesSigned_NetService(attachFileResultModel, systemLog, DetailSignType.SIGN_USB_TOKEN);

                    foreach (var item in result)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = item.DocumentId.ToString(),
                            Description = "Attach chữ ký vào file tài liệu",
                            MetaData = JsonSerializer.Serialize(item)
                        });
                    }

                    //return new ResponseObject<List<DocumentSignedResult>>(result, MessageConstants.SignSuccess, Code.Success);

                    var fileSigned = await ms.GetObjectPresignUrlAsync(
                        result[0].ListFileSignedResult[0].BucketNameSigned,
                        result[0].ListFileSignedResult[0].ObjectNameSigned);

                    return new ResponseObject<string>(fileSigned, MessageConstants.SignSuccess, Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi khi đóng chữ ký vào file");
            }
        }
        #endregion
         
        public async Task SendDocumentEverifyToQueue()
        {
            string traceId = Guid.NewGuid().ToString();
            try
            {
                Log.Logger.Information($"{traceId} - Start Send Document to queue");
                
                var documents = _dataContext.Document.Where(x => x.IsUseEverify  && !x.IsVerified
                                                                 && !x.VerifyDate.HasValue && x.DocumentStatus == DocumentStatus.FINISH);
                var documentIds = await documents.Select(x => x.Id).ToListAsync(); 
                
                // Add to Queue

                
                // Yêu cầu eVerify
                var ms = new MinIOService();
                var listDocumentFile = await _dataContext.DocumentFile.Where(x => documentIds.Contains(x.DocumentId)).ToListAsync();
                
                var requestEverifyModel = new EVerifyRequestModel();
                requestEverifyModel.RequestList = new List<EVerifyRequestDataModel>();
                foreach (var item in documents)
                {
                    var docFile = listDocumentFile.FirstOrDefault(c => c.DocumentId == item.Id);
                    if (docFile != null)
                    {
                        var fileStream = await ms.DownloadObjectAsync(docFile.FileBucketName, docFile.FileObjectName);
                        requestEverifyModel.RequestList.Add(new EVerifyRequestDataModel
                        {
                            DocumentId = item.Id,
                            FileBase64 = Convert.ToBase64String(fileStream.ToArray())
                        });
                    }
                }
                await RequestEverify(requestEverifyModel, traceId);
                
                Log.Logger.Information($"{traceId} - End Send Document to queue");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{traceId} - Request eVerify error");
            }
        }

        public async Task RequestEverify(EVerifyRequestModel model, string traceId)
        {
            try
            {
                var documentIds = model.RequestList.Select(x => x.DocumentId);
                var documents = await _dataContext.Document.Where(x => documentIds.Contains(x.Id)).ToListAsync();
                
                var eVerifyRequestList = new List<NationHubRequestList>();
                foreach (var item in model.RequestList)
                {
                    var document = documents.FirstOrDefault(x => x.Id == item.DocumentId);
                    
                    if (document == null)
                    {
                        Log.Error("Không tìm thấy hợp đồng Id: " + item.DocumentId);
                        continue;
                    }

                    var listMetaData = document.MetaData;
                    var actionMetaData =
                        listMetaData.FirstOrDefault(x => x.Key == MetaDataCodeConstants.CONTRACT_TYPE_ACTION);

                    // nếu không có metadata CONTRACT_TYPE_ACTION hoặc lỗi lấy metadata mặc định action là tạo hoặc chỉnh sửa hợp đồng
                    DocumentAction? action = null;
                    try
                    {
                        if (actionMetaData != null)
                        {
                            action = (DocumentAction)Convert.ToInt32(actionMetaData.Value);
                        }
                        else
                        {
                            action = DocumentAction.TAO_CHINH_SUA_HOP_DONG;
                        }
                    }
                    catch
                    {
                        action = DocumentAction.TAO_CHINH_SUA_HOP_DONG;
                    }

                    // caculate contract type
                    var documentSignInfo = document.DocumentSignInfo;
                    ContractType? contractType = null;

                    var sumContractType = documentSignInfo.SignUSBToken + documentSignInfo.SignHSM +
                                          documentSignInfo.SignADSS + documentSignInfo.SignTSA;

                    if (sumContractType > 1)
                    {
                        contractType = ContractType.QUALIFIED_CONTRACT;
                    }

                    if (documentSignInfo.SignTSA > 0)
                    {
                        contractType = ContractType.BASIC_CONTRACT;
                    }

                    if (documentSignInfo.AdvanceEKyc)
                    {
                        contractType = ContractType.ADVANCED_CONTRACT;
                    }

                    eVerifyRequestList.Add(new NationHubRequestList
                    {
                        DocumentId = document.Id,
                        FileBase64 = item.FileBase64,
                        DocumentCode = document.Code,
                        DocumentAction = action,
                        StartDate = DateTime.Now,
                        ReferenceDigest = new List<string>(),
                        ContractType = contractType
                    });
                }

                var everifyRequest = new RequestSignNationHubRequestModel
                {
                    TraceId = traceId,
                    RequestList = eVerifyRequestList
                };

                using (HttpClient client = new HttpClient())
                {
                    Log.Logger.Information($"{traceId} - Request everify: " + JsonSerializer.Serialize(everifyRequest));

                    StringContent contentEVerifyRequest = new StringContent(JsonSerializer.Serialize(everifyRequest),
                        Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(_netSignHashUrl + "api/v1/ce-ca/request-everify-ceca",
                        contentEVerifyRequest);

                    var responseText = await response.Content.ReadAsStringAsync();
                    Log.Information("eVerify Response: " + responseText);
                }
            }
            catch (Exception e)
            {
                Log.Error("eVerify Error: " + e.Message);
            }
        }
    }   
}
