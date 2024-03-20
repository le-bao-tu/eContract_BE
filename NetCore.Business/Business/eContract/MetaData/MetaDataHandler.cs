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
    public class MetaDataHandler : IMetaDataHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.META_DATA;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "MD.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IOrganizationHandler _organizationHandler;

        public MetaDataHandler(DataContext dataContext, ICacheService cacheService, IOrganizationHandler organizationHandler)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _organizationHandler = organizationHandler;
        }

        public async Task<Response> Create(MetaDataCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CacheConstants.META_DATA}: " + JsonSerializer.Serialize(model));
                var entity = AutoMapperUtils.AutoMap<MetaDataCreateModel, MetaData>(model);

                entity.CreatedDate = DateTime.Now;

                var checkCode = await _dataContext.MetaData.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    return new ResponseError(Code.ServerError, "Mã thông tin trong biểu mẫu đã tồn tại trong hệ thống!");
                }

                //long identityNumber = await _dataContext.MetaData.DefaultIfEmpty().MaxAsync(x => x.IdentityNumber);

                //entity.IdentityNumber = ++identityNumber;
                //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                entity.Id = Guid.NewGuid();
                await _dataContext.MetaData.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CacheConstants.META_DATA} success: {JsonSerializer.Serialize(entity)}");
                    InvalidCache();
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới metadata mã: {entity.Code}",
                        ObjectCode = CacheConstants.META_DATA,
                        ObjectId = entity.Id.ToString(),
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateMany(List<MetaDataCreateModel> list, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create many {CacheConstants.META_DATA}: {JsonSerializer.Serialize(list)}");
                var listResult = new List<CreateManyMetaDataResult>();
                var listRS = new List<MetaData>();
                foreach (var item in list)
                {
                    var checkCode = await _dataContext.MetaData.AnyAsync(x => x.Code == item.Code);
                    if (checkCode)
                    {
                        listResult.Add(new CreateManyMetaDataResult()
                        {
                            Code = item.Code,
                            Message = $"Mã {CacheConstants.META_DATA} {item.Code} đã tồn tại trong hệ thống!"
                        });
                        continue;
                    }
                    var entity = AutoMapperUtils.AutoMap<MetaDataCreateModel, MetaData>(item);

                    entity.CreatedDate = DateTime.Now;
                    await _dataContext.MetaData.AddAsync(entity);
                    listResult.Add(new CreateManyMetaDataResult()
                    {
                        Id = entity.Id,
                        Code = entity.Code,
                        Message = $"Thêm mới thành công"
                    });
                    listRS.Add(entity);
                }

                Log.Information($"{systemLog.TraceId} - Mã {CacheConstants.META_DATA} đã tồn tại: {JsonSerializer.Serialize(listResult.Select(r => !r.Id.HasValue).ToList())}");

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"Create many {CacheConstants.META_DATA} success: {JsonSerializer.Serialize(listRS)}");
                    InvalidCache();

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới danh sách metadata",
                        ObjectCode = CacheConstants.META_DATA,
                        CreatedDate = DateTime.Now
                    });
                    return new ResponseObject<List<CreateManyMetaDataResult>>(listResult, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"Create many {CacheConstants.META_DATA} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(MetaDataUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CacheConstants.META_DATA}: {JsonSerializer.Serialize(model)}");
                var entity = await _dataContext.MetaData.FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information($"{systemLog.TraceId} - Before Update: {JsonSerializer.Serialize(entity)}");

                model.UpdateToEntity(entity);
                _dataContext.MetaData.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update: {JsonSerializer.Serialize(entity)}");
                    InvalidCache(model.Id.ToString());
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập metadata mã:{entity.Code}",
                        ObjectCode = CacheConstants.META_DATA,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CacheConstants.META_DATA} error: Save database error!");
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
                Log.Information($"{systemLog.TraceId} - Delete {CacheConstants.META_DATA}: {JsonSerializer.Serialize(listId)}");
                var listResult = new List<ResponeDeleteModel>();
                var name = "";

                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.MetaData.FindAsync(item);

                    if (entity.Code == MetaDataCodeConstants.EMAIL || entity.Code == MetaDataCodeConstants.FULLNAME ||
                        entity.Code == MetaDataCodeConstants.PHONENUMBER
                        || entity.Code == MetaDataCodeConstants.DOC_3RD_ID ||
                        entity.Code == MetaDataCodeConstants.USER_CONNECT_ID ||
                        entity.Code == MetaDataCodeConstants.DOC_NAME
                        || entity.Code == MetaDataCodeConstants.ORG_CODE ||
                        entity.Code == MetaDataCodeConstants.CONTRACT_TYPE_ACTION)
                    {
                        listResult.Add(new ResponeDeleteModel()
                        {
                            Id = item,
                            Name = name,
                            Result = false,
                            Message = "Bạn không thể xóa thông tin này vì đây là thông tin cố định."
                        });
                        continue;
                    }

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
                        _dataContext.MetaData.Remove(entity);
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
                                    Description = $"Xóa metadata mã: {entity.Code}",
                                    ObjectCode = CacheConstants.META_DATA,
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

        public async Task<Response> Filter(MetaDataQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.MetaData

                            select new MetaDataBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                Status = item.Status,
                                DataType = item.DataType,
                                Description = item.Description,
                                IsRequire = item.IsRequire,
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

                if (filter.OrganizationId.HasValue)
                {
                    var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(filter.OrganizationId.Value);
                    List<Guid> listChildOrgID = _organizationHandler.GetListChildOrgByParentID(rootOrg.Id);
                    data = data.Where(x =>
                        (x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value))
                        || (x.Code == MetaDataCodeConstants.EMAIL || x.Code == MetaDataCodeConstants.FULLNAME ||
                            x.Code == MetaDataCodeConstants.PHONENUMBER
                            || x.Code == MetaDataCodeConstants.DOC_ID || x.Code == MetaDataCodeConstants.DOC_3RD_ID ||
                            x.Code == MetaDataCodeConstants.USER_CONNECT_ID
                            || x.Code == MetaDataCodeConstants.DOC_NAME || x.Code == MetaDataCodeConstants.ORG_CODE ||
                            x.Code == MetaDataCodeConstants.CONTRACT_TYPE_ACTION)
                    );
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
                return new ResponseObject<PaginationList<MetaDataBaseModel>>(new PaginationList<MetaDataBaseModel>()
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

        public async Task<Response> GetById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.MetaData
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<MetaData, MetaDataModel>(entity);
                });
                return new ResponseObject<MetaDataModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.MetaData.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                //                select new MetaDataSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Code = item.Code,
                //                    Name = item.Name,
                //                    Note = item.Code,
                //                    OrganizationId = item.OrganizationId
                //                });

                //    return await data.ToListAsync();
                //});

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
                    list = list.Where(x => x.OrganizationId.HasValue && listChildOrgID.Contains(x.OrganizationId.Value)
                                           || (x.Code == MetaDataCodeConstants.EMAIL ||
                                               x.Code == MetaDataCodeConstants.FULLNAME ||
                                               x.Code == MetaDataCodeConstants.PHONENUMBER
                                               || x.Code == MetaDataCodeConstants.DOC_ID ||
                                               x.Code == MetaDataCodeConstants.DOC_3RD_ID ||
                                               x.Code == MetaDataCodeConstants.USER_CONNECT_ID ||
                                               x.Code == MetaDataCodeConstants.DOC_NAME ||
                                               x.Code == MetaDataCodeConstants.ORG_CODE
                                               || x.Code == MetaDataCodeConstants.CONTRACT_TYPE_ACTION)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<MetaDataSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<MetaDataSelectItemModel>> GetListFromCache()
        {
            string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
            var list = await _cacheService.GetOrCreate(cacheKey, async () =>
            {
                var data = (from item in _dataContext.MetaData.OrderBy(x => x.Order).ThenBy(x => x.Name)
                            select new MetaDataSelectItemModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                Note = item.Code,
                                Status = item.Status,
                                OrganizationId = item.OrganizationId
                            });

                return await data.ToListAsync();
            });
            return list;
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
    }
}