using Microsoft.AspNetCore.Mvc;
using MediCore.Controllers;
using MediCore.Services;

namespace MediCore.Controllers
{
    /// <summary>
    /// AdminController — thin HTTP adapter for admin operations.
    ///
    /// RESPONSIBILITIES (what this controller does):
    ///   1. Verify the logged-in user is an Admin (auth guard)
    ///   2. Call the appropriate IAdminService method
    ///   3. Map service results into ViewBag / Model for the view
    ///   4. Return the correct View or Redirect
    ///
    /// NOT RESPONSIBLE FOR (all moved to AdminService):
    ///   • Deciding which roles need approval
    ///   • Calculating KPI counts or analytics breakdowns
    ///   • Building the audit log feed
    ///   • Filtering / searching patients, records, complaints
    ///   • Any DateTime.Now stamps or status default values
    /// </summary>
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IAuditService _audit;

        public AdminController(IAdminService adminService, IAuditService audit)
        {
            _adminService = adminService;
            _audit        = audit;
        }

        // ── Auth guard ────────────────────────────────────────────────────

        /// <summary>
        /// Returns true only if the current session role is "Admin".
        /// Called at the top of every action to prevent unauthorised access.
        /// </summary>
        private bool IsAdmin() =>
            HttpContext.Session.GetString("UserRole") == "Admin";

        /// <summary>
        /// Populates sidebar badge counts (unread complaints, pending approvals)
        /// by asking the service — no DB queries in the controller.
        /// </summary>
        private void SetSidebarBag()
        {
            var stats = _adminService.GetDashboardStats();
            ViewBag.NewComplaints    = stats.NewComplaints;
            ViewBag.PendingApprovals = stats.PendingApprovals;
        }

        // ── Dashboard ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/Index
        /// Fetches all dashboard data from the service and passes to the view.
        /// Zero business logic here — all calculations are in AdminService.
        /// </summary>
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            // Single service call returns all KPI values
            var stats = _adminService.GetDashboardStats();
            ViewBag.TotalPatients      = stats.TotalPatients;
            ViewBag.TotalUsers         = stats.TotalUsers;
            ViewBag.TotalRecords       = stats.TotalRecords;
            ViewBag.TotalComplaints    = stats.TotalComplaints;
            ViewBag.ActiveRecords      = stats.ActiveRecords;
            ViewBag.ResolvedComplaints = stats.ResolvedComplaints;
            ViewBag.DoctorCount        = stats.DoctorCount;
            ViewBag.NurseCount         = stats.NurseCount;
            ViewBag.ReceptionistCount  = stats.ReceptionistCount;

            // Feed lists for the dashboard tables
            ViewBag.RecentPatients   = _adminService.GetRecentPatients();
            ViewBag.RecentComplaints = _adminService.GetRecentComplaints();

