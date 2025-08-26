using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Uxtrata.Models;
using Uxtrata.Models.ViewModels;
using Uxtrata.Services;

namespace Uxtrata.Controllers
{
    /// <summary>
    /// Manages CRUD, dashboard and reporting for Student entities
    /// NOTE: Students have related CourseSelections, Payments and LedgerEntries.
    /// Deleting a student touches multiple tables; use an explicit transaction for atomicity.
    /// </summary>
    public class StudentController : Controller
    {
        private readonly SchoolContext db = new SchoolContext();


        // GET: Students
        // Lists all students
        public async Task<ActionResult> Index()
        {
            var students = await db.Students.AsNoTracking().ToListAsync();
            return View(students);
        }


        // GET: Student/Details/{id}
        // Shows a single student by id; returns 400 if id missing, 404 if not found.
        // Not binding to any button.
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return HttpNotFound();
            return View(student);
        }


        // GET: Student/Create
        // Renders the empty Create form
        public ActionResult Create()
        {
            return View();
        }


        // POST: Student/Create
        // Creates a student from posted form values.
        // Security: [ValidateAntiForgeryToken] protects against CSRF
        // Security: Limits model binding to whitelisted fields
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "StudentId, Name, Age")] Student student)
        {
            // Re-display form if invalid
            if (!ModelState.IsValid) return View(student);
            db.Students.Add(student);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        //GET: Student/Edit/{id}
        // Show form to edit existing student
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return HttpNotFound();
            return View(student);
        }


        // POST: Student/Edit/{id}
        // Saves edited fields back to DB
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "StudentId, Name, Age")] Student student)
        { 
            if (!ModelState.IsValid) return View(student);

            // Mark as modified so EF generates an UPDATE
            db.Entry(student).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // GET: Student/Delete/{id}
        // Shows a confirmation page before deletion
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null) return HttpNotFound();
            return View(student);
        }


        // POST: Students/Delete/{id}
        // Actually remove student after confirmation
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var student = await db.Students
                .Include(s => s.CourseSelections)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return HttpNotFound();

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // 1) IDs of this student's course selections
                    var csIds = student.CourseSelections
                                       .Select(cs => cs.CourseSelectionId)
                                       .ToList();

                    // 2) Ledger entries that reference those course selections (nullable FK safe)
                    if (csIds.Any())
                    {
                        var ledgerBySelection = db.LedgerEntries
                            .Where(le => le.CourseSelectionId.HasValue &&
                                         csIds.Contains(le.CourseSelectionId.Value));
                        db.LedgerEntries.RemoveRange(ledgerBySelection);
                    }

                    // 3) Ledger entries that reference the student directly
                    var ledgerByStudent = db.LedgerEntries.Where(le => le.StudentId == id);
                    db.LedgerEntries.RemoveRange(ledgerByStudent);

                    // 4) Payments for this student (if applicable)
                    var payments = db.Payments.Where(p => p.StudentId == id);
                    db.Payments.RemoveRange(payments);

                    // 5) Course selections for this student
                    var selections = db.CourseSelections.Where(cs => cs.StudentId == id);
                    db.CourseSelections.RemoveRange(selections);

                    // 6) Finally delete the student
                    db.Students.Remove(student);

                    await db.SaveChangesAsync();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }

            return RedirectToAction("Index");
        }



        // ---------- dashboard ----------
        // GET: Students/Dashboard/{id}
        // Shows student details, balance, enrollments, payments, recent ledger entries
        public async Task<ActionResult> Dashboard(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(400); 

            var student = await db.Students.FindAsync(id);
            if (student == null) return HttpNotFound();

            //Session["CurrentStudentId"] = student.StudentId;

            var acct = new AccountingService(db);

            var vm = new StudentDashboardVm
            {
                Student = student,
                Balance = acct.GetStudentBalance(student.StudentId),
                Enrollments = await db.CourseSelections
                    .Include(s => s.Course)
                    .Where(s => s.StudentId == student.StudentId)
                    .ToListAsync(),
                Payments = await db.Payments
                    .Where(p => p.StudentId == student.StudentId)
                    .OrderByDescending(p => p.PaidAt)
                    .Take(10)
                    .ToListAsync(),
                RecentEntries = await db.LedgerEntries
                    .Where(e => e.StudentId == student.StudentId)
                    .OrderByDescending(e => e.TxnDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(vm);
        }


        // ---------- report ----------
        // GET: /Student/StatementPdf/{id}
        // Builds a statement and render it as a PDF via RDLC
        public async Task<ActionResult> StatementPdf(int id)
        {
            // 1. Load student with course selection
            var student = await db.Students
                .Include(s => s.CourseSelections.Select(cs => cs.Course))
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return HttpNotFound();

            //2. load this student's payments
            var payments = await db.Payments
                .Where(p => p.StudentId == student.StudentId)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            //3. build the RDLC dataset 
            var rows = new List<StudentStatement>();

            // Charge rows: one per enrolled course (no date in your model -> null)
            foreach (var e in student.CourseSelections)
            {
                rows.Add(new StudentStatement
                {
                    StudentName = student.Name,    
                    CourseName = e.Course?.CourseName ?? "Course",   
                    CourseCost = e.Course?.Cost ?? 0m,  
                    PaymentDate = null,   // no date for charge rows
                    AmountPaid = 0m,     
                    Balance = 0m    
                });
            }

            // Payment rows: one per payment
            foreach (var p in payments)
            {
                rows.Add(new StudentStatement
                {
                    StudentName = student.Name,
                    CourseName = "",                  
                    CourseCost = 0m,
                    PaymentDate = p.PaidAt,
                    AmountPaid = p.Amount,
                    Balance = 0m                      
                });
            }

            // 4) Sort rows then compute running balance
            rows = rows
                .OrderBy(r => r.PaymentDate == DateTime.MinValue ? 0 : 1) 
                .ThenBy(r => r.PaymentDate)                     
                .ThenBy(r => r.CourseName)                    
                .ToList();

            // 5) Compute running balance
            decimal running = 0m;
            foreach (var r in rows)
            {
                running += r.CourseCost;  // debit
                running -= r.AmountPaid;  // credit
                r.Balance = running;
            }

            // 6) Render RDLC -> PDF
            var report = new LocalReport
            {
                ReportPath = Server.MapPath("~/StudentStatement.rdlc") // make sure path/file matches
            };
            report.DataSources.Clear();
            report.DataSources.Add(new ReportDataSource("StudentStatementDataset", rows)); // dataset name must match RDLC

            // 7) Produce the PDF binary
            string mimeType, encoding, fileExt;
            Warning[] warnings;
            string[] streamIds;
            var pdf = report.Render(
                "PDF", null, out mimeType, out encoding, out fileExt, out streamIds, out warnings);

            var fileName = $"{(student.Name ?? "Student")}-Statement.pdf";
            return File(pdf, "application/pdf", fileName);

        }

        // Dispose the context when done
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}