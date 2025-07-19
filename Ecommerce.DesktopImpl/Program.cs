using Ecommerce.Bl.Concrete;
using Ecommerce.Dao;
using Ecommerce.Dao.Default;
using Ecommerce.Dao.Default.Validation;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Ninject;
using Ninject.Extensions.Conventions;

namespace Ecommerce.DesktopImpl;

static class Program
{
    public static readonly IKernel Kernel = new StandardKernel();
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        initDeps();
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, args) => {
            Utils.Error(args.Exception.Message + (args.Exception.InnerException != null ?args.Exception.InnerException.Message:""));
        };
        Application.Run(Kernel.Get<Form1>());
    }

    static void initDeps() {
        var context = new DefaultDbContext(new DbContextOptionsBuilder<DefaultDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=Ecommerce;User Id=sa;Password=12345;Trust Server Certificate=True;Encrypt=True;")
            .EnableSensitiveDataLogging().Options);
        Kernel.Bind<DefaultDbContext>().ToConstant(context).InSingletonScope();
        Kernel.Bind<DbContext>().ToConstant(context).InSingletonScope();
        var model = context.Model;
        Kernel.Bind<Navigation>().To<Navigation>().InSingletonScope();
        Kernel.Bind<ProductPage>().To<ProductPage>().InSingletonScope();
        Kernel.Bind<SellerPage>().To<SellerPage>().InSingletonScope();
        Kernel.Bind<RegisteryPage>().To<RegisteryPage>().InSingletonScope();
        Kernel.Bind<CartPage>().To<CartPage>().InSingletonScope();
        Kernel.Bind<ReviewPage>().To<ReviewPage>().InSingletonScope();
        Kernel.Bind<LoginPage>().To<LoginPage>().InSingletonScope();
        Kernel.Bind<UserPage>().To<UserPage>().InSingletonScope();
        Kernel.Bind<IRepository<Cart>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Cart>(model)));
        Kernel.Bind<IRepository<CartItem>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<CartItem>(model)));
        Kernel.Bind<IRepository<Category>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Category>(model)));
        Kernel.Bind<IRepository<Coupon>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Coupon>(model)));
        Kernel.Bind<IRepository<Order>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Order>(model)));
        Kernel.Bind<IRepository<OrderItem>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<OrderItem>(model)));
        Kernel.Bind<IRepository<Payment>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Payment>(model)));
        Kernel.Bind<IRepository<Product>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Product>(model)));
        Kernel.Bind<IRepository<ProductOffer>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<ProductOffer>(model)));
        Kernel.Bind<IRepository<ProductReview>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<ProductReview>(model)));
        Kernel.Bind<IRepository<ReviewComment>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<ReviewComment>(model)));
        Kernel.Bind<IRepository<ReviewVote>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<ReviewVote>(model)));
        Kernel.Bind<IRepository<Seller>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Seller>(model)));
        Kernel.Bind<IRepository<Session>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<Session>(model)));
        Kernel.Bind<IRepository<User>>().ToConstant(RepositoryFactory.Create(context, new GenericValidator<User>(model)));
        Kernel.Bind<UserManager.HashFunction>().ToConstant(new UserManager.HashFunction(s => s));
        Kernel.Bind(x => {
            x.FromAssembliesInPath(AppDomain.CurrentDomain.BaseDirectory).SelectAllTypes().BindAllInterfaces().Configure(
                c=>c.InSingletonScope());
        });


    }
}