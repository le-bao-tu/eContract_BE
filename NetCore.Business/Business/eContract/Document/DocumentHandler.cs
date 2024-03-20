using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iText.Kernel.Pdf;

namespace NetCore.Business
{
    public class DocumentHandler : IDocumentHandler
    {
        #region Message
        #endregion

        string portalWebUrl = Utils.GetConfig("Web:PortalUrl");

        private const string CachePrefix = CacheConstants.DOCUMENT;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "MD.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IEmailHandler _emailHandler;
        private readonly IOrganizationConfigHandler _organizationConfigHandler;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly IUserRoleHandler _userRoleHandler;
        // private readonly ISignServiceHandler _signServiceHandler;
        private readonly IOTPHandler _otpService;
        private readonly INotifyHandler _notifyHandler;
        private readonly IUserHandler _userHandler;
        private readonly IRoleHandler _roleHandler;
        private readonly IDocumentTemplateHandler _docTempHandler;

        private OrganizationConfig _orgConf = null;

        private DateTime dateNow = DateTime.Now;

        public DocumentHandler(
            DataContext dataContext,
            ICacheService cacheService,
            IEmailHandler emailHandler,
            // ISignServiceHandler signServiceHandler,
            IOrganizationConfigHandler organizationConfigHandler,
            IOrganizationHandler organizationHandler,
            IUserRoleHandler userRoleHandler,
            IOTPHandler otpService,
            INotifyHandler notifyHandler,
            IUserHandler userHandler,
            IRoleHandler roleHandler,
            IDocumentTemplateHandler docTempHandler
            )
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _emailHandler = emailHandler;
            // _signServiceHandler = signServiceHandler;
            _organizationConfigHandler = organizationConfigHandler;
            _organizationHandler = organizationHandler;
            _userRoleHandler = userRoleHandler;
            _otpService = otpService;
            _notifyHandler = notifyHandler;
            _userHandler = userHandler;
            _roleHandler = roleHandler;
            _docTempHandler = docTempHandler;
        }

        #region From 3rd
        public async Task<Response> CreatePDFMany3rd(DocumentCreatePDFManyModel model, bool isDocx, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add many document from 3rd ({(isDocx ? "Word" : "PDF")}) : " + JsonSerializer.Serialize(model));

                //Kiểm tra thông tin chung
                if (string.IsNullOrEmpty(model.WorkFlowCode))
                {
                    return new ResponseError(Code.Forbidden, $"Mã quy trình không được để trống");
                }
                if (string.IsNullOrEmpty(model.DocumentTypeCode))
                {
                    return new ResponseError(Code.Forbidden, $"Loại hợp đồng không được để trống");
                }

                var wfCheckNull =   model.ListDocument.Any(x => x.WorkFlowUser.Any(c => string.IsNullOrEmpty(c)));
                if (wfCheckNull)
                {
                    return new ResponseError(Code.Forbidden, $"Dữ liệu người dùng trong quy trình đang null hoặc rỗng");
                }

                // Kiểm tra mã hợp đồng trùng nhau
                var listDocCode = model.ListDocument.Where(x => !string.IsNullOrEmpty(x.DocumentCode)).Select(x => x.DocumentCode.Trim());
                var listDocCodeDistinct = listDocCode.Distinct();
                if (listDocCode.Count() != listDocCodeDistinct.Count())
                {
                    return new ResponseError(Code.Forbidden, $"Mã hợp đồng tải lên đang trùng nhau");
                }

                //Kiểm tra file tải lên
                if (model.ListDocument.Any(x => string.IsNullOrEmpty(x.FileBase64)))
                {
                    return new ResponseError(Code.Forbidden, $"Chưa tải lên đủ tài liệu cần ký");
                }
                //TODO: Kiểm tra file tải lên là pdf
                //if (model.File.ContentType != "application/pdf")
                //{
                //    return new ResponseError(Code.Forbidden, $"{MessageConstants.CreateErrorMessage}  - File vừa tải lên không phải là file PDF");
                //}

                // Nếu chưa có thì tiến hành bổ sung thông tin
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối");
                }

                // Nếu chưa có thì tiến hành bổ sung thông tin thông tin kết nối đơn vị
                var documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                      .FirstOrDefaultAsync(x => x.Code == model.DocumentTypeCode && x.OrganizationId == new Guid(systemLog.OrganizationId));
                if (documentType == null)
                {
                    return new ResponseError(Code.ServerError, "Không tìm thấy loại hợp đồng tương ứng với mã loại hợp đồng: " + model.DocumentTypeCode);
                }

