using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Uxtrata.Models
{
    public class Account
    {

        public int Id { get; set; }

        [Required, StringLength(16)]
        public string Code { get; set; } // "AR", "CASH", "REV"

        [Required, StringLength(64)]
        public string Name { get; set; }
    }
}