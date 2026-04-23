using Microsoft.AspNetCore.Mvc;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class ReceptionistController : Controller
    {
        private readonly IDoctorService      _doctorService;
        private readonly IAdminService       _adminService;
        private readonly IAppointmentService _apptService;
        private readonly IAuditService       _audit;

        public ReceptionistController(IDoctorService doctorService, IAdminService adminService,
                                      IAppointmentService apptService, IAuditService audit)
        {
            _doctorService = doctorService;
            _adminService  = adminService;
            _apptService   = apptService;
            _audit         = audit;
        }

        private bool IsReceptionist() =>
            HttpContext.Session.GetString("UserRole") == "Receptionist";

        public IActionResult Dashboard()
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            var stats = _doctorService.GetDashboardStats();
            ViewBag.TotalPatients  = stats.TotalPatients;
            ViewBag.TotalRecords   = stats.TotalRecords;

            ViewBag.RecentPatients = _adminService.GetRecentPatients(5);
            ViewBag.Doctors        = _adminService.GetAllStaff("Doctor", null);

            return View();
        }

        public IActionResult Doctors(string? search)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            var allDoctors = _adminService.GetAllStaff("Doctor", search);
            ViewBag.Search = search ?? "";
            return View(allDoctors);
        }

        public IActionResult AppointmentRequests()
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");
            ViewBag.PendingRequests  = _apptService.GetPendingRequestCount();
            return View(_apptService.GetPendingRequests());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult HandleRequest(int id)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");
            _apptService.MarkRequestHandled(id);
            TempData["Success"] = "Request marked as handled.";
            return RedirectToAction(nameof(AppointmentRequests));
        }

        public IActionResult Appointments(string? status, string? search, string? date)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            DateTime? dateFilter = null;
            if (DateTime.TryParse(date, out var d)) dateFilter = d;

            var stats = _apptService.GetStats();
            ViewBag.Stats           = stats;
            ViewBag.StatusFilter    = status ?? "";
            ViewBag.SearchFilter    = search ?? "";
            ViewBag.DateFilter      = date   ?? "";
            ViewBag.PendingRequests = _apptService.GetPendingRequestCount();

            return View(_apptService.GetAllAppointments(status, search, dateFilter));
        }

        public IActionResult BookAppointment(int? requestId, int? patientId)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            ViewBag.Patients        = _apptService.GetAllPatients();
            ViewBag.Doctors         = _apptService.GetAllDoctors();
            ViewBag.RequestId       = requestId;
            ViewBag.SelectedPatient = patientId;
            ViewBag.PendingRequests = _apptService.GetPendingRequestCount();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult BookAppointment(int patientId, int doctorId,
                                             string appointmentDate, string timeSlot,
                                             string? notes, int? requestId)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            if (!DateTime.TryParse(appointmentDate, out var d))
            {
                TempData["Error"] = "Invalid date selected.";
                return RedirectToAction(nameof(BookAppointment));
            }

            var appt = _apptService.BookAppointment(patientId, doctorId, d, timeSlot, notes);
            if (appt == null)
            {
                TempData["Error"] = "That time slot is already taken. Please choose another.";
                return RedirectToAction(nameof(BookAppointment));
            }

            if (requestId.HasValue)
                _apptService.MarkRequestHandled(requestId.Value);

            var receptionistName = HttpContext.Session.GetString("UserName") ?? "Receptionist";
            _audit.Log(receptionistName, "Appointment Booked",
                       $"Patient #{patientId} with Doctor #{doctorId} on {d:dd MMM yyyy} {timeSlot}",
                       "Appointment", HttpContext.Session.GetInt32("UserId"));

            TempData["Success"] = $"Appointment booked for {d:dd MMM yyyy} at {timeSlot}.";
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UpdateAppointmentStatus(int id, string status, string? reason)
        {
            if (!IsReceptionist()) return RedirectToAction("Login", "Account");

            if (status == "Cancelled")
                _apptService.CancelAppointment(id, reason ?? "Cancelled by receptionist");
            else
                _apptService.UpdateStatus(id, status);

            TempData["Success"] = $"Appointment status updated to {status}.";
            return RedirectToAction(nameof(Appointments));
        }

        public IActionResult GetSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var d)) return Json(new List<string>());
            return Json(_apptService.GetAvailableSlots(doctorId, d));
        }
    }
}
