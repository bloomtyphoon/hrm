using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Services;

/// <summary>
/// Resolves the multi-level approval chain for a leave request
/// using LOCAL snapshots (no cross-module queries).
///
/// Chain resolution logic:
///   Level 1 (Manager):        Employee's direct manager (from EmployeeOrganizationSnapshot)
///   Level 2 (DepartmentHead): Department's manager (from DepartmentSnapshot)
///   Level 3 (CompanyLevel):   Last approver's manager (from EmployeeOrganizationSnapshot)
///
/// Snapshots are kept in sync via integration events from Personnel and Organization modules.
///
/// Deduplication: If two levels resolve to the same person, the duplicate is skipped.
/// Missing approvers: If a level has no approver, it is skipped.
/// </summary>
internal sealed class ApprovalChainResolver : IApprovalChainResolver
{
    private readonly IAttendanceQueryContext _queryContext;

    public ApprovalChainResolver(IAttendanceQueryContext queryContext)
    {
        _queryContext = queryContext;
    }

    public async Task<IReadOnlyList<ApprovalChainEntry>> ResolveAsync(
        Guid employeeId,
        int maxLevels,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _queryContext.EmployeeOrganizationSnapshots
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, cancellationToken);

        if (snapshot is null)
            return [];

        var chain = new List<ApprovalChainEntry>();
        var usedApprovers = new HashSet<Guid> { employeeId }; // Employee can't approve their own
        var stepOrder = 0;

        // Level 1: Direct Manager
        if (maxLevels >= 1 && snapshot.ManagerId.HasValue && usedApprovers.Add(snapshot.ManagerId.Value))
        {
            chain.Add(new ApprovalChainEntry(++stepOrder, snapshot.ManagerId.Value, ApprovalLevelNames.Manager));
        }

        // Level 2: Department Head (from local DepartmentSnapshot)
        if (maxLevels >= 2 && snapshot.PrimaryDepartmentId.HasValue)
        {
            var deptSnapshot = await _queryContext.DepartmentSnapshots
                .FirstOrDefaultAsync(d => d.DepartmentId == snapshot.PrimaryDepartmentId.Value, cancellationToken);

            if (deptSnapshot?.ManagerEmployeeId is not null && usedApprovers.Add(deptSnapshot.ManagerEmployeeId.Value))
            {
                chain.Add(new ApprovalChainEntry(++stepOrder, deptSnapshot.ManagerEmployeeId.Value, ApprovalLevelNames.DepartmentHead));
            }
        }

        // Level 3: Company Level (last approver's manager from local snapshot)
        if (maxLevels >= 3)
        {
            var lastApprover = chain.Count > 0 ? chain[^1].ApproverEmployeeId : snapshot.ManagerId;
            if (lastApprover.HasValue)
            {
                var managerSnapshot = await _queryContext.EmployeeOrganizationSnapshots
                    .FirstOrDefaultAsync(s => s.EmployeeId == lastApprover.Value, cancellationToken);

                if (managerSnapshot?.ManagerId is not null && usedApprovers.Add(managerSnapshot.ManagerId.Value))
                {
                    chain.Add(new ApprovalChainEntry(++stepOrder, managerSnapshot.ManagerId.Value, ApprovalLevelNames.CompanyLevel));
                }
            }
        }

        return chain;
    }
}
