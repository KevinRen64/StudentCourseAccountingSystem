using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Uxtrata.Models.ViewModels
{
    public class StudentStatement
    {
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public decimal CourseCost { get; set; }
        public DateTime? PaymentDate { get; set; }   // nullable, since not all rows will have a date
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
    }
}