using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Uxtrata.Models;

namespace Uxtrata.Controllers
{
    /// <summary>
    /// Manages CRUD for Course entities
    /// </summary>
    public class CourseController : Controller
    {
        private readonly SchoolContext db = new SchoolContext();


        //GET: Course
        // Lists all courses for the Index view.
        public async Task<ActionResult> Index()
        {
            var courses = await db.Courses.AsNoTracking().ToListAsync();
            return View(courses);
        }


        //GET: Course/Details/{id}
        // Display a single student by id
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(s => s.CourseId == id);
            if (course == null) return HttpNotFound();
            return View(course);
        }


        // GET: Course/Create
        // Render the empty Create form
        public ActionResult Create()
        {
            return View();
        }


        // POST: Course/Create
        // Creates a course from posted form values     
        // Security: [ValidateAntiForgeryToken] protects against CSRF
        // Security: Limits model binding to whitelisted fields
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "CourseId, CourseName, Cost")] Course course)
        {
            // Re-display form if invalid
            if (!ModelState.IsValid) return View(course);
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // Get: Course/Edit/{id}
        // Load existing course into the edit form; returns 404/400 as needed
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(s => s.CourseId == id);
            if (course == null) return HttpNotFound();
            return View(course);
        }

        // POST: Course/Edit/{id}
        // Saves edited fields back to db
        // Handle form submission for editing course
        // Security: [ValidateAntiForgeryToken] protects against CSRF
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CourseId, CourseName, Cost")] Course course)
        {
            // Re-display form if invalid
            if (!ModelState.IsValid) return View(course);
            db.Entry(course).State = EntityState.Modified;
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // Get: Course/Delete/{id}
        // Show confirmation page for deleting course
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var course = await db.Courses.FirstOrDefaultAsync(s => s.CourseId == id);
            if (course == null) return HttpNotFound();
            return View(course);
        }

        // POST: Course/Delete/{id}
        // Handle deletion of course
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return HttpNotFound();
            db.Courses.Remove(course);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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