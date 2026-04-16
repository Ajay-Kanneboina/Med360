using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediCore.Data;

namespace MediCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) => _db = db;

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == null) return RedirectToAction("Login", "Account");
            if (role == "Patient") return RedirectToAction("Dashboard", "Patient");
            // Admin-only dashboard
            if (role != "Admin") return RedirectToAction("Login", "Account");

            ViewBag.NewComplaints    = _db.Complaints.Count(c => !c.IsRead);
            ViewBag.TotalPatients    = _db.Patients.Count();
            ViewBag.TotalRecords     = _db.Records.Count();
            ViewBag.RecentComplaints = _db.Complaints
                .Include(c => c.Patient)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(6)
                .ToList();

            ViewBag.PendingApprovals = _db.Users.Count(u => !u.IsActive && (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist"));

            return View();
        }
    }
}
