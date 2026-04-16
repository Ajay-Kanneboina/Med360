using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) => _db = db;

        private bool IsAdmin()
        {
            var r = HttpContext.Session.GetString("UserRole");
            return r == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.NewComplaints = _db.Complaints.Count(c => !c.IsRead);
            ViewBag.TotalUsers = _db.Users.Count();
            ViewBag.PendingApprovals = _db.Users.Count(u => !u.IsActive && (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist"));
            return View();
        }

        public IActionResult PendingUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var q = _db.Users.Where(u => !u.IsActive && (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist"))
                        .OrderBy(u => u.CreatedAt)
                        .ToList();
            return View(q);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var u = _db.Users.Find(id);
            if (u == null) return NotFound();
            u.IsActive = true;
            _db.SaveChanges();
            TempData["Success"] = "User approved.";
            return RedirectToAction(nameof(PendingUsers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var u = _db.Users.Find(id);
            if (u == null) return NotFound();
            _db.Users.Remove(u);
            _db.SaveChanges();
            TempData["Success"] = "User rejected and removed.";
            return RedirectToAction(nameof(PendingUsers));
        }
    }
}
