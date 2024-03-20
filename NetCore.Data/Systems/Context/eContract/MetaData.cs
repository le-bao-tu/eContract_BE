using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace NetCore.Data
{
    [Table("meta_data")]
    public class MetaData : BaseTableWithOrganization
    {
        public MetaData()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [StringLength(64)]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [StringLength(128)]
        [Column("name")]
        public string Name { get; set; }

        /*
         Kiểu dữ liệu:
            1: Kiểu số
            2: Kiểu chữ
            3. Kiểu ngày tháng
            4. Kiểu radio
            5. Kiểu checkbox
         */
        [Column("data_type")]
        public MetaDataType DataType { get; set; }

        [Column("is_require")]
        public bool IsRequire { get; set; } = false;

        [Column("list_data_json")]
        public string ListDataJson
        {
            get
            {
                return ListData == null ? null : JsonSerializer.Serialize(ListData);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    ListData = null;
                else
                    ListData = JsonSerializer.Deserialize<List<MetaDataList>>(value);
            }
        }

        [NotMapped]
        public List<MetaDataList> ListData { get; set; }
    }

    public class MetaDataList
    {
        public string Value { get; set; }

        public string Label { get; set; }
    }

    public enum MetaDataType
    {
        NUMBER = 1,
        TEXT = 2,
        DATETIME = 3,
        RADIO = 4,
        CHECKBOX = 5,
    }
}
