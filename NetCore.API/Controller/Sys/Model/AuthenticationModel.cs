using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCore.API
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string ApplicationId { get; set; }
    }


    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public BaseUserModel UserModel { get; set; }
        public string TokenString { get; set; }
        public DateTime TimeExpride { get; set; }
        //public BaseApplicationModel ApplicationModel { get; set; }
        public Guid ApplicationId { get; set; }
        public List<string> ListRight { get; set; }
        public List<string> ListRole { get; set; }
    }

    public class BaseUserLoginModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsLocked { get; set; }
        public string QrCodeString { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid? OrganizationId { get; set; }
        public List<string> ListRole { get; set; }
        public List<string> ListRight { get; set; }
    }
    public class UserLoginModel : BaseUserLoginModel
    {
    }

    public class BaseUserModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string IdentityNumber { get; set; }
        public string PhoneNumber { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
