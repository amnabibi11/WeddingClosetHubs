using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class CustomerCartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? ProductId { get; set; }

        public int? CustomizationRequestId { get; set; }

        public string? ProductName { get; set; }

        public string? CustomImage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string? Size { get; set; }

        public string? CustomMeasurements { get; set; }

        public string PurchaseType { get; set; } = "Buy";

        public int? NegotiationId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}