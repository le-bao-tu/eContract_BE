using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NetCore.Business;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.WorkerService
{
    public class NotifyConfig : INotifyConfig
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEmailHandler _emailHandler;
        private readonly DataContext _dataContext;
        private readonly IMongoCollection<NotifyLog> _notifyLog;
        private readonly INotifyHandler _notifyHandler;
        private readonly IOrganizationHandler _organizationHandler;

        public NotifyConfig(
            IMongoDBDatabaseSettings settings,
            DataContext dataContext,
            ILogger<Worker> logger,
            IEmailHandler emailHandler,
            INotifyHandler notifyHandler,
            IOrganizationHandler organizationHandler
            )
        {
            _logger = logger;
            _emailHandler = emailHandler;
            _dataContext = dataContext;
            _notifyHandler = notifyHandler;
            _organizationHandler = organizationHandler;

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _notifyLog = database.GetCollection<NotifyLog>(MongoCollections.NotifyLog);
        }

        public async Task SendNotifyRemind()
        {
            try
            {
                Log.Information("Start send notify remind.");
                DateTime now = DateTime.Now;

                // Danh sách thông báo cần gửi
                var notifications = await (from noti in _dataContext.DocumentNotifySchedule
                                           join user in _dataContext.User on noti.UserId equals user.Id
                                           join notiConfig in _dataContext.NotifyConfig on noti.NotifyConfigRemindId equals notiConfig.Id
                                           where !noti.SendedRemindAtDate.HasValue || (noti.SendedRemindAtDate.HasValue && noti.SendedRemindAtDate.Value.AddDays(1) == now.Date) && noti.NotifyConfigRemindId.HasValue
                                           select new NotifyConfigModel()
                                           {
                                               Id = noti.Id,
                                               DocumentId = noti.DocumentId,
                                               DocumentCode = noti.DocumentCode,
                                               DocumentName = noti.DocumentName,
                                               UserId = noti.UserId,
                                               UserName = noti.UserName,
                                               Email = user.Email,
                                               FullName = user.Name,
                                               NotifyConfigRemindId = noti.NotifyConfigRemindId,
                                               SignExpireAtDate = noti.SignExpireAtDate,
                                               SendedRemindAtDate = noti.SendedRemindAtDate,
                                               IsRepeat = notiConfig.IsRepeat,
                                               DaySendNotiBefore = notiConfig.DaySendNotiBefore,
                                               TimeSendNotify = notiConfig.TimeSendNotify,
                                               EmailTitleTemplate = notiConfig.EmailTitleTemplate,
                                               EmailBodyTemplate = notiConfig.EmailBodyTemplate,
                                               IsSendSMS = notiConfig.IsSendSMS,
                                               PhoneNumber = user.PhoneNumber,
                                               SMSTemplate = notiConfig.SMSTemplate,
                                               IsSendEmail = notiConfig.IsSendEmail,
                                               IsSendNotify = notiConfig.IsSendNotification,
                                               NotificationTitleTemplate = notiConfig.NotificationTitleTemplate,
                                               NotificationBodyTemplate = notiConfig.NotificationBodyTemplate,
                                               OrganizationId = noti.OrganizationId,
                                               NotifyType = notiConfig.NotifyType
                                           }).Take(100).ToListAsync();           

                foreach (var item in notifications)
                {
                    Log.Information("Notify remind object: " + JsonSerializer.Serialize(item));

                    // lấy phòng ban gốc
                    var orgCode = "";
                    if (item.OrganizationId.HasValue)
                    {
                        var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(item.OrganizationId.Value);
                        orgCode = rootOrg?.Code;
                    }

                    DateTime? dateSendRemind = null;

                    if (item.DaySendNotiBefore.HasValue) dateSendRemind = item.SignExpireAtDate.AddDays(-item.DaySendNotiBefore.Value);

                    /*
                        * gửi thông báo trước DaySendNotiBefore theo thời gian hết hạn hợp đồng, theo thời gian gửi
                        * và gửi khi SendedRemindAtDate = null(chưa gửi lần nào) và SendedRemindAtDate + 1 ngày tức là sang ngày tiếp theo gửi
                    */
                    DateTime? timeSend = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(item.TimeSendNotify))
                            timeSend = DateTime.ParseExact(item.TimeSendNotify, "HH:mm", CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        timeSend = null;
                    }

                    // thời gian gửi nằm trong khoảng 30 phút trước hiện tại và 30 phút sau hiện tại hoặc thời gian gửi null => sẽ gửi
                    if (dateSendRemind.HasValue
                        && dateSendRemind.Value.Date <= now.Date && now.Date <= item.SignExpireAtDate.Date
                        && (!timeSend.HasValue
                            || (timeSend.HasValue && timeSend >= DateTime.Now.AddMinutes(-30) && timeSend <= DateTime.Now.AddHours(1))))
                    {
                        object param = new
                        {
                            userFullName = item.FullName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            remindTime = item.SendedRemindAtDate.HasValue ? item.SendedRemindAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            remindDate = item.SendedRemindAtDate.HasValue ? item.SendedRemindAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        string[] contentsEmail = null;
                        if (item.IsSendEmail.HasValue && item.IsSendEmail.Value)
                            contentsEmail = Utils.ReplaceContentNotify(param, item.EmailTitleTemplate, item.EmailBodyTemplate);

                        string[] contentsSMS = null;
                        if (item.IsSendSMS.HasValue && item.IsSendSMS.Value)
                            contentsSMS = Utils.ReplaceContentNotify(param, item.SMSTemplate);

                        string[] contentsNotify = null;
                        if (item.IsSendNotify.HasValue && item.IsSendNotify.Value)
                            contentsNotify = Utils.ReplaceContentNotify(param, item.NotificationTitleTemplate, item.NotificationBodyTemplate);

                        var userToken = await _dataContext.UserMapFirebaseToken.Where(x => x.UserId == item.UserId).Select(x => x.FirebaseToken).ToListAsync();

                        var notiConfigModel = new NotificationConfigModel()
                        {
                            OraganizationCode = orgCode,
                            IsSendSMS = item.IsSendSMS,
                            ListPhoneNumber = new List<string>() { item.PhoneNumber },
                            SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                            IsSendEmail = item.IsSendEmail,
                            ListEmail = new List<string>() { item.Email },
                            EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                            EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                            IsSendNotification = item.IsSendNotify,
                            ListToken = userToken,
                            NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                            NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                            Data = new Dictionary<string, string>()
                            {
                                { "NotifyType", item.NotifyType.ToString() }
                            }
                        };
                        Log.Information("Notification config model: " + JsonSerializer.Serialize(notiConfigModel));
                        await _notifyHandler.SendNotificationFromNotifyConfig(notiConfigModel);

                        //var notifyLog = new NotifyLog
                        //{
                        //    DocumentId = item.DocumentId.ToString(),
                        //    DocumentCode = item.DocumentCode.ToString(),
                        //    UserId = item.UserId.ToString(),
                        //    UserName = item.UserName,
                        //    Content = contents[0] + contents[1],
                        //    NotifyType = NotifyType.EMAIL,
                        //    NotifyConfigType = NotifyConfigType.REMIND,
                        //    CreatedDate = now
                        //};

                        //_notifyLog.InsertOneAsync(notifyLog);

                        // đánh dấu những bản ghi đã gửi được thông báo
                        //item.IsSend = true;

                        var notiUpdate = await _dataContext.DocumentNotifySchedule.FirstOrDefaultAsync(x => x.Id == item.Id);
                        notiUpdate.IsSend = true;
                        notiUpdate.SendedRemindAtDate = DateTime.Now;
                        await _dataContext.SaveChangesAsync();
                    }
                }

                //Log.Information("Start update bản ghi đã được gửi thông báo remind");

                //// update những bản ghi đã gửi được thông báo ngày remind = ngày hiện tại
                //var notificationIds = notifications.Where(x1 => x1.IsSend).Select(x2 => x2.Id).ToList();
                //Log.Information("Update bản ghi đã gửi thông báo remind: " + JsonSerializer.Serialize(notificationIds));

                //if (notificationIds != null)
                //{
                //    var documentNotifyScheduleUpdate = _dataContext.DocumentNotifySchedule.Where(x => notificationIds.Contains(x.Id));
                //    if (await documentNotifyScheduleUpdate.AnyAsync())
                //    {
                //        foreach (var item in documentNotifyScheduleUpdate)
                //        {
                //            item.IsSend = true;
                //            item.SendedRemindAtDate = DateTime.Now;
                //        }

                //        _dataContext.DocumentNotifySchedule.UpdateRange(documentNotifyScheduleUpdate);
                //        await _dataContext.SaveChangesAsync();
                //    }
                //}
                //Log.Information("End update bản ghi đã được gửi thông báo remind");

                Log.Information("End send notify remind.");
            }
            catch (Exception ex)
            {
                Log.Error("Send notify remind error: " + ex.Message);
                Console.WriteLine("Error: ", ex);
            }
        }

        public async Task SendNotifyExpire()
        {
            try
            {
                Log.Information("Start send notify expire.");
                DateTime now = DateTime.Now;

                // Danh sách thông báo cần gửi
                var notifications = await (from noti in _dataContext.DocumentNotifySchedule
                                           join user in _dataContext.User on noti.UserId equals user.Id
                                           join wfStep in _dataContext.WorkflowUserSign on noti.WorkflowStepId equals wfStep.Id into gj
                                           from wfStep in gj.DefaultIfEmpty()

                                           join document in _dataContext.Document on noti.DocumentId equals document.Id
                                           join notiConfig in _dataContext.NotifyConfig on noti.NotifyConfigExpireId equals notiConfig.Id
                                           where !noti.IsSend && noti.NotifyConfigExpireId.HasValue && noti.SignExpireAtDate <= now
                                           select new NotifyConfigModel()
                                           {
                                               Id = noti.Id,
                                               DocumentId = noti.DocumentId,
                                               DocumentCode = noti.DocumentCode,
                                               DocumentName = noti.DocumentName,
                                               UserId = noti.UserId,
                                               UserName = noti.UserName,
                                               Email = user.Email,
                                               FullName = user.Name,
                                               WorkFlowUserJson = document.WorkFlowUserJson,
                                               UserReceiveNotiExpireJson = wfStep.UserReceiveNotiExpireJson,
                                               NotifyConfigRemindId = noti.NotifyConfigRemindId,
                                               NotifyConfigExpireId = noti.NotifyConfigExpireId,
                                               WorkflowStepId = noti.WorkflowStepId,
                                               SignExpireAtDate = noti.SignExpireAtDate,
                                               SendedRemindAtDate = noti.SendedRemindAtDate,
                                               IsRepeat = notiConfig.IsRepeat,
                                               EmailTitleTemplate = notiConfig.EmailTitleTemplate,
                                               EmailBodyTemplate = notiConfig.EmailBodyTemplate,
                                               IsSendSMS = notiConfig.IsSendSMS,
                                               PhoneNumber = user.PhoneNumber,
                                               SMSTemplate = notiConfig.SMSTemplate,
                                               IsSendEmail = notiConfig.IsSendEmail,
                                               IsSendNotify = notiConfig.IsSendNotification,
                                               NotificationTitleTemplate = notiConfig.NotificationTitleTemplate,
                                               NotificationBodyTemplate = notiConfig.NotificationBodyTemplate,
                                               OrganizationId = noti.OrganizationId,
                                               NotifyType = notiConfig.NotifyType
                                           }).Take(100).ToListAsync();              

                // Lấy danh sách user/firebase token để tránh phải chọc vào database nhiều lần
                List<Guid> listUserId = new List<Guid>();
                foreach (var item in notifications)
                {
                    listUserId.Add(item.UserId);
                    if (item.UserReceiveNotiExpire != null && item.UserReceiveNotiExpire.Count > 0)
                    {
                        foreach (var u in item.UserReceiveNotiExpire)
                        {
                            if (u >= item.WorkFlowUser.Count)
                            {
                                continue;
                            }

                            if (item.WorkFlowUser[u].UserId.HasValue)
                            {
                                listUserId.Add(item.WorkFlowUser[u].UserId.Value);
                            }
                        }
                    }
                }

                var listUser = await _dataContext.User.Where(x => listUserId.Contains(x.Id)).ToListAsync();
                var listUserFirebaseToken = await _dataContext.UserMapFirebaseToken.Where(x => listUserId.Contains(x.UserId)).ToListAsync();

                foreach (var item in notifications)
                {
                    Log.Information("Notify expire object: " + JsonSerializer.Serialize(item));

                    // lấy phòng ban gốc
                    var orgCode = "";
                    if (item.OrganizationId.HasValue)
                    {
                        var rootOrg = await _organizationHandler.GetRootOrgModelByChidId(item.OrganizationId.Value);
                        orgCode = rootOrg?.Code;
                    }

                    // Nếu UserReceiveNotiExpire có dữ liệu thì kiểm tra xem cần gửi cho ai trong quy trình
                    if (item.UserReceiveNotiExpire != null && item.UserReceiveNotiExpire.Count > 0)
                    {
                        foreach (var u in item.UserReceiveNotiExpire)
                        {
                            // Kiểm tra xem vị trí của user có nằm trong phạm vi quy trình không?
                            if (u >= item.WorkFlowUser.Count)
                            {
                                continue;
                            }

                            var userInfo = listUser.Where(x => x.Id == item.WorkFlowUser[u].Id).FirstOrDefault();
                            if (userInfo == null)
                            {
                                continue;
                            }
                            object param = new
                            {
                                userFullName = userInfo.Name,
                                documentCode = item.DocumentCode,
                                documentName = item.DocumentName,
                                expireTime = item.SendedRemindAtDate.HasValue ? item.SendedRemindAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                                expireDate = item.SendedRemindAtDate.HasValue ? item.SendedRemindAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                            };

                            string[] contentsEmail = null;
                            if (item.IsSendEmail.HasValue && item.IsSendEmail.Value)
                                contentsEmail = Utils.ReplaceContentNotify(param, item.EmailTitleTemplate, item.EmailBodyTemplate);

                            string[] contentsSMS = null;
                            if (item.IsSendSMS.HasValue && item.IsSendSMS.Value)
                                contentsSMS = Utils.ReplaceContentNotify(param, item.SMSTemplate);

                            string[] contentsNotify = null;
                            if (item.IsSendNotify.HasValue && item.IsSendNotify.Value)
                                contentsNotify = Utils.ReplaceContentNotify(param, item.NotificationTitleTemplate, item.NotificationBodyTemplate);

                            var userToken = listUserFirebaseToken.Where(x => x.UserId == userInfo.Id).Select(x => x.FirebaseToken).ToList();

                            var notiConfigModel = new NotificationConfigModel()
                            {
                                OraganizationCode = orgCode,
                                IsSendSMS = item.IsSendSMS,
                                ListPhoneNumber = new List<string>() { userInfo.PhoneNumber },
                                SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                                IsSendEmail = item.IsSendEmail,
                                ListEmail = new List<string>() { userInfo.Email },
                                EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                                EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                                IsSendNotification = item.IsSendNotify,
                                ListToken = userToken,
                                NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                                NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                                Data = new Dictionary<string, string>()
                                {
                                    { "NotifyType", item.NotifyType.ToString() }
                                }
                            };
                            Log.Information("Notification config model: " + JsonSerializer.Serialize(notiConfigModel));
                            await _notifyHandler.SendNotificationFromNotifyConfig(notiConfigModel);

                            //var notifyLog = new NotifyLog
                            //{
                            //    DocumentId = item.DocumentId.ToString(),
                            //    DocumentCode = item.DocumentCode.ToString(),
                            //    UserId = item.UserId.ToString(),
                            //    UserName = item.UserName,
                            //    Content = contents[0] + contents[1],
                            //    NotifyType = NotifyType.EMAIL,
                            //    NotifyConfigType = NotifyConfigType.EXPIRE,
                            //    CreatedDate = now
                            //};

                            //_notifyLog.InsertOneAsync(notifyLog);
                        }
                        // đánh dấu những bản ghi đã gửi thông báo

                        var notiSchedule = await _dataContext.DocumentNotifySchedule.FirstOrDefaultAsync(x => x.Id == item.Id);
                        notiSchedule.IsSend = true;
                        await _dataContext.SaveChangesAsync();
                    }
                    // Nếu UserReceiveNotiExpire trống => gửi cho người dùng hiện tại
                    else
                    {
                        object param = new
                        {
                            userFullName = item.FullName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SendedRemindAtDate.HasValue ? item.SendedRemindAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SendedRemindAtDate.HasValue ? item.SendedRemindAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        string[] contentsEmail = null;
                        if (item.IsSendEmail.HasValue && item.IsSendEmail.Value)
                            contentsEmail = Utils.ReplaceContentNotify(param, item.EmailTitleTemplate, item.EmailBodyTemplate);

                        string[] contentsSMS = null;
                        if (item.IsSendSMS.HasValue && item.IsSendSMS.Value)
                            contentsSMS = Utils.ReplaceContentNotify(param, item.SMSTemplate);

                        string[] contentsNotify = null;
                        if (item.IsSendNotify.HasValue && item.IsSendNotify.Value)
                            contentsNotify = Utils.ReplaceContentNotify(param, item.NotificationTitleTemplate, item.NotificationBodyTemplate);

                        var userToken = listUserFirebaseToken.Where(x => x.UserId == item.UserId).Select(x => x.FirebaseToken).ToList();

                        var notiConfigModel = new NotificationConfigModel()
                        {
                            OraganizationCode = orgCode,
                            IsSendSMS = item.IsSendSMS,
                            ListPhoneNumber = new List<string>() { item.PhoneNumber },
                            SmsContent = contentsSMS != null && contentsSMS.Length > 0 ? contentsSMS[0] : "",
                            IsSendEmail = item.IsSendEmail,
                            ListEmail = new List<string>() { item.Email },
                            EmailTitle = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[0] : "",
                            EmailContent = contentsEmail != null && contentsEmail.Length > 0 ? contentsEmail[1] : "",
                            IsSendNotification = item.IsSendNotify,
                            ListToken = userToken,
                            NotificationTitle = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[0] : "",
                            NotificationContent = contentsNotify != null && contentsNotify.Length > 0 ? contentsNotify[1] : "",
                            Data = new Dictionary<string, string>()
                        {
                            { "NotifyType", item.NotifyType.ToString() }
                        }
                        };
                        Log.Information("Notification config model: " + JsonSerializer.Serialize(notiConfigModel));
                        await _notifyHandler.SendNotificationFromNotifyConfig(notiConfigModel);

                        //var notifyLog = new NotifyLog
                        //{
                        //    DocumentId = item.DocumentId.ToString(),
                        //    DocumentCode = item.DocumentCode.ToString(),
                        //    UserId = item.UserId.ToString(),
                        //    UserName = item.UserName,
                        //    Content = contents[0] + contents[1],
                        //    NotifyType = NotifyType.EMAIL,
                        //    NotifyConfigType = NotifyConfigType.EXPIRE,
                        //    CreatedDate = now
                        //};

                        //_notifyLog.InsertOneAsync(notifyLog);

                        // đánh dấu những bản ghi đã gửi thông báo

                        var notiSchedule = await _dataContext.DocumentNotifySchedule.FirstOrDefaultAsync(x => x.Id == item.Id);
                        notiSchedule.IsSend = true;
                        await _dataContext.SaveChangesAsync();
                    }
                }

                //Log.Information("Start update bản ghi đã được gửi thông báo expire");

                //// update các bản ghi thành đã gửi
                //var documentIds = notifications.Where(x => x.IsSend).Select(x1 => x1.Id).ToList();
                //Log.Information("Update bản ghi đã gửi thông báo expire: " + JsonSerializer.Serialize(documentIds));

                //if (documentIds != null)
                //{
                //    var docNotifySchedules = _dataContext.DocumentNotifySchedule.Where(x => documentIds.Contains(x.Id));
                //    if (await docNotifySchedules.AnyAsync())
                //    {
                //        foreach (var item in docNotifySchedules)
                //        {
                //            item.IsSend = true;
                //        }

                //        _dataContext.DocumentNotifySchedule.UpdateRange(docNotifySchedules);
                //        await _dataContext.SaveChangesAsync();
                //    }
                //}
                //Log.Information("End update bản ghi đã được gửi thông báo expire");

                Log.Information("End send notify expire.");
            }
            catch (Exception ex)
            {
                Log.Error("Send notify expire error: " + ex.Message);
                Console.WriteLine("Error: ", ex);
            }
        }

        public async Task SendEmailExpiredAsync()
        {
            try
            {
                DateTime dateNow = DateTime.Now;
                //var listWFLUserSign = _dataContext.WorkflowUserSign.ToList();

                //Danh sách user ký trên những hợp đồng đã hết hạn ký
                var listUserSignOnDoc = from d in _dataContext.Document.AsNoTracking()
                                        where !d.IsDeleted && d.SignExpireAtDate.HasValue && d.SignExpireAtDate.Value < dateNow
                                        join u in _dataContext.WorkflowUserSign.AsNoTracking() on d.NextStepUserId equals u.UserId
                                        select new
                                        {
                                            DocumentId = d.Id,
                                            DocumentCode = d.Code,
                                            DocumentName = d.Name,
                                            d.NextStepUserId,
                                            d.NextStepUserName,
                                            d.NextStepUserEmail,
                                            d.SignExpireAtDate,
                                            u.NotifyConfigExpireId
                                        };

                //Danh sách user có xác nhận gửi mail
                var listUserSendEmailExpired = (from us in listUserSignOnDoc
                                                join w in _dataContext.NotifyConfig.AsNoTracking() on us.NotifyConfigExpireId equals w.Id
                                                where us.NotifyConfigExpireId.HasValue && w.Status && w.IsSendEmail
                                                select new
                                                {
                                                    us.DocumentId,
                                                    us.DocumentCode,
                                                    us.DocumentName,
                                                    us.NextStepUserId,
                                                    us.NextStepUserName,
                                                    us.NextStepUserEmail,
                                                    us.SignExpireAtDate,
                                                    w.TimeSendNotify,
                                                    w.EmailTitleTemplate,
                                                    w.EmailBodyTemplate,
                                                }).ToList();

                //Lấy danh sách log email gửi trong ngày
                DateTime beginOfDay = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0);
                DateTime endOfDay = beginOfDay.AddDays(1);
                var builder = Builders<NotifyLog>.Filter.And(
                    Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.EMAIL) && p.NotifyConfigType.Equals(NotifyConfigType.EXPIRE) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                IFindFluent<NotifyLog, NotifyLog> data = _notifyLog.Find(builder);
                var listEmailExpireSendedInDay = await data.ToListAsync();

                //Lấy danh sách email gửi ngày trước đó
                DateTime yesterday = dateNow.AddDays(-1);
                beginOfDay = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
                endOfDay = beginOfDay.AddDays(1);

                builder = Builders<NotifyLog>.Filter.And(
                  Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.EMAIL) && p.NotifyConfigType.Equals(NotifyConfigType.EXPIRE) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                data = _notifyLog.Find(builder);
                var listEmailExpireSendedYesterday = await data.ToListAsync();

                List<string> toEmails;
                bool isSendedInDay, isSendedYesterday = false;
                string timeNow, docId, userId;
                object param;
                string[] contents;
                NotifyLog notify;
                foreach (var item in listUserSendEmailExpired)
                {
                    docId = item.DocumentId.ToString();
                    userId = item.NextStepUserId.ToString();
                    timeNow = dateNow.ToString("HH:mm");
                    isSendedInDay = listEmailExpireSendedInDay.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    isSendedYesterday = listEmailExpireSendedYesterday.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    if (timeNow.Equals(item.TimeSendNotify) && !isSendedInDay && !isSendedYesterday)
                    {
                        toEmails = new List<string>();
                        toEmails.Add(item.NextStepUserEmail);

                        #region Replace nội dung
                        param = new
                        {
                            userFullName = item.NextStepUserName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        contents = Utils.ReplaceContentNotify(param, item.EmailTitleTemplate, item.EmailBodyTemplate);
                        #endregion

                        #region Ghi log lịch sử gửi notify
                        notify = new NotifyLog
                        {
                            DocumentId = item.DocumentId.ToString(),
                            DocumentCode = item.DocumentCode.ToString(),
                            UserId = item.NextStepUserId.HasValue ? item.NextStepUserId.Value.ToString() : string.Empty,
                            UserName = item.NextStepUserName,
                            Content = contents[0] + contents[1],
                            NotifyType = NotifyType.EMAIL,
                            NotifyConfigType = NotifyConfigType.EXPIRE,
                            CreatedDate = dateNow
                        };

                        await _notifyLog.InsertOneAsync(notify).ConfigureAwait(false);
                        #endregion

                        #region Gửi Email
                        _emailHandler.SendMailGoogle(toEmails, null, null, item.EmailTitleTemplate, item.EmailBodyTemplate);
                        #endregion
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
            }
        }

        public async Task SendEmailRemindAsync()
        {
            try
            {
                DateTime dateNow = DateTime.Now;

                //Danh sách user ký trên những hợp đồng trong thời hạn ký
                var listUserSignOnDoc = (from d in _dataContext.Document.AsNoTracking()
                                         where !d.IsDeleted && d.SignExpireAtDate.HasValue && d.SignExpireAtDate.Value >= dateNow
                                         join u in _dataContext.WorkflowUserSign.AsNoTracking() on d.NextStepUserId equals u.UserId
                                         select new
                                         {
                                             DocumentId = d.Id,
                                             DocumentCode = d.Code,
                                             DocumentName = d.Name,
                                             d.SignExpireAtDate,
                                             d.NextStepUserId,
                                             d.NextStepUserName,
                                             d.NextStepUserEmail,
                                             u.NotifyConfigRemindId,
                                         });

                //Danh sách user có xác nhận gửi mail
                var listUserSendEmailRemind = (from us in listUserSignOnDoc
                                               join w in _dataContext.NotifyConfig.AsNoTracking() on us.NotifyConfigRemindId equals w.Id
                                               where us.NotifyConfigRemindId.HasValue && w.Status && w.IsSendEmail
                                               select new
                                               {
                                                   us.DocumentId,
                                                   us.DocumentCode,
                                                   us.DocumentName,
                                                   us.SignExpireAtDate,
                                                   us.NextStepUserId,
                                                   us.NextStepUserName,
                                                   us.NextStepUserEmail,
                                                   w.IsRepeat,
                                                   w.DaySendNotiBefore,
                                                   w.TimeSendNotify,
                                                   w.EmailTitleTemplate,
                                                   w.EmailBodyTemplate,
                                               }).ToList();

                //Lấy danh sách log email gửi trong ngày
                DateTime beginOfDay = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0);
                DateTime endOfDay = beginOfDay.AddDays(1);
                var builder = Builders<NotifyLog>.Filter.And(
                    Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.EMAIL) && p.NotifyConfigType.Equals(NotifyConfigType.REMIND) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                IFindFluent<NotifyLog, NotifyLog> data = _notifyLog.Find(builder);
                var listEmailRemindSendedInDay = await data.ToListAsync();

                //Lấy danh sách email gửi ngày trước đó
                DateTime yesterday = dateNow.AddDays(-1);
                beginOfDay = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
                endOfDay = beginOfDay.AddDays(1);
                builder = Builders<NotifyLog>.Filter.And(
                  Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.EMAIL) && p.NotifyConfigType.Equals(NotifyConfigType.REMIND) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                data = _notifyLog.Find(builder);
                var listEmailRemindSendedYesterday = await data.ToListAsync();

                DateTime? dateStartRemind = null;
                List<string> toEmails;
                bool isSendedInDay, isSendedYesterday = false;
                string timeNow, docId, userId;
                object param;
                string[] contents;
                NotifyLog notifyLog;
                foreach (var item in listUserSendEmailRemind)
                {
                    timeNow = dateNow.ToString("HH:mm");
                    if (item.DaySendNotiBefore.HasValue) dateStartRemind = item.SignExpireAtDate.Value.AddDays(-item.DaySendNotiBefore.Value);

                    docId = item.DocumentId.ToString();
                    userId = item.NextStepUserId.ToString();

                    isSendedInDay = listEmailRemindSendedInDay.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    isSendedYesterday = listEmailRemindSendedYesterday.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));

                    //Trong khoảng ngày gửi và đến thời gian sẽ gửi
                    if (dateStartRemind.HasValue && dateStartRemind <= dateNow && dateNow <= item.SignExpireAtDate && timeNow.Equals(item.TimeSendNotify) && !isSendedInDay)
                    {
                        if (!item.IsRepeat && isSendedYesterday)//Nếu không lặp và đã gửi mail một lần
                            continue;

                        toEmails = new List<string>();
                        toEmails.Add(item.NextStepUserEmail);

                        #region Replace nội dung
                        param = new
                        {
                            userFullName = item.NextStepUserName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        contents = Utils.ReplaceContentNotify(param, item.EmailTitleTemplate, item.EmailBodyTemplate);
                        #endregion

                        #region Ghi log lịch sử gửi notify
                        notifyLog = new NotifyLog
                        {
                            DocumentId = item.DocumentId.ToString(),
                            DocumentCode = item.DocumentCode.ToString(),
                            UserId = item.NextStepUserId.HasValue ? item.NextStepUserId.Value.ToString() : string.Empty,
                            UserName = item.NextStepUserName,
                            Content = contents[0] + contents[1],
                            NotifyType = NotifyType.EMAIL,
                            NotifyConfigType = NotifyConfigType.REMIND,
                            CreatedDate = dateNow
                        };

                        await _notifyLog.InsertOneAsync(notifyLog).ConfigureAwait(false);
                        #endregion

                        _emailHandler.SendMailGoogle(toEmails, null, null, contents[0], contents[1]);
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
            }
        }

        public async Task SendNotifyExpiredAsync()
        {
            try
            {
                DateTime dateNow = DateTime.Now;

                //Danh sách user ký trên những hợp đồng đã hết hạn ký
                var listUserSignOnDoc = from d in _dataContext.Document.AsNoTracking()
                                        where !d.IsDeleted && d.SignExpireAtDate.HasValue && d.SignExpireAtDate.Value < dateNow
                                        join u in _dataContext.WorkflowUserSign.AsNoTracking() on d.NextStepUserId equals u.UserId
                                        select new
                                        {
                                            DocumentId = d.Id,
                                            DocumentCode = d.Code,
                                            DocumentName = d.Name,
                                            d.NextStepUserId,
                                            d.NextStepUserName,
                                            d.NextStepUserEmail,
                                            d.SignExpireAtDate,
                                            u.NotifyConfigExpireId
                                        };

                //Danh sách user có xác nhận gửi Notify
                var listUserSendNotifyExpired = (from us in listUserSignOnDoc
                                                 join w in _dataContext.WorkflowStepExpireNotify.AsNoTracking() on us.NotifyConfigExpireId equals w.Id
                                                 where us.NotifyConfigExpireId.HasValue && w.Status && w.IsSendNotification
                                                 select new
                                                 {
                                                     us.DocumentId,
                                                     us.DocumentCode,
                                                     us.DocumentName,
                                                     us.NextStepUserId,
                                                     us.NextStepUserName,
                                                     us.SignExpireAtDate,
                                                     w.TimeSendNotify,
                                                     w.NotificationTitleTemplate,
                                                     w.NotificationBodyTemplate,
                                                 }).ToList();

                //Lấy danh sách log email gửi trong ngày
                DateTime beginOfDay = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0);
                DateTime endOfDay = beginOfDay.AddDays(1);
                var builder = Builders<NotifyLog>.Filter.And(
                    Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.NOTIFY) && p.NotifyConfigType.Equals(NotifyConfigType.EXPIRE) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                IFindFluent<NotifyLog, NotifyLog> data = _notifyLog.Find(builder);
                var listNotifyExpireSendedInDay = await data.ToListAsync();

                //Lấy danh sách email gửi ngày trước đó
                DateTime yesterday = dateNow.AddDays(-1);
                beginOfDay = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
                endOfDay = beginOfDay.AddDays(1);

                builder = Builders<NotifyLog>.Filter.And(
                  Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.NOTIFY) && p.NotifyConfigType.Equals(NotifyConfigType.EXPIRE) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                data = _notifyLog.Find(builder);
                var listNotifyExpireSendedYesterday = await data.ToListAsync();

                bool isSendedInDay, isSendedYesterday = false;
                string timeNow, docId, userId;
                object param;
                string[] contents;
                NotifyLog notify;
                foreach (var item in listUserSendNotifyExpired)
                {
                    docId = item.DocumentId.ToString();
                    userId = item.NextStepUserId.ToString();
                    timeNow = dateNow.ToString("HH:mm");
                    isSendedInDay = listNotifyExpireSendedInDay.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    isSendedYesterday = listNotifyExpireSendedYesterday.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    if (timeNow.Equals(item.TimeSendNotify) && !isSendedInDay && !isSendedYesterday)
                    {
                        #region Replace nội dung
                        param = new
                        {
                            userFullName = item.NextStepUserName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        contents = Utils.ReplaceContentNotify(param, item.NotificationTitleTemplate, item.NotificationBodyTemplate);
                        #endregion

                        #region Ghi log lịch sử gửi notify
                        notify = new NotifyLog
                        {
                            DocumentId = item.DocumentId.ToString(),
                            DocumentCode = item.DocumentCode.ToString(),
                            UserId = item.NextStepUserId.HasValue ? item.NextStepUserId.Value.ToString() : string.Empty,
                            UserName = item.NextStepUserName,
                            Content = contents[0] + contents[1],
                            NotifyType = NotifyType.NOTIFY,
                            NotifyConfigType = NotifyConfigType.EXPIRE,
                            CreatedDate = dateNow
                        };

                        await _notifyLog.InsertOneAsync(notify).ConfigureAwait(false);
                        #endregion

                        #region Gửi Notify
                        #endregion
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
            }
        }

        public async Task SendNotifyRemindAsync()
        {
            try
            {
                DateTime dateNow = DateTime.Now;

                //Danh sách user ký trên những hợp đồng trong thời hạn ký
                var listUserSignOnDoc = from d in _dataContext.Document.AsNoTracking()
                                        where !d.IsDeleted && d.SignExpireAtDate.HasValue && d.SignExpireAtDate.Value >= dateNow
                                        join u in _dataContext.WorkflowUserSign.AsNoTracking() on d.NextStepUserId equals u.UserId
                                        select new
                                        {
                                            DocumentId = d.Id,
                                            DocumentCode = d.Code,
                                            DocumentName = d.Name,
                                            d.NextStepUserId,
                                            d.NextStepUserName,
                                            d.NextStepUserEmail,
                                            d.SignExpireAtDate,
                                            u.NotifyConfigRemindId,
                                        };

                //Danh sách user có xác nhận gửi Notify
                var listUserSendNotifyRemind = (from us in listUserSignOnDoc
                                                join w in _dataContext.NotifyConfig.AsNoTracking() on us.NotifyConfigRemindId equals w.Id
                                                where us.NotifyConfigRemindId.HasValue && w.Status && w.IsSendNotification
                                                select new
                                                {
                                                    us.DocumentId,
                                                    us.DocumentCode,
                                                    us.DocumentName,
                                                    us.SignExpireAtDate,
                                                    us.NextStepUserId,
                                                    us.NextStepUserName,
                                                    w.IsRepeat,
                                                    w.DaySendNotiBefore,
                                                    w.TimeSendNotify,
                                                    w.NotificationTitleTemplate,
                                                    w.NotificationBodyTemplate,
                                                }).ToList();

                //Lấy danh sách log Notify gửi trong ngày
                DateTime beginOfDay = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0);
                DateTime endOfDay = beginOfDay.AddDays(1);
                var builder = Builders<NotifyLog>.Filter.And(
                    Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.NOTIFY) && p.NotifyConfigType.Equals(NotifyConfigType.REMIND) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                IFindFluent<NotifyLog, NotifyLog> data = _notifyLog.Find(builder);
                var listNotifyRemindSendedInDay = await data.ToListAsync();

                //Lấy danh sách log Notify gửi ngày trước đó
                DateTime yesterday = dateNow.AddDays(-1);
                beginOfDay = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
                endOfDay = beginOfDay.AddDays(1);
                builder = Builders<NotifyLog>.Filter.And(
                  Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.NOTIFY) && p.NotifyConfigType.Equals(NotifyConfigType.REMIND) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                data = _notifyLog.Find(builder);
                var listNotifyRemindSendedYesterday = await data.ToListAsync();

                DateTime? dateStartRemind = null;
                bool isSendedInDay, isSendedYesterday = false;
                string timeNow, docId, userId;
                object param;
                string[] contents;
                NotifyLog notifyLog;
                foreach (var item in listUserSendNotifyRemind)
                {
                    timeNow = dateNow.ToString("HH:mm");
                    if (item.DaySendNotiBefore.HasValue) dateStartRemind = item.SignExpireAtDate.Value.AddDays(-item.DaySendNotiBefore.Value);

                    docId = item.DocumentId.ToString();
                    userId = item.NextStepUserId.ToString();

                    isSendedInDay = listNotifyRemindSendedInDay.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    isSendedYesterday = listNotifyRemindSendedYesterday.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));

                    //Trong khoảng ngày gửi và đến thời gian sẽ gửi
                    if (dateStartRemind <= dateNow && dateNow <= item.SignExpireAtDate && timeNow.Equals(item.TimeSendNotify) && !isSendedInDay)
                    {
                        if (!item.IsRepeat && isSendedYesterday)//Nếu không lặp và đã gửi mail một lần
                            continue;

                        #region Replace nội dung
                        param = new
                        {
                            userFullName = item.NextStepUserName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        contents = Utils.ReplaceContentNotify(param, item.NotificationTitleTemplate, item.NotificationBodyTemplate);
                        #endregion

                        #region Ghi log lịch sử gửi notify
                        notifyLog = new NotifyLog
                        {
                            DocumentId = item.DocumentId.ToString(),
                            DocumentCode = item.DocumentCode.ToString(),
                            UserId = item.NextStepUserId.HasValue ? item.NextStepUserId.Value.ToString() : string.Empty,
                            UserName = item.NextStepUserName,
                            Content = contents[0] + contents[1],
                            NotifyType = NotifyType.NOTIFY,
                            NotifyConfigType = NotifyConfigType.REMIND,
                            CreatedDate = dateNow
                        };

                        await _notifyLog.InsertOneAsync(notifyLog).ConfigureAwait(false);
                        #endregion

                        #region Gửi Notify
                        #endregion
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
            }
        }

        public async Task SendSMSExpiredAsync()
        {
            try
            {
                DateTime dateNow = DateTime.Now;

                //Danh sách user ký trên những hợp đồng đã hết hạn ký
                var listUserSignOnDoc = from d in _dataContext.Document.AsNoTracking()
                                        where !d.IsDeleted && d.SignExpireAtDate.HasValue && d.SignExpireAtDate.Value < dateNow
                                        join u in _dataContext.WorkflowUserSign.AsNoTracking() on d.NextStepUserId equals u.UserId
                                        select new
                                        {
                                            DocumentId = d.Id,
                                            DocumentCode = d.Code,
                                            DocumentName = d.Name,
                                            d.SignExpireAtDate,
                                            d.NextStepUserId,
                                            d.NextStepUserName,
                                            d.NextStepUserPhoneNumber,
                                            u.NotifyConfigExpireId
                                        };

                //Danh sách user có xác nhận gửi SMS
                var listUserSendSMSExpired = (from us in listUserSignOnDoc
                                              join w in _dataContext.WorkflowStepExpireNotify.AsNoTracking() on us.NotifyConfigExpireId equals w.Id
                                              where us.NotifyConfigExpireId.HasValue && w.Status && w.IsSendSMS
                                              select new
                                              {
                                                  us.DocumentId,
                                                  us.DocumentCode,
                                                  us.DocumentName,
                                                  us.NextStepUserId,
                                                  us.NextStepUserName,
                                                  us.NextStepUserPhoneNumber,
                                                  us.SignExpireAtDate,
                                                  w.TimeSendNotify,
                                                  w.SMSTemplate,
                                              }).ToList();

                //Lấy danh sách log SMS gửi trong ngày
                DateTime beginOfDay = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0);
                DateTime endOfDay = beginOfDay.AddDays(1);
                var builder = Builders<NotifyLog>.Filter.And(
                    Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.SMS) && p.NotifyConfigType.Equals(NotifyConfigType.EXPIRE) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                IFindFluent<NotifyLog, NotifyLog> data = _notifyLog.Find(builder);
                var listSMSExpireSendedInDay = await data.ToListAsync();

                //Lấy danh sách email gửi ngày trước đó
                DateTime yesterday = dateNow.AddDays(-1);
                beginOfDay = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
                endOfDay = beginOfDay.AddDays(1);

                builder = Builders<NotifyLog>.Filter.And(
                  Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.EMAIL) && p.NotifyConfigType.Equals(NotifyConfigType.EXPIRE) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                data = _notifyLog.Find(builder);
                var listSMSExpireSendedYesterday = await data.ToListAsync();

                bool isSendedInDay, isSendedYesterday = false;
                string timeNow, docId, userId;
                object param;
                string[] contents;
                NotifyLog notify;
                foreach (var item in listUserSendSMSExpired)
                {
                    docId = item.DocumentId.ToString();
                    userId = item.NextStepUserId.ToString();
                    timeNow = dateNow.ToString("HH:mm");
                    isSendedInDay = listSMSExpireSendedInDay.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    isSendedYesterday = listSMSExpireSendedYesterday.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    if (timeNow.Equals(item.TimeSendNotify) && !isSendedInDay && !isSendedYesterday)
                    {
                        #region Replace nội dung
                        param = new
                        {
                            userFullName = item.NextStepUserName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        contents = Utils.ReplaceContentNotify(param, item.SMSTemplate);
                        #endregion

                        #region Ghi log lịch sử gửi notify
                        notify = new NotifyLog
                        {
                            DocumentId = item.DocumentId.ToString(),
                            DocumentCode = item.DocumentCode.ToString(),
                            UserId = item.NextStepUserId.HasValue ? item.NextStepUserId.Value.ToString() : string.Empty,
                            UserName = item.NextStepUserName,
                            Content = contents[0],
                            NotifyType = NotifyType.SMS,
                            NotifyConfigType = NotifyConfigType.EXPIRE,
                            CreatedDate = dateNow
                        };

                        await _notifyLog.InsertOneAsync(notify).ConfigureAwait(false);
                        #endregion

                        #region Gửi SMS
                        #endregion
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
            }
        }

        public async Task SendSMSRemindAsync()
        {
            try
            {
                DateTime dateNow = DateTime.Now;

                //Danh sách user ký trên những hợp đồng trong thời hạn ký
                var listUserSignOnDoc = from d in _dataContext.Document.AsNoTracking()
                                        where !d.IsDeleted && d.SignExpireAtDate.HasValue && d.SignExpireAtDate.Value >= dateNow
                                        join u in _dataContext.WorkflowUserSign.AsNoTracking() on d.NextStepUserId equals u.UserId
                                        select new
                                        {
                                            DocumentId = d.Id,
                                            DocumentCode = d.Code,
                                            DocumentName = d.Name,
                                            d.NextStepUserId,
                                            d.NextStepUserName,
                                            d.NextStepUserPhoneNumber,
                                            d.SignExpireAtDate,
                                            u.NotifyConfigRemindId,
                                        };

                //Danh sách user có xác nhận gửi SMS
                var listUserSendSMSRemind = (from us in listUserSignOnDoc
                                             join w in _dataContext.NotifyConfig.AsNoTracking() on us.NotifyConfigRemindId equals w.Id
                                             where us.NotifyConfigRemindId.HasValue && w.Status && w.IsSendSMS
                                             select new
                                             {
                                                 us.DocumentId,
                                                 us.DocumentCode,
                                                 us.DocumentName,
                                                 us.SignExpireAtDate,
                                                 us.NextStepUserId,
                                                 us.NextStepUserName,
                                                 us.NextStepUserPhoneNumber,
                                                 w.IsRepeat,
                                                 w.DaySendNotiBefore,
                                                 w.TimeSendNotify,
                                                 w.SMSTemplate,
                                             }).ToList();

                //Lấy danh sách log SMS gửi trong ngày
                DateTime beginOfDay = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0);
                DateTime endOfDay = beginOfDay.AddDays(1);
                var builder = Builders<NotifyLog>.Filter.And(
                    Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.SMS) && p.NotifyConfigType.Equals(NotifyConfigType.REMIND) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                IFindFluent<NotifyLog, NotifyLog> data = _notifyLog.Find(builder);
                var listSMSRemindSendedInDay = await data.ToListAsync();

                //Lấy danh sách log SMS gửi ngày trước đó
                DateTime yesterday = dateNow.AddDays(-1);
                beginOfDay = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0);
                endOfDay = beginOfDay.AddDays(1);
                builder = Builders<NotifyLog>.Filter.And(
                  Builders<NotifyLog>.Filter.Where(p => p.NotifyType.Equals(NotifyType.SMS) && p.NotifyConfigType.Equals(NotifyConfigType.REMIND) && (beginOfDay <= p.CreatedDate && p.CreatedDate < endOfDay)));

                data = _notifyLog.Find(builder);
                var listSMSRemindSendedYesterday = await data.ToListAsync();

                DateTime? dateStartRemind = null;
                bool isSendedInDay, isSendedYesterday = false;
                string timeNow, docId, userId;
                object param;
                string[] contents;
                NotifyLog notifyLog;
                foreach (var item in listUserSendSMSRemind)
                {
                    timeNow = dateNow.ToString("HH:mm");
                    if (dateStartRemind.HasValue) dateStartRemind = item.SignExpireAtDate.Value.AddDays(-item.DaySendNotiBefore.Value);

                    docId = item.DocumentId.ToString();
                    userId = item.NextStepUserId.ToString();

                    isSendedInDay = listSMSRemindSendedInDay.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));
                    isSendedYesterday = listSMSRemindSendedYesterday.Any(r => r.DocumentId.Equals(docId) && r.UserId.Equals(userId));

                    //Trong khoảng ngày gửi và đến thời gian sẽ gửi
                    if (dateStartRemind <= dateNow && dateNow <= item.SignExpireAtDate && timeNow.Equals(item.TimeSendNotify) && !isSendedInDay)
                    {
                        if (!item.IsRepeat && isSendedYesterday)//Nếu không lặp và đã gửi mail một lần
                            continue;

                        #region Replace nội dung
                        param = new
                        {
                            userFullName = item.NextStepUserName,
                            documentCode = item.DocumentCode,
                            documentName = item.DocumentName,
                            expireTime = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("HH:mm:ss") : string.Empty,
                            expireDate = item.SignExpireAtDate.HasValue ? item.SignExpireAtDate.Value.ToString("dd/MM/yyyy") : string.Empty
                        };

                        contents = Utils.ReplaceContentNotify(param, item.SMSTemplate);
                        #endregion

                        #region Ghi log lịch sử gửi notify
                        notifyLog = new NotifyLog
                        {
                            DocumentId = item.DocumentId.ToString(),
                            DocumentCode = item.DocumentCode.ToString(),
                            UserId = item.NextStepUserId.HasValue ? item.NextStepUserId.Value.ToString() : string.Empty,
                            UserName = item.NextStepUserName,
                            Content = contents[0],
                            NotifyType = NotifyType.SMS,
                            NotifyConfigType = NotifyConfigType.REMIND,
                            CreatedDate = dateNow
                        };

                        await _notifyLog.InsertOneAsync(notifyLog).ConfigureAwait(false);
                        #endregion

                        #region Send SMS
                        #endregion
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
            }
        }
    }
}
