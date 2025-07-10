using System.Reflection;

namespace Ecommerce.Bl;

public class ManagerEnhancer
{

    /**
     * TM should be the manager interface, not implementation. 
     */
    public static TM Enhance<TM>(TM manager) 
    {
        object proxy= DispatchProxy.Create<TM, ManagerProxy<TM>>();
        ((ManagerProxy<TM>)proxy)._manager= manager;
        return (TM) proxy ;
    }
    private class ManagerProxy<TM> : DispatchProxy
    {
        private static Dictionary<Type, Lock> TransactionLocks = new Dictionary<Type, Lock>(); 
        public TM _manager { get; set; }
        public ManagerProxy(){}
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var transactional =
                targetMethod.GetCustomAttributes(false).SingleOrDefault(a => a is Transactional) as Transactional;
            if (transactional != null)
            {
                foreach (var type in transactional.EntityType)
                {
                    TransactionLocks[type].Enter();
                }
            }
            var ret = targetMethod.Invoke(_manager, args);
            if (transactional!=null)
            {
                foreach (var type in transactional.EntityType)
                {
                    TransactionLocks[type].Exit();
                }
            }
            return ret;
        }
    }
    public class Transactional : Attribute
    {
        public Transactional(params ICollection<Type> entityType)
        {
            this.EntityType = entityType;
        }
        public ICollection<Type> EntityType { get; set; }
    }
}