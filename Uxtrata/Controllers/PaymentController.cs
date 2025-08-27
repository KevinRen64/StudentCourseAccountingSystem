using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Uxtrata.Models;
using Uxtrata.Services;

namespace Uxtrata.Controllers
{
    /// <summary>
    /// Manages student payments
    /// Note: Creating payments also creates ledger entries via AccountingService
    /// Note: Editing/deleting payments does NOT adjust ledger entries, as this is a simplified example
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly SchoolContext db = new SchoolContext();


        // ---------- helpers ----------
        // Populate Student dropdown for Create/Edit forms
        private async Task PopulateStudentDropDownsAsync(int? selectedId = null)
        {
            var students = await db.Students.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

            ViewBag.StudentId = new SelectList(students, "StudentId", "Name", selectedId);

            if (selectedId.HasValue)
            {
                // Display current balance for selected student
                var acct = new AccountingService(db);
                ViewBag.CurrentBalance = acct.GetStudentBalance(selectedId.Value);
            }
            else
            {
                ViewBag.CurrentBalance = null;
            }
        }


        // GET: Payment
        // List all course payment with related Student info
        // Also computes current balance for each student with payments
        public async Task<ActionResult> Index()
        {
            var payments = await db.Payments
                                     .AsNoTracking()
                                     .Include(cs => cs.Student)  
                                     .ToListAsync();

            // Compute balances for each student with payments
            var acct = new AccountingService(db);
            var studentIds = payments.Select(p => p.StudentId).Distinct().ToList();
            var balances = new Dictionary<int, decimal>(studentIds.Count);
            foreach (var id in studentIds)
                balances[id] = acct.GetStudentBalance(id);  

            ViewBag.Balances = balances;
            return View(payments);
        }

        // GET: Payment/Details/{id}
        // Show details for one payment (400 if id missing, 404 if not found)
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var selection = await db.Payments
                                    .AsNoTracking()
                                    .Include(cs => cs.Student)
                                    .FirstOrDefaultAsync(cs => cs.PaymentId == id);

            if (selection == null) return HttpNotFound();
            return View(selection);
        }


        // GET: Payment/Create
        // Render form for creating a new payment
        // If a student is preselected, show their current balance
        public async Task<ActionResult> Create(int? studentId = null)
        {
            await PopulateStudentDropDownsAsync(studentId);

            //// Compute current balance if a student is preselected
            //if (studentId.HasValue)
            //{
            //    var acct = new AccountingService(db);                 // if you have it
            //    ViewBag.CurrentBalance = acct.GetStudentBalance(studentId.Value);
            //}
            //else
            //{
            //    ViewBag.CurrentBalance = null;
            //}

            var model = new Payment
            {
                PaidAt = DateTime.UtcNow,
                StudentId = studentId ?? 0
            };
            return View(model);
        }


        // POST: Payment/Create
        // Handles creation of a new payment and posting to ledger
        // Security: [ValidateAntiForgeryToken] protects against CSRF
        // Security: Limits model binding to whitelisted fields
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "PaymentId,StudentId,Amount,PaidAt,Reference")] Payment payment)
        {
            if (payment.PaidAt == default) payment.PaidAt = DateTime.UtcNow;

            // 1. Basic validation: payment must be > 0
            if (payment.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Payment must be greater than zero.");
            }

            // 2. For UX, compute balance before payment so we can warn about overpayment
            decimal? balanceBefore = null;
            if (payment.StudentId > 0)
            {
                var acct = new AccountingService(db);                 
                balanceBefore = acct.GetStudentBalance(payment.StudentId);

                // Warn if payment exceeds balance
                if (payment.Amount > balanceBefore)
                {
                    ModelState.AddModelError("Amount", $"Warning: amount exceeds current balance ({balanceBefore.Value.ToString("C")}).");
                }
            }

            // 3. If validation fails, reload dropdowns and show form again
            if (!ModelState.IsValid)
            {
                await PopulateStudentDropDownsAsync(payment.StudentId);
                return View(payment);
            }

            //4. Use a db transaction to ensure both payment and ledger posting succeed or fail together
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // Insert the payment record
                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();

                    // Post to Ledger (Debit CASH, Credit AR)
                    var acct = new AccountingService(db);
                    await acct.PostPaymentAsync(payment.StudentId, payment.Amount, payment.PaymentId);

                    tx.Commit();

                    // 5) On success, redirect back to the student's dashboard
                    return RedirectToAction("Dashboard", "Student", new { id = payment.StudentId });
                }
                catch 
                {
                    tx.Rollback();

                    // Show error and reload form
                    ModelState.AddModelError("", "Failed to save payment and post ledger. No changes were saved.");
                    await PopulateStudentDropDownsAsync(payment.StudentId);
                    return View(payment);
                }
            }
        }


        // GET: Payment/Edit/{id}
        // Loads the edit form for an existing payment; 404/400 as needed
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var payment = await db.Payments.FindAsync(id);
            if (payment == null) return HttpNotFound();

            await PopulateStudentDropDownsAsync(payment.StudentId);
            return View(payment);
        }


        // POST: Payment/Edit/{id}
        // Saves edits to an existing payment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "PaymentId,StudentId,Amount,PaidAt,Reference")] Payment payment)
        {
            
            if (!ModelState.IsValid)
            {
                await PopulateStudentDropDownsAsync(payment.StudentId);
                return View(payment);
            }

            db.Entry(payment).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // GET: Payment/Delete/{id}
        // Show confirmation page
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var payment = await db.Payments
                                    .AsNoTracking()
                                    .Include(cs => cs.Student)
                                    .FirstOrDefaultAsync(cs => cs.PaymentId == id);
            if (payment == null) return HttpNotFound();

            return View(payment);
        }

        // POST: Payment/Delete/5
        // Delete confirmed
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var payment = await db.Payments.FindAsync(id);
            if (payment == null) return HttpNotFound();

            db.Payments.Remove(payment);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // ---------- cleanup ----------
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}