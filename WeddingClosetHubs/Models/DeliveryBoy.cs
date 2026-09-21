using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class DeliveryBoy
    {
        public int DeliveryBoyId { get; set; }


        
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }


        [Required]
        [StringLength(20)]
        public string CNIC { get; set; } = string.Empty;

        public string? CNICFrontImage { get; set; }

        public string? CNICBackImage { get; set; }

        [Required]
        [StringLength(50)]
        public string MotorbikeNumber { get; set; } = string.Empty;
 

        [StringLength(50)]
        public string? PreferredZone { get; set; }

        [StringLength(50)]
        public string? AssignedZone { get; set; }


        [Required]
        [StringLength(50)]
        public string VerificationStatus { get; set; } = "Pending";

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public string? PaymentMethod { get; set; }

        public string? PaymentAccount { get; set; }
        
        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedDate { get; set; }
            = DateTime.Now;
    }
}