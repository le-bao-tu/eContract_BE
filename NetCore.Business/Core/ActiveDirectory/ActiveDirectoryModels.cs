namespace NetCore.Business
{
    public class ADClient
    {
        public string Name { get; set; }
        public bool ADSysnAcount { get; set; }
        public bool ADAuthenEnable { get; set; }
        public string ADServer { get; set; }
        public string ADLdapPath { get; set; }
        public string Ou { get; set; }
        public string Server { get; set; }
        public string Ldap { get; set; }
        public string Status { get; set; }
        public bool IsWork { get; set; }
    }

    public class BaseUserClient
    {
        public string UserName { get; set; }
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
    }
}
