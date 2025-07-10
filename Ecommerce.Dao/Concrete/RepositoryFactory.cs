using System.Reflection;
using System.Runtime.CompilerServices;
using Ecommerce.Dao.Iface;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Ecommerce.Dao.Concrete;

public class RepositoryFactory
{
    public static IRepository<TE> CreateEf<TE,TC>(TC context) where TE: class, new() where TC: DbContext
    {
        var repo =  new EfRepository<TE>(context);
        return RepositoryProxy<TE>.Create(repo);
    }
    //create EfRepository
    public class RepositoryProxy<TE> : DispatchProxy where TE : class, new()
    {
        //This assumes each repository that use the same type of context actually use the same context.
        // private static Dictionary<Type, Lock?> TransactionLocks = new Dictionary<Type, Lock>();
        private static Lock TransactionLock = new Lock();
        private AbstractValidator<TE>? Validator { get; set; }
        private IRepository<TE>? _repository;
        public static IRepository<TE> Create(IRepository<TE> repository, AbstractValidator<TE>? validator = null)
        {
            object proxy = DispatchProxy.Create<IRepository<TE>, RepositoryProxy<TE>>();
            ((RepositoryProxy<TE>)proxy).Repository = repository;
            Lock lck;
            // if ((lck = TransactionLocks.GetValueOrDefault(typeof(TC)))==null){
            //     lck = new Lock();
            //     TransactionLocks.Add(typeof(TC), lck);
            // }
            ((RepositoryProxy<TE>)proxy).Lock = TransactionLock;
            ((RepositoryProxy<TE>)proxy).Validator = validator;
            return (IRepository<TE>)proxy;
        }
        public IRepository<TE> Repository { 
            set => _repository = value;
            private get => _repository;
        }
        public Lock Lock { get; set; }
        public RepositoryProxy() {}
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod.Name.Equals("Add") || targetMethod.Name.Equals("Update"))
            {
                if (Validator!=null){
                    var res= Validator.Validate((TE)args[0]);
                    if (res.Errors.Any()){
                        throw new ValidationException(String.Join(',',res.Errors.Select(e => e.ErrorMessage).ToList()));
                    }
                }
            }
            lock (Lock)
            {
                return targetMethod.Invoke(Repository, args);
            }
        }
    }
}