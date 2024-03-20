using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetCore.Data;
using NetCore.Shared;

namespace NetCore.Business
{
    public class ADHelper
    {
        #region Vars

        private string
            _connectionStringKeyword,
            _encryptKey,
            _serverAddress,
            _domainName,
            _rootOU,
            _defaultOU,
            _userName,
            _password,
            _realPassword,
            _defaultPassword,
            _subServerAddress,
            _subDomainName,
            _subRootOU,
            _subDefaultOU,
            _subUserName,
            _subPassword,
            _subRealPassword,
            _subDefaultPassword;

        private static string
            _sConnectionStringKeyword,
            _sEncryptKey,
            _sServerAddress,
            _sDomainName,
            _sRootOU,
            _sDefaultOU,
            _sUserName,
            _sPassword,
            _sRealPassword,
            _sDefaultPassword,
            _ssubServerAddress,
            _ssubDomainName,
            _ssubRootOU,
            _ssubDefaultOU,
            _ssubUserName,
            _ssubPassword,
            _ssubRealPassword,
            _ssubDefaultPassword;
        #endregion Vars

        #region Properties
        public string ServerAddress
        {
            get { return _serverAddress; }
            //set
            //{
            //    _serverAddress = value;
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "serverAddress", _serverAddress);
            //}
        }