                var docTempByDocType = await _dataContext.DocumentTemplate.FirstOrDefaultAsync(x => x.DocumentTypeId == documentType.Id);
                var firstDocumentTemplate = (await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode)).FirstOrDefault();

                if (firstDocumentTemplate == null)
                {
                    return new ResponseError(Code.ServerError, "Loại hợp đồng " + model.DocumentTypeCode + " chưa được cấu hình biểu mẫu");
                }
                var fileTemplate = await _dataContext.DocumentFileTemplate.OrderBy(x => x.Order)
                        .FirstOrDefaultAsync(x => x.DocumentTemplateId == firstDocumentTemplate.Id);

                //Kiểm tra WF
                var wfDetail = await GetWorkFlowDetailByCode(model.WorkFlowCode, systemLog);
                if (wfDetail == null)
                {
                    return new ResponseError(Code.NotFound, "Không tìm thấy thông tin Workflow tương ứng với WorkFlowCode: " + model.WorkFlowCode);
                }

                if (model.ListDocument.Any(x => x.WorkFlowUser == null || x.WorkFlowUser.Count == 0))
                {
                    return new ResponseError(Code.Forbidden, $"WorkFlowUser không được để trống");
                }

                //maping WorkFlowUser
                //Danh sách người ký được cấu hình theo quy trình
                var listUserWF = wfDetail.ListUser;
                if (listUserWF == null)
                {
                    return new ResponseError(Code.ServerError, $"WorkFlow chưa được cấu hình");
                }

                List<WorkFlowUserDocumentModel> listUser = new List<WorkFlowUserDocumentModel>();

                foreach (var item in listUserWF)
                {
                    listUser.Add(AutoMapperUtils.AutoMap<WorkflowUserModel, WorkFlowUserDocumentModel>(item));
                }

                //// Số lượng người cần bổ sung trong quy trình
                //var userInWFCount = listUser.Where(x => x.UserId == null).Count();

                //// Kiểm tra số người ký trong quy trình có đủ không
                //if (model.ListDocument.Any(x => x.WorkFlowUser.Count < userInWFCount))
                //{
                //    Log.Information($"{systemLog.TraceId} - Thông tin quy trình truyền lên bị thiếu");
                //    return new ResponseError(Code.ServerError, $"WorkFlowUser truyền lên không đầy đủ thông tin ({wfDetail.ListUser.Count} bước)");
                //}

                //Lấy thông tin Org and User
                var listUserConnect = new List<string>();
                foreach (var item in model.ListDocument)
                {
                    if (!string.IsNullOrEmpty(item.CustomerConnectId))
                    {
                        listUserConnect.Add(item.CustomerConnectId);
                    }
                    if (!string.IsNullOrEmpty(item.CreatedByUserConnectId))
                    {
                        listUserConnect.Add(item.CreatedByUserConnectId);
                    }
                    listUserConnect = listUserConnect.Concat(item.WorkFlowUser).ToList();
                }

                //Distin danh sách người dùng
                listUserConnect = listUserConnect.Distinct().ToList();
                listUserConnect.ForEach(x => x = (!string.IsNullOrEmpty(x) ? x.Trim().ToLower() : ""));

                var orgAndUser = new OrgAndUserConnectInfoRequestModel()
                {
                    OrganizationId = model.OrganizationId,
                    CustomOrganizationId = model.CustomOrganizationId,
                    ListUserConnectId = listUserConnect
                };

                // Lấy thông tin người dùng theo connectId từ User service
                var ordAndUserResponse = await GetOrgAndUserConnectInfo(orgAndUser, systemLog);
                var orgInfo = ordAndUserResponse.OrganizationInfo;
                var rootOrgInfo = await _organizationHandler.GetRootOrgModelByChidId(orgInfo.Id);
                // systemLog.UserId = orgInfo.UserId.ToString();

                var userConectInfo = ordAndUserResponse.ListUserConnectInfo;

                if (userConectInfo.Count < listUserConnect.Count)
                {
                    //Lấy danh sách người dùng chưa được tạo tài khoản
                    var tmpUser = "";
                    foreach (var item in listUserConnect)
                    {
                        if (!userConectInfo.Any(x => x.UserConnectId == item))
                        {
                            tmpUser += item + ", ";
                        }
                    }

                    return new ResponseError(Code.Forbidden, $"Tài khoản {tmpUser.Substring(0, tmpUser.Length - 2)} chưa được đăng ký");
                }

                List<PDFConvertPNGCallbackServiceModel> listDocumentFileConvertToPNG = new List<PDFConvertPNGCallbackServiceModel>();

                DocumentBatch docBatch = null;
                if (!string.IsNullOrEmpty(model.DocumentBatch3rdId))
                {
                    docBatch = await _dataContext.DocumentBatch.FirstOrDefaultAsync(x => x.DocumentBatch3rdId == model.DocumentBatch3rdId && x.OrganizationId == new Guid(systemLog.OrganizationId));
                }

                if (docBatch == null)
                {
                    var countDocumentBatch = await _dataContext.DocumentBatch.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;
                    var documentBatchCode = Utils.GenerateAutoCode(documentType.Code + ".", countDocumentBatch) + Utils.GenerateNewRandom();

                    //Thêm mới lô hợp đồng
                    docBatch = new DocumentBatch()
                    {
                        Id = Guid.NewGuid(),
                        Code = documentBatchCode,
                        DocumentBatch3rdId = model.DocumentBatch3rdId,
                        Name = documentBatchCode,
                        Status = true,
                        CreatedDate = dateNow,
                        WorkflowId = wfDetail.Id,
                        Order = countDocumentBatch,
                        DocumentTypeId = documentType.Id,
                        CreatedUserId = orgInfo.UserId,
                        OrganizationId = orgInfo.Id,
                        IsGenerateFile = true,
                        Type = 1
                    };
                    await _dataContext.DocumentBatch.AddAsync(docBatch);
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Thêm mới lô hợp đồng từ bên thứ 3",
                        ObjectCode = CacheConstants.DOCUMENT_BATCH,
                        ObjectId = docBatch.Id.ToString(),
                        CreatedDate = dateNow
                    });
                }

                var listDocRS = new List<DocumentCreateManyResponseTempModel>();

                var count = await _dataContext.Document.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;

                // Thêm mới hợp đồng trong lô
                foreach (var item in model.ListDocument)
                {
                    var customerInfo = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.CustomerConnectId);
                    var userCreateInfo = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.CreatedByUserConnectId);

                    // Lấy danh sách người dùng trong quy trình
                    var workFlowUser = new List<UserConnectInfoModel>();
                    for (int i = 0; i < item.WorkFlowUser.Count(); i++)
                    {
                        var user = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.WorkFlowUser[i]);
                        if (user != null)
                        {
                            workFlowUser.Add(user);
                        }
                    }

                    // So sánh danh sách người dùng lấy từ hệ thống và quy trình
                    if (workFlowUser.Count() < item.WorkFlowUser.Count())
                    {
                        return new ResponseError(Code.Forbidden, $"Không xác định được người ký trong WorkFlowUser");
                    }
                    for (int i = 0; i < workFlowUser.Count(); i++)
                    {
                        if (i >= listUser.Count())
                            break;
                        var wfi = workFlowUser[i];
                        if (wfi.UserId != null && wfi.UserId != Guid.Empty)
                        {
                            listUser[i].UserId = wfi.UserId;
                            listUser[i].UserName = wfi.UserName;
                            listUser[i].UserConnectId = wfi.UserConnectId;
                            listUser[i].UserFullName = wfi.UserFullName;
                            listUser[i].UserEmail = wfi.UserEmail;
                            listUser[i].UserPhoneNumber = wfi.UserPhoneNumber;
                        }
                    }
                    foreach (var user in listUser)
                    {
                        if (user.UserId == null)
                        {
                            return new ResponseError(Code.MethodNotAllowed, $"Thiếu thông tin WorkFlow");
                        }
                    }
                    var nextStepUser = listUser[0];
                    var docCode = item.DocumentCode != null ? item.DocumentCode : (Utils.GenerateAutoCode(documentType.Code + "-", count++) + Utils.GenerateNewRandom());

                    //Tạo mới document

                    //TODO: Cần tối ưu luồng này check hết trước khi thêm
                    var checkCode = await _dataContext.Document.AnyAsync(x => x.Code == docCode);
                    if (checkCode)
                    {
                        return new ResponseError(Code.ServerError, $"Mã tài liệu đã tồn tại { docCode }");
                    }

                    var doc = new Data.Document()
                    {
                        Id = Guid.NewGuid(),
                        Code = docCode,
                        UserId = customerInfo?.UserId,
                        Email = customerInfo?.UserEmail,
                        FullName = customerInfo?.UserFullName,
                        PhoneNumber = customerInfo?.UserPhoneNumber,
                        Name = !string.IsNullOrEmpty(item.DocumentName) ? item.DocumentName : customerInfo?.UserFullName,
                        Status = true,
                        DocumentBatchId = docBatch.Id,
                        Document3rdId = item.Document3rdId,
                        CreatedDate = dateNow,
                        WorkflowId = wfDetail.Id,
                        Order = count,
                        DocumentTypeId = documentType.Id,
                        CreatedUserId = (userCreateInfo != null && userCreateInfo.UserId != null) ? userCreateInfo.UserId : orgInfo.UserId,
                        OrganizationId = orgInfo.Id,
                        DocumentStatus = DocumentStatus.PROCESSING,
                        NextStepId = nextStepUser.Id,
                        NextStepUserId = nextStepUser.UserId,
                        NextStepUserName = nextStepUser.UserName,
                        NextStepUserEmail = nextStepUser.UserEmail,
                        NextStepUserPhoneNumber = nextStepUser.UserPhoneNumber,
                        NextStepSignType = nextStepUser.Type,
                        WorkflowStartDate = dateNow,
                        WorkFlowUser = listUser,
                        MetaData = item.MetaData,
                        OneTimePassCode = Utils.GenerateNewRandom(),
                        PassCodeExpireDate = dateNow.AddDays(3),
                        BucketName = rootOrgInfo.Code,
                        ObjectNameDirectory = $"{orgInfo.Code}/{dateNow.Year}/{dateNow.Month}/{dateNow.Day}/{docCode}/",
                        FileNamePrefix = $"{orgInfo.Code}.{customerInfo?.UserName}.{docCode}"
                    };


                    await _dataContext.Document.AddAsync(doc);

                    #region Thêm DocumentNotifySchedule để thông báo và xóa DocumentNotifySchedule cũ
                    var wfUserSign = await _dataContext.WorkflowUserSign.FirstOrDefaultAsync(x => x.Id == nextStepUser.Id);
                    //if (wfUserSign != null && customerInfo != null)
                    //{
                    if (wfUserSign != null && customerInfo != null && (wfUserSign.NotifyConfigExpireId != null || wfUserSign.NotifyConfigRemindId != null) && doc.SignExpireAtDate.HasValue)
                    {
                        var documentNotifySchedule = new DocumentNotifySchedule()
                        {
                            DocumentId = doc.Id,
                            DocumentCode = doc.Code,
                            DocumentName = doc.Name,
                            UserId = customerInfo.UserId,
                            UserName = customerInfo.UserName,
                            WorkflowStepId = wfUserSign.Id,
                            NotifyConfigExpireId = wfUserSign.NotifyConfigExpireId,
                            NotifyConfigRemindId = wfUserSign.NotifyConfigRemindId,
                            SignExpireAtDate = doc.SignExpireAtDate.Value,
                            OrganizationId = doc.OrganizationId,
                            CreatedDate = DateTime.Now
                        };

                        _dataContext.DocumentNotifySchedule.Add(documentNotifySchedule);
                    }
                    #endregion

                    //Lưu file
                    var ms = new MinIOService();
                    item.FileName = System.IO.Path.GetFileNameWithoutExtension(item.FileName) + ".pdf";
                    var fileName = doc.FileNamePrefix + ".pdf";
                    //var fileName = item.FileName ?? docCode + ".pdf";
                    var file = new MinIOFileUploadResult();
                    //Upload file to MinIO
                    var bytes = Convert.FromBase64String(item.FileBase64);
                    MemoryStream contents = new MemoryStream(bytes);
                    //Convert PDF to PDF/A  

                    if (isDocx)
                    {
                        var dt = await ConvertPDF.ConvertWordToPDFAsync(new FileBase64Model()
                        {
                            FileName = fileName,
                            FileBase64 = item.FileBase64
                        });

                        if (dt.Code != Code.Success)
                        {
                            Log.Error($"{systemLog.TraceId} - Convert file docx to PDFA fail!");

                            return new ResponseError(Code.ServerError, dt.Message);
                        }

                        item.FileName = dt.FileName;
                        bytes = Convert.FromBase64String(dt.FileBase64);

                        contents = new MemoryStream(bytes);
                    }
                    ConvertPDF.ConvertToPDFA(ref contents);
                    string fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(contents);

                    fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                    fileName = ms.RenameFile(fileName);
                    file = await ms.UploadDocumentAsync(doc.BucketName, fileName, contents, false);


                    #region Lưu lịch sử khi hợp đồng thay đổi
                    await CreateDocumentWFLHistory(doc);
                    #endregion

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới hợp đồng {doc.Code} từ bên thứ 3",
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = doc.Id.ToString(),
                        CreatedDate = dateNow
                    });

                    //Lưu thông tin File document
                    var dFile = new DocumentFile()
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = doc.Id,
                        FileBucketName = file.BucketName,
                        FileObjectName = file.ObjectName,
                        FileName = Path.GetFileName(fileName),
                        FileType = FILE_TYPE.PDF,
                        ProfileName = fileTemplate.ProfileName,
                        DocumentFileTemplateId = fileTemplate.Id
                    };

                    await _dataContext.DocumentFile.AddAsync(dFile);

                    listDocumentFileConvertToPNG.Add(new PDFConvertPNGCallbackServiceModel()
                    {
                        DocumentFileId = dFile.Id,
                        //StreamData = Base64Convert.ConvertBase64ToMemoryStream(dt.FileBase64),
                        FileBase64 = fileBase64
                    });

                    listDocRS.Add(new DocumentCreateManyResponseTempModel()
                    {
                        Document = doc,
                        DocumentId = doc.Id,
                        DocumentCode = doc.Code,
                        Document3rdId = doc.Document3rdId,
                        NextStepUserId = doc.NextStepUserId,
                        NextStepUserName = doc.NextStepUserName,
                        NextStepUserEmail = doc.NextStepUserEmail,
                        NextStepUserPhoneNumber = doc.NextStepUserPhoneNumber,
                        NextStepUserFullName = nextStepUser.UserFullName,
                        OneTimePassCode = doc.OneTimePassCode,
                        State = nextStepUser.State,
                        StateName = nextStepUser.StateName,
                        SignExpireAtDate = doc.SignExpireAtDate
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(docBatch));
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }

                #region Convert hợp đồng sang file preview
                if (_orgConf.UseImagePreview)
                {
                    //Kiểm tra xem đơn vị có sử dụng hình ảnh preview hay không
                    PDFToImageService pdfToImageService = new PDFToImageService();
                    foreach (var item in listDocumentFileConvertToPNG)
                    {
                        _ = pdfToImageService.ConvertPDFBase64ToPNGCallBack(item, systemLog).ConfigureAwait(false);
                    }
                }
                #endregion

                var listAccessLink = new List<ResponseAccessLinkModel>();

                if (_orgConf.IsUseUI)
                {
                    #region Lấy ra đơn vị gốc
                    OrganizationModel orgRootModel = new OrganizationModel();
                    var rootOrg = await _organizationHandler.GetRootByChidId(model.OrganizationId);
                    if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                    {
                        orgRootModel = orgRoot.Data;
                    }
                    #endregion

                    foreach (var item in listDocRS)
                    {
                        string url = null;
                        #region Gửi thông báo cho người dùng
                        if (model.IsPos)
                        {
                            //Tạo URL
                            url = $"{Utils.GetConfig("Web:SignPageUrl:uri")}{item.DocumentCode}?email={item.NextStepUserEmail}&otp={item.OneTimePassCode}";
                        }
                        else
                        {
                            //Tạo URL
                            url = $"{Utils.GetConfig("Web:SignPageUrl:uri")}{item.DocumentCode}";

                            #region Gửi thông báo qua gateway
                            _ = _notifyHandler.SendNotificationRemindSignDocumentByGateway(new NotificationRemindSignDocumentModel()
                            {
                                TraceId = systemLog.TraceId,
                                OraganizationCode = orgRootModel.Code,
                                User = new NotifyUserModel()
                                {
                                    UserName = item.NextStepUserName,
                                    PhoneNumber = item.NextStepUserPhoneNumber,
                                    Email = item.NextStepUserEmail,
                                    FullName = item.NextStepUserFullName
                                },
                                Document = new GatewayNotifyDocumentModel()
                                {
                                    Code = item.DocumentCode,
                                    Name = item.DocumentName,
                                    OneTimePassCode = item.OneTimePassCode,
                                    Url = url,
                                }
                            }, systemLog).ConfigureAwait(false);
                            #endregion
                        }
                        #endregion

                        listAccessLink.Add(new ResponseAccessLinkModel()
                        {
                            Document3rdId = item.Document3rdId,
                            DocumentCode = item.DocumentCode,
                            Url = url,
                            State = item.State,
                            StateName = item.StateName,
                            SignExpireAtDate = item.SignExpireAtDate.HasValue ? new DateTime(item.SignExpireAtDate.Value.Ticks) : item.SignExpireAtDate
                        });
                    }
                }
                else
                {
                    listAccessLink = listDocRS.Select(x => new ResponseAccessLinkModel()
                    {
                        DocumentCode = x.DocumentCode,
                        Document3rdId = x.Document3rdId,
                        State = x.State,
                        StateName = x.StateName,
                        SignExpireAtDate = x.SignExpireAtDate.HasValue ? new DateTime(x.SignExpireAtDate.Value.Ticks) : x.SignExpireAtDate
                    }).ToList();
                }

                var rs = new CreatDocument3rdResponseModel()
                {
                    DocumentBatch3rdId = docBatch.DocumentBatch3rdId,
                    DocumentBatchCode = docBatch.Code,
                    ListDocument = listAccessLink
                };

                return new ResponseObject<CreatDocument3rdResponseModel>(rs, MessageConstants.CreateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateMetaDataMany3rd_iText7(DocumentCreateMetaDataManyModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add many document from meta data 3rd: " + JsonSerializer.Serialize(model));

                //Kiểm tra thông tin chung
                if (string.IsNullOrEmpty(model.WorkFlowCode))
                {
                    return new ResponseError(Code.Forbidden, $"Mã quy trình không được để trống");
                }
                if (string.IsNullOrEmpty(model.DocumentTypeCode))
                {
                    return new ResponseError(Code.Forbidden, $"Loại hợp đồng không được để trống");
                }

                //Kiểm tra WF
                var wfDetail = await GetWorkFlowDetailByCode(model.WorkFlowCode, systemLog);
                if (wfDetail == null)
                {
                    return new ResponseError(Code.NotFound, "Không tìm thấy thông tin Workflow tương ứng với WorkFlowCode: " + model.WorkFlowCode);
                }

                //Kiểm tra workflow đã đủ thông tin người dùng
                //chưa đủ => check WorkFlowUser truyền vào
                //đủ => ko check                
                if (model.ListDocument.Any(x => x.WorkFlowUser == null || x.WorkFlowUser.Count == 0 || x.WorkFlowUser.Any(c => string.IsNullOrEmpty(c)))
                    && wfDetail.ListUser.Any(x => !x.UserId.HasValue))
                {
                    return new ResponseError(Code.Forbidden, $"Dữ liệu người dùng trong quy trình đang null hoặc rỗng");
                }

                // Kiểm tra mã hợp đồng trùng nhau
                var listDocCode = model.ListDocument.Where(x => !string.IsNullOrEmpty(x.DocumentCode)).Select(x => x.DocumentCode.Trim());
                var listDocCodeDistinct = listDocCode.Distinct();
                if (listDocCode.Count() != listDocCodeDistinct.Count())
                {
                    return new ResponseError(Code.Forbidden, $"Mã hợp đồng tải lên đang trùng nhau");
                }

                //Kiểm tra metadata tải lên
                if (model.ListDocument.Any(x => x.ListMetaData.Count == 0))
                {
                    return new ResponseError(Code.Forbidden, $"ListMetaData cần được bổ sung dữ liệu");
                }

                // Nếu chưa có thì tiến hành bổ sung thông tin thông tin kết nối đơn vị
                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
                }
                if (_orgConf == null)
                {
                    return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối");
                }

                //Kiểm tra thông tin loại hợp đồng
                var documentType = await _dataContext.DocumentType.Where(x => x.Status == true && x.OrganizationId == new Guid(systemLog.OrganizationId))
                      .FirstOrDefaultAsync(x => x.Code == model.DocumentTypeCode);
                if (documentType == null)
                {
                    return new ResponseError(Code.ServerError, "Không tìm thấy loại hợp đồng tương ứng với mã loại hợp đồng: " + model.DocumentTypeCode);
                }

                var docTempByDocType = await _dataContext.DocumentTemplate.FirstOrDefaultAsync(x => x.DocumentTypeId == documentType.Id);
                var checkDocumentTemplate = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);

                if (checkDocumentTemplate == null || checkDocumentTemplate.Count < 1)
                {
                    return new ResponseError(Code.ServerError, "Loại hợp đồng " + model.DocumentTypeCode + " chưa được cấu hình biểu mẫu");
                }

                //maping WorkFlowUser

                //Danh sách người ký được cấu hình theo quy trình
                var listUserWF = wfDetail.ListUser;
                if (listUserWF == null)
                {
                    return new ResponseError(Code.ServerError, $"WorkFlow chưa được cấu hình");
                }

                List<WorkFlowUserDocumentModel> listUser = new List<WorkFlowUserDocumentModel>();

                foreach (var item in listUserWF)
                {
                    listUser.Add(AutoMapperUtils.AutoMap<WorkflowUserModel, WorkFlowUserDocumentModel>(item));
                }

                //// Số lượng người cần bổ sung trong quy trình
                //var userInWFCount = listUser.Where(x => x.UserId == null).Count();

                //// Kiểm tra số người ký trong quy trình có đủ không
                //if (model.ListDocument.Any(x => x.WorkFlowUser.Count < userInWFCount))
                //{
                //    Log.Information($"{systemLog.TraceId} - Thông tin quy trình truyền lên bị thiếu");
                //    return new ResponseError(Code.ServerError, $"WorkFlowUser truyền lên không đầy đủ thông tin ({wfDetail.ListUser.Count} bước)");
                //}

                //Lấy thông tin Org and User
                var listUserConnect = new List<string>();
                foreach (var item in model.ListDocument)
                {
                    if (!string.IsNullOrEmpty(item.CustomerConnectId))
                    {
                        listUserConnect.Add(item.CustomerConnectId);
                    }
                    if (!string.IsNullOrEmpty(item.CreatedByUserConnectId))
                    {
                        listUserConnect.Add(item.CreatedByUserConnectId);
                    }
                    listUserConnect = listUserConnect.Concat(item.WorkFlowUser).ToList();
                }

                //Distin danh sách người dùng
                listUserConnect = listUserConnect.Distinct().ToList();

                var orgAndUser = new OrgAndUserConnectInfoRequestModel()
                {
                    OrganizationId = model.OrganizationId,
                    CustomOrganizationId = model.CustomOrganizationId,
                    ListUserConnectId = listUserConnect
                };

                // Lấy thông tin người dùng theo connectId từ User service
                var orgAndUserResponse = await GetOrgAndUserConnectInfo(orgAndUser, systemLog);
                var orgInfo = orgAndUserResponse.OrganizationInfo;
                var rootOrgInfo = await _organizationHandler.GetRootOrgModelByChidId(orgInfo.Id);
                // systemLog.UserId = orgInfo.UserId.ToString();

                var userConectInfo = orgAndUserResponse.ListUserConnectInfo;

                if (userConectInfo.Count < listUserConnect.Count)
                {
                    //Lấy danh sách người dùng chưa được tạo tài khoản
                    var tmpUser = "";
                    foreach (var item in listUserConnect)
                    {
                        if (!userConectInfo.Any(x => x.UserConnectId == item))
                        {
                            tmpUser += item + ", ";
                        }
                    }

                    return new ResponseError(Code.Forbidden, $"Tài khoản {tmpUser.Substring(0, tmpUser.Length - 2)} chưa được đăng ký");
                }

                DocumentBatch docBatch = null;

                if (!string.IsNullOrEmpty(model.DocumentBatch3rdId))
                {
                    docBatch = await _dataContext.DocumentBatch.FirstOrDefaultAsync(x => x.DocumentBatch3rdId == model.DocumentBatch3rdId && x.OrganizationId == new Guid(systemLog.OrganizationId));
                }

                if (docBatch == null)
                {
                    var countDocumentBatch = await _dataContext.DocumentBatch.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;
                    var documentBatchCode = Utils.GenerateAutoCode(documentType.Code + ".", countDocumentBatch) + Utils.GenerateNewRandom();

                    //Thêm mới lô hợp đồng
                    docBatch = new DocumentBatch()
                    {
                        Id = Guid.NewGuid(),
                        Code = documentBatchCode,
                        Name = documentBatchCode,
                        DocumentBatch3rdId = model.DocumentBatch3rdId,
                        Status = true,
                        CreatedDate = dateNow,
                        WorkflowId = wfDetail.Id,
                        Order = countDocumentBatch,
                        DocumentTypeId = documentType.Id,
                        CreatedUserId = orgInfo.UserId,
                        OrganizationId = orgInfo.Id,
                        IsGenerateFile = true,
                        Type = 1
                    };
                    await _dataContext.DocumentBatch.AddAsync(docBatch);

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới lô hợp đồng {documentType.Code} từ bên thứ 3",
                        ObjectCode = CacheConstants.DOCUMENT_BATCH,
                        ObjectId = docBatch.Id.ToString(),
                        CreatedDate = dateNow
                    });
                }

                var ms = new MinIOService();

                //Lấy ra danh sách biểu mẫu thuộc loại hợp đồng
                var listFileTemplate = new List<DocumentFileTemplate>();

                // file template stream
                var docTempValids = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);
                var listDocumentTemplate = await _dataContext.DocumentTemplate.Where(x => docTempValids.Select(x1 => x1.Id).Contains(x.Id)).ToListAsync();
                var listFileStreamTemplate = await GetFileTemplateDocument(listDocumentTemplate, listFileTemplate, systemLog);

                var listDocRS = new List<DocumentCreateManyResponseTempModel>();
                var count = await _dataContext.Document.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;

                List<PDFConvertPNGCallbackServiceModel> listDocumentFileConvertToPNG = new List<PDFConvertPNGCallbackServiceModel>();

                // Thêm mới hợp đồng trong lô
                foreach (var item in model.ListDocument)
                {
                    var customerInfo = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.CustomerConnectId);
                    var userCreateInfo = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.CreatedByUserConnectId);

                    // Lấy danh sách người dùng trong quy trình
                    var workFlowUser = new List<UserConnectInfoModel>();
                    for (int i = 0; i < item.WorkFlowUser.Count(); i++)
                    {
                        var user = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.WorkFlowUser[i]);
                        if (user != null)
                        {
                            workFlowUser.Add(user);
                        }
                    }

                    // So sánh danh sách người dùng lấy từ hệ thống và quy trình
                    if (workFlowUser.Count() < item.WorkFlowUser.Count())
                    {
                        return new ResponseError(Code.Forbidden, $"Không xác định được người ký trong WorkFlowUser");
                    }
                    for (int i = 0; i < workFlowUser.Count(); i++)
                    {
                        if (i >= listUser.Count())
                            break;
                        var wfi = workFlowUser[i];
                        if (wfi.UserId != null && wfi.UserId != Guid.Empty)
                        {
                            listUser[i].UserId = wfi.UserId;
                            listUser[i].UserName = wfi.UserName;
                            listUser[i].UserConnectId = wfi.UserConnectId;
                            listUser[i].UserFullName = wfi.UserFullName;
                            listUser[i].UserEmail = wfi.UserEmail;
                            listUser[i].UserPhoneNumber = wfi.UserPhoneNumber;
                        }
                    }
                    foreach (var user in listUser)
                    {
                        if (user.UserId == null)
                        {
                            return new ResponseError(Code.MethodNotAllowed, $"Thiếu thông tin WorkFlow");
                        }
                    }
                    var nextStepUser = listUser[0];
                    var docCode = item.DocumentCode != null ? item.DocumentCode : (Utils.GenerateAutoCode(documentType.Code + "-", count++) + Utils.GenerateNewRandom());
                    var documentId = Guid.NewGuid();

                    //Tạo mới document

                    //TODO: Cần tối ưu luồng này check hết trước khi thêm
                    var checkCode = await _dataContext.Document.AnyAsync(x => x.Code == item.DocumentCode);
                    if (checkCode)
                    {
                        return new ResponseError(Code.ServerError, $"Mã tài liệu đã tồn tại { item.DocumentCode }");
                    }

                    //Fill thông tin từ Meta data vào file template

                    #region Sinh file pdf từ meta data
                    //Log.Information("Bat dau sinh file tu pdf");
                    //Log.Information(timer.ElapsedMilliseconds.ToString());
                    //Log.Information("Hoan thanh tai file ve tu MDM - so luong file: " + listFileTemplate.Count.ToString());
                    //Log.Information(timer.ElapsedMilliseconds.ToString());

                    //Mỗi danh sách meta data sẽ được Document

                    //Log.Information("File: " + metaData.FullName);
                    //Log.Information(timer.ElapsedMilliseconds.ToString());

                    var document3rdId = item.ListMetaData.Where(x => x.MetaDataCode == MetaDataCodeConstants.ID).Select(x => x.MetaDataValue).FirstOrDefault();

                    item.ListMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = MetaDataCodeConstants.DOC_ID,
                        MetaDataValue = "Doc ID#: " + documentId.ToString()
                    });

                    List<DocumentMetaData> metaData = new List<DocumentMetaData>();
                    if (item.ListMetaData != null && item.ListMetaData.Count > 0)
                    {
                        metaData = item.ListMetaData.Select(x => new DocumentMetaData()
                        {
                            Key = x.MetaDataCode,
                            Value = x.MetaDataValue
                        }).ToList();
                    }

                    if (item.MetaData != null && item.MetaData.Count > 0)
                    {
                        metaData = metaData.Concat(item.MetaData).ToList();
                    }

                    //TODO: Kiểm tra ngày hết hạn
                    DateTime? signExpireDate = null;
                    if (nextStepUser.SignExpireAfterDay.HasValue)
                    {
                        signExpireDate = dateNow.AddDays(nextStepUser.SignExpireAfterDay.Value);
                    }

                    DateTime? signCloseDate = null;
                    if (nextStepUser.SignCloseAfterDay.HasValue)
                    {
                        signCloseDate = dateNow.AddDays(nextStepUser.SignCloseAfterDay.Value);
                    }

                    var doc = new Data.Document()
                    {
                        Id = documentId,
                        Code = docCode,
                        UserId = customerInfo?.UserId,
                        Email = customerInfo?.UserEmail,
                        FullName = customerInfo?.UserFullName,
                        PhoneNumber = customerInfo?.UserPhoneNumber,
                        Name = !string.IsNullOrEmpty(item.DocumentName) ? item.DocumentName : customerInfo?.UserFullName,
                        Status = true,
                        DocumentBatchId = docBatch.Id,
                        CreatedDate = dateNow,
                        WorkflowId = wfDetail.Id,
                        Order = count,
                        DocumentTypeId = documentType.Id,
                        Document3rdId = item.Document3rdId ?? document3rdId,
                        CreatedUserId = (userCreateInfo != null && userCreateInfo.UserId != null) ? userCreateInfo.UserId : orgInfo.UserId,
                        OrganizationId = orgInfo.Id,
                        DocumentStatus = DocumentStatus.PROCESSING,
                        NextStepId = nextStepUser.Id,
                        NextStepUserId = nextStepUser.UserId,
                        NextStepUserName = nextStepUser.UserName,
                        NextStepUserEmail = nextStepUser.UserEmail,
                        StateId = nextStepUser.StateId,
                        State = nextStepUser.State,
                        SignExpireAtDate = signExpireDate,
                        NextStepUserPhoneNumber = nextStepUser.UserPhoneNumber,
                        NextStepSignType = nextStepUser.Type,
                        WorkflowStartDate = dateNow,
                        WorkFlowUser = listUser,
                        MetaData = metaData,
                        OneTimePassCode = Utils.GenerateNewRandom(),
                        PassCodeExpireDate = dateNow.AddDays(3),
                        BucketName = rootOrgInfo.Code,
                        ObjectNameDirectory = $"{orgInfo.Code}/{dateNow.Year}/{dateNow.Month}/{dateNow.Day}/{docCode}/",
                        FileNamePrefix = $"{orgInfo.Code}.{customerInfo.UserName}.{docCode}",
                        SignCloseAtDate = signCloseDate,
                        CreatedUserName = item.CreatedByUserName,
                        ExportDocumentData = item.ExportDocumentData,
                        IsUseEverify = wfDetail.IsUseEverify
                        //MetaDataJson = JsonSerializer.Serialize(item.ListMetaData)
                    };

                    await _dataContext.Document.AddAsync(doc);

                    #region Thêm DocumentNotifySchedule để thông báo và xóa DocumentNotifySchedule cũ
                    var wfUserSign = await _dataContext.WorkflowUserSign.FirstOrDefaultAsync(x => x.Id == nextStepUser.Id);
                    if (wfUserSign != null && customerInfo != null && (wfUserSign.NotifyConfigExpireId != null || wfUserSign.NotifyConfigRemindId != null) && doc.SignExpireAtDate.HasValue)
                    {
                        var documentNotifySchedule = new DocumentNotifySchedule()
                        {
                            DocumentId = doc.Id,
                            DocumentCode = doc.Code,
                            DocumentName = doc.Name,
                            UserId = customerInfo.UserId,
                            UserName = customerInfo.UserName,
                            WorkflowStepId = wfUserSign.Id,
                            NotifyConfigExpireId = wfUserSign.NotifyConfigExpireId,
                            NotifyConfigRemindId = wfUserSign.NotifyConfigRemindId,
                            SignExpireAtDate = doc.SignExpireAtDate.Value,
                            OrganizationId = doc.OrganizationId,
                            CreatedDate = DateTime.Now
                        };

                        _dataContext.DocumentNotifySchedule.Add(documentNotifySchedule);
                    }
                    #endregion

                    #region Lưu lịch sử khi hợp đồng thay đổi
                    await CreateDocumentWFLHistory(doc);
                    #endregion

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới hợp đồng {doc.Code} từ bên thứ 3",
                        ObjectCode = CacheConstants.DOCUMENT,
                        ObjectId = doc.Id.ToString(),
                        CreatedDate = dateNow
                    });

                    // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);

                    var minioRS = new MinIOFileUploadResult();

                    foreach (var template in listDocumentTemplate)
                    {
                        //Lấy ra danh sách file thuộc biểu mẫu
                        var fileTemplate = listFileTemplate.Where(x => x.DocumentTemplateId == template.Id).ToList();

                        //Duyệt từng biểu mẫu và thêm vào danh sách template document
                        foreach (var file2 in fileTemplate)
                        {
                            // lấy ra templateStream
                            var fileTemplateStreamModel = listFileStreamTemplate.FirstOrDefault(x => x.Id == file2.Id);

                            //Kiểm tra định dạng file
                            // File DOCX
                            if (file2.FileType == TemplateFileType.DOCX)
                            {
                                #region Xử lý dữ liệu
                                var listMetaDataValue = new List<MetaDataFileValue>();

                                // Lấy danh sách MetaData thuộc biểu mẫu
                                var template1 = listDocumentTemplate.FirstOrDefault(x => x.Id == file2.DocumentTemplateId);

                                // Cấu hình Meta Data trên file pdf
                                var fileMetaDataConfig = template1.MetaDataConfig;

                                // Giá trị của Meta data bên tên
                                var listMetaDataDraft = item.ListMetaData;

                                foreach (var config in fileMetaDataConfig)
                                {
                                    var meta = listMetaDataDraft.Find(c => c.MetaDataCode == config.MetaDataCode);

                                    if (meta != null)
                                    {
                                        listMetaDataValue.Add(new MetaDataFileValue()
                                        {
                                            MetaDataId = config.MetaDataId,
                                            MetaDataValue = meta.MetaDataValue,
                                            MetaDataCode = meta.MetaDataCode,
                                            MetaDataName = meta.MetaDataName,
                                        });
                                    }

                                }
                                #endregion

                                #region Gọi service thứ 3
                                var listData = listMetaDataValue.Select(x => new KeyValueModel()
                                {
                                    Key = $"{DocumentTemplateConstants.KeyPrefix}{x.MetaDataCode}{DocumentTemplateConstants.KeySubfix}",
                                    Value = x.MetaDataValue
                                }).ToList();

                                var fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(fileTemplateStreamModel.FileTemplateStream);

                                var dt = await ConvertPDF.ConvertDocxMetaDataToPDFAsync(new FileBase64Model()
                                {
                                    FileName = item.FileName,
                                    ListData = listData,
                                    FileBase64 = fileBase64
                                });

                                if (dt.Code != Code.Success)
                                {
                                    Log.Error($"{systemLog.TraceId} - Convert file docx and meta data to PDFA fail!");

                                    return new ResponseError(Code.ServerError, dt.Message);
                                }

                                #endregion

                                #region Send File to MinIO
                                var fileName = doc.FileNamePrefix + ".pdf";
                                try
                                {
                                    MemoryStream memStream = Base64Convert.ConvertBase64ToMemoryStream(dt.FileBase64);

                                    fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                                    fileName = ms.RenameFile(fileName);

                                    minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, memStream, false);

                                    var dFile = new DocumentFile()
                                    {
                                        Id = Guid.NewGuid(),
                                        DocumentId = doc.Id,
                                        FileName = Path.GetFileName(fileName),
                                        FileBucketName = minioRS.BucketName,
                                        FileObjectName = minioRS.ObjectName,
                                        ProfileName = "",
                                        CreatedDate = dateNow,
                                        FileType = FILE_TYPE.PDF,
                                        DocumentFileTemplateId = file2.Id
                                    };

                                    await _dataContext.DocumentFile.AddAsync(dFile);
                                    listDocumentFileConvertToPNG.Add(new PDFConvertPNGCallbackServiceModel()
                                    {
                                        DocumentFileId = dFile.Id,
                                        //StreamData = Base64Convert.ConvertBase64ToMemoryStream(dt.FileBase64),
                                        FileBase64 = dt.FileBase64
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, $"{systemLog.TraceId}");
                                    // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
                                    // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                                    return new ResponseError(Code.ServerError, $"Không thể upload file {fileName} lên server");
                                }
                                #endregion
                            }
                            // Mặc định là file PDF
                            else
                            {
                                #region Xử lý dữ liệu
                                // Cấu hình Meta Data trên file pdf
                                var fileMetaDataConfig = file2.MetaDataConfig;

                                // Giá trị của Meta data bên tên
                                var listMetaDataDraft = item.ListMetaData;

                                var listMetaDataValue = new List<MetaDataFileValue>();

                                foreach (var config in fileMetaDataConfig)
                                {
                                    var meta = listMetaDataDraft.Find(c => c.MetaDataCode == config.MetaDataCode);

                                    if (meta != null)
                                    {
                                        listMetaDataValue.Add(new MetaDataFileValue()
                                        {
                                            MetaDataId = config.MetaDataId,
                                            MetaDataValue = meta.MetaDataValue,
                                            MetaDataCode = meta.MetaDataCode,
                                            MetaDataName = meta.MetaDataName,
                                            Page = config.Page,
                                            TextAlign = config.TextAlign,
                                            TextDecoration = config.TextDecoration,
                                            Font = config.Font,
                                            FontStyle = config.FontStyle,
                                            FontSize = config.FontSize,
                                            FontWeight = config.FontWeight,
                                            Color = config.Color,
                                            LLX = config.LLX,
                                            LLY = config.LLY,
                                            PageHeight = config.PageHeight,
                                            PageWidth = config.PageWidth,
                                            Height = config.Height,
                                            Width = config.Width,
                                            BorderWidthOfPage = config.BorderWidthOfPage,
                                        });
                                    }

                                }
                                #endregion

                                var streamWriter = await FillMetaDataToPdfWithIText7(listMetaDataValue, fileTemplateStreamModel.FileTemplateStream, systemLog);

                                #region Send File to MinIO
                                var fileName = doc.FileNamePrefix + ".pdf";
                                try
                                {
                                    byte[] pdfBytes = streamWriter.ToArray();

                                    fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                                    fileName = ms.RenameFile(fileName);

                                    var streamConvert = new MemoryStream(pdfBytes);
                                    ConvertPDF.ConvertToPDFA(ref streamConvert);

                                    string fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(streamConvert);

                                    minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, streamConvert, false);

                                    var dFile = new DocumentFile()
                                    {
                                        Id = Guid.NewGuid(),
                                        DocumentId = doc.Id,
                                        FileName = Path.GetFileName(fileName),
                                        FileBucketName = minioRS.BucketName,
                                        FileObjectName = minioRS.ObjectName,
                                        ProfileName = "",
                                        CreatedDate = dateNow,
                                        FileType = FILE_TYPE.PDF,
                                        DocumentFileTemplateId = file2.Id
                                    };

                                    await _dataContext.DocumentFile.AddAsync(dFile);

                                    listDocumentFileConvertToPNG.Add(new PDFConvertPNGCallbackServiceModel()
                                    {
                                        DocumentFileId = dFile.Id,
                                        StreamData = streamConvert,
                                        FileBase64 = fileBase64
                                    });

                                    streamConvert.Close();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, $"{systemLog.TraceId}");
                                    // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
                                    // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                                    return new ResponseError(Code.ServerError, $"Không thể upload file {fileName} lên server");
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    listDocRS.Add(new DocumentCreateManyResponseTempModel()
                    {
                        DocumentId = doc.Id,
                        DocumentCode = doc.Code,
                        Document3rdId = doc.Document3rdId,
                        NextStepUserId = doc.NextStepUserId,
                        NextStepUserName = doc.NextStepUserName,
                        NextStepUserEmail = doc.NextStepUserEmail,
                        NextStepUserPhoneNumber = doc.NextStepUserPhoneNumber,
                        NextStepUserFullName = nextStepUser.UserFullName,
                        OneTimePassCode = doc.OneTimePassCode,
                        State = nextStepUser.State,
                        StateName = nextStepUser.StateName,
                        SignExpireAtDate = doc.SignExpireAtDate
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(docBatch));
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }

                #region Convert hợp đồng sang file preview
                // Kiểm tra xem đơn vị có dùng hình ảnh preview hay không
                if (_orgConf.UseImagePreview)
                {
                    PDFToImageService pdfToImageService = new PDFToImageService();
                    foreach (var item in listDocumentFileConvertToPNG)
                    {
                        _ = pdfToImageService.ConvertPDFBase64ToPNGCallBack(item, systemLog).ConfigureAwait(false);
                    }
                }
                #endregion

                var listAccessLink = new List<ResponseAccessLinkModel>();

                if (_orgConf.IsUseUI)
                {
                    #region Lấy ra đơn vị gốc
                    OrganizationModel orgRootModel = await _organizationHandler.GetRootOrgModelByChidId(model.OrganizationId);
                    #endregion
                    foreach (var item in listDocRS)
                    {
                        string url = null;
                        #region Gửi thông báo cho người dùng
                        if (model.IsPos)
                        {
                            //Tạo URL
                            url = $"{Utils.GetConfig("Web:SignPageUrl:uri")}{item.DocumentCode}?email={item.NextStepUserEmail}&otp={item.OneTimePassCode}";
                        }
                        else
                        {
                            //Tạo URL
                            url = $"{Utils.GetConfig("Web:SignPageUrl:uri")}{item.DocumentCode}";

                            #region Gửi thông báo qua gateway
                            _ = _notifyHandler.SendNotificationRemindSignDocumentByGateway(new NotificationRemindSignDocumentModel()
                            {
                                TraceId = systemLog.TraceId,
                                OraganizationCode = orgRootModel.Code,
                                User = new NotifyUserModel()
                                {
                                    UserName = item.NextStepUserName,
                                    PhoneNumber = item.NextStepUserPhoneNumber,
                                    Email = item.NextStepUserEmail,
                                    FullName = item.NextStepUserFullName
                                },
                                Document = new GatewayNotifyDocumentModel()
                                {
                                    Code = item.DocumentCode,
                                    Name = item.DocumentName,
                                    OneTimePassCode = item.OneTimePassCode,
                                    Url = url,
                                }
                            }, systemLog).ConfigureAwait(false);
                            #endregion
                        }
                        #endregion

                        listAccessLink.Add(new ResponseAccessLinkModel()
                        {
                            Document3rdId = item.Document3rdId,
                            DocumentCode = item.DocumentCode,
                            Url = url,
                            State = item.State,
                            StateName = item.StateName,
                            SignExpireAtDate = item.SignExpireAtDate.HasValue ? new DateTime(item.SignExpireAtDate.Value.Ticks) : item.SignExpireAtDate
                        }); ;
                    }
                }
                else
                {
                    listAccessLink = listDocRS.Select(x => new ResponseAccessLinkModel()
                    {
                        DocumentCode = x.DocumentCode,
                        Document3rdId = x.Document3rdId,
                        State = x.State,
                        StateName = x.StateName,
                        SignExpireAtDate = x.SignExpireAtDate.HasValue ? new DateTime(x.SignExpireAtDate.Value.Ticks) : x.SignExpireAtDate
                    }).ToList();
                }

                var rs = new CreatDocument3rdResponseModel()
                {
                    DocumentBatch3rdId = docBatch.DocumentBatch3rdId,
                    DocumentBatchCode = docBatch.Code,
                    ListDocument = listAccessLink
                };

                return new ResponseObject<CreatDocument3rdResponseModel>(rs, MessageConstants.CreateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        //public async Task<Response> CreateMetaDataMany3rd_SpirePdf(DocumentCreateMetaDataManyModel model, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        Log.Information($"{systemLog.TraceId} - Add many document from meta data 3rd: " + JsonSerializer.Serialize(model));

        //        //Kiểm tra thông tin chung
        //        if (string.IsNullOrEmpty(model.WorkFlowCode))
        //        {
        //            return new ResponseError(Code.Forbidden, $"Mã quy trình không được để trống");
        //        }
        //        if (string.IsNullOrEmpty(model.DocumentTypeCode))
        //        {
        //            return new ResponseError(Code.Forbidden, $"Loại hợp đồng không được để trống");
        //        }

        //        //Kiểm tra WF
        //        var wfDetail = await GetWorkFlowDetailByCode(model.WorkFlowCode, systemLog);
        //        if (wfDetail == null)
        //        {
        //            return new ResponseError(Code.NotFound, "Không tìm thấy thông tin Workflow tương ứng với WorkFlowCode: " + model.WorkFlowCode);
        //        }

        //        //Kiểm tra workflow đã đủ thông tin người dùng
        //        //chưa đủ => check WorkFlowUser truyền vào
        //        //đủ => ko check                
        //        if (model.ListDocument.Any(x => x.WorkFlowUser == null || x.WorkFlowUser.Count == 0 || x.WorkFlowUser.Any(c => string.IsNullOrEmpty(c)))
        //            && wfDetail.ListUser.Any(x => !x.UserId.HasValue))
        //        {
        //            return new ResponseError(Code.Forbidden, $"Dữ liệu người dùng trong quy trình đang null hoặc rỗng");
        //        }

        //        // Kiểm tra mã hợp đồng trùng nhau
        //        var listDocCode = model.ListDocument.Where(x => !string.IsNullOrEmpty(x.DocumentCode)).Select(x => x.DocumentCode.Trim());
        //        var listDocCodeDistinct = listDocCode.Distinct();
        //        if (listDocCode.Count() != listDocCodeDistinct.Count())
        //        {
        //            return new ResponseError(Code.Forbidden, $"Mã hợp đồng tải lên đang trùng nhau");
        //        }

        //        //Kiểm tra metadata tải lên
        //        if (model.ListDocument.Any(x => x.ListMetaData.Count == 0))
        //        {
        //            return new ResponseError(Code.Forbidden, $"ListMetaData cần được bổ sung dữ liệu");
        //        }

        //        // Nếu chưa có thì tiến hành bổ sung thông tin thông tin kết nối đơn vị
        //        if (_orgConf == null)
        //        {
        //            _orgConf = await _organizationConfigHandler.InternalGetByOrgId(new Guid(systemLog.OrganizationId));
        //        }
        //        if (_orgConf == null)
        //        {
        //            return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối");
        //        }

        //        //Kiểm tra thông tin loại hợp đồng
        //        var documentType = await _dataContext.DocumentType.Where(x => x.Status == true && x.OrganizationId == new Guid(systemLog.OrganizationId))
        //              .FirstOrDefaultAsync(x => x.Code == model.DocumentTypeCode);
        //        if (documentType == null)
        //        {
        //            return new ResponseError(Code.ServerError, "Không tìm thấy loại hợp đồng tương ứng với mã loại hợp đồng: " + model.DocumentTypeCode);
        //        }

        //        var docTempByDocType = await _dataContext.DocumentTemplate.FirstOrDefaultAsync(x => x.DocumentTypeId == documentType.Id);
        //        var checkDocumentTemplate = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);

        //        if (checkDocumentTemplate == null || checkDocumentTemplate.Count < 1)
        //        {
        //            return new ResponseError(Code.ServerError, "Loại hợp đồng " + model.DocumentTypeCode + " chưa được cấu hình biểu mẫu");
        //        }

        //        //maping WorkFlowUser

        //        //Danh sách người ký được cấu hình theo quy trình
        //        var listUserWF = wfDetail.ListUser;
        //        if (listUserWF == null)
        //        {
        //            return new ResponseError(Code.ServerError, $"WorkFlow chưa được cấu hình");
        //        }

        //        List<WorkFlowUserDocumentModel> listUser = new List<WorkFlowUserDocumentModel>();

        //        foreach (var item in listUserWF)
        //        {
        //            listUser.Add(AutoMapperUtils.AutoMap<WorkflowUserModel, WorkFlowUserDocumentModel>(item));
        //        }

        //        //// Số lượng người cần bổ sung trong quy trình
        //        //var userInWFCount = listUser.Where(x => x.UserId == null).Count();

        //        //// Kiểm tra số người ký trong quy trình có đủ không
        //        //if (model.ListDocument.Any(x => x.WorkFlowUser.Count < userInWFCount))
        //        //{
        //        //    Log.Information($"{systemLog.TraceId} - Thông tin quy trình truyền lên bị thiếu");
        //        //    return new ResponseError(Code.ServerError, $"WorkFlowUser truyền lên không đầy đủ thông tin ({wfDetail.ListUser.Count} bước)");
        //        //}

        //        //Lấy thông tin Org and User
        //        var listUserConnect = new List<string>();
        //        foreach (var item in model.ListDocument)
        //        {
        //            if (!string.IsNullOrEmpty(item.CustomerConnectId))
        //            {
        //                listUserConnect.Add(item.CustomerConnectId);
        //            }
        //            if (!string.IsNullOrEmpty(item.CreatedByUserConnectId))
        //            {
        //                listUserConnect.Add(item.CreatedByUserConnectId);
        //            }
        //            listUserConnect = listUserConnect.Concat(item.WorkFlowUser).ToList();
        //        }

        //        //Distin danh sách người dùng
        //        listUserConnect = listUserConnect.Distinct().ToList();

        //        var orgAndUser = new OrgAndUserConnectInfoRequestModel()
        //        {
        //            OrganizationId = model.OrganizationId,
        //            CustomOrganizationId = model.CustomOrganizationId,
        //            ListUserConnectId = listUserConnect
        //        };

        //        // Lấy thông tin người dùng theo connectId từ User service
        //        var orgAndUserResponse = await GetOrgAndUserConnectInfo(orgAndUser, systemLog);
        //        var orgInfo = orgAndUserResponse.OrganizationInfo;
        //        var rootOrgInfo = await _organizationHandler.GetRootOrgModelByChidId(orgInfo.Id);
        //        // systemLog.UserId = orgInfo.UserId.ToString();

        //        var userConectInfo = orgAndUserResponse.ListUserConnectInfo;

        //        if (userConectInfo.Count < listUserConnect.Count)
        //        {
        //            //Lấy danh sách người dùng chưa được tạo tài khoản
        //            var tmpUser = "";
        //            foreach (var item in listUserConnect)
        //            {
        //                if (!userConectInfo.Any(x => x.UserConnectId == item))
        //                {
        //                    tmpUser += item + ", ";
        //                }
        //            }

        //            return new ResponseError(Code.Forbidden, $"Tài khoản {tmpUser.Substring(0, tmpUser.Length - 2)} chưa được đăng ký");
        //        }

        //        DocumentBatch docBatch = null;

        //        if (!string.IsNullOrEmpty(model.DocumentBatch3rdId))
        //        {
        //            docBatch = await _dataContext.DocumentBatch.FirstOrDefaultAsync(x => x.DocumentBatch3rdId == model.DocumentBatch3rdId && x.OrganizationId == new Guid(systemLog.OrganizationId));
        //        }

        //        if (docBatch == null)
        //        {
        //            var countDocumentBatch = await _dataContext.DocumentBatch.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;
        //            var documentBatchCode = Utils.GenerateAutoCode(documentType.Code + ".", countDocumentBatch) + Utils.GenerateNewRandom();

        //            //Thêm mới lô hợp đồng
        //            docBatch = new DocumentBatch()
        //            {
        //                Id = Guid.NewGuid(),
        //                Code = documentBatchCode,
        //                Name = documentBatchCode,
        //                DocumentBatch3rdId = model.DocumentBatch3rdId,
        //                Status = true,
        //                CreatedDate = dateNow,
        //                WorkflowId = wfDetail.Id,
        //                Order = countDocumentBatch,
        //                DocumentTypeId = documentType.Id,
        //                CreatedUserId = orgInfo.UserId,
        //                OrganizationId = orgInfo.Id,
        //                IsGenerateFile = true,
        //                Type = 1
        //            };
        //            await _dataContext.DocumentBatch.AddAsync(docBatch);

        //            systemLog.ListAction.Add(new ActionDetail()
        //            {
        //                Description = $"Thêm mới lô hợp đồng {documentType.Code} từ bên thứ 3",
        //                ObjectCode = CacheConstants.DOCUMENT_BATCH,
        //                ObjectId = docBatch.Id.ToString(),
        //                CreatedDate = dateNow
        //            });
        //        }

        //        var ms = new MinIOService();

        //        //Lấy ra danh sách biểu mẫu thuộc loại hợp đồng
        //        var listFileTemplate = new List<DocumentFileTemplate>();

        //        var docTempValids = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);
        //        var listDocumentTemplate = await _dataContext.DocumentTemplate.Where(x => docTempValids.Select(x1 => x1.Id).Contains(x.Id)).ToListAsync();
        //        var listFileTemplateStream = await GetFileTemplateDocument(listDocumentTemplate, listFileTemplate, systemLog);

        //        var listDocRS = new List<DocumentCreateManyResponseTempModel>();
        //        var count = await _dataContext.Document.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;

        //        List<PDFConvertPNGCallbackServiceModel> listDocumentFileConvertToPNG = new List<PDFConvertPNGCallbackServiceModel>();

        //        // Thêm mới hợp đồng trong lô
        //        foreach (var item in model.ListDocument)
        //        {
        //            var customerInfo = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.CustomerConnectId);
        //            var userCreateInfo = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.CreatedByUserConnectId);

        //            // Lấy danh sách người dùng trong quy trình
        //            var workFlowUser = new List<UserConnectInfoModel>();
        //            for (int i = 0; i < item.WorkFlowUser.Count(); i++)
        //            {
        //                var user = userConectInfo.FirstOrDefault(x => x.UserConnectId == item.WorkFlowUser[i]);
        //                if (user != null)
        //                {
        //                    workFlowUser.Add(user);
        //                }
        //            }

        //            // So sánh danh sách người dùng lấy từ hệ thống và quy trình
        //            if (workFlowUser.Count() < item.WorkFlowUser.Count())
        //            {
        //                return new ResponseError(Code.Forbidden, $"Không xác định được người ký trong WorkFlowUser");
        //            }
        //            for (int i = 0; i < workFlowUser.Count(); i++)
        //            {
        //                if (i >= listUser.Count())
        //                    break;
        //                var wfi = workFlowUser[i];
        //                if (wfi.UserId != null && wfi.UserId != Guid.Empty)
        //                {
        //                    listUser[i].UserId = wfi.UserId;
        //                    listUser[i].UserName = wfi.UserName;
        //                    listUser[i].UserConnectId = wfi.UserConnectId;
        //                    listUser[i].UserFullName = wfi.UserFullName;
        //                    listUser[i].UserEmail = wfi.UserEmail;
        //                    listUser[i].UserPhoneNumber = wfi.UserPhoneNumber;
        //                }
        //            }
        //            foreach (var user in listUser)
        //            {
        //                if (user.UserId == null)
        //                {
        //                    return new ResponseError(Code.MethodNotAllowed, $"Thiếu thông tin WorkFlow");
        //                }
        //            }
        //            var nextStepUser = listUser[0];
        //            var docCode = item.DocumentCode != null ? item.DocumentCode : (Utils.GenerateAutoCode(documentType.Code + "-", count++) + Utils.GenerateNewRandom());
        //            var documentId = Guid.NewGuid();

        //            //Tạo mới document

        //            //TODO: Cần tối ưu luồng này check hết trước khi thêm
        //            var checkCode = await _dataContext.Document.AnyAsync(x => x.Code == item.DocumentCode);
        //            if (checkCode)
        //            {
        //                return new ResponseError(Code.ServerError, $"Mã tài liệu đã tồn tại { item.DocumentCode }");
        //            }

        //            //Fill thông tin từ Meta data vào file template

        //            #region Sinh file pdf từ meta data
        //            //Log.Information("Bat dau sinh file tu pdf");
        //            //Log.Information(timer.ElapsedMilliseconds.ToString());
        //            //Log.Information("Hoan thanh tai file ve tu MDM - so luong file: " + listFileTemplate.Count.ToString());
        //            //Log.Information(timer.ElapsedMilliseconds.ToString());

        //            //Mỗi danh sách meta data sẽ được Document

        //            //Log.Information("File: " + metaData.FullName);
        //            //Log.Information(timer.ElapsedMilliseconds.ToString());

        //            var document3rdId = item.ListMetaData.Where(x => x.MetaDataCode == MetaDataCodeConstants.ID).Select(x => x.MetaDataValue).FirstOrDefault();

        //            item.ListMetaData.Add(new MetaDataListForDocumentType()
        //            {
        //                MetaDataCode = MetaDataCodeConstants.DOC_ID,
        //                MetaDataValue = "Doc ID#: " + documentId.ToString()
        //            });

        //            List<DocumentMetaData> metaData = new List<DocumentMetaData>();
        //            if (item.ListMetaData != null && item.ListMetaData.Count > 0)
        //            {
        //                metaData = item.ListMetaData.Select(x => new DocumentMetaData()
        //                {
        //                    Key = x.MetaDataCode,
        //                    Value = x.MetaDataValue
        //                }).ToList();
        //            }

        //            if (item.MetaData != null && item.MetaData.Count > 0)
        //            {
        //                metaData = metaData.Concat(item.MetaData).ToList();
        //            }

        //            //TODO: Kiểm tra ngày hết hạn
        //            DateTime? signExpireDate = null;
        //            if (nextStepUser.SignExpireAfterDay.HasValue)
        //            {
        //                signExpireDate = dateNow.AddDays(nextStepUser.SignExpireAfterDay.Value);
        //            }

        //            DateTime? signCloseDate = null;
        //            if (nextStepUser.SignCloseAfterDay.HasValue)
        //            {
        //                signCloseDate = dateNow.AddDays(nextStepUser.SignCloseAfterDay.Value);
        //            }

        //            var doc = new Data.Document()
        //            {
        //                Id = documentId,
        //                Code = docCode,
        //                UserId = customerInfo?.UserId,
        //                Email = customerInfo?.UserEmail,
        //                FullName = customerInfo?.UserFullName,
        //                PhoneNumber = customerInfo?.UserPhoneNumber,
        //                Name = !string.IsNullOrEmpty(item.DocumentName) ? item.DocumentName : customerInfo?.UserFullName,
        //                Status = true,
        //                DocumentBatchId = docBatch.Id,
        //                CreatedDate = dateNow,
        //                WorkflowId = wfDetail.Id,
        //                Order = count,
        //                DocumentTypeId = documentType.Id,
        //                Document3rdId = item.Document3rdId ?? document3rdId,
        //                CreatedUserId = (userCreateInfo != null && userCreateInfo.UserId != null) ? userCreateInfo.UserId : orgInfo.UserId,
        //                OrganizationId = orgInfo.Id,
        //                DocumentStatus = DocumentStatus.PROCESSING,
        //                NextStepId = nextStepUser.Id,
        //                NextStepUserId = nextStepUser.UserId,
        //                NextStepUserName = nextStepUser.UserName,
        //                NextStepUserEmail = nextStepUser.UserEmail,
        //                StateId = nextStepUser.StateId,
        //                State = nextStepUser.State,
        //                SignExpireAtDate = signExpireDate,
        //                NextStepUserPhoneNumber = nextStepUser.UserPhoneNumber,
        //                NextStepSignType = nextStepUser.Type,
        //                WorkflowStartDate = dateNow,
        //                WorkFlowUser = listUser,
        //                MetaData = metaData,
        //                OneTimePassCode = Utils.GenerateNewRandom(),
        //                PassCodeExpireDate = dateNow.AddDays(3),
        //                BucketName = rootOrgInfo.Code,
        //                ObjectNameDirectory = $"{orgInfo.Code}/{dateNow.Year}/{dateNow.Month}/{dateNow.Day}/{docCode}/",
        //                FileNamePrefix = $"{orgInfo.Code}.{customerInfo.UserName}.{docCode}",
        //                SignCloseAtDate = signCloseDate
        //                //MetaDataJson = JsonSerializer.Serialize(item.ListMetaData)
        //            };

        //            await _dataContext.Document.AddAsync(doc);

        //            #region Thêm DocumentNotifySchedule để thông báo và xóa DocumentNotifySchedule cũ
        //            var wfUserSign = await _dataContext.WorkflowUserSign.FirstOrDefaultAsync(x => x.Id == nextStepUser.Id);
        //            if (wfUserSign != null && customerInfo != null && (wfUserSign.NotifyConfigExpireId != null || wfUserSign.NotifyConfigRemindId != null) && doc.SignExpireAtDate.HasValue)
        //            {
        //                var documentNotifySchedule = new DocumentNotifySchedule()
        //                {
        //                    DocumentId = doc.Id,
        //                    DocumentCode = doc.Code,
        //                    DocumentName = doc.Name,
        //                    UserId = customerInfo.UserId,
        //                    UserName = customerInfo.UserName,
        //                    WorkflowStepId = wfUserSign.Id,
        //                    NotifyConfigExpireId = wfUserSign.NotifyConfigExpireId,
        //                    NotifyConfigRemindId = wfUserSign.NotifyConfigRemindId,
        //                    SignExpireAtDate = doc.SignExpireAtDate.Value,
        //                    OrganizationId = doc.OrganizationId,
        //                    CreatedDate = DateTime.Now
        //                };

        //                _dataContext.DocumentNotifySchedule.Add(documentNotifySchedule);
        //            }
        //            #endregion

        //            #region Lưu lịch sử khi hợp đồng thay đổi
        //            await CreateDocumentWFLHistory(doc);
        //            #endregion

        //            systemLog.ListAction.Add(new ActionDetail()
        //            {
        //                Description = $"Thêm mới hợp đồng {doc.Code} từ bên thứ 3",
        //                ObjectCode = CacheConstants.DOCUMENT,
        //                ObjectId = doc.Id.ToString(),
        //                CreatedDate = dateNow
        //            });

        //            // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);

        //            foreach (var template in listDocumentTemplate)
        //            {
        //                //Lấy ra danh sách file thuộc biểu mẫu
        //                var fileTemplate = listFileTemplate.Where(x => x.DocumentTemplateId == template.Id).ToList();

        //                //Duyệt từng biểu mẫu và thêm vào danh sách template document
        //                foreach (var file2 in fileTemplate)
        //                {
        //                    var fileTemplateStreamModel = listFileTemplateStream.FirstOrDefault(x => x.Id == file2.Id);

        //                    //Kiểm tra định dạng file
        //                    // File DOCX
        //                    if (file2.FileType == TemplateFileType.DOCX)
        //                    {
        //                        #region Xử lý dữ liệu
        //                        // Lấy danh sách MetaData thuộc biểu mẫu
        //                        var template1 = listDocumentTemplate.FirstOrDefault(x => x.Id == file2.DocumentTemplateId);

        //                        // Cấu hình Meta Data trên file pdf
        //                        var fileMetaDataConfig = template1.MetaDataConfig;

        //                        // Giá trị của Meta data bên tên
        //                        var listMetaDataDraft = item.ListMetaData;

        //                        var listMetaDataValue = new List<MetaDataFileValue>();

        //                        foreach (var config in fileMetaDataConfig)
        //                        {
        //                            var meta = listMetaDataDraft.Find(c => c.MetaDataCode == config.MetaDataCode);

        //                            if (meta != null)
        //                            {
        //                                listMetaDataValue.Add(new MetaDataFileValue()
        //                                {
        //                                    MetaDataId = config.MetaDataId,
        //                                    MetaDataValue = meta.MetaDataValue,
        //                                    MetaDataCode = meta.MetaDataCode,
        //                                    MetaDataName = meta.MetaDataName,
        //                                });
        //                            }

        //                        }
        //                        #endregion

        //                        #region Spire DOC

        //                        //Document document = new Document();
        //                        //document.LoadFromFile(@"E:\Work\Documents\WordDocuments\New Zealand.docx");

        //                        //document.Replace("New Zealand", "NZ", false, true);

        //                        //document.SaveToFile("Replace.docx", FileFormat.Docx);
        //                        //System.Diagnostics.Process.Start("Replace.docx");

        //                        #endregion

        //                        #region Gọi service thứ 3
        //                        var listData = listMetaDataValue.Select(x => new KeyValueModel()
        //                        {
        //                            Key = $"{DocumentTemplateConstants.KeyPrefix}{x.MetaDataCode}{DocumentTemplateConstants.KeySubfix}",
        //                            Value = x.MetaDataValue
        //                        }).ToList();

        //                        var fileBase64 = "";

        //                        using (FileStream fileStream = File.OpenRead(file2.FileObjectName))
        //                        {
        //                            MemoryStream memStream = new MemoryStream();
        //                            memStream.SetLength(fileStream.Length);
        //                            fileStream.Read(memStream.GetBuffer(), 0, (int)fileStream.Length);

        //                            //Convert Steam to Base64
        //                            fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(memStream);
        //                        }

        //                        var dt = await ConvertPDF.ConvertDocxMetaDataToPDFAsync(new FileBase64Model()
        //                        {
        //                            FileName = item.FileName,
        //                            ListData = listData,
        //                            FileBase64 = fileBase64
        //                        });

        //                        if (dt.Code != Code.Success)
        //                        {
        //                            Log.Error($"{systemLog.TraceId} - Convert file docx and meta data to PDFA fail!");

        //                            return new ResponseError(Code.ServerError, dt.Message);
        //                        }

        //                        #endregion

        //                        #region Send File to MinIO
        //                        var fileName = doc.FileNamePrefix + ".pdf";
        //                        try
        //                        {
        //                            MemoryStream memStream = Base64Convert.ConvertBase64ToMemoryStream(dt.FileBase64);

        //                            fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

        //                            fileName = ms.RenameFile(fileName);

        //                            MinIOFileUploadResult minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, memStream, false);

        //                            var dFile = new DocumentFile()
        //                            {
        //                                Id = Guid.NewGuid(),
        //                                DocumentId = doc.Id,
        //                                FileName = Path.GetFileName(fileName),
        //                                FileBucketName = minioRS.BucketName,
        //                                FileObjectName = minioRS.ObjectName,
        //                                ProfileName = "",
        //                                CreatedDate = dateNow,
        //                                FileType = FILE_TYPE.PDF,
        //                                DocumentFileTemplateId = file2.Id
        //                            };

        //                            await _dataContext.DocumentFile.AddAsync(dFile);

        //                            listDocumentFileConvertToPNG.Add(new PDFConvertPNGCallbackServiceModel()
        //                            {
        //                                DocumentFileId = dFile.Id,
        //                                //StreamData = Base64Convert.ConvertBase64ToMemoryStream(dt.FileBase64),
        //                                FileBase64 = dt.FileBase64
        //                            });
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Log.Error(ex, $"{systemLog.TraceId}");
        //                            // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
        //                            // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
        //                            return new ResponseError(Code.ServerError, $"Không thể upload file {fileName} lên server");
        //                        }
        //                        #endregion
        //                    }
        //                    // Mặc định là file PDF
        //                    else
        //                    {
        //                        #region Xử lý dữ liệu
        //                        // Cấu hình Meta Data trên file pdf
        //                        var fileMetaDataConfig = file2.MetaDataConfig;

        //                        // Giá trị của Meta data bên tên
        //                        var listMetaDataDraft = item.ListMetaData;

        //                        var listMetaDataValue = new List<MetaDataFileValue>();

        //                        foreach (var config in fileMetaDataConfig)
        //                        {
        //                            var meta = listMetaDataDraft.Find(c => c.MetaDataCode == config.MetaDataCode);

        //                            if (meta != null)
        //                            {
        //                                listMetaDataValue.Add(new MetaDataFileValue()
        //                                {
        //                                    MetaDataId = config.MetaDataId,
        //                                    MetaDataValue = meta.MetaDataValue,
        //                                    MetaDataCode = meta.MetaDataCode,
        //                                    MetaDataName = meta.MetaDataName,
        //                                    Page = config.Page,
        //                                    TextAlign = config.TextAlign,
        //                                    TextDecoration = config.TextDecoration,
        //                                    Font = config.Font,
        //                                    FontStyle = config.FontStyle,
        //                                    FontSize = config.FontSize,
        //                                    FontWeight = config.FontWeight,
        //                                    Color = config.Color,
        //                                    LLX = config.LLX,
        //                                    LLY = config.LLY,
        //                                    PageHeight = config.PageHeight,
        //                                    PageWidth = config.PageWidth,
        //                                    Height = config.Height,
        //                                    Width = config.Width,
        //                                    BorderWidthOfPage = config.BorderWidthOfPage,
        //                                });
        //                            }

        //                        }
        //                        #endregion

        //                        #region Tạo file template mới + fill text = Spire PDF

        //                        var timer = new Stopwatch();
        //                        Log.Information("Bat dau Fill Text vao PDF");
        //                        timer.Start();
        //                        Log.Information(timer.ElapsedMilliseconds.ToString());

        //                        //Dùng thư viện Spire PDF bổ sung text vào file pdf                              
        //                        Spire.Pdf.PdfDocument pdfTemplate = new Spire.Pdf.PdfDocument();
        //                        pdfTemplate.LoadFromStream(fileTemplateStreamModel.FileTemplateStream);

        //                        Spire.Pdf.PdfDocument pdfWriter = new Spire.Pdf.PdfDocument();

        //                        int numberOfPages = pdfTemplate.Pages.Count;

        //                        for (var i = 1; i <= numberOfPages; i++)
        //                        {
        //                            // copy nội dung pdf từng page của template sang pdf được tạo mới
        //                            Spire.Pdf.PdfPageBase basePage = pdfTemplate.Pages[i - 1];
        //                            System.Drawing.SizeF pagesize = basePage.Size;
        //                            Spire.Pdf.Graphics.PdfTemplate templateReader = basePage.CreateTemplate();

        //                            pdfWriter.Pages.Add(pagesize, new Spire.Pdf.Graphics.PdfMargins(0, 0));
        //                            pdfWriter.Pages[i - 1].Canvas.DrawTemplate(templateReader, new System.Drawing.Point(0, 0));

        //                            Spire.Pdf.PdfPageBase basePageWriter = pdfWriter.Pages[i - 1];

        //                            #region Processing PDF Spire Pdf
        //                            string text = "";

        //                            foreach (var itemMT in listMetaDataValue)
        //                            {
        //                                #region Set font
        //                                Spire.Pdf.Graphics.PdfTrueTypeFont bf;
        //                                var fontName = string.Empty;
        //                                try
        //                                {
        //                                    // Font Style 
        //                                    var fontStyle = string.Empty;
        //                                    if (itemMT.FontWeight == "bold")
        //                                        fontStyle += "_BOLD";
        //                                    else if (itemMT.FontWeight == "light")
        //                                        fontStyle += "_LIGHT";

        //                                    if (itemMT.FontStyle == "italic") fontStyle += "_ITALIC";
        //                                    // if (item.TextDecoration == "underline") fontStyle += "_UNDERLINE";

        //                                    switch (itemMT.Font)
        //                                    {
        //                                        case "Arial":
        //                                            fontName += "ARIAL" + fontStyle + ".ttf";
        //                                            break;
        //                                        case "Calibri":
        //                                            fontName += "CALIBRI" + fontStyle + ".ttf";
        //                                            break;
        //                                        case "Helvetica":
        //                                            fontName += "HELVETICA" + fontStyle + ".ttf";
        //                                            break;
        //                                        case "Tahoma":
        //                                            fontName += "TAHOMA" + fontStyle + ".ttf";
        //                                            break;
        //                                        case "Times New Roman":
        //                                            fontName += "TIMES" + fontStyle + ".ttf";
        //                                            break;
        //                                        case "Roboto":
        //                                            fontName += "ROBOTO" + fontStyle + ".ttf";
        //                                            break;
        //                                        default:
        //                                            fontName += "ARIAL" + fontStyle + ".ttf";
        //                                            break;
        //                                    }

        //                                    // byte[] fontBinary = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/" + fontName));
        //                                }
        //                                catch (Exception ex)
        //                                {
        //                                    pdfWriter.Close();
        //                                    pdfTemplate.Close();

        //                                    Log.Error(ex, $"{systemLog.TraceId} - Có lỗi xảy ra");
        //                                    // systemLog.Description = $"Có lỗi xảy ra khi tạo file {item.DocumentCode} - {ex.Message}";
        //                                    // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
        //                                    return new ResponseObject<bool>(false, "Font " + fontName + " hiện tại chưa được hỗ trợ ở phiên bản này", Code.ServerError);
        //                                }

        //                                #endregion

        //                                #region Gạch chân và màu sắc
        //                                // Lấy màu sắc từ mã hex
        //                                var sysDrawingColor = Utils.HexToColor(itemMT.Color ?? string.Empty);

        //                                Spire.Pdf.Graphics.PdfSolidBrush brush = new Spire.Pdf.Graphics.PdfSolidBrush(new Spire.Pdf.Graphics.PdfRGBColor(sysDrawingColor));

        //                                var fontStyleSpire = Spire.Pdf.Graphics.PdfFontStyle.Regular;
        //                                if (itemMT.TextDecoration == "underline") fontStyleSpire = Spire.Pdf.Graphics.PdfFontStyle.Underline;
        //                                if (itemMT.FontWeight == "bold") fontStyleSpire = Spire.Pdf.Graphics.PdfFontStyle.Bold;

        //                                var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/" + fontName);
        //                                bf = new Spire.Pdf.Graphics.PdfTrueTypeFont(fontPath, itemMT.FontSize, fontStyleSpire, true);
        //                                #endregion

        //                                #region Calculate coordinate
        //                                // put the alignment and coordinates here
        //                                text = itemMT.MetaDataValue ?? "";

        //                                // Tính kích thước trên iTextSharp
        //                                var pageHeight = pagesize.Height; // Chiều cao PDF
        //                                var pageWidth = pagesize.Width; // Chiều rộng PDF

        //                                // Tỷ lệ trên Frontend
        //                                var temp = (float)pageHeight / (float)itemMT.PageHeight;

        //                                var itemHeight = (float)itemMT.Height * temp; // Độ cao vùng ký
        //                                var itemWidth = (float)itemMT.Width * temp;  // Độ rộng vùng ký
        //                                var itemLLY = pageHeight - ((float)(0 - itemMT.LLY - 9) * temp);  // Tọa độ LLY 
        //                                var itemLLX = (float)(itemMT.LLX - 9) * temp; // Tọa độ LLX

        //                                #endregion

        //                                #region Alignment + Recalculate coordinate
        //                                Spire.Pdf.Graphics.PdfTextAlignment text_alignment = Spire.Pdf.Graphics.PdfTextAlignment.Justify;

        //                                switch (itemMT.TextAlign)
        //                                {
        //                                    case "left":
        //                                        text_alignment = Spire.Pdf.Graphics.PdfTextAlignment.Left;
        //                                        break;
        //                                    case "center":
        //                                        text_alignment = Spire.Pdf.Graphics.PdfTextAlignment.Center;
        //                                        itemLLX += (float)itemWidth / 2;
        //                                        break;
        //                                    case "right":
        //                                        text_alignment = Spire.Pdf.Graphics.PdfTextAlignment.Right;
        //                                        itemLLX += (float)itemWidth;
        //                                        break;
        //                                    default:
        //                                        text_alignment = Spire.Pdf.Graphics.PdfTextAlignment.Left;
        //                                        break;
        //                                }
        //                                #endregion

        //                                if (itemMT.Page == i)
        //                                {
        //                                    basePageWriter.Canvas.DrawString(text, bf, brush, itemLLX, itemLLY, new Spire.Pdf.Graphics.PdfStringFormat(text_alignment));
        //                                }
        //                            }
        //                            #endregion
        //                        }

        //                        MemoryStream memStream = new MemoryStream();
        //                        pdfWriter.SaveToStream(memStream);

        //                        Log.Information("Ket thuc Fill Text vao PDF");
        //                        timer.Stop();
        //                        Log.Information(timer.ElapsedMilliseconds.ToString());

        //                        #endregion

        //                        #region Send File to MinIO
        //                        var fileName = doc.FileNamePrefix + ".pdf";
        //                        try
        //                        {
        //                            // byte[] pdfBytes = streamWriter.ToArray();
        //                            // File.WriteAllBytes(@"C:\Users\thienbq\Desktop\Projects\eContract\econtract.api\NetCore.API\bin\Debug\netcoreapp3.1\result.pdf", pdfBytes);

        //                            // var memStream = new MemoryStream(pdfBytes);

        //                            fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

        //                            string fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(memStream);

        //                            fileName = ms.RenameFile(fileName);

        //                            var streamConvert = new MemoryStream(memStream.ToArray());
        //                            ConvertPDF.ConvertToPDFA(ref streamConvert);

        //                            MinIOFileUploadResult minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, streamConvert, false);

        //                            pdfWriter.Close();
        //                            pdfTemplate.Close();

        //                            var dFile = new DocumentFile()
        //                            {
        //                                Id = Guid.NewGuid(),
        //                                DocumentId = doc.Id,
        //                                FileName = Path.GetFileName(fileName),
        //                                FileBucketName = minioRS.BucketName,
        //                                FileObjectName = minioRS.ObjectName,
        //                                ProfileName = "",
        //                                CreatedDate = dateNow,
        //                                FileType = FILE_TYPE.PDF,
        //                                DocumentFileTemplateId = file2.Id
        //                            };

        //                            await _dataContext.DocumentFile.AddAsync(dFile);

        //                            listDocumentFileConvertToPNG.Add(new PDFConvertPNGCallbackServiceModel()
        //                            {
        //                                DocumentFileId = dFile.Id,
        //                                StreamData = streamConvert,
        //                                FileBase64 = fileBase64
        //                            });

        //                            streamConvert.Close();
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Log.Error(ex, $"{systemLog.TraceId}");
        //                            // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
        //                            // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
        //                            return new ResponseError(Code.ServerError, $"Không thể upload file {fileName} lên server");
        //                        }

        //                        #endregion
        //                    }
        //                }
        //            }
        //            #endregion

        //            listDocRS.Add(new DocumentCreateManyResponseTempModel()
        //            {
        //                DocumentId = doc.Id,
        //                DocumentCode = doc.Code,
        //                Document3rdId = doc.Document3rdId,
        //                NextStepUserId = doc.NextStepUserId,
        //                NextStepUserName = doc.NextStepUserName,
        //                NextStepUserEmail = doc.NextStepUserEmail,
        //                NextStepUserPhoneNumber = doc.NextStepUserPhoneNumber,
        //                NextStepUserFullName = nextStepUser.UserFullName,
        //                OneTimePassCode = doc.OneTimePassCode,
        //                State = nextStepUser.State,
        //                StateName = nextStepUser.StateName,
        //                SignExpireAtDate = doc.SignExpireAtDate
        //            });
        //        }

        //        int dbSave = await _dataContext.SaveChangesAsync();

        //        if (dbSave > 0)
        //        {
        //            Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(docBatch));
        //        }
        //        else
        //        {
        //            Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
        //            return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
        //        }

        //        #region Convert hợp đồng sang file preview
        //        PDFToImageService pdfToImageService = new PDFToImageService();
        //        foreach (var item in listDocumentFileConvertToPNG)
        //        {
        //            _ = pdfToImageService.ConvertPDFBase64ToPNGCallBack(item, systemLog).ConfigureAwait(false);
        //        }
        //        #endregion

        //        var listAccessLink = new List<ResponseAccessLinkModel>();

        //        if (_orgConf.IsUseUI)
        //        {
        //            #region Lấy ra đơn vị gốc
        //            OrganizationModel orgRootModel = new OrganizationModel();
        //            var rootOrg = await _organizationHandler.GetRootByChidId(model.OrganizationId);
        //            if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
        //            {
        //                orgRootModel = orgRoot.Data;
        //            }
        //            #endregion
        //            foreach (var item in listDocRS)
        //            {
        //                string url = null;
        //                #region Gửi thông báo cho người dùng
        //                if (model.IsPos)
        //                {
        //                    //Tạo URL
        //                    url = $"{Utils.GetConfig("Web:SignPageUrl:uri")}{item.DocumentCode}?email={item.NextStepUserEmail}&otp={item.OneTimePassCode}";
        //                }
        //                else
        //                {
        //                    //Tạo URL
        //                    url = $"{Utils.GetConfig("Web:SignPageUrl:uri")}{item.DocumentCode}";

        //                    #region Gửi thông báo qua gateway
        //                    _ = _notifyHandler.SendNotificationRemindSignDocumentByGateway(new NotificationRemindSignDocumentModel()
        //                    {
        //                        TraceId = systemLog.TraceId,
        //                        OraganizationCode = orgRootModel.Code,
        //                        User = new NotifyUserModel()
        //                        {
        //                            UserName = item.NextStepUserName,
        //                            PhoneNumber = item.NextStepUserPhoneNumber,
        //                            Email = item.NextStepUserEmail,
        //                            FullName = item.NextStepUserFullName
        //                        },
        //                        Document = new GatewayNotifyDocumentModel()
        //                        {
        //                            Code = item.DocumentCode,
        //                            Name = item.DocumentName,
        //                            OneTimePassCode = item.OneTimePassCode,
        //                            Url = url,
        //                        }
        //                    }, systemLog).ConfigureAwait(false);
        //                    #endregion
        //                }
        //                #endregion

        //                listAccessLink.Add(new ResponseAccessLinkModel()
        //                {
        //                    Document3rdId = item.Document3rdId,
        //                    DocumentCode = item.DocumentCode,
        //                    Url = url,
        //                    State = item.State,
        //                    StateName = item.StateName,
        //                    SignExpireAtDate = item.SignExpireAtDate.HasValue ? new DateTime(item.SignExpireAtDate.Value.Ticks) : item.SignExpireAtDate
        //                }); ;
        //            }
        //        }
        //        else
        //        {
        //            listAccessLink = listDocRS.Select(x => new ResponseAccessLinkModel()
        //            {
        //                DocumentCode = x.DocumentCode,
        //                Document3rdId = x.Document3rdId,
        //                State = x.State,
        //                StateName = x.StateName,
        //                SignExpireAtDate = x.SignExpireAtDate.HasValue ? new DateTime(x.SignExpireAtDate.Value.Ticks) : x.SignExpireAtDate
        //            }).ToList();
        //        }

        //        var rs = new CreatDocument3rdResponseModel()
        //        {
        //            DocumentBatch3rdId = docBatch.DocumentBatch3rdId,
        //            DocumentBatchCode = docBatch.Code,
        //            ListDocument = listAccessLink
        //        };

        //        return new ResponseObject<CreatDocument3rdResponseModel>(rs, MessageConstants.CreateSuccessMessage, Code.Success);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId}");
        //        return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
        //    }
        //}

        public async Task<MemoryStream> FillMetaDataToPdfWithIText7(List<MetaDataFileValue> listMetaDataValue, MemoryStream fileTemplateStream, SystemLogModel systemLog)
        {
            #region Tạo file template mới + fill text = iText 7

            MemoryStream streamWriter = new MemoryStream();

            //Dùng thư viện iText7 bổ sung text vào file pdf
            var bytes = fileTemplateStream.ToArray();
            var streamTemp = new MemoryStream(bytes);

            var pdfReaderTemplate = new iText.Kernel.Pdf.PdfReader(streamTemp);
            iText.Kernel.Pdf.PdfDocument pdfTemplate = new iText.Kernel.Pdf.PdfDocument(pdfReaderTemplate);

            var pdfWriterStream = new iText.Kernel.Pdf.PdfWriter(streamWriter);
            iText.Kernel.Pdf.PdfDocument pdfWriter = new iText.Kernel.Pdf.PdfDocument(pdfWriterStream);

            int numberOfPages = pdfTemplate.GetNumberOfPages();

            for (var i = 1; i <= numberOfPages; i++)
            {
                // thông tin template
                iText.Kernel.Pdf.PdfPage pageTemplate = pdfTemplate.GetPage(i);
                iText.Kernel.Geom.Rectangle recTemplate = pageTemplate.GetPageSizeWithRotation();

                iText.Kernel.Pdf.PdfPage page = pdfTemplate.GetPage(i);
                var pagesize = page.GetPageSize();

                // thêm mới page cho pdf được tạo
                iText.Kernel.Pdf.PdfPage pageWriter = pdfWriter.AddNewPage(new iText.Kernel.Geom.PageSize(pagesize.GetWidth(), pagesize.GetHeight()));

                // lấy canvas của pdf được tạo mới để chỉnh sửa
                iText.Kernel.Pdf.Canvas.PdfCanvas pdfCanvasWriter = new iText.Kernel.Pdf.Canvas.PdfCanvas(pageWriter);
                iText.Layout.Canvas canvasWriter = new iText.Layout.Canvas(pdfCanvasWriter, recTemplate);

                // copy nội dung pdf từng page của template sang pdf được tạo mới
                iText.Kernel.Pdf.Xobject.PdfFormXObject pageCopy = pageTemplate.CopyAsFormXObject(pdfWriter);
                pdfCanvasWriter.AddXObjectAt(pageCopy, 0, 0);

                #region Processing PDF iText7
                string text = "";

                foreach (var itemMT in listMetaDataValue)
                {
                    #region Set font
                    iText.Kernel.Font.PdfFont bf;
                    var fontName = string.Empty;
                    try
                    {
                        // Font Style 
                        var fontStyle = string.Empty;
                        if (itemMT.FontWeight == "bold")
                            fontStyle += "_BOLD";
                        else if (itemMT.FontWeight == "light")
                            fontStyle += "_LIGHT";

                        if (itemMT.FontStyle == "italic") fontStyle += "_ITALIC";
                        // if (item.TextDecoration == "underline") fontStyle += "_UNDERLINE";

                        switch (itemMT.Font)
                        {
                            case "Arial":
                                fontName += "ARIAL" + fontStyle + ".ttf";
                                break;
                            case "Calibri":
                                fontName += "CALIBRI" + fontStyle + ".ttf";
                                break;
                            case "Helvetica":
                                fontName += "HELVETICA" + fontStyle + ".ttf";
                                break;
                            case "Tahoma":
                                fontName += "TAHOMA" + fontStyle + ".ttf";
                                break;
                            case "Times New Roman":
                                fontName += "TIMES" + fontStyle + ".ttf";
                                break;
                            case "Roboto":
                                fontName += "ROBOTO" + fontStyle + ".ttf";
                                break;
                            default:
                                fontName += "ARIAL" + fontStyle + ".ttf";
                                break;
                        }

                        byte[] fontBinary = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/" + fontName));

                        bf = iText.Kernel.Font.PdfFontFactory.CreateFont(fontBinary, iText.IO.Font.PdfEncodings.IDENTITY_H);
                    }
                    catch (Exception ex)
                    {
                        pdfWriterStream.Close();
                        pdfWriter.Close();
                        pdfReaderTemplate.Close();
                        pdfTemplate.Close();
                        streamTemp.Close();

                        Log.Error(ex, $"{systemLog.TraceId} - " + "Font " + fontName + " hiện tại chưa được hỗ trợ ở phiên bản này");
                        // systemLog.Description = $"Có lỗi xảy ra khi tạo file {item.DocumentCode} - {ex.Message}";
                        // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                        // return new ResponseObject<bool>(false, "Font " + fontName + " hiện tại chưa được hỗ trợ ở phiên bản này", Code.ServerError);
                        throw new Exception("Font " + fontName + " hiện tại chưa được hỗ trợ ở phiên bản này");
                    }

                    #endregion

                    #region Gạch chân và màu sắc
                    // Lấy màu sắc từ mã hex
                    var sysDrawingColor = Utils.HexToColor(itemMT.Color ?? string.Empty);
                    var textColor = new iText.Kernel.Colors.DeviceRgb(sysDrawingColor);

                    iText.Layout.Style bfStyle = new iText.Layout.Style();
                    bfStyle.SetFont(bf);
                    bfStyle.SetFontSize(itemMT.FontSize);
                    bfStyle.SetFontColor(textColor);
                    if (itemMT.TextDecoration == "underline") bfStyle.SetUnderline();
                    // if (itemMT.FontWeight == "bold") bfStyle.SetBold();
                    // if (itemMT.FontStyle == "italic") bfStyle.SetItalic();
                    #endregion

                    #region Calculate coordinate
                    // put the alignment and coordinates here
                    text = itemMT.MetaDataValue ?? "";

                    // Tính kích thước trên iTextSharp
                    var pageHeight = pagesize.GetHeight(); // Chiều cao PDF
                    var pageWidth = pagesize.GetWidth(); // Chiều rộng PDF

                    // Tỷ lệ trên Frontend
                    var temp = (float)pageHeight / (float)itemMT.PageHeight;

                    var itemHeight = (float)itemMT.Height * temp; // Độ cao vùng ký
                    var itemWidth = (float)itemMT.Width * temp;  // Độ rộng vùng ký
                    // 9 là từ giao diện margin
                    // 3 là do iText7 bị lệch
                    var itemLLY = (float)(0 - itemMT.LLY - 9) * temp - itemHeight - 3;  // Tọa độ LLY 
                    var itemLLX = (float)(itemMT.LLX - 9) * temp; // Tọa độ LLX

                    #endregion

                    #region Alignment + Recalculate coordinate
                    iText.Layout.Properties.TextAlignment text_alignment = iText.Layout.Properties.TextAlignment.JUSTIFIED;

                    switch (itemMT.TextAlign)
                    {
                        case "left":
                            text_alignment = iText.Layout.Properties.TextAlignment.LEFT;
                            break;
                        case "center":
                            text_alignment = iText.Layout.Properties.TextAlignment.CENTER;
                            itemLLX += (float)itemWidth / 2;
                            break;
                        case "right":
                            text_alignment = iText.Layout.Properties.TextAlignment.RIGHT;
                            itemLLX += (float)itemWidth;
                            break;
                        default:
                            text_alignment = iText.Layout.Properties.TextAlignment.LEFT;
                            break;
                    }
                    #endregion

                    if (itemMT.Page == i)
                    {
                        iText.Layout.Element.Paragraph p = new iText.Layout.Element.Paragraph();
                        p.Add(new iText.Layout.Element.Text(text).AddStyle(bfStyle));
                        canvasWriter.ShowTextAligned(p, itemLLX, itemLLY, text_alignment);
                    }
                }
                #endregion
                canvasWriter.Close();
            }

            pdfWriter.Close();
            pdfReaderTemplate.Close();
            pdfTemplate.Close();
            streamTemp.Close();
            #endregion

            return streamWriter;
        }

        public async Task<List<FileTemplateStreamModel>> GetFileTemplateDocument(
            List<DocumentTemplate> listDocumentTemplate,
            List<DocumentFileTemplate> listFileTemplate,
            SystemLogModel systemLog
            )
        {
            var listFileTemplateStream = new List<FileTemplateStreamModel>();
            var ms = new MinIOService();

            //Tải danh sách file thuộc document template về lưu trữ
            foreach (var template in listDocumentTemplate)
            {
                //Lấy ra danh sách file thuộc biểu mẫu
                var fileTemplateOfDocTemp = await _dataContext.DocumentFileTemplate.OrderBy(x => x.Order)
                        .Where(x => x.DocumentTemplateId == template.Id).ToListAsync();

                //Duyệt từng biểu mẫu và thêm vào danh sách template document
                foreach (var fileTemp in fileTemplateOfDocTemp)
                {
                    var fileTemplateStream = new FileTemplateStreamModel();

                    // Cấu hình Meta Data trên file pdf
                    var fileMetaDataConfig = fileTemp.MetaDataConfig;

                    using (HttpClientHandler clientHandler = new HttpClientHandler())
                    {
                        // MultipartFormDataContent multiForm = new MultipartFormDataContent();

                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, cert, chain, sslPolicyErrors) => { return true; };

                        using (var client = new HttpClient(clientHandler))
                        {
                            #region Tải file template từ MinIO
                            try
                            {
                                fileTemplateStream.Id = fileTemp.Id;
                                fileTemplateStream.FileTemplateStream = await ms.DownloadObjectAsync(fileTemp.FileDataBucketName, fileTemp.FileDataObjectName);
                                fileTemplateStream.FileTemplateStream.Position = 0;

                                listFileTemplateStream.Add(fileTemplateStream);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId}");

                                // return new ResponseError(Code.ServerError, "Kiểm tra lại file đã cấu hình trong biểu mẫu: " + template.Name);
                                throw new Exception("Kiểm tra lại file đã cấu hình trong biểu mẫu: " + template.Name);
                            }
                            #endregion
                        }
                    }

                    listFileTemplate.Add(new DocumentFileTemplate()
                    {
                        Id = fileTemp.Id,
                        FileName = fileTemp.FileName,
                        FileDataBucketName = fileTemp.FileDataBucketName,
                        FileDataObjectName = fileTemp.FileDataObjectName,
                        FileType = fileTemp.FileType,
                        DocumentTemplateId = fileTemp.DocumentTemplateId,
                        MetaDataConfig = fileTemp.MetaDataConfig,
                        ProfileName = fileTemp.ProfileName
                    });
                }
            }

            return listFileTemplateStream;
        }

        public async Task<Response> GetListDocumentByUserConnectId(DocumentRequestByUserConnectIdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Get list document by user connect id from 3rd: " + JsonSerializer.Serialize(model));

                if (model == null || model.UserConnectId == null)
                {
                    Log.Information($"{systemLog.TraceId} - Thông tin người dùng đang trống");
                    return new ResponseError(Code.ServerError, $"Thông tin người dùng không được để trống.");
                }

                var orgConnectModel = new OrganizationModel();
                var orgConnect = await _organizationHandler.GetById(new Guid(systemLog.OrganizationId));
                if (orgConnect.Code == Code.Success && orgConnect is ResponseObject<OrganizationModel> orgConnectModelTemp)
                {
                    orgConnectModel = orgConnectModelTemp.Data;
                }

                var orgRootModel = new OrganizationModel();
                var orgRootOfOrgConnect = await _organizationHandler.GetRootByChidId(orgConnectModel.Id);
                if (orgRootOfOrgConnect.Code == Code.Success && orgRootOfOrgConnect is ResponseObject<OrganizationModel> orgRootOfConnectTemp)
                {
                    orgRootModel = orgRootOfConnectTemp.Data;
                }

                var orgsRelationGuid = new List<Guid>();
                var orgsRelationId = _organizationHandler.GetListChildOrgByParentID(orgRootModel.Id);

                var users = await _dataContext.User.Where(x => x.ConnectId == model.UserConnectId && (x.OrganizationId.HasValue && orgsRelationId.Contains(x.OrganizationId.Value))).Select(x => x.Id).ToListAsync();

                if (users == null || users.Count <= 0)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy người dùng");
                    return new ResponseError(Code.ServerError, $"Không tìm thấy người dùng với ID đang yêu cầu.");
                }
                else
                {
                    var listDocument = await
                        (
                            from doc in _dataContext.Document.Where(x => x.UserId.HasValue && users.Contains(x.UserId.Value) && !x.IsDeleted)
                            join docType in _dataContext.DocumentType on doc.DocumentTypeId equals docType.Id
                            orderby doc.CreatedDate descending
                            select new DocumentRequestByUserConnectIdResonseModel()
                            {
                                DocumentTypeCode = docType.Code,
                                DocumentTypeName = docType.Name,
                                DocumentCode = doc.Code,
                                DocumentName = doc.Name,
                                State = doc.State,
                                DocumentStatus = doc.DocumentStatus,
                                CreadtedDate = doc.CreatedDate,
                            }).ToListAsync();

                    Log.Information($"{systemLog.TraceId} - Lấy danh sách hợp đồng theo người dùng từ đơn vị thứ 3 - {JsonSerializer.Serialize(listDocument)}");
                    return new ResponseObject<List<DocumentRequestByUserConnectIdResonseModel>>(listDocument, "Lấy danh sách hợp đồng thành công", Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateEFormFrom3rd_v2(CreateEFormFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Bổ sung eForm cho tài khoản người dùng: " + JsonSerializer.Serialize(model));

                User userInfo;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    userInfo = await _dataContext.User
                       .Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (userInfo == null)
                    {
                        return new ResponseError(Code.ServerError, $"Không tim thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Kết nối từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        return new ResponseError(Code.ServerError, "Thông tin người dùng không được để trống");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    userInfo = await _dataContext.User
                        .Where(x => !string.IsNullOrEmpty(x.ConnectId)
                        && model.UserConnectId.ToLower() == x.ConnectId.ToLower()
                        && x.OrganizationId.HasValue
                        && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();

                    if (userInfo == null)
                    {
                        return new ResponseError(Code.ServerError, $"Không tim thấy người dùng với mã đang kết nối - {model.UserConnectId}");
                    }
                }


                // Lấy thông tin cấu hình của đơn vị
                if (!userInfo.OrganizationId.HasValue)
                {
                    return new ResponseError(Code.ServerError, $"Tài khoản chưa có thông tin đơn vị");
                }

                // Lấy đơn vị gốc
                var rootOrgInfo = await _organizationHandler.GetRootOrgModelByChidId(userInfo.OrganizationId.Value);

                if (_orgConf == null)
                {
                    _orgConf = await _organizationConfigHandler.InternalGetByOrgId(rootOrgInfo.Id);
                }
                if (_orgConf == null)
                {
                    return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối");
                }

                var ms = new MinIOService();
                DocumentType documentType;
                string consent = "";
                //Kiểm tra thông tin loại hợp đồng
                if (userInfo.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS);
                    consent = Utils.GetConfig("eForm:YeuCauCapCTSConsent");
                }
                else
                {
                    documentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                     .FirstOrDefaultAsync(x => x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU);
                    consent = Utils.GetConfig("eForm:KyDienTuConsent");
                }

                // Kiểm tra người dùng đã tạo eForm hay chưa?
                var docCheck = await _dataContext.Document
                    .Where(x => x.DocumentTypeId == documentType.Id && x.UserId == userInfo.Id && (x.DocumentStatus == DocumentStatus.PROCESSING || x.DocumentStatus == DocumentStatus.FINISH))
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();

                //TODO: Kiểm tra xem người dùng có thay đổi thông tin không??? Trường hợp chưa ký mà thay đổi thông tin thì sẽ tạo eForm mới
                // Hợp đồng có tồn tại
                if (docCheck != null)
                {
                    var docFile = await _dataContext.DocumentFile.Where(x => x.DocumentId == docCheck.Id).FirstOrDefaultAsync();

                    var rsTemp = new CreateEFormFrom3rdResponseModel()
                    {
                        EFormType = documentType.Code,
                        DocumentCode = docCheck.Code,
                        DocumentStatus = docCheck.DocumentStatus,
                        FilePreviewUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName),
                        Consent = consent
                    };

                    if (docFile.ImagePreview != null)
                    {
                        rsTemp.ListImagePreview = new List<string>();
                        foreach (var item in docFile.ImagePreview)
                        {
                            rsTemp.ListImagePreview.Add(await ms.GetObjectPresignUrlAsync(item.BucketName, item.ObjectName));
                        }
                    }

                    if (docCheck.DocumentStatus == DocumentStatus.FINISH)
                    {
                        rsTemp.IdentifierDevice = _dataContext.UserMapDevice.Where(x => x.UserId == userInfo.Id && x.IsIdentifierDevice).OrderByDescending(x => x.CreatedDate).FirstOrDefault()?.DeviceId;

                        return new ResponseObject<CreateEFormFrom3rdResponseModel>(rsTemp, "Người dùng đã ký thành công eForm", Code.Success);
                    }
                    else
                    {
                        return new ResponseObject<CreateEFormFrom3rdResponseModel>(rsTemp, "Thêm mới eForm thành công", Code.Success);
                    }
                }

                // Nếu chưa tạo thì tạo và yêu cầu ký
                // Nếu tạo rồi thì kiểm tra đã ký chưa
                // Đã tạo - chưa ký => trả ra chờ ký
                // Đã tạo - ký rồi => báo đã ký hợp đồng => next qua bước xem chi tiết hợp đồng

                if (documentType == null)
                {
                    return new ResponseError(Code.ServerError, "Loại hợp đồng eForm chưa được cấu hình trên hệ thống");
                }

                var docTempByDocType = await _dataContext.DocumentTemplate.FirstOrDefaultAsync(x => x.DocumentTypeId == documentType.Id);
                var checkDocumentTemplate = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);

                if (checkDocumentTemplate == null || checkDocumentTemplate.Count < 1)
                {
                    return new ResponseError(Code.ServerError, "eForm " + documentType.Name + " chưa được cấu hình biểu mẫu");
                }

                //Kiểm tra WF
                var wfDetail = await GetWorkFlowDetailByCode(EFormDocumentConstant.WF_EFORM, systemLog);
                if (wfDetail == null)
                {
                    return new ResponseError(Code.NotFound, "Quy trình ký eForm chưa được cấu hình");
                }

                //maping WorkFlowUser

                //Danh sách người ký được cấu hình theo quy trình
                var listUserWF = wfDetail.ListUser;
                if (listUserWF == null)
                {
                    return new ResponseError(Code.ServerError, $"WorkFlow chưa được cấu hình");
                }

                List<WorkFlowUserDocumentModel> listUser = new List<WorkFlowUserDocumentModel>();

                foreach (var item in listUserWF)
                {
                    listUser.Add(AutoMapperUtils.AutoMap<WorkflowUserModel, WorkFlowUserDocumentModel>(item));
                }

                //Lấy thông tin Org and User
                var listUserConnect = new List<string>();
                //listUserConnect.Add(model.UserConnectId);

                var orgAndUser = new OrgAndUserConnectInfoRequestModel()
                {
                    OrganizationId = new Guid(systemLog.OrganizationId),
                    ListUserConnectId = listUserConnect
                };

                // Lấy thông tin người dùng theo connectId từ User service
                var ordAndUserResponse = await GetOrgAndUserConnectInfo(orgAndUser, systemLog);
                var orgInfo = ordAndUserResponse.OrganizationInfo;

                DocumentBatch docBatch = null;

                var countDocumentBatch = await _dataContext.DocumentBatch.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;
                var documentBatchCode = Utils.GenerateAutoCode(documentType.Code + ".", countDocumentBatch) + Utils.GenerateNewRandom();

                //Thêm mới lô hợp đồng
                docBatch = new DocumentBatch()
                {
                    Id = Guid.NewGuid(),
                    Code = documentBatchCode,
                    Name = documentBatchCode,
                    Status = true,
                    CreatedDate = dateNow,
                    WorkflowId = wfDetail.Id,
                    Order = countDocumentBatch,
                    DocumentTypeId = documentType.Id,
                    CreatedUserId = orgInfo.UserId,
                    OrganizationId = orgInfo.Id,
                    IsGenerateFile = true,
                    Type = 1
                };
                await _dataContext.DocumentBatch.AddAsync(docBatch);

                systemLog.ListAction.Add(new ActionDetail()
                {
                    Description = $"Thêm mới lô hợp đồng {documentType.Code} sinh hợp đồng chấp thuận yêu cầu sử dụng CTS/ký điện tử an toàn",
                    ObjectCode = CacheConstants.DOCUMENT_BATCH,
                    ObjectId = docBatch.Id.ToString(),
                    CreatedDate = DateTime.Now
                });

                //Lấy ra danh sách biểu mẫu thuộc loại hợp đồng
                var listFileTemplate = new List<DocumentFileTemplate>();
                var listImage = new List<ImagePreview>();

                // file template stream
                var docTempValids = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);
                var listDocumentTemplate = await _dataContext.DocumentTemplate.Where(x => docTempValids.Select(x1 => x1.Id).Contains(x.Id)).ToListAsync();
                var listFileStreamTemplate = await GetFileTemplateDocument(listDocumentTemplate, listFileTemplate, systemLog);

                var listDocRS = new List<DocumentCreateManyResponseTempModel>();
                var count = await _dataContext.Document.Where(x => x.DocumentTypeId == documentType.Id).CountAsync() + 1;

                // Lấy danh sách người dùng trong quy trình
                var workFlowUser = new List<UserConnectInfoModel>();

                workFlowUser.Add(new UserConnectInfoModel()
                {
                    UserId = userInfo.Id,
                    UserConnectId = userInfo.ConnectId,
                    UserEmail = userInfo.Email,
                    UserFullName = userInfo.Name,
                    UserName = userInfo.UserName,
                    UserPhoneNumber = userInfo.PhoneNumber

                });

                for (int i = 0; i < workFlowUser.Count(); i++)
                {
                    if (i >= listUser.Count())
                        break;
                    var wfi = workFlowUser[i];
                    if (wfi.UserId != null && wfi.UserId != Guid.Empty)
                    {
                        listUser[i].UserId = wfi.UserId;
                        listUser[i].UserName = wfi.UserName;
                        listUser[i].UserConnectId = wfi.UserConnectId;
                        listUser[i].UserFullName = wfi.UserFullName;
                        listUser[i].UserEmail = wfi.UserEmail;
                        listUser[i].UserPhoneNumber = wfi.UserPhoneNumber;
                    }
                }
                foreach (var u in listUser)
                {
                    if (u.UserId == null)
                    {
                        return new ResponseError(Code.MethodNotAllowed, $"Thiếu thông tin WorkFlow");
                    }
                }
                var nextStepUser = listUser[0];
                var docCode = Utils.GenerateAutoCode(documentType.Code + "-", count++) + Utils.GenerateNewRandom();

                //Tạo mới document

                //Fill thông tin từ Meta data vào file template

                var documentId = Guid.NewGuid();

                #region Sinh file pdf từ meta data
                List<MetaDataListForDocumentType> listMetaData = new List<MetaDataListForDocumentType>();

                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = MetaDataCodeConstants.DOC_ID,
                    MetaDataValue = "Doc ID#: " + documentId.ToString()
                });

                #region Thông tin cơ bản
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_HOTEN",
                    MetaDataValue = userInfo.Name
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_NGAYSINH",
                    MetaDataValue = userInfo.Birthday.HasValue ? userInfo.Birthday.Value.ToString("dd/MM/yyyy") : ""
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_CMTND",
                    MetaDataValue = userInfo.IdentityNumber
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_NGAYCAP",
                    MetaDataValue = userInfo.IssueDate.HasValue ? userInfo.IssueDate.Value.ToString("dd/MM/yyyy") : ""
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_NOICAP",
                    MetaDataValue = userInfo.IssueBy
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_DIENTHOAI",
                    MetaDataValue = userInfo.PhoneNumber
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_EMAIL",
                    MetaDataValue = userInfo.Email
                });
                listMetaData.Add(new MetaDataListForDocumentType()
                {
                    MetaDataCode = "TMP_DCTT",
                    MetaDataValue = userInfo.Address
                });
                #endregion

                #region Thông tin nâng cao
                if (userInfo.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_THSD",
                        MetaDataValue = Utils.GetConfig("eForm:TimeValidCert")
                    });
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_CN_CTS",
                        MetaDataValue = userInfo.Name
                    });
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_CMND_CTS",
                        MetaDataValue = userInfo.IdentityNumber
                    });
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_MBN_CTS",
                        MetaDataValue = userInfo.PhoneNumber
                    });
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_C_CTS",
                        MetaDataValue = userInfo.CountryName
                    });
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_S_CTS",
                        MetaDataValue = userInfo.ProvinceName
                    });
                }
                else
                {
                    listMetaData.Add(new MetaDataListForDocumentType()
                    {
                        MetaDataCode = "TMP_MDSD",
                        MetaDataValue = Utils.GetConfig("eForm:Uses")
                    });
                }

                #endregion

                List<DocumentMetaData> metaData = new List<DocumentMetaData>();
                if (listMetaData != null && listMetaData.Count > 0)
                {
                    metaData = listMetaData.Select(x => new DocumentMetaData()
                    {
                        Key = x.MetaDataCode,
                        Value = x.MetaDataValue
                    }).ToList();
                }

                var doc = new Data.Document()
                {
                    Id = documentId,
                    Code = docCode,
                    UserId = userInfo.Id,
                    Email = userInfo.Email,
                    FullName = userInfo.Name,
                    PhoneNumber = userInfo.PhoneNumber,
                    Name = documentType.Name + " - " + userInfo.Name,
                    Status = true,
                    DocumentBatchId = docBatch.Id,
                    CreatedDate = dateNow,
                    WorkflowId = wfDetail.Id,
                    Order = count,
                    DocumentTypeId = documentType.Id,
                    CreatedUserId = orgInfo.UserId,
                    OrganizationId = orgInfo.Id,
                    DocumentStatus = DocumentStatus.PROCESSING,
                    NextStepId = nextStepUser.Id,
                    NextStepUserId = nextStepUser.UserId,
                    NextStepUserName = nextStepUser.UserName,
                    NextStepUserEmail = nextStepUser.UserEmail,
                    StateId = nextStepUser.StateId,
                    State = nextStepUser.State,
                    NextStepUserPhoneNumber = nextStepUser.UserPhoneNumber,
                    NextStepSignType = nextStepUser.Type,
                    WorkflowStartDate = dateNow,
                    WorkFlowUser = listUser,
                    MetaData = metaData,
                    BucketName = rootOrgInfo.Code,
                    ObjectNameDirectory = $"{orgInfo.Code}/{dateNow.Year}/{dateNow.Month}/{dateNow.Day}/{docCode}/",
                    FileNamePrefix = $"{orgInfo.Code}.{userInfo.UserName}.{docCode}"
                };

                await _dataContext.Document.AddAsync(doc);

                if (userInfo.UserEFormInfo == null)
                {
                    userInfo.UserEFormInfo = new UserEFormModel();
                }
                //Kiểm tra thông tin loại hợp đồng => update eform tương ứng vào thông tin người dùng
                if (userInfo.EFormConfig == EFormConfigEnum.KY_CTS_CA_NHAN)
                {
                    userInfo.UserEFormInfo.RequestCertificateDocumentId = documentId;
                }
                else
                {
                    userInfo.UserEFormInfo.ConfirmDigitalSignatureDocumentId = documentId;
                }
                _dataContext.User.Update(userInfo);

                systemLog.ListAction.Add(new ActionDetail()
                {
                    Description = $"Thêm mới eForm {doc.Code}",
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = doc.Id.ToString(),
                    CreatedDate = dateNow
                });

                MinIOFileUploadResult minioRS = new MinIOFileUploadResult();

                foreach (var template in listDocumentTemplate)
                {
                    //Lấy ra danh sách file thuộc biểu mẫu
                    var fileTemplate = listFileTemplate.Where(x => x.DocumentTemplateId == template.Id).ToList();

                    //Duyệt từng biểu mẫu và thêm vào danh sách template document
                    foreach (var file2 in fileTemplate)
                    {
                        // lấy ra templateStream
                        var fileTemplateStreamModel = listFileStreamTemplate.FirstOrDefault(x => x.Id == file2.Id);

                        //Kiểm tra định dạng file
                        // File DOCX
                        if (file2.FileType == TemplateFileType.DOCX)
                        {
                            #region Xử lý dữ liệu
                            var listMetaDataValue = new List<MetaDataFileValue>();
                            foreach (var config in listMetaData)
                            {
                                listMetaDataValue.Add(new MetaDataFileValue()
                                {
                                    //MetaDataId = config.MetaDataId,
                                    MetaDataValue = config.MetaDataValue,
                                    MetaDataCode = config.MetaDataCode,
                                    MetaDataName = config.MetaDataName,
                                });
                            }
                            #endregion

                            #region Gọi service thứ 3
                            var listData = listMetaDataValue.Select(x => new KeyValueModel()
                            {
                                Key = $"{DocumentTemplateConstants.KeyPrefix}{x.MetaDataCode}{DocumentTemplateConstants.KeySubfix}",
                                Value = x.MetaDataValue
                            }).ToList();

                            var fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(fileTemplateStreamModel.FileTemplateStream);

                            var fileNamePreview = "";
                            fileNamePreview = doc.FileNamePrefix + ".pdf";
                            fileNamePreview = Utils.GetValidFileName(fileNamePreview);

                            var dt = await ConvertPDF.ConvertDocxMetaDataToPDFAsync(new FileBase64Model()
                            {
                                FileName = fileNamePreview,
                                ListData = listData,
                                FileBase64 = fileBase64
                            });

                            if (dt.Code != Code.Success)
                            {
                                Log.Error($"{systemLog.TraceId} - Convert file docx and meta data to PDFA fail!");

                                return new ResponseError(Code.ServerError, dt.Message);
                            }

                            #endregion

                            #region Send File to MinIO
                            var fileName = doc.FileNamePrefix + ".pdf";
                            try
                            {
                                MemoryStream memStream = Base64Convert.ConvertBase64ToMemoryStream(dt.FileBase64);

                                fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                                fileName = ms.RenameFile(fileName);

                                minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, memStream, false);

                                var dFile = new DocumentFile()
                                {
                                    Id = Guid.NewGuid(),
                                    DocumentId = doc.Id,
                                    FileName = Path.GetFileName(fileName),
                                    FileBucketName = minioRS.BucketName,
                                    FileObjectName = minioRS.ObjectName,
                                    ProfileName = "",
                                    CreatedDate = dateNow,
                                    FileType = FILE_TYPE.PDF,
                                    DocumentFileTemplateId = file2.Id
                                };

                                #region Sinh file preview
                                // Kiểm tra xem đơn vị có sử dụng hình ảnh preview hay không
                                if (_orgConf.UseImagePreview)
                                {
                                    PDFToImageService pdfToImageService = new PDFToImageService();

                                    var pdf2img = await pdfToImageService.ConvertPDFBase64ToPNG(new PDFConvertPNGServiceModel()
                                    {
                                        FileBase64 = dt.FileBase64
                                    }, systemLog);

                                    //Convert và tải file lên minio
                                    byte[] bytes;
                                    MemoryStream memory;
                                    int i = 0;
                                    foreach (var imgItem in pdf2img)
                                    {
                                        i++;
                                        bytes = Convert.FromBase64String(imgItem);
                                        memory = new MemoryStream(bytes);
                                        var eformUploadPNG = await ms.UploadDocumentAsync(dFile.FileBucketName, dFile.FileObjectName.Replace(".pdf", "").Replace(".PDF", "") + $"_page{i}.png", memory, false);
                                        listImage.Add(new ImagePreview()
                                        {
                                            BucketName = eformUploadPNG.BucketName,
                                            ObjectName = eformUploadPNG.ObjectName
                                        });
                                    }

                                    dFile.ImagePreview = listImage;
                                }
                                #endregion

                                await _dataContext.DocumentFile.AddAsync(dFile);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId}");
                                // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
                                // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                                return new ResponseError(Code.ServerError, $"Không thể upload file {fileName} lên server");
                            }
                            #endregion
                        }
                        // Mặc định là file PDF
                        else
                        {
                            #region Xử lý dữ liệu
                            // Cấu hình Meta Data trên file pdf
                            var fileMetaDataConfig = file2.MetaDataConfig;

                            // Giá trị của Meta data bên tên
                            var listMetaDataValue = new List<MetaDataFileValue>();

                            foreach (var config in fileMetaDataConfig)
                            {
                                var meta = listMetaData.Find(c => c.MetaDataCode == config.MetaDataCode);

                                if (meta != null)
                                {
                                    listMetaDataValue.Add(new MetaDataFileValue()
                                    {
                                        MetaDataId = config.MetaDataId,
                                        MetaDataValue = meta.MetaDataValue,
                                        MetaDataCode = meta.MetaDataCode,
                                        MetaDataName = meta.MetaDataName,
                                        Page = config.Page,
                                        TextAlign = config.TextAlign,
                                        TextDecoration = config.TextDecoration,
                                        Font = config.Font,
                                        FontStyle = config.FontStyle,
                                        FontSize = config.FontSize,
                                        FontWeight = config.FontWeight,
                                        Color = config.Color,
                                        LLX = config.LLX,
                                        LLY = config.LLY,
                                        PageHeight = config.PageHeight,
                                        PageWidth = config.PageWidth,
                                        Height = config.Height,
                                        Width = config.Width,
                                        BorderWidthOfPage = config.BorderWidthOfPage,
                                    });
                                }

                            }
                            #endregion

                            var streamWriter = await FillMetaDataToPdfWithIText7(listMetaDataValue, fileTemplateStreamModel.FileTemplateStream, systemLog);

                            #region Send File to MinIO
                            var fileName = doc.FileNamePrefix + ".pdf";
                            try
                            {
                                byte[] pdfBytes = streamWriter.ToArray();

                                fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                                fileName = ms.RenameFile(fileName);

                                var streamConvert = new MemoryStream(pdfBytes);
                                ConvertPDF.ConvertToPDFA(ref streamConvert);

                                string fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(streamConvert);

                                minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, streamConvert, false);

                                var dFile = new DocumentFile()
                                {
                                    Id = Guid.NewGuid(),
                                    DocumentId = doc.Id,
                                    FileName = Path.GetFileName(fileName),
                                    FileBucketName = minioRS.BucketName,
                                    FileObjectName = minioRS.ObjectName,
                                    ProfileName = "",
                                    CreatedDate = dateNow,
                                    FileType = FILE_TYPE.PDF,
                                    DocumentFileTemplateId = file2.Id
                                };

                                #region Convert pdf to image
                                // Kiểm tra xem đơn vị có sử dụng hình ảnh preview hay không
                                if (_orgConf.UseImagePreview)
                                {
                                    PDFToImageService pdfToImageService = new PDFToImageService();

                                    var pdf2img = await pdfToImageService.ConvertPDFBase64ToPNG(new PDFConvertPNGServiceModel()
                                    {
                                        FileBase64 = fileBase64
                                    }, systemLog);

                                    //Convert và tải file lên minio
                                    byte[] bytesFileUpload;
                                    MemoryStream memory;
                                    int i = 0;
                                    foreach (var pdfImg in pdf2img)
                                    {
                                        i++;
                                        bytesFileUpload = Convert.FromBase64String(pdfImg);
                                        memory = new MemoryStream(bytesFileUpload);
                                        var eformUploadPNG = await ms.UploadDocumentAsync(dFile.FileBucketName, dFile.FileObjectName.Replace(".pdf", "").Replace(".PDF", "") + $"_page{i}.png", memory, false);
                                        listImage.Add(new ImagePreview()
                                        {
                                            BucketName = eformUploadPNG.BucketName,
                                            ObjectName = eformUploadPNG.ObjectName
                                        });
                                    }

                                    dFile.ImagePreview = listImage;
                                }
                                #endregion

                                await _dataContext.DocumentFile.AddAsync(dFile);

                                streamConvert.Close();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId}");
                                // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
                                // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                                return new ResponseError(Code.ServerError, $"Không thể upload file {fileName} lên server");
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                #region Lưu lịch sử khi hợp đồng thay đổi
                await CreateDocumentWFLHistory(doc);
                #endregion


                listDocRS.Add(new DocumentCreateManyResponseTempModel()
                {
                    DocumentId = doc.Id,
                    DocumentCode = doc.Code,
                    Document3rdId = doc.Document3rdId,
                    NextStepUserId = doc.NextStepUserId,
                    NextStepUserName = doc.NextStepUserName,
                    NextStepUserEmail = doc.NextStepUserEmail,
                    NextStepUserPhoneNumber = doc.NextStepUserPhoneNumber,
                    NextStepUserFullName = nextStepUser.UserFullName
                });

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Add {CachePrefix} success: " + JsonSerializer.Serialize(docBatch));
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Save database error!");

                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }

                var fileUrl = await ms.GetObjectPresignUrlAsync(minioRS.BucketName, minioRS.ObjectName);

                var rs = new CreateEFormFrom3rdResponseModel()
                {
                    EFormType = documentType.Code,
                    DocumentCode = docCode,
                    FilePreviewUrl = fileUrl,
                    DocumentStatus = DocumentStatus.PROCESSING,
                    Consent = consent
                };
                if (listImage != null && listImage.Count > 0)
                {
                    rs.ListImagePreview = new List<string>();
                    foreach (var item in listImage)
                    {
                        rs.ListImagePreview.Add(await ms.GetObjectPresignUrlAsync(item.BucketName, item.ObjectName));
                    }
                }

                return new ResponseObject<CreateEFormFrom3rdResponseModel>(rs, MessageConstants.CreateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> RequestUrlDownloadDocumentFrom3rd(RequestUrlDownloadDocumentFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Lấy thông tin download hợp đồng theo người dùng: " + JsonSerializer.Serialize(model));

                User userInfo;

                if (model.UserId.HasValue)
                {
                    //Gọi api nội bộ
                    userInfo = await _dataContext.User.AsNoTracking()
                       .Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (userInfo == null)
                    {
                        return new ResponseError(Code.ServerError, $"Không tim thấy người dùng đang truy cập");
                    }
                }
                else
                {
                    //Gọi api từ ứng dụng bên thứ 3
                    if (string.IsNullOrEmpty(model.UserConnectId))
                    {
                        return new ResponseError(Code.ServerError, "Thông tin người dùng không được để trống");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    userInfo = await _dataContext.User.AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.ConnectId) && model.UserConnectId.ToLower() == x.ConnectId.ToLower() && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).FirstOrDefaultAsync();

                    if (userInfo == null)
                    {
                        return new ResponseError(Code.ServerError, $"Không tim thấy người dùng với mã đang kết nối - {model.UserConnectId}");
                    }
                }

                var docCheck = await _dataContext.Document
                    .Where(x => x.Code == model.DocumentCode && x.UserId == userInfo.Id)
                    .FirstOrDefaultAsync();

                if (docCheck == null)
                {
                    return new ResponseError(Code.NotFound, "Không tìm thấy thông tin hợp đồng");
                }
                else
                {
                    var docFile = await _dataContext.DocumentFile.Where(x => x.DocumentId == docCheck.Id).FirstOrDefaultAsync();
                    var minIOService = new MinIOService();
                    var fileUrl = await minIOService.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName);

                    return new ResponseObject<UrlDownloadDocumentFrom3rdResponseModel>(new UrlDownloadDocumentFrom3rdResponseModel()
                    {
                        Fileurl = fileUrl
                    }, MessageConstants.GetDataSuccessMessage, Code.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetDoumentDetailByCodeFrom3rd(GetDoumentDetailByCodeFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Lấy thông tin chi tiết hợp đồng từ người dùng: {JsonSerializer.Serialize(model)}");

                // Kiểm tra thông tin người dùng
                User userInfo;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    userInfo = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (userInfo == null)
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
                        return new ResponseError(Code.ServerError, "Thông tin người dùng không được để trống");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    userInfo = await _dataContext.User.AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.ConnectId)
                        && model.UserConnectId.ToLower() == x.ConnectId.ToLower()
                        && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)
                        //&& x.OrganizationId == new Guid(systemLog.OrganizationId)
                        ).FirstOrDefaultAsync();

                    if (userInfo == null)
                    {
                        return new ResponseError(Code.ServerError, $"Không tim thấy người dùng với mã đang kết nối - {model.UserConnectId}");
                    }
                }

                //// Lấy thông tin 2 loại hợp đồng yêu cầu chấp thuận ký số 
                //var lsDocumentType = await _dataContext.DocumentType.Where(x => x.Status == true)
                //    .Where(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS || x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU).Select(x => x.Id).ToListAsync();

                //Guid? documentTypeId = null;
                //if (!string.IsNullOrEmpty(model.DocumentTypeCode))
                //{
                //    var documentType = await _dataContext.DocumentType.Where(x => x.Code == model.DocumentTypeCode).FirstOrDefaultAsync();
                //    if (documentType == null)
                //    {
                //        Log.Error($"{systemLog.TraceId} - Không tìm thấy loại hợp đồng");
                //        return new ResponseError(Code.NotFound, $"Không tìm thấy loại hợp đồng");
                //    }
                //    documentTypeId = documentType.Id;
                //}

                //Lấy thông tin tài liệu
                var document = await _dataContext.Document
                    .Where(x => !x.IsDeleted
                        && x.DocumentTypeId.HasValue
                        && (x.UserId == userInfo.Id || x.WorkFlowUserJson.Contains(userInfo.Id.ToString()))
                        && x.Code == model.DocumentCode)
                    .FirstOrDefaultAsync();

                if (document == null)
                {
                    Log.Error($"{systemLog.TraceId} - Người dùng không có quyền truy cập hợp đồng");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin hợp đồng");
                }

                var docType = await _dataContext.DocumentType.Where(x => x.Id == document.DocumentTypeId).FirstOrDefaultAsync();

                //Lấy thông tin quy trình
                var docWFState = _dataContext.WorkflowState.FirstOrDefault(x => x.Id == document.StateId);

                //Thông tin file tài liệu
                var docFile = _dataContext.DocumentFile.FirstOrDefault(x => x.DocumentId == document.Id);
                if (docFile == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy file tài liệu {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file tài liệu");
                }
                var ms = new MinIOService();
                var fileUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName);

                var result = new DocumentInfoFrom3rdResponseModel()
                {
                    DocumentName = document.Name,
                    DocumentCode = document.Code,
                    DocumentStatus = document.DocumentStatus,
                    FilePreviewUrl = fileUrl,
                    State = docWFState?.Code,
                    StateName = docWFState?.Name,
                    SignExpireAtDate = document.SignExpireAtDate,
                    LastReasonReject = document.LastReasonReject,
                    ListMetaData = document.MetaData,
                    DocumentTypeCode = docType.Code,
                    DocumentTypeName = docType.Name,
                    IdentiNumber = userInfo.IdentityNumber,
                    UserFullName = userInfo.Name
                };

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Lấy thông tin chi tiết hợp đồng từ bên thứ 3: {document.Code}",
                    MetaData = JsonSerializer.Serialize(result)
                });

                return new ResponseObject<DocumentInfoFrom3rdResponseModel>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetLatestDocumentUser(GetLatestDocumentUserFrom3rdModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Lấy hợp đồng mới nhất từ người dùng: {JsonSerializer.Serialize(model)}");

                // Kiểm tra thông tin người dùng
                User userInfo;
                if (model.UserId.HasValue)
                {
                    //Kết nối từ mobile app
                    userInfo = await _dataContext.User.Where(x => x.Id == model.UserId).FirstOrDefaultAsync();

                    if (userInfo == null)
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
                        return new ResponseError(Code.ServerError, "Thông tin người dùng không được để trống");
                    }

                    var orgId = new Guid(systemLog.OrganizationId);

                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                    userInfo = await _dataContext.User.AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.ConnectId)
                        && model.UserConnectId.ToLower() == x.ConnectId.ToLower()
                        && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)
                        //&& x.OrganizationId == new Guid(systemLog.OrganizationId)
                        ).FirstOrDefaultAsync();

                    if (userInfo == null)
                    {
                        return new ResponseError(Code.ServerError, $"Không tim thấy người dùng với mã đang kết nối - {model.UserConnectId}");
                    }
                }

                // Lấy thông tin 2 loại hợp đồng yêu cầu chấp thuận ký số 
                var lsDocumentTypeEForm = await _dataContext.DocumentType.Where(x => x.Status == true)
                    .Where(x => x.Code == EFormDocumentConstant.YEU_CAU_CAP_CTS || x.Code == EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU).Select(x => x.Id).ToListAsync();

                Guid? documentTypeId = null;
                if (!string.IsNullOrEmpty(model.DocumentTypeCode))
                {
                    var documentType = await _dataContext.DocumentType.Where(x => x.Code == model.DocumentTypeCode).FirstOrDefaultAsync();
                    if (documentType == null)
                    {
                        Log.Error($"{systemLog.TraceId} - Không tìm thấy loại hợp đồng");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy loại hợp đồng");
                    }
                    documentTypeId = documentType.Id;
                }

                List<Guid> lsDocumentTypeId = new List<Guid>();
                if (model.ListDocumentTypeCode != null && model.ListDocumentTypeCode.Count > 0)
                {
                    lsDocumentTypeId = await _dataContext.DocumentType.Where(x => model.ListDocumentTypeCode.Contains(x.Code)).Select(x => x.Id).ToListAsync();
                    if (lsDocumentTypeId == null || lsDocumentTypeId.Count == 0)
                    {
                        Log.Error($"{systemLog.TraceId} - Không tìm thấy loại hợp đồng");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy loại hợp đồng");
                    }
                }

                List<Guid> lsWorkflowId = new List<Guid>();
                if (model.ListWorkFlowCode != null && model.ListWorkFlowCode.Count > 0)
                {
                    lsWorkflowId = await _dataContext.Workflow.Where(x => model.ListWorkFlowCode.Contains(x.Code)).Select(x => x.Id).ToListAsync();
                    if (lsWorkflowId == null || lsWorkflowId.Count == 0)
                    {
                        Log.Error($"{systemLog.TraceId} - Không tìm thấy thông tin quy trình");
                        return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin quy trình");
                    }
                }

                //Lấy thông tin tài liệu
                var document = await _dataContext.Document
                    .Where(x => !x.IsDeleted
                        && x.DocumentTypeId.HasValue
                        && x.UserId == userInfo.Id
                        && (documentTypeId == null || x.DocumentTypeId == documentTypeId)
                        && (lsDocumentTypeId.Count == 0 || lsDocumentTypeId.Contains(x.DocumentTypeId.Value))
                        && (lsWorkflowId.Count == 0 || lsWorkflowId.Contains(x.WorkflowId))
                        && !lsDocumentTypeEForm.Contains(x.DocumentTypeId.Value))
                    .OrderByDescending(x => x.CreatedDate)
                    .FirstOrDefaultAsync();
                if (document == null)
                {
                    Log.Error($"{systemLog.TraceId} - Người dùng đang không có hợp đồng");
                    return new ResponseError(Code.NotFound, $"Người dùng chưa được tạo hợp đồng");
                }

                var docType = await _dataContext.DocumentType.Where(x => x.Id == document.DocumentTypeId).FirstOrDefaultAsync();

                //Lấy thông tin quy trình
                var docWFState = _dataContext.WorkflowState.FirstOrDefault(x => x.Id == document.StateId);

                //Thông tin file tài liệu
                var docFile = _dataContext.DocumentFile.FirstOrDefault(x => x.DocumentId == document.Id);
                if (docFile == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy file tài liệu {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file tài liệu");
                }
                var ms = new MinIOService();
                var fileUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName);

                List<string> listImagePreview = new List<string>();
                if (docFile.ImagePreview != null && docFile.ImagePreview.Count > 0)
                {
                    foreach (var item in docFile.ImagePreview)
                    {
                        listImagePreview.Add(await ms.GetObjectPresignUrlAsync(item.BucketName, item.ObjectName));
                    }
                }

                var result = new DocumentInfoFrom3rdResponseModel()
                {
                    DocumentName = document.Name,
                    DocumentCode = document.Code,
                    DocumentStatus = document.DocumentStatus,
                    FilePreviewUrl = fileUrl,
                    State = docWFState?.Code,
                    StateName = docWFState?.Name,
                    SignExpireAtDate = document.SignExpireAtDate,
                    LastReasonReject = document.LastReasonReject,
                    ListMetaData = document.MetaData,
                    DocumentTypeCode = docType.Code,
                    DocumentTypeName = docType.Name,
                    IdentiNumber = userInfo.IdentityNumber,
                    UserFullName = userInfo.Name,
                    ListImagePreview = listImagePreview
                };

                if (document.StateId != null)
                {
                    var state = _dataContext.WorkflowState.AsNoTracking().FirstOrDefault(x => x.Id == document.StateId);
                    if (state != null)
                    {
                        if (document.DocumentStatus == DocumentStatus.DRAFT)
                        {
                            result.DocumentStatusName = "Đang soạn thảo";
                        }
                        if (document.DocumentStatus == DocumentStatus.PROCESSING)
                        {
                            result.DocumentStatusName = state.Name;
                        }
                        if (document.DocumentStatus == DocumentStatus.CANCEL)
                        {
                            result.DocumentStatusName = state.NameForReject;
                        }
                        if (document.DocumentStatus == DocumentStatus.FINISH)
                        {
                            result.DocumentStatusName = "Đã hoàn thành";
                        }
                    }
                }

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Lấy thông tin chi tiết hợp đồng từ bên thứ 3: {document.Code}",
                    MetaData = JsonSerializer.Serialize(result)
                });

                return new ResponseObject<DocumentInfoFrom3rdResponseModel>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetDocumentInfo(string documentCode, int fileUrlExpireSeconds, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - DocumentHandler Lấy thông tin hợp đồng từ 3rd App DocumentCode: {documentCode} với thời gian sống của file là {fileUrlExpireSeconds}");
                //1. Lấy thông tin tài liệu
                var orgId = new Guid(systemLog.OrganizationId);

                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(orgId);

                var document = await _dataContext.Document.AsNoTracking().FirstOrDefaultAsync(x => x.Code == documentCode && x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value));
                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Không tìm thấy tài liệu {documentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin tài liệu có mã {documentCode}");
                }

                if (document.IsDeleted)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã bị xóa {documentCode}");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng {documentCode} đã bị xóa");
                }

                //Kiểm tra quy trình kys
                var workflow = document.WorkFlowUser;
                //var checkSign = workflow.Any(x => x.SignAtDate != null);
                //var docStatus = document.DocumentStatus;
                //var documentWorkflowStatus = !checkSign ? DocumentStatus.DRAFT : docStatus;

                var docType = _dataContext.DocumentType.AsNoTracking().FirstOrDefault(x => x.Id == document.DocumentTypeId);

                //Thông tin file tài liệu
                var docFile = _dataContext.DocumentFile.AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefault(x => x.DocumentId == document.Id);
                if (docFile == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy file tài liệu {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file tài liệu");
                }
                var ms = new MinIOService();
                var fileUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName, fileUrlExpireSeconds);


                var result = new Document3rdDetailResponseModel()
                {
                    DocumentTypeCode = docType.Code,
                    DocumentTypeName = docType.Name,
                    State = document.State,
                    Document3rdId = document.Document3rdId,
                    DocumentName = document.Name,
                    DocumentCode = document.Code,
                    DocumentStatus = document.DocumentStatus,
                    FileUrl = fileUrl,
                    WorkFlowUser = AutoMapperUtils.AutoMap<WorkFlowUserDocumentModel, DocumentSignedWorkFlowUser>(workflow),
                    MetaData = document.MetaData
                };

                if (document.StateId != null)
                {
                    var state = _dataContext.WorkflowState.AsNoTracking().FirstOrDefault(x => x.Id == document.StateId);
                    if (state != null)
                    {
                        if (document.DocumentStatus == DocumentStatus.DRAFT)
                        {
                            result.DocumentStatusName = "Đang soạn thảo";
                        }
                        if (document.DocumentStatus == DocumentStatus.PROCESSING)
                        {
                            result.DocumentStatusName = state.Name;
                        }
                        if (document.DocumentStatus == DocumentStatus.CANCEL)
                        {
                            result.DocumentStatusName = state.NameForReject;
                        }
                        if (document.DocumentStatus == DocumentStatus.FINISH)
                        {
                            result.DocumentStatusName = "Đã hoàn thành";
                        }
                    }
                }

                systemLog.ListAction.Add(new ActionDetail()
                {
                    ObjectCode = CacheConstants.DOCUMENT,
                    ObjectId = document.Id.ToString(),
                    Description = $"Lấy thông tin hợp đồng từ bên thứ 3: {document.Code}",
                    MetaData = JsonSerializer.Serialize(result)
                });

                return new ResponseObject<Document3rdDetailResponseModel>(result, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        #endregion

        #region From web app
        public async Task<Response> GetDocumentFromWebApp(GetDocumentFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Get document from web app: " + JsonSerializer.Serialize(model));

                if (string.IsNullOrEmpty(model.DocumentCode) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.PassCode))
                {
                    return new ResponseError(Code.Forbidden, $"Thông tin truy cập không được để trống.");
                }

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && !x.IsDeleted).FirstOrDefaultAsync();

                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Cannot find document with code {model.DocumentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin hợp đồng");
                }

                if (document.OneTimePassCode != model.PassCode || document.PassCodeExpireDate == null || document.PassCodeExpireDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Mã truy cập hợp đồng không đúng {model.DocumentCode}");
                    return new ResponseError(Code.Forbidden, $"Sai thông tin truy cập, vui lòng thực hiện lại.");
                }

                if (document.DocumentStatus == DocumentStatus.CANCEL)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã từ chối ký {model.DocumentCode}");
                    return new ResponseError(Code.Forbidden, $"Bạn đã từ chối ký hợp đồng.");
                }

                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hoàn thành quy trình ký {model.DocumentCode}");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hòa thành quy trình ký.");
                }

                var user = await _dataContext.User.Where(x => x.Id == document.NextStepUserId).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Cannot find user in document {model.DocumentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin người dùng thuộc hợp đồng");
                }

                if (user.Email != model.Email)
                {
                    Log.Information($"{systemLog.TraceId} - Sai thông tin IdentityNumber");
                    return new ResponseError(Code.Forbidden, $"Sai thông tin truy cập, vui lòng thực hiện lại.");
                }

                //Thông tin file tài liệu
                var docFile = _dataContext.DocumentFile.FirstOrDefault(x => x.DocumentId == document.Id);
                if (docFile == null)
                {
                    Log.Error($"{systemLog.TraceId} - Không tìm thấy file tài liệu {document.Code}");
                    return new ResponseError(Code.NotFound, $"{MessageConstants.GetDataErrorMessage} - Không tìm thấy file tài liệu");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                systemLog.UserId = user.Id.ToString();
                systemLog.OrganizationId = document.OrganizationId?.ToString();

                #region Khởi tạo eForm
                var eform = await this.CreateEFormFrom3rd_v2(new CreateEFormFrom3rdModel()
                {
                    UserId = user.Id
                }, systemLog);

                CreateEFormFrom3rdResponseModel eformData = new CreateEFormFrom3rdResponseModel();

                if (eform.Code == Code.Success && eform is ResponseObject<CreateEFormFrom3rdResponseModel> resultData)
                {
                    eformData = resultData.Data;
                }

                #endregion

                var ms = new MinIOService();
                var fileUrl = await ms.GetObjectPresignUrlAsync(docFile.FileBucketName, docFile.FileObjectName);

                var rs = new GetDocumentFromWebAppResponseModel()
                {
                    DocumentId = document.Id,
                    UserId = user.Id,
                    FilePreviewUrl = fileUrl,
                    EFormData = eformData,
                    HashCode = GenerateToken(user.Id, document.Id)
                };
                return new ResponseObject<GetDocumentFromWebAppResponseModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {ex.Message}");
            }
        }

        public async Task<Response> ResendEmailPassCodeWebApp(GetDocumentFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {

                var document = await _dataContext.Document.Where(x => x.Code == model.DocumentCode && !x.IsDeleted).FirstOrDefaultAsync();

                if (document == null)
                {
                    Log.Information($"{systemLog.TraceId} - Cannot find document with code {model.DocumentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin hợp đồng");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                if (document.DocumentStatus == DocumentStatus.CANCEL)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã từ chối ký {model.DocumentCode}");
                    return new ResponseError(Code.Forbidden, $"Bạn đã từ chối ký hợp đồng.");
                }

                if (document.DocumentStatus == DocumentStatus.FINISH)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hoàn thành quy trình ký {model.DocumentCode}");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hòa thành quy trình ký.");
                }

                var user = await _dataContext.User.Where(x => x.Id == document.NextStepUserId).FirstOrDefaultAsync();

                if (user == null)
                {
                    Log.Information($"{systemLog.TraceId} - Cannot find user in document {model.DocumentCode}");
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin người dùng thuộc hợp đồng");
                }

                systemLog.UserId = user.Id.ToString();
                systemLog.OrganizationId = document.OrganizationId?.ToString();

                if (user.Email == model.Email)
                {
                    await this.SendMailToUserSign(document.Id, systemLog);
                }
                else
                {
                    Log.Information($"{systemLog.TraceId} - Sai thông tin Email");
                    return new ResponseError(Code.Forbidden, $"Bạn đã nhập sai Email, xin vui lòng xác nhận  thông tin đúng.");
                }

                return new ResponseObject<bool>(true, "Gửi mã truy cập thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {ex.Message}");
            }
        }

        public async Task<Response> SendOTPSignDocumentFromWebApp(ResendOTPSignDocumentFromWebAppModel model, SystemLogModel systemLog)
        {
            try
            {
                var tokenPayload = GetTokenPayloadAndValidate(model.HashCode);
                if (string.IsNullOrEmpty(tokenPayload.userId) || string.IsNullOrEmpty(tokenPayload.documentId))
                {
                    return new ResponseError(Code.NotFound, $"Token không chính xác");
                }

                var document = await _dataContext.Document.FindAsync(new Guid(tokenPayload.documentId));
                if (document == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy hợp đồng cần gửi OTP");
                }

                if (document.DocumentStatus != DocumentStatus.PROCESSING)
                {
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đang không ở trạng thái đang thực hiện");
                }

                #region Kiểm tra thời gian hết hạn ký
                if (document.SignExpireAtDate.HasValue && document.SignExpireAtDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã hết hạn ký");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã hết hạn ký.");
                }
                #endregion

                #region Kiểm tra thời gian đóng hợp đồng
                if (document.SignCloseAtDate.HasValue && document.SignCloseAtDate.Value < dateNow)
                {
                    Log.Information($"{systemLog.TraceId} - Hợp đồng đã đóng");
                    return new ResponseError(Code.Forbidden, $"Hợp đồng đã đóng.");
                }
                #endregion

                var user = await _dataContext.User.FindAsync(document.NextStepUserId);

                if (user == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin người dùng cần thực hiện ký");
                }

                systemLog.UserId = user.Id.ToString();
                systemLog.OrganizationId = document.OrganizationId?.ToString();

                var dt = await _userHandler.SendOTPAuthToUser(user.Id, systemLog);

                if (dt.Code == Code.Success)
                    return new ResponseObject<bool>(true, "Gửi thành công", Code.Success);
                else
                    return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại");
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId}", ex);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra vui lòng thực hiện lại - {ex.Message}");
            }
        }

        private string GenerateToken(Guid userId, Guid documentId)
        {
            var timeToLiveConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:TimeToLive");
            var keyConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Key");
            var issuerConfig = Utils.GetConfig("Web:SignPageUrl:Authentication:Issuer");

            var claims = new[]
            {
                new Claim(ClaimConstants.USER_ID, userId.ToString()),
                new Claim(ClaimConstants.DOCUMENT_ID, documentId.ToString())
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

        private (string userId, string documentId) GetTokenPayloadAndValidate(string token)
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
                            && validatedToken.Issuer == Utils.GetConfig("Authentication:Jwt:Issuer")
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

        public async Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List Delete: " + JsonSerializer.Serialize(listId));
                var listResult = new List<ResponeDeleteModel>();
                var name = "";
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.Document.FindAsync(item);

                    if (entity == null || entity.IsDeleted == true)
                    {
                        listResult.Add(new ResponeDeleteModel()
                        {
                            Id = item,
                            Name = name,
                            Result = false,
                            Message = MessageConstants.DeleteItemNotFoundMessage
                        });
                    }
                    else
                    {
                        name = entity.Name;
                        if (entity.DocumentStatus == DocumentStatus.DRAFT)
                        {
                            entity.IsDeleted = true;
                            _dataContext.Document.Update(entity);
                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    listResult.Add(new ResponeDeleteModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = true,
                                        Message = MessageConstants.DeleteItemSuccessMessage
                                    });

                                    systemLog.ListAction.Add(new ActionDetail
                                    {
                                        Description = $"Xóa hợp đồng có mã: {entity.Code}",
                                        ObjectCode = entity.Code,
                                        ObjectId = entity.Id.ToString(),
                                        CreatedDate = dateNow
                                    });
                                }
                                else
                                {
                                    // systemLog.Description += entity.Code + " (thất bại); ";
                                    listResult.Add(new ResponeDeleteModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = false,
                                        Message = MessageConstants.DeleteItemErrorMessage
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                // systemLog.Description += entity.Code + " (thất bại); ";
                                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = item,
                                    Name = name,
                                    Result = false,
                                    Message = ex.Message
                                });
                            }
                        }
                        else
                        {
                            // systemLog.Description += entity.Code + " (thất bại - trạng thái không phải ở đang soạn thảo); ";
                            listResult.Add(new ResponeDeleteModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = "Chỉ có thể xóa khi hợp đồng đang ở trạng thái soạn thảo."
                            });
                        }

                    }
                }

                // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                Log.Information($"{systemLog.TraceId} - List Result Delete: " + JsonSerializer.Serialize(listResult));

                #region remove DocumentNotifySchedule
                var documentNotifySchedule = _dataContext.DocumentNotifySchedule.Where(x => listId.Contains(x.DocumentId));
                if (await documentNotifySchedule.AnyAsync())
                {
                    _dataContext.DocumentNotifySchedule.RemoveRange(documentNotifySchedule);
                    await _dataContext.SaveChangesAsync();
                }
                #endregion

                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(DocumentQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                //#region Lấy các đơn vi con
                ////Nếu tài khoản quản trị show hết
                //var userRole = await _userRoleHandler.GetById(filter.CurrentUserId);
                //bool isOrgAdmin = false;
                //if (userRole != null && userRole.GetPropValue("Data") != null)
                //{
                //    isOrgAdmin = (bool)userRole?.GetPropValue("Data")?.GetPropValue("IsOrgAdmin");
                //}
                //List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(filter.OrganizationId);

                ////Nếu không phải admin đơn vị thì chỉ xem được:
                ////hợp đồng mình tạo
                ////hợp đồng liên quan đến mình
                ////hợp đồng ở các đơn vị cấp dưới
                //if (!isOrgAdmin)
                //    listChildOrgID.Remove(filter.OrganizationId);
                //#endregion

                #region Lấy quyền người dùng
                var roleIds = await _userHandler.GetUserRoleFromCacheAsync(filter.CurrentUserId);
                var userRole = await _roleHandler.GetRoleDataPermissionFromCacheByListIdAsync(roleIds);
                #endregion

                var data = (from item in _dataContext.Document.AsNoTracking()
                            join type in _dataContext.DocumentType.AsNoTracking() on item.DocumentTypeId equals type.Id into gj2
                            from type in gj2.DefaultIfEmpty()
                            join user in _dataContext.User.AsNoTracking() on item.UserId equals user.Id into gi3
                            from user in gi3.DefaultIfEmpty()
                            join org in _dataContext.Organization.AsNoTracking() on item.OrganizationId equals org.Id into orgGrp
                            from org in orgGrp.DefaultIfEmpty()
                            join wf in _dataContext.Workflow.AsNoTracking() on item.WorkflowId equals wf.Id into gj4
                            from wf in gj4.DefaultIfEmpty()
                            join wfu in _dataContext.WorkflowUserSign.AsNoTracking() on item.NextStepId equals wfu.Id into gj5
                            from wfu in gj5.DefaultIfEmpty()
                            where item.IsDeleted == filter.IsDeleted
                                && ((item.CreatedUserId == filter.CurrentUserId || item.WorkFlowUserJson.Contains(filter.CurrentUserId.ToString())
                                                        //Phân quyền dữ liệu theo đơn vị
                                                        || (item.OrganizationId.HasValue && userRole.ListDocumentOfOrganizationId.Contains(item.OrganizationId.Value))))
                                //Phân quyền dữ liệu theo loại hợp đồng
                                && userRole.ListDocumentTypeId.Contains(type.Id)
                                && type.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS && type.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                            select new DocumentBaseModel()
                            {
                                Id = item.Id,
                                Document3rdId = item.Document3rdId,
                                Code = item.Code,
                                Name = item.Name,
                                Status = item.Status,
                                DocumentTypeName = type == null ? "" : type.Name,
                                WorkflowId = item.WorkflowId,
                                CreatedDate = item.CreatedDate,
                                ModifiedDate = item.ModifiedDate,
                                DocumentStatus = item.DocumentStatus,
                                Email = item.Email,
                                FullName = item.FullName,
                                StateId = item.StateId,
                                State = item.State,
                                StateName = item.WorkflowState.Name,
                                StateNameForReject = item.WorkflowState.NameForReject,
                                NextStepId = item.NextStepId,
                                NextStepUserId = item.NextStepUserId,
                                NextStepSignType = item.NextStepSignType,
                                NextStepUserName = item.NextStepUserName,
                                NextStepUserEmail = item.NextStepUserEmail,
                                WorkFlowUserJson = item.WorkFlowUserJson,
                                IsSign = item.NextStepUserId == filter.CurrentUserId,
                                IsSignExpireAtDate = (item.DocumentStatus.Equals(DocumentStatus.PROCESSING) && item.SignExpireAtDate.HasValue && item.SignExpireAtDate.Value < dateNow), //Trạng thái hết hạn ký hợp đồng
                                SignExpireAtDate = item.SignExpireAtDate,
                                LastReasonReject = item.LastReasonReject,
                                UserName = user.UserName,
                                UserFullName = user.Name,
                                UserIdentityNumber = user.IdentityNumber,
                                UserEmail = user.Email,
                                UserPhoneNumber = user.PhoneNumber,
                                OrganizationName = org.Name,
                                SignCompleteAtDate = item.SignCompleteAtDate,
                                IsDeleted = item.IsDeleted,
                                WorkflowCode = wf.Code,
                                WorkflowCreatedDate = wf.CreatedDate,
                                SignCloseAtDate = item.SignCloseAtDate,
                                IsCloseDocument = (item.DocumentStatus.Equals(DocumentStatus.PROCESSING) && item.SignCloseAtDate.HasValue && item.SignCloseAtDate.Value < dateNow),
                                IsAllowRenew = wfu != null && ((wfu.IsAllowRenew && wfu.MaxRenewTimes > item.RenewTimes) || (wfu.IsAllowRenew && !wfu.MaxRenewTimes.HasValue)) ? true : false,
                                CreatedUserName = item.CreatedUserName,
                                ExportDocumentDataJson = item.ExportDocumentDataJson
                            });

                if (filter.DocumentBatchId.HasValue)
                {
                    data = data.Where(x => x.DocumentBatchId == filter.DocumentBatchId);
                }

                if (filter.DocumentTypeId.HasValue)
                {
                    data = data.Where(x => x.DocumentTypeId == filter.DocumentTypeId);
                }

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x =>
                        x.Name.ToLower().Contains(ts)
                        || x.Id.ToString().ToLower().Contains(ts)
                        || x.Code.ToLower().Contains(ts)
                        || x.Document3rdId.ToLower().Contains(ts)
                        || x.UserName.ToLower().Contains(ts)
                        || x.UserFullName.ToLower().Contains(ts)
                        || x.UserIdentityNumber.ToLower().Contains(ts)
                        || x.UserEmail.ToLower().Contains(ts)
                        || x.UserPhoneNumber.ToLower().Contains(ts)
                        || x.OrganizationName.ToLower().Contains(ts)
                        || x.FullName.ToLower().Contains(ts)
                        );
                }

                // Nếu là lọc theo trạng thái
                if (filter.IsSignExpireAtDate)
                {
                    data = data.Where(x => x.IsSignExpireAtDate && !x.IsCloseDocument);
                }
                else if (filter.IsClosed)
                {
                    data = data.Where(x => x.IsCloseDocument);
                }
                else
                {
                    if (filter.Status.HasValue)
                    {
                        data = data.Where(x => (int)x.DocumentStatus == filter.Status.Value && !x.IsSignExpireAtDate && !x.IsCloseDocument);
                    }
                }

                if (filter.AssignMe == true)
                {
                    data = data.Where(x => x.NextStepUserId == filter.CurrentUserId);
                }

                if (filter.StartDate.HasValue && !filter.IsIncommingSignExpirationDate)
                {
                    data = data.Where(x => x.SignExpireAtDate.HasValue && x.SignExpireAtDate.Value.Date >= filter.StartDate.Value.Date);
                }

                if (filter.EndDate.HasValue && !filter.IsIncommingSignExpirationDate)
                {
                    data = data.Where(x => x.SignExpireAtDate.HasValue && x.SignExpireAtDate.Value.Date <= filter.EndDate.Value.Date);
                }

                if (filter.IsIncommingSignExpirationDate && filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    data = data.Where(x => x.DocumentStatus == DocumentStatus.PROCESSING
                        && x.SignExpireAtDate.HasValue
                        && x.SignExpireAtDate.Value.Date >= filter.StartDate.Value.Date
                        && x.SignExpireAtDate.Value.Date <= filter.EndDate.Value.Date
                        && !x.IsCloseDocument
                        && !x.IsSignExpireAtDate);
                }

                if (filter.StateId.HasValue)
                {
                    data = data.Where(x => x.StateId.HasValue && x.StateId.Value.Equals(filter.StateId));
                }

                if (filter.IsIncommingSignExpirationDate && !filter.StartDate.HasValue && !filter.EndDate.HasValue)
                {
                    int incommingExpirationDate = Convert.ToInt16(Utils.GetConfig("DefaultValue:IncommingExpirationDate"));
                    data = data.Where(x => x.SignExpireAtDate.HasValue
                        && x.SignExpireAtDate.Value > dateNow
                        && x.SignExpireAtDate.Value <= dateNow.AddDays(incommingExpirationDate)
                        && x.DocumentStatus == DocumentStatus.PROCESSING
                        && !x.IsCloseDocument
                        && !x.IsSignExpireAtDate);
                }

                if (!string.IsNullOrEmpty(filter.UserName))
                {
                    //var userName = filter.UserName.Trim().ToLower();

                    //data = data.Where(x => (!string.IsNullOrEmpty(x.UserName) && x.UserName.ToLower().Contains(filter.UserName.ToLower()))
                    //    || (!string.IsNullOrEmpty(x.UserFullName) && x.UserFullName.ToLower().Contains(filter.UserName.ToLower())));

                    string ts = filter.UserName.Trim().ToLower();
                    data = data.Where(x =>
                        x.UserName.ToLower().Contains(ts)
                        || x.UserFullName.ToLower().Contains(ts)
                        || x.UserIdentityNumber.ToLower().Contains(ts)
                        || x.UserEmail.ToLower().Contains(ts)
                        || x.UserPhoneNumber.ToLower().Contains(ts)
                        );
                }

                if (!string.IsNullOrEmpty(filter.ReferenceCode))
                {
                    var ls = filter.ReferenceCode.ToLower().Trim().Split(";").ToList();
                    data = data.Where(x =>
                       ls.Contains(x.Document3rdId.ToLower())
                       );
                }

                if (filter.SignStartDate.HasValue)
                {
                    data = data.Where(x => x.SignCompleteAtDate.HasValue && x.SignCompleteAtDate.Value.Date >= filter.SignStartDate.Value.Date);
                }

                if (filter.SignEndDate.HasValue)
                {
                    data = data.Where(x => x.SignCompleteAtDate.HasValue && x.SignCompleteAtDate.Value.Date <= filter.SignEndDate.Value.Date);
                }

                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                int totalCount = data.Count();

                // Pagination
                if (filter.PageSize.HasValue && filter.PageNumber.HasValue)
                {
                    if (filter.PageSize <= 0)
                    {
                        filter.PageSize = QueryFilter.DefaultPageSize;
                    }

                    //Calculate nunber of rows to skip on pagesize
                    int excludedRows = (filter.PageNumber.Value - 1) * (filter.PageSize.Value);
                    if (excludedRows <= 0)
                    {
                        excludedRows = 0;
                    }

                    // Query
                    data = data.Skip(excludedRows).Take(filter.PageSize.Value);
                }

                int dataCount = data.Count();
                var listResult = await data.ToListAsync();

                foreach (var item in listResult)
                {
                    if (item.IsSign && item.IsSignExpireAtDate)
                    {
                        item.IsSign = false;
                    }
                }

                return new ResponseObject<PaginationList<DocumentBaseModel>>(new PaginationList<DocumentBaseModel>()
                {
                    DataCount = dataCount,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber ?? 0,
                    PageSize = filter.PageSize ?? 0,
                    Data = listResult
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetById(Guid id, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.Document

                            join batch in _dataContext.DocumentBatch on item.DocumentBatchId equals batch.Id into gj
                            from batch in gj.DefaultIfEmpty()

                            join type in _dataContext.DocumentType on item.DocumentTypeId equals type.Id into gj2
                            from type in gj2.DefaultIfEmpty()

                            join wfUserSign in _dataContext.WorkflowUserSign on item.NextStepId equals wfUserSign.Id into gj3
                            from wfUserSign in gj3.DefaultIfEmpty()

                            where item.IsDeleted == false
                            select new DocumentModel()
                            {
                                Id = item.Id,
                                DocumentBatchCode = batch == null ? "" : batch.Code,
                                DocumentBatchName = batch == null ? "" : batch.Name,
                                Code = item.Code,
                                Name = item.Name,
                                Status = item.Status,
                                DocumentBatchId = item.DocumentBatchId,
                                DocumentTypeId = item.DocumentTypeId,
                                DocumentTypeName = type == null ? "" : type.Name,
                                WorkflowId = item.WorkflowId,
                                CreatedDate = item.CreatedDate,
                                DocumentStatus = item.DocumentStatus,
                                Email = item.Email,
                                FullName = item.FullName,
                                NextStepId = item.NextStepId,
                                NextStepUserId = item.NextStepUserId,
                                NextStepSignType = item.NextStepSignType,
                                NextStepUserName = item.NextStepUserName,
                                NextStepUserEmail = item.NextStepUserEmail,
                                WorkFlowUserJson = item.WorkFlowUserJson,
                                IsSign = item.NextStepUserId == userId,
                                SignExpireAfterDay = wfUserSign.SignExpireAfterDay,
                                LastReasonReject = item.LastReasonReject
                            });

                var model = await data.FirstOrDefaultAsync(x => x.Id == id);

                if (model == null)
                {
                    return new ResponseObject<DocumentModel>(null, MessageConstants.GetDataSuccessMessage, Code.Success);
                }
                model.DocumentFile = await _dataContext.DocumentFile
                    .Where(x => x.DocumentId == id)
                    .OrderBy(x => x.Order)
                    .Select(x => new DocumentFileModel()
                    {
                        Id = x.Id,
                        FileBucketName = x.FileBucketName,
                        Order = x.Order,
                        CreatedDate = x.CreatedDate,
                        FileName = x.FileName,
                        FileObjectName = x.FileObjectName,
                        ProfileName = x.ProfileName
                    }).ToListAsync();

                var ms = new MinIOService();

                foreach (var item in model.DocumentFile)
                {
                    item.FileUrl = await ms.GetObjectPresignUrlAsync(item.FileBucketName, item.FileObjectName);
                }

                return new ResponseObject<DocumentModel>(model, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<Data.Document>> InternalGetDocumentByListCode(List<string> listCode, SystemLogModel systemLog)
        {
            try
            {
                var dt = await _dataContext.Document.Where(x => listCode.Contains(x.Code)).ToListAsync();

                return dt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new List<Data.Document>();
            }
        }

        public async Task<List<Data.Document>> InternalGetDocumentByListId(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                var dt = await _dataContext.Document.Where(x => listId.Contains(x.Id)).ToListAsync();

                return dt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new List<Data.Document>();
            }
        }

        public async Task<Data.Document> InternalGetDocumentById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                var dt = await _dataContext.Document.FirstOrDefaultAsync(x => x.Id == id);

                return dt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        public async Task<Response> SendToWorkflow(List<Guid> listId, string currentEmail, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List SendToWorkflow: " + JsonSerializer.Serialize(listId));
                var listResult = new List<ResponeSendToWorkflowModel>();
                var name = "";
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.Document.FindAsync(item);

                    if (entity == null || entity.IsDeleted == true)
                    {
                        listResult.Add(new ResponeSendToWorkflowModel()
                        {
                            Id = item,
                            Name = name,
                            Result = false,
                            Message = "Không tìm thấy hợp đồng"
                        });
                    }
                    else
                    {
                        name = entity.Name;
                        if (entity.DocumentStatus == DocumentStatus.DRAFT)
                        {
                            entity.DocumentStatus = DocumentStatus.PROCESSING;
                            entity.ModifiedDate = dateNow;

                            var stepInfo = entity.WorkFlowUser.First();
                            var userInfo = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == stepInfo.UserId);

                            var stepWFInfo = await _dataContext.WorkflowUserSign.FirstOrDefaultAsync(x => x.WorkflowId == entity.WorkflowId);
                            //if (firstWFUser == null)
                            //{
                            //    listResult.Add(new ResponeSendToWorkflowModel()
                            //    {
                            //        Id = item,
                            //        Name = name,
                            //        Result = false,
                            //        Message = "Lỗi quy trình (Quy trình không có người xử lý)."
                            //    });
                            //    continue;
                            //}

                            DateTime? signExpireDate = null;
                            if (stepWFInfo.SignExpireAfterDay.HasValue)
                                signExpireDate = dateNow.AddDays(stepWFInfo.SignExpireAfterDay.Value);

                            DateTime? signCloseDate = null;
                            if (stepWFInfo.SignCloseAfterDay.HasValue)
                                signCloseDate = dateNow.AddDays(stepWFInfo.SignCloseAfterDay.Value);

                            entity.SignExpireAtDate = signExpireDate;
                            entity.WorkflowStartDate = dateNow;
                            entity.NextStepId = stepWFInfo.Id;
                            entity.NextStepUserId = userInfo.Id;
                            entity.NextStepUserName = userInfo.Name;
                            entity.NextStepUserEmail = userInfo.Email;
                            entity.NextStepSignType = stepWFInfo.Type;
                            entity.State = stepWFInfo.State;
                            entity.StateId = stepWFInfo.StateId;
                            entity.NextStepUserPhoneNumber = userInfo.PhoneNumber;
                            entity.ModifiedDate = dateNow;

                            _dataContext.Document.Update(entity);

                            #region Lưu lịch sử khi hợp đồng thay đổi
                            await CreateDocumentWFLHistory(entity);
                            #endregion

                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    listResult.Add(new ResponeSendToWorkflowModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = true,
                                        Message = "Trình duyệt thành công"
                                    });

                                    systemLog.ListAction.Add(new ActionDetail
                                    {
                                        Description = $"Trình duyệt hợp đồng có mã {entity.Code}",
                                        ObjectCode = entity.Code,
                                        ObjectId = entity.Id.ToString(),
                                        CreatedDate = dateNow
                                    });

                                    //if (userSign != null && !string.IsNullOrEmpty(userSign.Email))
                                    //    SendMailRemindSign(currentEmail, userSign.Email, firstWFUser.UserName, systemLog);
                                }
                                else
                                {
                                    listResult.Add(new ResponeSendToWorkflowModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = false,
                                        Message = "Trình duyệt không thành công"
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                                listResult.Add(new ResponeSendToWorkflowModel()
                                {
                                    Id = item,
                                    Name = name,
                                    Result = false,
                                    Message = ex.Message
                                });
                            }
                        }
                        else
                        {
                            listResult.Add(new ResponeSendToWorkflowModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = "Chỉ có thể trình duyệt khi hợp đồng đang ở trạng thái soạn thảo."
                            });
                        }

                    }
                }
                Log.Information($"{systemLog.TraceId} - List Result SendToWorkflow: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeSendToWorkflowModel>>(listResult, "Kết quả trình duyệt", Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Reject(DocumentRejectModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List Reject: " + JsonSerializer.Serialize(model));
                var listResult = new List<ResponeSendToWorkflowModel>();
                var name = "";

                var orgCode = "";
                var rootOrg = await _organizationHandler.GetRootByChidId(new Guid(systemLog.OrganizationId));
                if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                    orgCode = orgRoot != null && orgRoot.Data != null ? orgRoot.Data.Code : "";

                foreach (var item in model.ListId)
                {
                    name = "";
                    var entity = await _dataContext.Document.FindAsync(item);

                    if (entity == null || entity.IsDeleted == true)
                    {
                        listResult.Add(new ResponeSendToWorkflowModel()
                        {
                            Id = item,
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
                            entity.DocumentStatus = DocumentStatus.CANCEL;
                            entity.ModifiedDate = dateNow;
                            entity.LastReasonReject = model.LastReasonReject;
                            _dataContext.Document.Update(entity);

                            #region Lưu lịch sử thay đổi của hợp đồng
                            await CreateDocumentWFLHistory(entity);
                            #endregion

                            try
                            {
                                int dbSave = await _dataContext.SaveChangesAsync();
                                if (dbSave > 0)
                                {
                                    listResult.Add(new ResponeSendToWorkflowModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = true,
                                        Message = "Từ chối hợp đồng thành công"
                                    });

                                    systemLog.ListAction.Add(new ActionDetail
                                    {
                                        Description = $"Từ chối hợp đồng với lý do: {model.LastReasonReject}",
                                        ObjectCode = CacheConstants.DOCUMENT,
                                        ObjectId = entity.Id.ToString(),
                                        CreatedDate = dateNow
                                    });

                                    if (model.NotifyConfigId.HasValue)
                                    {
                                        var notify = _dataContext.NotifyConfig.Find(model.NotifyConfigId);
                                        if (notify != null)
                                        {
                                            var userNoti = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == entity.UserId);

                                            object param = new
                                            {
                                                userFullName = userNoti != null ? userNoti.Name : "",
                                                documentCode = entity.Code,
                                                documentName = entity.Name,
                                                expireTime = entity.SignExpireAtDate.HasValue ? entity.SignExpireAtDate.Value.ToString("HH:mm") : "",
                                                expireDate = entity.SignExpireAtDate.HasValue ? entity.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : ""
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

                                            var userTokens = await _dataContext.UserMapFirebaseToken.Where(x => x.UserId == entity.UserId).Select(x => x.FirebaseToken).ToListAsync();
                                            if (userNoti != null)
                                            {
                                                _ = _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
                                                {
                                                    OraganizationCode = orgCode,
                                                    IsSendSMS = notify.IsSendSMS,
                                                    ListPhoneNumber = new List<string>() { userNoti.PhoneNumber },
                                                    SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                                                    IsSendEmail = notify.IsSendEmail,
                                                    ListEmail = new List<string>() { userNoti.Email },
                                                    EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                                                    EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                                                    IsSendNotification = notify.IsSendNotification,
                                                    ListToken = userTokens,
                                                    NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                                                    NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                                                    TraceId = systemLog.TraceId,
                                                    Data = new Dictionary<string, string>()
                                                    {
                                                        { "NotifyType", notify.NotifyType.ToString() }
                                                    }
                                                }).ConfigureAwait(false);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    listResult.Add(new ResponeSendToWorkflowModel()
                                    {
                                        Id = item,
                                        Name = name,
                                        Result = false,
                                        Message = "Hủy hợp đồng không thành công"
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                                listResult.Add(new ResponeSendToWorkflowModel()
                                {
                                    Id = item,
                                    Name = name,
                                    Result = false,
                                    Message = ex.Message
                                });
                            }
                        }
                        else
                        {
                            listResult.Add(new ResponeSendToWorkflowModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = "Chỉ có thể từ chối khi hợp đồng đang ở trạng thái đang xử lý."
                            });
                        }

                    }
                }
                Log.Information($"{systemLog.TraceId} - List Result Reject: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeSendToWorkflowModel>>(listResult, "Kết quả trình duyệt", Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Approve(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
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

                                    DateTime? closeDate;
                                    if (lstUser[stepOrderUpdate + 1].SignCloseAfterDay.HasValue)
                                        closeDate = DateTime.Now.AddDays(lstUser[stepOrderUpdate + 1].SignCloseAfterDay.Value);
                                    else
                                        closeDate = null;

                                    entity.NextStepId = lstUser[stepOrderUpdate + 1].Id;
                                    entity.NextStepUserId = lstUser[stepOrderUpdate + 1].UserId;
                                    entity.NextStepUserName = lstUser[stepOrderUpdate + 1].UserName;
                                    entity.NextStepUserEmail = lstUser[stepOrderUpdate + 1].UserEmail;
                                    entity.NextStepSignType = lstUser[stepOrderUpdate + 1].Type;
                                    entity.StateId = lstUser[stepOrderUpdate + 1].StateId;
                                    entity.State = lstUser[stepOrderUpdate + 1].State;
                                    entity.SignExpireAtDate = expireDate;
                                    entity.SignCloseAtDate = closeDate;
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
                                    entity.SignCloseAtDate = null;
                                }

                                entity.WorkFlowUser.Where(c => c.Id == currentStepUser.Id).FirstOrDefault().SignAtDate = dateNow;
                                entity.ModifiedDate = dateNow;
                                _dataContext.Document.Update(entity);

                                #region Lưu lịch sử thay đổi của hợp đồng
                                await CreateDocumentWFLHistory(entity);
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
                                            Description = $"Duyệt hơp đồng có mã: {entity.Code}",
                                            ObjectCode = entity.Code,
                                            ObjectId = entity.Id.ToString(),
                                            CreatedDate = dateNow
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
                Log.Information($"{systemLog.TraceId} - List Result Approve: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeSendToWorkflowModel>>(listResult, "Kết quả duyệt", Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateStatus(DocumentUpdateStatusModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update status {CachePrefix}: {JsonSerializer.Serialize(model)}");

                var documents = await _dataContext.Document.Where(x => model.DocumentIds.Contains(x.Id) && (x.DocumentStatus == DocumentStatus.DRAFT || x.DocumentStatus == DocumentStatus.PROCESSING)).ToListAsync();

                if (documents.Count != model.DocumentIds.Count)
                {
                    return new ResponseError(Code.ServerError, $"Người dùng không có quyền thao tác dữ liệu");
                }

                var orgCode = "";
                var rootOrg = await _organizationHandler.GetRootByChidId(new Guid(systemLog.OrganizationId));
                if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                    orgCode = orgRoot != null && orgRoot.Data != null ? orgRoot.Data.Code : "";

                #region Xóa document notify schedule
                var documentNotifySchedules = _dataContext.DocumentNotifySchedule.Where(x => model.DocumentIds.Contains(x.DocumentId));
                if (await documentNotifySchedules.AnyAsync()) _dataContext.DocumentNotifySchedule.RemoveRange(documentNotifySchedules);
                #endregion

                // lấy thông tin cấu hình thông báo
                var notify = await _dataContext.NotifyConfig.Where(x => x.Id == model.NotifyConfigId).FirstOrDefaultAsync();

                // lấy danh sách người dùng để gửi thông báo
                var listUser = await _userHandler.GetListUserFromCache();

                var listUserSendNotiId = new List<Guid>();
                var listUserReceiveNoti = new List<UserSelectItemModel>();

                foreach (var document in documents)
                {
                    document.DocumentStatus = DocumentStatus.CANCEL;
                    document.LastReasonReject = model.LastReasonReject;
                    _dataContext.Update(document);
                    await CreateDocumentWFLHistory(document);

                    int result = await _dataContext.SaveChangesAsync();
                    if (result > 0)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = "Từ chối hợp đồng thành công - Lý do từ chối: " + model.LastReasonReject,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = document.Id.ToString(),
                            CreatedDate = DateTime.Now
                        });

                        Log.Information($"{systemLog.TraceId} - After update: " + JsonSerializer.Serialize(document));

                        #region Gửi thông báo
                        if (model.NotifyConfigId.HasValue)
                        {
                            if (notify != null)
                            {
                                // add userId trong quy trình của hợp đồng để gửi thông báo
                                foreach (var wfUser in document.WorkFlowUser)
                                {
                                    if (wfUser.UserId.HasValue && !listUserSendNotiId.Contains(wfUser.UserId.Value))
                                    {
                                        listUserSendNotiId.Add(wfUser.UserId.Value);
                                    }
                                }

                                // lấy danh sách token của list user
                                var lsWFUserTokenSendNoti = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => listUserSendNotiId.Contains(x.UserId)).ToListAsync();

                                // lấy danh sách user nhận thông báo                                                              
                                foreach (var userNoti in listUser.Where(x => listUserSendNotiId.Contains(x.Id)).ToList())
                                {
                                    if (!listUserReceiveNoti.Any(x => x.Id == userNoti.Id))
                                    {
                                        object param = new
                                        {
                                            userFullName = userNoti != null ? userNoti.Name : "",
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

                                        _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
                                        {
                                            OraganizationCode = orgCode,
                                            IsSendSMS = notify.IsSendSMS,
                                            ListPhoneNumber = new List<string>() { userNoti.PhoneNumber },
                                            SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                                            IsSendEmail = notify.IsSendEmail,
                                            ListEmail = new List<string>() { userNoti.Email },
                                            EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                                            EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                                            IsSendNotification = notify.IsSendNotification,
                                            ListToken = lsWFUserTokenSendNoti.Where(x => x.UserId == userNoti.Id).Select(x => x.FirebaseToken).ToList(),
                                            NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                                            NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                                            TraceId = systemLog.TraceId,
                                            Data = new Dictionary<string, string>()
                                            {
                                                { "NotifyType", notify.NotifyType.ToString() }
                                            }
                                        });

                                        listUserReceiveNoti.Add(userNoti);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        Log.Error($"{systemLog.TraceId} - {MessageConstants.UpdateErrorMessage}");
                    }
                }

                return new ResponseObject<List<Guid>>(model.DocumentIds, MessageConstants.UpdateSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateDocumentFilePreview(PdfCallBackResponseModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update file preview - {model.ObjectId}");

                var documentFile = await _dataContext.DocumentFile.FindAsync(new Guid(model.ObjectId));
                if (documentFile == null)
                    return new ResponseError(Code.NotFound, "File hợp đồng không tồn tại trên hệ thống");

                var document = await _dataContext.Document.FindAsync(documentFile.DocumentId);
                if (document == null)
                    return new ResponseError(Code.NotFound, "Hợp đồng không tồn tại trên hệ thống");
                if (document.OrganizationId.HasValue)
                {
                    systemLog.OrganizationId = document.OrganizationId?.ToString();
                }

                List<ImagePreview> listImage = new List<ImagePreview>();

                //Convert và tải file lên minio
                var ms = new MinIOService();
                byte[] bytes;
                MemoryStream memory;
                int i = 0;
                foreach (var item in model.ListFileBase64)
                {
                    i++;
                    bytes = Convert.FromBase64String(item);
                    memory = new MemoryStream(bytes);
                    var rs = await ms.UploadObjectAsync(document.BucketName, documentFile.FileObjectName.Replace(".pdf", "").Replace(".PDF", "") + $"_page{i}.png", memory, false);
                    listImage.Add(new ImagePreview()
                    {
                        BucketName = rs.BucketName,
                        ObjectName = rs.ObjectName
                    });
                }

                documentFile.ImagePreview = listImage;

                _dataContext.DocumentFile.Update(documentFile);

                int result = await _dataContext.SaveChangesAsync();
                if (result > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Cập nhật file preview hợp đồng thành công",
                        //ObjectCode = CacheConstants.DOCUMENT,
                        //ObjectId = document.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });

                    Log.Information($"{systemLog.TraceId} - Update document preview success");
                    return new ResponseObject<bool>(true, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update status error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.ErrorLogMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateSignExpireAtDate(DocumentUpdateSignExpireAtDateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List UpdateSignExpireAtDate: " + JsonSerializer.Serialize(model));

                var documents = await _dataContext.Document.Where(x => model.DocumentIds.Contains(x.Id)).ToListAsync();

                if (documents.Count != model.DocumentIds.Count)
                {
                    return new ResponseError(Code.ServerError, $"Người dùng không có quyền thao tác dữ liệu");
                }

                var orgCode = "";
                var rootOrg = await _organizationHandler.GetRootByChidId(new Guid(systemLog.OrganizationId));
                if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                    orgCode = orgRoot != null && orgRoot.Data != null ? orgRoot.Data.Code : "";

                #region Insert DocumentNotifySchedule and remove DocumentNotifySchedule
                var documentNotifyScheduleRemove = _dataContext.DocumentNotifySchedule.Where(x => model.DocumentIds.Contains(x.DocumentId));
                if (await documentNotifyScheduleRemove.AnyAsync()) _dataContext.DocumentNotifySchedule.RemoveRange(documentNotifyScheduleRemove);
                #endregion

                // lấy thông tin cấu hình thông báo
                var notify = await _dataContext.NotifyConfig.Where(x => x.Id == model.NotifyConfigId).FirstOrDefaultAsync();

                // lấy danh sách người dùng để gửi thông báo
                var listUser = await _userHandler.GetListUserFromCache();

                var wfUserSigns = await _dataContext.WorkflowUserSign
                    .Where(x => documents.Select(x1 => x1.NextStepId).Contains(x.Id))
                    .Select(x => new WorkflowUserSign()
                    {
                        Id = x.Id,
                        NotifyConfigExpireId = x.NotifyConfigExpireId,
                        NotifyConfigRemindId = x.NotifyConfigRemindId,
                        IsAllowRenew = x.IsAllowRenew,
                        MaxRenewTimes = x.MaxRenewTimes,
                        SignCloseAfterDay = x.SignCloseAfterDay
                    })
                    .ToListAsync();

                var listUserSendNotiId = new List<Guid>();
                var listUserReceiveNoti = new List<UserSelectItemModel>();

                var listResult = new List<ResponeUpdateItemsModel>();
                foreach (var entity in documents)
                {
                    var wfUserSign = wfUserSigns.FirstOrDefault(x => x.Id == entity.NextStepId);

                    if (!wfUserSign.IsAllowRenew)
                    {
                        listResult.Add(new ResponeUpdateItemsModel()
                        {
                            Id = entity.Id,
                            Name = entity.Name,
                            Result = false,
                            Message = $"Hợp đồng {entity.Code} chưa được cấu hình cho phép làm mới!"
                        });
                        continue;
                    }

                    if (wfUserSign.MaxRenewTimes.HasValue && wfUserSign.MaxRenewTimes.Value <= entity.RenewTimes)
                    {
                        listResult.Add(new ResponeUpdateItemsModel()
                        {
                            Id = entity.Id,
                            Name = entity.Name,
                            Result = false,
                            Message = $"Hợp đồng {entity.Code} quá số lần được làm mới hợp đồng!"
                        });
                        continue;
                    }

                    entity.RenewTimes = entity.RenewTimes + 1;
                    entity.SignExpireAtDate = model.SignExpireAtDate;
                    entity.LastReasonReject = model.LastReasonReject;

                    if (wfUserSign.SignCloseAfterDay.HasValue)
                    {
                        entity.SignCloseAtDate = DateTime.Now.AddDays(wfUserSign.SignCloseAfterDay.Value);
                    }

                    //Cập nhập về trạng thái đang xử lý đối với hợp đồng đã hủy
                    if (entity.DocumentStatus == DocumentStatus.CANCEL)
                        entity.DocumentStatus = DocumentStatus.PROCESSING;

                    _dataContext.Document.Update(entity);

                    #region Lưu lịch sử khi hợp đồng thay đổi
                    await CreateDocumentWFLHistory(entity);
                    #endregion

                    int isSaved = await _dataContext.SaveChangesAsync();
                    if (isSaved > 0)
                    {
                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = "Cập nhật ngày hết hạn hợp đồng thành công - Lý do cập nhật: " + model.LastReasonReject,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = entity.Id.ToString(),
                            CreatedDate = DateTime.Now
                        });

                        listResult.Add(new ResponeUpdateItemsModel()
                        {
                            Id = entity.Id,
                            Name = entity.Name,
                            Result = true,
                            Message = MessageConstants.UpdateSuccessMessage
                        });

                        Log.Information($"{systemLog.TraceId} - After update: " + JsonSerializer.Serialize(entity));

                        if (model.NotifyConfigId.HasValue)
                        {
                            if (notify != null && entity.NextStepUserId.HasValue)
                            {
                                if (model.UserIds != null && model.UserIds.Count > 0)
                                {
                                    foreach (var item in model.UserIds)
                                    {
                                        await SendNotifyRenew(item.Value, wfUserSign, entity, listUser, notify, orgCode, systemLog, listUserReceiveNoti);
                                    }
                                }
                                else
                                {
                                    Guid userSendNotiId = entity.NextStepUserId.Value;
                                    await SendNotifyRenew(userSendNotiId, wfUserSign, entity, listUser, notify, orgCode, systemLog, listUserReceiveNoti);
                                }
                            }
                        }
                    }
                    else
                    {
                        listResult.Add(new ResponeUpdateItemsModel()
                        {
                            Id = entity.Id,
                            Name = entity.Name,
                            Result = false,
                            Message = MessageConstants.UpdateErrorMessage
                        });
                        Log.Error($"{systemLog.TraceId} - Update signExpireAtDate error: Save database error!");
                    }
                }

                return new ResponseObject<List<ResponeUpdateItemsModel>>(listResult, string.Join('\n', listResult.Select(x => x.Message)), Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private async Task SendNotifyRenew(Guid userId, WorkflowUserSign wfUserSign, Document entity, List<UserSelectItemModel> listUser, NotifyConfig notify, string orgCode, SystemLogModel systemLog, List<UserSelectItemModel> listUserReceiveNoti)
        {
            Guid userSendNotiId = userId;

            var userNoti = listUser.Where(x => userSendNotiId == x.Id).FirstOrDefault();
            if (userNoti != null)
            {
                //// lấy danh sách token của list user
                var lsWFUserTokenSendNoti = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => x.UserId == userSendNotiId).ToListAsync();

                #region insert notify schedule
                var user = await _dataContext.User.FirstOrDefaultAsync(x => x.Id == userNoti.Id);
                var documentNotifySchedule = new DocumentNotifySchedule()
                {
                    DocumentId = entity.Id,
                    DocumentCode = entity.Code,
                    DocumentName = entity.Name,
                    UserId = user.Id,
                    UserName = user.UserName,
                    WorkflowStepId = wfUserSign.Id,
                    NotifyConfigExpireId = wfUserSign.NotifyConfigExpireId,
                    NotifyConfigRemindId = wfUserSign.NotifyConfigRemindId,
                    SignExpireAtDate = entity.SignExpireAtDate.Value,
                    OrganizationId = entity.OrganizationId,
                    CreatedDate = DateTime.Now
                };

                _dataContext.DocumentNotifySchedule.Add(documentNotifySchedule);
                #endregion

                object param = new
                {
                    userFullName = userNoti != null ? userNoti.Name : "",
                    documentCode = entity.Code,
                    documentName = entity.Name,
                    expireTime = entity.SignExpireAtDate.HasValue ? entity.SignExpireAtDate.Value.ToString("HH:mm") : "",
                    expireDate = entity.SignExpireAtDate.HasValue ? entity.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : ""
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

                await _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
                {
                    OraganizationCode = orgCode,
                    IsSendSMS = notify.IsSendSMS,
                    ListPhoneNumber = new List<string>() { userNoti.PhoneNumber },
                    SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                    IsSendEmail = notify.IsSendEmail,
                    ListEmail = new List<string>() { userNoti.Email },
                    EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                    EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                    IsSendNotification = notify.IsSendNotification,
                    ListToken = lsWFUserTokenSendNoti.Where(x => x.UserId == userNoti.Id).Select(x => x.FirebaseToken).ToList(),
                    NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                    NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                    TraceId = systemLog.TraceId,
                    Data = new Dictionary<string, string>()
                                            {
                                                { "NotifyType", notify.NotifyType.ToString() }
                                            }
                });

                listUserReceiveNoti.Add(userNoti);
            }
        }

        //public async Task<Response> ProcessingWorkflow(WorkflowDocumentProcessingModel model, Guid userId, SystemLogModel systemLog)
        //{
        //    try
        //    {
        //        #region Lấy thông tin ký 
        //        var certAlias = string.Empty;
        //        var certUserPin = string.Empty;
        //        var certSlotLabel = string.Empty;

        //        if (model.SignType == 1)
        //        {
        //            // Lấy thông tin đơn vị theo id
        //            if (string.IsNullOrEmpty(model.OrganizationId))
        //            {
        //                return new ResponseError(Code.ServerError, $"Không lấy được thông tin chữ ký của đơn vị");
        //            }
        //            var org = await _dataContext.Organization
        //                .FirstOrDefaultAsync(x => x.Id.ToString() == model.OrganizationId);

        //            //certAlias = org.CertAlias ?? "";
        //            //certUserPin = org.CertUserPin ?? "";
        //            //certSlotLabel = org.CertSlotLabel ?? "";
        //        }
        //        else if (model.SignType == 2)
        //        {
        //            // Lấy thông tin người dùng theo Id
        //            var user = await _dataContext.User
        //                .FirstOrDefaultAsync(x => x.Id == userId);

        //            //certAlias = user.CertAlias ?? "";
        //            //certUserPin = user.CertUserPin ?? "";
        //            //certSlotLabel = user.CertSlotLabel ?? "";
        //        }

        //        if (string.IsNullOrEmpty(certAlias) || string.IsNullOrEmpty(certUserPin) || string.IsNullOrEmpty(certSlotLabel))
        //        {
        //            return new ResponseError(Code.ServerError, $"Chưa cấu hình thông tin chứ ký");
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
        //                var convertResult = await ConvertCoordinateFile(metaDataConfig, file, model.Base64Image, systemLog);

        //                if (convertResult.Code == Code.Success && convertResult is ResponseObject<DataInputSignPDF> resultData)
        //                {
        //                    // Thực hiện ký
        //                    // Gán thông tin ký
        //                    resultData.Data.CertAlias = certAlias;
        //                    resultData.Data.CertUserPin = certUserPin;
        //                    resultData.Data.CertSlotLabel = certSlotLabel;

        //                    var resultSigned = await _signServiceHandler.SignBySigningBox(resultData.Data, userId, model.SignType);

        //                    if (resultSigned.Code == Code.Success && resultSigned is ResponseObject<SignFileModel> resultDataSigned)
        //                    {
        //                        var fileAfterSigned = resultDataSigned.Data;
        //                        // Lưu log vào bảng document sign history
        //                        await _dataContext.DocumentSignHistory.AddAsync(new DocumentSignHistory()
        //                        {
        //                            Id = Guid.NewGuid(),
        //                            CreatedDate = dateNow,
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
        //                        });

        //                        // Cập nhật document-file
        //                        file.FileBucketName = fileAfterSigned.FileBucketName;
        //                        // file.FileName = fileAfterSigned.FileName;
        //                        file.FileObjectName = fileAfterSigned.FileObjectName;

        //                        _dataContext.DocumentFile.Update(file);
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
        //            }

        //            // Cập nhật signed Date
        //            document.WorkFlowUser.Where(c => c.Id == currentStepUser.Id).FirstOrDefault().SignAtDate = dateNow;

        //            // Cập nhật lại ngày xử lý file
        //            document.ModifiedDate = dateNow;

        //            // Lưu 
        //            _dataContext.Document.Update(document);
        //            #endregion

        //            // Save vào database
        //            int dbSave = await _dataContext.SaveChangesAsync();
        //            if (dbSave > 0)
        //            {
        //                Log.Information($"{systemLog.TraceId} - Ký document" + document.Id + " thành công");

        //                responseSignModel.Add(new WorkflowDocumentSignReponseModel()
        //                {
        //                    DocumentId = documentId,
        //                    DocumentCode = document.Code,
        //                    DocumentName = document.Name,
        //                    IsSuccess = true,
        //                    Message = $"Ký thành công"
        //                });

        //                // Gửi mail cho người kế tiếp
        //                if (lstUser.Count - 1 > stepOrderUpdate)
        //                {
        //                    SendMailRemindSign(currentStepUser.UserEmail, lstUser[stepOrderUpdate + 1].UserEmail, lstUser[stepOrderUpdate + 1].UserName, systemLog);
        //                }
        //            }
        //            else
        //            {
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

        //        return new ResponseObject<List<WorkflowDocumentSignReponseModel>>(responseSignModel, MessageConstants.CreateSuccessMessage, Code.Success);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
        //        return new ResponseError(Code.ServerError, $"{ex.Message}");
        //    }
        //}        

        public async Task<Response> SendMail(DocumentSendMailModel model, SystemLogModel systemLog)
        {
            try
            {
                var ms = new MinIOService();

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

                foreach (var item in model.ListDocumentId)
                {
                    var entity = await _dataContext.Document.FindAsync(item);

                    if (entity != null && entity.IsDeleted == false && entity.DocumentStatus == DocumentStatus.FINISH)
                    {
                        var listFile = await _dataContext.DocumentFile.Where(x => x.DocumentId == item).OrderBy(x => x.Order).ThenBy(x => x.CreatedDate).ToListAsync();

                        #region Gửi mail thông báo đến từng người theo file
                        if (model.IsSendPrivateMail && !string.IsNullOrEmpty(entity.Email))
                        {
                            string title = "[SAVIS eContract] -  Giải pháp hợp đồng điện tử";

                            StringBuilder htmlBody = new StringBuilder();
                            htmlBody.Append("<html><body>");
                            htmlBody.Append("<p>Xin chào <b>" + entity.FullName + "</b>,</p>");
                            htmlBody.Append("<p>Bạn nhận được một hồ sơ đã ký hoàn chỉnh!</p>");
                            htmlBody.Append("<p>Chi tiết tại tệp đính kèm.</p>");
                            foreach (var file in listFile)
                            {
                                var fileUrl = await ms.GetObjectPresignUrlAsync(file.FileBucketName, file.FileObjectName);
                                //var fileUrl = $"{minio_service_url}api/v1/core/minio/download-object?bucketName={file.FileBucketName}&objectName={file.FileObjectName}";
                                htmlBody.Append("<p>- <a href='" + fileUrl + "' target='_blank'>" + file.FileName + "</a> </p>");
                            }
                            htmlBody.Append("</body></html>");

                            _emailHandler.SendMailWithConfig(new List<string>() { entity.Email }, null, null, title, htmlBody.ToString(), _orgConf);
                        }
                        #endregion

                        #region Gửi email thông báo đến địa chỉ các email
                        if (model.IsSendMuiltipleMail)
                        {
                            foreach (var email in model.ListEmail)
                            {
                                string title = "[SAVIS eContract] -  Giải pháp hợp đồng điện tử";
                                StringBuilder htmlBody = new StringBuilder();
                                htmlBody.Append("<html><body>");
                                htmlBody.Append("<p>Xin chào <b>" + email.Name + "</b>,</p>");
                                htmlBody.Append("<p>Bạn nhận được một hồ sơ đã ký hoàn chỉnh!</p>");
                                htmlBody.Append("<p>Chi tiết tại tệp đính kèm.</p>");
                                foreach (var file in listFile)
                                {
                                    var fileUrl = await ms.GetObjectPresignUrlAsync(file.FileBucketName, file.FileObjectName);
                                    //var fileUrl = $"{minio_service_url}api/v1/core/minio/download-object?bucketName={file.FileBucketName}&objectName={file.FileObjectName}";
                                    htmlBody.Append("<p>- <a href='" + fileUrl + "' target='_blank'>" + file.FileName + "</a> </p>");
                                }
                                htmlBody.Append("</body></html>");

                                _emailHandler.SendMailWithConfig(new List<string>() { email.Email }, null, null, title, htmlBody.ToString(), _orgConf);
                            }
                        }
                        #endregion
                    }
                }
                return new ResponseObject<bool>(true, "Gửi mail thành công", Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Gửi mail thất bại - {ex.Message}");
            }
        }

        public async Task<Response> SendMailToUserSign(Guid? docId, SystemLogModel systemLog)
        {
            try
            {
                var document = await _dataContext.Document.FindAsync(docId.Value);
                if (document != null)
                {
                    document.OneTimePassCode = Utils.GenerateNewRandom();
                    document.PassCodeExpireDate = dateNow.AddDays(3);
                    _dataContext.Document.Update(document);

                    #region Lưu lịch sử khi hợp đồng thay đổi
                    await CreateDocumentWFLHistory(document);
                    #endregion

                    int result = _dataContext.SaveChanges();

                    if (result > 0)
                    {
                        var user = await _dataContext.User.FindAsync(document.NextStepUserId);

                        if (user == null)
                        {
                            return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin người dùng cần thực hiện ký");
                        }

                        if (string.IsNullOrEmpty(user.Email))
                        {
                            return new ResponseObject<bool>(false, "Người dùng không được đăng ký email", Code.Success);
                        }

                        #region Gửi thông báo qua gateway

                        #region Lấy ra đơn vị gốc
                        OrganizationModel orgRootModel = new OrganizationModel();
                        if (user.OrganizationId.HasValue)
                        {
                            var rootOrg = await _organizationHandler.GetRootByChidId(document.OrganizationId.Value);
                            if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                            {
                                orgRootModel = orgRoot.Data;
                            }
                        }
                        #endregion

                        var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);

                        var lsToken = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

                        var rs = await _notifyHandler.SendNotificationRemindSignDocumentByGateway(new NotificationRemindSignDocumentModel()
                        {
                            TraceId = systemLog.TraceId,
                            OraganizationCode = orgRootModel.Code,
                            User = new NotifyUserModel()
                            {
                                UserName = user.UserName,
                                Email = user.Email,
                                FullName = user.Name,
                                ListToken = lsToken.Select(x => x.FirebaseToken).ToList()
                            },
                            Document = new GatewayNotifyDocumentModel()
                            {
                                Code = document.Code,
                                Name = document.Name,
                                DocumentTypeCode = documentType?.Code,
                                OneTimePassCode = document.OneTimePassCode,
                                Url = Utils.GetConfig("Web:SignPageUrl:uri") + document.Code,
                            }
                        }, systemLog).ConfigureAwait(false);

                        return rs;

                        #endregion
                        //string title = "[eContract] -  Giải pháp hợp đồng điện tử";
                        //var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
                        //{
                        //    UserName = user.Name,
                        //    DocumentName = document.Name,
                        //    DocumentCode = document.Code,
                        //    DocumentUrl = Utils.GetConfig("Web:SignPageUrl:uri") + document.Code,
                        //    OTP = document.OneTimePassCode
                        //});

                        //_emailHandler.SendMailGoogle(new List<string>() { user.Email }, null, null, title, body);
                    }
                    else
                    {
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi lưu dữ liệu.");
                    }
                }

                return new ResponseObject<bool>(true, "Gửi thông báo thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Gửi mail thất bại - {ex.Message}");
            }
        }

        public async Task<Response> SendMailToUserSignWithConfig(DocumentSendNotify model, SystemLogModel systemLog)
        {
            try
            {
                var document = await _dataContext.Document.FindAsync(model.Id);
                if (document != null)
                {
                    document.OneTimePassCode = Utils.GenerateNewRandom();
                    document.PassCodeExpireDate = dateNow.AddDays(3);
                    _dataContext.Document.Update(document);

                    int result = _dataContext.SaveChanges();

                    if (result > 0)
                    {
                        var user = await _dataContext.User.FindAsync(document.NextStepUserId);

                        if (user == null)
                        {
                            return new ResponseError(Code.NotFound, $"Không tìm thấy thông tin người dùng cần thực hiện ký");
                        }

                        #region Gửi thông báo qua gateway

                        #region Lấy ra đơn vị gốc
                        OrganizationModel orgRootModel = new OrganizationModel();
                        if (user.OrganizationId.HasValue)
                        {
                            var rootOrg = await _organizationHandler.GetRootByChidId(document.OrganizationId.Value);
                            if (rootOrg.Code == Code.Success && rootOrg is ResponseObject<OrganizationModel> orgRoot)
                            {
                                orgRootModel = orgRoot.Data;
                            }
                        }
                        #endregion

                        var documentType = await _dataContext.DocumentType.FindAsync(document.DocumentTypeId);

                        var lsToken = await _dataContext.UserMapFirebaseToken.AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();

                        if (model.NotifyConfigId.HasValue)
                        {
                            var notify = await _dataContext.NotifyConfig.Where(x => x.Id == model.NotifyConfigId.Value).FirstOrDefaultAsync();
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

                            await _notifyHandler.SendNotificationFromNotifyConfig(new NotificationConfigModel()
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
                                        { "NotifyType", notify.NotifyType.ToString() }
                                    }
                            });
                        }
                        else
                        {
                            var rs = await _notifyHandler.SendNotificationRemindSignDocumentByGateway(new NotificationRemindSignDocumentModel()
                            {
                                TraceId = systemLog.TraceId,
                                OraganizationCode = orgRootModel.Code,
                                User = new NotifyUserModel()
                                {
                                    UserName = user.UserName,
                                    PhoneNumber = user.PhoneNumber,
                                    Email = user.Email,
                                    FullName = user.Name,
                                    ListToken = lsToken.Select(x => x.FirebaseToken).ToList()
                                },
                                Document = new GatewayNotifyDocumentModel()
                                {
                                    Code = document.Code,
                                    Name = document.Name,
                                    DocumentTypeCode = documentType?.Code,
                                    OneTimePassCode = document.OneTimePassCode,
                                    Url = Utils.GetConfig("Web:SignPageUrl:uri") + document.Code,
                                }
                            }, systemLog).ConfigureAwait(false);

                            return rs;
                        }
                        #endregion
                        //string title = "[eContract] -  Giải pháp hợp đồng điện tử";
                        //var body = _emailHandler.GenerateDocumentEmailBody(new GenerateEmailBodyModel()
                        //{
                        //    UserName = user.Name,
                        //    DocumentName = document.Name,
                        //    DocumentCode = document.Code,
                        //    DocumentUrl = Utils.GetConfig("Web:SignPageUrl:uri") + document.Code,
                        //    OTP = document.OneTimePassCode
                        //});

                        //_emailHandler.SendMailGoogle(new List<string>() { user.Email }, null, null, title, body);
                    }
                    else
                    {
                        return new ResponseError(Code.ServerError, $"Có lỗi xảy ra khi lưu dữ liệu.");
                    }
                }

                return new ResponseObject<bool>(true, "Gửi thông báo thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Gửi mail thất bại - {ex.Message}");
            }
        }

        public async Task<Response> SendNotify(Guid documentId, Guid notifyConfigId, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                var document = await _dataContext.Document.FindAsync(documentId);
                var user = await _dataContext.User.FindAsync(userId);
                var notifyConfig = await _dataContext.NotifyConfig.FindAsync(notifyConfigId);

                var data = new
                {
                    userFullName = user.Name,
                    documentCode = document.Code,
                    documentName = document.Name,
                    expireTime = document.SignExpireAtDate.HasValue ? document.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                    expireDate = document.SignExpireAtDate.HasValue ? document.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                };

                var contents = Utils.ReplaceContentNotify(data, notifyConfig.EmailTitleTemplate, notifyConfig.EmailBodyTemplate);
                _emailHandler.SendMailGoogle(new List<string> { user.Email }, null, null, contents[0], contents[1]);

                return new ResponseObject<bool>(true, "Gửi mail thành công", Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Gửi mail thất bại - {ex.Message}");
            }
        }

        private void SendMailRemindSign(string fromEmail, string toEmail, string userName, SystemLogModel systemLog)
        {
            try
            {
                string title = "[SAVIS eContract] -  Giải pháp hợp đồng điện tử";

                StringBuilder htmlBody = new StringBuilder();
                htmlBody.Append("<html><body>");
                htmlBody.Append("<p>Xin chào <b>" + userName + "</b>,</p>");
                htmlBody.Append("<p>Bạn có một yêu cầu ký hồ sơ từ " + fromEmail + ".</p>");
                htmlBody.Append("<p>Để xem hồ sơ và ký, bạn thực hiện theo các bước sau:</p>");
                htmlBody.Append("<p><b>Bước 1:</b> Nhấn chọn <a hreft='" + portalWebUrl + "' target='_blank'>Xem hồ sơ</a> để Xem thông tin hồ sơ cần ký. </p>");
                htmlBody.Append("<p><b>Bước 2:</b> Đăng nhập bằng tài khoản đã được cấp.</p>");
                htmlBody.Append("<p><b>Bước 3:</b> Xem hồ sơ và ký.</p>");
                htmlBody.Append("</body></html>");

                _emailHandler.SendMailGoogle(new List<string>() { toEmail }, null, null, title, htmlBody.ToString());

                //CallApiSendMail(toEmail, title, htmlBody);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
            }
        }

        public async Task<Response> GetByListId(List<Guid> listId, Guid userId, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.Document
                            join batch in _dataContext.DocumentBatch on item.DocumentBatchId equals batch.Id
                            join type in _dataContext.DocumentType on batch.DocumentTypeId equals type.Id
                            join workflowUserSign in _dataContext.WorkflowUserSign.AsNoTracking() on item.NextStepId equals workflowUserSign.Id into wkTemp
                            from workflowUserSign in wkTemp.DefaultIfEmpty()
                            where listId.Contains(item.Id)
                            select new DocumentModel()
                            {
                                Id = item.Id,
                                DocumentBatchCode = batch.Code,
                                DocumentBatchName = batch.Name,
                                Code = item.Code,
                                Name = item.Name,
                                Status = item.Status,
                                DocumentBatchId = item.DocumentBatchId,
                                DocumentTypeId = batch.DocumentTypeId,
                                DocumentTypeName = type.Name,
                                WorkflowId = item.WorkflowId,
                                CreatedDate = item.CreatedDate,
                                DocumentStatus = item.DocumentStatus,
                                Email = item.Email,
                                FullName = item.FullName,
                                NextStepId = item.NextStepId,
                                NextStepUserId = item.NextStepUserId,
                                NextStepSignType = item.NextStepSignType,
                                NextStepUserName = item.NextStepUserName,
                                NextStepUserEmail = item.NextStepUserEmail,
                                WorkFlowUserJson = item.WorkFlowUserJson,
                                IsSign = item.NextStepUserId == userId,
                                IsDeleted = item.IsDeleted,
                                ADSSProfileName = workflowUserSign != null ? workflowUserSign.ADSSProfileName : null,
                                SignType = workflowUserSign != null ? (int?)workflowUserSign.Type : null,
                                SignCloseAtDate = item.SignCloseAtDate,
                                IsCloseDocument = (item.DocumentStatus.Equals(DocumentStatus.PROCESSING) && item.SignCloseAtDate.HasValue && item.SignCloseAtDate.Value < dateNow)
                            });

                var listResult = await data.ToListAsync();

                var listDocumenFile = await _dataContext.DocumentFile
                    .Where(x => listId.Contains(x.DocumentId))
                    .OrderBy(x => x.Order)
                    .Select(x => new DocumentFileModel()
                    {
                        Id = x.Id,
                        FileBucketName = x.FileBucketName,
                        Order = x.Order,
                        CreatedDate = x.CreatedDate,
                        FileName = x.FileName,
                        FileObjectName = x.FileObjectName,
                        FileUrl = "",
                        ProfileName = x.ProfileName,
                        DocumentId = x.DocumentId
                    }).ToListAsync();

                var ms = new MinIOService();

                foreach (var item in listDocumenFile)
                {
                    item.FileUrl = await ms.GetObjectPresignUrlAsync(item.FileBucketName, item.FileObjectName);
                }

                foreach (var item in listResult)
                {
                    item.ListDocumentFile = listDocumenFile.Where(x => x.DocumentId == item.Id).ToList();

                    //systemLog.ListAction.Add(new ActionDetail()
                    //{
                    //    Description = $"Xem chi tiết/tải xuống hợp đồng {item.Code} từ eContract-portal",
                    //    ObjectCode = CacheConstants.DOCUMENT,
                    //    ObjectId = item.Id.ToString()
                    //});
                }

                return new ResponseObject<List<DocumentModel>>(listResult, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private async Task<WorkflowModel> GetWorkFlowDetailByCode(string code, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheWFKey(code);
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Workflow
                        .FirstOrDefaultAsync(x => x.Code == code && x.IsDeleted == false);

                    if (entity == null)
                    {
                        return null;
                    }

                    var model = AutoMapperUtils.AutoMap<Workflow, WorkflowModel>(entity);

                    model.ListUser = await
                        (from wfc in _dataContext.WorkflowUserSign.Where(x => x.WorkflowId == entity.Id)
                         join user in _dataContext.User on wfc.UserId equals user.Id into gj
                         from user in gj.DefaultIfEmpty()
                         join state in _dataContext.WorkflowState on wfc.StateId equals state.Id into gstate
                         from state in gstate.DefaultIfEmpty()
                         orderby wfc.Order
                         select new WorkflowUserModel()
                         {
                             Id = wfc.Id,
                             UserId = wfc.UserId,
                             UserName = user.UserName,
                             UserEmail = user.Email,
                             Name = user.Name,
                             StateId = state.Id,
                             State = state.Code,
                             StateName = state.Name,
                             SignExpireAfterDay = wfc.SignExpireAfterDay,
                             UserPhoneNumber = user.PhoneNumber,
                             UserPositionName = user.PositionName,
                             Type = wfc.Type,
                             UserFullName = user.Name,
                             UserConnectId = user.ConnectId,
                             SignCloseAfterDay = wfc.SignCloseAfterDay
                             //PositionId = contact.PositionId
                         }).ToListAsync();

                    return model;
                });
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        private async Task<OrgAndUserConnectInfo> GetOrgAndUserConnectInfo(OrgAndUserConnectInfoRequestModel model, SystemLogModel systemLog)
        {
            try
            {
                Organization entity;

                if (model.CustomOrganizationId.HasValue)
                {
                    entity = await _dataContext.Organization
                    .FirstOrDefaultAsync(x => x.Id == model.CustomOrganizationId);
                }
                else
                {
                    entity = await _dataContext.Organization
                    .FirstOrDefaultAsync(x => x.Id == model.OrganizationId);
                }

                if (entity == null)
                {
                    return null;
                }

                var user = await _dataContext.User.Where(x => x.OrganizationId == entity.Id && x.UserName.EndsWith(".admin")).OrderBy(x => x.CreatedDate).FirstOrDefaultAsync();

                //if (user == null)
                //{
                //    return null;
                //}

                OrganizationForServiceModel org = new OrganizationForServiceModel()
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Name = entity.Name,
                    UserId = user != null ? user.Id : Guid.Empty,
                    UserName = user != null ? user.UserName : "",
                };

                var lsUserConnectId = new List<string>();
                foreach (var item in model.ListUserConnectId)
                {
                    lsUserConnectId.Add(item.ToLower());
                }

                var listUserInfo = new List<User>();

                // lấy all theo connectId

                List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(model.OrganizationId);

                var listUserInfoConnect = _dataContext.User.Where(x => x.OrganizationId.HasValue
                    && listChildOrgID.Contains(x.OrganizationId.Value)
                    && lsUserConnectId.Contains(x.ConnectId.ToLower()));

                //var listUserInfoConnectChildOrg = listUserInfoConnect.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value));
                //if (await listUserInfoConnectChildOrg.AnyAsync())
                //{
                //    listUserInfo.AddRange(listUserInfoConnectChildOrg);
                //}

                // lấy theo phòng ban con
                //var listUserInfoConnectChildOrg = listUserInfoConnect.Where(x => x.OrganizationId.Value.Equals(model.CustomOrganizationId));
                //if (await listUserInfoConnectChildOrg.AnyAsync())
                //{
                //    listUserInfo.AddRange(listUserInfoConnectChildOrg);
                //}

                // lấy theo phòng ban cấp trên không bao gồm connectid đã lấy được theo phòng ban con ở trên
                //var listUserInfoConnectParentOrg = listUserInfoConnect.Where(x => x.OrganizationId.Value.Equals(model.OrganizationId)
                //    && !listUserInfo.Select(x1 => x1.ConnectId.ToLower()).Contains(x.ConnectId.ToLower()));
                //if (await listUserInfoConnectParentOrg.AnyAsync())
                //{
                //    listUserInfo.AddRange(listUserInfoConnectParentOrg);
                //}

                //// nếu cả phòng ban con và phòng ban cha không có user connect lấy tất cả user theo connect id
                //if (listUserInfo.Count <= 0)
                //{
                //    listUserInfo.AddRange(listUserInfoConnect);
                //}

                var listUserInfoModel = listUserInfoConnect.Select(x => new UserConnectInfoModel()
                {
                    UserId = x.Id,
                    UserConnectId = x.ConnectId,
                    UserName = x.UserName,
                    UserEmail = x.Email,
                    UserFullName = x.Name,
                    UserPhoneNumber = x.PhoneNumber,
                    OrganizationId = x.OrganizationId
                }).ToList();

                //var listOrg = new List<Guid?>() { model.CustomOrganizationId, model.OrganizationId };
                //var listUserInfo = await _dataContext.User.Where(x => lsUserConnectId.Contains(x.ConnectId.ToLower())
                //    && listOrg.Contains(x.OrganizationId))
                //                .Select(x => new UserConnectInfoModel()
                //                {
                //                    UserId = x.Id,
                //                    UserConnectId = x.ConnectId,
                //                    UserName = x.UserName,
                //                    UserEmail = x.Email,
                //                    UserFullName = x.Name,
                //                    UserPhoneNumber = x.PhoneNumber
                //                }).ToListAsync();

                var response = new OrgAndUserConnectInfo()
                {
                    OrganizationInfo = org,
                    ListUserConnectInfo = listUserInfoModel
                };

                return response;
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - { MessageConstants.ErrorLogMessage} {ex.Message}");
                throw ex;
            }
        }

        private string BuildCacheWFKey(string id)
        {
            return $"{CacheConstants.WORKFLOW}-{id}";
        }

        public async Task CreateDocumentWFLHistory(NetCore.Data.Document document, MemoryStream memoryStream = null)
        {
            return;

            //TODO: Kiểm tra lại logic dữ liệu của hàm này
            #region Lịch sử document
            var documentWFLHistory = new DocumentWorkflowHistory
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                State = document.State,
                DocumentStatus = document.DocumentStatus,
                Description = document.Description,
                ReasonReject = document.LastReasonReject,
                CreatedDate = dateNow
            };
            #endregion

            #region Lịch sử document file
            var listDocumentFile = _dataContext.DocumentFile.AsNoTracking().Where(r => r.DocumentId.Equals(document.Id)).ToList();
            MinIOService minIOService = new MinIOService();
            var checkMemory = false;
            if (memoryStream != null)
            {
                checkMemory = true;
            }

            documentWFLHistory.ListDocumentFile = new List<DocumentFileWorkflowHistory>();
            DocumentFileWorkflowHistory documentFileWFLHistory;
            foreach (var docFile in listDocumentFile)
            {
                if (!checkMemory)
                {
                    memoryStream = await minIOService.DownloadObjectAsync(docFile.FileBucketName, docFile.FileObjectName);
                }
                documentFileWFLHistory = new DocumentFileWorkflowHistory
                {
                    DocumentFileId = docFile.Id,
                    BucketName = docFile.FileBucketName,
                    ObjectName = docFile.FileObjectName,
                    FileName = docFile.FileName,
                    HashSHA256 = SHA256Convert.ConvertMemoryStreamToSHA256(memoryStream)
                };

                documentWFLHistory.ListDocumentFile.Add(documentFileWFLHistory);
            }

            _dataContext.DocumentWorkflowHistory.Add(documentWFLHistory);
            #endregion
        }

        public async Task<Response> GetDocumentWFLHistory(Guid docId, SystemLogModel systemLog)
        {
            try
            {
                //Log.Information($"{systemLog.TraceId} - GetDocumentWFLHistory: {docId}");
                var data = await _dataContext.DocumentWorkflowHistory.AsNoTracking()
                            .Where(r => r.DocumentId.Equals(docId))
                            .Select(r => new
                            {
                                r.Id,
                                r.DocumentId,
                                r.DocumentStatus,
                                r.State,
                                r.ReasonReject,
                                r.Description,
                                r.ListDocumentFileJson,
                                r.ListDocumentFile,
                                r.CreatedDate
                            }).ToListAsync<object>();

                return new ResponseObject<List<object>>(data, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - { MessageConstants.ErrorLogMessage} {ex.Message}");
                throw ex;
            }
        }

        public async Task<Response> GetMaxExpiredAfterDayByListDocumentId(List<Guid> docIds, SystemLogModel systemLog)
        {
            try
            {
                var data = (from doc in _dataContext.Document
                            join wfUserSign in _dataContext.WorkflowUserSign on doc.NextStepId equals wfUserSign.Id into gj1
                            from wfUserSign in gj1.DefaultIfEmpty()
                            where docIds.Contains(doc.Id) && wfUserSign.SignExpireAfterDay.HasValue
                            select new DocumentModel()
                            {
                                SignExpireAfterDay = wfUserSign.SignExpireAfterDay
                            });

                var maxSignExpireAfterDay = await data.MaxAsync(x => x.SignExpireAfterDay.Value);

                return new ResponseObject<int>(maxSignExpireAfterDay, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error($"{systemLog.TraceId} - { MessageConstants.ErrorLogMessage} {ex.Message}");
                throw ex;
            }
        }

        #region Api cho Mobile App
        public async Task<Response> GetListDocumentFrom3rd(DocumentQueryFilterMobileApp model, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.Document.AsNoTracking()
                            join type in _dataContext.DocumentType.AsNoTracking() on item.DocumentTypeId equals type.Id into gj2
                            from type in gj2.DefaultIfEmpty()
                            where !item.IsDeleted && item.Status
                                && (item.UserId == model.CurrentUserId || item.CreatedUserId == model.CurrentUserId || item.WorkFlowUserJson.Contains(model.CurrentUserId.ToString()))
                                && (!item.SignExpireAtDate.HasValue || item.SignExpireAtDate.HasValue && item.SignExpireAtDate >= DateTime.Now)
                                && (!item.SignCloseAtDate.HasValue || item.SignCloseAtDate.HasValue && item.SignCloseAtDate >= DateTime.Now)
                                && (string.IsNullOrEmpty(model.DocumentTypeCode) || type.Code == model.DocumentTypeCode)
                                && type.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS && type.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                            select new DocumentBaseModelMobileApp()
                            {
                                DocumentCode = item.Code,
                                DocumentName = item.Name,
                                DocumentTypeName = type == null ? "" : type.Name,
                                DocumentTypeCode = type == null ? "" : type.Code,
                                CreatedDate = item.CreatedDate,
                                DocumentStatus = item.DocumentStatus,
                                IsSign = item.NextStepUserId == model.CurrentUserId,
                                SignExpireAtDate = item.SignExpireAtDate,
                                State = item.State
                            });

                if (!model.IsSign.HasValue)
                {
                    data = data.Where(x => x.IsSign == true);
                }

                if (model.IsSign.HasValue)
                {
                    data = data.Where(x => x.IsSign == model.IsSign);
                }

                int totalCount = data.Count();

                // Pagination
                if (model.PageSize.HasValue && model.PageNumber.HasValue)
                {
                    if (model.PageSize <= 0)
                    {
                        model.PageSize = QueryFilter.DefaultPageSize;
                    }

                    //Calculate nunber of rows to skip on pagesize
                    int excludedRows = (model.PageNumber.Value - 1) * (model.PageSize.Value);
                    if (excludedRows <= 0)
                    {
                        excludedRows = 0;
                    }

                    // Query
                    data = data.Skip(excludedRows).Take(model.PageSize.Value);
                }

                int dataCount = data.Count();
                var listResult = await data.ToListAsync();

                return new ResponseObject<PaginationList<DocumentBaseModelMobileApp>>(new PaginationList<DocumentBaseModelMobileApp>()
                {
                    DataCount = dataCount,
                    TotalCount = totalCount,
                    PageNumber = model.PageNumber ?? 0,
                    PageSize = model.PageSize ?? 0,
                    Data = listResult
                }, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
        #endregion

        public async Task<Response> GetListDocumentByListUser(List<Guid> userIds, SystemLogModel systemLog)
        {
            try
            {
                //var documents = await _dataContext.Document
                //   .Where(x => x.UserId.HasValue && userIds.Contains(x.UserId.Value))
                //   .Select(x => new DocumentByListUserInfoModel()
                //   {
                //       DocumentId = x.Id,
                //       DocumentCode = x.Code,
                //       UserId = x.UserId.Value
                //   })
                //   .ToListAsync();

                var documents = await (from item in _dataContext.Document.AsNoTracking()

                                       join type in _dataContext.DocumentType.AsNoTracking() on item.DocumentTypeId equals type.Id into gj2
                                       from type in gj2.DefaultIfEmpty()

                                       where !item.IsDeleted
                                           && item.UserId.HasValue && userIds.Contains(item.UserId.Value)
                                           && type.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS && type.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                                       select new DocumentByListUserInfoModel()
                                       {
                                           DocumentId = item.Id,
                                           DocumentCode = item.Code,
                                           UserId = item.UserId.Value,
                                           CreatedDate = item.CreatedDate
                                       }).ToListAsync();

                var documentsUser = documents.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.OrderByDescending(c => c.CreatedDate).ToList());

                var listDocumentByLstUser = new List<DocumentByListUserModel>();
                foreach (var dic in documentsUser)
                {
                    var item = new DocumentByListUserModel();
                    item.UserId = dic.Key;
                    item.Documents = dic.Value;

                    listDocumentByLstUser.Add(item);
                }

                return new ResponseObject<List<DocumentByListUserModel>>(listDocumentByLstUser, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GenerateImagePreview(List<Guid> listDocumentId, SystemLogModel systemLog)
        {
            try
            {
                var minIO = new MinIOService();
                var listDocumentFile = await _dataContext.DocumentFile.Where(x => listDocumentId.Contains(x.DocumentId)).ToListAsync();
                if (listDocumentFile == null || listDocumentFile.Count < 1) return new ResponseError(Code.BadRequest, "Không tìm thấy file hợp đồng.");

                foreach (var item in listDocumentFile)
                {
                    var fileDocStream = await minIO.DownloadObjectAsync(item.FileBucketName, item.FileObjectName);
                    var fileDocBase64 = Convert.ToBase64String(fileDocStream.ToArray());

                    PDFToImageService pdfToImageService = new PDFToImageService();
                    var pdf2img = await pdfToImageService.ConvertPDFBase64ToPNG(new PDFConvertPNGServiceModel()
                    {
                        FileBase64 = fileDocBase64
                    }, systemLog);

                    //Convert và tải file lên minio
                    byte[] bytes;
                    MemoryStream memory;
                    int i = 0;

                    item.ImagePreview = new List<ImagePreview>();

                    foreach (var img in pdf2img)
                    {
                        i++;
                        bytes = Convert.FromBase64String(img);
                        memory = new MemoryStream(bytes);
                        var rs = await minIO.UploadObjectAsync(item.FileBucketName, item.FileObjectName.Replace(".pdf", "").Replace(".PDF", "") + $"_page{i}.png", memory, false);
                        item.ImagePreview.Add(new ImagePreview()
                        {
                            BucketName = rs.BucketName,
                            ObjectName = rs.ObjectName
                        });
                    }
                }

                _dataContext.DocumentFile.UpdateRange(listDocumentFile);
                var dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    return new ResponseObject<bool>(true, "Generate thành công.", Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateDocumentEVerify(EVerifyDocumentRequest model)
        {
            try
            {
                var documentIds = model.EVerifyDocuments.Select(x => x.DocumentId);
                var documents = await _dataContext.Document.Where(x => documentIds.Contains(x.Id)).ToListAsync();

                var documentUpdates = new List<Document>();
                foreach (var item in model.EVerifyDocuments)
                {
                    var document = documents.FirstOrDefault(x => x.Id == item.DocumentId);

                    if (document != null)
                    {
                        document.IsVerified = true;
                        document.VerifyCode = item.VerificationCode;
                        document.VerifyDate = DateTime.Now;
                        
                        documentUpdates.Add(document);
                    }
                }
                
                _dataContext.Document.UpdateRange(documentUpdates);
                await _dataContext.SaveChangesAsync();

                return new Response("Cập nhật eVerify thành công");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{model.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, "Lỗi khi cập nhập eVerify Document");
            }
        }

        public async Task<Response> GetUserInWorkflowByListDocument(GetUserInWorkflowInListDocumentIdModel model, SystemLogModel systemLog)
        {
            try
            {
                var wfUserModels = new List<WorkflowUserModel>();
                var documents = await _dataContext.Document.Where(x => model.DocumentIds.Contains(x.Id)).ToListAsync();

                var listUser = await _userHandler.GetListUserFromCache();
                foreach (var item in documents)
                {
                    var user = listUser.FirstOrDefault(x => x.Id == item.UserId);

                    var wfUsers = await _dataContext.WorkflowUserSign.Where(x => x.WorkflowId == item.WorkflowId).ToListAsync();
                    wfUsers[0].UserId = item.UserId;
                    wfUsers[0].UserName = user.Name;

                    wfUsers.ForEach(x =>
                    {
                        var wfUserModel = AutoMapperUtils.AutoMap<WorkflowUserSign, WorkflowUserModel>(x);
                        if (!wfUserModels.Any(x => x.UserId == wfUserModel.UserId))
                        {
                            wfUserModels.Add(wfUserModel);
                        }
                    });
                }

                return new ResponseObject<List<WorkflowUserModel>>(wfUserModels, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
    }
}