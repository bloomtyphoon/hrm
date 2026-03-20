using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Organization;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.Modules.Attendance.Application.Abstractions;

namespace HRM.Modules.Attendance.Infrastructure.Services;

/// <summary>
/// Resolves the multi-level approval chain for a leave request.
///
/// Chain resolution logic:
///   Level 1 (Manager):        Employee's direct manager
///   Level 2 (DepartmentHead): Department.ManagerId from employee's primary department
///   Level 3 (CompanyLevel):   Department head's manager (next level up in hierarchy)
///
/// Deduplication: If two levels resolve to the same person, the duplicate is skipped.
/// Missing approvers: If a level has no approver, it is skipped.
/// </summary>
internal sealed class ApprovalChainResolver : IApprovalChainResolver
{
    private readonly IPersonnelQuery _personnelQuery;
    private readonly IOrganizationQuery _organizationQuery;
    private readonly IHierarchyScopeResolver _hierarchyResolver;

    public ApprovalChainResolver(
        IPersonnelQuery personnelQuery,
        IOrganizationQuery organizationQuery,
        IHierarchyScopeResolver hierarchyResolver)
    {
        _personnelQuery = personnelQuery;
        _organizationQuery = organizationQuery;
        _hierarchyResolver = hierarchyResolver;
    }

    public async Task<IReadOnlyList<ApprovalChainEntry>> ResolveAsync(
        Guid employeeId,
        int maxLevels,
        CancellationToken cancellationToken = default)
    {
        var info = await _personnelQuery.GetEmployeeApprovalInfoAsync(employeeId, cancellationToken);
        if (info is null)
            return [];

        var chain = new List<ApprovalChainEntry>();
        var usedApprovers = new HashSet<Guid> { employeeId }; // Employee can't approve their own
        var stepOrder = 0;

        // Level 1: Direct Manager
        if (maxLevels >= 1 && info.ManagerId.HasValue && usedApprovers.Add(info.ManagerId.Value))
        {
            chain.Add(new ApprovalChainEntry(++stepOrder, info.ManagerId.Value, ApprovalLevelNames.Manager));
        }

        // Level 2: Department Head
        if (maxLevels >= 2 && info.PrimaryDepartmentId.HasValue)
        {
            var deptHeadId = await _organizationQuery.GetDepartmentManagerIdAsync(
                info.PrimaryDepartmentId.Value, cancellationToken);

            if (deptHeadId.HasValue && usedApprovers.Add(deptHeadId.Value))
            {
                chain.Add(new ApprovalChainEntry(++stepOrder, deptHeadId.Value, ApprovalLevelNames.DepartmentHead));
            }
        }

        // Level 3: Company Level (department head's manager, i.e., next up in hierarchy)
        if (maxLevels >= 3)
        {
            // Use the last resolved approver to find the next level up
            var lastApprover = chain.Count > 0 ? chain[^1].ApproverEmployeeId : info.ManagerId;
            if (lastApprover.HasValue)
            {
                var managerInfo = await _personnelQuery.GetEmployeeApprovalInfoAsync(
                    lastApprover.Value, cancellationToken);

                if (managerInfo?.ManagerId is not null && usedApprovers.Add(managerInfo.ManagerId.Value))
                {
                    chain.Add(new ApprovalChainEntry(++stepOrder, managerInfo.ManagerId.Value, ApprovalLevelNames.CompanyLevel));
                }
            }
        }

        return chain;
    }
}
