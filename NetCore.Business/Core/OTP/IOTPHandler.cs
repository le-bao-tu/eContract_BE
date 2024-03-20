using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface IOTPHandler
    {
        /// <summary>
        /// Generate OTP
        /// </summary>
        /// <param name="userName">Tài khoản</param>
        /// <returns></returns>
        Task<string> GenerateOTP(string userName);

        /// <summary>
        /// Validate OTP
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<bool> ValidateOTP(ValidateOTPModel model);

        Task<OTPResponseModel> GenerateOTP(OTPRequestModel model, SystemLogModel systemLog);

        Task<bool> ValidateOTP(OTPValidateModel model, SystemLogModel systemLog);

        Task<HOTPResponseDetailModel> GenerateHOTPFromService(HOTPRequestModel model, SystemLogModel systemLog);
        Task<HOTPValidateResponseModel> ValidateHOTPFromService(HOTPValidateModel model, SystemLogModel systemLog);
    }
}
