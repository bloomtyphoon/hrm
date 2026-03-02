using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.CreateCompany;

/// <summary>
/// Handler for CreateCompanyCommand.
/// Creates a new company in Active status.
///
/// Dependencies:
/// - ICompanyRepository: Check uniqueness and persist company
/// - IUnitOfWork: Commit transaction (injected via UnitOfWorkBehavior)
///
/// Processing Steps:
/// 1. Check code uniqueness (409 Conflict if exists)
/// 2. Create Company aggregate via factory method
/// 3. Add to repository (EF tracks entity)
/// 4. Return company ID
///
/// Error Handling:
/// - Result pattern: Success<Guid> or Failure<Error>
/// - Validation errors: Handled by FluentValidation (ValidationBehavior)
/// - Business rule violations: Returned as domain errors
///
/// Transaction Management:
/// - UnitOfWorkBehavior wraps handler in transaction
/// - Rollback on any error
/// </summary>
internal sealed class CreateCompanyCommandHandler : ICommandHandler<CreateCompanyCommand, Guid>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;

    public CreateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        ITenantContext tenantContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        // 1. Check code uniqueness
        if (await _companyRepository.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            return Result.Failure<Guid>(CompanyErrors.CodeAlreadyExists(request.Code));
        }

        // 2. Create Company aggregate
        // Factory method encapsulates creation logic
        // Sets status to Active
        // Normalizes code to uppercase
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required to create a company.");
        var company = Company.Create(
            tenantId: tenantId,
            code: request.Code,
            name: request.Name,
            taxId: request.TaxId
        );

        // 3. Add to repository
        // EF Core tracks entity (no explicit SaveChanges yet)
        _companyRepository.Add(company);

        // 4. Unit of Work commits transaction
        // UnitOfWorkBehavior calls SaveChangesAsync after handler returns

        // 5. Return company ID
        return Result.Success(company.Id);
    }
}
