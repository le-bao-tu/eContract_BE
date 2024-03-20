using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Serilog;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NetCore.Shared;

namespace NetCore.API
{
    /// <summary>
    /// Core - Upload
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{api-version:apiVersion}/core/nodes")]
    [ApiExplorerSettings(GroupName = "00. Core Upload", IgnoreApi = true)]
    [AllowAnonymous]
    public class MdmNodeUploadController : ControllerBase
    {
        #region Upload physical 

        /// <summary>
        ///     Tải lên một file dưới dạng Base64
        /// </summary>
        /// <param name="model">Thông tin upload</param>
        /// <param name="destinationPhysicalPath">Đường dẫn vật lý (tùy chọn)</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [Route("upload/physical/base64")]
        [ProducesResponseType(typeof(ResponseObject<FileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFileBase64Physical([FromBody] Base64FileData model, string destinationPhysicalPath = null)
        {
            // Call service
            try
            {
                #region Lấy thông tin vùng lưu trữ vật lý

                string partitionPhysicalPath;

                partitionPhysicalPath = Path.Combine(Utils.GetConfig("StaticFiles:Folder"));

                #endregion

                #region Xác định thư mục đích

                // Get root path
                var rootPath = partitionPhysicalPath;
                //Log.Debug("Upload root path : " + rootPath);
                // Append destinationPhysicalPath
                if (string.IsNullOrEmpty(destinationPhysicalPath))
                {
                    destinationPhysicalPath = partitionPhysicalPath + "\\" + destinationPhysicalPath + "\\" +
                                              DateTime.Now.ToString("yyyy") + "\\" + DateTime.Now.ToString("MM") +
                                              "\\" +
                                              DateTime.Now.ToString("dd") + "\\";
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (!isWindows)
                    {
                        destinationPhysicalPath = destinationPhysicalPath.Replace("\\", "/");
                    }
                }

                //Log.Debug("DestinationPhysicalPath : " + destinationPhysicalPath);
                var sourcefolder = destinationPhysicalPath;
                //Log.Debug("Upload source folder : " + sourcefolder);
                // Create folder path
                var isExistDirectory = Directory.Exists(Path.Combine(rootPath, sourcefolder));
                if (!isExistDirectory) Directory.CreateDirectory(Path.Combine(rootPath, sourcefolder));
                var fullfolderPath = Path.Combine(rootPath, sourcefolder);

                #endregion

                #region Lưu file upload vật lý

                // get data image from client
                var base64 = model.FileData;
                if (base64.IndexOf(',') >= 0) base64 = base64.Substring(base64.IndexOf(',') + 1);
                var bytes = Convert.FromBase64String(base64);
                var filename = Regex.Replace(model.Name, @"\s", "_");
                #region makefileName

                var filePrefix = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm") +
                                 DateTime.Now.ToString("ss") +
                                 new Random(DateTime.Now.Millisecond).Next(10, 99);
                var filePostFix = DateTime.Now.ToString("yy-MM-dd");
                var newFileName = Path.GetFileNameWithoutExtension(filename) + "_" + filePrefix + "_" +
                                  filePostFix + Path.GetExtension(filename);
                Log.Debug("New file name : " + newFileName);
                if (newFileName.Length > 255)
                {
                    var withoutExtName = Path.GetFileNameWithoutExtension(filename);
                    var extName = Path.GetExtension(filename);
                    var trimmed = withoutExtName.Substring(0,
                        withoutExtName.Length - (255 - filePrefix.Length - filePostFix.Length - extName.Length));
                    newFileName = trimmed + "_" + filePrefix + "_" + filePostFix + Path.GetExtension(filename);
                }

                #endregion

                var fileSavePath = Path.Combine(fullfolderPath, newFileName);
                using (var imageFile = new FileStream(fileSavePath, FileMode.Create))
                {
                    await imageFile.WriteAsync(bytes, 0, bytes.Length);
                    imageFile.Flush();
                }

                var fileInfo = new FileInfo(fileSavePath);

                #endregion

                return Helper.TransformData(new ResponseObject<FileUploadResult>(new FileUploadResult
                {
                    PhysicalPath = Path.Combine(sourcefolder, fileInfo.Name).Replace("\\", "/"),
                    PhysicalName = fileInfo.Name,
                    Name = model.Name,
                    Size = fileInfo.Length,
                    Extension = fileInfo.Extension
                }, "Tải lên file thành công", Code.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                return Helper.TransformData(new ResponseError(Code.ServerError,
                    "Có lỗi trong quá trình xử lý: " + ex.Message));
            }
        }

        /// <summary>
        ///     Tải lên nhiều file dưới dạng Base64
        /// </summary>
        /// <param name="model">Thông tin upload</param>
        /// <param name="destinationPhysicalPath">Đường dẫn vật lý (tùy chọn)</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [Route("upload/physical/base64/many")]
        [ProducesResponseType(typeof(ResponseList<FileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFileBase64ManyPhysical([FromBody] IList<Base64FileData> model, string destinationPhysicalPath = null)
        {
            try
            {
                #region Lấy thông tin vùng lưu trữ vật lý

                string partitionPhysicalPath;
                partitionPhysicalPath = Path.Combine(Utils.GetConfig("StaticFiles:Folder"));

                #endregion

                #region Xác định thư mục đích

                // Get root path
                var rootPath = partitionPhysicalPath;
                //Log.Debug("Upload root path : " + rootPath);
                // Append destinationPhysicalPath
                if (string.IsNullOrEmpty(destinationPhysicalPath))
                {
                    destinationPhysicalPath = partitionPhysicalPath + "\\" + destinationPhysicalPath + "\\" +
                                              DateTime.Now.ToString("yyyy") + "\\" + DateTime.Now.ToString("MM") +
                                              "\\" +
                                              DateTime.Now.ToString("dd") + "\\";
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (!isWindows)
                    {
                        destinationPhysicalPath = destinationPhysicalPath.Replace("\\", "/");
                    }
                }

                //Log.Debug("DestinationPhysicalPath : " + destinationPhysicalPath);
                var sourcefolder = destinationPhysicalPath;
                //Log.Debug("Upload source folder : " + sourcefolder);
                // Create folder path
                var isExistDirectory = Directory.Exists(Path.Combine(rootPath, sourcefolder));
                if (!isExistDirectory) Directory.CreateDirectory(Path.Combine(rootPath, sourcefolder));
                var fullfolderPath = Path.Combine(rootPath, sourcefolder);

                #endregion

                #region Lưu file upload vật lý

                var listUploadResult = new List<FileUploadResult>();
                foreach (var fileData in model)
                {
                    #region Create File profile in file source path

                    // get data image from client
                    var base64 = fileData.FileData;
                    if (base64.IndexOf(',') >= 0) base64 = base64.Substring(base64.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64);
                    var filename = fileData.Name;
                    filename = Regex.Replace(filename, @"\s", "_");
                    #region makefileName

                    var filePrefix = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm") +
                                     DateTime.Now.ToString("ss") +
                                     new Random(DateTime.Now.Millisecond).Next(10, 99);
                    var filePostFix = DateTime.Now.ToString("yy-MM-dd");
                    var newFileName = Path.GetFileNameWithoutExtension(filename) + "_" + filePrefix + "_" +
                                      filePostFix + Path.GetExtension(filename);
                    Log.Debug("New file name : " + newFileName);
                    if (newFileName.Length > 255)
                    {
                        var withoutExtName = Path.GetFileNameWithoutExtension(filename);
                        var extName = Path.GetExtension(filename);
                        var trimmed = withoutExtName.Substring(0,
                            withoutExtName.Length - (255 - filePrefix.Length - filePostFix.Length - extName.Length));
                        newFileName = trimmed + "_" + filePrefix + "_" + filePostFix + Path.GetExtension(filename);
                    }

                    #endregion

                    var fileSavePath = Path.Combine(fullfolderPath, newFileName);
                    using (var imageFile = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await imageFile.WriteAsync(bytes, 0, bytes.Length);
                        imageFile.Flush();
                    }

                    var fileInfo = new FileInfo(fileSavePath);
                    listUploadResult.Add(new FileUploadResult
                    {
                        PhysicalPath = Path.Combine(sourcefolder, fileInfo.Name).Replace("\\", "/"),
                        PhysicalName = fileInfo.Name,
                        Name = fileData.Name,
                        Size = fileInfo.Length,
                        Extension = fileInfo.Extension
                    });

                    #endregion
                }

                #endregion

                return Helper.TransformData(new ResponseObject<List<FileUploadResult>>(listUploadResult, "Tải lên file thành công", Code.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                return Helper.TransformData(new ResponseError(Code.ServerError,
                    "Có lỗi trong quá trình xử lý: " + ex.Message));
            }
        }

        /// <summary>
        ///     Tải lên một file
        /// </summary>
        /// <param name="file">File upoad</param>
        /// <param name="destinationPhysicalPath">Đường dẫn vật lý (tùy chọn)</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [Route("upload/physical/blob")]
        [ProducesResponseType(typeof(ResponseObject<FileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFileBlobPhysical(IFormFile file, string destinationPhysicalPath = null)
        {
            try
            {
                #region Lấy thông tin vùng lưu trữ vật lý

                string partitionPhysicalPath;
                partitionPhysicalPath = Path.Combine(Utils.GetConfig("StaticFiles:Folder"));

                #endregion

                #region Xác định thư mục đích

                // Get root path
                var rootPath = partitionPhysicalPath;
                //Log.Debug("Upload root path : " + rootPath);
                // Append destinationPhysicalPath
                if (string.IsNullOrEmpty(destinationPhysicalPath))
                {

                    destinationPhysicalPath = partitionPhysicalPath + "\\" + destinationPhysicalPath + "\\" +
                                              DateTime.Now.ToString("yyyy") + "\\" + DateTime.Now.ToString("MM") +
                                              "\\" +
                                              DateTime.Now.ToString("dd") + "\\";
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (!isWindows)
                    {
                        destinationPhysicalPath = destinationPhysicalPath.Replace("\\", "/");
                    }
                }

                //Log.Debug("DestinationPhysicalPath : " + destinationPhysicalPath);
                var sourcefolder = destinationPhysicalPath;
                //Log.Debug("Upload source folder : " + sourcefolder);
                // Create folder path
                var isExistDirectory = Directory.Exists(Path.Combine(rootPath, sourcefolder));
                if (!isExistDirectory) Directory.CreateDirectory(Path.Combine(rootPath, sourcefolder));
                #endregion

                #region Do Upload

                #region Tạo tên file từ đích

                var filename = file.FileName ?? "NoName";
                filename = Regex.Replace(filename, @"\s", "_");

                // Generate new file name
                var filePrefix = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm") +
                                 DateTime.Now.ToString("ss") +
                                 new Random(DateTime.Now.Millisecond).Next(10, 99);

                var filePostFix = DateTime.Now.ToString("yy-MM-dd");

                var newFileName = Path.GetFileNameWithoutExtension(filename) + "_" + filePrefix + "_" +
                                  filePostFix + Path.GetExtension(filename);
                // Check length

                if (newFileName.Length > 255)
                {
                    var withoutExtName = Path.GetFileNameWithoutExtension(filename);
                    var extName = Path.GetExtension(filename);
                    var trimmed = withoutExtName.Substring(0,
                        withoutExtName.Length - (255 - filePrefix.Length - filePostFix.Length - extName.Length));

                    newFileName = trimmed + "_" + filePrefix + "_" + filePostFix + Path.GetExtension(filename);
                }

                #endregion

                var fullPath = Path.Combine(sourcefolder, newFileName);
                var uploadResult = new FileUploadResult
                {
                    PhysicalPath = Path.Combine(fullPath).Replace("\\", "/"),
                    Name = filename,
                    PhysicalName = newFileName,
                    Size = file.Length,
                    Extension = Path.GetExtension(file.FileName)
                };

                if (file.Length > 0)
                    using (var stream = new FileStream(Path.Combine(rootPath, fullPath), FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                #endregion 
                return Helper.TransformData(new ResponseObject<FileUploadResult>(uploadResult, "Tải lên file thành công", Code.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                return Helper.TransformData(new ResponseError(Code.ServerError,
                    "Có lỗi trong quá trình xử lý: " + ex.Message));
            }
        }

        /// <summary>
        ///     Tải lên nhiều file
        /// </summary>
        /// <param name="formData"></param>
        /// <param name="destinationPhysicalPath">Đường dẫn vật lý (tùy chọn)</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [Route("upload/physical/blob/many")]
        [ProducesResponseType(typeof(ResponseList<FileUploadResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadFileBlobManyPhysical([FromForm] IFormCollection formData, string destinationPhysicalPath = null)
        {
            try
            {
                #region Lấy thông tin vùng lưu trữ vật lý

                string partitionPhysicalPath;
                partitionPhysicalPath = Path.Combine(Utils.GetConfig("StaticFiles:Folder"));

                #endregion

                #region Xác định thư mục đích

                // Get root path
                var rootPath = partitionPhysicalPath;
                //Log.Debug("Upload root path : " + rootPath);
                // Append destinationPhysicalPath
                if (string.IsNullOrEmpty(destinationPhysicalPath))
                {
                    destinationPhysicalPath = partitionPhysicalPath + "\\" + destinationPhysicalPath + "\\" +
                                              DateTime.Now.ToString("yyyy") + "\\" + DateTime.Now.ToString("MM") +
                                              "\\" +
                                              DateTime.Now.ToString("dd") + "\\";
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (!isWindows)
                    {
                        destinationPhysicalPath = destinationPhysicalPath.Replace("\\", "/");
                    }
                }

                //Log.Debug("DestinationPhysicalPath : " + destinationPhysicalPath);
                var sourcefolder = destinationPhysicalPath;
                //Log.Debug("Upload source folder : " + sourcefolder);
                // Create folder path
                var isExistDirectory = Directory.Exists(Path.Combine(rootPath, sourcefolder));
                if (!isExistDirectory) Directory.CreateDirectory(Path.Combine(rootPath, sourcefolder));
                #endregion

                #region Do Upload

                var listUploadResult = new List<FileUploadResult>();
                foreach (var file in formData.Files)
                {
                    #region Tạo tên file từ đích

                    var filename = file.FileName ?? "NoName";
                    filename = filename.Trim('"').Replace("&", "and");
                    filename = Regex.Replace(filename, @"\s", "_");
                    // Generate new file name
                    var filePrefix = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm") +
                                     DateTime.Now.ToString("ss") +
                                     new Random(DateTime.Now.Millisecond).Next(10, 99);

                    var filePostFix = DateTime.Now.ToString("yy-MM-dd");

                    var newFileName = Path.GetFileNameWithoutExtension(filename) + "_" + filePrefix + "_" +
                                      filePostFix + Path.GetExtension(filename);
                    // Check length

                    if (newFileName.Length > 255)
                    {
                        var withoutExtName = Path.GetFileNameWithoutExtension(filename);
                        var extName = Path.GetExtension(filename);
                        var trimmed = withoutExtName.Substring(0,
                            withoutExtName.Length - (255 - filePrefix.Length - filePostFix.Length - extName.Length));

                        newFileName = trimmed + "_" + filePrefix + "_" + filePostFix + Path.GetExtension(filename);
                    }

                    #endregion

                    var fullPath = Path.Combine(sourcefolder, newFileName);
                    listUploadResult.Add(new FileUploadResult
                    {
                        PhysicalPath = Path.Combine(sourcefolder, newFileName).Replace("\\", "/"),
                        Name = filename,
                        PhysicalName = newFileName,
                        Size = file.Length,
                        Extension = Path.GetExtension(file.FileName)
                    });
                    if (file.Length > 0)
                        using (var stream = new FileStream(Path.Combine(rootPath, fullPath), FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                }

                #endregion

                return Helper.TransformData(new ResponseObject<List<FileUploadResult>>(listUploadResult, "Tải lên file thành công", Code.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                return Helper.TransformData(new ResponseError(Code.ServerError,
                    "Có lỗi trong quá trình xử lý: " + ex.Message));
            }
        }

        #endregion
    }
}