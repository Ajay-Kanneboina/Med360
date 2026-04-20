using Microsoft.AspNetCore.Mvc;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _svc;
        private readonly IAdminService _admin;

        public AppointmentController(IAppointmentService svc, IAdminService admin)
        { _svc = svc; _admin = admin; }

        private bool IsStaff()
        {
            var r = HttpContext.Session.GetString("UserRole");
            return r == "Admin" || r == "Doctor" || r == "Nurse" || r == "Receptionist";
        }

        private void SetSidebarBag()
        {
            var s = _admin.GetDashboardStats();
            ViewBag.NewComplaints = s.NewComplaints;
            ViewBag.PendingApprovals = s.PendingApprovals;
        }

        public IActionResult Index(string? status, string? search, string? date)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();
            DateTime? d = null;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var dd)) d = dd;
            ViewBag.StatusFilter = status ?? ""; ViewBag.SearchFilter = search ?? ""; ViewBag.DateFilter = date ?? "";
            ViewBag.Stats = _svc.GetStats();
            return View(_svc.GetAllAppointments(status, search, d));
        }

        public IActionResult Book()
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            SetSidebarBag();
            ViewBag.Patients = _svc.GetAllPatients(); ViewBag.Doctors = _svc.GetAllDoctors();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Book(int patientId, int doctorId, string appointmentDate, string timeSlot, string? notes)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            if (!DateTime.TryParse(appointmentDate, out var dt))
            { TempData["Error"] = "Invalid date."; return RedirectToAction(nameof(Book)); }
            var r = _svc.BookAppointment(patientId, doctorId, dt, timeSlot, notes);
            if (r == null) { TempData["Error"] = "Slot already booked."; return RedirectToAction(nameof(Book)); }
            TempData["Success"] = "Appointment booked!"; return RedirectToAction(nameof(Index));
        }

        public IActionResult GetSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var d)) return Json(new List<string>());
            return Json(_svc.GetAvailableSlots(doctorId, d));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Confirm(int id)
        {
            if (!IsStaff()) return RedirectToAction("Login","Account");
            _svc.UpdateStatus(id, "Confirmed"); TempData["Success"]="Confirmed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Complete(int id)
        {
            if (!IsStaff()) return RedirectToAction("Login","Account");
            _svc.UpdateStatus(id, "Completed"); TempData["Success"]="Completed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Cancel(int id, string reason)
        {
            if (!IsStaff()) return RedirectToAction("Login","Account");
            _svc.CancelAppointment(id, reason ?? "Cancelled by staff"); TempData["Success"]="Cancelled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Reschedule(int id, string newDate, string newSlot)
        {
            if (!IsStaff()) return RedirectToAction("Login","Account");
            if (!DateTime.TryParse(newDate, out var d)) { TempData["Error"]="Invalid date."; return RedirectToAction(nameof(Index)); }
            var r = _svc.Reschedule(id, d, newSlot);
            if (r == null) { TempData["Error"]="Slot taken."; return RedirectToAction(nameof(Index)); }
            TempData["Success"]="Rescheduled."; return RedirectToAction(nameof(Index));
        }
    }
}
