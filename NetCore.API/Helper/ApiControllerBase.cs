using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Authentication;
using NetCore.Shared;
using Serilog;
using NetCore.Business;
using NetCore.DataLog;
using System.Text.Json;

namespace NetCore.API
{
    public class ApiControllerBase : ControllerBase
    {
        private readonly ISystemLogHandler _logHandler;

        public ApiControllerBase(ISystemLogHandler logHandler)
        {
            _logHandler = logHandler;
        }

        protected async Task<IActionResult> ExecuteFunction<T>(Func<RequestUser, Task<T>> func)
        {
            try
            {
                var currentUser = await Helper.GetRequestInfo(HttpContext.Request);
                var result = await func(currentUser);

                //currentUser.SystemLog.ListAction.Add(new ActionDetail()
                //{
                //    CreatedDate = DateTime.Now,
                //    Description = $"Gọi api {HttpContext.Request.Path} thành công",
                //    MetaData = JsonSerializer.Serialize(result)
                //});

                //Bổ sung thông tin log
                await _logHandler.Create(currentUser.SystemLog).ConfigureAwait(false);

                if (result is Response)
                {
                    var rs = result as Response;
                    rs.TraceId = currentUser.SystemLog.TraceId;
                    return Helper.TransformData(rs);
                }
                if (result is IActionResult)
                {
                    return (IActionResult)result;
                }
                return Helper.TransformData(new ResponseObject<T>(result) { TraceId = currentUser.SystemLog.TraceId });
            }
            catch (ArgumentException agrEx)
            {
                Log.Information(agrEx, string.Empty);
                return Helper.TransformData(new ResponseError(Code.BadRequest, agrEx.Message));
            }
            catch (NullReferenceException nullEx)
            {
                Log.Error(nullEx, string.Empty);
                return Helper.TransformData(new ResponseError(Code.NotFound, nullEx.Message));
            }
            catch (AuthenticationException authEx)
            {
                Log.Error(authEx, string.Empty);
                return Helper.TransformData(new ResponseError(Code.Unauthorized, authEx.Message));
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Empty);
                return Helper.TransformData(new ResponseError(Code.BadRequest, "An error was occur, read this message for more details: " + ex.Message));
            }
        }

        protected async Task<IActionResult> ExecuteFunction<T>(Func<RequestUser, T> func)
        {
            try
            {
                var currentUser = await Helper.GetRequestInfo(HttpContext.Request);
                var result = func(currentUser);

                //currentUser.SystemLog.ListAction.Add(new ActionDetail()
                //{
                //    CreatedDate = DateTime.Now,
                //    Description = $"Gọi api {HttpContext.Request.Path} thành công",
                //    MetaData = JsonSerializer.Serialize(result)
                //});

                //Bổ sung thông tin log
                await _logHandler.Create(currentUser.SystemLog).ConfigureAwait(false);

                if (result is Response)
                {
                    return Helper.TransformData(result as Response);
                }
                if (result is IActionResult)
                {
                    return (IActionResult)result;
                }
                return Helper.TransformData(new ResponseObject<T>(result));
            }
            catch (ArgumentException agrEx)
            {
                Log.Information(agrEx, string.Empty);
                return Helper.TransformData(new ResponseError(Code.BadRequest, agrEx.Message));
            }
            catch (NullReferenceException nullEx)
            {
                Log.Information(nullEx, string.Empty);
                return Helper.TransformData(new ResponseError(Code.NotFound, nullEx.Message));
            }
            catch (AuthenticationException authEx)
            {
                Log.Warning(authEx, string.Empty);
                return Helper.TransformData(new ResponseError(Code.Unauthorized, authEx.Message));
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Empty);
                return Helper.TransformData(new ResponseError(Code.BadRequest, "An error was occur, read this message for more details: " + ex.Message));
            }
        }
    }
}
