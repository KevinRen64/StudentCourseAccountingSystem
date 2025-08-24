using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Uxtrata.Models;

namespace Uxtrata.Services
{
    public class AccountingService
    {
        public readonly SchoolContext db;
        private readonly int AR, CASH, REV;

        public AccountingService(SchoolContext context)
        {
            db = context;
            AR = db.Accounts.FirstOrDefault(a => a.Code == "AR").Id;
            CASH = db.Accounts.FirstOrDefault(a => a.Code == "CASH").Id;
            REV = db.Accounts.FirstOrDefault(a => a.Code == "REV").Id;
        }

        public async Task PostEnrollmentAsync(int studentId, int courseId, int selectionId, decimal amount)
        {
            var now = DateTime.UtcNow;
            db.LedgerEntries.Add(new LedgerEntry
            {
                TxnDate = now,
                AccountId = AR,
                Debit = amount,
                Credit = 0,
                StudentId = studentId,
                CourseId = courseId,
                CourseSelectionId = selectionId,
                Description = "Enrollment charge"
            });
            db.LedgerEntries.Add(new LedgerEntry
            {
                TxnDate = now,
                AccountId = REV,
                Debit = 0,
                Credit = amount,
                StudentId = studentId,
                CourseId = courseId,
                CourseSelectionId = selectionId,
                Description = "Tuition revenue"
            });

            await db.SaveChangesAsync();
        }

        public async Task PostPaymentAsync(int studentId, decimal amount, int? selectionId=null)
        {
            var now = DateTime.UtcNow;
            db.LedgerEntries.Add(new LedgerEntry
            {
                TxnDate = now,
                AccountId = CASH,
                Debit = amount,
                Credit = 0,
                StudentId = studentId,
                CourseSelectionId = selectionId,
                Description = "Student payment"
            });
            db.LedgerEntries.Add(new LedgerEntry
            {
                TxnDate = now,
                AccountId = AR,
                Debit = 0,
                Credit = amount,
                StudentId = studentId,
                Description = "Payment applied to account"
            });
            await db.SaveChangesAsync();
        }

        public decimal GetStudentBalance(int studentId)
        {
            int arAccountId = db.Accounts.Single(a => a.Code == "AR").Id;

            var deb = db.LedgerEntries
                .Where(x => x.StudentId == studentId && x.AccountId == arAccountId)
                .Sum(x => (decimal?)x.Debit) ?? 0m;

            var cre = db.LedgerEntries
                .Where(x => x.StudentId == studentId && x.AccountId == arAccountId)
                .Sum(x => (decimal?)x.Credit) ?? 0m;

            return deb - cre; // amount owed
        }

    }
}