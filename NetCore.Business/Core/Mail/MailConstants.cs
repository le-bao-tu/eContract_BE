using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class MailConstants
    {
        public class MailConfigParameter
        {
            public const string MAIL_CONFIG_ENABLED = "email:enabled";
            public const string MAIL_CONFIG_PORT = "email:port";
            public const string MAIL_CONFIG_FROM = "email:from";
            public const string MAIL_CONFIG_SMTP = "email:smtp";
            public const string MAIL_CONFIG_SSL = "email:ssl";
            public const string MAIL_CONFIG_PASSWORD = "email:password";
            public const string MAIL_CONFIG_USER = "email:user";
            public const string MAIL_CONFIG_SEND_TYPE = "email:sendtype";
        }
        public class MailConfigSendType
        {
            public const string ASYNC = "async";
            public const string SYNC = "sync";
            public const string BOTH = "both";
        }

        public const char EMAIL_SPLIT_CHARACTER = ';';
    }
}
