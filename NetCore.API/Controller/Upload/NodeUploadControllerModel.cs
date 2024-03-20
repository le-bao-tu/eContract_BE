using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace NetCore.API
{
    /// <summary>
    /// Dữ liệu upload blob
    /// </summary>
    public class NodeUploadManyRequest
    {
        /// <summary>
        /// Danh sách dữ liệu metadata mỗi file
        /// </summary>
        public string MetadataJson { get; set; }
        /// <summary>
        /// Danh sách file tải lên
        /// </summary>
        public IFormFileCollection Files { get; }
        /// <summary>
        /// Nhãn
        /// </summary>
        public string Lablel { get; set; }
        /// <summary>
        /// Tags
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// Đánh dấu sao
        /// </summary>
        public bool Star { get; set; }
    }

    /// <summary>
    /// Dữ liệu upload blob
    /// </summary>
    public class NodeUploadRequest
    {
        /// <summary>
        /// Danh sách dữ liệu metadata mỗi file
        /// </summary>
        public string MetadataJson { get; set; }
        /// <summary>
        ///File tải lên
        /// </summary>
        public IFormFile File { get; set; }
        /// <summary>
        /// Nhãn
        /// </summary>
        public string Lablel { get; set; }
        /// <summary>
        /// Tags
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// Đánh dấu sao
        /// </summary>
        public bool Star { get; set; }
    }
    /// <summary>
    /// Dữ liệu upload base64
    /// </summary>
    public class NodeUploadBase64ManyRequest
    {
        /// <summary>
        /// Danh sách dữ liệu file
        /// </summary>
        public List<Base64FileData> ListFileData { get; set; }
        /// <summary>
        /// Danh sách dữ liệu metadata mỗi file
        /// </summary>
        public string MetadataJson { get; set; }
        /// <summary>
        /// Nhãn
        /// </summary>
        public string Lablel { get; set; }
        /// <summary>
        /// Tags
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// Đánh dấu sao
        /// </summary>
        public bool Star { get; set; }
    }
    /// <summary>
    /// Dữ liệu upload base64
    /// </summary>
    public class NodeUploadBase64Request
    {
        /// <summary>
        /// Dữ liệu file
        /// </summary>
        public Base64FileData FileData { get; set; }
        /// <summary>
        /// Danh sách dữ liệu metadata mỗi file
        /// </summary>
        public string MetadataJson { get; set; }
        /// <summary>
        /// Nhãn
        /// </summary>
        public string Lablel { get; set; }
        /// <summary>
        /// Tags
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// Đánh dấu sao
        /// </summary>
        public bool Star { get; set; }
    }

    /// <summary>
    /// Dữ liệu kết quả upload vào tạo node
    /// </summary>
    public class NodeUploadResult
    {
        /// <summary>
        /// Id của file
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Tên file
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tên vật lý
        /// </summary>
        public string PhysicalName { get; set; }
        /// <summary>
        /// Kích cỡ tập tin
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// Định dạng tập tin
        /// </summary>
        public string Extension { get; set; }
        /// <summary>
        /// Đường đẫn logic
        /// </summary>
        public string Path { get; set; }
    }
    /// <summary>
    /// Dữ liệu kết quả upload vật lý
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// Tên vật lý
        /// </summary>
        public string PhysicalName { get; set; }
        /// <summary>
        /// Tên file vật lý
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Dung lượng file
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// Loại file
        /// </summary>
        public string Extension { get; set; }
        /// <summary>
        /// Đường dẫn tương đói vật lý
        /// </summary>
        public string PhysicalPath { get; set; }
        /// <summary>
        /// Đường đẫn tuyệt đối vật lý
        /// </summary>
        public string RelativePath { get; set; }
    }
    /// <summary>
    /// NodeUploadQueryBodyFileData
    /// </summary>
    public class Base64FileData
    {
        /// <summary>
        /// Tiêu đề
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Dữ liệu dạng base64
        /// </summary>
        public string FileData { get; set; }
    }
}



