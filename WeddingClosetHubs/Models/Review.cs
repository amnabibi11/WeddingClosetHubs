using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
public class Review
{
public int ReviewId { get; set; }


    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [Required]
    public int ShopId { get; set; }

    [ForeignKey("ShopId")]
    public virtual Shop? Shop { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }


    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    [StringLength(50)]
    public string ReviewType { get; set; } = "Product";

    public bool IsApproved { get; set; } = true;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}


}
