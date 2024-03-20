using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Org.BouncyCastle.X509;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class UserHSMAccountHandler : IUserHSMAccountHandler
    {
        private const string CachePrefix = CacheConstants.USER_HSM_ACCOUNT;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "HSC.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly string _raUrl = Utils.GetConfig("RAService:uri");

        public UserHSMAccountHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> Create(UserHSMAccountCreateOrUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = AutoMapperUtils.AutoMap<UserHSMAccountCreateOrUpdateModel, UserHSMAccount>(model);

                entity.CreatedDate = DateTime.Now;

                var checkCode = await _dataContext.UserHSMAccount.AnyAsync(x => x.Code == model.Code);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Mã cấu hình chữ ký đã tồn tại trong hệ thống!");
                    return new ResponseError(Code.ServerError, "Mã cấu hình chữ ký đã tồn tại trong hệ thống!");
                }
                if (model.IsDefault)
                {
                    await _dataContext.UserHSMAccount.Where(x => x.UserId == model.UserId).ForEachAsync(x => x.IsDefault = false);
                }

                entity.Id = Guid.NewGuid();

                #region Request RA Service

                try
                {
                    using (HttpClientHandler clientHandler = new HttpClientHandler())
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, cert, chain, sslPolicyErrors) => { return true; };

                        using (var client = new HttpClient(clientHandler))
                        {
                            HttpResponseMessage res = await client.GetAsync(_raUrl + "api/certificate?alias=" + model.Alias);
                            var responseText = await res.Content.ReadAsStringAsync();
                            Log.Logger.Information("Get Cert response model: " + responseText);

                            if (res.IsSuccessStatusCode)
                            {
                                var rs = JsonSerializer.Deserialize<RequestCertRAResponseModel>(responseText);
                                if (rs.Code == 1)
                                {
                                    entity.ChainCertificateBase64 = Utils.DecodeCertificate(rs.Data?.Certificate);
                                    entity.CertificateBase64 = entity.ChainCertificateBase64?.FirstOrDefault();
                                    entity.ValidFrom = DateTime.ParseExact(rs.Data?.ValidFrom, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    entity.ValidTo = DateTime.ParseExact(rs.Data?.ValidTo, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    entity.SubjectDN = rs.Data?.SubjectDn;
                                }
                            }
                        }
                    }
                }
                catch { }

                #endregion Request RA Service

                await _dataContext.UserHSMAccount.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Add success: " + JsonSerializer.Serialize(entity));
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

        public async Task<Response> CreateFromService(UserHSMAccountCreateOrUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create from service {CachePrefix}: " + JsonSerializer.Serialize(model));

                model.IsDefault = true;
                model.Code = model.SubjectDN;

                var entity = AutoMapperUtils.AutoMap<UserHSMAccountCreateOrUpdateModel, UserHSMAccount>(model);

                entity.CreatedDate = DateTime.Now;

                if (model.IsDefault)
                {
                    await _dataContext.UserHSMAccount.Where(x => x.UserId == model.UserId).ForEachAsync(x => x.IsDefault = false);
                }

                entity.Id = Guid.NewGuid();

                #region Request RA Service

                try
                {
                    using (HttpClientHandler clientHandler = new HttpClientHandler())
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, cert, chain, sslPolicyErrors) => { return true; };

                        using (var client = new HttpClient(clientHandler))
                        {
                            HttpResponseMessage res = await client.GetAsync(_raUrl + "api/certificate?alias=" + model.Alias);
                            var responseText = await res.Content.ReadAsStringAsync();
                            Log.Logger.Information("Get Cert response model: " + responseText);

                            if (res.IsSuccessStatusCode)
                            {
                                var rs = JsonSerializer.Deserialize<RequestCertRAResponseModel>(responseText);
                                if (rs.Code == 1)
                                {
                                    entity.ChainCertificateBase64 = Utils.DecodeCertificate(rs.Data?.Certificate);
                                    entity.CertificateBase64 = entity.ChainCertificateBase64?.FirstOrDefault();
                                    entity.ValidFrom = DateTime.ParseExact(rs.Data?.ValidFrom, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    entity.ValidTo = DateTime.ParseExact(rs.Data?.ValidTo, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    entity.SubjectDN = rs.Data?.SubjectDn;
                                }
                            }
                        }
                    }
                }
                catch { }

                #endregion Request RA Service

                await _dataContext.UserHSMAccount.AddAsync(entity);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Add success: " + JsonSerializer.Serialize(entity));
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

        public async Task<Response> Update(UserHSMAccountCreateOrUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.UserHSMAccount
                         .FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                if (model.IsDefault)
                {
                    await _dataContext.UserHSMAccount.Where(x => x.UserId == model.UserId).ForEachAsync(x => x.IsDefault = false);
                }
                model.UpdateToEntity(entity);

                if (!string.IsNullOrEmpty(model.UserPIN) && !model.UserPIN.Equals("******")) entity.UserPIN = model.UserPIN;
                if (string.IsNullOrEmpty(model.UserPIN)) entity.UserPIN = null;

                #region Request RA Service

                try
                {
                    using (HttpClientHandler clientHandler = new HttpClientHandler())
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (sender, cert, chain, sslPolicyErrors) => { return true; };

                        using (var client = new HttpClient(clientHandler))
                        {
                            HttpResponseMessage res = await client.GetAsync(_raUrl + "api/certificate?alias=" + model.Alias);
                            var responseText = await res.Content.ReadAsStringAsync();
                            Log.Logger.Information("Get Cert response model: " + responseText);

                            if (res.IsSuccessStatusCode)
                            {
                                var rs = JsonSerializer.Deserialize<RequestCertRAResponseModel>(responseText);
                                if (rs.Code == 1)
                                {
                                    entity.ChainCertificateBase64 = Utils.DecodeCertificate(rs.Data?.Certificate);
                                    entity.CertificateBase64 = entity.ChainCertificateBase64?.FirstOrDefault();
                                    entity.ValidFrom = DateTime.ParseExact(rs.Data?.ValidFrom, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    entity.ValidTo = DateTime.ParseExact(rs.Data?.ValidTo, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                    entity.SubjectDN = rs.Data?.SubjectDn;
                                }
                            }
                        }
                    }
                }
                catch { }

                #endregion Request RA Service

                _dataContext.UserHSMAccount.Update(entity);

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
                    var entity = await _dataContext.UserHSMAccount.FindAsync(item);
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
                        _dataContext.UserHSMAccount.Remove(entity);
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
                Log.Information($"{systemLog.TraceId} - List Result Delete: " + JsonSerializer.Serialize(listResult));
                return new ResponseObject<List<ResponeDeleteModel>>(listResult, MessageConstants.DeleteSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.DeleteErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Filter(UserHSMAccountQueryFilter filter)
        {
            try
            {
                var data = (from item in _dataContext.UserHSMAccount
                            select new UserHSMAccountModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Alias = item.Alias,
                                IsDefault = item.IsDefault,
                                UserId = item.UserId,
                                CreatedDate = item.CreatedDate,
                                ValidFrom = item.ValidFrom,
                                ValidTo = item.ValidTo,
                                AccountType = item.AccountType,
                                HasUserPIN = !string.IsNullOrEmpty(item.UserPIN),
                                SubjectDN = item.SubjectDN
                            });
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Code.ToLower().Contains(ts));
                }
                if (filter.UserId.HasValue)
                {
                    data = data.Where(x => x.UserId == filter.UserId);
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
                return new ResponseObject<PaginationList<UserHSMAccountModel>>(new PaginationList<UserHSMAccountModel>()
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

        public async Task<Response> GetListCombobox(int count = 0, Guid? userId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await GetListData();
                if (userId.HasValue)
                {
                    list = list.Where(x => x.UserId == userId).ToList();
                    //TODO: Lấy thông tin đã có PIN ADSS
                    var check = _dataContext.User.Find(userId.Value).IsNotRequirePINToSign;
                    foreach (var item in list)
                    {
                        if (item.AccountType == AccountType.ADSS)
                        {
                            item.IsHasUserPIN = check;
                        }
                    }
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }
                return new ResponseObject<List<UserHSMAccountSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListComboboxHSMValid(int count = 0, Guid? userId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await GetListData();
                // lấy cts valid hoặc ko có ngày hết hạn
                list = list.Where(x => (!x.ValidFrom.HasValue && !x.ValidTo.HasValue)
                    || (x.ValidFrom.HasValue && x.ValidTo.HasValue && x.ValidFrom.Value <= DateTime.Now && x.ValidTo.Value >= DateTime.Now)).ToList();

                if (userId.HasValue)
                {
                    list = list.Where(x => x.UserId == userId).ToList();
                    //TODO: Lấy thông tin đã có PIN ADSS
                    var check = _dataContext.User.Find(userId.Value).IsNotRequirePINToSign;
                    foreach (var item in list)
                    {
                        if (item.AccountType == AccountType.ADSS)
                        {
                            item.IsHasUserPIN = check;
                        }
                    }
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }
                return new ResponseObject<List<UserHSMAccountSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<List<UserHSMAccountSelectItemModel>> GetListData()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.UserHSMAccount.AsNoTracking().OrderBy(x => x.Order)
                                select new UserHSMAccountSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    UserId = item.UserId,
                                    IsHasUserPIN = !string.IsNullOrEmpty(item.UserPIN),
                                    IsDefault = item.IsDefault,
                                    Alias = item.Alias,
                                    UserPIN = item.UserPIN,
                                    AccountType = item.AccountType,
                                    ValidFrom = item.ValidFrom,
                                    ValidTo = item.ValidTo
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

        public async Task<Response> GetInfoCertificate(Guid userHSMAccountId, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - GetInfoCertificate for user_hsm_account: {userHSMAccountId}");

                var userHSMAccount = await _dataContext.UserHSMAccount.FindAsync(userHSMAccountId);
                string certificateBase64 = userHSMAccount?.CertificateBase64;

                CertificateInfoResponseModel certInfo = null;
                X509Certificate cert = null;
                if (!string.IsNullOrEmpty(certificateBase64))
                {
                    byte[] bytes = Convert.FromBase64String(certificateBase64);
                    cert = new X509CertificateParser().ReadCertificate(bytes);

                    if (cert != null)
                    {
                        certInfo = new CertificateInfoResponseModel
                        {
                            Version = cert.Version,
                            SerialNumber = cert.SerialNumber.ToString(),
                            SubjectName = Utils.GetValueByKeyOfDN(cert.SubjectDN.ToString(), "CN"),
                            IssuerName = Utils.GetValueByKeyOfDN(cert.IssuerDN.ToString(), "CN"),
                            SignatureAlgorithm = cert.SigAlgName,
                            NotBefore = cert.NotBefore,
                            NotAfter = cert.NotAfter,
                            Subject = cert.SubjectDN.ToString(),
                            Issuer = cert.IssuerDN.ToString(),
                        };
                    }
                }

                return new ResponseObject<CertificateInfoResponseModel>(certInfo, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, ex.Message);
            }
        }

        public async Task<MemoryStream> DownloadCertificate(Guid userHSMAccountId, SystemLogModel systemLog)
        {
            MemoryStream memoryStreamm = null;

            try
            {
                Log.Information($"{systemLog.TraceId} - DownloadCertificate: {userHSMAccountId}");

                var userHSMAccount = await _dataContext.UserHSMAccount.FindAsync(userHSMAccountId);
                string certificateBase64 = userHSMAccount?.CertificateBase64;
                byte[] bytes = Encoding.ASCII.GetBytes(certificateBase64);

                memoryStreamm = new MemoryStream(bytes);
                memoryStreamm.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
            }

            return memoryStreamm;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task<Response> GetListHSM(Guid userId, UserHSMAccountQueryFilter filter)
        {
            try
            {
                var data = (from item in _dataContext.UserHSMAccount
                            where item.UserId == userId
                            select new UserHSMAccountModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Alias = item.Alias,
                                IsDefault = item.IsDefault,
                                UserId = item.UserId,
                                CreatedDate = item.CreatedDate,
                                ValidFrom = item.ValidFrom,
                                ValidTo = item.ValidTo,
                                AccountType = item.AccountType,
                                HasUserPIN = !string.IsNullOrEmpty(item.UserPIN),
                                SubjectDN = item.SubjectDN,
                                Status = item.Status
                            });
                if (!string.IsNullOrEmpty(filter.TextSearch))
                {
                    string ts = filter.TextSearch.Trim().ToLower();
                    data = data.Where(x => x.Code.ToLower().Contains(ts));
                }

                if (filter.UserId.HasValue)
                {
                    data = data.Where(x => x.UserId == filter.UserId);
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
                return new ResponseObject<PaginationList<UserHSMAccountModel>>(new PaginationList<UserHSMAccountModel>()
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

        public async Task<Response> UpdateStatus(Guid userHSMAccountId, SystemLogModel systemLog)
        {
            try
            {
                var itemStatus = await _dataContext.UserHSMAccount.FirstOrDefaultAsync(x => x.Id == userHSMAccountId);
                itemStatus.Status = !itemStatus.Status;
                await _dataContext.SaveChangesAsync();

                return new Response(Code.Success, MessageConstants.UpdateSuccessMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
    }
}