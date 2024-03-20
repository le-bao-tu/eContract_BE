using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NetCore.Business
{
    public class QueueSendEmailHandler : IQueueSendEmailHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.EMAIL_ACCOUNT;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "QSE_";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IEmailHandler _emailHandler;

        public QueueSendEmailHandler(DataContext dataContext, ICacheService cacheService, IEmailHandler emailHandler)
        {
            _emailHandler = emailHandler;
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(QueueSendEmailCreateModel model)
        {
            try
            {
                Log.Information($"Add {CachePrefix}: " + JsonSerializer.Serialize(model));

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

                var email = list.Where(x => x.Code == model.EmailCode).FirstOrDefault();

                if (email == null)
                {
                    return new ResponseError(Code.ServerError, "Không tồn tại email tương ứng với mã: " + model.EmailCode);
                }

                var entity = AutoMapperUtils.AutoMap<QueueSendEmailCreateModel, QueueSendEmail>(model);

                entity.EmailAccountId = email.Id;

                //long identityNumber = 0;
                //var tempData = _dataContext.QueueSendEmail.Select(t => t.IdentityNumber);

                //if (await tempData.AnyAsync())
                //{
                //    identityNumber = await tempData.DefaultIfEmpty().MaxAsync();
                //}

                entity.CreatedDate = DateTime.Now;

                await _dataContext.QueueSendEmail.AddAsync(entity);

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

        public async Task<Response> CreateMany(List<QueueSendEmailCreateModel> list)
        {
            try
            {
                Log.Information($"Add {CachePrefix}: " + JsonSerializer.Serialize(list));
                var listId = new List<Guid>();
                var listRS = new List<QueueSendEmail>();

                //long identityNumber = 0;
                //var tempData = _dataContext.QueueSendEmail.Select(t => t.IdentityNumber);

                //if (await tempData.AnyAsync())
                //{
                //    identityNumber = await tempData.DefaultIfEmpty().MaxAsync();
                //}

                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var listEmail = await _cacheService.GetOrCreate(cacheKey, async () =>
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

                foreach (var item in list)
                {
                    var entity = AutoMapperUtils.AutoMap<QueueSendEmailCreateModel, QueueSendEmail>(item);

                    //entity.IdentityNumber = ++identityNumber;
                    //entity.Code = Utils.GenerateAutoCode(CodePrefix, identityNumber);

                    var email = listEmail.Where(x => x.Code == item.EmailCode).FirstOrDefault();

                    if (email == null)
                    {
                        return new ResponseError(Code.ServerError, "Không tồn tại email tương ứng với mã: " + item.EmailCode);
                    }

                    entity.EmailAccountId = email.Id;

                    entity.CreatedDate = DateTime.Now;
                    await _dataContext.QueueSendEmail.AddAsync(entity);
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

        public async Task<Response> SendMailInQueue()
        {
            try
            {
                Log.Information("Send Mail in Queue Working");
                List<Guid> listId = new List<Guid>();
                var listQueueEmails = await _dataContext.QueueSendEmail.Where(x => x.IsSended == false).ToListAsync();
                if (listQueueEmails.Count == 0)
                {
                    return new ResponseObject<List<Guid>>(null, "All Mail is Sended", Code.Success);
                }
                foreach (var queueEmail in listQueueEmails)
                {
                    string cacheKey = BuildCacheKey(queueEmail.Id.ToString());
                    var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                    {
                        var fromEmail = await _dataContext.EmailAccount.Where(x => x.Id == queueEmail.EmailAccountId)
                            .FirstOrDefaultAsync();
                        return AutoMapperUtils.AutoMap<EmailAccount, EmailAccountModel>(fromEmail);
                    });
                    var status = _emailHandler.SendMailGoogle(rs, queueEmail.ToEmails, queueEmail.CCEmails, queueEmail.BccEmails,
                        queueEmail.Title, queueEmail.Body);
                    if (status)
                    {
                        listId.Add(queueEmail.Id);
                        Log.Information("From: " + JsonSerializer.Serialize(rs));
                        Log.Information("Email in Queue: " + JsonSerializer.Serialize(queueEmail));
                        queueEmail.IsSended = true;
                    }
                }
                _dataContext.UpdateRange(listQueueEmails);
                var dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    return new ResponseObject<List<Guid>>(listId, MessageConstants.GetDataSuccessMessage, Code.Success);
                }
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> SendMailNow(QueueSendEmailCreateModel model)
        {
            try
            {
                Log.Information("Send Mail Working");

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

                var email = list.Where(x => x.Code == model.EmailCode).FirstOrDefault();

                if (email == null)
                {
                    return new ResponseError(Code.ServerError, "Không tồn tại email tương ứng với mã: " + model.EmailCode);
                }

                cacheKey = BuildCacheKey(email.Id.ToString());

                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var fromEmail = await _dataContext.EmailAccount.Where(x => x.Id == email.Id)
                        .FirstOrDefaultAsync();
                    return AutoMapperUtils.AutoMap<EmailAccount, EmailAccountModel>(fromEmail);
                });

                var status = _emailHandler.SendMailGoogle(rs, model.ToEmails, model.CCEmails, model.BccEmails,
                    model.Title, model.Body);

                var modelCreate = AutoMapperUtils.AutoMap<QueueSendEmailBaseModel, QueueSendEmail>(model);

                modelCreate.EmailAccountId = email.Id;
                modelCreate.IsSended = status;
                modelCreate.CreatedDate = DateTime.Now;

                _dataContext.QueueSendEmail.Add(modelCreate);
                var dbSave = await _dataContext.SaveChangesAsync();
                if (dbSave > 0)
                {
                    if (status)
                    {
                        Log.Information("From: " + JsonSerializer.Serialize(rs));
                        Log.Information("Email Sended: " + JsonSerializer.Serialize(model));

                        return new ResponseObject<Guid>(model.Id, "Gửi Email thành công", Code.Success);
                    }
                    else
                    {
                        Log.Information("From: " + JsonSerializer.Serialize(rs));
                        Log.Information("Email Send Fail: " + JsonSerializer.Serialize(model));

                        return new ResponseObject<Guid>(model.Id, "Gửi Email thất bại", Code.ServerError);
                    }
                }
                else
                {
                    return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage}");
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

    }
}