using Microsoft.AspNetCore.Mvc;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) => _db = db;

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserName") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
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

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(string fullName, string email,
                                      string password, string confirmPassword, string role)
        {
            if (password != confirmPassword)
            { ViewBag.Error = "Passwords do not match."; return View(); }

            if (_db.Users.Any(u => u.Email == email))
            { ViewBag.Error = "Email already registered."; return View(); }

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

            _db.Users.Add(new User
            {
                FullName     = fullName,
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role         = role,
                PatientId    = patientId,
                IsActive     = true
            });
            _db.SaveChanges();

            TempData["Success"] = "Account created. Please sign in.";
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
