using NetCore.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetCore.Data
{
    [Table("user_map_firebase_token")]
    public class UserMapFirebaseToken
    {
        public UserMapFirebaseToken()
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

        [Column("firebase_token")]
        public string FirebaseToken { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
