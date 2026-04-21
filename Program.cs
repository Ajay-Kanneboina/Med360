using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;
using MediCore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllersWithViews();

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

using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    db.Database.Migrate();

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
    }

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
