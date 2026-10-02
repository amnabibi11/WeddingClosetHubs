using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class ProductImage
    {
        public int ProductImageId { get; set; }


        [Required]
        public int ProductId { get; set; }


        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }


        [Required]
        public string ImagePath { get; set; } = string.Empty;


        public bool IsPrimary { get; set; }


        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}