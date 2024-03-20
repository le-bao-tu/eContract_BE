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
    public class PositionHandler : IPositionHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.POSITION;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "POS.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;

        public PositionHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(PositionCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = AutoMapperUtils.AutoMap<PositionCreateModel, Position>(model);

                entity.OrganizationId = model.OrganizationId;
                entity.CreatedDate = DateTime.Now;

                //long identityNumber = await _dataContext.Position.DefaultIfEmpty().MaxAsync(x => x.IdentityNumber);

                //entity.IdentityNumber = ++identityNumber;
                //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                entity.Id = Guid.NewGuid();
                await _dataContext.Position.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới {CachePrefix} mã: {entity.Code}",
                        ObjectCode = CachePrefix,
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
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateMany(List<PositionCreateModel> list, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - CreateMany {CachePrefix}: " + JsonSerializer.Serialize(list));

                var listId = new List<Guid>();
                var listRS = new List<Position>();
                foreach (var item in list)
                {
                    var entity = AutoMapperUtils.AutoMap<PositionCreateModel, Position>(item);
                   
                    entity.CreatedDate = DateTime.Now;
                    await _dataContext.Position.AddAsync(entity);
                    listId.Add(entity.Id);
                    listRS.Add(entity);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - CreateMany success: " + JsonSerializer.Serialize(listRS));
                    InvalidCache();

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Thêm mới danh sách {CachePrefix}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<List<Guid>>(listId, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - CreateMany {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(PositionUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.Position
                         .FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.Position.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());

                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = $"Cập nhập {CachePrefix} mã:{entity.Code}",
                        ObjectCode = CachePrefix,
                        CreatedDate = DateTime.Now
                    });

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Update {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Delete: " + JsonSerializer.Serialize(listId));

                var listResult = new List<ResponeDeleteModel>();
                var name = "";

                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.Position.FindAsync(item);

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
                        _dataContext.Position.Remove(entity);
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
                                    Description = $"Xóa {CachePrefix} mã: {entity.Code}",
                                    ObjectCode = CachePrefix,
                                    CreatedDate = DateTime.Now
                                });
                            }
                            else
                            {
                                Log.Error($"{systemLog.TraceId} - Delete {CachePrefix} error: Save database error!");
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
                            Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
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
                Log.Information($"{systemLog.TraceId} - List {CachePrefix} Result Delete: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(PositionQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.Position

                            select new PositionBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
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

                if (filter.OrganizationId.HasValue)
                {
                    data = data.Where(x => x.OrganizationId == filter.OrganizationId);
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
                return new ResponseObject<PaginationList<PositionBaseModel>>(new PaginationList<PositionBaseModel>()
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
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
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
                    var entity = await _dataContext.Position
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Position, PositionModel>(entity);
                });
                return new ResponseObject<PositionModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Position.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new PositionSelectItemModel()
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

                return new ResponseObject<List<PositionSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
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
    }
}