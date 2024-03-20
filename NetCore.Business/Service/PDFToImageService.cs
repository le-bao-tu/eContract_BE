using MimeMapping;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using NetCore.Data;
using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class PDFToImageService
    {
        private static readonly string _baseUrl = Utils.GetConfig("PDFCovert:uri");
        private static readonly string _eContractBaseUrl = Utils.GetConfig("eContractService:uri");

        public PDFToImageService()
        {

        }

        public async Task<List<string>> ConvertPDFToPNG(PDFConvertPNGServiceModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - PDF To PNG");
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();

                        byte[] fileBytes = model.StreamData.ToArray();

                        HttpContent bytesContent = new ByteArrayContent(fileBytes);
                        form.Add(bytesContent, "file", "contract.pdf");
                        //form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "file", "contract.pdf");
                        //form.Add(new StringContent(useremail), "email");

                        HttpResponseMessage res = await client.PostAsync(_baseUrl + "api/v1/core/pdfconvert/pdf-to-png-base64", form);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Convert file thành công: " + responeText);

                            var listFileBase64 = JsonSerializer.Deserialize<List<string>>(responeText);

                            return listFileBase64;
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không convert được file");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi convert file");
                return null;
            }
        }

        public async Task<List<string>> ConvertPDFBase64ToPNG(PDFConvertPNGServiceModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - PDFBase64 To PNG");
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(_baseUrl + "api/v1/core/pdfconvert/pdf-base64-to-png-base64", content);

                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;

                            var responseModel = JsonSerializer.Deserialize<ConvertPDFBase64ToPNGResponseModel>(responeText);
                            if (responseModel.Code == (int)Code.Success)
                            {
                                Log.Information($"{systemLog.TraceId} - Convert file thành công {responseModel.Data.Count}");
                                return responseModel.Data;
                            }
                            else
                            {
                                Log.Information($"{systemLog.TraceId} - Convert file thất bại {responseModel.Code} {responseModel.Message}");
                                return null;
                            }
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không convert được file");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi convert file");
                return null;
            }
        }

        public async Task<bool> ConvertPDFToPNGCallBack(PDFConvertPNGCallbackServiceModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - PDF To PNG - {model.DocumentFileId}");
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        MultipartFormDataContent form = new MultipartFormDataContent();

                        byte[] fileBytes = model.StreamData.ToArray();

                        HttpContent bytesContent = new ByteArrayContent(fileBytes);
                        form.Add(bytesContent, "file", "contract.pdf");
                        //form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "file", "contract.pdf");

                        PdfCallBackRequestModel pdfCallBack = new PdfCallBackRequestModel()
                        {
                            ObjectId = model.DocumentFileId.ToString(),
                            CallBackUrl = _eContractBaseUrl + "api/v1/document/update-document-file-preview"
                        };
                        form.Add(new StringContent(JsonSerializer.Serialize(pdfCallBack)), "callback");

                        HttpResponseMessage res = await client.PostAsync(_baseUrl + "api/v1/core/pdfconvert/pdf-to-png-base64-callback", form);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Convert file thành công: " + responeText);
                            return true;
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không convert được file");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi convert file");
                return false;
            }
        }

        public async Task<bool> ConvertPDFBase64ToPNGCallBack(PDFConvertPNGCallbackServiceModel model, SystemLogModel systemLog)
        {
            Log.Information($"{systemLog.TraceId} - PDFBase64 To PNG - {model.DocumentFileId}");
            try
            {
                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        PdfCallBackRequestModel pdfCallBack = new PdfCallBackRequestModel()
                        {
                            ObjectId = model.DocumentFileId.ToString(),
                            CallBackUrl = _eContractBaseUrl + "api/v1/document/update-document-file-preview",
                            FileBase64 = model.FileBase64
                        };

                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(pdfCallBack), Encoding.UTF8, "application/json");
                        HttpResponseMessage res = await client.PostAsync(_baseUrl + "api/v1/core/pdfconvert/pdf-base64-to-png-base64-callback", content);
                        if (res.IsSuccessStatusCode)
                        {
                            var responeText = res.Content.ReadAsStringAsync().Result;
                            Log.Information($"{systemLog.TraceId} - Convert file thành công: " + responeText);
                            return true;
                        }
                        else
                        {
                            Log.Error($"{systemLog.TraceId} - Lỗi kế nối dịch vụ, không convert được file");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"{systemLog.TraceId} - Lỗi khi convert file");
                return false;
            }
        }
    }

    public class PdfCallBackRequestModel
    {
        public string CallBackUrl { get; set; }
        public string ObjectId { get; set; }
        public string FileBase64 { get; set; }
    }

    public class PdfCallBackResponseModel
    {
        public string ObjectId { get; set; }
        public List<string> ListFileBase64 { get; set; }
    }

    public class PDFConvertPNGServiceModel
    {
        public Guid DocumentFileId { get; set; }
        [JsonIgnore]
        public MemoryStream StreamData { get; set; }
        public string FileBase64 { get; set; }
    }
    public class PDFConvertPNGCallbackServiceModel
    {
        public Guid DocumentFileId { get; set; }
        [JsonIgnore]
        public MemoryStream StreamData { get; set; }
        public string FileBase64 { get; set; }
    }

    public class ConvertPDFBase64ToPNGResponseModel
    {
        [JsonPropertyName("data")]
        public List<string> Data { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("traceId")]
        public Guid TraceId { get; set; }
    }
}
