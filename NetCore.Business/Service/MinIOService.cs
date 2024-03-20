using MimeMapping;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using NetCore.Data;
using NetCore.Shared;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetCore.Business
{
    public class MinIOService
    {
        private readonly MinioClient _minio;

        //Use https instead of http
        private static readonly bool _useSSL = Utils.GetConfig("minio:enableSsl") == "1";

        //endPoint is an URL, domain name, IPv4 address or IPv6 address
        private static readonly string _endpoint = Utils.GetConfig("minio:endpoint");
        private static readonly string _endpointUrl = Utils.GetConfig("minio:endpointUrl");
        private static readonly string _cdnUrl = Utils.GetConfig("minio:cdnUrl");

        //accessKey is like user-id that uniquely identifies your account
        private static readonly string _accessKey = Utils.GetConfig("minio:accesskey");

        //secretKey is the password to your account
        private static readonly string _secretKey = Utils.GetConfig("minio:secretKey");

        //default bucketname
        private static readonly string _defaultBucketName = Utils.GetConfig("minio:defaultBucketName");

        public MinIOService()
        {
            if (_useSSL)
                _minio = new MinioClient(_endpoint, _accessKey, _secretKey).WithSSL();
            else
                _minio = new MinioClient(_endpoint, _accessKey, _secretKey);
        }

        /// <summary>
        /// Uploads contents from a stream to objectName
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<MinIOFileUploadResult> UploadObjectAsync(string bucketName, string fileName, Stream data, bool isCustomSubFolder = true)
        {
            try
            {
                bucketName = bucketName ?? _defaultBucketName;
                bucketName = bucketName.ToLower();
                if (bucketName.Length < 3)
                {
                    bucketName = "bn-" + bucketName;
                }
                fileName = RenameFile(fileName);
                string objectName = "";
                if (isCustomSubFolder)
                {
                    var time = DateTime.Now;
                    var subFolder = $"{time.Year}/{time.Month}/{time.Day}/";
                    objectName = subFolder + fileName;
                }
                else
                {
                    objectName = fileName;
                }

                var contentType = MimeUtility.GetMimeMapping(fileName);
                data.Position = 0;

                // Make a bucket on the server, if not already present.
                bool found = await _minio.BucketExistsAsync(bucketName);
                if (!found)
                {
                    await _minio.MakeBucketAsync(bucketName);
                }
                if (_useSSL)
                {
                    //Server - side encryption with customer provided keys(SSE - C)
                    Aes aesEncryption = Aes.Create();
                    aesEncryption.KeySize = 256;
                    aesEncryption.GenerateKey();
                    var ssec = new SSEC(aesEncryption.Key);

                    await _minio.PutObjectAsync(bucketName, objectName, data, data.Length, contentType, null, ssec);
                }
                else
                {
                    await _minio.PutObjectAsync(bucketName, objectName, data, data.Length, contentType);
                }
                return new MinIOFileUploadResult
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    FileName = fileName
                };
            }
            catch (MinioException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        ///  Downloads an object
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public async Task<MemoryStream> DownloadObjectAsync(string bucketName, string objectName)
        {
            try
            {
                MemoryStream memory = new MemoryStream();
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                bucketName = bucketName ?? _defaultBucketName;
                await _minio.StatObjectAsync(bucketName, objectName);

                // Get input stream to have content of 'my-objectname' from 'my-bucketname'
                await _minio.GetObjectAsync(bucketName, objectName, (stream) =>
                {
                    stream.CopyTo(memory);
                });
                return memory;
            }
            catch (MinioException e)
            {
                throw new Exception(e.ServerMessage);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        ///  Downloads an object and return base64
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public async Task<string> DownloadObjectReturnBase64Async(string bucketName, string objectName)
        {
            try
            {
                MemoryStream memory = new MemoryStream();
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                bucketName = bucketName ?? _defaultBucketName;
                await _minio.StatObjectAsync(bucketName, objectName);

                // Get input stream to have content of 'my-objectname' from 'my-bucketname'
                await _minio.GetObjectAsync(bucketName, objectName, (stream) =>
                {
                    stream.CopyTo(memory);
                });
                return Base64Convert.ConvertMemoryStreamToBase64(memory);
            }
            catch (MinioException e)
            {
                throw new Exception(e.ServerMessage);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// Downloads and saves the object as a file in the local filesystem
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task DownloadObjectAsync(string bucketName, string objectName, string filename)
        {
            try
            {
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                bucketName = bucketName ?? _defaultBucketName;
                await _minio.StatObjectAsync(bucketName, objectName);
                // Get input stream to have content of 'my-objectname' from 'my-bucketname'
                await _minio.GetObjectAsync(bucketName, objectName, filename); //string with file path
            }
            catch (MinioException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Generates a presigned URL for HTTP GET operations
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="expiresInt"></param>
        /// <returns></returns>
        public async Task<string> GetObjectPresignUrlAsync(string bucketName, string objectName, int expiresInt = 3600)
        {
            try
            {
                if (expiresInt <= 0)
                {
                    expiresInt = 3600;
                }
                if (expiresInt > 604800)
                {
                    expiresInt = 604800;
                }
                bucketName = bucketName ?? _defaultBucketName;
                string url = await _minio.PresignedGetObjectAsync(bucketName, objectName, expiresInt);
                return url.Replace(_endpointUrl, _cdnUrl);
            }
            catch (MinioException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Uploads contents from a stream to objectName
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="objectName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<MinIOFileUploadResult> UploadDocumentAsync(string bucketName, string fileName, Stream data, bool isCustomSubFolder = true)
        {
            try
            {
                bucketName = bucketName ?? _defaultBucketName;
                bucketName = bucketName.ToLower();
                if (bucketName.Length < 3)
                {
                    bucketName = "bn-" + bucketName;
                }
                string objectName = "";
                if (isCustomSubFolder)
                {
                    var time = DateTime.Now;
                    var subFolder = $"{time.Year}/{time.Month}/{time.Day}/";
                    objectName = subFolder + fileName;
                }
                else
                {
                    objectName = fileName;
                }

                var contentType = MimeUtility.GetMimeMapping(fileName);
                data.Position = 0;

                // Make a bucket on the server, if not already present.
                bool found = await _minio.BucketExistsAsync(bucketName);
                if (!found)
                {
                    await _minio.MakeBucketAsync(bucketName);
                }
                if (_useSSL)
                {
                    //Server - side encryption with customer provided keys(SSE - C)
                    Aes aesEncryption = Aes.Create();
                    aesEncryption.KeySize = 256;
                    aesEncryption.GenerateKey();
                    var ssec = new SSEC(aesEncryption.Key);

                    await _minio.PutObjectAsync(bucketName, objectName, data, data.Length, contentType, null, ssec);
                }
                else
                {
                    await _minio.PutObjectAsync(bucketName, objectName, data, data.Length, contentType);
                }
                return new MinIOFileUploadResult
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    FileName = fileName
                };
            }
            catch (MinioException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string RenameFile(string filename)
        {
            var dateNow = DateTime.Now;
            filename = Regex.Replace(filename, @"\s", "_");
            // Generate new file name
            var fileSubfix = $"{dateNow:yyMMddHHmmss}" + new Random(DateTime.Now.Millisecond).Next(10, 99);
            var newFileName = Path.GetFileNameWithoutExtension(filename) + "_" + fileSubfix +
                              Path.GetExtension(filename);
            // Check length
            if (newFileName.Length > 255)
            {
                var withoutExtName = Path.GetFileNameWithoutExtension(filename);
                var extName = Path.GetExtension(filename);
                var trimmed = withoutExtName.Substring(0,
                    withoutExtName.Length - (255 - fileSubfix.Length - extName.Length));
                newFileName = trimmed + "_" + fileSubfix + "_" + Path.GetExtension(filename);
            }
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(filename)))
            {
                return Path.GetDirectoryName(filename).Replace("\\","/") + "/" + newFileName;
            }
            return newFileName;
        }      
    }

    public class MinIOFileUploadResult
    {
        public string BucketName { get; set; }
        public string ObjectName { get; set; }
        public string FileName { get; set; }
        public TemplateFileType FileType { get; set; }
    }

    public class DocumentFileTemplateUploadResult
    {
        public string BucketName { get; set; }
        public string ObjectName { get; set; }
        public string DataBucketName { get; set; }
        public string DataObjectName { get; set; }
        public string FileName { get; set; }
        public TemplateFileType FileType { get; set; } = TemplateFileType.PDF;
    }
}
