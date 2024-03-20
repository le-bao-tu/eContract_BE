using NetCore.Shared;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace NetCore.Business
{
    public class SignServiceHandler : ISignServiceHandler
    {
        [Obsolete]
        private IHostingEnvironment environment;

        [Obsolete]
        public SignServiceHandler(IHostingEnvironment _environment)
        {
            environment = _environment;
        }

        public async Task<ResponseObject<SignFileModel>> SignBySigningBox(DataInputSignPDF request, Guid? userId, int? signType)
        {
            throw new Exception("Không thể kế nối service ký!");
            try
            {
                string api_key = Utils.GetConfig("Sign:APIKey_Sign");
                string sign_pdf_uri = Utils.GetConfig("Sign:TrustCASign_PDF_API_URL");

                // DataInputSign
                string alias = request.CertAlias;
                string userPin = request.CertUserPin;
                string slotLabel = request.CertSlotLabel;
                string contactInfo = "contact";
                string reason = "reason";
                string location = "location";
                string isVisible = Utils.GetConfig("Sign:isVisible");
                string responseType = Utils.GetConfig("Sign:responseType");

                SignFileModel dataOut = new SignFileModel();

                // Get binary file
                // Url để lấy file PFF
                MemoryStream memoryStream;

                try
                {
                    var ms = new MinIOService();
                    memoryStream = await ms.DownloadObjectAsync(request.FileInfo.FileBucketName, request.FileInfo.FileObjectName);
                    memoryStream.Position = 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return new ResponseObject<SignFileModel>(null, "Không thể tải về file", Code.ServerError);
                }

                //var fileUrl = $"{minio_api_url}api/v1/core/minio/download-object?bucketName={request.FileInfo.FileBucketName}&objectName={request.FileInfo.FileObjectName}";

                byte[] fileByte = memoryStream.ToArray();

                string filename = request.FileInfo.FileName;
                string filenameNonExtension = Path.GetFileNameWithoutExtension(filename);
                string extension = Path.GetExtension(filename);
                HttpContent bytesContent = new ByteArrayContent(fileByte);


                // // Get binary image
                //var imageUrl = Path.Combine(this.Environment.WebRootPath, "images/SAVIS_GROUP_SEAL.png");
                // byte[] fileBytesImage = File.ReadAllBytes(imageUrl);
                // HttpContent bytesContentImage = new ByteArrayContent(fileBytesImage);


                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        multiForm.Add(bytesContent, "fileSign", filename);

                        // Ảnh chữ ký nếu có
                        if (!string.IsNullOrEmpty(request.Base64Image))
                        {
                            byte[] fileBytesImage = Convert.FromBase64String(request.Base64Image);
                            HttpContent bytesContentImage = new ByteArrayContent(fileBytesImage);

                            multiForm.Add(bytesContentImage, "image", "signature-image");
                        }

                        var values = new List<KeyValuePair<string, string>>
                            {
                                //new KeyValuePair<string, string>("slotLabel", slotLabel),
                                new KeyValuePair<string, string>("userPin", userPin),
                                new KeyValuePair<string, string>("alias", alias),
                                new KeyValuePair<string, string>("isVisible", isVisible),
                                new KeyValuePair<string, string>("page", request.Page),
                                new KeyValuePair<string, string>("llx", request.Llx),
                                new KeyValuePair<string, string>("lly", request.Lly),
                                new KeyValuePair<string, string>("urx", request.Urx),
                                new KeyValuePair<string, string>("ury", request.Ury),
                                // new KeyValuePair<string, string>("detail", detail),
                                new KeyValuePair<string, string>("reason", reason),
                                new KeyValuePair<string, string>("location", location),
                                new KeyValuePair<string, string>("contactInfo", contactInfo),
                                new KeyValuePair<string, string>("responseType", responseType)
                            };


                        // Nếu không truyền giá trị signType thì mặc định là ký phê duyệt
                        if (!signType.HasValue)
                        {
                            signType = 2;
                        }

                        // Khai báo thuộc tính kèm theo cho mỗi SignType
                        var certifyValue = string.Empty;
                        var tsaValue = string.Empty;
                        var ltvValue = string.Empty;
                        var detail = string.Empty;

                        if (signType == 1)
                        {
                            certifyValue = Utils.GetConfig("Sign:certify:certify");
                            tsaValue = Utils.GetConfig("Sign:certify:tsa");
                            ltvValue = Utils.GetConfig("Sign:certify:ltv");
                            detail = Utils.GetConfig("Sign:certify:detail");
                        }
                        else if (signType == 2)
                        {
                            certifyValue = Utils.GetConfig("Sign:approval:certify");
                            tsaValue = Utils.GetConfig("Sign:approval:tsa");
                            ltvValue = Utils.GetConfig("Sign:approval:ltv");
                            detail = Utils.GetConfig("Sign:approval:detail");
                        }

                        if (!string.IsNullOrEmpty(certifyValue))
                        {
                            values.Add(new KeyValuePair<string, string>("certify", certifyValue));
                        }

                        if (!string.IsNullOrEmpty(tsaValue))
                        {
                            values.Add(new KeyValuePair<string, string>("tsa", tsaValue));
                        }

                        if (!string.IsNullOrEmpty(ltvValue))
                        {
                            values.Add(new KeyValuePair<string, string>("ltv", ltvValue));
                        }

                        if (!string.IsNullOrEmpty(detail))
                        {
                            values.Add(new KeyValuePair<string, string>("detail", detail));
                        }


                        foreach (var keyValuePair in values)
                        {
                            multiForm.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                        }
                        #region tạm thời pass qua
                        //client.DefaultRequestHeaders.Add("apiKey", api_key);
                        //var res = await client.PostAsync(sign_pdf_uri, multiForm);

                        //if (!res.IsSuccessStatusCode)
                        //{
                        //    throw new Exception("Ký hợp đồng không thành công");
                        //}

                        //string responseText = res.Content.ReadAsStringAsync().Result;

                        //var rsSign = JsonConvert.DeserializeObject<SigningBoxResponseModel>(responseText);

                        //if (rsSign.Code == 1)
                        //{
                        //    var base64FileSigned = rsSign.Data;

                        //    byte[] bytesFileSigned = Convert.FromBase64String(base64FileSigned);

                        //    #region Make file Name
                        //    //var filePrefix = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm") +
                        //    //            DateTime.Now.ToString("ss") +
                        //    //            new Random(DateTime.Now.Millisecond).Next(10, 99);
                        //    // var filePostFix = DateTime.Now.ToString("yy-MM-dd");
                        //    var newFileName = filenameNonExtension + "_" + "_signed" + extension;

                        //    Log.Debug("New file name : " + newFileName);

                        //    #endregion

                        //    #region Send File to MDM

                        //    try
                        //    {

                        //        MemoryStream memStream = new MemoryStream(bytesFileSigned);

                        //        var ms = new MinIOService();

                        //        var fileName = request.FileInfo.FileName ?? "NoName";

                        //        MinIOFileUploadResult minioRS = await ms.UploadObjectAsync(null, fileName, memStream);

                        //        dataOut.FileBucketName = minioRS.BucketName;
                        //        dataOut.FileName = request.FileInfo.FileName;
                        //        dataOut.FileObjectName = minioRS.ObjectName;

                        //        return new ResponseObject<SignFileModel>(dataOut, "Ký hợp thành công", Code.Success);

                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        Log.Error(ex, "");
                        //        return new ResponseObject<SignFileModel>(null, "Ký hợp đồng không thành công - lỗi khi lưu file", Code.ServerError);
                        //    }

                        //    #endregion
                        //}
                        #endregion
                        dataOut.FileBucketName = request.FileInfo.FileBucketName;
                        dataOut.FileName = request.FileInfo.FileName;
                        dataOut.FileObjectName = request.FileInfo.FileObjectName;
                        return new ResponseObject<SignFileModel>(dataOut, "Ký hợp đồng không thành công", Code.ServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseObject<SignFileModel>(null, "Exception: " + ex.Message, Code.ServerError);
            }
        }

        public async Task<ResponseObject<SignFileModel>> ElectronicSigning(DataInputSignPDF request)
        {
            throw new Exception("Không thể kế nối service ký!");
            try
            {
                string apiKey = Utils.GetConfig("ElectronicSigning:APIKey_Sign");
                string sign_pdf_uri = Utils.GetConfig("ElectronicSigning:Sign_PDF_API_URL");

                // DataInputSign
                string isVisible = Utils.GetConfig("ElectronicSigning:isVisible");
                string responseType = Utils.GetConfig("ElectronicSigning:responseType");
                string scaleText = Utils.GetConfig("ElectronicSigning:scaleText");
                string scaleImage = Utils.GetConfig("ElectronicSigning:scaleImage");
                string scaleLogo = Utils.GetConfig("ElectronicSigning:scaleLogo");
                string signatureType = Utils.GetConfig("ElectronicSigning:signatureType");
                string mail = request.Mail ?? Utils.GetConfig("ElectronicSigning:mail");

                SignFileModel dataOut = new SignFileModel();

                // Get binary file
                // Url để lấy file PFF
                MemoryStream memoryStream;

                try
                {
                    var ms = new MinIOService();
                    memoryStream = await ms.DownloadObjectAsync(request.FileInfo.FileBucketName, request.FileInfo.FileObjectName);
                    memoryStream.Position = 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    return new ResponseObject<SignFileModel>(null, "Không thể tải về file", Code.ServerError);
                }

                //var fileUrl = $"{minio_api_url}api/v1/core/minio/download-object?bucketName={request.FileInfo.FileBucketName}&objectName={request.FileInfo.FileObjectName}";

                byte[] fileByte = memoryStream.ToArray();

                string filename = request.FileInfo.FileName;
                string filenameNonExtension = Path.GetFileNameWithoutExtension(filename);
                string extension = Path.GetExtension(filename);
                HttpContent bytesContent = new ByteArrayContent(fileByte);


                // // Get binary image
                //var imageUrl = Path.Combine(this.Environment.WebRootPath, "images/SAVIS_GROUP_SEAL.png");
                // byte[] fileBytesImage = File.ReadAllBytes(imageUrl);
                // HttpContent bytesContentImage = new ByteArrayContent(fileBytesImage);

                using (HttpClientHandler clientHandler = new HttpClientHandler())
                {
                    MultipartFormDataContent multiForm = new MultipartFormDataContent();

                    clientHandler.ServerCertificateCustomValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => { return true; };

                    using (var client = new HttpClient(clientHandler))
                    {
                        multiForm.Add(bytesContent, "fileSign", filename);

                        var values = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("page", request.Page),
                                new KeyValuePair<string, string>("llx", request.Llx),
                                new KeyValuePair<string, string>("lly", request.Lly),
                                new KeyValuePair<string, string>("urx", request.Urx),
                                new KeyValuePair<string, string>("ury", request.Ury),
                                new KeyValuePair<string, string>("isVisible", isVisible),
                                new KeyValuePair<string, string>("signatureType", signatureType),
                                new KeyValuePair<string, string>("mail", mail),
                                new KeyValuePair<string, string>("scaleText", scaleText),
                                new KeyValuePair<string, string>("scaleImage", scaleImage),
                                new KeyValuePair<string, string>("scaleLogo", scaleLogo)
                            };
                        // Ảnh chữ ký nếu có
                        if (!string.IsNullOrEmpty(request.Base64Image))
                        {
                            values.Add(new KeyValuePair<string, string>("imageData", request.Base64Image));
                        }

                        foreach (var keyValuePair in values)
                        {
                            multiForm.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                        }

                        client.DefaultRequestHeaders.Add("apikey", apiKey);
                        var res = await client.PostAsync(sign_pdf_uri, multiForm);

                        if (!res.IsSuccessStatusCode)
                        {
                            Log.Error(JsonSerializer.Serialize(res));
                            throw new Exception("Ký tài liệu không thành công");
                        }

                        string responseText = res.Content.ReadAsStringAsync().Result;

                        var rsSign = JsonSerializer.Deserialize<ElectronicSigningResponseModel>(responseText);

                        if (rsSign.Code == 1)
                        {
                            var base64FileSigned = rsSign.Data.File;

                            byte[] bytesFileSigned = Convert.FromBase64String(base64FileSigned);

                            #region Make file Name
                            //var filePrefix = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm") +
                            //            DateTime.Now.ToString("ss") +
                            //            new Random(DateTime.Now.Millisecond).Next(10, 99);
                            // var filePostFix = DateTime.Now.ToString("yy-MM-dd");
                            var newFileName = filenameNonExtension + "_" + "_signed" + extension;

                            Log.Debug("New file name : " + newFileName);

                            #endregion

                            #region Send File to MDM

                            try
                            {
                                MemoryStream memStream = new MemoryStream(bytesFileSigned);

                                var ms = new MinIOService();

                                var fileName = request.FileInfo.FileName ?? "NoName";

                                MinIOFileUploadResult minioRS = await ms.UploadObjectAsync(null, fileName, memStream);

                                dataOut.FileBucketName = minioRS.BucketName;
                                dataOut.FileName = request.FileInfo.FileName;
                                dataOut.FileObjectName = minioRS.ObjectName;

                                return new ResponseObject<SignFileModel>(dataOut, "Ký tài liệu thành công", Code.Success);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "");
                                return new ResponseObject<SignFileModel>(null, "Ký tài liệu không thành công - lỗi khi lưu file", Code.ServerError);
                            }

                            #endregion
                        }
                        return new ResponseObject<SignFileModel>(null, "Ký tài liệu không thành công", Code.ServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseObject<SignFileModel>(null, "Exception: " + ex.Message, Code.ServerError);
            }
        }
    }
}