using System.Reflection;
using FluentValidation;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Ecommerce.Dao.Spi;
public class RepositoryProxy<TE> : DispatchProxy where TE : class
{
    //This assumes each repository that use the same type of Entity actually use the same context.
    // private static Dictionary<Type, Lock?> TransactionLocks = new Dictionary<Type, Lock>();
    private static readonly ReaderWriterLockSlim TransactionLock = new();
    private IValidator<TE>[]? _validators;
    private IRepository<TE>? _repository;

    public static IRepository<TE> Create(IRepository<TE> repository, IValidator<TE>[]? validators = null) {
        object proxy = DispatchProxy.Create<IRepository<TE>, RepositoryProxy<TE>>();
        ((RepositoryProxy<TE>)proxy)._repository = repository;
        ((RepositoryProxy<TE>)proxy)._validators = validators;
        return (IRepository<TE>)proxy;
    }

    public RepositoryProxy() {
    }
    /// <summary>
    /// This serializes write and read operations on the table.
    /// I'm not sure if it is actually needed since Db does serialization in the background any way.
    /// </summary>
    /// <param name="targetMethod"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="ValidationException">If entity validation failed by _validator</exception>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
        if (!typeof(IRepository<TE>).IsAssignableFrom(targetMethod.DeclaringType)) 
            return targetMethod.Invoke(_repository,args);
        bool isAddOrUpdate = targetMethod.Name.Equals("Add") || targetMethod.Name.Equals("Update");
        if (isAddOrUpdate){
            if (_validators != null && args[0] is TE entity){
                var errors = _validators.Select(v => v.Validate(entity))
                    .Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();
                if (errors.Count > 0){
                    throw new ValidationException(String.Join(',', errors.Select(e => e.ErrorMessage).ToList()));
                }
            }
        }

        if (isAddOrUpdate = (isAddOrUpdate || targetMethod.Name.Equals("Delete"))){
            TransactionLock.EnterWriteLock();
        }
        else TransactionLock.EnterReadLock();
        try{
            return targetMethod.Invoke(_repository, args);
        }
        finally{
            if (isAddOrUpdate) TransactionLock.ExitWriteLock();
            else TransactionLock.ExitReadLock();
        }
    }
}
