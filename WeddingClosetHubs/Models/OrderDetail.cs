using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }


        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public string? CustomMeasurements { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        public string PurchaseType { get; set; } = "Buy";

        public decimal RentalSecurity { get; set; }

        public string? RentalDuration { get; set; }

        public string? RentalConditions { get; set; }
    }
}