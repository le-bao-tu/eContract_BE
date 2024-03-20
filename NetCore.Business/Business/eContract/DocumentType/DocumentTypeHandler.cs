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
    public class DocumentTypeHandler : IDocumentTypeHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.DOCUMENT_TYPE;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "DT.";
        private readonly DataContext _dataContext;
        private readonly IOrganizationConfigHandler _orgConfigHandler;
        private readonly IOrganizationHandler _organizationHandler;
        private readonly ICacheService _cacheService;
        private readonly IUserHandler _userHandler;
        private readonly IRoleHandler _roleHandler;

        public DocumentTypeHandler(DataContext dataContext, ICacheService cacheService, IOrganizationConfigHandler orgConfigHandler, IOrganizationHandler organizationHandler,
            IUserHandler userHandler,
            IRoleHandler roleHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _orgConfigHandler = orgConfigHandler;
            _organizationHandler = organizationHandler;
            _userHandler = userHandler;
            _roleHandler = roleHandler;
        }

        public async Task<Response> Create(DocumentTypeCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CacheConstants.DOCUMENT_TYPE}: {JsonSerializer.Serialize(model)}");
                // Kiểm tra cấu hình đơn vị
                var orgConfigRs = await _orgConfigHandler.GetByOrgId(model.OrganizationId == null ? Guid.Empty : model.OrganizationId.Value);
                if (orgConfigRs == null)
                {
                    return new ResponseError(Code.Forbidden, "Đơn vị chưa được cấu hình thông tin");
                }

                var count = await _dataContext.DocumentType.CountAsync(x => x.OrganizationId == model.OrganizationId);

                if (count >= orgConfigRs.MaxDocumentType)
                {
                    return new ResponseError(Code.Forbidden, "Đơn vị đã đạt đến số lượng loại hợp đồng tối đa");
                }

                #region Check is exist document Type
                var isExistCode = _dataContext.DocumentType.Any(c => c.Code == model.Code);
                if (isExistCode)
                    return new ResponseError(Code.NotFound, "Mã loại CC/CN đã tồn tại");

                #endregion

                var entity = AutoMapperUtils.AutoMap<DocumentTypeCreateModel, DocumentType>(model);

                entity.CreatedDate = DateTime.Now;

                //long identityNumber = await _dataContext.DocumentType.DefaultIfEmpty().MaxAsync(x => x.IdentityNumber);

                //entity.IdentityNumber = ++identityNumber;
                //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                entity.Id = Guid.NewGuid();
                await _dataContext.DocumentType.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"Create {CacheConstants.DOCUMENT_TYPE} success: {JsonSerializer.Serialize(entity)}");
                    InvalidCache();
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới loại hợp đồng có mã: {entity.Code}",
                        ObjectCode = CacheConstants.DOCUMENT_TYPE,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"Create {CacheConstants.DOCUMENT_TYPE} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateMany(List<DocumentTypeCreateModel> list, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create many {CacheConstants.DOCUMENT_TYPE}: {JsonSerializer.Serialize(list)}");
                var listId = new List<Guid>();
                var listRS = new List<DocumentType>();
                foreach (var item in list)
                {
                    var entity = AutoMapperUtils.AutoMap<DocumentTypeCreateModel, DocumentType>(item);

                    entity.CreatedDate = DateTime.Now;
                    await _dataContext.DocumentType.AddAsync(entity);
                    listId.Add(entity.Id);
                    listRS.Add(entity);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"Create many {CacheConstants.DOCUMENT_TYPE} success: {JsonSerializer.Serialize(listRS)}");
                    InvalidCache();
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới danh sách loại hợp đồng.",
                        ObjectCode = CacheConstants.DOCUMENT_TYPE,
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<List<Guid>>(listId, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"Create many {CacheConstants.DOCUMENT_TYPE} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(DocumentTypeUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CacheConstants.DOCUMENT_TYPE}: {JsonSerializer.Serialize(model)}");
                #region Check is exist document Type
                var isExistEmail = _dataContext.DocumentType.Any(c => c.Code == model.Code && c.Id != model.Id);
                if (isExistEmail)
                    return new ResponseError(Code.NotFound, "Mã loại CC/CN đã tồn tại");

                #endregion

                var entity = await _dataContext.DocumentType
                         .FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information("Before Update: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.DocumentType.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update: {JsonSerializer.Serialize(entity)}");
                    InvalidCache(model.Id.ToString());
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập loại hợp đồng có có mã:{entity.Code}.",
                        ObjectCode = CacheConstants.DOCUMENT_TYPE,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CacheConstants.DOCUMENT_TYPE} error: Save database error!");
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
                var listResult = new List<ResponeDeleteModel>();
                var name = "";
                Log.Information("List Delete: " + JsonSerializer.Serialize(listId));
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.DocumentType.FindAsync(item);

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
                        _dataContext.DocumentType.Remove(entity);
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

                                systemLog.ListAction.Add(new ActionDetail()
                                {
                                    Description = $"Xóa loại hợp đồng có mã:{entity.Code}.",
                                    ObjectCode = CacheConstants.DOCUMENT_TYPE,
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

        public async Task<Response> Filter(DocumentTypeQueryFilter filter)
        {
            try
            {
                #region Lấy quyền người dùng
                var roleIds = await _userHandler.GetUserRoleFromCacheAsync(filter.CurrentUserId);
                var userRole = await _roleHandler.GetRoleDataPermissionFromCacheByListIdAsync(roleIds);
                #endregion

                var data = (from item in _dataContext.DocumentType
                            where userRole.ListDocumentTypeId.Contains(item.Id) || item.CreatedUserId == filter.CurrentUserId
                            select new DocumentTypeBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                Description = item.Description,
                                Status = item.Status,
                                CreatedDate = item.CreatedDate,
                                OrganizationId = item.OrganizationId
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

                //if (filter.OrganizationId.HasValue)
                //{
                //    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(filter.OrganizationId.Value);
                //    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                //    data = data.Where(x => x.OrganizationId.HasValue && (listChildOrgID.Contains(x.OrganizationId.Value)));
                //}

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
                return new ResponseObject<PaginationList<DocumentTypeBaseModel>>(new PaginationList<DocumentTypeBaseModel>()
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
                    var entity = await _dataContext.DocumentType
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<DocumentType, DocumentTypeModel>(entity);
                });
                return new ResponseObject<DocumentTypeModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<DocumentTypeModel> GetDetailById(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.DocumentType
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<DocumentType, DocumentTypeModel>(entity);
                });
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        public async Task<Response> GetAllListCombobox(int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                var list = await GetListFromCache();
                list = list.Where(x => x.Status).ToList();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (orgId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(orgId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<DocumentTypeSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListComboboxAllStatus(int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                var list = await GetListFromCache();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (orgId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(orgId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<DocumentTypeSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
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
                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.DocumentType.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                //                select new DocumentTypeSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Name = item.Name,
                //                    Code = item.Code,
                //                    Note = item.Code,
                //                    OrganizationId = item.OrganizationId
                //                });

                //    return await data.ToListAsync();
                //});

                var list = await GetListFromCache();

                list = list.Where(x => x.Status && x.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS && x.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU).ToList();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (orgId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(orgId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<DocumentTypeSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListComboboxFor3rd(int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                Log.Information($"Đơn vị: {orgId?.ToString()} thực hiện lấy danh sách hợp đồng");
                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.DocumentType.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                //                select new DocumentTypeSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Name = item.Name,
                //                    Code = item.Code,
                //                    Note = item.Code,
                //                    OrganizationId = item.OrganizationId
                //                });

                //    return await data.ToListAsync();
                //});

                var list = await GetListFromCache();

                list = list.Where(x => x.Status && x.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS && x.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU).ToList();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (orgId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(orgId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                var listFor3rd = new List<DocumentTypeSelectItemFor3rdModel>();
                foreach (var item in list)
                {
                    listFor3rd.Add(new DocumentTypeSelectItemFor3rdModel()
                    {
                        DocumentTypeCode = item.Code,
                        DocumentTypeName = item.Name
                    });
                }

                return new ResponseObject<List<DocumentTypeSelectItemFor3rdModel>>(listFor3rd, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        private async Task<List<DocumentTypeSelectItemModel>> GetListFromCache()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.DocumentType.OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new DocumentTypeSelectItemModel()
                                {
                                    Id = item.Id,
                                    Name = item.Name,
                                    Code = item.Code,
                                    Note = item.Code,
                                    OrganizationId = item.OrganizationId,
                                    Status = item.Status
                                });

                    return await data.ToListAsync();
                });

                return list;
            }
            catch (Exception ex)
            {
                throw ex;
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

        public async Task<Response> GetListMetaDataByDocumentType(string documenTypeCode, Guid? orgId = null)
        {
            try
            {
                Log.Information($"Đơn vị: {orgId?.ToString()} thực hiện lấy danh sách Meta Data cho loại hợp đồng {documenTypeCode}");

                //TODO: Cần bổ sung cache cho case này
                var entity = await _dataContext.DocumentType
                    .FirstOrDefaultAsync(x => x.Code == documenTypeCode && x.OrganizationId == orgId && x.Status);

                if (entity == null)
                {
                    return new ResponseError(Code.NotFound, $"Không tìm thấy loại hợp đồng có mã là: {documenTypeCode}");
                }

                // Lấy danh sách biểu mẫu thuộc document
                var dt =
                    from documentTemp in _dataContext.DocumentTemplate.Where(x => x.Status && x.DocumentTypeId == entity.Id)
                    join metaDataConfig in _dataContext.DocumentMetaDataConfig on documentTemp.Id equals metaDataConfig.DocumentTemplateId
                    join metaData in _dataContext.MetaData on metaDataConfig.MetaDataId equals metaData.Id
                    select new MetaDataListForDocumentType()
                    {
                        MetaDataCode = metaData.Code,
                        MetaDataName = metaData.Name
                    };

                List<MetaDataListForDocumentType> list = await dt.ToListAsync();

                var dtFromTemp = await _dataContext.DocumentTemplate.Where(x => x.Status && x.DocumentTypeId == entity.Id).Select(x => x.MetaDataConfig).ToListAsync();

                foreach (var item in dtFromTemp)
                {
                    if (item != null)
                    {
                        var ls = item.Select(x => new MetaDataListForDocumentType()
                        {
                            MetaDataCode = x.MetaDataCode,
                            MetaDataName = x.MetaDataName
                        }).ToList();
                        list.AddRange(ls);
                    }
                }

                list = list.Distinct().ToList();

                return new ResponseObject<List<MetaDataListForDocumentType>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
    }
}