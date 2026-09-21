using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class Order
    {
        public int OrderId { get; set; }


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
        public virtual User? Delivery { get; set; }


       

        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending";

        public string? DeliveryCity { get; set; }
        [Required]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;


        [StringLength(500)]
        public string? Notes { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryCharges { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShopkeeperAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; }
            = "Cash on Delivery";

        public bool PaymentReceived { get; set; } = false;

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? TransactionImage { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(50)]
        public string ShopkeeperPaymentStatus { get; set; }
            = "Pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShopkeeperPaidAmount { get; set; }

        [StringLength(30)]
        public string? ShopkeeperPaymentMethod { get; set; }

        [StringLength(50)]
        public string? ShopkeeperPaymentAccount { get; set; }

        public DateTime? ShopkeeperPaymentDate { get; set; }

        
        [StringLength(100)]
        public string? ShopkeeperTransactionId { get; set; }


        
        [StringLength(500)]
        public string? ShopkeeperPaymentNotes { get; set; }

        

        [StringLength(50)]
        public string DeliveryPaymentStatus { get; set; }
            = "Pending";


        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryCollectedAmount { get; set; }

        public DateTime? DeliveryCollectionDate { get; set; }

        public bool DeliveryCashHandedToAdmin { get; set; } = false;

        public DateTime? DeliveryCashHandoverDate { get; set; }


        [StringLength(500)]
        public string? DeliveryCashHandoverNotes { get; set; }

        

        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryPaidAmount { get; set; }


        
        [StringLength(30)]
        public string? DeliveryPaymentMethod { get; set; }

        [StringLength(50)]
        public string? DeliveryPaymentAccount { get; set; }

        [StringLength(100)]
        public string? DeliveryTransactionId { get; set; }


        [StringLength(500)]
        public string? DeliveryPaymentNotes { get; set; }
        

        public DateTime? DeliveryPaymentDate { get; set; }
    
        public DateTime CreatedDate { get; set; }
            = DateTime.Now;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
            = new List<OrderDetail>();
    }
}