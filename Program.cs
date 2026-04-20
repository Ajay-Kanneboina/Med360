using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;
using MediCore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllersWithViews();

// ── Service Layer Registration ────────────────────────────────────────────
// AddScoped: one instance per HTTP request — correct for services that use DbContext
// The controller depends on the interface (IAdminService), not the concrete class.
// This allows swapping implementations or mocking in unit tests without changing controllers.
builder.Services.AddScoped<IAdminService,       AdminService>();
builder.Services.AddScoped<IDoctorService,      DoctorService>();
builder.Services.AddScoped<IPatientService,     PatientService>();
builder.Services.AddScoped<IAuditService,       AuditService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout        = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly    = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Account}/{action=Login}/{id?}");

// ── Database migration + bootstrap admin ──────────────────────────────────
// Only a single bootstrap admin is ever seeded — and its credentials come
// from configuration (appsettings.json / env vars), NOT hardcoded in source.
// All other users (doctors, nurses, receptionists, patients) are expected to
// register through /Account/Register and be approved by an admin where required.
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    db.Database.Migrate();

    // Only create a bootstrap admin if no admin exists at all in the database.
    // This is needed because /Account/Register explicitly forbids self-registering
    // as Admin — so at least one admin must exist for the system to be usable.
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var adminEmail    = config["BootstrapAdmin:Email"];
        var adminPassword = config["BootstrapAdmin:Password"];
        var adminName     = config["BootstrapAdmin:FullName"];

        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            !string.IsNullOrWhiteSpace(adminPassword) &&
            !string.IsNullOrWhiteSpace(adminName))
        {
            db.Users.Add(new User
            {
                FullName     = adminName,
                Email        = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role         = "Admin",
                IsActive     = true
            });
            db.SaveChanges();
        }
        // If config values are missing, no admin is created and the operator must
        // configure BootstrapAdmin in appsettings.json (or env vars) and restart.
    }

    // ── Seed existing events into AuditLogs table (runs once) ────────────
    // Before this fix, audit events were assembled on-the-fly from other tables.
    // Now we have a real AuditLogs table. On first run, we copy existing events
    // into it so history is not lost. After that, every action writes directly.
    if (!db.AuditLogs.Any())
    {
        var seeds = new List<AuditLog>();

        foreach (var u in db.Users.ToList())
            seeds.Add(new AuditLog
            {
                Actor     = u.FullName,
                Action    = u.IsActive ? "Account Created / Approved" : "Registration Pending",
                Target    = $"{u.Role} — {u.Email}",
                Category  = "User",
                Timestamp = u.CreatedAt
            });

        foreach (var r in db.Records.Include(x => x.Patient).ToList())
            seeds.Add(new AuditLog
            {
                Actor     = r.DoctorName ?? "Doctor",
                Action    = "Medical Record Added",
                Target    = r.Patient?.FullName ?? $"Patient #{r.PatientId}",
                Category  = "Record",
                Timestamp = r.CreatedAt
            });

        foreach (var c in db.Complaints.Include(x => x.Patient).ToList())
        {
            seeds.Add(new AuditLog
            {
                Actor     = c.Patient?.FullName ?? $"Patient #{c.PatientId}",
                Action    = "Complaint Submitted",
                Target    = c.Description.Length > 60 ? c.Description[..60] + "…" : c.Description,
                Category  = "Complaint",
                Timestamp = c.SubmittedAt
            });

            if (c.RespondedAt.HasValue)
                seeds.Add(new AuditLog
                {
                    Actor     = c.RespondedBy ?? "Staff",
                    Action    = "Complaint Responded",
                    Target    = c.Patient?.FullName ?? $"Patient #{c.PatientId}",
                    Category  = "Complaint",
                    Timestamp = c.RespondedAt.Value
                });
        }

        db.AuditLogs.AddRange(seeds);
        db.SaveChanges();
    }
}

app.Run();
