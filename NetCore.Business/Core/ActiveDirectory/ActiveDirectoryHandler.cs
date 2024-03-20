using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class ActiveDirectoryHandler : IActiveDirectoryHandler
    {
        private ADHelper handler;
        public ActiveDirectoryHandler()
        {
            handler = new ADHelper();
        }

        public Response GetUserByName(string name)
        {
            try
            {
                var allUser = handler.GetUser(name);

                if (allUser != null)
                {
                    var nU = new BaseUserClient() { UserName = allUser.SamAccountName, EmailAddress = allUser.EmailAddress, FullName = allUser.DisplayName };
                    return new ResponseObject<BaseUserClient>(nU, MessageConstants.GetDataSuccessMessage, Code.Success);
                }

                return new ResponseObject<BaseUserClient>(null, MessageConstants.GetDataErrorMessage, Code.BadRequest);
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new ResponseObject<BaseUserClient>(null, MessageConstants.GetDataErrorMessage, Code.BadRequest);
            }
        }

        public Response ValidateCredentials(string userName, string password)
        {
            try
            {
                var result = handler.ValidateCredentials(userName, password);

                if (result)
                {
                    return new ResponseObject<bool>(true, "Credentials valid.", Code.Success);
                }

                return new ResponseObject<bool>(false, "Credentials invalid!", Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Validate Credentials error!", Code.BadRequest);
            }
        }

        public Response IsUserExpired(string userName)
        {
            try
            {
                var result = handler.IsUserExpired(userName);

                if (result)
                {
                    return new ResponseObject<bool>(true, "User is expired.", Code.Success);
                }

                return new ResponseObject<bool>(false, "User is not expired!", Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Check user expired error!", Code.BadRequest);
            }
        }

        public Response IsUserExisiting(string userName)
        {
            try
            {
                var result = handler.IsUserExisiting(userName);

                if (result)
                {
                    return new ResponseObject<bool>(true, "User is exisiting.", Code.Success);
                }

                return new ResponseObject<bool>(false, "User is not existing!", Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Check user existing error!", Code.BadRequest);
            }
        }

        public Response IsAccountLocked(string userName)
        {
            try
            {
                var result = handler.IsAccountLocked(userName);

                if (result)
                {
                    return new ResponseObject<bool>(true, "Is account locked.", Code.Success);
                }

                return new ResponseObject<bool>(false, "Is account active!", Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Check account locked error!", Code.BadRequest);
            }
        }

        public Response IsGroupExisiting(string groupName)
        {
            try
            {
                var result = handler.IsGroupExisiting(groupName);

                if (result)
                {
                    return new ResponseObject<bool>(true, "Group is exisiting.", Code.Success);
                }

                return new ResponseObject<bool>(false, "Group is not exisiting!", Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Check group exisiting error!", Code.BadRequest);
            }
        }

        public Response AddOrUpdateUser(string userName, string password, string givenName, string surName, bool enabled, string tel, string email)
        {
            try
            {
                var result = handler.AddOrUpdateUser(userName, password, givenName, surName, enabled, tel, email);

                if (result != null)
                {
                    var nU = new BaseUserClient() { UserName = result.Name, EmailAddress = result.EmailAddress };
                    return new ResponseObject<BaseUserClient>(nU, MessageConstants.CreateSuccessMessage, Code.Success);
                }

                return new ResponseObject<BaseUserClient>(null, MessageConstants.CreateErrorMessage, Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<BaseUserClient>(null, MessageConstants.CreateErrorMessage, Code.BadRequest);
            }
        }

        public Response DeleteUser(string name)
        {
            try
            {
                var result = handler.DeleteUser(name);

                if (result)
                {
                    return new ResponseObject<bool>(true, MessageConstants.DeleteItemSuccessMessage, Code.Success);
                }

                return new ResponseObject<bool>(false, MessageConstants.DeleteItemNotFoundMessage, Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, MessageConstants.DeleteItemErrorMessage, Code.BadRequest);
            }
        }

        public Response SetUserPassword(string userName, string newPassword)
        {
            try
            {
                handler.SetUserPassword(userName, newPassword);

                return new ResponseObject<bool>(true, "Set password success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Set password failed!", Code.BadRequest);
            }
        }
        public Response ChangePassword(string userName, string oldPassword, string newPassword)
        {
            try
            {
                //string userDn = "LDAP://betacorp.vn:389/CN=root,OU=BetaSavisTMS,DC=betacorp,dc=vn";
                //DirectoryEntry uEntry = new DirectoryEntry(userDn, "Administrator@betacorp.vn", "csi@123");
                //uEntry.Invoke("ChangePassword", new object[] { oldPassword, newPassword });
                //uEntry.Properties["LockOutTime"].Value = 0; //unlock account
                //uEntry.CommitChanges();
                handler.ChangePassword(userName, oldPassword, newPassword);

                return new ResponseObject<bool>(true, "Change password success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Change password failed!", Code.BadRequest);
            }
        }
        public Response ResetUserPasswordDefault(string userName, string defaultPassword)
        {
            try
            {
                handler.SetUserPassword(userName, defaultPassword);

                return new ResponseObject<bool>(true, "Reset password default success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Reset password default failed!", Code.BadRequest);
            }
        }

        public Response EnableUserAccount(string userName)
        {
            try
            {
                handler.EnableUserAccount(userName);

                return new ResponseObject<bool>(true, "Enable user success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Enable user failed!", Code.BadRequest);
            }
        }

        public Response DisableUserAccount(string userName)
        {
            try
            {
                handler.DisableUserAccount(userName);

                return new ResponseObject<bool>(true, "Disable user account success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Disable user account failed!", Code.BadRequest);
            }
        }

        public Response ExpireUserPassword(string userName)
        {
            try
            {
                handler.ExpireUserPassword(userName);

                return new ResponseObject<bool>(true, "Expire user password success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Expire user password failed!", Code.BadRequest);
            }
        }

        public Response SetPasswordNeverExpire(string userName, bool isExpire)
        {
            try
            {
                handler.SetPasswordNeverExpire(userName, isExpire);

                return new ResponseObject<bool>(true, "Set password never expire success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Set password never expire failed.", Code.BadRequest);
            }
        }

        public Response UnlockUserAccount(string userName)
        {
            try
            {
                handler.UnlockUserAccount(userName);

                return new ResponseObject<bool>(true, "Unlock user account success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Unlock user account failed!", Code.BadRequest);
            }
        }

        // public OldResponse<bool> UpdateGroup(GroupPrincipal grPrincipal)
        // {
        //     try
        //     {
        //         handler.UpdateGroup(grPrincipal);

        //         return new OldResponse<bool>(1, string.Empty, true);
        //     }
        //     catch (Exception ex)
        //     {
        //         Serilog.Log.Error(ex, "");
        //         return new OldResponse<bool>(-1, ex.Message, false);
        //     }
        // }

        // public OldResponse<bool> CreateNewGroup(string ouName, string groupName, string sDescription, GroupScope oGroupScope, bool bSecurityGroup)
        // {
        //     try
        //     {
        //         handler.CreateNewGroup(ouName, groupName, sDescription, oGroupScope, bSecurityGroup);

        //         return new OldResponse<bool>(1, string.Empty, true);
        //     }
        //     catch (Exception ex)
        //     {
        //         Serilog.Log.Error(ex, "");
        //         return new OldResponse<bool>(-1, ex.Message, false);
        //     }
        // }

        public Response AddUserToGroup(string userName, string groupName)
        {
            try
            {
                handler.AddUserToGroup(userName, groupName);

                return new ResponseObject<bool>(true, "Unlock user account success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Unlock user account failed!", Code.BadRequest);
            }
        }

        public Response RemoveUserFromGroup(string userName, string groupName)
        {
            try
            {
                handler.RemoveUserFromGroup(userName, groupName);

                return new ResponseObject<bool>(true, "Remove user from group success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Remove user from group failed!", Code.BadRequest);
            }
        }

        public Response IsUserGroupMember(string userName, string groupName)
        {
            try
            {
                handler.IsUserGroupMember(userName, groupName);

                return new ResponseObject<bool>(true, "Is user group member success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Is user group member failed!", Code.BadRequest);
            }
        }

        public Response GetUserGroups(string userName)
        {
            try
            {
                var result = handler.GetUserGroups(userName);

                return new ResponseObject<bool>(true, "Get user group success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Get user group failed!", Code.BadRequest);
            }
        }

        public Response GetUserAuthorizationGroups(string userName)
        {
            try
            {
                var result = handler.GetUserAuthorizationGroups(userName);

                return new ResponseObject<bool>(true, "Get user authorization group success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Get user authorization group failed!", Code.BadRequest);
            }
        }


        public Response GetADInfomation()
        {
            try
            {
                var adSysnAcountConfig = Utils.GetConfig("membership:ad:sysnaccount");
                var adAuthenEnableConfig = Utils.GetConfig("membership:ad:authentication");
                var adServerConfig = Utils.GetConfig("ad:serverAddress");
                var adLdapPathConfig = Utils.GetConfig("ad:ldap-path");

                var result = new ADClient()
                {
                    ADServer = adServerConfig,
                    ADAuthenEnable = adAuthenEnableConfig == "1" ? true : false,
                    ADLdapPath = adLdapPathConfig,
                    ADSysnAcount = adSysnAcountConfig == "1" ? true : false,
                };

                try
                {
                    var resultS = handler.GetUser("administrator");
                    if (resultS != null)
                    {
                        result.IsWork = true;
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "");
                    result.IsWork = false;
                }

                return new ResponseObject<ADClient>(result, "Get AD infomation success.", Code.Success);
            }
            catch (Exception ex)
            {
                return new ResponseObject<bool>(false, "Get AD infomation failed!", Code.BadRequest);
            }
        }

        public Response ChangeStatus(ADClient adClient)
        {
            throw new NotImplementedException();
        }

        // public OldResponse<bool> ChangeStatus(ADClient adClient)
        // {
        //     try
        //     {
        //         var resultS = handler.GetUser("administrator");
        //         var adSysnAcountConfig = Utils.GetConfig("membership:ad:sysnaccount"];
        //         var adAuthenEnableConfig = Utils.GetConfig("membership:ad:authentication"];
        //         var adServerConfig = Utils.GetConfig("ad:serverAddress"];
        //         var adLdapPathConfig = Utils.GetConfig("ad:ldap-path"];

        //         System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        //         config.AppSettings.Settings["membership:ad:sysnaccount"].Value = adClient.ADSysnAcount ? "1" : "0";
        //         config.AppSettings.Settings["membership:ad:authentication"].Value = adClient.ADAuthenEnable ? "1" : "0";
        //         config.Save(ConfigurationSaveMode.Modified);

        //         return new OldResponse<bool>(0, string.Empty, true);
        //     }
        //     catch (Exception ex)
        //     {
        //         Serilog.Log.Error(ex, "");
        //         return new OldResponse<bool>(-1, string.Empty, false);
        //     }
        // }


        //public OldResponse<bool> SysnAccFromAd(string p)
        //{
        //    try
        //    {
        //        var acc = GetUserByName(p);
        //        if (acc.Data != null)
        //        {
        //            //add member to membership

        //            var handler = new MembershipHandler();

        //            var user = handler.GetUserByName(p);

        //            if (user == null)
        //            {
        //                var userClient = handler.CreateUser(acc.Data.UserName, "12345aA@", acc.Data.UserName, acc.Data.EmailAddress, true, DateTime.Now,
        //                    Application.SuperUserID.ToString(), DateTime.Now, Application.SuperUserID.ToString(), acc.Data.UserName, null, new List<RoleModel>(), acc.Data.ApplicationId, false);
        //            }
        //            else
        //            {
        //                var userClient = handler.UpdateUser(user.UserId, user.Password, acc.Data.UserName, acc.Data.EmailAddress,
        //                            true, DateTime.Now, Application.SuperUserID.ToString(), new List<RoleModel>(), acc.Data.NickName, null, acc.Data.ApplicationId);
        //            }

        //            //var addResult = AddOrUpdateUser(acc.Data.UserName, "12345aA@", acc.Data.NickName, acc.Data.FullName, true, acc.Data.MobileAlias, acc.Data.EmailAddress);

        //            return new OldResponse<bool>(1, "Account đồng bộ hoàn tất", true);

        //        }
        //        return new OldResponse<bool>(0, string.Empty, false);
        //    }
        //    catch (Exception ex)
        //    {
        //        Serilog.Log.Error(ex, "");
        //        return new OldResponse<bool>(-1, string.Empty, false);
        //    }
        //}

        //public OldResponse<bool> SysnAccFromAdNew(string p, UsersPostModel model)
        //{
        //    try
        //    {
        //        var acc = GetUserByName(p);
        //        if (acc.Data != null)
        //        {
        //            //add member to membership

        //            var handler = new MembershipHandler();

        //            var user = handler.GetUserByName(p);

        //            if (user == null)
        //            {
        //                var userClient = handler.CreateUser(acc.Data.UserName, "12345aA@", model.FullName, model.Email, true, DateTime.Now,
        //                    Application.SuperUserID.ToString(), DateTime.Now, Application.SuperUserID.ToString(), model.NickName, null, model.Roles, new Guid("48ED5B71-66DC-4725-9604-4C042E45FA3F"), model.AllowBackDateDKN);
        //            }
        //            else
        //            {
        //                var userClient = handler.UpdateUser(user.UserId, user.Password, model.FullName, model.Email,
        //                            true, DateTime.Now, Application.SuperUserID.ToString(), model.Roles, model.NickName, null, new Guid("48ED5B71-66DC-4725-9604-4C042E45FA3F"));
        //            }

        //            //var addResult = AddOrUpdateUser(acc.Data.UserName, "12345aA@", acc.Data.NickName, acc.Data.FullName, true, acc.Data.MobileAlias, acc.Data.EmailAddress);

        //            return new OldResponse<bool>(1, "Account đồng bộ hoàn tất", true);

        //        }
        //        return new OldResponse<bool>(0, string.Empty, false);
        //    }
        //    catch (Exception ex)
        //    {
        //        Serilog.Log.Error(ex, "");
        //        return new OldResponse<bool>(-1, string.Empty, false);
        //    }
        //}
    }
}
