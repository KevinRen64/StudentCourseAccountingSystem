using System;
using System.ComponentModel.DataAnnotations;


namespace Uxtrata.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // Foreign Key, link to a specific student
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }

        // Reference number for the payment
        public string Reference { get; set; }

        // The Student entity for this payment
        public virtual Student Student { get; set; }
    }
}