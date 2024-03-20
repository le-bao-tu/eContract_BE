using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;

namespace NetCore.Business
{
    public class WorkflowBaseModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; } = true;
        public Guid? OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSignOrgConfirm { get; set; }
        public bool IsSignCertify { get; set; } = false;
        public Guid? CreatedUserId { get; set; }
        public string CreatedUserName { get; set; }
        public List<WorkflowBaseModel> ListWorkflowHistory { get; set; }
        public bool IsUseEverify { get; set; }
    }

    public class WorkflowModel : WorkflowBaseModel
    {
        public Guid? NotifyConfigDocumentCompleteId { get; set; }
        public List<WorkflowUserModel> ListUser { get; set; }
        public int Order { get; set; } = 0;
        public string Description { get; set; }
    }

    public class WorkflowDetailModel : WorkflowModel
    {
        public Guid? CreatedUserId { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? ModifiedUserId { get; set; }
    }

    public class WorkflowCreateModel : WorkflowModel
    {
        public Guid? CreatedUserId { get; set; }
        public Guid? ApplicationId { get; set; }
    }

    public class WorkflowUpdateModel : WorkflowModel
    {
        public Guid ModifiedUserId { get; set; }

        public void UpdateToEntity(Workflow entity)
        {
            //entity.Code = this.Code;
            entity.Name = this.Name;
            entity.Status = this.Status;
            entity.IsSignOrgConfirm = this.IsSignOrgConfirm;
            entity.IsSignCertify = this.IsSignCertify;
            entity.NotifyConfigDocumentCompleteId = this.NotifyConfigDocumentCompleteId;
            entity.Order = this.Order;
            entity.OrganizationId = this.OrganizationId;
            entity.UserId = this.UserId;
            entity.Description = this.Description;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedUserId = this.ModifiedUserId;
            entity.IsUseEverify = this.IsUseEverify;
        }
    }

    public class WorkflowQueryFilter
    {
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public bool? Status { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid CurrentOrganizationId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public Guid? UserId { get; set; }
        public WorkflowQueryFilter()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }

    public class WorkflowUserModel
    {
        public Guid Id { get; set; }
        public int Order { get; set; } = 0;
        public SignType Type { get; set; }
        public string Name { get; set; }
        public Guid? StateId { get; set; }
        public string State { get; set; }
        public string StateName { get; set; }
        public int? SignExpireAfterDay { get; set; }
        public int? SignCloseAfterDay { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string UserConnectId { get; set; }
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserPositionName { get; set; }
        public string ADSSProfileName { get; set; }
        //public Guid? PositionId { get; set; }

        public bool IsSignLTV { get; set; } = false;
        public bool IsSignTSA { get; set; } = false;
        public bool IsSignCertify { get; set; } = false;

        public List<ConsentSignConfig> ConsentSignConfig { get; set; }

        //Quy trình chạy đến bước hiện tại
        //Có gửi OTP truy cập hợp đồng hay không => nhớ kiểm tra người dùng đã bật smart OTP chưa
        public bool IsSendOTPNotiSign { get; set; } = false;

        //Có nhận thông báo hợp đồng cần ký hay không?
        public bool IsSendMailNotiSign { get; set; } = false;

        //Có nhận thông báo hợp đồng đã hoàn thành ký hay không?
        public bool IsSendMailNotiResult { get; set; } = false;

        //Có ký tự động hay là không? => nếu có cấu hình HSM và userpin + alias đã được lưu
        public bool IsAutoSign { get; set; } = false;

        //Quy trình chạy xong bước hiện tại
        //Có gửi thông báo cho hệ thống khách hàng hay không
        public bool IsSendNotiSignedFor3rdApp { get; set; } = false;
        public Guid? NotifyConfigExpireId { get; set; }
        public Guid? NotifyConfigRemindId { get; set; }
        public Guid? NotifyConfigUserSignCompleteId { get; set; }

        public List<int> UserReceiveNotiExpire { get; set; }
        //public int? SignCloseAfterDay { get; set; }

        public bool IsAllowRenew { get; set; }
        public int? MaxRenewTimes { get; set; }
    }

    public class WorkflowUserStepDetailModel : WorkflowUserModel
    {
        public Guid WorkflowId { get; set; }
        public List<Guid> ListStepIdReceiveResult { get; set; }
    }
    public class WorkflowSelectItemModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public Guid? UserId { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public class WorkFlowUserProcessModel
    {
        public SignType Type { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string UserName { get; set; }
        public bool IsProcessed { get; set; }
    }
}