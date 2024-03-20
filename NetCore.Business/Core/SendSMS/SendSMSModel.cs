using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NetCore.Business
{
    public class SendSMSModel
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }

    public class SendSMSResponseModel
    {
        /// <summary>
        /// Số điện thoại nhận tin nhắn
        /// </summary>
        [JsonPropertyName("dest_addr")]
        public string DestAddr { get; set; }
        /// <summary>
        /// Trạng thái gửi tin nhắn
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; }
        /// <summary>
        /// Id tin nhắn
        /// </summary>
        [JsonPropertyName("msgid")]
        public int? MsgId { get; set; }
        /// <summary>
        /// Mô tả trạng thái gửi tin nhắn
        /// </summary>
        [JsonPropertyName("decription")]
        public string Decription { get; set; }
    }

    #region GHTK
    public class GHTKSMSSendModel
    {
        [JsonPropertyName("sender")]
        public string Sender { get; set; }

        [JsonPropertyName("receiver")]
        public string Receiver { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }
    }
    #endregion

    #region SNF
    public class SNFSMSSendModel
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = "2532931653958908159";

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("client_req_id")]
        public string ClientReqId { get; set; } = "2532931653958908159";

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("smsFailover")]
        public SNFSMSFailOver SMSFailover { get; set; }
    }

    public class SNFSMSFailOver
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = "09x";

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("unicode")]
        public int Unicode { get; set; } = 1;
    }

    #endregion
}
