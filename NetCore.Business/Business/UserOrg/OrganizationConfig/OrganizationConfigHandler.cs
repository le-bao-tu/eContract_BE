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
    public class OrganizationConfigHandler : IOrganizationConfigHandler
    {
        #region Message
        #endregion

        private const string CachePrefix = CacheConstants.ORG_CONFIG;
        private const string SelectItemCacheSubfix = CacheConstants.LIST_SELECT;
        private const string CodePrefix = "UR.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;

        public OrganizationConfigHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> CreateOrUpdate(OrganizationConfigModel model, SystemLogModel systemLog)
        {
            try
            {
                Log.Information($"{systemLog.TraceId} - Create or Update {CacheConstants.ORG_CONFIG}: {JsonSerializer.Serialize(model)}");
                string msgSuccess = "";
                string msgErr = "";
                var orgConfig = await _dataContext.OrganizationConfig.Where(x => x.OrganizationId == model.OrganizationId).FirstOrDefaultAsync();
                if (orgConfig == null)
                {
                    orgConfig = AutoMapperUtils.AutoMap<OrganizationConfigModel, OrganizationConfig>(model);
                    orgConfig.CreatedDate = DateTime.Now;
                    msgSuccess = "Thêm mới cấu hình hiển thị thành công";
                    msgErr = "Thêm mới cấu hình hiển thị thất bại";
                    await _dataContext.OrganizationConfig.AddAsync(orgConfig);
                }
                else
                {
                    model.UpdateToEntity(orgConfig);

                    msgSuccess = "Cập nhật cấu hình hiển thị thành công";
                    msgErr = "Cập nhật cấu hình hiển thị thất bại";
                    _dataContext.OrganizationConfig.Update(orgConfig);
                }

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    Log.Information($"{systemLog.TraceId} - {msgSuccess}: {JsonSerializer.Serialize(orgConfig)}");
                    InvalidCache(orgConfig.OrganizationId.ToString());

                    return new ResponseObject<Guid>(orgConfig.Id, msgSuccess, Code.Success);
                }
                else
                {
                    Log.Error($"{systemLog.TraceId} - Create or Update {CacheConstants.ORG_CONFIG} error: Save database error!");
                    return new ResponseError(Code.ServerError, msgErr);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> GetById(Guid organizationId)
        {
            try
            {
                //string cacheKey = BuildCacheKey(organizationId.ToString());
                //var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var entity = await _dataContext.OrganizationConfig
                //        .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

                //    return AutoMapperUtils.AutoMap<OrganizationConfig, OrganizationConfigModel>(entity);
                //});

                var rs = await GetOrgConfigFromCache(organizationId);

                return new ResponseObject<OrganizationConfigModel>(rs, MessageConstants.GetDataSuccessMessage, Code.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"{MessageConstants.GetDataErrorMessage} - {ex.Message}");
            }
        }

        public async Task<OrganizationConfig> InternalGetByOrgId(Guid organizationId)
        {
            try
            {
                string cacheKey = BuildCacheKey(organizationId.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.OrganizationConfig
                        .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

                    return entity;
                });
                return rs;
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        public async Task<OrganizationConfigModel> GetByOrgId(Guid organizationId)
        {
            try
            {
                //string cacheKey = BuildCacheKey(organizationId.ToString());
                //var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                //{
                //    var entity = await _dataContext.OrganizationConfig
                //        .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

                //    return AutoMapperUtils.AutoMap<OrganizationConfig, OrganizationConfigModel>(entity);
                //});
                return await GetOrgConfigFromCache(organizationId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return null;
            }
        }

        private async Task<OrganizationConfigModel> GetOrgConfigFromCache(Guid organizationId)
        {
            try
            {
                string cacheKey = BuildCacheKey(organizationId.ToString());
                var rs = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var entity = await _dataContext.OrganizationConfig
                        .FirstOrDefaultAsync(x => x.OrganizationId == organizationId);

                    return AutoMapperUtils.AutoMap<OrganizationConfig, OrganizationConfigModel>(entity);
                });

                return rs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<Response> GetListCombobox(int count = 0, string consumerKey = "", Guid? orgId = null)
        {
            try
            {
                string cacheKey = BuildCacheKey(SelectItemCacheSubfix);
                var list = await _cacheService.GetOrCreate(cacheKey, async () =>
                {
                    var data = (from item in _dataContext.OrganizationConfig select item);
                    var dt = await data.ToListAsync();

                    List<OrganizationConfigModel> list = new List<OrganizationConfigModel>();
                    foreach (var item in dt)
                    {
                        list.Add(AutoMapperUtils.AutoMap<OrganizationConfig, OrganizationConfigModel>(item));
                    }

                    return list;
                });

                if (orgId.HasValue)
                {
                    list = list.Where(x => x.OrganizationId == orgId).ToList();
                }

                if (!string.IsNullOrEmpty(consumerKey))
                {
                    list = list.Where(x => x.ConsumerKey != null && x.ConsumerKey.Equals(consumerKey)).ToList();
                }

                if (count > 0)
                {
                    list = list.Take(count).ToList();
                }

                return new ResponseObject<List<OrganizationConfigModel>>(list, MessageConstants.GetDataSuccessMessage, Code.Success);
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