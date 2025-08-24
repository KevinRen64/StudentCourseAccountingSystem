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
    public class PaymentController : Controller
    {
        private readonly SchoolContext db = new SchoolContext();


        // ---------- helpers ----------
        // Populate dropdowns for Students and Courses
        // Called in Create/Edit GET and POST to build SelectLists for the views
        private async Task PopulateStudentDropDownsAsync(int? selectedId = null)
        {
            var students = await db.Students.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
            ViewBag.StudentId = new SelectList(students, "StudentId", "Name", selectedId);
            if (selectedId.HasValue)
            {
                var acct = new AccountingService(db);
                ViewBag.CurrentBalance = acct.GetStudentBalance(selectedId.Value);
            }
            else
            {
                ViewBag.CurrentBalance = null;
            }
        }


        // ---------- list / details ----------
        // GET: Payment
        // Show all course payment with related Student info
        public async Task<ActionResult> Index()
        {
            var payments = await db.Payments
                                     .AsNoTracking()
                                     .Include(cs => cs.Student)  //eager load student data
                                     .ToListAsync();
            var acct = new AccountingService(db);
            var studentIds = payments.Select(p => p.StudentId).Distinct().ToList();
            var balances = new Dictionary<int, decimal>(studentIds.Count);
            foreach (var id in studentIds)
                balances[id] = acct.GetStudentBalance(id);  // use your helper

            ViewBag.Balances = balances;
            return View(payments);
        }

        // GET: Payment/Details/{id}
        // Show details for one payment
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

        // ---------- create ----------
        // GET: Payment/Create
        // Show form with dropdowns for Student
        public async Task<ActionResult> Create(int? studentId = null)
        {
            await PopulateStudentDropDownsAsync(studentId);
            // Compute current balance if a student is preselected
            if (studentId.HasValue)
            {
                var acct = new AccountingService(db);                 // if you have it
                ViewBag.CurrentBalance = acct.GetStudentBalance(studentId.Value);
            }
            else
            {
                ViewBag.CurrentBalance = null;
            }

            var model = new Payment
            {
                PaidAt = DateTime.UtcNow,
                StudentId = studentId ?? 0
            };
            return View(model);
        }

        // POST: Payment/Create
        // Save new enrollment if not duplicate
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "PaymentId,StudentId,Amount,PaidAt,Reference")] Payment payment)
        {
            if (payment.PaidAt == default) payment.PaidAt = DateTime.UtcNow;

            // Basic validation
            if (payment.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Payment must be greater than zero.");
            }

            // Balance (for UX: show it when redisplaying the form)
            decimal? balanceBefore = null;
            if (payment.StudentId > 0)
            {
                var acct = new AccountingService(db);                 // if you have it
                balanceBefore = acct.GetStudentBalance(payment.StudentId);

                // Optional: warn if overpaying (allow or block — your call).
                if (payment.Amount > balanceBefore)
                {
                    ModelState.AddModelError("Amount", $"Warning: amount exceeds current balance ({balanceBefore.Value.ToString("C")}).");
                    // If you'd rather BLOCK instead of warn, keep the error but also prevent saving.
                }
            }

            // If validation fails, reload dropdowns and show form again
            if (!ModelState.IsValid)
            {
                await PopulateStudentDropDownsAsync(payment.StudentId);
                return View(payment);
            }

            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            // (Optional) Post to your ledger if you implemented double-entry
            await new AccountingService(db).PostPaymentAsync(payment.StudentId, payment.Amount, null);
            TempData["Toast"] = $"Payment of {payment.Amount.ToString("C")} recorded.";
            return RedirectToAction("Dashboard", "Student", new { id = payment.StudentId });

        }

        // ---------- edit ----------
        // GET: Payment/Edit/{id}
        // Show form for editing an existing enrollment
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var payment = await db.Payments.FindAsync(id);
            if (payment == null) return HttpNotFound();

            await PopulateStudentDropDownsAsync(payment.StudentId);
            return View(payment);
        }

        // POST: Payment/Edit/{id}
        // Update enrollment, ensuring no duplicates
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "PaymentId,StudentId,Amount,PaidAt,Reference")] Payment payment)
        {
            // If validation fails, reload dropdowns and show form again
            if (!ModelState.IsValid)
            {
                await PopulateStudentDropDownsAsync(payment.StudentId);
                return View(payment);
            }

            db.Entry(payment).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ---------- delete ----------
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