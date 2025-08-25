using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Uxtrata.Models;
using Uxtrata.Services;

namespace Uxtrata.Controllers
{
    /// <summary>
    /// Manages student course enrollments (CourseSelection entities)
    /// Note: Creating/deleting enrollemnts also creates/deletes ledger entries via AccountingService
    /// </summary>
    public class CourseSelectionController : Controller
    {
        private readonly SchoolContext db = new SchoolContext();

        // ---------- helpers ----------
        // Populate dropdowns for Students and Courses
        // Called in Create/Edit GET and POST to build SelectLists for the views
        private async Task PopulateDropDownsAsync(CourseSelection cs = null)
        {
            ViewBag.StudentId = new SelectList(
                await db.Students.AsNoTracking().ToListAsync(),   // pull students without tracking
                "StudentId", "Name", cs?.StudentId);              // bind StudentId as value, Name as text

            ViewBag.CourseId = new SelectList(
                await db.Courses.AsNoTracking().ToListAsync(),    // pull courses without tracking
                "CourseId", "CourseName", cs?.CourseId);          // bind CourseId as value, CourseName as text
        }

        // Returns true if a duplicate enrollment exists
        // Used in Create and Edit to prevent duplicates
        private Task<bool> IsDuplicateAsync(int studentId, int courseId, int? excludeId = null) // ingnore the current record   
        {
            var q = db.CourseSelections.AsNoTracking()
                        .Where(x => x.StudentId == studentId && x.CourseId == courseId);

            // Exclude the current row when editing
            if (excludeId.HasValue) q = q.Where(x => x.CourseSelectionId != excludeId.Value);
            return q.AnyAsync();   
        }


        // GET: CourseSelection
        // List all enrollments with student and course data
        public async Task<ActionResult> Index()
        {
            var selections = await db.CourseSelections
                                     .AsNoTracking()
                                     .Include(cs => cs.Student)  //eager load student data
                                     .Include(cs => cs.Course)   //eager load course data
                                     .ToListAsync();
            return View(selections);
        }


        // GET: CourseSelection/Details/{id}
        // Show one enrollment (400 if id missing, 404 if not found)
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var selection = await db.CourseSelections
                                    .AsNoTracking()
                                    .Include(cs => cs.Student)
                                    .Include(cs => cs.Course)
                                    .FirstOrDefaultAsync(cs => cs.CourseSelectionId == id);
            if (selection == null) return HttpNotFound();

            return View(selection);
        }


        // GET: CourseSelection/Create
        // Show form with dropdowns for Student and Course
        public async Task<ActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View();
        }


        // POST: CourseSelection/Create
        // Create a new enrollment if not a duplicate
        // Security: [ValidateAntiForgeryToken] protects against CSRF
        // Security: Limits model binding to whitelisted fields
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "CourseSelectionId,StudentId,CourseId,SelectionDate")] CourseSelection selection)
        {
            // Check for duplicate enrollment
            if (await IsDuplicateAsync(selection.StudentId, selection.CourseId))
                ModelState.AddModelError("", "This student is already enrolled in the selected course.");

            // If validation fails, reload dropdowns and show form again
            if (!ModelState.IsValid)
            {
                await PopulateDropDownsAsync(selection);
                return View(selection);
            }

            db.CourseSelections.Add(selection);
            await db.SaveChangesAsync();


            // Post to ledger (Debit AR, Credit REV)
            var course = await db.Courses.FindAsync(selection.CourseId);
            var acct = new AccountingService(db);
            await acct.PostEnrollmentAsync(selection.StudentId, selection.CourseId, selection.CourseSelectionId, course.Cost);

            // UX: Jump to the student's dashboard after enrolling
            return RedirectToAction("Dashboard", "Student", new { id = selection.StudentId });

        }


        // GET: CourseSelection/Edit/{id}
        // Loads the edit form for an existing enrollment; 404/400 as needed
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var selection = await db.CourseSelections.FindAsync(id);
            if (selection == null) return HttpNotFound();

            await PopulateDropDownsAsync(selection);
            return View(selection);
        }


        // POST: CourseSelection/Edit/{id}
        // Save edits to an existing enrollment if not a duplicate
        // Security: [ValidateAntiForgeryToken] protects against CSRF
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CourseSelectionId,StudentId,CourseId,SelectionDate")] CourseSelection selection)
        {
            // Check for duplicate enrollment (excluding this row itself)
            if (await IsDuplicateAsync(selection.StudentId, selection.CourseId, selection.CourseSelectionId))
                ModelState.AddModelError("", "This student is already enrolled in the selected course.");

            // If validation fails, reload dropdowns and show form again
            if (!ModelState.IsValid)
            {
                await PopulateDropDownsAsync(selection);
                return View(selection);
            }

            db.Entry(selection).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // GET: CourseSelection/Delete/{id}
        // Show a delete confirmation page
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var selection = await db.CourseSelections
                                    .AsNoTracking()
                                    .Include(cs => cs.Student)
                                    .Include(cs => cs.Course)
                                    .FirstOrDefaultAsync(cs => cs.CourseSelectionId == id);
            if (selection == null) return HttpNotFound();

            return View(selection);
        }

        // POST: CourseSelection/Delete/{id}
        // Deletes the enrollment after confirmation
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var selection = await db.CourseSelections.FindAsync(id);
                    if (selection == null) return HttpNotFound();

                    // 1) Remove ledger rows that reference this enrollment
                    var ledger = db.LedgerEntries.Where(le => le.CourseSelectionId == id);
                    db.LedgerEntries.RemoveRange(ledger);

                    // 2) Remove the enrollment
                    db.CourseSelections.Remove(selection);

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


        // ---------- cleanup ----------
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
