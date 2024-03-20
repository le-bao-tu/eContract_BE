using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace NetCore.Business
{
    public class RightHandler : IRightHandler
    {
        private const string CachePrefix = CacheConstants.RIGHT;
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;

        public RightHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(RightCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));

                // Check code unique
                var checkCode = await _dataContext.Right.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} fail: Code {model.Code} is exist!");
                    return new ResponseError(Code.Conflict, $"Mã {model.Code} đã tồn tại trong hệ thống");
                }

                var entity = AutoMapperUtils.AutoMap<RightModel, Right>(model);
                entity.CreatedDate = DateTime.Now;
                entity.CreatedUserId = model.CreatedUserId;

                await _dataContext.Right.AddAsync(entity);

                //if (model.RoleIds != null)
                //{
                //    var listRoleMapRight = new List<RoleMapRight>();
                //    foreach (var item in model.RoleIds)
                //    {
                //        var rightRole = new RoleMapRight()
                //        {
                //            Id = Guid.NewGuid(),
                //            RightId = entity.Id,
                //            RoleId = item
                //        };
                //        listRoleMapRight.Add(rightRole);
                //    }

                //    if (listRoleMapRight.Count > 0) await _dataContext.RoleMapRight.AddRangeAsync(listRoleMapRight);
                //}

                var dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Create {CachePrefix} success: " + JsonSerializer.Serialize(entity));
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
                    var entity = await _dataContext.Right.FindAsync(item);

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

                        // xóa right role
                        var rightRoles = await _dataContext.RoleMapRight.Where(x => x.RightId == entity.Id).ToListAsync();
                        _dataContext.RoleMapRight.RemoveRange(rightRoles);

                        // xóa right
                        _dataContext.Right.Remove(entity);
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
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteItemSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetById(Guid id, SystemLogModel systemLog)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Right.FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Right, RightModel>(entity);
                });

                return new ResponseObject<RightModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "")
        {
            try
            {
                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.Right.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                //                select new RightSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Code = item.Code,
                //                    Name = item.Name,
                //                    GroupName = item.GroupName,
                //                    Note = ""
                //                });

                //    return await data.ToListAsync();
                //});

                var list = await GetListRightFromCacheAsync();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<RightSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<RightSelectItemModel>> GetListRightFromCacheAsync()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Right.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new RightSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    GroupName = item.GroupName,
                                    Note = ""
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

        public async Task<Response> Update(RightUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.Right.FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.Right.Update(entity);

                //if (model.RoleIds != null && model.RoleIds.Count > 0)
                //{
                //    // xóa role cũ
                //    var roles = await _dataContext.RoleMapRight.Where(x => x.RightId == entity.Id).ToListAsync();
                //    _dataContext.RoleMapRight.RemoveRange(roles);

                //    // insert role mới
                //    var listRightMapRole = new List<RoleMapRight>();
                //    foreach (var item in model.RoleIds)
                //    {
                //        var rightRole = new RoleMapRight()
                //        {
                //            Id = Guid.NewGuid(),
                //            RightId = entity.Id,
                //            RoleId = item
                //        };
                //        listRightMapRole.Add(rightRole);
                //    }

                //    if (listRightMapRole.Count > 0) await _dataContext.RoleMapRight.AddRangeAsync(listRightMapRole);
                //}

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
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
                return new ResponseError(Code.ServerError, $"{MessageConstants.UpdateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(RightQueryFilter filter, SystemLogModel systemLog)
        {
            try
            {
                var data = (from item in _dataContext.Right
                            select new RightBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                GroupName = item.GroupName,
                                Status = item.Status,
                                Description = item.Description,
                                CreatedDate = item.CreatedDate
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
                return new ResponseObject<PaginationList<RightBaseModel>>(new PaginationList<RightBaseModel>()
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
