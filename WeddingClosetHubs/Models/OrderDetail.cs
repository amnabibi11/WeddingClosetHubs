
using System;
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

        [Column(TypeName = "decimal(18,2)")]
        public decimal RentalSecurity { get; set; }

        public string? RentalDuration { get; set; }

        public string? RentalConditions { get; set; }

        public string RentalReturnStatus { get; set; } = "Not Required";

        public string? ReturnMethod { get; set; }

        public DateTime? ReturnRequestedDate { get; set; }

        public DateTime? ReturnPickedUpDate { get; set; }

        public DateTime? DressReceivedDate { get; set; }

        public DateTime? DressReturnedDate { get; set; }

        public string? ReturnNotes { get; set; }

        public string? InspectionResult { get; set; }

        public string? InspectionNotes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DamageDeduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        public DateTime? SecurityReadyDate { get; set; }

        [StringLength(30)]
        public string? RefundPreferenceMethod { get; set; }

        [StringLength(50)]
        public string? RefundPreferenceAccount { get; set; }

        public bool SecurityRefunded { get; set; } = false;

        public DateTime? SecurityRefundDate { get; set; }

        public string? SecurityRefundMethod { get; set; }

        public string? SecurityRefundAccount { get; set; }

        public string? SecurityRefundTransactionId { get; set; }

        public string? SecurityRefundNotes { get; set; }
    }
}