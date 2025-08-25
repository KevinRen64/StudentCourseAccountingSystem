using Mysqlx.Crud;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Uxtrata.Models
{
    /// <summary>
    /// AR - Accounts Receivable (money owed by students)
    /// CASH - Cash amount (payments received)
    /// REV - Revenue (course income)
    /// INSERT INTO Accounts (Code, Name) VALUES ('AR', 'Accounts Receivable');
    /// INSERT INTO Accounts(Code, Name) VALUES('CASH', 'Cash');
    /// INSERT INTO Accounts(Code, Name) VALUES('REV', 'Tuition Revenue');
    /// </summary>
    public class Account
    {

        public int Id { get; set; }

        [Required, StringLength(16)]
        public string Code { get; set; } // "AR", "CASH", "REV"

        [Required, StringLength(64)]
        public string Name { get; set; }
    }
}