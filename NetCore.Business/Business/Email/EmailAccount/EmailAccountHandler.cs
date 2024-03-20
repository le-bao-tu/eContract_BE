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
    public class EmailAccountHandler : IEmailAccountHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.EMAIL_ACCOUNT;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "EA_";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;

        public EmailAccountHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(EmailAccountCreateModel model)
        {
            try
            {
                Log.Information($"Add {CachePrefix}: " + JsonSerializer.Serialize(model));
                // Check code unique
                var checkCode = await _dataContext.EmailAccount.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    Log.Information($"Add {CachePrefix} fail: Code {model.Code} is exist!");
                    return new ResponseError(Code.ServerError, $"Mã {model.Code} đã tồn tại trong hệ thống");
                }

                var entity = AutoMapperUtils.AutoMap<EmailAccountCreateModel, EmailAccount>(model);

                long identityNumber = 0;
                var tempData = _dataContext.EmailAccount.Select(t => t.IdentityNumber);

                if (await tempData.AnyAsync())
                {
                    identityNumber = await tempData.DefaultIfEmpty().MaxAsync();
                    identityNumber += 1;
                }

                // Lấy số identityNumber từ Code
                try
                {
                    if (model.Code.Length > 3)
                    {
                        var position = model.Code.IndexOf("_");
                        string tmp = model.Code.Substring(position + 1);
                        identityNumber = Convert.ToInt64(tmp);
                    }
                }
                catch
                {
                }

                entity.IdentityNumber = identityNumber;

                entity.CreatedDate = DateTime.Now;

                await _dataContext.EmailAccount.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"Add {CachePrefix} success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();

                    return new ResponseObject<Guid>(entity.Id, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"Add {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> CreateMany(List<EmailAccountCreateModel> list)
        {
            try
            {
                Log.Information($"Add {CachePrefix}: " + JsonSerializer.Serialize(list));
                var listId = new List<Guid>();
                var listRS = new List<EmailAccount>();

                long identityNumber = 0;
                var tempData = _dataContext.EmailAccount.Select(t => t.IdentityNumber);

                if (await tempData.AnyAsync())
                {
                    identityNumber = await tempData.DefaultIfEmpty().MaxAsync();
                }

                foreach (var item in list)
                {
                    // Check code unique
                    //var checkCode = await _dataContext.EmailAccount.AnyAsync(x => x.Code == item.Code);
                    //if (checkCode)
                    //{
                    //    Log.Information($"Add {CachePrefix} fail: Code {item.Code} is exist!");
                    //    return new ResponseError(Code.ServerError, $"Mã {item.Code} đã tồn tại trong hệ thống");
                    //}

                    var entity = AutoMapperUtils.AutoMap<EmailAccountCreateModel, EmailAccount>(item);

                    entity.IdentityNumber = ++identityNumber;
                    entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                    entity.CreatedDate = DateTime.Now;
                    await _dataContext.EmailAccount.AddAsync(entity);
                    listId.Add(entity.Id);
                    listRS.Add(entity);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"Add {CachePrefix} success: " + JsonSerializer.Serialize(listRS));
                    InvalidCache();

                    return new ResponseObject<List<Guid>>(listId, MessageConstants.CreateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"Add {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(EmailAccountUpdateModel model)
        {
            try
            {
                var entity = await _dataContext.EmailAccount
                         .FirstOrDefaultAsync(x => x.Id == model.Id);
                Log.Information($"Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                model.UpdateToEntity(entity);

                _dataContext.EmailAccount.Update(entity);

                int dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    Log.Information($"After Update {CachePrefix}: " + JsonSerializer.Serialize(entity));
                    InvalidCache(model.Id.ToString());

                    return new ResponseObject<Guid>(model.Id, MessageConstants.UpdateSuccessMessage, Code.Success);
                }
                else
                {
                    Log.Error($"Update {CachePrefix} error: Save database error!");
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
                Log.Information($"List {CachePrefix} Delete: " + JsonSerializer.Serialize(listId));
                foreach (var item in listId)
                {
                    name = "";
                    var entity = await _dataContext.EmailAccount.FindAsync(item);

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
                        _dataContext.EmailAccount.Remove(entity);
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
                                Log.Error($"Delete {CachePrefix} error: Save database error!");
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
                Log.Information($"List {CachePrefix} Result Delete: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);

            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(EmailAccountQueryFilter filter)
        {
            try
            {
                var data = (from account in _dataContext.EmailAccount

                            select new EmailAccountBaseModel()
                            {
                                Id = account.Id,
                                Code = account.Code,
                                Name = account.Name,
                                Status = account.Status,
                                From = account.From,
                                Smtp = account.Smtp,
                                Port = account.Port,
                                User = account.User,
                                SendType = account.SendType,
                                Password = account.Password,
                                Ssl = account.Ssl,
                                CreatedDate = account.CreatedDate
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
                return new ResponseObject<PaginationList<EmailAccountBaseModel>>(new PaginationList<EmailAccountBaseModel>()
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
                    var entity = await _dataContext.EmailAccount
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<EmailAccount, EmailAccountModel>(entity);
                });
                return new ResponseObject<EmailAccountModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
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
                    var data = (from item in _dataContext.EmailAccount.Where(x => x.Status == true).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new EmailAccountSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    Note = ""
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

                return new ResponseObject<List<EmailAccountSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
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