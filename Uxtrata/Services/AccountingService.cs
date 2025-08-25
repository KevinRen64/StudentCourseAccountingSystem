using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Uxtrata.Models;

namespace Uxtrata.Services
{
    /// <summary>
    /// Provides accounting-related operations such as posting enrollments and payments,
    /// Handles double-entry bookkeeping to ensure accurate financial records.
    /// </summary>
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


        // Posts an enrollment transaction:
        // Debut AR (student owes money), Credit REV (school earns revenue)
        public async Task PostEnrollmentAsync(int studentId, int courseId, int selectionId, decimal amount)
        {
            var now = DateTime.UtcNow;

            // Debit AR (increase receivable)
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

            // Credit REV (increase revenue)
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


        // Posts a payment transaction:
        // Debit CASH (increase assets), Credit AR (decrease receivable)
        public async Task PostPaymentAsync(int studentId, decimal amount, int? selectionId=null)
        {
            var now = DateTime.UtcNow;

            // Debit CASH (student paid money)
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

            // Credit AR (reduce amount student owes)
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


        // Calculates the current balance for a student.
        // Balance = Total AR Debits - Total AR Credits
        public decimal GetStudentBalance(int studentId)
        {

            var deb = db.LedgerEntries
                .Where(x => x.StudentId == studentId && x.AccountId == AR)
                .Sum(x => (decimal?)x.Debit) ?? 0m;

            var cre = db.LedgerEntries
                .Where(x => x.StudentId == studentId && x.AccountId == AR)
                .Sum(x => (decimal?)x.Credit) ?? 0m;

            return deb - cre; 
        }

    }
}