using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeMapping;
using NetCore.Business;
using NetCore.Shared;
using Serilog;

namespace NetCore.API
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/core/minio")]
    [ApiExplorerSettings(GroupName = "MinIO - 01. MinIO")]
    [Authorize]
    public class MinIOController : ApiControllerBase
    {
        public MinIOController(ISystemLogHandler logHandler) : base(logHandler)
        {
        }
        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="fileName"></param>
        /// <param name="file">File upoad</param>
        /// <param name="isConvertToPDFA">Option convert file pdf to pdf/a</param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload-object")]
        [ProducesResponseType(typeof(ResponseObject<MinIOFileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadObject(string bucketName, string fileName, IFormFile file, bool isConvertToPDFA = false)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = new Response();
                try
                {
                    var ms = new MinIOService();
                    if (file == null)
                        result = new ResponseError(Code.MethodNotAllowed, "Chưa tải lên file");
                    fileName = fileName ?? file.FileName ?? "NoName";
                    var fileStream = file.OpenReadStream();
                    var memory = new MemoryStream();
                    fileStream.CopyTo(memory);
                    if (file.ContentType == "application/pdf" && isConvertToPDFA)
                    {
                        //Convert PDF to PDF/A  
                        ConvertPDF.ConvertToPDFA(ref memory);
                    }
                    var rs = await ms.UploadObjectAsync(bucketName, fileName, memory);
                    result = new ResponseObject<MinIOFileUploadResult>(rs, "Tải lên file thành công", Code.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    result = new ResponseError(Code.ServerError,
                        "Có lỗi trong quá trình xử lý: " + ex.Message);
                }

                return result;
            });
        }


        /// <summary>
        /// Upload document template
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="fileName"></param>
        /// <param name="file">File upoad</param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload-document-file-template")]
        [ProducesResponseType(typeof(ResponseObject<DocumentFileTemplateUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFileTemplate(string bucketName, string fileName, IFormFile file)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = new Response();
                try
                {
                    var ms = new MinIOService();
                    if (file == null)
                        result = new ResponseError(Code.MethodNotAllowed, "Chưa tải lên file");
                    fileName = fileName ?? file.FileName ?? "NoName";
                    var fileStream = file.OpenReadStream();
                    var memory = new MemoryStream();
                    fileStream.CopyTo(memory);
                    if (file.ContentType == "application/pdf")
                    {
                        //Convert PDF to PDF/A  
                        ConvertPDF.ConvertToPDFA(ref memory);
                        var rsUpload = await ms.UploadObjectAsync(bucketName, fileName, memory);

                        DocumentFileTemplateUploadResult rs = new DocumentFileTemplateUploadResult()
                        {
                            BucketName = rsUpload.BucketName,
                            ObjectName = rsUpload.ObjectName,
                            DataBucketName = rsUpload.BucketName,
                            DataObjectName = rsUpload.ObjectName,
                            FileName = fileName,
                            FileType = Data.TemplateFileType.PDF
                        };

                        result = new ResponseObject<DocumentFileTemplateUploadResult>(rs, "Tải lên file thành công", Code.Success);
                    }
                    else if (file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                    {
                        //Convert docx to PDF/A  
                        var memoryPDF = await ConvertPDF.ConvertDocxToPDFAsync(memory);

                        //upload docx
                        var rsDocx = await ms.UploadObjectAsync(bucketName, System.IO.Path.GetFileNameWithoutExtension(fileName) + ".docx", memory);

                        //upload pdf
                        var rsPDF = await ms.UploadObjectAsync(bucketName, System.IO.Path.GetFileNameWithoutExtension(fileName) + ".pdf", memoryPDF);
                        DocumentFileTemplateUploadResult rs = new DocumentFileTemplateUploadResult()
                        {
                            BucketName = rsPDF.BucketName,
                            ObjectName = rsPDF.ObjectName,
                            DataBucketName = rsDocx.BucketName,
                            DataObjectName = rsDocx.ObjectName,
                            FileName = fileName,
                            FileType = Data.TemplateFileType.DOCX
                        };

                        result = new ResponseObject<DocumentFileTemplateUploadResult>(rs, "Tải lên file thành công", Code.Success);
                    }
                    else
                    {
                        result = new Response(Code.BadRequest, "Định dạng file chưa được hỗ trợ");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    result = new ResponseError(Code.ServerError,
                        "Có lỗi trong quá trình xử lý: " + ex.Message);
                }



                return result;
            });
        }


        /// <summary>
        /// Upload file Base 64
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="file"></param>
        /// <param name="isConvertToPDFA">Option convert file pdf to pdf/a</param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload-object-base64")]
        [ProducesResponseType(typeof(ResponseObject<MinIOFileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadObjectBase64(string bucketName, [FromBody] Base64FileData file, bool isConvertToPDFA = false)
        {
            try
            {
                var ms = new MinIOService();
                if (string.IsNullOrEmpty(file.Name))
                    return Helper.TransformData(new ResponseError(Code.MethodNotAllowed, "Tên file không được để trống"));
                if (string.IsNullOrEmpty(file.FileData))
                    return Helper.TransformData(new ResponseError(Code.MethodNotAllowed, "Chưa tải lên file"));
                file.Name = file.Name ?? "NoName";
                if (file.FileData.IndexOf(',') >= 0)
                    file.FileData = file.FileData.Substring(file.FileData.IndexOf(',') + 1);
                var bytes = Convert.FromBase64String(file.FileData);
                var memory = new MemoryStream(bytes);
                if (isConvertToPDFA && file.Name.ToLower().EndsWith(".pdf"))
                {
                    //Convert PDF to PDF/A  
                    ConvertPDF.ConvertToPDFA(ref memory);
                }
                var result = await ms.UploadObjectAsync(bucketName, file.Name, memory);
                return Helper.TransformData(new ResponseObject<MinIOFileUploadResult>(result, "Tải lên file thành công", Code.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                return Helper.TransformData(new ResponseError(Code.ServerError,
                    "Có lỗi trong quá trình xử lý: " + ex.Message));
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("download-object")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadObject(string bucketName, string objectName)
        {
            try
            {
                var ms = new MinIOService();
                var fileName = Path.GetFileName(objectName);
                var memory = await ms.DownloadObjectAsync(bucketName, objectName);
                memory.Position = 0;
                var cd = new ContentDisposition
                {
                    FileName = Utils.RemoveVietnameseSign(fileName),
                    Inline = true
                };
                Response.Headers.Add("Content-Disposition", cd.ToString());
                return File(memory, MimeUtility.GetMimeMapping(fileName));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                if (ex.Message == "Not found.")
                {
                    return Helper.TransformData(new ResponseError(Code.NotFound, "File not found."));
                }
                else
                {
                    return Helper.TransformData(new ResponseError(Code.ServerError, "Có lỗi trong quá trình xử lý: " + ex.Message));
                }
            }
        }
        /// <summary>
        /// Download file base64
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("download-object-base64")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResponseObject<Base64FileData>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadObjectBase64(string bucketName, string objectName)
        {
            try
            {
                var ms = new MinIOService();
                var fileName = Path.GetFileName(objectName);
                var memory = await ms.DownloadObjectAsync(bucketName, objectName);
                var fileData = memory.ToArray();
                var file = new Base64FileData()
                {
                    Name = fileName,
                    FileData = Convert.ToBase64String(fileData)
                };
                return Helper.TransformData(new ResponseObject<Base64FileData>(file, "Success", Code.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                if (ex.Message == "Not found.")
                {
                    return Helper.TransformData(new ResponseError(Code.NotFound, "File not found."));
                }
                else
                {
                    return Helper.TransformData(new ResponseError(Code.ServerError, "Có lỗi trong quá trình xử lý: " + ex.Message));
                }
            }
        }

        ///// <summary>
        ///// Get link download file
        ///// </summary>
        ///// <param name="bucketName"></param>
        ///// <param name="objectName"></param>
        ///// <param name="expiresInt"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet]
        //[Route("getlink-object")]
        //[ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetLinkObject(string bucketName, string objectName, int expiresInt = 60 * 60 * 24)
        //{
        //    try
        //    {
        //        var ms = new MinIOService();
        //        var url = await ms.GetObjectUrlAsync(bucketName, objectName, expiresInt);
        //        return Helper.TransformData(new ResponseObject<string>(url, "Success", Code.Success));
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "");
        //        return Helper.TransformData(new ResponseError(Code.ServerError, "Có lỗi trong quá trình xử lý: " + ex.Message));
        //    }
        //}

        /// <summary>
        /// Upload từ client nhưng không cần xử lý file ở api
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("upload-file-unsave")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadFileUnsave()
        {
            return Ok();
        }

        #region WebApp
        /// <summary>
        /// Upload file đã ký từ web app
        /// </summary>
        /// <param name="file">File upoad</param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload-file-from-webapp")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResponseObject<MinIOFileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadObjectFromWebApp(IFormFile file)
        {
            return await ExecuteFunction(async (RequestUser u) =>
            {
                var result = new Response();
                string authHeader = Request.Headers["Authorization"];
                try
                {
                    //kiểm tra token có hợp lệ hay không => nếu hợp lệ thì cho upload file 
                    var token = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                    if (!SignHashHandler.ValidateJWTToken(token))
                    {
                        return new ResponseError(Code.BadRequest, "Token không hợp lệ, vui lòng truy cập lại và thực hiện ký");
                    }

                    var ms = new MinIOService();
                    if (file == null)
                        result = new ResponseError(Code.MethodNotAllowed, "Chưa tải lên file");
                    string fileName = file.FileName;
                    var fileStream = file.OpenReadStream();
                    var memory = new MemoryStream();
                    fileStream.CopyTo(memory);
                    if (file.ContentType != "application/pdf")
                    {
                        result = new Response(Code.BadRequest, "Định dạng file chưa được hỗ trợ");
                    }
                    // TODO: Bổ sung code kiểm tra chữ ký số của file

                    var rs = await ms.UploadObjectAsync("", fileName, memory);
                    result = new ResponseObject<MinIOFileUploadResult>(rs, "Tải lên file thành công", Code.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    result = new ResponseError(Code.ServerError,
                        "Có lỗi trong quá trình xử lý: " + ex.Message);
                }

                return result;
            });
        }
        #endregion
    }
}
