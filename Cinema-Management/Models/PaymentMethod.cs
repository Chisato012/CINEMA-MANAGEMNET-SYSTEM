using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema_Management.Models
{
    [Table("PaymentMethods")]
    public class PaymentMethod
    {
        [Key]
        public int MethodID { get; set; }

        [Required]
        [StringLength(100)]
        public string MethodName { get; set; } = string.Empty;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
