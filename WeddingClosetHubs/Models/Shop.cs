using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Shop
    {
        public int ShopId { get; set; }


        [Required]
        public int ShopkeeperId { get; set; }


        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(100)]
        public string ShopName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Shop category is required")]
        [StringLength(100)]
        public string ShopCategory { get; set; } = string.Empty;


        
        [StringLength(500)]
        public string? ShopCollections { get; set; }


        [StringLength(500)]
        public string? ShopDescription { get; set; }


        [Required(ErrorMessage = "Shop address is required")]
        public string ShopAddress { get; set; } = string.Empty;


        public string? ShopPhone { get; set; }


        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }


        public string? Logo { get; set; }

        public string? ShopFrontPhoto { get; set; }

        public string? ShopInsidePhoto { get; set; }

        public string? ShopSignboardPhoto { get; set; }


        public string? WhatsAppNumber { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? TikTokUrl { get; set; }

        public string? WebsiteUrl { get; set; }


        [StringLength(50)]
        public string? ShopProofType { get; set; }

        public string? ShopProofDocument { get; set; }


        public string VerificationStatus { get; set; } = "Pending";


        public string? AdminNotes { get; set; }


        public string? RejectionReason { get; set; }


        public DateTime? VerificationSubmittedDate { get; set; }


        public DateTime? VerifiedDate { get; set; }


        public bool Status { get; set; } = false;


        public bool IsApproved { get; set; } = false;


        public DateTime? ApprovedDate { get; set; }


        public DateTime CreatedDate { get; set; } = DateTime.Now;


        [ForeignKey("ShopkeeperId")]
        public virtual User? Shopkeeper { get; set; }
    }
}