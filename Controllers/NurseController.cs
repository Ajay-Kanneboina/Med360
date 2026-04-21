using Microsoft.AspNetCore.Mvc;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class NurseController : Controller
    {
        private readonly IDoctorService _doctorService;

        public NurseController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        private bool IsNurse() =>
            HttpContext.Session.GetString("UserRole") == "Nurse";

        public IActionResult Dashboard()
        {
            if (!IsNurse()) return RedirectToAction("Login", "Account");

            var stats = _doctorService.GetDashboardStats();
            ViewBag.TotalPatients    = stats.TotalPatients;
            ViewBag.TotalRecords     = stats.TotalRecords;
            ViewBag.ActiveRecords    = stats.ActiveRecords;
            ViewBag.UnreadComplaints = stats.UnreadComplaints;

            ViewBag.RecentPatients   = _doctorService.GetPriorityPatients(5);
            ViewBag.RecentComplaints = _doctorService.GetRecentComplaints(5);
            ViewBag.NewComplaints    = stats.UnreadComplaints;

            return View();
        }
    }
}
