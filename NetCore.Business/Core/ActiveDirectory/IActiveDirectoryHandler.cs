

using NetCore.Data;
using NetCore.Shared;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface IActiveDirectoryHandler
    {

        #region Validate
        Response ValidateCredentials(string userName, string password);
        Response IsUserExpired(string userName);
        Response IsUserExisiting(string userName);
        Response IsAccountLocked(string userName);
        Response IsGroupExisiting(string groupName);
        #endregion

        #region Methods
        Response GetUserByName(string name);
        Response DeleteUser(string name);
        Response SetUserPassword(string userName, string newPassword);
        Response ChangePassword(string userName, string oldPassword, string newPassword);
        Response ResetUserPasswordDefault(string userName, string defaultPassword);
        Response EnableUserAccount(string userName);
        Response DisableUserAccount(string userName);
        Response ExpireUserPassword(string userName);
        Response SetPasswordNeverExpire(string userName, bool isExpire);
        Response UnlockUserAccount(string userName);
        #endregion


        Response GetADInfomation();
        Response ChangeStatus(ADClient adClient);

        //bool GetAllOUs();

        //Response SysnAccFromAd(string p);
        //Response SysnAccFromAdNew(string p, UsersPostModel model);
    }
}
