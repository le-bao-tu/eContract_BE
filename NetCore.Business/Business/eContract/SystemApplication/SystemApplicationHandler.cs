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
    public class SystemApplicationHandler : ISystemApplicationHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.SYS_APP;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "APP.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;

        public SystemApplicationHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(SystemApplicationCreateModel model)
        {
            try
            {
                var entity = AutoMapperUtils.AutoMap<SystemApplicationCreateModel, SystemApplication>(model);

                entity.CreatedDate = DateTime.Now;

                //long identityNumber = await _dataContext.SystemApplication.DefaultIfEmpty().MaxAsync(x => x.IdentityNumber);

                //entity.IdentityNumber = ++identityNumber;
                //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                entity.Id = Guid.NewGuid();
                await _dataContext.SystemApplication.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information("Add success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateMany(List<SystemApplicationCreateModel> list)
        {
            try
            {

                var listId = new List<Guid>();
                var listRS = new List<SystemApplication>();
                foreach (var item in list)
                {
                    var entity = AutoMapperUtils.AutoMap<SystemApplicationCreateModel, SystemApplication>(item);

                    entity.CreatedDate = DateTime.Now;
                    await _dataContext.SystemApplication.AddAsync(entity);
                    listId.Add(entity.Id);
                    listRS.Add(entity);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information("Add success: " + JsonSerializer.Serialize(listRS));
                    InvalidCache();

                    return new ResponseObject<List<Guid>>(listId, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(SystemApplicationUpdateModel model)
        {
            try
            {
                var entity = await _dataContext.SystemApplication
                         .FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information("Before Update: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.SystemApplication.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information("After Update: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    return new ResponseError(Code.ServerError, MessageConstants.UpdateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Delete(List<Guid> listId)
        {
            try
            {
                var listResult = new List<ResponeDeleteModel>();
                var name = "";
                Log.Information("List Delete: " + JsonSerializer.Serialize(listId));
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.SystemApplication.FindAsync(item);

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
                        _dataContext.SystemApplication.Remove(entity);
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

        public async Task<Response> Filter(SystemApplicationQueryFilter filter)
        {
            try
            {
                var data = (from account in _dataContext.SystemApplication

                            select new SystemApplicationBaseModel()
                            {
                                Id = account.Id,
                                Code = account.Code,
                                Name = account.Name,
                                Status = account.Status,
                                CreatedDate = account.CreatedDate,
                                Order = account.Order,
                                Description = account.Description
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
                return new ResponseObject<PaginationList<SystemApplicationBaseModel>>(new PaginationList<SystemApplicationBaseModel>()
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
                    var entity = await _dataContext.SystemApplication
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<SystemApplication, SystemApplicationModel>(entity);
                });
                return new ResponseObject<SystemApplicationModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
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
                    var data = (from item in _dataContext.SystemApplication.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
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