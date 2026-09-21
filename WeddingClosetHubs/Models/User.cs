using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeddingClosetHubs.Models
{
    public class User
    {
        public int UserId { get; set; }


        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        public string? Phone { get; set; }


        public string? Address { get; set; }

        [Required(ErrorMessage = "Please select an account type")]
        public int? RoleId { get; set; }


        public string? ProfileImage { get; set; }



        [StringLength(30)]
        public string? PaymentMethod { get; set; }


        [StringLength(50)]
        public string? PaymentAccount { get; set; }


        public bool Status { get; set; } = true;


        public bool IsApproved { get; set; } = false;


        public DateTime? ApprovedDate { get; set; }


        public DateTime CreatedDate { get; set; } = DateTime.Now;


        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }


        public virtual Shop? Shop { get; set; }


        public virtual DeliveryBoy? DeliveryBoy { get; set; }
    }
}