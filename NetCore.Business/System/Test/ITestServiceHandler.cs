using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public interface ITestServiceHandler
    {
        Task<Response> TestPostgreeSQL(SystemLogModel systemLog);
        Task<Response> TestMongoDB(SystemLogModel systemLog);
        Task<Response> TestMinIO(SystemLogModel systemLog);
        Task<Response> TestServiceHashAttach(SystemLogModel systemLog);
        Task<Response> TestGatewaySMS(TestServiceGatewaySMS gatewaySMS, SystemLogModel systemLog);
        Task<Response> TestGatewayNotify(TestServiceGatewayNotify gatewayNotify, SystemLogModel systemLog);
        Task<Response> TestServiceConvertPdfToPng(SystemLogModel systemLog);
        Task<Response> TestServiceConvertPDFA(SystemLogModel systemLog);
        Task<Response> TestServiceCIAM(SystemLogModel systemLog);
        Task<Response> TestServiceOTP(SystemLogModel systemLog);
        Task<Response> TestServiceADSS(TestADSSModel model, SystemLogModel systemLog);
    }
}
