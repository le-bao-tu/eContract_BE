using NetCore.Shared;
using System;

namespace NetCore.Business
{
    public interface IADSSCoreHandler
    {
        public Response SignADSSExistingBlankSignatureField(ADSSCoreModelRequest model, SystemLogModel systemLog);

        public Response SignADSSWithExistingSignatureField(ADSSCoreModelRequest model, SystemLogModel systemLog);

        public Response SignADSSWithNoExistingBlankSignatureField(ADSSCoreModelRequest model, SystemLogModel systemLog);
    }
}
