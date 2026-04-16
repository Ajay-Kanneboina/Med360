using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllersWithViews();

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
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.Add(new User
        {
            FullName     = "System Admin",
            Email        = "admin@medicore.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role         = "Admin",
            IsActive     = true
        });

        db.Users.Add(new User
        {
            FullName     = "Dr. Sarah Wilson",
            Email        = "doctor@medicore.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor@123"),
            Role         = "Doctor",
            IsActive     = true
        });

        db.SaveChanges();

        var patient = new Patient
        {
            FullName         = "John Doe",
            DateOfBirth      = new DateTime(1990, 5, 15),
            Gender           = "Male",
            BloodGroup       = "O+",
            Phone            = "9876543210",
            Email            = "patient@medicore.com",
            EmergencyContact = "Jane Doe - 9876543211",
            RegisteredOn     = DateTime.Now
        };
        db.Patients.Add(patient);
        db.SaveChanges();

        db.Users.Add(new User
        {
            FullName     = "John Doe",
            Email        = "patient@medicore.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patient@123"),
            Role         = "Patient",
            PatientId    = patient.Id,
            IsActive     = true
        });
        db.SaveChanges();
    }
}

app.Run();
