using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Negotiation
    {
        [Key]
        public int NegotiationId { get; set; }

        public int ProductId { get; set; }

        public int CustomerId { get; set; }

        public int ShopkeeperId { get; set; }

        public decimal OriginalPrice { get; set; }

        public decimal RequestedPrice { get; set; }

        public decimal? CounterPrice { get; set; }

        public decimal? AgreedPrice { get; set; }

        public string? CustomerMessage { get; set; }

        public string? ShopkeeperMessage { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? RespondedDate { get; set; }


        

        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual User? Customer { get; set; }

        [ForeignKey(nameof(ShopkeeperId))]
        public virtual User? Shopkeeper { get; set; }
    }
}