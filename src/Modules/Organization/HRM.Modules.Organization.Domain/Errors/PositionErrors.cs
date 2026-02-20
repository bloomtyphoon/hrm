using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Organization.Domain.Errors;

/// <summary>
/// Static error definitions for Position operations.
/// </summary>
public static class PositionErrors
{
    public static ConflictError CodeAlreadyExists(string code, Guid companyId) =>
        new("Position.CodeAlreadyExists",
            $"Position code '{code}' already exists in company '{companyId}'.");

    public static NotFoundError NotFound(Guid id) =>
        new("Position.NotFound",
            $"Position with ID '{id}' was not found.");

    public static NotFoundError CompanyNotFound(Guid companyId) =>
        new("Position.CompanyNotFound",
            $"Company with ID '{companyId}' was not found.");

    public static NotFoundError DepartmentNotFound(Guid departmentId) =>
        new("Position.DepartmentNotFound",
            $"Department with ID '{departmentId}' was not found.");

    public static ValidationError DepartmentInDifferentCompany(Guid departmentId, Guid companyId) =>
        new("Position.DepartmentInDifferentCompany",
            $"Department '{departmentId}' does not belong to company '{companyId}'.");
}
