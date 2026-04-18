using Microsoft.AspNetCore.Mvc;
using MediCore.Models;
using MediCore.Services;

namespace MediCore.Controllers
{
    /// <summary>
    /// PatientController — thin HTTP adapter for the patient portal.
    ///
    /// RESPONSIBILITIES (what this controller does):
    ///   1. Verify session role is Patient (auth guard)
    ///   2. Read PatientId from session
    ///   3. Call the appropriate IPatientService method
    ///   4. Map results to ViewBag / Model and return View or Redirect
    ///
    /// NOT RESPONSIBLE FOR (all moved to PatientService):
    ///   • Setting default Status = "Active" on new complaints
    ///   • Setting IsRead = false on create/edit
    ///   • Setting SubmittedAt / UpdatedAt timestamps
    ///   • Filtering records by status or keyword
    ///   • Ownership checks (patient can only see their own records)
    /// </summary>
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly IAuditService   _audit;

        public PatientController(IPatientService patientService, IAuditService audit)
        {
            _patientService = patientService;
            _audit          = audit;
        }

        // ── Auth helpers ──────────────────────────────────────────────────

        /// <summary>Returns true only if the current session role is Patient.</summary>
        private bool IsPatient() =>
            HttpContext.Session.GetString("UserRole") == "Patient";

        /// <summary>
        /// Returns the PatientId from the session (set at login).
        /// Returns 0 if missing — treated as unlinked account by the service.
        /// </summary>
        private int Pid() => HttpContext.Session.GetInt32("PatientId") ?? 0;

        // ── Dashboard ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /Patient/Dashboard
        /// Lightweight landing page — just the patient identity for the hero
        /// card and quick-action tiles. All detail views (records, complaints,
        /// profile) load their own data on their own pages.
        /// </summary>
        public IActionResult Dashboard()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            int pid = Pid();
            if (pid == 0) return View("NoRecord"); // unlinked account

            var patient = _patientService.GetMyProfile(pid);
            if (patient == null) return View("NoRecord");

            return View(patient);
        }

        // ── My Medical Records ────────────────────────────────────────────

        /// <summary>
        /// GET /Patient/MyRecords?status=…&search=…
        /// Service handles filtering and ownership — controller passes parameters through.
        /// </summary>
        public IActionResult MyRecords(string? status, string? search)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            ViewBag.StatusFilter = status ?? "";
            ViewBag.SearchFilter = search ?? "";

            // Record summary counts for the summary pills at top of page
            var recordStats = _patientService.GetRecordStats(Pid());
            ViewBag.TotalRecords = recordStats.Total;
            ViewBag.ActiveCount  = recordStats.Active;
            ViewBag.ClosedCount  = recordStats.Closed;

            return View(_patientService.GetMyRecords(Pid(), status, search));
        }

        /// <summary>
        /// GET /Patient/RecordDetail/{id}
        /// Service performs the ownership check — returns null if mismatch.
        /// Controller returns 404 without exposing why (security by obscurity).
        /// </summary>
        public IActionResult RecordDetail(int id)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            // Service verifies this record belongs to this patient
            var record = _patientService.GetMyRecord(id, Pid());
            if (record == null) return NotFound();

            return View(record);
        }

        // ── Complaints ────────────────────────────────────────────────────

        /// <summary>
        /// GET /Patient/MyComplaints
        /// Service returns complaints ordered by newest first.
        /// </summary>
        public IActionResult MyComplaints()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            return View(_patientService.GetMyComplaints(Pid()));
        }

        /// <summary>
        /// GET /Patient/SubmitComplaint
        /// Renders the blank complaint submission form.
        /// </summary>
        public IActionResult SubmitComplaint()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            return View();
        }

        /// <summary>
        /// POST /Patient/SubmitComplaint
        /// Validates input and delegates creation to the service.
        /// Service sets Status, IsRead, and timestamps — not this controller.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SubmitComplaint(string description, string? additionalNotes)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            // Minimal input validation — description must not be empty
            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Please describe your problem.";
                return View();
            }

            _patientService.SubmitComplaint(Pid(), description, additionalNotes);

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";
            var shortDesc   = description.Length > 60 ? description[..60] + "…" : description;
            _audit.Log(patientName, "Complaint Submitted", shortDesc, "Complaint");

            TempData["Success"] = "Problem submitted. A doctor will respond soon.";
            return RedirectToAction(nameof(MyComplaints));
        }

        /// <summary>
        /// GET /Patient/EditComplaint/{id}
        /// Deprecated — editing now happens inline on MyComplaints. Redirect
        /// any stale links back to the list.
        /// </summary>
        public IActionResult EditComplaint(int id)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            return RedirectToAction(nameof(MyComplaints));
        }

        /// <summary>
        /// POST /Patient/EditComplaint/{id}
        /// Passes updated text to the service.
        /// Service resets IsRead = false to re-notify the doctor — controller
        /// does NOT set IsRead directly.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditComplaint(int id, string description, string? additionalNotes)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["Error"] = "Please describe your problem.";
                return RedirectToAction(nameof(MyComplaints));
            }

            if (!_patientService.UpdateComplaint(id, Pid(), description, additionalNotes))
                return NotFound();

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";
            _audit.Log(patientName, "Complaint Updated", $"Complaint #{id}", "Complaint");

            TempData["Success"] = "Problem updated. The doctor will be notified.";
            return RedirectToAction(nameof(MyComplaints));
        }

        // ── Profile ───────────────────────────────────────────────────────

        /// <summary>
        /// GET /Patient/MyProfile
        /// Read-only view of patient demographics and medical history.
        /// Editable contact fields have an Edit button linking to EditProfile.
        /// </summary>
        public IActionResult MyProfile()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            var patient = _patientService.GetMyProfile(Pid());
            if (patient == null) return View("NoRecord");

            return View(patient);
        }

        /// <summary>
        /// GET /Patient/EditProfile
        /// Deprecated — editing now happens inline on MyProfile. Redirect any
        /// stale links here so the user lands on the right page.
        /// </summary>
        public IActionResult EditProfile()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            return RedirectToAction(nameof(MyProfile));
        }

        /// <summary>
        /// POST /Patient/EditProfile
        /// Accepts all patient-editable fields: contact info plus clinical info
        /// (DOB, gender, blood group, medical history). FullName is not posted
        /// and is not changed here.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditProfile(string?   phone,
                                         string?   email,
                                         string?   address,
                                         string?   emergencyContact,
                                         DateTime? dateOfBirth,
                                         string?   gender,
                                         string?   bloodGroup,
                                         string?   medicalHistory)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (!_patientService.UpdateProfile(Pid(),
                    phone, email, address, emergencyContact,
                    dateOfBirth, gender, bloodGroup, medicalHistory))
                return View("NoRecord");

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(MyProfile));
        }
    }
}
