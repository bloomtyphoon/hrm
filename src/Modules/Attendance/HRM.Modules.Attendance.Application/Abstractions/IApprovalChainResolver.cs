namespace HRM.Modules.Attendance.Application.Abstractions;

/// <summary>
/// Resolves the approval chain for a leave request.
/// Determines who approves at each level: Manager → DepartmentHead → CompanyLevel.
/// </summary>
public interface IApprovalChainResolver
{
    /// <summary>
    /// Resolve the approval chain for an employee, returning ordered approver entries.
    /// Skips levels where no approver exists or where the approver is the same as a previous level.
    /// </summary>
    Task<IReadOnlyList<ApprovalChainEntry>> ResolveAsync(
        Guid employeeId,
        int maxLevels,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single entry in the approval chain.
/// </summary>
public sealed record ApprovalChainEntry(
    int StepOrder,
    Guid ApproverEmployeeId,
    string LevelName);

/// <summary>
/// Well-known approval level names.
/// </summary>
public static class ApprovalLevelNames
{
    public const string Manager = "Manager";
    public const string DepartmentHead = "DepartmentHead";
    public const string CompanyLevel = "CompanyLevel";
}
