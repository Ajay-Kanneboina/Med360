using Microsoft.EntityFrameworkCore;
using MediCore.Models;

namespace MediCore.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User>      Users      { get; set; }
        public DbSet<Patient>   Patients   { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Record>    Records    { get; set; }

        protected override void OnModelCreating(ModelBuilder m)
        {
            base.OnModelCreating(m);

            m.Entity<Complaint>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.Complaints)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            m.Entity<Record>()
                .HasOne(r => r.Patient)
                .WithMany(p => p.Records)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
