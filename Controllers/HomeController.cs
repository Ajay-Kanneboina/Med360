using Microsoft.AspNetCore.Mvc;

namespace MediCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            return role switch
            {
                "Admin"        => RedirectToAction("Index",     "Admin"),
                "Doctor"       => RedirectToAction("Dashboard", "Doctor"),
                "Patient"      => RedirectToAction("Dashboard", "Patient"),
                "Nurse"        => RedirectToAction("Dashboard", "Nurse"),
                "Receptionist" => RedirectToAction("Dashboard", "Receptionist"),
                _              => RedirectToAction("Login",     "Account")
            };
        }
    }
}
