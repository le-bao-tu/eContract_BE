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
    public class SeedDataHandler : ISeedDataHandler
    {
        #region Message
        //private static string MessageInitDataSuccess = "Khởi tạo dữ liệu thành công";
        //private static string MessageInitDataError = "Khởi tạo dữ liệu thất bại";
        private static string MessageDataIsExits = "Dữ liệu đã tồn tại";
        #endregion

        private const string CachePrefixApp = "SystemApplication";
        private const string SelectItemCacheSubfix = "list-select";
        //private const string CodePrefix = "APP.";
        private readonly DataContext _dataContext;
        private readonly ICacheService _cacheService;


        public SeedDataHandler(DataContext dataContext, ICacheService cacheService)
        {
            _dataContext = dataContext;
            _cacheService = cacheService;
        }

        public async Task<Response> InitDataSysApplication(SystemLogModel systemLog)
        {
            try
            {
                var check = await _dataContext.SystemApplication.AnyAsync(x => x.Id == AppConstants.RootAppId);

                if (check)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Dữ liệu đã tồn tại",
                        ObjectCode = "Application",
                        MetaData = "",
                        ObjectId = ""
                    });
                    return new ResponseObject<bool>(true, MessageDataIsExits, Code.Created);
                }

                var dt = new SystemApplication()
                {
                    Id = AppConstants.RootAppId,
                    Code = "default-app",
                    Name = "Ứng dụng mặc định",
                    Description = "",
                    Status = true,
                    CreatedDate = DateTime.Now,
                    CreatedUserId = UserConstants.AdministratorId,
                    ModifiedDate = null,
                    ModifiedUserId = null,
                    Order = 0
                };

                await _dataContext.SystemApplication.AddAsync(dt);

                int dbSave = await _dataContext.SaveChangesAsync();

                if (dbSave > 0)
                {
                    systemLog.ListAction.Add(new ActionDetail()
                    {
                        Description = "Khởi tạo dữ liệu ứng dụng thành công",
                        ObjectCode = "Application",
                        MetaData = JsonSerializer.Serialize(dt),
                        ObjectId = dt.Id.ToString()
                    });

                    Log.Information("Khởi tạo dữ liệu ứng dụng thành công");
                    InvalidCache(CachePrefixApp);
                    return new ResponseObject<bool>(true, MessageConstants.CreateSuccessMessage, Code.Success);
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

        private void InvalidCache(string prefix, string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                string cacheKey = BuildCacheKey(prefix, id);
                _cacheService.Remove(cacheKey);
            }

            string selectItemCacheKey = BuildCacheKey(prefix, SelectItemCacheSubfix);
            _cacheService.Remove(selectItemCacheKey);
        }

        private string BuildCacheKey(string prefix, string id)
        {
            return $"{prefix}-{id}";
        }
    }
}