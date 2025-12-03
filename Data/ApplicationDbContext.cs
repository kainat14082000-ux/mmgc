using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;

namespace MMGC.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Nurse> Nurses { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Procedure> Procedures { get; set; }
    public DbSet<LabTestCategory> LabTestCategories { get; set; }
    public DbSet<LabTest> LabTests { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships and constraints
        builder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Appointment>()
            .HasOne(a => a.Nurse)
            .WithMany(n => n.Appointments)
            .HasForeignKey(a => a.NurseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Procedure>()
            .HasOne(p => p.Patient)
            .WithMany(p => p.Procedures)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procedure>()
            .HasOne(p => p.Doctor)
            .WithMany(d => d.Procedures)
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Procedure>()
            .HasOne(p => p.Nurse)
            .WithMany(n => n.Procedures)
            .HasForeignKey(p => p.NurseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.Patient)
            .WithMany(p => p.LabTests)
            .HasForeignKey(lt => lt.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.LabTestCategory)
            .WithMany(c => c.LabTests)
            .HasForeignKey(lt => lt.LabTestCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.Procedure)
            .WithMany(p => p.LabTests)
            .HasForeignKey(lt => lt.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Transaction>()
            .HasOne(t => t.Patient)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transaction>()
            .HasOne(t => t.Procedure)
            .WithMany()
            .HasForeignKey(t => t.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Prescription>()
            .HasOne(pr => pr.Patient)
            .WithMany(p => p.Prescriptions)
            .HasForeignKey(pr => pr.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prescription>()
            .HasOne(pr => pr.Doctor)
            .WithMany()
            .HasForeignKey(pr => pr.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prescription>()
            .HasOne(pr => pr.Procedure)
            .WithMany()
            .HasForeignKey(pr => pr.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<DoctorSchedule>()
            .HasOne(ds => ds.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.Entity<Patient>()
            .HasIndex(p => p.MRNumber)
            .IsUnique();

        builder.Entity<Appointment>()
            .HasIndex(a => a.AppointmentDate);

        builder.Entity<Transaction>()
            .HasIndex(t => t.TransactionDate);
    }
}

