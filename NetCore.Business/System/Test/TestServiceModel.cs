using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class TestServiceGatewaySMS
    {
        public string PhoneNumber { get; set; }
        public string Content { get; set; }
    }

    public class TestServiceGatewayNotify
    {
        public string PhoneNumber { get; set; }
        public string Content { get; set; }
        public string Title { get; set; }
    }

    public class TestADSSModel
    {
        public string ProfileId { get; set; }
        public string Alias { get; set; }
    }
}
