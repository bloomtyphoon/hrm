using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Abstractions.Data;

/// <summary>
/// Query context interface for Personnel module.
/// Provides read-only access to DbSets for query handlers.
/// </summary>
public interface IPersonnelQueryContext
{
    /// <summary>
    /// Employees table (read-only access).
    /// </summary>
    DbSet<Employee> Employees { get; }

    /// <summary>
    /// Employee assignments table (read-only access).
    /// </summary>
    DbSet<EmployeeAssignment> EmployeeAssignments { get; }
}
