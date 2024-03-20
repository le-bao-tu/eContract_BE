using Com.Ascertia.ADSS.Client.API.Signing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using NetCore.Data;
using NetCore.DataLog;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class TestServiceHandler : ITestServiceHandler
    {
        private readonly DataContext _dataContext;
        private readonly IMongoCollection<SystemLog> _logs;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IUserHandler _userHandler;
        private readonly IOTPHandler _otpService;

        private readonly string _signHashUrl = Utils.GetConfig("SignHash:url");
        private readonly string API_ELECTRONIC_SIGN_FILE = "api/v1/sign/param-minio-electronic/pdf";
        private readonly string bucketTest = "bn-test";
        private readonly string fileNameTest = "test_file.pdf";
        private readonly string fileWordTest= "test_file_word.docx";
        private readonly string notiGateWayUrl = Utils.GetConfig("NotificationGateway:Uri");
        private readonly string pdfServiceUrl = Utils.GetConfig("PDFToImageService:uri");
        private readonly string signAdss = $"{Utils.GetConfig("Adss:url")}adss/signing/dss";

        public TestServiceHandler(
            DataContext dataContext,
            IMongoDBDatabaseSettings settings,
            IWebHostEnvironment environment,
            IUserHandler userHandler,
            IOTPHandler otpService)
        {
            _dataContext = dataContext;
            _hostEnvironment = environment;
            _userHandler = userHandler;
            _otpService = otpService;

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _logs = database.GetCollection<SystemLog>(MongoCollections.SysLog);
        }

        public async Task<Shared.Response> TestPostgreeSQL(SystemLogModel systemLog)
        {
            try
            {
                var testPostgree = await _dataContext.Country.ToListAsync();

                return new ResponseObject<string>("Test Postgree SQL Success");
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Postgree SQL Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestMongoDB(SystemLogModel systemLog)
        {          
            try
            {
                var builder = Builders<SystemLog>.Filter.And(
                    Builders<SystemLog>.Filter.Where(p => p.ActionName.ToLower().Contains(LogConstants.ACTION_WSO2_LOGIN.ToLower()))
                );

                var testMongoDb = await _logs.Find(builder).FirstOrDefaultAsync();

                return new ResponseObject<string>("Test Mongo DB Success");
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Mongo DB Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestMinIO(SystemLogModel systemLog)
        {
            try
            {
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", fileNameTest);

                var minIO = new MinIOService();
                var testMinIO = await minIO.UploadObjectAsync(bucketTest, fileNameTest, File.OpenRead(documentPath), false);

                return new ResponseObject<string>("MinIO Success");
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - MinIO Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestServiceHashAttach(SystemLogModel systemLog)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var requestList = new List<SignHashModel>();
                    var signHashModel = new SignHashModel()
                    {
                        Id = Guid.NewGuid(),
                        BucketName = bucketTest,
                        ObjectName = fileNameTest,
                        BucketNameSigned = bucketTest,
                        ObjectNameSigned = fileNameTest,
                    };
                    requestList.Add(signHashModel);

                    var electronicSignFileRequest = new ElectronicSignFileRequestModel()
                    {
                        RequestList = requestList
                    };

                    string uri = _signHashUrl + API_ELECTRONIC_SIGN_FILE;

                    StringContent content = new StringContent(JsonSerializer.Serialize(electronicSignFileRequest), Encoding.UTF8, "application/json");

                    var res = new HttpResponseMessage();
                    res = await client.PostAsync(uri, content);

                    if (!res.IsSuccessStatusCode)
                    {
                        return new ResponseObject<string>($"{systemLog.TraceId} - Service Hash Attach Error: " + JsonSerializer.Serialize(res));
                    }

                    return new ResponseObject<string>("Service Hash Attach Success");
                }
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Service Hash Attach Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestGatewaySMS(TestServiceGatewaySMS gatewaySMS, SystemLogModel systemLog)
        {
            try
            {
                var model = new NotificationConfigModel()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    IsSendSMS = true,
                    ListPhoneNumber = new List<string>() { gatewaySMS.PhoneNumber },
                    SmsContent = gatewaySMS.Content
                };

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-test", content);                   

                        return new ResponseObject<string>("Gateway SMS: " + JsonSerializer.Serialize(res));
                    }
                }                
            }
            catch(Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Gateway SMS Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestGatewayNotify(TestServiceGatewayNotify gatewayNotify, SystemLogModel systemLog)
        {
            try
            {
                var model = new NotificationConfigModel()
                {
                    TraceId = Guid.NewGuid().ToString(),
                    IsSendNotification = true,
                    ListPhoneNumber = new List<string>() { gatewayNotify.PhoneNumber },
                    NotificationTitle = gatewayNotify.Title,
                    NotificationContent = gatewayNotify.Content,
                };

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(notiGateWayUrl + "api/v1/notification/push-notification-test", content);

                        return new ResponseObject<string>("Gateway Notify: " + JsonSerializer.Serialize(res));
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Gateway Notify Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestServiceConvertPdfToPng(SystemLogModel systemLog)
        {
            try
            {
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", fileNameTest);
                var model = new PDFConvertPNGServiceModel() { };

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();

                        byte[] fileBytes = File.ReadAllBytes(documentPath);

                        HttpContent bytesContent = new ByteArrayContent(fileBytes);
                        form.Add(bytesContent, "file", "contract.pdf");    

                        HttpResponseMessage res = await client.PostAsync(pdfServiceUrl + "api/v1/core/pdfconvert/pdf-to-png-base64", form);

                        return new ResponseObject<string>("Service Convert Pdf: " + JsonSerializer.Serialize(res));
                    }
                }
            }
            catch(Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Service Convert Pdf Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestServiceConvertPDFA(SystemLogModel systemLog)
        {
            try
            {
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", fileWordTest);

                var convertPDFA = await ConvertPDF.ConvertDocxToPDFAsync(new MemoryStream(File.ReadAllBytes(documentPath)));
                
                if (convertPDFA.Length > 0)
                {
                    return new ResponseObject<string>("Service Convert PDFA Success");
                }
                else
                {
                    return new ResponseObject<string>("Service Convert PDFA Error");
                }
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Service Convert PDFA Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestServiceCIAM(SystemLogModel systemLog)
        {
            try
            {
                var user = new User()
                {
                    Id = Guid.Empty,
                    UserName = "user_test_730afc6d_ccd2_41ba",
                    Password = "123456a@"
                };
                var rs = await _userHandler.CreateSCIMUser(user, systemLog);

                if (rs == null)
                {
                    return new ResponseObject<string>("Service CIAM Connection Error");
                }
                else
                {
                    return new ResponseObject<string>("Service CIAM: " + JsonSerializer.Serialize(rs));
                }
            }
            catch(Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Service CIAM Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestServiceOTP(SystemLogModel systemLog)
        {
            try
            {
                var otp = await _otpService.GenerateHOTPFromService(new HOTPRequestModel()
                {
                    AppRequest = "Savis",
                    UserName = "admin",
                    Description = "Test Service OTP",
                    HOTPSize = 6,
                    Step = 300,
                    ObjectId = Guid.Empty.ToString()
                }, systemLog);

                return new ResponseObject<string>("Service OTP: " + JsonSerializer.Serialize(otp));
            }
            catch(Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Service OTP Error: " + ex.Message);
            }
        }

        public async Task<Shared.Response> TestServiceADSS(TestADSSModel model, SystemLogModel systemLog)
        {
            try
            {              
                string documentPath = Path.Combine(_hostEnvironment.WebRootPath + "/files", "test_file.pdf");

                PdfSigningRequest obj_signRequest = new PdfSigningRequest("samples_test_client", documentPath);
                                
                obj_signRequest.SetProfileId(model.ProfileId);
                if (!string.IsNullOrEmpty(model.Alias))
                {
                    obj_signRequest.SetCertificateAlias(model.Alias);
                }               
                  
                obj_signRequest.SetSigningReason("Ký ADSS");
                obj_signRequest.SetSigningLocation("Hà Nội");
                obj_signRequest.SetContactInfo("thienbq - 0968511597");                
                obj_signRequest.SetRequestMode(PdfSigningRequest.DSS);

                PdfSigningResponse obj_signingResponse = (PdfSigningResponse)obj_signRequest.Send(signAdss);

                if (obj_signingResponse.IsSuccessful())
                    return new ResponseObject<bool>(true, $"{systemLog.TraceId} - Ký thành công.", Code.Success);

                return new ResponseObject<bool>(false, $"{systemLog.TraceId} - Service ADSS Error: " + obj_signingResponse.GetErrorMessage(), Code.BadRequest);
            }
            catch (Exception ex)
            {
                return new ResponseError(Code.ServerError, $"{systemLog.TraceId} - Service ADSS Error: " + ex.Message);
            }
        }
    }
}
