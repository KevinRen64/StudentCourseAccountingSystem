using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Uxtrata.Models;
using Uxtrata.Services;

namespace Uxtrata.Controllers
{
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

        // Check if a student is already enrolled in a course
        // Used in Create and Edit to prevent duplicates
        private Task<bool> IsDuplicateAsync(int studentId, int courseId, int? excludeId = null) // ingnore the current record   
        {
            var q = db.CourseSelections.AsNoTracking()
                        .Where(x => x.StudentId == studentId && x.CourseId == courseId);

            // Exclude the current row when editing
            if (excludeId.HasValue) q = q.Where(x => x.CourseSelectionId != excludeId.Value);
            return q.AnyAsync();   // true if duplicate exists
        }


        // ---------- list / details ----------
        // GET: CourseSelection
        // Show all course enrollments with related Student + Course info
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
        // Show details for one enrollment
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

        // ---------- create ----------
        // GET: CourseSelection/Create
        // Show form with dropdowns for Student and Course
        public async Task<ActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View();
        }

        // POST: CourseSelection/Create
        // Save new enrollment if not duplicate
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

            // Post invoice to ledger (DR AR, CR REV)
            var course = await db.Courses.FindAsync(selection.CourseId);
            var acct = new AccountingService(db);
            await acct.PostEnrollmentAsync(selection.StudentId, selection.CourseId, selection.CourseSelectionId, course.Cost);

            return RedirectToAction("Dashboard", "Student", new { id = selection.StudentId });

        }

        // ---------- edit ----------
        // GET: CourseSelection/Edit/{id}
        // Show form for editing an existing enrollment
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var selection = await db.CourseSelections.FindAsync(id);
            if (selection == null) return HttpNotFound();

            await PopulateDropDownsAsync(selection);
            return View(selection);
        }

        // POST: CourseSelection/Edit/{id}
        // Update enrollment, ensuring no duplicates
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

        // ---------- delete ----------
        // GET: CourseSelection/Delete/{id}
        // Show confirmation page
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

        // POST: CourseSelection/Delete/5
        // Delete confirmed
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var selection = await db.CourseSelections.FindAsync(id);
            if (selection == null) return HttpNotFound();

            db.CourseSelections.Remove(selection);
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
