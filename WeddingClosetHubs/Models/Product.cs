using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Product
    {
        public int ProductId { get; set; }


        // =====================================================
        // SHOP
        // =====================================================

        [Required]
        public int ShopId { get; set; }

        [ForeignKey("ShopId")]
        public virtual Shop? Shop { get; set; }


        // =====================================================
        // PRODUCT INFORMATION
        // =====================================================

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;


        [StringLength(1000)]
        public string? Description { get; set; }


        // =====================================================
        // MAIN CATEGORY
        // =====================================================

        [StringLength(100)]
        public string? Category { get; set; }


        // =====================================================
        // SUBCATEGORY
        // =====================================================

        [StringLength(100)]
        public string? Subcategory { get; set; }


        // =====================================================
        // OCCASION
        // =====================================================

        [StringLength(100)]
        public string? Occasion { get; set; }


        // =====================================================
        // SIZE
        // =====================================================

        [StringLength(50)]
        public string? Size { get; set; }


        // =====================================================
        // SIZE TYPE
        // Standard / Custom Measurement
        // =====================================================

        [StringLength(50)]
        public string? SizeType { get; set; }


        // =====================================================
        // CUSTOM MEASUREMENTS
        // =====================================================

        [StringLength(1000)]
        public string? CustomMeasurements { get; set; }


        // =====================================================
        // COLOR
        // =====================================================

        [StringLength(50)]
        public string? Color { get; set; }


        // =====================================================
        // PRICE
        // =====================================================

        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }


        // =====================================================
        // STOCK QUANTITY
        // =====================================================

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue,
            ErrorMessage = "Stock cannot be negative")]
        public int StockQuantity { get; set; }

        public string? Image { get; set; }


        public bool Status { get; set; } = true;
        public string? ProductType { get; set; }

        public bool HasCustomMeasurement { get; set; } = false;
        
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