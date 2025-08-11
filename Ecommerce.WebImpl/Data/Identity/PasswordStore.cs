using Ecommerce.Bl.Concrete;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.WebImpl.Data.Identity;

public class PasswordStore : IUserPasswordStore<User>
{
    private readonly IRepository<User> _userRepository;
    private readonly UserManager.HashFunction _hashFunction;

    public PasswordStore(IRepository<User> userRepository, UserManager.HashFunction hashFunction) {
        _userRepository = userRepository;
        _hashFunction = hashFunction;
    }
    public Task<string> GetUserIdAsync(User User, CancellationToken cancellationToken) {
        var e = User.NormalizedEmail;
        var p = _hashFunction(User.PasswordHash);
        return Task.Run(() => {
            return _userRepository.FirstP(u => u.Id, u => u.NormalizedEmail == e && u.PasswordHash == p ).ToString();
        }, cancellationToken);
    }

    public Task<string> GetUserNameAsync(User User, CancellationToken cancellationToken) {
        return Task.FromResult(User.NormalizedEmail);
    }

    public Task SetUserNameAsync(User User, string? userName, CancellationToken cancellationToken) {
        if(userName == null) throw new ArgumentNullException(nameof(userName));
        return Task.Run(() => {
            User.NormalizedEmail = userName;
            _userRepository.Update(User);
        }, cancellationToken);
    }

    public Task<string> GetNormalizedUserNameAsync(User User, CancellationToken cancellationToken) {
        return Task.FromResult(User.NormalizedEmail.ToUpper());
    }

    public Task SetNormalizedUserNameAsync(User User, string? normalizedName, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(User User, CancellationToken cancellationToken) {
        var ret =_userRepository.AddAsync(User, true, cancellationToken).Result;
        if (ret == null) {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Failed to create user." }));
        }
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(User User, CancellationToken cancellationToken) {
        return _userRepository.UpdateAsync(User,true, cancellationToken).ContinueWith(r => {
            if(cancellationToken.IsCancellationRequested || r.IsCanceled)
                return IdentityResult.Failed(new IdentityError { Description = "Update operation was cancelled." });
            else if(r.IsFaulted)
                return IdentityResult.Failed(new  IdentityError { Description = "Update operation failed.\n" + r.Exception.InnerException?.Message });
            else return IdentityResult.Success;
        }, cancellationToken);
    }

    public Task<IdentityResult> DeleteAsync(User User, CancellationToken cancellationToken) {
        return _userRepository.DeleteAsync(User, true,cancellationToken).IsCompleted?
            Task.FromResult(IdentityResult.Success) :
            Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Failed to delete user." }));;
    }

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) {
        return Task.Run(() => {
            var id = uint.Parse(userId);
            return _userRepository.First(u => u.Id == id);
        }, cancellationToken);
    }

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) {
        return Task.Run(() => {
            return _userRepository.First(u => u.NormalizedEmail.ToUpper() == normalizedUserName);
        });
    }

    public Task SetPasswordHashAsync(User User, string? passwordHash, CancellationToken cancellationToken) {
        return passwordHash!=null? Task.Run(() => {
            User.PasswordHash = passwordHash;
            return _userRepository.UpdateExpr([
            (u =>u.PasswordHash ,passwordHash!)
                ]
                , u => u.Id == User.Id);
        }, cancellationToken):throw new ArgumentNullException(nameof(passwordHash));
    }

    public Task<string> GetPasswordHashAsync(User User, CancellationToken cancellationToken) {
        return Task.FromResult(User.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User User, CancellationToken cancellationToken) {
        return Task.FromResult(User.PasswordHash != null);
    }
    public void Dispose() {
    }
}