using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Attendance.Domain.Errors;

public static class LeaveErrors
{
    public static NotFoundError LeaveTypeNotFound(Guid id) =>
        new("LeaveType.NotFound", $"Leave type '{id}' was not found.");

    public static NotFoundError LeaveRequestNotFound(Guid id) =>
        new("LeaveRequest.NotFound", $"Leave request '{id}' was not found.");

    public static ForbiddenError ManageTypesForbidden() =>
        new("LeaveType.Manage.Forbidden", "You don't have permission to manage leave types.");

    public static ForbiddenError RequestForbidden() =>
        new("LeaveRequest.Forbidden", "You don't have permission to submit leave requests.");

    public static ForbiddenError ApproveForbidden() =>
        new("LeaveRequest.Approve.Forbidden", "You don't have permission to approve/reject leave requests.");

    public static ValidationError EmployeeNotResolved() =>
        new("Leave.EmployeeNotResolved",
            "Could not resolve employee from current user context.");

    public static ConflictError DuplicateTypeName(string name) =>
        new("LeaveType.DuplicateName", $"A leave type with name '{name}' already exists.");

    public static ForbiddenError SelfCancelNotAllowed() =>
        new("LeaveRequest.SelfCancel.NotAllowed", "Self-cancellation of leave requests is not allowed per approval settings.");

    public static ForbiddenError ManageSettingsForbidden() =>
        new("LeaveApprovalSettings.Manage.Forbidden", "You don't have permission to manage leave approval settings.");

    public static ForbiddenError NotCurrentApprover() =>
        new("LeaveRequest.Approve.NotCurrentApprover",
            "You are not the designated approver for the current approval step.");

    public static ValidationError NoApprovalChainResolved() =>
        new("LeaveRequest.NoApprovalChain",
            "Could not resolve any approvers in the approval chain. Ensure the employee has a manager assigned.");

    public static ValidationError ApprovalStepNotFound(int stepOrder) =>
        new("LeaveRequest.ApprovalStep.NotFound",
            $"Approval step {stepOrder} was not found for this leave request.");
}
