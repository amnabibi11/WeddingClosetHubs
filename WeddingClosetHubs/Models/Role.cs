using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WeddingClosetHubs.Models;

namespace WeddingClosetHubs.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}