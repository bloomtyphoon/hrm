using System.Reflection;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.Persistence;

/// <summary>
/// DbContext for Personnel module.
/// Inherits from ModuleDbContext for Unit of Work, domain events, soft delete, audit trail.
/// </summary>
public sealed class PersonnelDbContext : ModuleDbContext, IPersonnelQueryContext
{
    public PersonnelDbContext(
        DbContextOptions<PersonnelDbContext> options,
        IPublisher publisher)
        : base(options, publisher)
    {
    }

    public override string ModuleName => "Personnel";

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeAssignment> EmployeeAssignments => Set<EmployeeAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Personnel");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure Employee entity
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EmployeeCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasMaxLength(20);

            entity.Ignore(e => e.FullName);

            entity.HasIndex(e => e.EmployeeCode)
                .IsUnique()
                .HasDatabaseName("IX_Employees_EmployeeCode");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Employees_Email");

            entity.HasIndex(e => e.ManagerId)
                .HasDatabaseName("IX_Employees_ManagerId");

            entity.HasIndex(e => e.PrimaryCompanyId)
                .HasDatabaseName("IX_Employees_PrimaryCompanyId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Employees_Status");

            // Self-referencing manager relationship
            entity.HasOne(e => e.Manager)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee -> Assignments (one-to-many)
            entity.HasMany<EmployeeAssignment>("_assignments")
                .WithOne(a => a.Employee)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation("_assignments")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Configure EmployeeAssignment entity
        modelBuilder.Entity<EmployeeAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EmployeeId)
                .HasDatabaseName("IX_EmployeeAssignments_EmployeeId");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("IX_EmployeeAssignments_CompanyId");

            entity.HasIndex(e => e.DepartmentId)
                .HasDatabaseName("IX_EmployeeAssignments_DepartmentId");

            entity.HasIndex(e => e.PositionId)
                .HasDatabaseName("IX_EmployeeAssignments_PositionId");

            entity.HasIndex(e => new { e.EmployeeId, e.CompanyId, e.DepartmentId, e.PositionId })
                .HasDatabaseName("IX_EmployeeAssignments_Composite");
        });
    }
}
