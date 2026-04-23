using Microsoft.AspNetCore.Mvc;
using MediCore.Services;
using MediCore.Models;

namespace MediCore.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IDoctorService      _doctorService;
        private readonly IAuditService       _audit;
        private readonly IAppointmentService _apptService;

        public DoctorController(IDoctorService doctorService, IAuditService audit, IAppointmentService apptService)
        {
            _doctorService = doctorService;
            _audit         = audit;
            _apptService   = apptService;
        }

        private bool IsStaff()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Doctor" || role == "Admin" ||
                   role == "Nurse" || role == "Receptionist";
        }

        private bool IsDoctor()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Doctor" || role == "Admin";
        }

        private bool CanViewComplaints()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Doctor" || role == "Admin" || role == "Nurse";
        }

        private void SetSidebarBag()
        {
            ViewBag.NewComplaints = _doctorService.GetUnreadCount();
        }

        public IActionResult Dashboard()
        {
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

        public IActionResult EditRecord(int id)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var record = _doctorService.GetRecord(id);
            if (record == null) return NotFound();

            ViewBag.PatientName = record.Patient?.FullName ?? "";
            return View(record);
        }

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

        public IActionResult Complaints(string? filter)
        {
            if (!CanViewComplaints()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            ViewBag.Filter      = filter ?? "all";
            ViewBag.UnreadCount = _doctorService.GetUnreadCount();

            return View(_doctorService.GetComplaints(filter));
        }

        public IActionResult ViewComplaint(int id)
        {
            if (!CanViewComplaints()) return RedirectToAction("Login", "Account");
            SetSidebarBag();

            var complaint = _doctorService.GetAndMarkRead(id);
            if (complaint == null) return NotFound();

            return View(complaint);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Respond(int id, string response, string status)
        {
            if (!CanViewComplaints()) return RedirectToAction("Login", "Account");

            var doctorName = HttpContext.Session.GetString("UserName") ?? "Unknown";

            if (!_doctorService.RespondToComplaint(id, response, status, doctorName))
                return NotFound();

            _audit.Log(doctorName, "Complaint Responded", $"Complaint #{id}", "Complaint", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = "Response sent to patient.";
            return RedirectToAction(nameof(Complaints));
        }

        public IActionResult MyAppointments(string? status)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            ViewBag.StatusFilter = status ?? "";
            ViewBag.Appointments = _apptService.GetDoctorAppointments(userId, status);
            return View();
        }

        public IActionResult MyAvailability()
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");
            SetSidebarBag();
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var existing = _apptService.GetDoctorAvailability(userId);
            var days = new List<MediCore.Models.DoctorAvailability>();
            for (int i = 0; i < 7; i++)
            {
                var found = existing.FirstOrDefault(e => e.DayOfWeek == i);
                days.Add(found ?? new MediCore.Models.DoctorAvailability
                { DoctorId = userId, DayOfWeek = i, StartTime = "09:00 AM", EndTime = "05:00 PM", MaxSlots = 10, IsAvailable = false });
            }
            ViewBag.DayNames = new[] { "Sunday","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday" };
            return View(days);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SaveAvailability(bool[] available, string[] startTimes, string[] endTimes, int[] maxSlots)
        {
            if (!IsDoctor()) return RedirectToAction("Login", "Account");
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var slots = new List<MediCore.Models.DoctorAvailability>();
            for (int i = 0; i < 7; i++)
            {
                slots.Add(new MediCore.Models.DoctorAvailability
                {
                    DoctorId = userId, DayOfWeek = i,
                    IsAvailable = available.Length > i && available[i],
                    StartTime = startTimes.Length > i ? startTimes[i] : "09:00 AM",
                    EndTime = endTimes.Length > i ? endTimes[i] : "05:00 PM",
                    MaxSlots = maxSlots.Length > i ? maxSlots[i] : 10
                });
            }
            _apptService.SaveAvailability(userId, slots);
            TempData["Success"] = "Availability saved!";
            return RedirectToAction(nameof(MyAvailability));
        }
    }
}
