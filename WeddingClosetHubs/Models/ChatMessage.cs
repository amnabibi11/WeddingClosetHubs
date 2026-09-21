
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class ChatMessage
    {
        public int ChatMessageId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public virtual User? Sender { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public virtual User? Receiver { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime SentDate { get; set; } = DateTime.Now;
    }
}