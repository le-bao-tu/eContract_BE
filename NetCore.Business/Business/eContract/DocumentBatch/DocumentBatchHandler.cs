using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class DocumentBatchHandler : IDocumentBatchHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.DOCUMENT_BATCH;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "BATCH.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly IDocumentHandler _documentHandler;
        private readonly IDocumentTemplateHandler _docTempHandler;
        //[Obsolete]
        //private IHostingEnvironment environment;

        //[Obsolete]
        //public DocumentBatchHandler(DataContext dataContext, ICacheService cacheService, IHostingEnvironment _environment)

        private DateTime dateNow = DateTime.Now;

        public DocumentBatchHandler(DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler, IDocumentHandler documentHandler, IDocumentTemplateHandler documentTemplateHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
            _documentHandler = documentHandler;
            _docTempHandler = documentTemplateHandler;
            //environment = _environment;
        }

        public async Task<Response> Create(DocumentBatchCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_BATCH}: {JsonSerializer.Serialize(model)}");
                var entity = AutoMapperUtils.AutoMap<DocumentBatchCreateModel, DocumentBatch>(model);

                var docType = await _dataContext.DocumentType.FirstOrDefaultAsync(x => x.Id == model.DocumentTypeId);

                if (docType == null)
                {
                    return new ResponseError(Code.ServerError, "Không tìm thấy loại hợp đồng trong hệ thống!");
                }

                entity.CreatedDate = DateTime.Now;

                if (string.IsNullOrEmpty(model.Code))
                {
                    long identityNumber = 0;
                    identityNumber = await _dataContext.DocumentBatch.Where(x => x.DocumentTypeId == model.DocumentTypeId).CountAsync();

                    entity.IdentityNumber = ++identityNumber;
                    entity.Code = Utils.GenerateAutoCode(docType.Code + ".", identityNumber);
                }

                var checkCode = await _dataContext.DocumentBatch.AnyAsync(x => x.Code == model.Code && x.IsGenerateFile == true);
                if (checkCode)
                {
                    if (!string.IsNullOrEmpty(model.Code))
                    {
                        return new ResponseError(Code.ServerError, "Mã lô hợp đồng đã tồn tại trong hệ thống!");
                    }
                    else
                    {
                        return new ResponseError(Code.ServerError, "Sinh mã lô hợp đồng thất bại, vui lòng thực hiện lại!");
                    }
                }
                entity.Name = string.IsNullOrEmpty(entity.Name) ? entity.Code : entity.Name;

                entity.Id = Guid.NewGuid();
                await _dataContext.DocumentBatch.AddAsync(entity);

                int count = 1;
                foreach (var item in model.ListFile)
                {
                    await _dataContext.DocumentBatchFile.AddAsync(new DocumentBatchFile()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now,
                        DocumentBatchId = entity.Id,
                        FileBucketName = item.FileBucketName,
                        FileObjectName = item.FileObjectName,
                        FileName = item.FileName,
                        Order = count++,
                        DocumentFileTemplateId = item.DocumentFileTemplateId,
                        //ListMetaData = item.ListMetaData
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Tạo lô hợp đồng - {entity.Code}",
                        CreatedDate = DateTime.Now,
                        ObjectCode = CacheConstants.DOCUMENT_BATCH,
                        ObjectId = entity.Id.ToString(),
                    });

                    Log.Information($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_BATCH} success: {JsonSerializer.Serialize(entity)}");
                    InvalidCache();

                    var dt = await GenerateListDocument_v2(new DocumentBatchGenerateFileModel()
                    {
                        Id = entity.Id,
                        ApplicationId = entity.ApplicationId,
                        CreatedUserId = entity.CreatedUserId,
                        OrganizationId = model.OrganizationId
                    }, systemLog);
                    return dt;

                    //return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_BATCH} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(DocumentBatchUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CacheConstants.DOCUMENT_BATCH}: {JsonSerializer.Serialize(model)}");
                var entity = await _dataContext.DocumentBatch
                         .FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information($"{systemLog.TraceId} - Before Update: {JsonSerializer.Serialize(entity)}");

                model.UpdateToEntity(entity);

                _dataContext.DocumentBatch.Update(entity);

                var lsFile = _dataContext.DocumentBatchFile.Where(x => x.DocumentBatchId == entity.Id);

                _dataContext.DocumentBatchFile.RemoveRange(lsFile);

                int count = 1;
                foreach (var file in model.ListFile)
                {
                    await _dataContext.DocumentBatchFile.AddAsync(new DocumentBatchFile()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now,
                        DocumentBatchId = entity.Id,
                        FileObjectName = file.FileObjectName,
                        FileName = file.FileName,
                        FileBucketName = file.FileBucketName,
                        Order = count++,
                        DocumentFileTemplateId = file.DocumentFileTemplateId,
                        //ListMetaData = file.ListMetaData
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} After Update: {JsonSerializer.Serialize(entity)}");
                    InvalidCache(model.Id.ToString());
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập lô hợp đồng - {entity.Code}",
                        CreatedDate = DateTime.Now,
                        ObjectCode = CacheConstants.DOCUMENT_BATCH,
                        ObjectId = entity.Id.ToString(),
                    });

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CacheConstants.DOCUMENT_BATCH} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List Delete: {JsonSerializer.Serialize(listId)}");
                var listResult = new List<ResponeDeleteModel>();
                var name = "";
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.DocumentBatch.FindAsync(item);

                    if (entity == null)
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
                        _dataContext.DocumentBatch.Remove(entity);
                        var lsFile = _dataContext.DocumentBatchFile.Where(x => x.DocumentBatchId == entity.Id);
                        _dataContext.DocumentBatchFile.RemoveRange(lsFile);

                        try
                        {
                            int dbSave = await _dataContext.SaveChangesAsync();
                            if (dbSave > 0)
                            {
                                InvalidCache(item.ToString());

                                listResult.Add(new ResponeDeleteModel()
                                {
                                    Id = item,
                                    Name = name,
                                    Result = true,
                                    Message = MessageConstants.DeleteItemSuccessMessage
                                });

                                systemLog.ListAction.Add(new ActionDetail
                                {
                                    Description = $"Xóa lô hợp đồng - {entity.Code}",
                                    ObjectCode = entity.Code,
                                    ObjectId = entity.Id.ToString(),
                                    CreatedDate = DateTime.Now
                                });
                            }
                            else
                            {
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
                            Log.Error(ex, MessageConstants.ErrorLogMessage);
                            listResult.Add(new ResponeDeleteModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = ex.Message
                            });
                        }
                    }
                }
                Log.Information("List Result Delete: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(DocumentBatchQueryFilter filter)
        {
            try
            {
                var data = (from item in _dataContext.DocumentBatch
                            where item.IsGenerateFile == true
                            select new DocumentBatchBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                Description = item.Description,
                                Status = item.Status,
                                CreatedDate = item.CreatedDate,
                            });

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.Status.HasValue)
                {
                    data = data.Where(x => x.Status == filter.Status);
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
                return new ResponseObject<PaginationList<DocumentBatchBaseModel>>(new PaginationList<DocumentBatchBaseModel>()
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
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.DocumentBatch
                        .FirstOrDefaultAsync(x => x.Id == id);

                    var model = AutoMapperUtils.AutoMap<DocumentBatch, DocumentBatchModel>(entity);

                    var listFile = await _dataContext.DocumentBatchFile.Where(x => x.DocumentBatchId == id)
                    .OrderBy(x => x.Order)
                    .Select(x => new DocumentBatchFileModel()
                    {
                        Id = x.Id,
                        FileBucketName = x.FileBucketName,
                        FileObjectName = x.FileObjectName,
                        FileName = x.FileName,
                        DocumentFileTemplateId = x.DocumentFileTemplateId,
                    }).ToListAsync();

                    return model;
                });
                return new ResponseObject<DocumentBatchModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(int count = 0, string textSearch = "")
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.DocumentBatch.Where(x => x.Status == true && x.IsGenerateFile == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new SelectItemModel()
                                {
                                    Id = item.Id,
                                    Name = item.Name,
                                    Note = item.Code
                                });

                    return await data.ToListAsync();
                });

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<SelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GenerateListDocument_v2(DocumentBatchGenerateFileModel model, SystemLogModel systemLog)
        {
            try
            {
                int count = 1;
                var documentBatch = await _dataContext.DocumentBatch
                      .FirstOrDefaultAsync(x => x.Id == model.Id);
                if (!documentBatch.WorkflowId.HasValue)
                {
                    return new ResponseError(Code.ServerError, "Lô hợp đồng chưa chọn quy trình");
                }

                var orgInfo = await _dataContext.Organization.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.OrganizationId);
                OrganizationModel rootOrgInfo = null;
                if (orgInfo != null)
                {
                    rootOrgInfo = await _organizationHandler.GetRootOrgModelByChidId(orgInfo.Id);
                }

                var firtStepWF = documentBatch.WorkFlowUser.FirstOrDefault();
                var listFile = await _dataContext.DocumentBatchFile.Where(x => x.DocumentBatchId == model.Id).OrderBy(x => x.Order).ToListAsync();

                var listUserId = documentBatch.WorkFlowUser.Select(x => x.UserId).ToList();

                var dtUser = await _dataContext.User.Where(x => listUserId.Contains(x.Id)).ToListAsync();

                foreach (var item in documentBatch.WorkFlowUser)
                {
                    var userTemp = dtUser.Find(x => x.Id == item.UserId);
                    if (userTemp != null)
                    {
                        item.Name = userTemp.Name;
                        item.UserConnectId = userTemp.ConnectId;
                        item.UserEmail = userTemp.Email;
                        item.UserFullName = userTemp.Name;
                        item.UserName = userTemp.UserName;
                        item.UserPhoneNumber = userTemp.PhoneNumber;
                    }
                }

                documentBatch.WorkFlowUserJson = JsonSerializer.Serialize(documentBatch.WorkFlowUser);

                var docTempByDocType = await _dataContext.DocumentTemplate.FirstOrDefaultAsync(x => x.DocumentTypeId == documentBatch.DocumentTypeId);

                //Nếu là thêm mới bằng file pdf
                if (documentBatch.Type == 1)
                {
                    var firstDocumentTemplate = (await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode)).FirstOrDefault();

                    if (firstDocumentTemplate == null)
                    {
                        return new ResponseError(Code.ServerError, "Loại hợp đồng chưa được cấu hình biểu mẫu");
                    }
                    var fileTemplate = await _dataContext.DocumentFileTemplate.OrderBy(x => x.Order)
                            .FirstOrDefaultAsync(x => x.DocumentTemplateId == firstDocumentTemplate.Id);
                    foreach (var item in listFile)
                    {
                        var docCode = Utils.GenerateAutoCode(documentBatch.Code + "_", count);
                        var doc = new Data.Document()
                        {
                            Id = Guid.NewGuid(),
                            Code = docCode,
                            DocumentTypeId = documentBatch.DocumentTypeId,
                            Email = "",
                            FullName = "",
                            Name = item.FileName,
                            Status = true,
                            CreatedDate = DateTime.Now,
                            ApplicationId = model.ApplicationId,
                            CreatedUserId = model.CreatedUserId,
                            DocumentBatchId = model.Id,
                            WorkflowId = documentBatch.WorkflowId.Value,
                            Order = count++,
                            DocumentStatus = DocumentStatus.DRAFT,
                            WorkFlowUserJson = documentBatch.WorkFlowUserJson,
                            OrganizationId = documentBatch.OrganizationId,
                            BucketName = rootOrgInfo?.Code,
                            ObjectNameDirectory = $"{orgInfo?.Code}/{dateNow.Year}/{dateNow.Month}/{dateNow.Day}/{docCode}/",
                            FileNamePrefix = $"{orgInfo?.Code}.{docCode}"
                        };

                        await _dataContext.Document.AddAsync(doc);

                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = $"Tạo tài liệu theo file pdf - {doc.Code}",
                            CreatedDate = DateTime.Now,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = doc.Id.ToString(),
                        });

                        await _dataContext.DocumentFile.AddAsync(new DocumentFile()
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = doc.Id,
                            FileBucketName = item.FileBucketName,
                            FileObjectName = item.FileObjectName,
                            FileName = item.FileName,
                            ProfileName = fileTemplate.ProfileName,
                            DocumentFileTemplateId = fileTemplate.Id
                        });
                    }
                }
                // Nếu là thêm mới bằng file excel
                else if (documentBatch.Type == 2)
                {
                    //Lấy ra danh sách biểu mẫu thuộc loại hợp đồng
                    var listFileTemplate = new List<DocumentFileTemplate>();

                    // file template stream
                    var docTempValids = await _docTempHandler.CaculateActiveDocumentTemplateByGroupCode(docTempByDocType.GroupCode);
                    var listDocumentTemplate = await _dataContext.DocumentTemplate.Where(x => docTempValids.Select(x1 => x1.Id).Contains(x.Id)).ToListAsync();
                    var listFileStreamTemplate = await _documentHandler.GetFileTemplateDocument(listDocumentTemplate, listFileTemplate, systemLog);

                    //Mỗi danh sách meta data sẽ được Document
                    foreach (var metaData in documentBatch.ListMetaData)
                    {
                        var documentId = Guid.NewGuid();
                        //metaData.MetaData.Add(new MetaDataDocumentModel()
                        //{
                        //    MetaDataCode = MetaDataCodeConstants.DOC_ID,
                        //    Value = "Doc ID#: " + documentId.ToString()
                        //});

                        if (metaData.MetaData == null || metaData.MetaData.Count < 1) throw new Exception("Không tìm thấy Metadata.");

                        var metaDataDocID = metaData.MetaData.FirstOrDefault(x => x.MetaDataCode == MetaDataCodeConstants.DOC_ID);
                        if (metaDataDocID != null)
                        {
                            metaDataDocID.Value = "Doc ID#: " + documentId.ToString();
                        }

                        var docCode = Utils.GenerateAutoCode(documentBatch.Code + "_", count);

                        var docMetaData = metaData.MetaData.Select(x => new DocumentMetaData() { Key = x.MetaDataCode, Value = x.Value }).ToList();

                        var doc = new Data.Document()
                        {
                            Id = documentId,
                            Code = docCode,
                            Email = metaData.Email,
                            FullName = metaData.FullName,
                            DocumentTypeId = documentBatch.DocumentTypeId,
                            Name = metaData.FullName,
                            Status = true,
                            CreatedDate = DateTime.Now,
                            ApplicationId = model.ApplicationId,
                            CreatedUserId = model.CreatedUserId,
                            DocumentBatchId = model.Id,
                            WorkflowId = documentBatch.WorkflowId.Value,
                            Order = count++,
                            DocumentStatus = DocumentStatus.DRAFT,
                            WorkFlowUserJson = documentBatch.WorkFlowUserJson,
                            MetaData = docMetaData,
                            OrganizationId = documentBatch.OrganizationId,
                            BucketName = rootOrgInfo?.Code,
                            ObjectNameDirectory = $"{orgInfo?.Code}/{dateNow.Year}/{dateNow.Month}/{dateNow.Day}/{docCode}/",
                            FileNamePrefix = $"{orgInfo?.Code}.{docCode}"
                        };

                        await _dataContext.Document.AddAsync(doc);

                        systemLog.ListAction.Add(new ActionDetail()
                        {
                            Description = $"Tạo tài liệu theo meta data - {doc.Code}",
                            CreatedDate = DateTime.Now,
                            ObjectCode = CacheConstants.DOCUMENT,
                            ObjectId = doc.Id.ToString(),
                        });

                        foreach (var template in listDocumentTemplate)
                        {
                            //Lấy ra danh sách file thuộc biểu mẫu
                            var fileTemplate = listFileTemplate.Where(x => x.DocumentTemplateId == template.Id).ToList();

                            //Duyệt từng biểu mẫu và thêm vào danh sách template document
                            foreach (var file in fileTemplate)
                            {
                                // lấy ra templateStream
                                var fileTemplateStreamModel = listFileStreamTemplate.FirstOrDefault(x => x.Id == file.Id);

                                #region Xử lý dữ liệu

                                // Cấu hình Meta Data trên file pdf
                                var fileMetaDataConfig = file.MetaDataConfig;

                                // Giá trị của Meta data bên tên
                                var listMetaDataDraft = metaData;

                                var listMetaDataValue = new List<MetaDataFileValue>();

                                foreach (var config in fileMetaDataConfig)
                                {
                                    var meta = listMetaDataDraft.MetaData.Find(c => c.MetaDataCode == config.MetaDataCode);

                                    if (meta != null)
                                    {
                                        listMetaDataValue.Add(new MetaDataFileValue()
                                        {
                                            MetaDataId = meta.MetaDataId,
                                            MetaDataValue = meta.Value,
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


                                //Kiểm tra định dạng file
                                // File DOCX
                                if (file.FileType == TemplateFileType.DOCX)
                                {
                                    #region Gọi service thứ 3
                                    var listData = docMetaData.Select(x => new KeyValueModel()
                                    {
                                        Key = $"{DocumentTemplateConstants.KeyPrefix}{x.Key}{DocumentTemplateConstants.KeySubfix}",
                                        Value = x.Value
                                    }).ToList();

                                    Log.Information("List MetaData Convert: " + JsonSerializer.Serialize(listData));

                                    var fileBase64 = Base64Convert.ConvertMemoryStreamToBase64(fileTemplateStreamModel.FileTemplateStream);

                                    var dt = await ConvertPDF.ConvertDocxMetaDataToPDFAsync(new FileBase64Model()
                                    {
                                        FileName = file.FileName,
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

                                        //var fileName = System.IO.Path.GetFileNameWithoutExtension(file.FileName) + ".pdf";
                                        fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                                        //Convert PDF to PDF/A  
                                        ConvertPDF.ConvertToPDFA(ref memStream);
                                        var ms = new MinIOService();

                                        fileName = ms.RenameFile(fileName);

                                        MinIOFileUploadResult minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, memStream, false);

                                        await _dataContext.DocumentFile.AddAsync(new DocumentFile()
                                        {
                                            Id = Guid.NewGuid(),
                                            DocumentId = doc.Id,
                                            FileName = fileName,
                                            FileBucketName = minioRS.BucketName,
                                            FileObjectName = minioRS.ObjectName,
                                            ProfileName = "",
                                            CreatedDate = DateTime.Now,
                                            FileType = FILE_TYPE.PDF,
                                            DocumentFileTemplateId = file.Id,
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, $"{systemLog.TraceId}");
                                        // systemLog.Description = $"Lưu file vào server thất bại {item.FileName} - {ex.Message}";
                                        // await _sysLogHandler.Create(systemLog).ConfigureAwait(false);
                                        return new ResponseError(Code.ServerError, $"Không thể upload file {file.FileName} lên server");
                                    }
                                    #endregion
                                }
                                // Mặc định là file PDF
                                else
                                {
                                    var streamWriter = await _documentHandler.FillMetaDataToPdfWithIText7(listMetaDataValue, fileTemplateStreamModel.FileTemplateStream, systemLog);

                                    #region Send File to MinIO
                                    var fileName = doc.FileNamePrefix + ".pdf";
                                    try
                                    {
                                        var ms = new MinIOService();

                                        fileName = doc.ObjectNameDirectory + Utils.GetValidFileName(fileName);

                                        fileName = ms.RenameFile(fileName);

                                        //Convert PDF to PDF/A  
                                        var bytes = streamWriter.ToArray();

                                        var streamConvert = new MemoryStream(bytes);
                                        ConvertPDF.ConvertToPDFA(ref streamConvert);
                                        MinIOFileUploadResult minioRS = await ms.UploadDocumentAsync(doc.BucketName, fileName, streamConvert, false);

                                        await _dataContext.DocumentFile.AddAsync(new DocumentFile()
                                        {
                                            Id = Guid.NewGuid(),
                                            DocumentId = doc.Id,
                                            FileName = file.FileName,
                                            FileBucketName = minioRS.BucketName,
                                            FileObjectName = minioRS.ObjectName,
                                            ProfileName = file.ProfileName,
                                            DocumentFileTemplateId = file.Id
                                        });

                                        streamConvert.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "");
                                        return new ResponseError(Code.ServerError, $"Không thể upload file {file.FileName} lên server");
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                else
                {
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
                documentBatch.IsGenerateFile = true;

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information("GenerateListDocument success: " + JsonSerializer.Serialize(model));

                    return new ResponseObject<Guid>(model.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private void InvalidCache(string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                string cacheKey = BuildCacheKey(id);
                _cacheService.Remove(cacheKey);
            }

            string selectItemCacheKey = BuildCacheKey(SelectItemCacheSubfix);
            _cacheService.Remove(selectItemCacheKey);
        }

        private string BuildCacheKey(string id)
        {
            return $"{CachePrefix}-{id}";
        }

        //public iTextSharp.text.Font GetFont(string name)
        //{
        //    if (!FontFactory.IsRegistered(name))
        //    {
        //        var fontPath = environment.WebRootPath + "\\fonts\\" + name + ".ttf";
        //        FontFactory.Register(fontPath);
        //    }
        //    return FontFactory.GetFont(name, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        //}
    }
}