using System;
using System.Linq;                  // FirstOrDefault()
using System.Web.Mvc;
using Uxtrata.Models;             // SchoolContext

namespace UxCollege.Controllers
{
    public class HealthController : Controller
    {
        // GET: /Health/DbPing
        public ActionResult DbPing()
        {
            try
            {
                using (var db = new SchoolContext())
                {
                    // Open connection explicitly (good for testing)
                    db.Database.Connection.Open();

                    // Quick checks
                    var version = db.Database.SqlQuery<string>("SELECT VERSION()").FirstOrDefault();
                    var dbName = db.Database.SqlQuery<string>("SELECT DATABASE()").FirstOrDefault();

                    return Content($"EF OK ✅  MySQL: {version}  |  DB: {dbName}");
                }
            }
            catch (Exception ex)
            {
                // Return the error so you can fix config fast
                return Content("EF FAIL ❌  " + ex.Message);
            }
        }
    }
}
