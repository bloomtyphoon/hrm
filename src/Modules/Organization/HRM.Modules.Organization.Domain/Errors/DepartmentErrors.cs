using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Organization.Domain.Errors;

/// <summary>
/// Static error definitions for Department operations.
/// </summary>
public static class DepartmentErrors
{
    public static ConflictError CodeAlreadyExists(string code, Guid companyId) =>
        new("Department.CodeAlreadyExists",
            $"Department code '{code}' already exists in company '{companyId}'.");

    public static NotFoundError NotFound(Guid id) =>
        new("Department.NotFound",
            $"Department with ID '{id}' was not found.");

    public static NotFoundError CompanyNotFound(Guid companyId) =>
        new("Department.CompanyNotFound",
            $"Company with ID '{companyId}' was not found.");

    public static NotFoundError ParentNotFound(Guid parentId) =>
        new("Department.ParentNotFound",
            $"Parent department with ID '{parentId}' was not found.");

    public static ValidationError ParentInDifferentCompany(Guid parentId, Guid companyId) =>
        new("Department.ParentInDifferentCompany",
            $"Parent department '{parentId}' does not belong to company '{companyId}'.");
}
