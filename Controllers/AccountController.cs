using Microsoft.AspNetCore.Mvc;
using MediCore.Data;
using MediCore.Models;
using MediCore.Services;

namespace MediCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAuditService _audit;

        public AccountController(AppDbContext db, IAuditService audit)
        {
            _db    = db;
            _audit = audit;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserName") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Your account is pending administrator approval. Please wait until an admin approves your account.";
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole",  user.Role);
            HttpContext.Session.SetInt32("UserId",     user.Id);

            if (user.PatientId.HasValue)
                HttpContext.Session.SetInt32("PatientId", user.PatientId.Value);

            if (user.Role == "Patient")
                return RedirectToAction("Dashboard", "Patient");

            if (user.Role == "Doctor")
                return RedirectToAction("Dashboard", "Doctor");

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Home");

            // Other staff roles (Receptionist, Nurse) for now go to the main home area
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(string fullName, string email,
                                      string password, string confirmPassword, string role, string? phone = null)
        {
            if (password != confirmPassword)
            { ViewBag.Error = "Passwords do not match."; return View(); }

            if (_db.Users.Any(u => u.Email == email))
            { ViewBag.Error = "Email already registered."; return View(); }

            // Prevent self-registration as Admin — admin accounts must be created/approved by an existing admin
            if (role == "Admin")
            {
                ViewBag.Error = "Cannot register as Admin. An administrator must create admin accounts.";
                return View();
            }

            int? patientId = null;
            if (role == "Patient")
            {
                var linked = _db.Patients.FirstOrDefault(p => p.Email == email);
                if (linked != null)
                {
                    patientId = linked.Id;
                }
                else
                {
                    var np = new Patient { FullName = fullName, Email = email, RegisteredOn = DateTime.Now };
                    _db.Patients.Add(np);
                    _db.SaveChanges();
                    patientId = np.Id;
                }
            }

            // Users who request staff roles must be approved by an admin before they can login
            var isStaffRole = role == "Doctor" || role == "Nurse" || role == "Receptionist";

            _db.Users.Add(new User
            {
                FullName     = fullName,
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role         = role,
                Phone        = phone,
                PatientId    = patientId,
                IsActive     = !isStaffRole // staff roles are inactive until approved by admin
            });
            _db.SaveChanges();

            _audit.Log(fullName,
                       isStaffRole ? "Registration Pending" : "Account Created / Approved",
                       $"{role} — {email}", "User", null);

            if (isStaffRole)
            {
                // Inform user that admin approval is required
                TempData["Info"] = "Your account request has been received. An administrator must approve your account before you can sign in.";
            }
            else
            {
                TempData["Success"] = "Account created. Please sign in.";
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
    }
}
