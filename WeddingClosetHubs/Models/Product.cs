using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Product
    {
        public int ProductId { get; set; }


        [Required]
        public int ShopId { get; set; }

        [ForeignKey("ShopId")]
        public virtual Shop? Shop { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;


        [StringLength(1000)]
        public string? Description { get; set; }


        [StringLength(100)]
        public string? Category { get; set; }


        [StringLength(100)]
        public string? Subcategory { get; set; }


        [StringLength(100)]
        public string? Occasion { get; set; }


        [StringLength(50)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? SizeType { get; set; }


        [StringLength(1000)]
        public string? CustomMeasurements { get; set; }


        [StringLength(50)]
        public string? Color { get; set; }


        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }


        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Stock cannot be negative")]
        public int StockQuantity { get; set; }

        public string? Image { get; set; }


        public bool Status { get; set; } = true;
        public string? ProductType { get; set; }

        public bool HasCustomMeasurement { get; set; } = false;
        public bool IsAvailableForBuy { get; set; } = true;
        public bool IsOnSale { get; set; }

        public decimal? SalePrice { get; set; }

        public string? SaleDetails { get; set; }


        public bool IsAvailableForRent { get; set; }

        public decimal? RentPrice { get; set; }

        public decimal? RentalSecurity { get; set; }

        public string? RentalDuration { get; set; }

        public string? RentalConditions { get; set; }


        public bool AllowNegotiation { get; set; }
        
        public DateTime CreatedDate { get; set; }
            = DateTime.Now;
    }
}