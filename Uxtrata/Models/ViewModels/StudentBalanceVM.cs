using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Uxtrata.Models.ViewModels
{
    public class StudentBalanceVM
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public decimal Charges { get; set; }   
        public decimal Payments { get; set; }  
        public decimal Balance => Charges - Payments;
    }

}