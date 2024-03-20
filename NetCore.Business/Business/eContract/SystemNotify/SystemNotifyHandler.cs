using Microsoft.EntityFrameworkCore;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class SystemNotifyHandler : ISystemNotifyHandler
    {
        private readonly DataContext _dataContext;
        private readonly INotifyHandler _notifyHandler;
        private readonly IOrganizationHandler _organizationHandler;

        public SystemNotifyHandler(
            DataContext dataContext,
            INotifyHandler notifyHandler,
            IOrganizationHandler organizationHandler)
        {
            _dataContext = dataContext;
            _notifyHandler = notifyHandler;
            _organizationHandler = organizationHandler;
        }

        public async Task<Response> PushNotificationRemindSignDocumentDaily(SystemLogModel systemLog)
        {
            try
            {
                // Lấy danh sách người dùng nội bộ có nhận thông báo
                var lsUser = await (from item in _dataContext.User
                                .Where(x => x.Status && !x.IsDeleted && x.Type == UserType.USER && x.IsInternalUser && !x.IsLock && x.IsReceiveSystemNoti)
                                    select new UserSelectItemModel()
                                    {
                                        Id = item.Id,
                                        Code = item.UserName,
                                        FullName = item.Name,
                                        Name = item.Name,
                                        Email = item.Email,
                                        PhoneNumber = item.PhoneNumber,
                                    }).ToListAsync();

                var dateNow = DateTime.Now;

                // Lấy danh sách hợp đồng đang cần xử lý
                foreach (var item in lsUser)
                {
                    var data = (from doc in _dataContext.Document.AsNoTracking()
                                join docType in _dataContext.DocumentType.AsNoTracking() on doc.DocumentTypeId equals docType.Id into gj1
                                from docType in gj1.DefaultIfEmpty()
                                where !doc.IsDeleted
                                    && doc.NextStepUserId == item.Id && doc.Status
                                    && doc.DocumentStatus == DocumentStatus.PROCESSING
                                    && docType.Code != EFormDocumentConstant.YEU_CAU_CAP_CTS
                                    && docType.Code != EFormDocumentConstant.CHAP_THUAN_KY_DIEN_TU
                                select new Document()
                                {
                                    Id = doc.Id,
                                    SignExpireAtDate = doc.SignExpireAtDate,
                                    SignCloseAtDate = doc.SignCloseAtDate
                                });

                    // Hợp đồng đang chờ xử lý
                    var waitSignMe = await data.Where(x =>
                        !(x.SignExpireAtDate.HasValue && x.SignExpireAtDate.Value < dateNow)
                        && !(x.SignCloseAtDate.HasValue && x.SignCloseAtDate.Value < dateNow))
                        .CountAsync();

                    // Hợp đồng đã hết hạn xử lý
                    var signExpire = await data.Where(x =>
                        x.SignExpireAtDate.HasValue && x.SignExpireAtDate.Value < dateNow
                        && !(x.SignCloseAtDate.HasValue && x.SignCloseAtDate.Value < dateNow))
                        .CountAsync();

                    if (waitSignMe == 0 && signExpire == 0)
                    {
                        continue;
                    }

                    // Lấy đơn vị gốc
                    var orgCode = "";
                    if (item.OrganizationId.HasValue)
                    {
                        var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(item.OrganizationId.Value);
                        orgCode = rootOrg?.Code;
                    }

                    // Gửi thông báo qua gateway
                    NotificationRemindSignDocumentDailyModel notiModel = new NotificationRemindSignDocumentDailyModel()
                    {
                        TraceId = systemLog.TraceId,
                        ListEmail = new List<string>() { item.Email },
                        ListPhoneNumber = new List<string>() { item.PhoneNumber },
                        NumberOfDocumentExpired = signExpire,
                        NumberOfDocumentWaitMeSign = waitSignMe,
                        OraganizationCode = orgCode,
                        UserFullName = item.FullName
                    };

                    await _notifyHandler.PushNotificationRemindSignDoucmentDaily(notiModel);
                }
                return new Response(Code.Success, "Gửi thông báo thành công");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }

        public async Task<Response> PushNotificationSignFail(NotificationAutoSignFailModel model, SystemLogModel systemLog)
        {
            try
            {
                // Lấy danh sách đơn vị
                var lsOrg = _organizationHandler.GetListChildOrgByParentID(model.OraganizationRootId);

                // Lấy danh sách người dùng nội bộ có nhận thông báo
                var lsUser = await (from item in _dataContext.User
                                .Where(x => x.Status && !x.IsDeleted && x.Type == UserType.USER && x.IsInternalUser && !x.IsLock && x.IsReceiveSignFailNoti && x.OrganizationId.HasValue && lsOrg.Contains(x.OrganizationId.Value))
                                    select new UserSelectItemModel()
                                    {
                                        Id = item.Id,
                                        Name = item.Name,
                                        Email = item.Email,
                                        PhoneNumber = item.PhoneNumber,
                                    }).ToListAsync();

                var dateNow = DateTime.Now;

                var listEmail = lsUser.Select(x => x.Email).ToList();

                // Gửi thông báo qua gateway
                NotificationAutoSignFailModel notiModel = new NotificationAutoSignFailModel()
                {
                    TraceId = systemLog.TraceId,
                    ListEmail = lsUser.Select(x => x.Email).ToList(),
                    ListPhoneNumber = lsUser.Select(x => x.PhoneNumber).ToList(),
                    Document3rdId = model.Document3rdId,
                    DocumentCode = model.DocumentCode,
                    DocumentName = model.DocumentName,
                    ListToken = new List<string>(),
                    OraganizationCode = model.OraganizationCode
                };

                await _notifyHandler.PushNotificationAutoSignFail(notiModel);
                return new Response(Code.Success, "Gửi thông báo thành công");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - " + MessageConstants.ErrorLogMessage);
                return new ResponseError(Code.ServerError, $"Có lỗi xảy ra - {ex.Message}");
            }
        }
    }
}
