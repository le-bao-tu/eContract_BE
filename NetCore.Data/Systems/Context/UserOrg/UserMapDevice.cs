using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("user_map_device")]
    public class UserMapDevice
    {
        public UserMapDevice()
        {
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("device_id")]
        public string DeviceId { get; set; }

        [Column("device_name")]
        public string DeviceName { get; set; }

        [Column("isIdentifierDevice")]
        public bool IsIdentifierDevice { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
