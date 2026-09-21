using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }


        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [Required]
        public int ShopId { get; set; }

        [ForeignKey("ShopId")]
        public virtual Shop? Shop { get; set; }

        public int? DeliveryId { get; set; }

        [ForeignKey("DeliveryId")]
        public virtual DeliveryBoy? Delivery { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; }
            = "Cash on Delivery";


        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; }
            = "Pending";


        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryCharges { get; set; } = 300;


        [Column(TypeName = "decimal(18,2)")]
        public decimal Commission { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal ShopkeeperAmount { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryBoyAmount { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string ShopkeeperPaymentStatus { get; set; }
            = "Pending";


        [Required]
        [StringLength(50)]
        public string DeliveryPaymentStatus { get; set; }
            = "Pending";

        public DateTime? PaymentReceivedDate { get; set; }

        public DateTime? ShopkeeperPaidDate { get; set; }

        public DateTime? DeliveryPaidDate { get; set; }


        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }


        public DateTime CreatedDate { get; set; }
            = DateTime.Now;
    }
}