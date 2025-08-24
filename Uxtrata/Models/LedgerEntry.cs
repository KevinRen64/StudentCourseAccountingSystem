using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Uxtrata.Models
{
    public class LedgerEntry
    {
        public int Id { get; set; }

        [Required]
        public DateTime TxnDate { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Column(TypeName = "decimal")]
        [Range(0, 99999999.99)]
        public decimal Debit { get; set; }

        [Column(TypeName = "decimal")]
        [Range(0, 99999999.99)]
        public decimal Credit { get; set; }
        
        //references for reporting
        public int? StudentId { get; set; }
        public int? CourseId { get; set; }
        public int? CourseSelectionId { get; set; }

        [StringLength(256)]
        public string Description { get; set; }

        //Navigation properties
        public virtual Account Account { get; set; }
        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }
        public virtual CourseSelection CourseSelection { get; set; }



    }
}