using Microsoft.AspNetCore.Mvc;
using MediCore.Models;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class PatientController : Controller
    {
        private readonly IPatientService  _patientService;
        private readonly IAuditService  _audit;
        private readonly IAppointmentService _apptService;

        public PatientController(IPatientService patientService, IAuditService audit, IAppointmentService apptService)
        {
            _patientService = patientService;
            _audit = audit;
            _apptService = apptService;
        }

        private bool IsPatient() =>
            HttpContext.Session.GetString("UserRole") == "Patient";

        private int Pid() => HttpContext.Session.GetInt32("PatientId") ?? 0;

        public IActionResult Dashboard()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            int pid = Pid();
            if (pid == 0) return View("NoRecord");

            var patient = _patientService.GetMyProfile(pid);
            if (patient == null) return View("NoRecord");

            return View(patient);
        }

        public IActionResult MyRecords(string? status, string? search)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";

            var recordStats = _patientService.GetRecordStats(Pid());
            ViewBag.TotalRecords = recordStats.Total;
            ViewBag.ActiveCount  = recordStats.Active;
            ViewBag.ClosedCount  = recordStats.Closed;

            return View(_patientService.GetMyRecords(Pid(), status, search));
        }

        public IActionResult RecordDetail(int id)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            var record = _patientService.GetMyRecord(id, Pid());
            if (record == null) return NotFound();

            return View(record);
        }

        public IActionResult MyComplaints()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            return View(_patientService.GetMyComplaints(Pid()));
        }

        public IActionResult SubmitComplaint()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SubmitComplaint(Complaint input)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(input.Description))
            {
                ViewBag.Error = "Please describe your problem.";
                return View();
            }

            _patientService.SubmitComplaint(Pid(), input);

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";
            var shortDesc  = input.Description.Length > 60 ? input.Description[..60] + "…" : input.Description;

            _audit.Log(new AuditLog
            {
                Actor    = patientName,
                Action   = "Complaint Submitted",
                Target   = shortDesc,
                Category = "Complaint",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = "Problem submitted. A doctor will respond soon.";
            return RedirectToAction(nameof(MyComplaints));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditComplaint(int id, Complaint input)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(input.Description))
            {
                TempData["Error"] = "Please describe your problem.";
                return RedirectToAction(nameof(MyComplaints));
            }

            if (!_patientService.UpdateComplaint(id, Pid(), input))
                return NotFound();

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";
            _audit.Log(new AuditLog
            {
                Actor    = patientName,
                Action   = "Complaint Updated",
                Target   = $"Complaint #{id}",
                Category = "Complaint",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = "Problem updated. The doctor will be notified.";
            return RedirectToAction(nameof(MyComplaints));
        }

        public IActionResult MyProfile()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            var patient = _patientService.GetMyProfile(Pid());
            if (patient == null) return View("NoRecord");

            return View(patient);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditProfile(Patient input)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (!_patientService.UpdateProfile(Pid(), input))
                return View("NoRecord");

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(MyProfile));
        }

        public IActionResult MyAppointments()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            return View(_apptService.GetPatientAppointments(Pid()));
        }

        public IActionResult SendAppointmentRequest()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            ViewBag.PastRequests = _apptService.GetPatientRequests(Pid());
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SendAppointmentRequest(AppointmentRequest input)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(input.Message))
            {
                ViewBag.Error = "Please describe what kind of appointment you need.";
                ViewBag.PastRequests = _apptService.GetPatientRequests(Pid());
                return View();
            }

            _apptService.SendRequest(Pid(), input);

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";

            _audit.Log(new AuditLog
            {
                Actor    = patientName,
                Action   = "Appointment Requested",
                Target   = input.Message.Length > 60 ? input.Message[..60] + "…" : input.Message,
                Category = "Appointment",
                UserId   = HttpContext.Session.GetInt32("UserId")
            });

            TempData["Success"] = "Your request has been sent to the receptionist. They will book your appointment shortly.";
            return RedirectToAction(nameof(MyAppointments));
        }
    }
}
