using System.Collections;
using System.Reflection;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ecommerce.Dao.Default.Initializer;

public static class BulkExtensions
{
    public static void BulkInsertDynamic(this DbContext dbContext, IEntityType type, IEnumerable batch, BulkConfig? bulkConfig = null) {
        if (type.BaseType != null){
            BulkInsertDynamic(dbContext, type.BaseType, batch, bulkConfig);
        }
        var method = typeof(DbContextBulkExtensions).GetMethods()
            .First(m=>m.Name.Equals(nameof(DbContextBulkExtensions.BulkInsert))&&
                      m.GetParameters()[2].ParameterType == typeof(BulkConfig))
            .MakeGenericMethod(type.ClrType);
        var args = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.ClrType));
        foreach (var o in batch){
            args.GetType().GetMethod("Add", [type.ClrType]).Invoke(args, [o]);
        }
        try{
            method.Invoke(null, [dbContext,args,  bulkConfig ,null,null]);
        }
        catch (TargetInvocationException e){
            throw e.InnerException;
        }
    }
}