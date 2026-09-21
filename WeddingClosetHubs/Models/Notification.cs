using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;


        [StringLength(50)]
        public string Type { get; set; } = "General";


        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }


        public bool IsRead { get; set; } = false;


        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}