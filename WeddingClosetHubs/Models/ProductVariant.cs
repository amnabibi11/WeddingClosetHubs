using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class ProductVariant
    {
        public int ProductVariantId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;

        [Range(
            0,
            int.MaxValue,
            ErrorMessage = "Stock quantity cannot be negative.")]
        public int StockQuantity { get; set; }

       

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}