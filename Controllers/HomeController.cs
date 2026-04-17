using Microsoft.AspNetCore.Mvc;

namespace MediCore.Controllers
{
    /// <summary>
    /// HomeController — central routing hub after login.
    /// Redirects each role to their own dashboard.
    /// No DB queries here — just reads the session role and redirects.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// GET /Home/Index
        /// Called after login. Routes the user to the correct dashboard
        /// based on their role stored in the session.
        /// </summary>
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            // Not logged in — send to login page
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            // Route each role to their own dashboard
            return role switch
            {
                "Admin"        => RedirectToAction("Index",     "Admin"),
                "Doctor"       => RedirectToAction("Dashboard", "Doctor"),
                "Patient"      => RedirectToAction("Dashboard", "Patient"),
                "Nurse"        => RedirectToAction("Dashboard", "Doctor"),
                "Receptionist" => RedirectToAction("Dashboard", "Doctor"),
                _              => RedirectToAction("Login",     "Account")
            };
        }
    }
}
