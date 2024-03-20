using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ISignServiceHandler
    {
        Task<ResponseObject<SignFileModel>> SignBySigningBox(DataInputSignPDF request, Guid? userId, int? signType);
        Task<ResponseObject<SignFileModel>> ElectronicSigning(DataInputSignPDF request);
    }
}
