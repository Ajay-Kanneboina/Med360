using Microsoft.AspNetCore.Mvc;
using MediCore.Data;
using MediCore.Models;
using MediCore.Services;
using MediCore.ViewModels;

namespace MediCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAuditService _audit;

        public AccountController(AppDbContext db, IAuditService audit)
        {
            _db = db;
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
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetInt32("UserId", user.Id);

            if (user.Role == "Patient")
            {
                var patient = _db.Patients.FirstOrDefault(p => p.UserId == user.Id);
                if (patient != null)
                    HttpContext.Session.SetInt32("PatientId", patient.Id);
            }

            if (user.Role == "Patient")
                return RedirectToAction("Dashboard", "Patient");

            if (user.Role == "Doctor")
                return RedirectToAction("Dashboard", "Doctor");

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Admin");

            if (user.Role == "Nurse")
                return RedirectToAction("Dashboard", "Nurse");

            if (user.Role == "Receptionist")
                return RedirectToAction("Dashboard", "Receptionist");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel input)
        {
            if (input.Password != input.ConfirmPassword)
            { ViewBag.Error = "Passwords do not match."; return View(); }

            if (_db.Users.Any(u => u.Email == input.Email))
            { ViewBag.Error = "Email already registered."; return View(); }

            if (input.Role == "Admin")
            {
                ViewBag.Error = "Cannot register as Admin. An administrator must create admin accounts.";
                return View();
            }

            var isStaffRole = input.Role == "Doctor" || input.Role == "Nurse" || input.Role == "Receptionist";

            var newUser = new User
            {
                FullName = input.FullName,
                Email = input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                Role = input.Role,
                Phone = input.Phone,
                IsActive = !isStaffRole
            };
            _db.Users.Add(newUser);
            _db.SaveChanges();

            if (input.Role == "Patient")
            {
                var linked = _db.Patients.FirstOrDefault(p => p.Email == input.Email);
                if (linked != null)
                {
                    linked.UserId = newUser.Id;
                }
                else
                {
                    _db.Patients.Add(new Patient
                    {
                        FullName = input.FullName,
                        Phone = input.Phone,
                        Email = input.Email,
                        UserId = newUser.Id,
                        RegisteredOn = DateTime.Now
                    });
                }
                _db.SaveChanges();
            }

            _audit.Log(new AuditLog
            {
                Actor    = input.FullName,
                Action   = isStaffRole ? "Registration Pending" : "Account Created / Approved",
                Target   = $"{input.Role} — {input.Email}",
                Category = "User",
                UserId   = newUser.Id
            });

            if (isStaffRole)
            {
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