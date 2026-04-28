using Microsoft.AspNetCore.Mvc;
using MediCore.Models;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IAuditService _audit;
        private readonly IAppointmentService _apptService;

        public AdminController(IAdminService adminService, IAuditService audit, IAppointmentService apptService)
        {
            _adminService = adminService;
            _audit = audit;
            _apptService = apptService;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("UserRole") == "Admin";

        private void SetSidebarBag()
        {
            var stats = _adminService.GetDashboardStats();
            ViewBag.NewComplaints = stats.NewComplaints;
            ViewBag.PendingApprovals = stats.PendingApprovals;
        }

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var stats = _adminService.GetDashboardStats();
            ViewBag.TotalPatients = stats.TotalPatients;
            ViewBag.TotalRecords = stats.TotalRecords;
            ViewBag.TotalComplaints = stats.TotalComplaints;
            ViewBag.ActiveRecords = stats.ActiveRecords;
            ViewBag.ResolvedComplaints = stats.ResolvedComplaints;
            ViewBag.DoctorCount = stats.DoctorCount;
            ViewBag.NurseCount = stats.NurseCount;
            ViewBag.ReceptionistCount = stats.ReceptionistCount;

            ViewBag.RecentPatients = _adminService.GetRecentPatients();
            ViewBag.RecentComplaints = _adminService.GetRecentComplaints();

            return View();
        }

        public IActionResult PendingUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            SetSidebarBag();

            return View(_adminService.GetPendingUsers());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _adminService.ApproveUser(id);
            if (user == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(new AuditLog
            {
                Actor    = admin,
                Action   = "Account Approved",
                Target   = $"{user.Role} — {user.Email}",
                Category = "User",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = $"{user.FullName} has been approved and can now log in.";
            return RedirectToAction(nameof(PendingUsers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var name = _adminService.RejectUser(id);
            if (name == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(new AuditLog
            {
                Actor    = admin,
                Action   = "Account Rejected",
                Target   = name,
                Category = "User",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = "User registration rejected and removed.";
            return RedirectToAction(nameof(PendingUsers));
        }

        public IActionResult AllUsers(string? role, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.RoleFilter = role   ?? "";
            ViewBag.SearchFilter = search ?? "";

            return View(_adminService.GetAllStaff(role, search));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _adminService.ToggleUserActive(id);
            if (user == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(new AuditLog
            {
                Actor    = admin,
                Action   = user.IsActive ? "User Activated" : "User Deactivated",
                Target   = $"{user.Role} — {user.Email}",
                Category = "User",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = $"{user.FullName} is now {(user.IsActive ? "active" : "deactivated")}.";
            return RedirectToAction(nameof(AllUsers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var name = _adminService.DeleteUser(id);
            if (name == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(new AuditLog
            {
                Actor    = admin,
                Action   = "User Deleted",
                Target   = name,
                Category = "User",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = "User deleted permanently.";
            return RedirectToAction(nameof(AllUsers));
        }

        public IActionResult AllPatients(string? search, string? gender)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.SearchFilter = search ?? "";
            ViewBag.GenderFilter = gender ?? "";

            return View(_adminService.GetAllPatients(search, gender));
        }

        public IActionResult PatientDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var patient = _adminService.GetPatientDetail(id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeletePatient(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var patient = _adminService.GetPatientDetail(id);
            if (patient == null) return NotFound();

            if (!_adminService.DeletePatient(id)) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(new AuditLog
            {
                Actor    = admin,
                Action   = "Patient Deleted",
                Target   = patient.FullName,
                Category = "User",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = "Patient and all associated records removed.";
            return RedirectToAction(nameof(AllPatients));
        }

        public IActionResult AllRecords(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";

            return View(_adminService.GetAllRecords(status, search));
        }

        public IActionResult AllComplaints(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";
            ViewBag.UnreadCount = _adminService.GetUnreadComplaintCount();

            return View(_adminService.GetAllComplaints(status, search));
        }

        public IActionResult AuditLog()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.Entries = _adminService.GetAuditEntries();
            return View();
        }

        public IActionResult Appointments(string? status, string? search, string? date)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            DateTime? dateFilter = null;
            if (DateTime.TryParse(date, out var d)) dateFilter = d;

            ViewBag.Stats = _apptService.GetStats();
            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";
            ViewBag.DateFilter = date   ?? "";

            return View(_apptService.GetAllAppointments(status, search, dateFilter));
        }

        public IActionResult Analytics()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var stats = _adminService.GetAnalyticsStats();

            ViewBag.TotalPatients = stats.TotalPatients;
            ViewBag.TotalRecords = stats.TotalRecords;
            ViewBag.TotalComplaints = stats.TotalComplaints;
            ViewBag.ResolvedComplaints = stats.ResolvedComplaints;
            ViewBag.ActiveRecords = stats.ActiveRecords;
            ViewBag.TotalStaff = stats.TotalStaff;
            ViewBag.MaleCount  = stats.MaleCount;
            ViewBag.FemaleCount = stats.FemaleCount;
            ViewBag.OtherGender = stats.OtherGender;
            ViewBag.ActiveComplaints = stats.ActiveComplaints;
            ViewBag.ReviewedComplaints = stats.ReviewedComplaints;
            ViewBag.ClosedComplaints   = stats.ClosedComplaints;
            ViewBag.DoctorCount = stats.DoctorCount;
            ViewBag.NurseCount  = stats.NurseCount;
            ViewBag.ReceptionistCount  = stats.ReceptionistCount;
            ViewBag.BloodGroups = stats.BloodGroups;

            return View();
        }
    }
}
