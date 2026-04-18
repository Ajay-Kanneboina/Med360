using Microsoft.AspNetCore.Mvc;
using MediCore.Services;
using MediCore.Models;

namespace MediCore.Controllers
{
    /// <summary>
    /// DoctorController — thin HTTP adapter for all doctor/clinical views.
    /// Accessible by Doctor, Admin, Nurse, and Receptionist roles.
    /// Delegates all business logic to IDoctorService.
    /// </summary>
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IAuditService  _audit;

        public DoctorController(IDoctorService doctorService, IAuditService audit)
        {
            _doctorService = doctorService;
            _audit         = audit;
        }

        // ── Auth guards ───────────────────────────────────────────────────

        /// <summary>
        /// Returns true for Doctor, Admin, Nurse, Receptionist.
        /// All staff roles can access the doctor-facing views.
        /// </summary>
        private bool IsStaff()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Doctor" || role == "Admin" ||
                   role == "Nurse" || role == "Receptionist";
        }

        /// <summary>
        /// Returns true only for Doctor and Admin.
        /// Only these roles can add/edit medical records.
        /// </summary>
        private bool IsDoctor()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Doctor" || role == "Admin";
        }

        /// <summary>Populates sidebar unread complaint count.</summary>
        private void SetSidebarBag()
        {
            ViewBag.NewComplaints = _doctorService.GetUnreadCount();
        }

        // ── Dashboard ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /Doctor/Dashboard
        /// Main landing page for all staff roles.
        /// Shows KPI cards, recent complaints, recent records, priority patients.
        /// </summary>
        public IActionResult Dashboard()
        {
            // FIX: Use IsStaff() so Nurse/Receptionist can also see the dashboard
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var stats = _doctorService.GetDashboardStats();
            ViewBag.TotalPatients    = stats.TotalPatients;
            ViewBag.TotalRecords     = stats.TotalRecords;
            ViewBag.ActiveRecords    = stats.ActiveRecords;
            ViewBag.UnreadComplaints = stats.UnreadComplaints;

            ViewBag.RecentComplaints = _doctorService.GetRecentComplaints();
            ViewBag.RecentRecords    = _doctorService.GetRecentRecords();
            ViewBag.PriorityPatients = _doctorService.GetPriorityPatients();

            return View();
        }

        // ── Patient List ──────────────────────────────────────────────────

        /// <summary>GET /Doctor/Patients — all patients with search and filter.</summary>
        public IActionResult Patients(string? search, string? gender, string? blood)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.Search      = search ?? "";
            ViewBag.Gender      = gender ?? "";
            ViewBag.Blood       = blood  ?? "";
            ViewBag.BloodGroups = _doctorService.GetBloodGroups();

            return View(_doctorService.GetPatients(search, gender, blood));
        }

        // ── Patient Detail ────────────────────────────────────────────────

        /// <summary>GET /Doctor/PatientDetail/{id} — full patient profile.</summary>
        public IActionResult PatientDetail(int id)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var patient = _doctorService.GetPatientDetail(id);
            if (patient == null) return NotFound();

            ViewBag.ActiveRecordCount  = patient.Records.Count(r => r.Status == "Active");
            ViewBag.ClosedRecordCount  = patient.Records.Count(r => r.Status == "Closed");
            ViewBag.OpenComplaintCount = patient.Complaints.Count(c => c.Status == "Active");

            return View(patient);
        }

        // ── Add Record ────────────────────────────────────────────────────

        /// <summary>GET /Doctor/AddRecord/{patientId} — blank add record form.</summary>
        public IActionResult AddRecord(int patientId)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var patient = _doctorService.GetPatientDetail(patientId);
            if (patient == null) return NotFound();

            ViewBag.PatientName = patient.FullName;
            ViewBag.PatientId   = patientId;
            return View();
        }

        /// <summary>
        /// POST /Doctor/AddRecord — saves new record.
        /// DoctorName stamped from session and passed to service.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddRecord(int patientId, string diagnosis,
                                       string treatment, string? notes,
                                       DateTime visitDate, string status)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(diagnosis) || string.IsNullOrWhiteSpace(treatment))
            {
                var p = _doctorService.GetPatientDetail(patientId);
                ViewBag.PatientName = p?.FullName;
                ViewBag.PatientId   = patientId;
                ViewBag.Error       = "Diagnosis and treatment are required.";
                return View();
            }

            var doctorName  = HttpContext.Session.GetString("UserName") ?? "Unknown";
            var patientName = _doctorService.GetPatientDetail(patientId)?.FullName ?? $"Patient #{patientId}";
            _doctorService.AddRecord(patientId, diagnosis, treatment, notes, visitDate, status, doctorName);
            _audit.Log(doctorName, "Medical Record Added", patientName, "Record", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "Medical record saved successfully.";
            return RedirectToAction(nameof(PatientDetail), new { id = patientId });
        }

        // ── Edit Record ───────────────────────────────────────────────────

        /// <summary>GET /Doctor/EditRecord/{id} — pre-filled edit form.</summary>
        public IActionResult EditRecord(int id)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var record = _doctorService.GetRecord(id);
            if (record == null) return NotFound();

            ViewBag.PatientName = record.Patient?.FullName ?? "";
            return View(record);
        }

        /// <summary>POST /Doctor/EditRecord/{id} — updates clinical fields only.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditRecord(int id, string diagnosis, string treatment,
                                        string? notes, DateTime visitDate, string status)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(diagnosis) || string.IsNullOrWhiteSpace(treatment))
            {
                var record = _doctorService.GetRecord(id);
                ViewBag.Error = "Diagnosis and treatment are required.";
                return View(record);
            }

            if (!_doctorService.UpdateRecord(id, diagnosis, treatment, notes, visitDate, status))
                return NotFound();

            var updated     = _doctorService.GetRecord(id);
            var doctorName  = HttpContext.Session.GetString("UserName") ?? "Unknown";
            var patientName = updated?.Patient?.FullName ?? $"Patient #{updated?.PatientId}";
            _audit.Log(doctorName, "Medical Record Updated", patientName, "Record", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "Record updated.";
            return RedirectToAction(nameof(PatientDetail), new { id = updated?.PatientId });
        }

        // ── Close Record ──────────────────────────────────────────────────

        /// <summary>POST /Doctor/CloseRecord/{id} — soft-archives a record.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CloseRecord(int id)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");

            var record = _doctorService.GetRecord(id);
            if (record == null) return NotFound();

            int patientId   = record.PatientId;
            var doctorName  = HttpContext.Session.GetString("UserName") ?? "Unknown";
            var patientName = record.Patient?.FullName ?? $"Patient #{patientId}";
            _doctorService.CloseRecord(id);
            _audit.Log(doctorName, "Medical Record Closed", patientName, "Record", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "Record archived.";
            return RedirectToAction(nameof(PatientDetail), new { id = patientId });
        }

        // ── Complaints ────────────────────────────────────────────────────

        /// <summary>GET /Doctor/Complaints?filter=… — all complaints with filter.</summary>
        public IActionResult Complaints(string? filter)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.Filter      = filter ?? "all";
            ViewBag.UnreadCount = _doctorService.GetUnreadCount();

            return View(_doctorService.GetComplaints(filter));
        }

        /// <summary>GET /Doctor/ViewComplaint/{id} — view and auto-mark as read.</summary>
        public IActionResult ViewComplaint(int id)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var complaint = _doctorService.GetAndMarkRead(id);
            if (complaint == null) return NotFound();

            return View(complaint);
        }

        /// <summary>POST /Doctor/Respond — saves doctor response, stamps from session.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Respond(int id, string response, string status)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var doctorName = HttpContext.Session.GetString("UserName") ?? "Unknown";

            if (!_doctorService.RespondToComplaint(id, response, status, doctorName))
                return NotFound();

            _audit.Log(doctorName, "Complaint Responded", $"Complaint #{id}", "Complaint", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "Response sent to patient.";
            return RedirectToAction(nameof(Complaints));
        }
    }
}
