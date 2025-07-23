using Ecommerce.Bl.Concrete;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.WebImpl.Data.Identity;

public class PasswordStore : IUserPasswordStore<Customer>
{
    private readonly IRepository<Customer> _userRepository;
    private readonly UserManager.HashFunction _hashFunction;

    public PasswordStore(IRepository<Customer> userRepository, UserManager.HashFunction hashFunction) {
        _userRepository = userRepository;
        _hashFunction = hashFunction;
    }
    public Task<string> GetUserIdAsync(Customer customer, CancellationToken cancellationToken) {
        var e = customer.NormalizedEmail;
        var p = _hashFunction(customer.PasswordHash);
        return Task.Run(() => {
            return _userRepository.FirstP(u => u.Id, u => u.NormalizedEmail == e && u.PasswordHash == p ).ToString();
        }, cancellationToken);
    }

    public Task<string> GetUserNameAsync(Customer customer, CancellationToken cancellationToken) {
        return Task.FromResult(customer.NormalizedEmail);
    }

    public Task SetUserNameAsync(Customer customer, string? userName, CancellationToken cancellationToken) {
        if(userName == null) throw new ArgumentNullException(nameof(userName));
        return Task.Run(() => {
            customer.NormalizedEmail = userName;
            _userRepository.Update(customer);
        }, cancellationToken);
    }

    public Task<string> GetNormalizedUserNameAsync(Customer customer, CancellationToken cancellationToken) {
        return Task.FromResult(customer.NormalizedEmail.ToUpper());
    }

    public Task SetNormalizedUserNameAsync(Customer customer, string? normalizedName, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(Customer customer, CancellationToken cancellationToken) {
        var ret =_userRepository.AddAsync(customer, true, cancellationToken).Result;
        if (ret == null) {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Failed to create user." }));
        }
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(Customer customer, CancellationToken cancellationToken) {
        return _userRepository.UpdateAsync(customer,true, cancellationToken).ContinueWith(r => {
            if(cancellationToken.IsCancellationRequested || r.IsCanceled)
                return IdentityResult.Failed(new IdentityError { Description = "Update operation was cancelled." });
            else if(r.IsFaulted)
                return IdentityResult.Failed(new  IdentityError { Description = "Update operation failed.\n" + r.Exception.InnerException?.Message });
            else return IdentityResult.Success;
        }, cancellationToken);
    }

    public Task<IdentityResult> DeleteAsync(Customer customer, CancellationToken cancellationToken) {
        return _userRepository.DeleteAsync(customer, true,cancellationToken).IsCompleted?
            Task.FromResult(IdentityResult.Success) :
            Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Failed to delete user." }));;
    }

    public Task<Customer?> FindByIdAsync(string userId, CancellationToken cancellationToken) {
        return Task.Run(() => {
            var id = uint.Parse(userId);
            return _userRepository.First(u => u.Id == id);
        }, cancellationToken);
    }

    public Task<Customer?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) {
        return Task.Run(() => {
            return _userRepository.First(u => u.NormalizedEmail.ToUpper() == normalizedUserName);
        });
    }

    public Task SetPasswordHashAsync(Customer customer, string? passwordHash, CancellationToken cancellationToken) {
        return passwordHash!=null? Task.Run(() => {
            customer.PasswordHash = passwordHash;
            return _userRepository.UpdateExpr([
            (u =>u.PasswordHash ,passwordHash!)
                ]
                , u => u.Id == customer.Id);
        }, cancellationToken):throw new ArgumentNullException(nameof(passwordHash));
    }

    public Task<string> GetPasswordHashAsync(Customer customer, CancellationToken cancellationToken) {
        return Task.FromResult(customer.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(Customer customer, CancellationToken cancellationToken) {
        return Task.FromResult(customer.PasswordHash != null);
    }
    public void Dispose() {
        _userRepository.Dispose();
    }
}