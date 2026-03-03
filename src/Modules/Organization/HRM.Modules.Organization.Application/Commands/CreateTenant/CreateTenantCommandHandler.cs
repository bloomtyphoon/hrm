using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.CreateTenant;

internal sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (await _tenantRepository.ExistsByCodeAsync(request.Code, cancellationToken))
            return Result.Failure<Guid>(TenantErrors.CodeAlreadyExists(request.Code));

        var tenant = Tenant.Create(request.Code, request.Name);
        _tenantRepository.Add(tenant);

        return Result.Success(tenant.Id);
    }
}
