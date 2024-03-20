using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class DocumentTemplateHandler : IDocumentTemplateHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.DOCUMENT_TEMPLATE;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "APP.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly IOrganizationConfigHandler _organizationConfigHandler;
        private readonly IUserHandler _userHandler;
        private readonly IRoleHandler _roleHandler;
        private readonly IMetaDataHandler _metaDataHandler;

        public DocumentTemplateHandler(DataContext dataContext,
            ICacheService cacheService,
            IOrganizationHandler organizationHandler,
            IOrganizationConfigHandler organizationConfigHandler,
            IUserHandler userHandler,
            IMetaDataHandler metaDataHandler,
            IRoleHandler roleHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
            _organizationConfigHandler = organizationConfigHandler;
            _userHandler = userHandler;
            _roleHandler = roleHandler;
            _metaDataHandler = metaDataHandler;
        }

        public async Task<Response> Create(DocumentTemplateCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_TEMPLATE}: {JsonSerializer.Serialize(model)}");

                #region Kiểm tra số lượng biểu mẫu tối đa
                // Kiểm tra thông tin số lượng biểu mẫu của đơn vị
                var orgConfig = await _organizationConfigHandler.GetByOrgId(model.OrganizationId.Value);
                if (orgConfig == null)
                {
                    return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối!");
                }

                var maxDoumentTemplate = orgConfig.TemplatePerDocumentType;

                if (maxDoumentTemplate <= 0)
                {
                    return new ResponseError(Code.Forbidden, "Đơn vị chưa được cấu hình số lượng biểu mẫu trên loại hợp đồng");
                }

                var countTemplatePerDocType = await _dataContext.DocumentTemplate.Where(x => x.DocumentTypeId == model.DocumentTypeId).Select(x => x.GroupCode).Distinct().CountAsync();

                if (countTemplatePerDocType >= maxDoumentTemplate)
                {
                    return new ResponseError(Code.Forbidden, $"Số lượng biểu mẫu trên hợp đồng tối đa là {maxDoumentTemplate}!");
                }
                #endregion

                var entity = AutoMapperUtils.AutoMap<DocumentTemplateCreateModel, DocumentTemplate>(model);

                entity.GroupCode = entity.Code;
                entity.CreatedDate = DateTime.Now;
                entity.MetaDataConfig = new List<DocumentTemplateMeteDataConfig>();
                //long identityNumber = await _dataContext.DocumentTemplate.DefaultIfEmpty().MaxAsync(x => x.IdentityNumber);

                //entity.IdentityNumber = ++identityNumber;
                //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                var checkCode = await _dataContext.DocumentTemplate.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    return new ResponseError(Code.ServerError, "Mã biểu mẫu đã tồn tại trong hệ thống!");
                }

                entity.Id = Guid.NewGuid();

                int count = 0;
                foreach (var item in model.DocumentMetaDataConfig)
                {
                    entity.MetaDataConfig.Add(new DocumentTemplateMeteDataConfig()
                    {
                        MetaDataId = item.MetaDataId,
                        MetaDataCode = item.MetaDataCode,
                        MetaDataName = item.MetaDataName
                    });
                    //await _dataContext.DocumentMetaDataConfig.AddAsync(new DocumentMetaDataConfig()
                    //{
                    //    Id = Guid.NewGuid(),
                    //    CreatedDate = DateTime.Now,
                    //    DocumentTemplateId = entity.Id,
                    //    MetaDataId = item.MetaDataId,
                    //    Order = ++count
                    //});
                }
                await _dataContext.DocumentTemplate.AddAsync(entity);

                count = 0;
                foreach (var item in model.DocumentFileTemplate)
                {
                    await _dataContext.DocumentFileTemplate.AddAsync(new DocumentFileTemplate()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now,
                        DocumentTemplateId = entity.Id,
                        FileBucketName = item.FileBucketName,
                        FileName = item.FileName,
                        FileObjectName = item.FileObjectName,
                        FileDataBucketName = item.FileDataBucketName,
                        FileDataObjectName = item.FileDataObjectName,
                        FileType = item.FileType,
                        MetaDataConfig = item.MetaDataConfig,
                        ProfileName = item.ProfileName,
                        Order = ++count
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_TEMPLATE} success: { JsonSerializer.Serialize(model)}");
                    InvalidCache();
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới biểu mẫu",
                        ObjectCode = CacheConstants.DOCUMENT_TEMPLATE,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_TEMPLATE} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Duplicate(DocumentTemplateDuplicateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Duplicate {CacheConstants.DOCUMENT_TEMPLATE}: {JsonSerializer.Serialize(model)}");

                var originItem = await _dataContext.DocumentTemplate
                        .FirstOrDefaultAsync(x => x.Id == model.Id);

                var entity = AutoMapperUtils.AutoMap<DocumentTemplate, DocumentTemplate>(originItem);

                entity.CreatedDate = DateTime.Now;
                entity.MetaDataConfig = new List<DocumentTemplateMeteDataConfig>();

                entity.Code = originItem.GroupCode + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss");

                entity.Id = Guid.NewGuid();

                entity.Status = false;

                entity.MetaDataConfig = new List<DocumentTemplateMeteDataConfig>();

                entity.MetaDataConfig = (from config in _dataContext.DocumentMetaDataConfig
                                         join metaData in _dataContext.MetaData on config.MetaDataId equals metaData.Id
                                         where entity.Id == config.DocumentTemplateId
                                         select new DocumentTemplateMeteDataConfig()
                                         {
                                             MetaDataId = metaData.Id,
                                             MetaDataCode = metaData.Code,
                                             MetaDataName = metaData.Name
                                         }).OrderBy(x => x.MetaDataCode).ToList();

                if (originItem.MetaDataConfig.Count != 0)
                {
                    entity.MetaDataConfig = entity.MetaDataConfig.Concat(originItem.MetaDataConfig).ToList();
                }

                entity.MetaDataConfig = entity.MetaDataConfig.Distinct().ToList();
                entity.GroupCode = originItem.GroupCode;
                entity.FromDate = null;

                await _dataContext.DocumentTemplate.AddAsync(entity);

                var lsDocumentFileTemplate = await _dataContext.DocumentFileTemplate.Where(x => x.DocumentTemplateId == originItem.Id).ToListAsync();

                int count = 0;
                foreach (var item in lsDocumentFileTemplate)
                {
                    await _dataContext.DocumentFileTemplate.AddAsync(new DocumentFileTemplate()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now,
                        DocumentTemplateId = entity.Id,
                        FileBucketName = item.FileBucketName,
                        FileName = item.FileName,
                        FileObjectName = item.FileObjectName,
                        FileDataBucketName = item.FileDataBucketName,
                        FileDataObjectName = item.FileDataObjectName,
                        FileType = item.FileType,
                        MetaDataConfig = item.MetaDataConfig,
                        ProfileName = item.ProfileName,
                        Order = ++count
                    });
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_TEMPLATE} success: { JsonSerializer.Serialize(model)}");
                    InvalidCache();
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới biểu mẫu",
                        ObjectCode = CacheConstants.DOCUMENT_TEMPLATE,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_TEMPLATE} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(DocumentTemplateUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CacheConstants.DOCUMENT_TEMPLATE}: {JsonSerializer.Serialize(model)}");
                var entity = await _dataContext.DocumentTemplate
                         .FirstOrDefaultAsync(x => x.Id == model.Id);

                #region Kiểm tra người dùng có thay đổi loại hợp đồng hay không => nếu có thay đổi biểu mẫu thì phải tính toán lại số lượng biểu mẫu trong hợp đồng
                if (entity.DocumentTypeId != model.DocumentTypeId)
                {
                    #region Kiểm tra số lượng biểu mẫu tối đa
                    // Kiểm tra thông tin số lượng biểu mẫu của đơn vị
                    var orgConfig = await _organizationConfigHandler.GetByOrgId(entity.OrganizationId.Value);
                    if (orgConfig == null)
                    {
                        return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối!");
                    }

                    var maxDoumentTemplate = orgConfig.TemplatePerDocumentType;

                    if (maxDoumentTemplate <= 0)
                    {
                        return new ResponseError(Code.Forbidden, "Đơn vị chưa được cấu hình số lượng biểu mẫu trên loại hợp đồng");
                    }

                    var countTemplatePerDocType = await _dataContext.DocumentTemplate.Where(x => x.DocumentTypeId == model.DocumentTypeId).Select(x => x.GroupCode).Distinct().CountAsync();

                    if (countTemplatePerDocType >= maxDoumentTemplate)
                    {
                        return new ResponseError(Code.Forbidden, $"Số lượng biểu mẫu trên hợp đồng tối đa là {maxDoumentTemplate}!");
                    }
                    #endregion
                }
                #endregion

                model.OrganizationId = entity.OrganizationId;
                model.GroupCode = entity.GroupCode;

                var listDocTempInGroup = await _dataContext.DocumentTemplate.Where(x => x.GroupCode == model.GroupCode).ToListAsync();
                if (listDocTempInGroup.Any(x => x.FromDate.HasValue
                    && model.FromDate.HasValue
                    && entity.FromDate.HasValue
                    && x.FromDate.Value.Date == model.FromDate.Value.Date
                    && entity.FromDate.Value.Date != x.FromDate.Value.Date))
                {
                    return new ResponseError(Code.BadRequest, "Ngày hiệu lực đã tồn tại trong nhóm biểu mẫu!");
                }

                // Nếu cập nhật trạng thái từ không sử dụng sang đang sử dụng
                //if (model.Status && !entity.Status)
                //{
                //    #region Kiểm tra số lượng biểu mẫu tối đa
                //    // Kiểm tra thông tin số lượng biểu mẫu của đơn vị
                //    var orgConfig = await _organizationConfigHandler.GetByOrgId(model.OrganizationId.Value);
                //    if (orgConfig == null)
                //    {
                //        return new ResponseError(Code.ServerError, "Đơn vị chưa được cấu hình thông tin kết nối!");
                //    }

                //    var maxDoumentTemplate = orgConfig.TemplatePerDocumentType;

                //    if (maxDoumentTemplate <= 0)
                //    {
                //        return new ResponseError(Code.Forbidden, "Đơn vị chưa được cấu hình số lượng biểu mẫu trên loại hợp đồng");
                //    }

                //    var countTemplatePerDocType = await _dataContext.DocumentTemplate.CountAsync(x => x.Status && x.DocumentTypeId == model.DocumentTypeId);

                //    if (countTemplatePerDocType >= maxDoumentTemplate)
                //    {
                //        // Cập nhật biểu mẫu có trạng thái đang sử dụng lâu nhất về false
                //        var oldestDocTemp = await _dataContext.DocumentTemplate
                //            .Where(x => x.Status && x.DocumentTypeId == model.DocumentTypeId)
                //            .OrderBy(x => x.CreatedDate)
                //            .FirstOrDefaultAsync();

                //        // Cập nhật biểu mẫu xa nhất về trạng thái không sử dụng
                //        oldestDocTemp.Status = false;
                //        //oldestDocTemp.GroupCode = model.GroupCode;
                //        Log.Information($"{systemLog.TraceId} - Update DocumentTemplate Status to False - TemplateCode: {oldestDocTemp.Code}");
                //    }
                //    #endregion

                //    //var listDocTemp = await _dataContext.DocumentTemplate.Where(x => model.GroupCode.Contains(x.GroupCode)).ToListAsync();
                //    //listDocTemp.ForEach(x => x.GroupCode = model.GroupCode);
                //    //_dataContext.DocumentTemplate.UpdateRange(listDocTemp);

                //    //model.GroupCode = null;
                //}
                //else 
                //if (!model.Status && entity.Status)
                //{
                //    var lastActive = await _dataContext.DocumentTemplate.Where(x => x.GroupCode == model.GroupCode && x.Status)
                //        .OrderByDescending(x => x.ModifiedDate)
                //        .FirstOrDefaultAsync();

                //    lastActive.Status = true;
                //    lastActive.ModifiedDate = DateTime.Now;

                //    _dataContext.DocumentTemplate.Update(lastActive);
                //}

                Log.Information($"{systemLog.TraceId} - Before Update: {JsonSerializer.Serialize(entity)}");

                model.UpdateToEntity(entity);

                var lsMetaData = await _metaDataHandler.GetListFromCache();

                entity.MetaDataConfig = new List<DocumentTemplateMeteDataConfig>();
                foreach (var item in model.DocumentMetaDataConfig)
                {
                    var mt = lsMetaData.FirstOrDefault(x => x.Id == item.MetaDataId);
                    entity.MetaDataConfig.Add(new DocumentTemplateMeteDataConfig()
                    {
                        MetaDataId = mt.Id,
                        MetaDataCode = mt.Code,
                        MetaDataName = mt.Name
                    });
                }

                _dataContext.DocumentTemplate.Update(entity);

                //Xóa các file đã bị xóa
                var listDeteleDocumentFileTemplateConfig = _dataContext.DocumentFileTemplate.Where(x => x.DocumentTemplateId == entity.Id && !model.DocumentFileTemplate.Select(newFile => newFile.Id).Contains(x.Id)).AsEnumerable();
                _dataContext.DocumentFileTemplate.RemoveRange(listDeteleDocumentFileTemplateConfig);

                var listDeteleDocumentFileTemplate = _dataContext.DocumentFileTemplate.Where(x => x.DocumentTemplateId == entity.Id).AsEnumerable();
                int count = 0;
                foreach (var item in model.DocumentFileTemplate)
                {
                    count = count + 1;
                    if (item.Id == Guid.Empty)
                    {
                        await _dataContext.DocumentFileTemplate.AddAsync(new DocumentFileTemplate()
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = DateTime.Now,
                            DocumentTemplateId = entity.Id,
                            FileBucketName = item.FileBucketName,
                            FileName = item.FileName,
                            FileObjectName = item.FileObjectName,
                            FileDataBucketName = item.FileDataBucketName,
                            FileDataObjectName = item.FileDataObjectName,
                            FileType = item.FileType,
                            ProfileName = item.ProfileName,
                            Order = count
                        });
                    }
                    else
                    {
                        var oldFile = await _dataContext.DocumentFileTemplate.FindAsync(item.Id);
                        oldFile.FileObjectName = item.FileObjectName;
                        oldFile.FileName = item.FileName;
                        oldFile.FileBucketName = item.FileBucketName;
                        oldFile.ProfileName = item.ProfileName;
                        oldFile.FileDataBucketName = item.FileDataBucketName;
                        oldFile.FileDataObjectName = item.FileDataObjectName;
                        oldFile.FileType = item.FileType;
                        oldFile.CreatedDate = DateTime.Now;
                        _dataContext.Update(oldFile);
                    }
                }

                var listDeteleDocumentMetaDataConfig = _dataContext.DocumentMetaDataConfig.Where(x => x.DocumentTemplateId == entity.Id).AsEnumerable();
                _dataContext.DocumentMetaDataConfig.RemoveRange(listDeteleDocumentMetaDataConfig);

                //count = 0;
                //foreach (var item in model.DocumentMetaDataConfig)
                //{
                //    await _dataContext.DocumentMetaDataConfig.AddAsync(new DocumentMetaDataConfig()
                //    {
                //        Id = Guid.NewGuid(),
                //        CreatedDate = DateTime.Now,
                //        DocumentTemplateId = entity.Id,
                //        MetaDataId = item.MetaDataId,
                //        Order = ++count
                //    });
                //}

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update: {JsonSerializer.Serialize(entity)}");
                    InvalidCache(model.Id.ToString());
                    systemLog.ListAction.Add(new ActionDetail
                    {
                        Description = $"Cập nhập biểu mẫu có mã:{entity.Code}",
                        ObjectCode = entity.Code,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CacheConstants.DOCUMENT_TEMPLATE} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> UpdateMetaData(List<DocumentFileTemplateModel> list, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update metaData {CacheConstants.DOCUMENT_TEMPLATE}: {JsonSerializer.Serialize(list)}");
                foreach (var item in list)
                {
                    var entity = await _dataContext.DocumentFileTemplate
                             .FirstOrDefaultAsync(x => x.Id == item.Id);
                    Log.Information($"{systemLog.TraceId} - Before Update: {JsonSerializer.Serialize(entity)}");

                    // Xử lý dữ liệu cũ
                    foreach (var mt in item.MetaDataConfig)
                    {
                        if (!string.IsNullOrEmpty(mt.FixCode))
                        {
                            switch (mt.FixCode)
                            {
                                case "ky-chung-thuc":
                                    mt.SignType = SignType.KY_CHUNG_THUC;
                                    mt.FixCode = "KY_CHUNG_THUC";
                                    break;
                                case "ky-phe-duyet":
                                    mt.SignType = SignType.KY_PHE_DUYET;
                                    mt.FixCode = "KY_PHE_DUYET";
                                    break;
                                case "KY_CHUNG_THUC":
                                    mt.SignType = SignType.KY_CHUNG_THUC;
                                    mt.FixCode = "KY_CHUNG_THUC";
                                    break;
                                case "KY_PHE_DUYET":
                                    mt.SignType = SignType.KY_PHE_DUYET;
                                    mt.FixCode = "KY_PHE_DUYET";
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    entity.MetaDataConfig = item.MetaDataConfig;

                    _dataContext.DocumentFileTemplate.Update(entity);
                }

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update: {JsonSerializer.Serialize(list)}");
                    systemLog.ListAction.Add(new ActionDetail
                    {
                        Description = $"Cập nhập cấu hình biểu mẫu hợp đồng",
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<bool>(true, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update metaData {CacheConstants.DOCUMENT_TEMPLATE} error: Save database error!");
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
                Log.Information($"{systemLog.TraceId} - Delete {CacheConstants.DOCUMENT_TEMPLATE}: {JsonSerializer.Serialize(listId)}");
                var listResult = new List<ResponeDeleteModel>();
                var name = "";
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.DocumentTemplate.FindAsync(item);

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

                        var listDeteleDocumentMetaDataConfig = _dataContext.DocumentMetaDataConfig.Where(x => x.DocumentTemplateId == entity.Id).AsEnumerable();
                        _dataContext.DocumentMetaDataConfig.RemoveRange(listDeteleDocumentMetaDataConfig);

                        var listDeteleDocumentFileTemplate = _dataContext.DocumentFileTemplate.Where(x => x.DocumentTemplateId == entity.Id).AsEnumerable();
                        _dataContext.DocumentFileTemplate.RemoveRange(listDeteleDocumentFileTemplate);

                        _dataContext.DocumentTemplate.Remove(entity);

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
                                    Description = $"Xóa biểu mẫu có mã: {entity.Code}",
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

        public async Task<Response> Filter(DocumentTemplateQueryFilter filter)
        {
            try
            {
                #region Lấy quyền người dùng
                var roleIds = await _userHandler.GetUserRoleFromCacheAsync(filter.CurrentUserId);
                var userRole = await _roleHandler.GetRoleDataPermissionFromCacheByListIdAsync(roleIds);
                #endregion                

                var data = from docTemp in _dataContext.DocumentTemplate
                           join docType in _dataContext.DocumentType on docTemp.DocumentTypeId equals docType.Id
                           join docFileTemplate in _dataContext.DocumentFileTemplate on docTemp.Id equals docFileTemplate.DocumentTemplateId
                           where (userRole.ListDocumentTypeId.Contains(docType.Id) || docType.CreatedUserId == filter.CurrentUserId)
                           select new DocumentTemplateBaseModel()
                           {
                               Id = docTemp.Id,
                               Code = docTemp.Code,
                               Name = docTemp.Name,
                               Status = docTemp.Status,
                               Description = docTemp.Description,
                               DocumentTypeId = docType.Id,
                               DocumentTypeName = docType.Name,
                               CreatedDate = docTemp.CreatedDate,
                               OrganizationId = docTemp.OrganizationId,
                               FromDate = docTemp.FromDate,
                               ToDate = docTemp.ToDate,
                               GroupCode = docTemp.GroupCode,
                               ModifiedDate = docTemp.ModifiedDate
                           };

                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Name.ToLower().Contains(ts) || x.Code.ToLower().Contains(ts));
                }

                if (filter.Status.HasValue)
                {
                    data = data.Where(x => x.Status == filter.Status);
                }
                if (filter.DocumentTypeId.HasValue)
                {
                    data = data.Where(x => x.DocumentTypeId == filter.DocumentTypeId);
                }

                if (filter.OrganizationId.HasValue)
                {
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(filter.OrganizationId.Value);
                    data = data.Where(x => x.OrganizationId.HasValue && (listChildOrgID.Contains(x.OrganizationId.Value)));
                }

                data = data.OrderByField(filter.PropertyName, filter.Ascending);

                var listResult = await data.ToListAsync();

                var listTemplateParent = new List<DocumentTemplateModel>();
                listResult.ForEach(x =>
                {
                    var templateParents = CaculateActiveDocumentTemplateByGroupCode_v2(x.GroupCode, listResult);
                    if ((templateParents == null || templateParents.Count < 1) && !x.IsContainParent)
                    {
                        var listTempByGroupCode = listResult.Where(x1 => x1.GroupCode == x.GroupCode).ToList();
                        var parent = listTempByGroupCode
                            .OrderByDescending(x1 => x1.ModifiedDate.HasValue)
                            .ThenByDescending(x1 => x1.ModifiedDate)
                            .FirstOrDefault();

                        var parentTemp = AutoMapperUtils.AutoMap<DocumentTemplateBaseModel, DocumentTemplateModel>(parent);
                        listTemplateParent.Add(parentTemp);
                        listResult.Where(x1 => x1.GroupCode == x.GroupCode).ToList().ForEach(x1 => x1.IsContainParent = true);
                    }
                    else if (!x.IsContainParent)
                    {
                        listTemplateParent.AddRange(templateParents);
                        x.IsContainParent = true;
                        listResult.Where(x1 => x1.GroupCode == x.GroupCode).ToList().ForEach(x1 => x1.IsContainParent = true);
                    }
                });

                listResult = listResult.Where(x => listTemplateParent.Select(x1 => x1.Id).Contains(x.Id)).ToList();

                int totalCount = listResult.Count();

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
                    listResult = listResult.Skip(excludedRows).Take(filter.PageSize.Value).ToList();
                }
                int dataCount = listResult.Count();

                // Kiểm tra biểu mẫu đã được cấu hình vùng ký chưa
                var lstDocTempIds = _dataContext.DocumentTemplate.Select(c => c.Id);

                var lstDocTempFileMetaDataConfig = (from dft in _dataContext.DocumentFileTemplate
                                                    join dt in lstDocTempIds on dft.DocumentTemplateId equals dt
                                                    select new
                                                    {
                                                        DocTempId = dt,
                                                        MetaDataConfig = dft.MetaDataConfig
                                                    }).ToList();

                var lstDocTempIdsConfigedSignZone = lstDocTempFileMetaDataConfig.Where(c => c.MetaDataConfig.Count(m => m.SignType != 0) > 0);

                var distinctLstDoctempIds = lstDocTempIdsConfigedSignZone.OrderByDescending(c => c.DocTempId).Distinct().ToList();

                listResult = (from d in listResult
                              join docTempConfigedSignZone in distinctLstDoctempIds on d.Id equals docTempConfigedSignZone.DocTempId into joinConfiged
                              from docTempConfigedSignZone in joinConfiged.DefaultIfEmpty()
                              select new DocumentTemplateBaseModel()
                              {
                                  Id = d.Id,
                                  Code = d.Code,
                                  Name = d.Name,
                                  Status = d.Status,
                                  Description = d.Description,
                                  DocumentTypeId = d.DocumentTypeId,
                                  DocumentTypeName = d.DocumentTypeName,
                                  CreatedDate = d.CreatedDate,
                                  CheckSignConfig = (docTempConfigedSignZone != null) ? true : false,
                                  FromDate = d.FromDate,
                                  ToDate = d.ToDate,
                                  GroupCode = d.GroupCode
                              }).ToList();

                return new ResponseObject<PaginationList<DocumentTemplateBaseModel>>(new PaginationList<DocumentTemplateBaseModel>()
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

                var entity = await _dataContext.DocumentTemplate
                    .FirstOrDefaultAsync(x => x.Id == id);

                var model = AutoMapperUtils.AutoMap<DocumentTemplate, DocumentTemplateModel>(entity);

                model.DocumentFileTemplate = await _dataContext.DocumentFileTemplate.Where(x => x.DocumentTemplateId == entity.Id).OrderBy(x => x.Order)
                        .Select(x => new DocumentFileTemplateModel()
                        {
                            Id = x.Id,
                            FileObjectName = x.FileObjectName,
                            FileName = x.FileName,
                            FileBucketName = x.FileBucketName,
                            FileDataBucketName = x.FileDataBucketName,
                            FileDataObjectName = x.FileDataObjectName,
                            FileType = x.FileType,
                            ProfileName = x.ProfileName,
                            MetaDataConfig = x.MetaDataConfig,
                        }).ToListAsync();

                var ms = new MinIOService();

                foreach (var item in model.DocumentFileTemplate)
                {
                    if (!string.IsNullOrEmpty(item.FileObjectName) && !string.IsNullOrEmpty(item.FileBucketName))
                    {
                        item.FileUrl = await ms.GetObjectPresignUrlAsync(item.FileBucketName, item.FileObjectName);
                    }
                    if (!string.IsNullOrEmpty(item.FileDataBucketName) && !string.IsNullOrEmpty(item.FileDataObjectName))
                    {
                        item.FileDataUrl = await ms.GetObjectPresignUrlAsync(item.FileDataBucketName, item.FileDataObjectName);
                    }
                }

                model.DocumentMetaDataConfig = await (from config in _dataContext.DocumentMetaDataConfig
                                                      join metaData in _dataContext.MetaData on config.MetaDataId equals metaData.Id
                                                      where entity.Id == config.DocumentTemplateId
                                                      select new DocumentMetaDataConfigModel()
                                                      {
                                                          MetaDataId = metaData.Id,
                                                          MetaDataCode = metaData.Code,
                                                          MetaDataName = metaData.Name,
                                                          DataType = metaData.DataType,
                                                          IsRequire = metaData.IsRequire,
                                                          ListData = metaData.ListData,
                                                          Order = config.Order
                                                      }).OrderBy(x => x.MetaDataName).ToListAsync();

                if (model.DocumentMetaDataConfig == null || model.DocumentMetaDataConfig.Count == 0 && entity.MetaDataConfig != null)
                {
                    model.DocumentMetaDataConfig = entity.MetaDataConfig.Select(x => new DocumentMetaDataConfigModel()
                    {
                        MetaDataId = x.MetaDataId,
                        MetaDataCode = x.MetaDataCode,
                        MetaDataName = x.MetaDataName
                    }).ToList();
                }

                return new ResponseObject<DocumentTemplateModel>(model, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListTemplateByTypeId(Guid id)
        {
            try
            {
                var listData = await _dataContext.DocumentTemplate
                    .Where(x => x.DocumentTypeId == id).ToListAsync();

                var listRS = new List<DocumentTemplateModel>();

                foreach (var entity in listData)
                {
                    var model = AutoMapperUtils.AutoMap<DocumentTemplate, DocumentTemplateModel>(entity);

                    model.DocumentFileTemplate = await _dataContext.DocumentFileTemplate.Where(x => x.DocumentTemplateId == entity.Id).OrderBy(x => x.Order)
                            .Select(x => new DocumentFileTemplateModel()
                            {
                                Id = x.Id,
                                FileBucketName = x.FileBucketName,
                                FileName = x.FileName,
                                FileObjectName = x.FileObjectName,
                                ProfileName = x.ProfileName
                            }).ToListAsync();

                    var lsDT = await (from config in _dataContext.DocumentMetaDataConfig
                                      join metaData in _dataContext.MetaData on config.MetaDataId equals metaData.Id
                                      where entity.Id == config.DocumentTemplateId
                                      select new DocumentMetaDataConfigModel()
                                      {
                                          MetaDataId = metaData.Id,
                                          MetaDataCode = metaData.Code,
                                          MetaDataName = metaData.Name
                                      }).ToListAsync();
                    if (entity.MetaDataConfig != null && entity.MetaDataConfig.Count > 0)
                    {
                        model.DocumentMetaDataConfig = entity.MetaDataConfig.Select(x => new DocumentMetaDataConfigModel()
                        {
                            MetaDataId = x.MetaDataId,
                            MetaDataCode = x.MetaDataCode,
                            MetaDataName = x.MetaDataName
                        }).ToList();
                        model.DocumentMetaDataConfig.AddRange(lsDT);
                        model.DocumentMetaDataConfig = model.DocumentMetaDataConfig.Distinct().ToList();
                    }
                    else
                    {
                        model.DocumentMetaDataConfig = lsDT;
                    }

                    listRS.Add(model);
                }

                return new ResponseObject<List<DocumentTemplateModel>>(listRS, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.DocumentTemplate.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new DocumentTemplateSelectItemModel()
                                {
                                    Id = item.Id,
                                    Name = item.Name,
                                    Note = item.Code,
                                    OrganizationId = item.OrganizationId
                                });

                    return await data.ToListAsync();
                });

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (orgId.HasValue)
                {
                    list = list.Where(x => x.OrganizationId == orgId).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<DocumentTemplateSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
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

        public async Task<Response> GetListDocumentTemplateByGroupCode(DocumentByGroupCodeModel model)
        {
            try
            {
                #region Lấy quyền người dùng
                var roleIds = await _userHandler.GetUserRoleFromCacheAsync(model.CurrentUserId);
                var userRole = await _roleHandler.GetRoleDataPermissionFromCacheByListIdAsync(roleIds);
                #endregion

                var data = from docTemp in _dataContext.DocumentTemplate
                           join docType in _dataContext.DocumentType on docTemp.DocumentTypeId equals docType.Id
                           join docFileTemplate in _dataContext.DocumentFileTemplate on docTemp.Id equals docFileTemplate.DocumentTemplateId
                           where userRole.ListDocumentTypeId.Contains(docType.Id) || docType.CreatedUserId == model.CurrentUserId
                           select new DocumentTemplateBaseModel()
                           {
                               Id = docTemp.Id,
                               Code = docTemp.Code,
                               Name = docTemp.Name,
                               Status = docTemp.Status,
                               Description = docTemp.Description,
                               DocumentTypeId = docType.Id,
                               DocumentTypeName = docType.Name,
                               CreatedDate = docTemp.CreatedDate,
                               OrganizationId = docTemp.OrganizationId,
                               FromDate = docTemp.FromDate,
                               ToDate = docTemp.ToDate,
                               ModifiedDate = docTemp.ModifiedDate,
                               GroupCode = docTemp.GroupCode
                           };

                data = data.OrderByDescending(x => x.CreatedDate);
                var listResult = await data.ToListAsync();

                var listTemplateParent = new List<DocumentTemplateModel>();
                listResult.ForEach(x =>
                {
                    var templateParents = CaculateActiveDocumentTemplateByGroupCode_v2(x.GroupCode, listResult);
                    if ((templateParents == null || templateParents.Count < 1) && !x.IsContainParent)
                    {
                        var listTempByGroupCode = listResult.Where(x1 => x1.GroupCode == x.GroupCode).ToList();
                        var parent = listTempByGroupCode
                            .OrderByDescending(x1 => x1.ModifiedDate.HasValue)
                            .ThenByDescending(x1 => x1.ModifiedDate)
                            .FirstOrDefault();

                        var parentTemp = AutoMapperUtils.AutoMap<DocumentTemplateBaseModel, DocumentTemplateModel>(parent);
                        listTemplateParent.Add(parentTemp);
                        listResult.Where(x1 => x1.GroupCode == x.GroupCode).ToList().ForEach(x1 => x1.IsContainParent = true);
                    }
                    else if (!x.IsContainParent)
                    {
                        listTemplateParent.AddRange(templateParents);
                        x.IsContainParent = true;
                        listResult.Where(x1 => x1.GroupCode == x.GroupCode).ToList().ForEach(x1 => x1.IsContainParent = true);
                    }
                });

                listResult = listResult.Where(x => !listTemplateParent.Select(x1 => x1.Id).Contains(x.Id) && !string.IsNullOrEmpty(x.GroupCode) && model.GroupCode.Contains(x.GroupCode)).ToList();

                // Kiểm tra biểu mẫu đã được cấu hình vùng ký chưa
                var lstDocTempIds = _dataContext.DocumentTemplate.Select(c => c.Id);

                var lstDocTempFileMetaDataConfig = (from dft in _dataContext.DocumentFileTemplate
                                                    join dt in lstDocTempIds on dft.DocumentTemplateId equals dt
                                                    select new
                                                    {
                                                        DocTempId = dt,
                                                        MetaDataConfig = dft.MetaDataConfig
                                                    }).ToList();

                var lstDocTempIdsConfigedSignZone = lstDocTempFileMetaDataConfig.Where(c => c.MetaDataConfig.Count(m => m.SignType != 0) > 0);

                var distinctLstDoctempIds = lstDocTempIdsConfigedSignZone.OrderByDescending(c => c.DocTempId).Distinct().ToList();

                listResult = (from d in listResult
                              join docTempConfigedSignZone in distinctLstDoctempIds on d.Id equals docTempConfigedSignZone.DocTempId into joinConfiged
                              from docTempConfigedSignZone in joinConfiged.DefaultIfEmpty()
                              select new DocumentTemplateBaseModel()
                              {
                                  Id = d.Id,
                                  Code = d.Code,
                                  Name = d.Name,
                                  Status = d.Status,
                                  Description = d.Description,
                                  DocumentTypeId = d.DocumentTypeId,
                                  DocumentTypeName = d.DocumentTypeName,
                                  CreatedDate = d.CreatedDate,
                                  CheckSignConfig = (docTempConfigedSignZone != null) ? true : false,
                                  FromDate = d.FromDate,
                                  ToDate = d.ToDate
                              }).ToList();

                listResult = listResult.OrderByDescending(x => x.CreatedDate).ToList();

                return new ResponseObject<List<DocumentTemplateBaseModel>>(listResult, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<DocumentTemplateModel>> CaculateActiveDocumentTemplateByGroupCode(string groupCode)
        {
            try
            {
                var result = new List<DocumentTemplateModel>();

                var documentTemplate = await _dataContext.DocumentTemplate
                    .Where(x => ((x.FromDate.HasValue && x.FromDate.Value.Date <= DateTime.Now.Date) || !x.FromDate.HasValue)
                        && x.GroupCode == groupCode
                        && x.Status)
                    .OrderByDescending(x => x.FromDate.HasValue)
                    .ThenByDescending(x => x.FromDate)
                    .Take(1)
                    .ToListAsync();

                documentTemplate.ForEach(x =>
                {
                    var docTemp = AutoMapperUtils.AutoMap<DocumentTemplate, DocumentTemplateModel>(x);
                    result.Add(docTemp);
                });

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        public List<DocumentTemplateModel> CaculateActiveDocumentTemplateByGroupCode_v2(string groupCode, List<DocumentTemplateBaseModel> documentTemplates)
        {
            try
            {
                var result = new List<DocumentTemplateModel>();

                var documentTemplate = documentTemplates
                    .Where(x => ((x.FromDate.HasValue && x.FromDate.Value.Date <= DateTime.Now.Date) || !x.FromDate.HasValue)
                        && x.GroupCode == groupCode
                        && x.Status)
                    .OrderByDescending(x => x.FromDate.HasValue)
                    .ThenByDescending(x => x.FromDate)
                    .Take(1)
                    .ToList();

                documentTemplate.ForEach(x =>
                {
                    var docTemp = AutoMapperUtils.AutoMap<DocumentTemplateBaseModel, DocumentTemplateModel>(x);
                    result.Add(docTemp);
                });

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }
    }
}