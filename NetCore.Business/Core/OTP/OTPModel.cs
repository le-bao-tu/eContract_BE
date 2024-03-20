using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class ValidateOTPModel
    {
        public string UserName { get; set; }
        public string OTP { get; set; }
    }

    #region OTP Model
    public class OTPRequestModel
    {
        public string AppRequest { get; set; }
        public string UserName { get; set; }
        public int Step { get; set; } = 300;
        public int TOTPSize { get; set; } = 6;
        public string Description { get; set; }
    }

    public class OTPResponseModel
    {
        public string UserName { get; set; }
        public string OTP { get; set; }
        public int RemainingSeconds { get; set; }
        public DateTime ExpireAtUTCDate { get; set; }
    }

    public class OTPValidateModel
    {
        public string AppRequest { get; set; }
        public string UserName { get; set; }
        public int Step { get; set; } = 300;
        public string OTP { get; set; }
        public string Description { get; set; }
    }

    #endregion

    public class TokenResponseModel
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("scope")]
        public string Scope { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("creadted_date")]
        public DateTime? CreadtedDate { get; set; }
    }

    public class RetrieveSecretKeyResponseModel
    {
        [JsonPropertyName("retrieveSecretKeyResponse")]
        public RetrieveSecretKeyResponse RetrieveSecretKeyResponse { get; set; }
    }
    public class RetrieveSecretKeyResponse
    {
        [JsonPropertyName("return")]
        public string Return { get; set; }
    }

    public class ValidateOTPResponseModel
    {
        [JsonPropertyName("validateTOTPResponse")]
        public ValilateOTPResponse ValidateTOTPResponse { get; set; }
    }
    public class ValilateOTPResponse
    {
        [JsonPropertyName("return")]
        public bool Return { get; set; }
    }


    #region OTP From Service
    public class HOTPRequestModel
    {
        public string AppRequest { get; set; }
        public string UserName { get; set; }
        public string ObjectId { get; set; }
        public int Step { get; set; } = 300;
        public int HOTPSize { get; set; } = 6;
        public string Description { get; set; }
    }

    public class HOTPResponseDetailModel
    {
        [JsonPropertyName("otp")]
        public string OTP { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class HOTPValidateResponseModel
    {
        [JsonPropertyName("is_success")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class HOTPValidateModel
    {
        public string AppRequest { get; set; }
        public string ObjectId { get; set; }
        public string UserName { get; set; }
        public int Step { get; set; } = 300;
        public string OTP { get; set; }
        public string Description { get; set; }
    }

    public class HOTPResponseModel
    {
        [JsonPropertyName("data")]
        public HOTPResponseDetailModel Data { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; set; }
    }

    #endregion
}
