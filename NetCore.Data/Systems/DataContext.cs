using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<SystemApplication> SystemApplication { get; set; }

        #region eContract
        public DbSet<DocumentType> DocumentType { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplate { get; set; }
        public DbSet<DocumentFileTemplate> DocumentFileTemplate { get; set; }
        public DbSet<DocumentMetaDataConfig> DocumentMetaDataConfig { get; set; }
        public DbSet<MetaData> MetaData { get; set; }
        public DbSet<DocumentBatch> DocumentBatch { get; set; }
        public DbSet<DocumentBatchFile> DocumentBatchFile { get; set; }
        public DbSet<Document> Document { get; set; }
        public DbSet<OrganizationConfig> OrganizationConfig { get; set; }
        public DbSet<DocumentFile> DocumentFile { get; set; }
        public DbSet<DocumentSignHistory> DocumentSignHistory { get; set; }
        public DbSet<VSMSSendQueue> VSMSSendQueue { get; set; }
        public DbSet<SignRequestHistory> SignRequestHistory { get; set; }
        public DbSet<DocumentWorkflowHistory> DocumentWorkflowHistory { get; set; }
        public DbSet<DocumentNotifySchedule> DocumentNotifySchedule { get; set; }
        #endregion

        #region User&Org
        public DbSet<User> User { get; set; }
        public DbSet<UserHSMAccount> UserHSMAccount { get; set; }
        public DbSet<Organization> Organization { get; set; }
        public DbSet<OrganizationType> OrganizationType { get; set; }
        public DbSet<UserRole> UserRole { get; set; }
        public DbSet<UserSignConfig> UserSignConfig { get; set; }
        public DbSet<UserMapDevice> UserMapDevice { get; set; }
        public DbSet<UserMapFirebaseToken> UserMapFirebaseToken { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<UserMapRole> UserMapRole { get; set; }
        public DbSet<RoleMapDocumentType> RoleMapDocumentType { get; set; }
        public DbSet<RoleMapDocumentOfOrganization> RoleMapDocumentOfOrganization { get; set; }
        public DbSet<RoleMapUserInfoOfOrganization> RoleMapUserInfoOfOrganization { get; set; }

        public DbSet<Navigation> Navigation { get; set; }
        public DbSet<Right> Right { get; set; }
        public DbSet<NavigationMapRole> NavigationMapRole { get; set; }
        public DbSet<RoleMapRight> RoleMapRight { get; set; }

        #endregion

        #region Email
        public DbSet<EmailAccount> EmailAccount { get; set; }
        public DbSet<QueueSendEmail> QueueSendEmail { get; set; }
        #endregion

        #region Workflow
        public DbSet<Workflow> Workflow { get; set; }
        public DbSet<WorkflowState> WorkflowState { get; set; }
        public DbSet<WorkflowUserSign> WorkflowUserSign { get; set; }
        public DbSet<WorkflowStepRemindNotify> WorkflowStepRemindNotify { get; set; }
        public DbSet<WorkflowStepExpireNotify> WorkflowStepExpireNotify { get; set; }
        #endregion

        #region Cấu hình 
        public DbSet<NotifyConfig> NotifyConfig { get; set; }

        #endregion

        #region Catalog
        public DbSet<Position> Position { get; set; }
        public DbSet<Country> Country { get; set; }
        public DbSet<Province> Province { get; set; }
        public DbSet<District> District { get; set; }
        public DbSet<Ward> Ward { get; set; }
        #endregion
    }
}
