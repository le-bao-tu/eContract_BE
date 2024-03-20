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
    public class OrganizationHandler : IOrganizationHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.ORGANIZATION;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;
        private readonly IUserRoleHandler _userRoleHandler;
        private readonly IOrganizationConfigHandler _orgConfigHandler;

        private string defaultPassword = "";

        public OrganizationHandler(IUserRoleHandler userRoleHandler, IOrganizationConfigHandler orgConfigHandler, DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
            _userRoleHandler = userRoleHandler;
            _orgConfigHandler = orgConfigHandler;
        }

        public async Task<Response> Create(OrganizationCreateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Add {CachePrefix}: " + JsonSerializer.Serialize(model));
                // Check code unique
                var checkCode = await _dataContext.Organization.AnyAsync(x => x.Code == model.Code && !x.IsDeleted);
                if (checkCode)
                {
                    Log.Information($"{systemLog.TraceId} - Add {CachePrefix} fail: Code {model.Code} is exist!");
                    return new ResponseError(Code.ServerError, $"Mã {model.Code} đã tồn tại trong hệ thống");
                }

                //var checkTaxCode = await _dataContext.Organization.AnyAsync(x => x.TaxCode == model.TaxCode && !x.IsDeleted);
                //if (checkTaxCode)
                //{
                //    Log.Information($"Add {CachePrefix} fail: TaxCode {model.TaxCode} is exist!");
                //    return new ResponseError(Code.ServerError, $"Mã số thuế {model.TaxCode} đã tồn tại trong hệ thống");
                //}

                var entity = AutoMapperUtils.AutoMap<OrganizationCreateModel, Organization>(model);

                //Generate path
                if (entity.ParentId != null)
                {
                    var parent = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == entity.ParentId && !x.IsDeleted);
                    if (parent == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Add {CachePrefix} fail: ParentId {entity.ParentId} is not exist!");
                        return new ResponseError(Code.ServerError, $"Đơn vị cha không tồn tại trong hệ thống");
                    }
                    entity.Path = parent.Path + entity.Code + "/";
                }
                else
                {
                    entity.Path = entity.Code + "/";
                }

                entity.CreatedDate = DateTime.Now;

                await _dataContext.Organization.AddAsync(entity);

                //Thêm cấu hình đơn vị
                OrganizationConfig orgConfig = new OrganizationConfig()
                {
                    OrganizationTitle = entity.Code,
                    CreatedDate = DateTime.Now,
                    CreatedUserId = entity.CreatedUserId,
                    MaxDocumentType = 2000,
                    TemplatePerDocumentType = 1,
                    IsApproveLTV = true,
                    IsApproveTSA = true,
                    OrganizationId = entity.Id,
                    Status = true,
                    EmailConfig = new EmailConfig()
                };
                await _dataContext.OrganizationConfig.AddAsync(orgConfig);

                //// Add user
                //var user = new User()
                //{
                //    Id = Guid.NewGuid(),
                //    Email = entity.Email,
                //    Code = entity.TaxCode,
                //    IssuerBy = entity.IssuerBy,
                //    IssuerDate = entity.IssuerDate,
                //    ApplicationId = entity.ApplicationId,
                //    Birthday = null,
                //    CountryId = entity.CountryId,
                //    ProvinceId = entity.ProvinceId,
                //    PhoneNumber = entity.PhoneNumber,
                //    CreatedDate = DateTime.Now,
                //    CreatedUserId = entity.CreatedUserId,
                //    Description = entity.Description,
                //    IdentityNumber = entity.TaxCode,
                //    IdentityType = "MST",
                //    UserName = entity.TaxCode.Trim().ToLower(),
                //    IsLock = false,
                //    Status = true,
                //    Name = model.Name,
                //    Order = 0,
                //    OrganizationId = entity.Id,
                //    Sex = GenderEnum.UNKNOW,
                //    Type = UserType.ORG
                //};

                //user.PasswordSalt = Utils.PassowrdCreateSalt512();
                //user.Password = Utils.PasswordGenerateHmac(defaultPassword, user.PasswordSalt);

                //await _dataContext.User.AddAsync(user);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - Add {CachePrefix} success: " + JsonSerializer.Serialize(entity));
                    InvalidCache();
                    InvalidCacheUser();
                    //TODO: Gửi mail cho khách hàng thông báo đăng ký tài khoản thành công

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
                    Log.Error($"{systemLog.TraceId} - Add {CachePrefix} error: Save database error!");
                    return new ResponseError(Code.ServerError, MessageConstants.CreateErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - {MessageConstants.ErrorLogMessage}");
                return new ResponseError(Code.ServerError, $"{MessageConstants.CreateErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> Update(OrganizationUpdateModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Update {CachePrefix}: " + JsonSerializer.Serialize(model));

                var entity = await _dataContext.Organization
                         .FirstOrDefaultAsync(x => x.Id == model.Id);

                Log.Information($"{systemLog.TraceId} - Before Update {CachePrefix}: " + JsonSerializer.Serialize(entity));

                if (entity == null)
                {
                    Log.Information($"{systemLog.TraceId} - Add {CachePrefix} fail: entity {entity.Id} is not exist!");
                    return new ResponseError(Code.ServerError, $"Đơn vị không tồn tại trong hệ thống");
                }

                model.UpdateToEntity(entity);

                //Update path
                if (entity.ParentId != null)
                {
                    var parent = await _dataContext.Organization.FirstOrDefaultAsync(x => x.Id == entity.ParentId);
                    if (parent == null)
                    {
                        Log.Information($"{systemLog.TraceId} - Add {CachePrefix} fail: ParentId {entity.ParentId} is not exist!");
                        return new ResponseError(Code.ServerError, $"Đơn vị cha không tồn tại trong hệ thống");
                    }
                    entity.Path = parent.Path + entity.Code + "/";
                }
                else
                {
                    entity.Path = entity.Code + "/";
                }

                _dataContext.Organization.Update(entity);

                // Cập nhật người dùng thuộc đơn vị
                // Lấy ra người dùng
                //var userOrg = await _dataContext.User
                //         .FirstOrDefaultAsync(x => x.OrganizationId.HasValue && x.OrganizationId.Value == model.Id && x.Type == UserType.ORG);

                //if (userOrg != null)
                //{
                //    userOrg.Email = entity.Email;
                //    //userOrg.Code = entity.TaxCode;
                //    userOrg.IssuerBy = entity.IssuerBy;
                //    userOrg.IssuerDate = entity.IssuerDate;
                //    userOrg.ApplicationId = entity.ApplicationId;
                //    //userOrg.Birthday = null;
                //    userOrg.CountryId = entity.CountryId;
                //    userOrg.ProvinceId = entity.ProvinceId;
                //    userOrg.PhoneNumber = entity.PhoneNumber;
                //    //userOrg.CreatedDate = DateTime.Now;
                //    //userOrg.CreatedUserId = entity.CreatedUserId;
                //    userOrg.ModifiedUserId = entity.ModifiedUserId;
                //    userOrg.Description = entity.Description;
                //    //userOrg.IdentifyNumber = entity.TaxCode;
                //    //userOrg.UserName = entity.TaxCode;
                //    //userOrg.IsLock = false;
                //    //userOrg.Status = true;
                //    userOrg.Name = model.Name;
                //    userOrg.Order = 0;
                //    //userOrg.OrganizationId = entity.Id;
                //    //userOrg.Sex = GenderEnum.UNKNOW;
                //    //userOrg.Type = UserType.ORG;

                //    _dataContext.User.Update(userOrg);
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
                    var entity = await _dataContext.Organization.FindAsync(item);

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
                        // Kiểm tra user của đơn vị
                        //var checkUser = await _dataContext.User
                        //.AnyAsync(x => x.OrganizationId.HasValue && x.OrganizationId.Value == item && x.Type == UserType.USER && x.IsDeleted == false);

                        // Kiểm tra đơn vị con
                        var checkOrg = await _dataContext.Organization
                                 .AnyAsync(x => x.ParentId.HasValue && x.ParentId.Value == item && x.IsDeleted == false);

                        if (checkOrg)
                        {
                            listResult.Add(new ResponeDeleteModel()
                            {
                                Id = item,
                                Name = name,
                                Result = false,
                                Message = "Không thể xóa đơn vị có đơn vị con"
                            });
                        }
                        //else if (checkUser)
                        //{
                        //    listResult.Add(new ResponeDeleteModel()
                        //    {
                        //        Id = item,
                        //        Name = name,
                        //        Result = false,
                        //        Message = "Không thể xóa đơn vị đang tồn tại người dùng"
                        //    });
                        //}
                        else
                        {
                            name = entity.Name;
                            entity.IsDeleted = true;
                            _dataContext.Organization.Update(entity);

                            // Lấy ra người dùng
                            //var userOrg = await _dataContext.User
                            //         .FirstOrDefaultAsync(x => x.OrganizationId.HasValue && x.OrganizationId.Value == item && x.Type == UserType.ORG);

                            //if (userOrg != null)
                            //{
                            //    userOrg.IsDeleted = true;

                            //    _dataContext.User.Update(userOrg);
                            //}

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

        public async Task<Response> Filter(OrganizationQueryFilter filter)
        {
            try
            {
                var data = (from item in _dataContext.Organization
                            where item.IsDeleted == false
                            select new OrganizationBaseModel()
                            {
                                Id = item.Id,
                                Code = item.Code,
                                Name = item.Name,
                                Status = item.Status,
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
                return new ResponseObject<PaginationList<OrganizationBaseModel>>(new PaginationList<OrganizationBaseModel>()
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

        public async Task<OrganizationModel> GetByCode(string code)
        {
            try
            {
                string cacheKey = BuildCacheKey(code.ToString());
                _cacheService.Remove(cacheKey);

                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Code == code);

                    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                });
                return rs;
                //return new ResponseObject<OrganizationModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
                //return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetRootByChidId(Guid id)
        {
            try
            {
                var rs = await GetRootOrgModelByChidId(id);
                return new ResponseObject<OrganizationModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
        public async Task<OrganizationModel> GetRootOrgModelByChidId(Guid id)
        {
            try
            {
                var list = await GetAllListOrgFromCacheAsync();

                var org = list.Where(x => x.Id == id).FirstOrDefault();
                if (org == null)
                {
                    return null;
                }

                while (org.ParentId != null)
                {
                    org = list.Where(x => x.Id == org.ParentId).FirstOrDefault();
                }

                string cacheKeyRootOrgId = BuildCacheKey(org.Id.ToString());

                var rs = await _cacheService.GetOrCreate(cacheKeyRootOrgId, async () =>
                {
                    var entity = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == org.Id);

                    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                });
                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Response> GetById(Guid id)
        {
            try
            {
                //string cacheKey = BuildCacheKey(id.ToString());
                //_cacheService.Remove(cacheKey);

                //var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var entity = await _dataContext.Organization
                //        .FirstOrDefaultAsync(x => x.Id == id);

                //    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                //});

                var rs = await GetOrgFromCache(id);

                var ms = new MinIOService();
                if (!string.IsNullOrEmpty(rs.BussinessLicenseBucketName) && !string.IsNullOrEmpty(rs.BussinessLicenseObjectName))
                    rs.BussinessLicenseFilePath = await ms.GetObjectPresignUrlAsync(rs.BussinessLicenseBucketName, rs.BussinessLicenseObjectName);
                if (!string.IsNullOrEmpty(rs.IdentityFrontBucketName) && !string.IsNullOrEmpty(rs.IdentityFrontObjectName))
                    rs.IdentityFrontFilePath = await ms.GetObjectPresignUrlAsync(rs.IdentityFrontBucketName, rs.IdentityFrontObjectName);
                if (!string.IsNullOrEmpty(rs.IdentityBackBucketName) && !string.IsNullOrEmpty(rs.IdentityBackObjectName))
                    rs.IdentityBackFilePath = await ms.GetObjectPresignUrlAsync(rs.IdentityBackBucketName, rs.IdentityBackObjectName);

                return new ResponseObject<OrganizationModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<OrganizationModel> GetOrgFromCache(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                _cacheService.Remove(cacheKey);

                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                });

                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        public async Task<Response> GetOrgHeaderInfo(Guid id)
        {
            try
            {
                string cacheKey = BuildCacheKey(id.ToString());
                _cacheService.Remove(cacheKey);

                var childOrg = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                });

                var list = await GetAllListOrgFromCacheAsync();

                var org = list.Where(x => x.Id == id).FirstOrDefault();

                while (org.ParentId != null)
                {
                    org = list.Where(x => x.Id == org.ParentId).FirstOrDefault();
                }

                string cacheKeyRootOrgId = BuildCacheKey(org.Id.ToString());
                var rootOrg = await _cacheService.GetOrCreate(cacheKeyRootOrgId, async () =>
                {
                    var entity = await _dataContext.Organization
                        .FirstOrDefaultAsync(x => x.Id == id);

                    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                });

                var rootOrgConfig = await _orgConfigHandler.GetByOrgId(rootOrg.Id);
                OrgLayoutModel layout;

                if (childOrg.Id == rootOrg.Id)
                {
                    layout = new OrgLayoutModel()
                    {
                        Id = id,
                        DisplayName = rootOrg.Name,
                        OrgName = org.Name,
                        LogoBase64 = rootOrgConfig.LogoFileBase64
                    };
                }
                else
                {
                    layout = new OrgLayoutModel()
                    {
                        Id = id,
                        DisplayName = rootOrg.Name + " - " + childOrg.Name,
                        OrgName = childOrg.Name,
                        LogoBase64 = rootOrgConfig.LogoFileBase64
                    };
                }

                return new ResponseObject<OrgLayoutModel>(layout, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListCombobox(Guid userId, Guid organizationId, int count = 0, string textSearch = "")
        {
            try
            {
                //Nếu tài khoản quản trị show hết
                var userRole = await _userRoleHandler.GetByUserId(userId);
                bool isOrgAdmin = false;
                if (userRole != null && userRole.GetPropValue("Data") != null)
                {
                    isOrgAdmin = (bool)userRole?.GetPropValue("Data")?.GetPropValue("IsOrgAdmin");
                }
                List<Guid> listChildOrgID = GetListChildOrgByParentID(organizationId);

                //Nếu không phải admin đơn vị thì chỉ xem được:
                //hợp đồng mình tạo
                //hợp đồng liên quan đến mình
                //hợp đồng ở các đơn vị cấp dưới
                if (!isOrgAdmin)
                    listChildOrgID.Remove(organizationId);

                //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                //var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var data = (from item in _dataContext.Organization.Where(x => x.Status && !x.IsDeleted && listChildOrgID.Contains(x.Id)).OrderBy(x => x.Order).ThenBy(x => x.Name)
                //                select new OrganizationSelectItemModel()
                //                {
                //                    Id = item.Id,
                //                    Code = item.Code,
                //                    Name = item.Name,
                //                    Note = "",
                //                    ParentId = item.ParentId
                //                });

                //    return await data.ToListAsync();
                //});

                var list = await GetListOrgFromCacheAsync();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<OrganizationSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<Response> GetListComboboxAll(bool? status = null, int count = 0, string textSearch = "")
        {
            try
            {                        
                var list = await GetAllListOrgFromCacheAsync();
                if (status.HasValue)
                {
                    list = list.Where(x => x.Status == status).ToList();
                }

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<OrganizationSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
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

        private void InvalidCacheUser(string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                string cacheKey = BuildCacheKeyUser(id);
                _cacheService.Remove(cacheKey);
            }

            string selectItemCacheKey = BuildCacheKey(SelectItemCacheSubfix);
            _cacheService.Remove(selectItemCacheKey);
        }

        private string BuildCacheKeyUser(string id)
        {
            return $"User-{id}";
        }

        public async Task<Response> GetDettailForServiceById(Guid id)
        {
            try
            {
                //string cacheKey = BuildCacheKey(id.ToString());
                //var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                var entity = await _dataContext.Organization
                    .FirstOrDefaultAsync(x => x.Id == id);

                var user = await _dataContext.User.Where(x => x.OrganizationId == id && x.UserName.EndsWith("admin")).OrderBy(x => x.CreatedDate).FirstOrDefaultAsync();

                OrganizationForServiceModel model = new OrganizationForServiceModel()
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Name = entity.Name,
                    UserId = user != null ? user.Id : Guid.Empty,
                    UserName = user != null ? user.UserName : "",
                };

                //    return AutoMapperUtils.AutoMap<Organization, OrganizationModel>(entity);
                //});
                return new ResponseObject<OrganizationForServiceModel>(model, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public List<Guid> GetListChildOrgByParentID(Guid parentID)
        {
            //string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
            //var list = _cacheService.GetOrCreate(cacheKey, async () =>
            //{
            //    var data = (from item in _dataContext.Organization.Where(x => x.Status && !x.IsDeleted).OrderBy(x => x.Order).ThenBy(x => x.Name)
            //                select new OrganizationSelectItemModel()
            //                {
            //                    Id = item.Id,
            //                    Code = item.Code,
            //                    Name = item.Name,
            //                    Note = "",
            //                    ParentId = item.ParentId
            //                });

            //    return await data.ToListAsync();
            //});

            var list = GetAllListOrgFromCacheAsync();

            List<Guid> listOrgID = new List<Guid>();
            var listOrg = list.Result
                        .Select(r => new OrganizationSelectItemModel
                        {
                            Id = r.Id,
                            ParentId = r.ParentId
                        }).ToList();

            GetListChildOrgID(parentID, listOrg, listOrgID);
            listOrgID.Add(parentID);

            return listOrgID;
        }

        public void GetListChildOrgID(Guid parentID, List<OrganizationSelectItemModel> listOrg, List<Guid> listOrgID)
        {
            var listOrgChildID = listOrg.Where(s => s.ParentId.Equals(parentID)).Select(r => r.Id).ToList();

            if (listOrgChildID.Any())
                listOrgID.AddRange(listOrgChildID);

            foreach (Guid id in listOrgChildID)
            {
                GetListChildOrgID(id, listOrg, listOrgID);
            }
        }

        #region Service dùng chung
        private async Task<List<OrganizationSelectItemModel>> GetListOrgFromCacheAsync()
        {
            var list = await GetAllListOrgFromCacheAsync();
            return list.Where(x => x.Status).ToList();
        }

        public async Task<List<OrganizationSelectItemModel>> GetAllListOrgFromCacheAsync()
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.Organization.Where(x => !x.IsDeleted).OrderBy(x => x.Order).ThenBy(x => x.Name)
                                select new OrganizationSelectItemModel()
                                {
                                    Id = item.Id,
                                    Code = item.Code,
                                    Name = item.Name,
                                    Status = item.Status,
                                    Note = "",
                                    ParentId = item.ParentId
                                });

                    return await data.ToListAsync();
                });
                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }
        #endregion

        public async Task<Response> GetListComboboxCurrentOrgOfUser(Guid userId, Guid organizationId, int count = 0, string textSearch = "")
        {
            try
            {
                ////Nếu tài khoản quản trị show hết
                //var userRole = await _userRoleHandler.GetById(userId);
                //bool isOrgAdmin = false;
                //if (userRole != null && userRole.GetPropValue("Data") != null)
                //{
                //    isOrgAdmin = (bool)userRole?.GetPropValue("Data")?.GetPropValue("IsOrgAdmin");
                //}
                //List<Guid> listChildOrgID = GetListChildOrgByParentID(organizationId);


                var rootOrg = await GetRootOrgModelByChidId(organizationId);
                List<Guid> listChildOrgID = GetListChildOrgByParentID(rootOrg.Id);

                ////Nếu không phải admin đơn vị thì chỉ xem được:
                ////hợp đồng mình tạo
                ////hợp đồng liên quan đến mình
                ////hợp đồng ở các đơn vị cấp dưới
                //if (!isOrgAdmin)
                //    listChildOrgID.Remove(organizationId);

                var list = await GetListOrgFromCacheAsync();
                list = list.Where(x => listChildOrgID.Contains(x.Id)).ToList();

                if (!string.IsNullOrEmpty(textSearch))
                {
                    textSearch = textSearch.ToLower().Trim();
                    list = list.Where(x => x.Name.ToLower().Contains(textSearch) || x.Note.ToLower().Contains(textSearch)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<OrganizationSelectItemModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }
    }
}