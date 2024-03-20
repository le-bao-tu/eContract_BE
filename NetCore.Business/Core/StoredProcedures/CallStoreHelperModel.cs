using System.Data;

namespace NetCore.Business
{
    public class CallStoreHelperModel
    {
        public DataTable Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
