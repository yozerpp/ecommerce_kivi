using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.WebImpl.Data.Identity;

public class RoleStore : IRoleStore<Role>
{
    private readonly IRepository<Role> _roleRepository;

    public RoleStore(IRepository<Role> roleRepository) {
        _roleRepository = roleRepository;
    }
    public void Dispose() {
    }

    public Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken) {
        return _roleRepository.SaveAsync(role, true,cancellationToken).IsCompleted?
            Task.FromResult(IdentityResult.Success) :
            Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Failed to create role." }));;
    }

    public Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken) {
        return _roleRepository.UpdateAsync(role, true, cancellationToken).IsCompleted?
                Task.FromResult(IdentityResult.Success):
                Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Failed to update role." }));
    }

    public Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken) {
        return _roleRepository.DeleteAsync(role, true, cancellationToken).IsCompleted?
                Task.FromResult(IdentityResult.Success):
                Task.FromResult(IdentityResult.Failed(new IdentityError{ Description = "Failed to delete role." }));
    }

    public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken) {
        return Task.FromResult(role.Id.ToString());
    }

    public Task<string> GetRoleNameAsync(Role role, CancellationToken cancellationToken) {
        return Task.FromResult(role.Name);
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(roleName);
        return Task.Run(() => {
            role.Name = roleName;
            _roleRepository.UpdateAsync(role, true, cancellationToken);
        }, cancellationToken);
    }

    public Task<string> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken) {
        return Task.FromResult(role.Name.ToUpper());
    }

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken) {
        var id = uint.Parse(roleId);
        return Task.Run(() => _roleRepository.First(r => r.Id == id), cancellationToken);
    }

    public Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) {
        return Task.Run(() => _roleRepository.First(r => r.Name.ToUpper() == normalizedRoleName), cancellationToken);
    }
}