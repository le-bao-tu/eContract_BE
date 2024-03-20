using NetCore.Data;
using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCore.Business
{
    /// <summary>
    /// Interface quản lý người dùng
    /// </summary>
    public interface IUserHandler
    {
        /// <summary>
        /// lấy danh sách thiết bị người dùng
        /// </summary>
        /// <param name="model">lấy danh sách thiết bị người dùng </param>
        /// <returns>Id người dùng</returns>
        Task<Response> GetListDeviceByUser(Guid userId, SystemLogModel systemLog);
        
        /// <summary>
        /// Thêm mới người dùng
        /// </summary>
        /// <param name="model">Model thêm mới người dùng</param>
        /// <returns>Id người dùng</returns>
        Task<Response> Create(UserCreateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật người dùng
        /// </summary>
        /// <param name="model">Model cập nhật người dùng</param>
        /// <returns>Id người dùng</returns>
        Task<Response> Update(UserUpdateModel model, SystemLogModel systemLog);
        /// <summary>
        /// Cập nhật thông tin người dùng 
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Id người dùng</returns>
        Task<Response> UpdateUser(UserProfileUpdateModel model, SystemLogModel systemLog);
        /// <summary>
        /// Thêm mới hoặc cập nhật người dùng (dùng cho service bên thứ 3 gọi)
        /// </summary>
        /// <param name="model">Model cập nhật người dùng</param>
        /// <returns>Id người dùng</returns>
        Task<Response> CreateOrUpdate(UpdateOrCreateUserModel model, SystemLogModel systemLog);
        Task<Response> GetByUserConnectId(string connectId, Guid orgId, SystemLogModel systemLog);
        Task<Response> AddDevice(DeviceAddRequestModel model, SystemLogModel systemLog);
        Task<Response> AddOrUpdateFirebaseToken(FirebaseRequestModel model, SystemLogModel systemLog);
        Task<Response> AddOrUpdateFirebaseToken3rd(FirebaseRequestModel3rd model, SystemLogModel systemLog);
        Task<Response> DeleteFirebaseToken(FirebaseRequestModel model, SystemLogModel systemLog);
        Task<UserModel> GetUserFromOrganizationAndUserConnect(Guid orgId, string userConnectId);
        Task<UserModel> GetUserFromCache(Guid id);

        /// <summary>
        /// Lấy thông tin đơn vị, người dùng từ OrganirationId, UserConnectId
        /// </summary>
        /// <param name="model">Thông tin request</param>
        /// <returns>Danh sách thông tin chi tiết người dùng</returns> 
        Task<Response> GetListUserByListConnectId(OrgAndUserConnectRequestModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật mật khẩu
        /// </summary>
        /// <param name="model">Model cập nhật mật khẩu</param>
        /// <returns>Id người dùng</returns>
        Task<Response> UpdatePassword(UserUpdatePasswordModel model, SystemLogModel systemLog);

        /// <summary>
        /// Khóa người dùng
        /// </summary>
        /// <param name="model">Thông tin khóa tài khoản</param>
        /// <returns></returns>
        Task<Response> LockOrUnlock(UserLockModel model, SystemLogModel systemLog);

        /// <summary>
        /// Xóa người dùng
        /// </summary>
        /// <param name="listId">Danh sách Id người dùng</param>
        /// <returns>Danh sách kết quả xóa</returns>
        Task<Response> Delete(List<Guid> listId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách người dùng theo điều kiện lọc
        /// </summary>
        /// <param name="filter">Model điều kiện lọc</param>
        /// <returns>Danh sách người dùng</returns>
        Task<Response> Filter(UserQueryFilter filter, SystemLogModel systemLog);

        /// <summary>
        /// Lấy người dùng theo Id
        /// </summary>
        /// <param name="id">Id người dùng</param>
        /// <returns>Thông tin người dùng</returns>
        Task<Response> GetById(Guid id, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách người dùng cho combobox
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách người dùng cho combobox</returns>
        Task<Response> GetListCombobox(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null);

        /// <summary>
        /// Lấy danh sách người dùng cho combobox by root Org
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách người dùng cho combobox</returns>
        Task<Response> GetListComboboxByRootOrg(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null);

        /// <summary>
        /// Lấy danh sách người dùng cho combobox filter theo người dùng nội bộ hoặc khác hàng
        /// </summary>
        /// <param name="count">Số bản ghi tối đa</param>
        /// <param name="textSearch">Từ khóa tìm kiếm</param>
        /// <returns>Danh sách người dùng cho combobox</returns>
        Task<Response> GetListComboboxFilterInternalOrCustomer(SystemLogModel systemLog, int count = 0, string textSearch = "", Guid? orgId = null, bool isInternalUser = true);

        /// <summary>
        /// Lấy danh sách người dùng từ cache
        /// </summary>
        /// <returns></returns>
        Task<List<UserSelectItemModel>> GetListUserFromCache();

        /// <summary>
        /// Xác thực người dùng
        /// </summary>
        /// <param name="userName">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns></returns>
        Task<Response> Authentication(string userName, string password);

        /// <summary>
        /// Lấy thông tin để tạo CTS
        /// </summary>
        /// <param name="id">Id người dùng, đơn vị</param>
        /// <param name="type">1: Người dùng, 2: Người dùng trong tổ chức, 3: Tổ chức</param>
        /// <returns></returns>
        Task<Response> GetUserCertificateCreateInfo(Guid id, int type, SystemLogModel systemLog);

        /// <summary>
        /// Kiểm tra mật khẩu người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog">Log</param>
        /// <returns></returns>
        Task<Response> ValidatePassword(ChangePasswordModel model, SystemLogModel systemLog);

        /// <summary>
        /// Kiểm tra mật khẩu người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog">Log</param>
        /// <returns></returns>
        Task<Response> SendOTPToChangePassword(Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Thay đổi mật khẩu người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog">Log</param>
        /// <returns></returns>
        Task<Response> ChangePassword(ChangePasswordModel model, SystemLogModel systemLog);

        /// <summary>
        /// Thay đổi mã PIN của người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> ChangeUserPIN(ChangeUserPINModel model, SystemLogModel systemLog);

        /// <summary>
        /// Đăng ký hình thức ký: ký điện tử, ký số
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> UpdateEFormConfig(UpdateEFormConfigModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lấy thông tin người dùng(đang sử dụng ký số hay ký điện tử)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetUserEFormConfig(Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách CTS
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetUserCertificate(Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Lấy danh sách CTS cho đơn vị thứ 3
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> GetUserCertificateFrom3rd(string connectId, Guid orgId, SystemLogModel systemLog);

        /// <summary>
        /// Làm mới những CTS đã hết hạn cho người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> AddCertificate(AddCertificateModel model, SystemLogModel systemLog);

        /// <summary>
        /// Cập nhật user PIN của người dùng
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> UpdateUserPIN(UserUpdatePIN model, SystemLogModel systemLog);


        #region Phân quyền
        Task<Response> GetUserPermission(Guid userId, SystemLogModel systemLog);
        Task<Response> GetUserRole(GetUserRoleModel model, SystemLogModel systemLog);
        Task<Response> UpdateUserRole(UpdateUserRoleModel model, SystemLogModel systemLog);
        Task<List<Guid>> GetUserRoleFromCacheAsync(Guid id);

        /// <summary>
        /// Lấy danh sách User Id theo Role Id
        /// </summary>
        /// <param name="model">Gồm Role Id</param>
        /// <param name="systemLog"></param>
        /// <returns>List User Id</returns>
        Task<Response> GetUserRoleByRoleId(GetUserRoleByRoleModel model, SystemLogModel systemLog);

        /// <summary>
        /// Lưu danh sách user role
        /// </summary>
        /// <param name="model">Role id và list user id</param>
        /// <param name="systemLog"></param>
        /// <returns>True thành công, false thất bại</returns>
        Task<Response> SaveListUserRole(SaveListUserRoleModel model, SystemLogModel systemLog);
        #endregion

        /// <summary>
        /// Gửi thông báo OTP xác thực người dùng
        /// </summary>
        /// <param name="userId">Id người dùng</param>
        /// <param name="systemLog">Model log</param>
        /// <returns></returns>
        Task<Response> SendOTPAuthToUser(Guid userId, SystemLogModel systemLog);

        /// <summary>
        /// Thêm mới người dùng nếu chưa tồn tạo trên hệ thống
        /// </summary>
        /// <param name="model">UserCreateModel</param>
        /// <returns>Trả về thông tin cơ bản của người dùng</returns>
        Task<UserBaseModel> CreateUserIfNotExists(UserCreateModel model);

        /// <summary>
        /// Khởi tạo dữ liệu quản trị cho đơn vị
        /// </summary>
        /// <param name="model"></param>
        /// <param name="systemLog"></param>
        /// <returns></returns>
        Task<Response> InitUserAdminOrg(InitUserAdminOrgModel model, SystemLogModel systemLog);

        Task<SICMUserCreateResponseModel> CreateSCIMUser(User entity, SystemLogModel systemLog);

        #region EKYC
        Task<Response> RegisterFrontCard(RegisterFrontCardModel model, Guid userId, SystemLogModel systemLog);
        Task<Response> RegisterBackCard(RegisterBackCardModel model, Guid userId, SystemLogModel systemLog);
        Task<Response> RegisterFaceVideo_Liveness(RegisterFaceVideoLivenessModel model, Guid userId, SystemLogModel systemLog);
        Task<Response> UpdateEKYC_UserInfo(UpdateEKYCUserInfoModel model, SystemLogModel systemLog);
        #endregion
    }
}