        public string DomainName
        {
            get { return _domainName; }
            //set 
            //{ 
            //    _domainName = value;
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "domainName", _domainName);
            //}
        }

        public string RootOU
        {
            get { return _rootOU; }
            //set
            //{
            //    _rootOU = value;
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "rootOU", _rootOU);
            //}
        }

        public string DefaultOU
        {
            get { return _defaultOU; }
            //set
            //{
            //    _defaultOU = value;
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "defaultOU", _defaultOU);
            //}
        }

        public string UserName
        {
            get { return _userName; }
            //set
            //{
            //    _userName = value;
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "userName", _userName);
            //}
        }

        public string Password
        {
            get { return _password; }
            //set
            //{
            //    _password = _encryptKey + CommonLib.Encryption.StringEncryption.Instance.EncryptString_MD5(_password, _encryptKey, true);
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "password", _password);
            //}
        }

        public string DefaultPassword
        {
            get { return _defaultPassword; }
            //set
            //{
            //    _defaultPassword = value;
            //    CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "defaultPassword", _defaultPassword);
            //}
        }

        public string SubServerAddress
        {
            get { return _subServerAddress; }
        }

        public string SubDomainName
        {
            get { return _subDomainName; }
        }

        public string SubRootOU
        {
            get { return _subRootOU; }
        }

        public string SubDefaultOU
        {
            get { return _subDefaultOU; }
        }

        public string SubUserName
        {
            get { return _subUserName; }
        }

        public string SubPassword
        {
            get { return _subPassword; }
        }

        public string SubDefaultPassword
        {
            get { return _subDefaultPassword; }
        }

        public string ConnectionStringKeyword
        {
            get { return @"LDAP://" + this.ServerAddress + @"/"; }
        }

        public string OUKeyword
        {
            get { return "OrganizationalUnit"; }
        }

        public string DefaultConnectionString
        {
            get
            {
                return this.ConnectionStringKeyword + this.DefaultOU;
            }
        }

        public string RootConnectionString
        {
            get
            {
                return this.ConnectionStringKeyword + this.RootOU;
            }
        }

        public string sServerAddress
        {
            get { return _sServerAddress; }
        }

        public string sDomainName
        {
            get { return _sDomainName; }
        }

        public string sRootOU
        {
            get { return _sRootOU; }
        }

        public string sDefaultOU
        {
            get { return _sDefaultOU; }
        }

        public string sUserName
        {
            get { return _sUserName; }
        }


        public string sPassword
        {
            get { return _sPassword; }
        }

        public string sDefaultPassword
        {
            get { return _sDefaultPassword; }
        }

        public string sConnectionStringKeyword
        {
            get { return @"LDAP://" + this.sServerAddress + @"/"; }
        }

        public string sDefaultConnectionString
        {
            get
            {
                return this.sConnectionStringKeyword + this.sDefaultOU;
            }
        }

        public string sRootConnectionString
        {
            get
            {
                return this.sConnectionStringKeyword + this.sRootOU;
            }
        }
        #endregion Properties

        #region Constructors
        private void _Initialize()
        {

            _serverAddress = Utils.GetConfig("ad:serverAddress");
            _domainName = Utils.GetConfig("ad:domainName");
            _rootOU = Utils.GetConfig("ad:rootOU");
            _defaultOU = Utils.GetConfig("ad:defaultOU");
            _userName = Utils.GetConfig("ad:userName");
            _password = Utils.GetConfig("ad:password");
            _defaultPassword = Utils.GetConfig("ad:defaultPassword");
            _realPassword = Utils.GetConfig("ad:password");

            if (string.IsNullOrEmpty(_defaultPassword))
            {
                _defaultPassword = "Abc@123";
                //CommonLib.XML.Configuration.Instance.SetValue(Properties.Instance.ConfigurationFile, Properties.Instance.ActiveDirectoryRootNode, "defaultPassword", _defaultPassword);
            }

            //if (WebUtils.GetConfig(""SubserverAddress"] != null)
            //{
            //    _subServerAddress = WebUtils.GetConfig(""SubserverAddress");
            //}

            //if (WebUtils.GetConfig(""SubdomainName"] != null)
            //{
            //    _subDomainName = WebUtils.GetConfig(""SubdomainName");
            //}

            //if (WebUtils.GetConfig(""SubrootOU"] != null)
            //{
            //    _subRootOU = WebUtils.GetConfig(""SubrootOU");
            //}

            //if (WebUtils.GetConfig(""SubdefaultOU"] != null)
            //{
            //    _subDefaultOU = WebUtils.GetConfig(""SubdefaultOU");
            //}

            //if (WebUtils.GetConfig(""SubuserName"] != null)
            //{
            //    _subUserName = WebUtils.GetConfig(""SubuserName");
            //}

            //if (WebUtils.GetConfig(""Subpassword"] != null)
            //{
            //    _subPassword = WebUtils.GetConfig(""Subpassword");
            //}

            //if (WebUtils.GetConfig(""SubdefaultPassword"] != null)
            //{
            //    _subDefaultPassword = WebUtils.GetConfig(""SubdefaultPassword");
            //}
        }

        private static void _InitializeStatic()
        {
            _sServerAddress = Utils.GetConfig("ad:serverAddress");
            _sDomainName = Utils.GetConfig("ad:domainName");
            _sRootOU = Utils.GetConfig("ad:rootOU");
            _sDefaultOU = Utils.GetConfig("ad:defaultOU");
            _sUserName = Utils.GetConfig("ad:userName");
            _sPassword = Utils.GetConfig("ad:password");
            _sDefaultPassword = Utils.GetConfig("ad:defaultPassword");
            _sRealPassword = Utils.GetConfig("ad:password");

            if (string.IsNullOrEmpty(_sDefaultPassword))
            {
                _sDefaultPassword = "Abc@123";
            }

            try
            {
                //_sEncryptKey = _sPassword.Substring(0, 36);
                //Guid encryptKey = new Guid(_sEncryptKey);
                //try
                //{
                //    _sRealPassword = CommonLib.Encryption.StringEncryption.Instance.DecryptString_MD5(_sPassword.Replace(_sEncryptKey, string.Empty), _sEncryptKey, true);
                //}
                //catch
                //{
                //    _sRealPassword = string.Empty;
                //}
            }
            catch
            {
                _sEncryptKey = Guid.NewGuid().ToString();
                _sRealPassword = _sPassword;
            }
        }

        public ADHelper()
        {
            this._Initialize();
        }

        public static ADHelper Instance
        {
            get
            {
                _InitializeStatic();
                return Nested.instance;
            }
        }

        class Nested
        {
            // Explicit constructor to tell C# compiler
            // not to mark type as beforefieldinit
            Nested()
            {
            }
            internal static readonly ADHelper instance = new ADHelper();
        }

        #endregion Constructors

        //PrincipalContext insPrincipalContext = new PrincipalContext(ContextType.Machine);
        //PrincipalContext insPrincipalContext = new PrincipalContext(ContextType.Domain, "thvn.net", "DC=thvn,DC=net", "ducdt3", "tonghop1234"); //domaine bağlanma
        //PrincipalContext insPrincipalContext = new PrincipalContext(ContextType.Machine,"TAMERO","administrator","password");//lokal bilgisayara bir kullanıcı ile bağlanma

        #region Functions
        public string GetOUName(string ouName)
        {
            return ouName.IndexOf("OU=", StringComparison.OrdinalIgnoreCase) >= 0 ? ouName : "OU=" + ouName;
        }

        public string GetDEPath(string dePath)
        {
            if (dePath.IndexOf(this.DefaultOU, StringComparison.OrdinalIgnoreCase) >= 0) return dePath;
            else return string.IsNullOrEmpty(dePath) ? string.Empty : dePath + "," + this.DefaultOU;
        }

        public string GetOUPath(string ouPath)
        {
            if (string.IsNullOrEmpty(ouPath)) return string.Empty;

            if (!string.IsNullOrEmpty(this.DefaultOU))
            {
                if (ouPath.IndexOf(this.DefaultOU, StringComparison.OrdinalIgnoreCase) >= 0) return ouPath;
            }
            else
            {
                if (ouPath.IndexOf(this.sDefaultOU, StringComparison.OrdinalIgnoreCase) >= 0) return ouPath;
            }

            try
            {
                if (ouPath.IndexOf("OU=") >= 0)
                {
                    if (!string.IsNullOrEmpty(this.DefaultOU))
                    {
                        return ouPath + "," + this.DefaultOU;
                    }
                    else
                    {
                        return ouPath + "," + this.sDefaultOU;
                    }
                }
                else
                {
                    string[] ouNames = ouPath.Split(',');
                    StringBuilder _ouPath = new StringBuilder("OU=" + ouNames[0]);
                    for (int i = 1; i < ouNames.Length; ++i)
                    {
                        _ouPath.Append(",OU=" + ouNames[i]);
                    }
                    if (!string.IsNullOrEmpty(this.DefaultOU))
                    {
                        return _ouPath.ToString() + "," + this.DefaultOU;
                    }
                    else
                    {
                        return _ouPath.ToString() + "," + this.sDefaultOU;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return string.Empty;
            }

        }

        public string GetSubOUPath(string ouPath)
        {
            if (string.IsNullOrEmpty(ouPath)) return string.Empty;
            if (ouPath.IndexOf(this.SubDefaultOU, StringComparison.OrdinalIgnoreCase) >= 0) return ouPath;

            try
            {
                if (ouPath.IndexOf("OU=") >= 0) return ouPath + "," + this.SubDefaultOU;
                else
                {
                    string[] ouNames = ouPath.Split(',');
                    StringBuilder _ouPath = new StringBuilder("OU=" + ouNames[0]);
                    for (int i = 1; i < ouNames.Length; ++i)
                    {
                        _ouPath.Append(",OU=" + ouNames[i]);
                    }
                    return _ouPath.ToString() + "," + this.SubDefaultOU;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return string.Empty;
            }
        }

        public string GetOUConnectionString(string ouPath)
        {
            string _ouPath = this.GetOUPath(ouPath);
            if (_ouPath.Equals(string.Empty)) return string.Empty;
            else return this.ConnectionStringKeyword + _ouPath;
        }

        public string GetDEConnectionString(string dePath)
        {
            return string.IsNullOrEmpty(dePath) ? string.Empty : this.ConnectionStringKeyword + this.GetDEPath(dePath);
        }

        #endregion Functions

        #region Validate Methods

        /// <summary>
        /// Validates the userName and password of a given user
        /// </summary>
        /// <param name="userName">The userName to validate</param>
        /// <param name="password">The password of the userName to validate</param>
        /// <returns>Returns True of user is valid</returns>
        public bool ValidateCredentials(string userName, string password)
        {
            var principalContext = GetPrincipalContext();
            return principalContext.ValidateCredentials(userName, password, ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing);
        }

        /// <summary>
        /// Checks if the User Account is Expired
        /// </summary>
        /// <param name="userName">The userName to check</param>
        /// <returns>Returns true if Expired</returns>
        public bool IsUserExpired(string userName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            if (userPrincipal.AccountExpirationDate != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks if user exists on AD
        /// </summary>
        /// <param name="userName">The userName to check</param>
        /// <returns>Returns true if userName Exists</returns>
        public bool IsUserExisiting(string userName)
        {
            if (GetUser(userName) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks if user account is locked
        /// </summary>
        /// <param name="userName">The userName to check</param>
        /// <returns>Returns true of Account is locked</returns>
        public bool IsAccountLocked(string userName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            return userPrincipal.IsAccountLockedOut();
        }

        /// <summary>
        /// Checks if Group exists on AD
        /// </summary>
        /// <param name="groupName">The groupname to check</param>
        /// <returns>Returns true if groupname Exists</returns>
        public bool IsGroupExisiting(string groupName)
        {
            if (GetGroup(groupName) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Gets a certain user on Active Directory
        /// </summary>
        /// <param name="userName">The userName to get</param>
        /// <returns>Returns the UserPrincipal Object</returns>
        public UserPrincipal GetUser(string userName)
        {
            PrincipalContext principalContext = GetPrincipalContextCheck();

            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(principalContext, userName);

            return userPrincipal;
        }

        /// <summary>
        /// Gets a certain user on Sub Active Directory
        /// </summary>
        /// <param name="userName">The userName to get</param>
        /// <returns>Returns the UserPrincipal Object</returns>
        public UserPrincipal GetSubUser(string userName)
        {
            PrincipalContext principalContext = GetSubPrincipalContextCheck();

            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(principalContext, userName);

            return userPrincipal;
        }

        /// <summary>
        /// Gets a certain group on Active Directory
        /// </summary>
        /// <param name="groupName">The group to get</param>
        /// <returns>Returns the GroupPrincipal Object</returns>
        public GroupPrincipal GetGroup(string groupName)
        {
            PrincipalContext principalContext = GetPrincipalContext();

            GroupPrincipal oGroupPrincipal =
               GroupPrincipal.FindByIdentity(principalContext, groupName);
            return oGroupPrincipal;
        }

        #endregion

        #region User Account Methods

        /// <summary>
        /// Sets the user password
        /// </summary>
        /// <param name="userName">The userName to set</param>
        /// <param name="newPassword">The new password to use</param>
        /// <param name="sMessage">Any output messages</param>
        public void SetUserPassword(string userName, string newPassword)
        {
            try
            {
                UserPrincipal userPrincipal = GetUser(userName);
                userPrincipal.SetPassword(newPassword);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                throw ex;
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="userName">The userName to set</param>
        /// <param name="oldPassword">Any output messages</param>
        /// <param name="newPassword">The new password to use</param>

        public void ChangePassword(string userName, string oldPassword, string newPassword)
        {
            try
            {
                UserPrincipal userPrincipal = GetUser(userName);
                userPrincipal.ChangePassword(oldPassword, newPassword);
                userPrincipal.PasswordNeverExpires = true;
                userPrincipal.Save();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                throw ex;
            }
        }

        /// <summary>
        /// Enables a disabled user account
        /// </summary>
        /// <param name="userName">The userName to enable</param>
        public void EnableUserAccount(string userName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            userPrincipal.Enabled = true;
            userPrincipal.PasswordNeverExpires = true;
            userPrincipal.Save();
        }

        /// <summary>
        /// Force disabling of a user account
        /// </summary>
        /// <param name="userName">The userName to disable</param>
        public void DisableUserAccount(string userName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            userPrincipal.Enabled = false;
            userPrincipal.Save();
        }

        /// <summary>
        /// Force expire password of a user
        /// </summary>
        /// <param name="userName">The userName to expire the password</param>
        public void ExpireUserPassword(string userName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            userPrincipal.ExpirePasswordNow();
            userPrincipal.Save();
        }

        /// <summary>
        /// SetPasswordNeverExpire
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="isExpire"></param>
        public void SetPasswordNeverExpire(string userName, bool isExpire)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            userPrincipal.PasswordNeverExpires = isExpire;
            userPrincipal.Save();
        }

        /// <summary>
        /// Unlocks a locked user account
        /// </summary>
        /// <param name="userName">The userName to unlock</param>
        public void UnlockUserAccount(string userName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            userPrincipal.UnlockAccount();
            userPrincipal.Save();
        }

        public UserPrincipal CreateNewUser(string ouName, string userName, string password, string givenName, string surName, bool enabled, string tel, string email)
        {
            try
            {
                if (!IsUserExisiting(userName))
                {
                    PrincipalContext principalContext = GetPrincipalContext(ouName);

                    UserPrincipal userPrincipal = new UserPrincipal(principalContext, userName, password, enabled);

                    //User Log on Name
                    userPrincipal.UserPrincipalName = userName;
                    userPrincipal.GivenName = string.IsNullOrEmpty(givenName) ? null : givenName;
                    userPrincipal.Surname = string.IsNullOrEmpty(surName) ? null : surName;
                    userPrincipal.VoiceTelephoneNumber = string.IsNullOrEmpty(tel) ? null : tel;
                    userPrincipal.EmailAddress = string.IsNullOrEmpty(email) ? null : email;
                    userPrincipal.PasswordNeverExpires = true;
                    userPrincipal.Save();

                    return userPrincipal;
                }
                else
                {
                    return GetUser(userName);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public UserPrincipal CreateNewUser(string ouName, string userName, string password, string givenName, string surName, string tel, string email)
        {
            return this.CreateNewUser(ouName, userName, password, givenName, surName, true, tel, email);
        }

        public UserPrincipal CreateNewUser(string userName, string password, string givenName, string surName, string tel, string email)
        {
            return this.CreateNewUser(_defaultOU, userName, password, givenName, surName, true, tel, email);
        }

        public UserPrincipal CreateNewUser(string userName, string password, string givenName, string surName, bool enabled, string tel, string email)
        {
            return this.CreateNewUser(_defaultOU, userName, password, givenName, surName, enabled, tel, email);
        }

        /// <summary>
        /// Deletes a user in Active Directory
        /// </summary>
        /// <param name="userName">The userName you want to delete</param>
        /// <returns>Returns true if successfully deleted</returns>
        public bool DeleteUser(string userName)
        {
            try
            {
                UserPrincipal userPrincipal = GetUser(userName);

                userPrincipal.Delete();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return false;
            }
        }

        public UserPrincipal AddOrUpdateUser(string ouName, string userName, string password, string givenName, string surName, bool enabled, string tel, string email)
        {
            try
            {
                UserPrincipal userPrincipal = this.GetUser(userName);
                PrincipalContext principalContext = GetPrincipalContext(ouName);
                //if (string.IsNullOrEmpty(password)) password = this.DefaultPassword;
                //UserPrincipal userPrincipal = new UserPrincipal(principalContext, userName, password, enabled);
                //userPrincipal.UserPrincipalName = userName;
                //userPrincipal.GivenName = string.IsNullOrEmpty(givenName) ? null : givenName;
                //userPrincipal.Surname = string.IsNullOrEmpty(surName) ? null : surName;
                //userPrincipal.VoiceTelephoneNumber = string.IsNullOrEmpty(tel) ? null : tel;
                //userPrincipal.EmailAddress = string.IsNullOrEmpty(email) ? null : email;
                //userPrincipal.Save();
                //isNewUser = true;

                if (userPrincipal == null)
                {
                    //if (string.IsNullOrEmpty(password)) password = this.DefaultPassword;
                    userPrincipal = new UserPrincipal(principalContext, userName, password, enabled);
                    userPrincipal.UserPrincipalName = userName;
                    userPrincipal.GivenName = string.IsNullOrEmpty(givenName) ? null : givenName;
                    userPrincipal.Surname = string.IsNullOrEmpty(surName) ? null : surName;
                    userPrincipal.VoiceTelephoneNumber = string.IsNullOrEmpty(tel) ? null : tel;
                    userPrincipal.EmailAddress = string.IsNullOrEmpty(email) ? null : email;
                    userPrincipal.PasswordNeverExpires = true;
                    userPrincipal.Save();
                    //isNewUser = true;
                }
                else
                {
                    userPrincipal.SetPassword(password);
                    userPrincipal.GivenName = string.IsNullOrEmpty(givenName) ? null : givenName;
                    userPrincipal.Surname = string.IsNullOrEmpty(surName) ? null : surName;
                    userPrincipal.VoiceTelephoneNumber = string.IsNullOrEmpty(tel) ? null : tel;
                    userPrincipal.EmailAddress = string.IsNullOrEmpty(email) ? null : email;
                    userPrincipal.Enabled = enabled;
                    //if (!string.IsNullOrEmpty(password)) userPrincipal.SetPassword(password);
                    userPrincipal.PasswordNeverExpires = true;
                    userPrincipal.Save();

                    //if (ouName.Equals(string.Empty)) this.MoveDirectoryEntry(userPrincipal.DistinguishedName, this.DefaultOU);
                    //else this.MoveDirectoryEntry(userPrincipal.DistinguishedName, this.GetOUPath(ouName));
                    //isNewUser = false;
                }
                return userPrincipal;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public UserPrincipal AddOrUpdateSubUser(string ouName, string userName, string password, string givenName, string surName, bool enabled, string tel, string email, ref bool isNewUser)
        {
            try
            {
                UserPrincipal userPrincipal = this.GetSubUser(userName);
                PrincipalContext principalContext = GetSubPrincipalContext(ouName);

                if (userPrincipal == null)
                {
                    if (string.IsNullOrEmpty(password)) password = this.DefaultPassword;
                    userPrincipal = new UserPrincipal(principalContext, userName, password, enabled);
                    userPrincipal.UserPrincipalName = userName;
                    userPrincipal.GivenName = string.IsNullOrEmpty(givenName) ? null : givenName;
                    userPrincipal.Surname = string.IsNullOrEmpty(surName) ? null : surName;
                    userPrincipal.VoiceTelephoneNumber = string.IsNullOrEmpty(tel) ? null : tel;
                    userPrincipal.EmailAddress = string.IsNullOrEmpty(email) ? null : email;
                    userPrincipal.PasswordNeverExpires = true;
                    userPrincipal.Save();
                    isNewUser = true;
                }
                else
                {
                    userPrincipal.GivenName = string.IsNullOrEmpty(givenName) ? null : givenName;
                    userPrincipal.Surname = string.IsNullOrEmpty(surName) ? null : surName;
                    userPrincipal.VoiceTelephoneNumber = string.IsNullOrEmpty(tel) ? null : tel;
                    userPrincipal.EmailAddress = string.IsNullOrEmpty(email) ? null : email;
                    userPrincipal.Enabled = enabled;
                    if (!string.IsNullOrEmpty(password)) userPrincipal.SetPassword(password);
                    userPrincipal.PasswordNeverExpires = true;
                    userPrincipal.Save();

                    //if (ouName.Equals(string.Empty)) this.MoveDirectoryEntry(userPrincipal.DistinguishedName, this.DefaultOU);
                    //else this.MoveDirectoryEntry(userPrincipal.DistinguishedName, this.GetSubOUPath(ouName));
                    isNewUser = false;
                }
                return userPrincipal;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public UserPrincipal AddOrUpdateUser(string userName, string password, string givenName, string surName, bool enabled, string tel, string email)
        {
            return this.AddOrUpdateUser(_defaultOU, userName, password, givenName, surName, enabled, tel, email);
        }

        #endregion

        #region Group Methods

        public void UpdateGroup(GroupPrincipal grPrincipal)
        {
            grPrincipal.Save();
        }

        /// <summary>
        /// Creates a new group in Active Directory
        /// </summary>
        /// <param name="ouName">The OU location you want to save your new Group</param>
        /// <param name="groupName">The name of the new group</param>
        /// <param name="sDescription">The description of the new group</param>
        /// <param name="oGroupScope">The scope of the new group</param>
        /// <param name="bSecurityGroup">True is you want this group 
        /// to be a security group, false if you want this as a distribution group</param>
        /// <returns>Returns the GroupPrincipal object</returns>
        public GroupPrincipal CreateNewGroup(string ouName, string groupName, string sDescription, GroupScope oGroupScope, bool bSecurityGroup)
        {
            PrincipalContext principalContext = GetPrincipalContext();

            GroupPrincipal oGroupPrincipal = new GroupPrincipal(principalContext, groupName);
            oGroupPrincipal.Description = sDescription;
            oGroupPrincipal.GroupScope = oGroupScope;
            oGroupPrincipal.IsSecurityGroup = bSecurityGroup;
            oGroupPrincipal.Save();

            return oGroupPrincipal;
        }

        /// <summary>
        /// Adds the user for a given group
        /// </summary>
        /// <param name="userName">The user you want to add to a group</param>
        /// <param name="groupName">The group you want the user to be added in</param>
        /// <returns>Returns true if successful</returns>
        public bool AddUserToGroup(string userName, string groupName)
        {
            try
            {
                UserPrincipal userPrincipal = GetUser(userName);
                GroupPrincipal oGroupPrincipal = GetGroup(groupName);
                if (userPrincipal == null || oGroupPrincipal == null)
                {
                    if (!IsUserGroupMember(userName, groupName))
                    {
                        oGroupPrincipal.Members.Add(userPrincipal);
                        oGroupPrincipal.Save();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return false;
            }
        }

        /// <summary>
        /// Removes user from a given group
        /// </summary>
        /// <param name="userName">The user you want to remove from a group</param>
        /// <param name="groupName">The group you want the user to be removed from</param>
        /// <returns>Returns true if successful</returns>
        public bool RemoveUserFromGroup(string userName, string groupName)
        {
            try
            {
                UserPrincipal userPrincipal = GetUser(userName);
                GroupPrincipal oGroupPrincipal = GetGroup(groupName);
                if (userPrincipal == null || oGroupPrincipal == null)
                {
                    if (IsUserGroupMember(userName, groupName))
                    {
                        oGroupPrincipal.Members.Remove(userPrincipal);
                        oGroupPrincipal.Save();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return false;
            }
        }

        /// <summary>
        /// Checks if user is a member of a given group
        /// </summary>
        /// <param name="userName">The user you want to validate</param>
        /// <param name="groupName">The group you want to check the 
        /// membership of the user</param>
        /// <returns>Returns true if user is a group member</returns>
        public bool IsUserGroupMember(string userName, string groupName)
        {
            UserPrincipal userPrincipal = GetUser(userName);
            GroupPrincipal oGroupPrincipal = GetGroup(groupName);

            if (userPrincipal == null || oGroupPrincipal == null)
            {
                return oGroupPrincipal.Members.Contains(userPrincipal);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of the users group memberships
        /// </summary>
        /// <param name="userName">The user you want to get the group memberships</param>
        /// <returns>Returns an arraylist of group memberships</returns>
        public ArrayList GetUserGroups(string userName)
        {
            ArrayList myItems = new ArrayList();
            UserPrincipal userPrincipal = GetUser(userName);

            PrincipalSearchResult<Principal> oPrincipalSearchResult = userPrincipal.GetGroups();

            foreach (Principal oResult in oPrincipalSearchResult)
            {
                myItems.Add(oResult.Name);
            }
            return myItems;
        }

        /// <summary>
        /// Gets a list of the users authorization groups
        /// </summary>
        /// <param name="userName">The user you want to get authorization groups</param>
        /// <returns>Returns an arraylist of group authorization memberships</returns>
        public ArrayList GetUserAuthorizationGroups(string userName)
        {
            ArrayList myItems = new ArrayList();
            UserPrincipal userPrincipal = GetUser(userName);

            PrincipalSearchResult<Principal> oPrincipalSearchResult =
                       userPrincipal.GetAuthorizationGroups();

            foreach (Principal oResult in oPrincipalSearchResult)
            {
                myItems.Add(oResult.Name);
            }
            return myItems;
        }

        #endregion

        #region OU Methods

        #region Get
        public DirectoryEntry GetDirectoryEntryByGuid(Guid guid)
        {
            try
            {
                return new DirectoryEntry(this.ConnectionStringKeyword + "<GUID=" + guid.ToString() + ">", this.UserName, _realPassword);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public DirectoryEntry GetOUByGuid(Guid guid)
        {
            try
            {
                DirectoryEntry ou = this.GetDirectoryEntryByGuid(guid);

                if (ou == null) return null;

                if (!ou.SchemaClassName.Equals(this.OUKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    ou.Close();
                    ou.Dispose();
                    return null;
                }
                else return ou;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public DirectoryEntry GetDirectoryEntryByDEPath(string dePath)
        {
            try
            {
                return new DirectoryEntry(this.GetDEConnectionString(dePath), this.UserName, _realPassword);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public DirectoryEntry GetOUByPath(string outPath)
        {
            try
            {
                DirectoryEntry ou = this.GetDirectoryEntryByDEPath(outPath);
                if (ou == null) return null;

                if (!ou.SchemaClassName.Equals(this.OUKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    ou.Close();
                    ou.Dispose();
                    return null;
                }
                else return ou;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public bool GetAllOUs()
        {
            //LDAP://OU=CATTT,DC=ais,DC=vn
            // connect to "RootDSE" to find default naming context
            DirectoryEntry rootDSE = new DirectoryEntry("LDAP://OU=CATTT,DC=ais,DC=vn", UserName, _realPassword);

            string defaultContext = rootDSE.Properties["defaultNamingContext"][0].ToString();

            // bind to default naming context - if you *know* where you want to bind to - 
            // you can just use that information right away
            DirectoryEntry domainRoot = new DirectoryEntry("LDAP://" + defaultContext);

            // set up directory searcher based on default naming context entry
            DirectorySearcher ouSearcher = new DirectorySearcher(domainRoot);

            // SearchScope: OneLevel = only immediate subordinates (top-level OUs); 
            // subtree = all OU's in the whole domain (can take **LONG** time!)
            ouSearcher.SearchScope = SearchScope.OneLevel;
            // ouSearcher.SearchScope = SearchScope.Subtree;

            // define properties to load - here I just get the "OU" attribute, the name of the OU
            ouSearcher.PropertiesToLoad.Add("ou");

            // define filter - only select organizational units
            ouSearcher.Filter = "(objectCategory=organizationalUnit)";

            // do search and iterate over results
            foreach (SearchResult deResult in ouSearcher.FindAll())
            {
                string ouName = deResult.Properties["ou"][0].ToString();
            }

            return true;
        }

        #endregion Get

        #region Add
        public DirectoryEntry AddNewOU(Guid parentOUGuid, string ouName, string description)
        {
            try
            {
                using (DirectoryEntry parentOU = this.GetOUByGuid(parentOUGuid))
                {
                    if (parentOU == null)
                    {
                        return null;
                    }

                    DirectoryEntry newOU = parentOU.Children.Add(this.GetOUName(ouName), this.OUKeyword);
                    if (!string.IsNullOrEmpty(description)) newOU.Properties["Description"].Value = description;

                    newOU.CommitChanges();

                    parentOU.Close();
                    parentOU.Dispose();

                    return newOU;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public DirectoryEntry AddNewOU(string parentOUPath, string ouName, string description)
        {
            string _parentOUPath = this.GetOUConnectionString(parentOUPath);
            if (string.IsNullOrEmpty(_parentOUPath)) return null;

            try
            {
                using (DirectoryEntry parentOU = new DirectoryEntry(_parentOUPath, this.UserName, _realPassword))
                {
                    if (parentOU == null) return null;

                    DirectoryEntry newOU = parentOU.Children.Add(this.GetOUName(ouName), this.OUKeyword);
                    if (!string.IsNullOrEmpty(description)) newOU.Properties["Description"].Value = description;

                    newOU.CommitChanges();

                    parentOU.Close();
                    parentOU.Dispose();

                    return newOU;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public DirectoryEntry AddNewOU(string ouName)
        {
            return this.AddNewOU(string.Empty, ouName, string.Empty);
        }

        public DirectoryEntry AddNewOU(string ouName, string description)
        {
            return this.AddNewOU(string.Empty, ouName, description);
        }
        #endregion Add

        #region Add or update
        public DirectoryEntry AddOrUpdateOUByPath(Guid ouGuid, Guid parentOUGuid, string ouName, string description)
        {
            try
            {
                using (DirectoryEntry ou = this.GetOUByGuid(ouGuid))
                {
                    if (ou == null)
                    {
                        DirectoryEntry newOU = this.AddNewOU(parentOUGuid, ouName, description);
                        if (newOU == null) return null;
                        else return newOU;
                    }
                    else
                    {
                        DirectoryEntry parentOU = this.GetOUByGuid(parentOUGuid);
                        if (parentOU == null)
                        {
                            ou.Close();
                            ou.Dispose();
                            return null;
                        }

                        if (ou.Parent.Path.Equals(parentOU.Path, StringComparison.OrdinalIgnoreCase))
                        {
                            ou.Rename(this.GetOUName(ouName));
                            ou.Properties["Description"].Value = description;
                            ou.CommitChanges();
                            parentOU.Close();
                            parentOU.Dispose();
                            return ou;
                        }
                        else
                        {
                            ou.MoveTo(parentOU, this.GetOUName(ouName));
                            ou.Properties["Description"].Value = description;
                            ou.CommitChanges();
                            parentOU.Close();
                            parentOU.Dispose();
                            return ou;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public DirectoryEntry AddOrUpdateOUByPath(string ouPath, string parentOUPath, string ouName, string description)
        {
            try
            {
                using (DirectoryEntry ou = this.GetOUByPath(ouPath))
                {
                    if (ou == null)
                    {
                        DirectoryEntry newOU = this.AddNewOU(parentOUPath, ouName, description);
                        if (newOU == null) return null;
                        else return newOU;
                    }
                    else
                    {
                        if (ou.Parent.Path.Equals(this.GetOUConnectionString(parentOUPath), StringComparison.OrdinalIgnoreCase))
                        {
                            ou.Rename(this.GetOUName(ouName));
                            ou.Properties["Description"].Value = description;
                            ou.CommitChanges();
                            return ou;
                        }
                        else
                        {
                            DirectoryEntry parentOU = this.GetOUByPath(parentOUPath);
                            if (parentOU == null)
                            {
                                ou.Close();
                                ou.Dispose();
                                return null;
                            }
                            else
                            {
                                ou.MoveTo(parentOU, this.GetOUName(ouName));
                                ou.Properties["Description"].Value = description;
                                ou.CommitChanges();
                                parentOU.Close();
                                parentOU.Dispose();
                                return ou;
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }
        #endregion Add or update

        #region Remove
        public bool RemoveOUByGuid(Guid ouGuid, bool cascadeDeleteTree)
        {
            try
            {
                DirectoryEntry ou = this.GetOUByGuid(ouGuid);
                if (ou == null) return false;

                if (cascadeDeleteTree)
                {
                    ou.DeleteTree();
                    return true;
                }
                else
                {
                    if (ou.Children == null)
                    {
                        ou.DeleteTree();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return false;
            }
        }

        public bool RemoveOUByPath(string ouPath, bool cascadeDeleteTree)
        {
            try
            {
                DirectoryEntry ou = this.GetOUByPath(ouPath);
                if (ou == null) return false;

                if (cascadeDeleteTree)
                {
                    ou.DeleteTree();
                    return true;
                }
                else
                {
                    if (ou.Children == null)
                    {
                        ou.DeleteTree();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return false;
            }
        }
        #endregion Remove

        #endregion OU Methods

        #region Helper Methods

        /// <summary>
        /// Gets the base principal context
        /// </summary>
        /// <returns>Returns the PrincipalContext object</returns>
        public PrincipalContext GetPrincipalContext()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.DomainName))
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.DomainName,
                                                                             this.DefaultOU,
                                                                             ContextOptions.Negotiate |
                                                                             ContextOptions.Sealing |
                                                                             ContextOptions.Signing, this.UserName,
                                                                             _realPassword);

                    return principalContext;
                }
                else
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.sDomainName,
                                                                           this.sDefaultOU,
                                                                           ContextOptions.Negotiate |
                                                                           ContextOptions.Sealing |
                                                                           ContextOptions.Signing, this.sUserName,
                                                                           _sRealPassword);

                    return principalContext;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public PrincipalContext GetPrincipalContextCheck()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.DomainName))
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.DomainName, this.RootOU, this.UserName, _realPassword);
                    return principalContext;
                }
                else
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.sDomainName, DomainConnection(this.sDomainName), this.sUserName, _sRealPassword);
                    return principalContext;
                }

            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public PrincipalContext GetSubPrincipalContextCheck()
        {
            try
            {
                PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.SubDomainName, DomainConnection(this.SubDomainName), this.SubUserName, _subRealPassword);
                return principalContext;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        /// <summary>
        /// Gets the principal context on specified OU
        /// </summary>
        /// <param name="ouName">The OU you want your Principal Context to run on</param>
        /// <returns>Returns the PrincipalContext object</returns>
        public PrincipalContext GetPrincipalContext(string ouName)
        {
            try
            {
                if (!string.IsNullOrEmpty(this.DomainName))
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.DomainName,
                                                                             this.GetOUPath(ouName),
                                                                             ContextOptions.Negotiate |
                                                                             ContextOptions.Sealing |
                                                                             ContextOptions.Signing, this.UserName,
                                                                             _realPassword);
                    return principalContext;
                }
                else
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.sDomainName,
                                                                             this.GetOUPath(ouName),
                                                                             ContextOptions.Negotiate |
                                                                             ContextOptions.Sealing |
                                                                             ContextOptions.Signing, this.sUserName,
                                                                             _sRealPassword);
                    return principalContext;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        /// <summary>
        /// Gets the sub context on specified OU
        /// </summary>
        /// <param name="ouName">The OU you want your Principal Context to run on</param>
        /// <returns>Returns the PrincipalContext object</returns>
        public PrincipalContext GetSubPrincipalContext(string ouName)
        {
            try
            {
                PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.SubDomainName, this.GetSubOUPath(ouName), ContextOptions.Negotiate | ContextOptions.Sealing | ContextOptions.Signing, this.SubUserName, _subRealPassword);
                return principalContext;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        public PrincipalContext GetPrincipalContextWindow()
        {
            try
            {
                if (!string.IsNullOrEmpty(this.DomainName))
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.DomainName,
                                                                             this.UserName, _realPassword);
                    return principalContext;
                }
                else
                {
                    PrincipalContext principalContext = new PrincipalContext(ContextType.Domain, this.sDomainName,
                                                                            this.sUserName, _sRealPassword);
                    return principalContext;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "");
                return null;
            }
        }

        #endregion

        #region Search Groups
        public PrincipalSearchResult<Principal> SearchGroups(GroupPrincipal parGroupPrincipal)
        {
            PrincipalSearcher insPrincipalSearcher = new PrincipalSearcher();
            insPrincipalSearcher.QueryFilter = parGroupPrincipal;
            PrincipalSearchResult<Principal> results = insPrincipalSearcher.FindAll();
            return results;
        }

        /// <summary>
        /// Get All Group
        /// </summary>
        /// <returns></returns>
        public PrincipalSearchResult<Principal> GetAllGroups()
        {
            PrincipalContext principalContext = GetPrincipalContext();
            GroupPrincipal insGroupPrincipal = new GroupPrincipal(principalContext);
            insGroupPrincipal.Name = "*";
            return SearchGroups(insGroupPrincipal);
        }
        #endregion

        #region Search Users
        public PrincipalSearchResult<Principal> SearchUsers(UserPrincipal parUserPrincipal)
        {

            PrincipalSearcher insPrincipalSearcher = new PrincipalSearcher();
            insPrincipalSearcher.QueryFilter = parUserPrincipal;
            return insPrincipalSearcher.FindAll();
        }

        public PrincipalSearchResult<Principal> GetAllUsers()
        {
            try
            {
                PrincipalContext principalContext = GetPrincipalContext();
                UserPrincipal insUserPrincipal = new UserPrincipal(principalContext);
                insUserPrincipal.Name = "*";
                var allU = SearchUsers(insUserPrincipal);

                return allU;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex,"");
                return null;
            }
        }

        #endregion

        #region Utils
        string DomainConnection(string input)
        {
            string strTemp = input.ToUpper();
            StringBuilder strBuilder = new StringBuilder();

            if (strTemp.Contains("."))
            {
                string[] arrTemp = strTemp.Split('.');

                foreach (string node in arrTemp)
                {
                    strBuilder.AppendFormat(",DC={0}", node);
                }

                return strBuilder.Remove(0, 1).ToString();
            }
            else
                return string.Empty;
        }

        string OUConnection(string input, bool lastNode)
        {
            string strTemp = input.ToUpper();
            StringBuilder strBuilder = new StringBuilder();

            if (strTemp.Contains("."))
            {
                string[] arrTemp = strTemp.Split('.');

                int iTop = arrTemp.Length - 1;

                if (!lastNode)
                {
                    if (arrTemp.Length >= 1)
                        iTop = iTop - 1;
                }

                for (int i = iTop; i >= 0; --i)
                {
                    strBuilder.AppendFormat(",OU={0}", arrTemp[i]);
                }

                return strBuilder.Remove(0, 1).ToString();
            }
            else
                return string.Empty;
        }
        #endregion

        public void ActiveDirectoryOrganizationalUnitRepository(string connectionString, string userName, string password)
        {
            //if (DomainExists(connectionString))
            {
                var baseDirectory = new DirectoryEntry(connectionString);
                baseDirectory.Username = userName;
                baseDirectory.Password = password;

                DirectorySearcher searcher = new DirectorySearcher();
                searcher.SearchRoot = baseDirectory;
                searcher.Filter = "(objectCategory=organizationalUnit)";
                searcher.SearchScope = SearchScope.Subtree;

                var ouResults = searcher.FindAll();

                StringBuilder strBuilder = new StringBuilder();
                foreach (SearchResult ou in ouResults)
                {
                    ResultPropertyCollection myResultPropColl;
                    myResultPropColl = ou.Properties;
                    Console.WriteLine("The properties of the " +
                            "'mySearchResult' are :");

                    foreach (string myKey in myResultPropColl.PropertyNames)
                    {
                        string tab = "    ";
                        strBuilder.AppendFormat("{0}:{1}", myKey, Environment.NewLine);
                        foreach (Object myCollection in myResultPropColl[myKey])
                        {
                            strBuilder.AppendFormat("{0} - {1} {2}", tab, myCollection, Environment.NewLine);
                        }
                    }

                }
            }
        }

    }

    public class ADTree
    {
        DirectoryEntry rootOU = null;
        string rootDN = string.Empty;
        List<ADTree> childOUs = new List<ADTree>();

        public DirectoryEntry RootOU
        {
            get { return rootOU; }
            set { rootOU = value; }
        }

        public string RootDN
        {
            get { return rootDN; }
            set { rootDN = value; }
        }

        public List<ADTree> ChildOUs
        {
            get { return childOUs; }
            set { childOUs = value; }
        }

        public ADTree(string dn)
        {
            RootOU = new DirectoryEntry(dn);
            RootDN = dn;
            BuildADTree().Wait();
        }

        public ADTree(DirectoryEntry root)
        {
            RootOU = root;
            RootDN = root.Path;
            BuildADTree().Wait();
        }

        private Task BuildADTree()
        {
            return Task.Factory.StartNew(() =>
            {
                object locker = new object();
                Parallel.ForEach(RootOU.Children.Cast<DirectoryEntry>().AsEnumerable(), child =>
                {
                    if (child.SchemaClassName.Equals("organizationalUnit"))
                    {
                        ADTree ChildTree = new ADTree(child);
                        lock (locker)
                        {
                            ChildOUs.Add(ChildTree);
                        }
                    }
                });
            });
        }
    }
}
