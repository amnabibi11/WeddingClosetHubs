using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class CustomizationRequest
    {
        public int CustomizationRequestId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [Required]
        public int ShopId { get; set; }

        [ForeignKey("ShopId")]
        public virtual Shop? Shop { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? Color { get; set; }

        [Required]
        public string Measurements { get; set; } = string.Empty;

        public string? ReferenceImages { get; set; }

        public string? CustomerNotes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShopkeeperPrice { get; set; }

        public string? ShopkeeperNotes { get; set; }
        public string? CustomerResponseNotes { get; set; }
        public string? CustomerUpdatedMeasurements { get; set; }
        public DateTime? CustomerResponseDate { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }
    }
}