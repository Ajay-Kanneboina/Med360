using Microsoft.AspNetCore.Mvc;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class ReceptionistController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IAdminService  _adminService;

        public ReceptionistController(IDoctorService doctorService, IAdminService adminService)
        {
            _doctorService = doctorService;
            _adminService  = adminService;
        }

        private bool IsReceptionist() =>
            HttpContext.Session.GetString("UserRole") == "Receptionist";

        // ── Dashboard ─────────────────────────────────────────────────────

        public IActionResult Dashboard()
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            // Reuse existing services — no new logic needed
            var stats = _doctorService.GetDashboardStats();
            ViewBag.TotalPatients    = stats.TotalPatients;
            ViewBag.TotalRecords     = stats.TotalRecords;
            ViewBag.TotalComplaints  = stats.UnreadComplaints;
            ViewBag.NewComplaints    = stats.UnreadComplaints;

            ViewBag.RecentPatients   = _adminService.GetRecentPatients(5);
            ViewBag.RecentComplaints = _adminService.GetRecentComplaints(5);
            ViewBag.Doctors          = _adminService.GetAllStaff("Doctor", null);

            return View();
        }

        // ── Doctors List ──────────────────────────────────────────────────

        public IActionResult Doctors(string? search)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            var allDoctors = _adminService.GetAllStaff("Doctor", search);
            ViewBag.Search = search ?? "";
            return View(allDoctors);
        }
    }
}