            return View();
        }

        // ── Pending Approvals ─────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/PendingUsers
        /// Displays staff registrations awaiting admin approval.
        /// Which roles require approval is decided by AdminService, not here.
        /// </summary>
        public IActionResult PendingUsers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            return View(_adminService.GetPendingUsers());
        }

        /// <summary>
        /// POST /Admin/Approve
        /// Delegates the approval logic to the service.
        /// Controller only composes the success message using the returned user.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _adminService.ApproveUser(id);
            if (user == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(admin, "Account Approved", $"{user.Role} — {user.Email}", "User", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = $"{user.FullName} has been approved and can now log in.";
            return RedirectToAction(nameof(PendingUsers));
        }

        /// <summary>
        /// POST /Admin/Reject
        /// Delegates removal to the service.
        /// Controller only sets the success message.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var name = _adminService.RejectUser(id);
            if (name == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(admin, "Account Rejected", name, "User", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "User registration rejected and removed.";
            return RedirectToAction(nameof(PendingUsers));
        }

        // ── All Staff ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/AllUsers?role=…&search=…
        /// Passes filter values to the service; receives a ready-to-display list.
        /// </summary>
        public IActionResult AllUsers(string? role, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.RoleFilter   = role   ?? "";
            ViewBag.SearchFilter = search ?? "";

            return View(_adminService.GetAllStaff(role, search));
        }

        /// <summary>
        /// POST /Admin/ToggleUser
        /// Flips a staff account's active/inactive state via the service.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _adminService.ToggleUserActive(id);
            if (user == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(admin, user.IsActive ? "User Activated" : "User Deactivated", $"{user.Role} — {user.Email}", "User", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = $"{user.FullName} is now {(user.IsActive ? "active" : "deactivated")}.";
            return RedirectToAction(nameof(AllUsers));
        }

        /// <summary>
        /// POST /Admin/DeleteUser
        /// Permanently removes a staff account via the service.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var name = _adminService.DeleteUser(id);
            if (name == null) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(admin, "User Deleted", name, "User", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "User deleted permanently.";
            return RedirectToAction(nameof(AllUsers));
        }

        // ── All Patients ──────────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/AllPatients?search=…&gender=…
        /// Service handles the filtering logic; controller just passes through filters.
        /// </summary>
        public IActionResult AllPatients(string? search, string? gender)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.SearchFilter = search ?? "";
            ViewBag.GenderFilter = gender ?? "";

            return View(_adminService.GetAllPatients(search, gender));
        }

        /// <summary>
        /// GET /Admin/PatientDetail/{id}
        /// Service loads the patient with all navigation properties.
        /// </summary>
        public IActionResult PatientDetail(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var patient = _adminService.GetPatientDetail(id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        /// <summary>
        /// POST /Admin/DeletePatient
        /// Delegates cascade delete to the service (Records + Complaints auto-removed).
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeletePatient(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var patient = _adminService.GetPatientDetail(id);
            if (patient == null) return NotFound();
            if (!_adminService.DeletePatient(id)) return NotFound();

            var admin = HttpContext.Session.GetString("UserName") ?? "Admin";
            _audit.Log(admin, "Patient Deleted", patient.FullName, "User", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "Patient and all associated records removed.";
            return RedirectToAction(nameof(AllPatients));
        }

        // ── Medical Records ───────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/AllRecords?status=…&search=…
        /// Service owns the filtering logic; controller only passes parameters.
        /// </summary>
        public IActionResult AllRecords(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";

            return View(_adminService.GetAllRecords(status, search));
        }

        // ── Complaints ────────────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/AllComplaints?status=…&search=…
        /// The "unread" vs Status filter distinction is handled by the service.
        /// </summary>
        public IActionResult AllComplaints(string? status, string? search)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";
            ViewBag.UnreadCount  = _adminService.GetUnreadComplaintCount();

            return View(_adminService.GetAllComplaints(status, search));
        }

        // ── Audit Log ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/AuditLog
        /// Service builds the merged, sorted audit feed from all tables.
        /// Controller simply passes it to the view.
        /// </summary>
        public IActionResult AuditLog()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.Entries = _adminService.GetAuditEntries();
            return View();
        }

        // ── Analytics ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /Admin/Analytics
        /// Service computes all breakdowns in one call.
        /// Controller maps the DTO properties into ViewBag for the view.
        /// </summary>
        public IActionResult Analytics()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var stats = _adminService.GetAnalyticsStats();

            ViewBag.TotalPatients      = stats.TotalPatients;
            ViewBag.TotalRecords       = stats.TotalRecords;
            ViewBag.TotalComplaints    = stats.TotalComplaints;
            ViewBag.ResolvedComplaints = stats.ResolvedComplaints;
            ViewBag.ActiveRecords      = stats.ActiveRecords;
            ViewBag.TotalStaff         = stats.TotalStaff;
            ViewBag.MaleCount          = stats.MaleCount;
            ViewBag.FemaleCount        = stats.FemaleCount;
            ViewBag.OtherGender        = stats.OtherGender;
            ViewBag.ActiveComplaints   = stats.ActiveComplaints;
            ViewBag.ReviewedComplaints = stats.ReviewedComplaints;
            ViewBag.ClosedComplaints   = stats.ClosedComplaints;
            ViewBag.DoctorCount        = stats.DoctorCount;
            ViewBag.NurseCount         = stats.NurseCount;
            ViewBag.ReceptionistCount  = stats.ReceptionistCount;
            ViewBag.BloodGroups        = stats.BloodGroups;

            return View();
        }
    }
}
