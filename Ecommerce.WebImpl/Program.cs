using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using Ecommerce.Bl.Concrete;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Default;
using Ecommerce.Dao.Default.Validation;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Ecommerce.Notifications;
using Ecommerce.Shipping;
using Ecommerce.Shipping.Dummy;
using Microsoft.EntityFrameworkCore;
using Ecommerce.WebImpl.Data;
using Ecommerce.WebImpl.Data.Identity;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Customer = Ecommerce.Entity.Customer;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DefaultDbContext.DefaultConnectionString;
builder.Services.AddDbContext<DefaultDbContext>(options =>
    options.UseSqlServer(connectionString,
            c=> {
                c.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                c.MigrationsAssembly(typeof(DefaultDbContext).Assembly.FullName);
            })
        .EnableServiceProviderCaching(),ServiceLifetime.Scoped,ServiceLifetime.Singleton);
builder.Services.AddDbContext<ShippingContext>(options =>
        options.UseSqlServer(ShippingContext.DefaultConntectionString).EnableServiceProviderCaching(),
    ServiceLifetime.Scoped, ServiceLifetime.Singleton);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var razorPageOptions = builder.Services.AddRazorPages();
razorPageOptions.Services.AddScoped<AuthorizationFilter>(sp =>
    new AuthorizationFilter(sp.GetRequiredKeyedService<ICartManager>(nameof(AuthorizationFilter)),
        sp.GetRequiredKeyedService<IJwtManager>(nameof(AuthorizationFilter))));
razorPageOptions.AddMvcOptions(o=>o.Filters.AddService<AuthorizationFilter>());
builder.Services.AddScoped<DbContext, DefaultDbContext>();
builder.Services.AddKeyedSingleton<DbContext, DefaultDbContext>(nameof(DefaultDbContext));
builder.Services.AddKeyedSingleton<DbContext, ShippingContext>(nameof(ShippingContext));
builder.Services.AddSingleton<UserManager.HashFunction>(s => s);

builder.Services.AddSingleton(BuildLocalizer);
builder.Services.AddSingleton<EntityMapper.Factory>(sp=>new EntityMapper.Factory(sp.GetKeyedService<IModel>(nameof(DefaultDbContext)), sp.GetRequiredService<Localizer>()));
builder.Services.AddSingleton<EntityMapper>(sp => sp.GetRequiredService<EntityMapper.Factory>().Create());
var blEntities = GetEntityTypes(Assembly.GetAssembly(typeof(Cart)), "Ecommerce.Entity").ToList();
var shippingEntities = GetEntityTypes(Assembly.GetAssembly(typeof(IShippingService)), "Ecommerce.Shipping.Entity").ToList();
var blValidators = Assembly.GetAssembly(typeof(CartItemValidator)).ExportedTypes
    .Where(t => t.Namespace.Split('.').Last().Equals("Validation")).ToList();
var blImpls = GetServices(true,Assembly.GetAssembly(typeof(ICartManager)), "Ecommerce.Bl.Concrete").ToDictionary(t=>t.Name, t=>t);
var shippingImpls = GetServices(true,Assembly.GetAssembly(typeof(IShippingService)), "Ecommerce.Shipping.Dummy").ToDictionary(t=>t.Name, t=>t);
var blInterfaces = GetServices(false, Assembly.GetAssembly(typeof(ICartManager)), "Ecommerce.Bl.Interface")
    .Order(Comparer<Type>.Create(CreateComparerLambda(typeof(IJwtManager), typeof(ICartManager)).Compile())).ToList();
var shippingInterfaces = GetServices(false, Assembly.GetAssembly(typeof(IShippingService)), "Ecommerce.Shipping").ToList();
new DependencyRegisterer(builder, typeof(DefaultDbContext), blValidators, blEntities, blImpls, blInterfaces,
    nameof(AuthorizationFilter)).Register();
new DependencyRegisterer(builder,typeof(ShippingContext) ,[], shippingEntities, shippingImpls, shippingInterfaces).Register();
builder.Services.AddKeyedScoped<DbContext, DefaultDbContext>(nameof(NotificationService));
builder.Services.AddScoped<IRepository<Notification>>(sp => RepositoryFactory.Create<Notification>(
    sp.GetRequiredKeyedService<DbContext>(nameof(NotificationService))));
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserManager>(sp => sp.GetRequiredKeyedService<IUserManager>(nameof(IUserManager)));
builder.Services.AddScoped<IRepository<User>>(sp =>
    sp.GetRequiredKeyedService<IRepository<User>>(nameof(IUserManager)));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddIdentityCore<User>(options => {
        options.SignIn.RequireConfirmedAccount = false;
    }).AddUserStore<PasswordStore>()
    .AddUserManager<IdentityUserManagerAdapter>();
var signingAlgorithm = SecurityAlgorithms.HmacSha256;
var key = new SymmetricSecurityKey(SHA256.HashData("uDF$Gldpgl3*-4-ags"u8.ToArray()));
var tokenValidationParameters = new TokenValidationParameters()
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = "App",
    ValidAudience = "App",
    IssuerSigningKey = key,
    ValidAlgorithms = [signingAlgorithm],
    NameClaimType = ClaimTypes.NameIdentifier,
    RoleClaimType = ClaimTypes.Role,
};
var creds = new SigningCredentials(key, signingAlgorithm);
builder.Services.AddSingleton(creds);
builder.Services.AddSignalR();
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddKeyedSingleton( nameof(IJwtManager), tokenValidationParameters);
builder.Services.AddKeyedSingleton(nameof(IJwtManager), creds);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.Events = new JwtBearerEvents(){
            OnMessageReceived = context => {
                if (!context.Request.Cookies.TryGetValue(JwtBearerDefaults.AuthenticationScheme, out var cookie))
                    return Task.CompletedTask;
                context.Token = cookie;
                return Task.CompletedTask;
            },
        };
        options.TokenValidationParameters = tokenValidationParameters;
    });
builder.Services.AddSingleton<Dictionary<uint,Category>>(sp =>
    sp.GetKeyedService<DbContext>(nameof(DefaultDbContext)).Set<Category>().AsNoTracking().Include(c=>c.Parent).Include(c=>c.Children)
        .Select(c=>new Category{
        Id = c.Id,
        ParentId = c.ParentId,
        Parent = c.Parent,
        Name = c.Name,
        Description = c.Description,
        Children = c.Children,
        CategoryProperties = null!,
    }).Where(_=> true).ToDictionary(c=>c.Id,c=>c));
builder.Services.AddAuthorization(options => {
    options.AddPolicy(nameof(Seller), policy => policy.RequireRole(nameof(Seller), nameof(Staff)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.AddPolicy(nameof(Customer), policy => policy.RequireRole(nameof(Customer),nameof(Seller), nameof(Staff)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.AddPolicy(nameof(Staff), policy=> policy.RequireRole(nameof(Staff)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.DefaultPolicy = //anonymous
        new AuthorizationPolicyBuilder().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser().Build();
});
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"] ?? throw new KeyNotFoundException("Stripe Secret Key not found in configuration.");
// builder.Services.AddScoped<SessionMiddleware>();
Environment.SetEnvironmentVariable("STRIPE_PK",builder.Configuration.GetSection("Stripe")["PublishableKey"]?? throw new KeyNotFoundException("Stripe Publishable Key not found in configuration."));
var app = builder.Build();
// Configure the HTTP request pipeline.
app.MapHub<NotificationHub>("/notifications", options => {
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling ;
});
if (app.Environment.IsDevelopment()){
    app.UseMigrationsEndPoint();
}
else{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts();
}
Expression<Comparison<T>> CreateComparerLambda<T>(params IEnumerable<T> order) {
    var param1 = Expression.Parameter(typeof(T), "p1");
    var param2 = Expression.Parameter(typeof(T), "p2");
    return Expression.Lambda<Comparison<T>>(CreateComparerLambdaRecursive(param1, param2, order), param1, param2);
}
Expression CreateComparerLambdaRecursive<T>(ParameterExpression param1, Expression param2,params IEnumerable<T> order) {
    if(!order.Any()) return Expression.Constant(0);
    var value = order.First();
    return Expression.Condition(Expression.Equal(param1, Expression.Constant(value)), Expression.Constant(-1),
        Expression.Condition(Expression.Equal(param2, Expression.Constant(value)), Expression.Constant(1),CreateComparerLambdaRecursive<T>(param1, param2, order.Skip(1))));
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();
app.UseMiddleware<GlobalExceptionHandler>();
// app.UseMiddleware<SessionMiddleware>();
app.Run();

Localizer BuildLocalizer(IServiceProvider sp) {
    return new Localizer.Builder()
        .Add<ProductStats>(
            (p=>p.MaxPrice, "Maksimum Fiyat"),
            (p=>p.MinPrice, "Minimum Fiyat"),
            (p=>p.SaleCount, "Satış Sayısı"),
            (p=>p.ReviewCount, "Yorum Sayısı"),
            (p=>p.RatingAverage, "Yorum Ortalaması"))
        .Build();
}
IEnumerable<Type> GetServices(bool implementations,Assembly assembly, params string[] ns) {
    return assembly.GetTypes().Where(t =>
        (implementations &&!t.IsAbstract  && !t.IsInterface &&t.IsClass|| !implementations && t.IsInterface)&& !t.IsNested && t.IsPublic && ns.Contains(t.Namespace)) ;
}

IEnumerable<Type> GetEntityTypes(Assembly assembly, params string[] ns) {
    return assembly.GetTypes().Where(t=>!t.IsNested && !t.IsEnum&& ns.Contains(t.Namespace));
}
public class DependencyRegisterer(
    WebApplicationBuilder builder,
    Type contextType,
    List<Type> validators,
    List<Type> entities,
    Dictionary<string, Type> ımplementations,
    List<Type> ınterfaces,
    params List<string> extraKeys)
{
    public void Register() {
        foreach (var serviceType in ınterfaces){
            foreach (var extraKey in extraKeys){
                Register(serviceType, extraKey);
            }
            Register(serviceType);
        }
    }

    private void Register(Type serviceType, string? key = null) {
        var key1 = key?? serviceType.Name;
        var implementationType = ımplementations[serviceType.Name.Remove(0,1)]; //TODO:fragile
        CreateManagerDeps(key1);
        builder.Services.AddScoped(serviceType,
            provider => CreateManager(provider, key1, implementationType));
        builder.Services.AddKeyedScoped(serviceType, key1, (p, k) => CreateManager(p, k.ToString(), implementationType));
    }
    private object CreateManager(IServiceProvider provider, string serviceKey, Type implementationType) {
        var c =implementationType.GetConstructors().First();
        var deps = c.GetParameters().Select(p => p.ParameterType).Select(t=> {
            try{
                return provider.GetRequiredKeyedService(t, serviceKey);
            }
            catch (Exception e){
                Console.WriteLine("Used non-keyed service " + t.Name + " for " + implementationType.Name);
                return provider.GetRequiredService(t);
            }
        }).ToArray();
        return c.Invoke(deps);
    }

    private void CreateManagerDeps(string serviceKey) {
        builder.Services.AddKeyedScoped(typeof(DbContext),serviceKey, contextType);
        builder.Services.AddScoped(typeof(DbContext), contextType);
        builder.Services.AddKeyedSingleton<IModel>(contextType.Name, (sp, k) =>
            sp.GetRequiredKeyedService<DbContext>(k).Model);
        builder.Services.AddSingleton<IModel>(sp => ((DbContext)sp.GetRequiredService(contextType)).Model);
        //Validators
        foreach (var validatorType in validators){
            builder.Services.AddScoped(validatorType, (sp) => {
                var c = validatorType.GetConstructors().First();
                return c.Invoke(c.GetParameters().Select(p => sp.GetRequiredService(p.ParameterType))
                    .ToArray());
            });
            builder.Services.AddKeyedScoped(validatorType, serviceKey, (sp, k) => {
                var c = validatorType.GetConstructors().First();
                return c.Invoke(c.GetParameters().Select(p => sp.GetRequiredKeyedService(p.ParameterType, k)).ToArray());
            });
        }
        foreach (var entityType in entities){
            builder.Services.AddScoped(typeof(IValidator<>).MakeGenericType(entityType),(sp) => {
                var m = sp.GetRequiredKeyedService<IModel>(contextType.Name.Split('.').Last());
                return typeof(GenericValidator<>).MakeGenericType(entityType).GetConstructor([typeof(IModel)])!.Invoke([m]);
            });
            builder.Services.AddKeyedScoped(typeof(IValidator<>).MakeGenericType(entityType),serviceKey, (sp,_) => {
                var m = sp.GetKeyedService<IModel>(contextType.Name);
                return typeof(GenericValidator<>).MakeGenericType(entityType).GetConstructor([typeof(IModel)])!.Invoke([m]);
            });
            builder.Services.AddScoped(typeof(IRepository<>).MakeGenericType(entityType),(sp) => {
                var ctx = sp.GetRequiredKeyedService<DbContext>(contextType.Name.Split('.').Last());
                var args = typeof(List<>).MakeGenericType(typeof(IValidator<>).MakeGenericType(entityType)).GetConstructor([]).Invoke([]);
                foreach (var validators in sp.GetServices(typeof(IValidator<>).MakeGenericType(entityType))){
                    args.GetType().GetMethod("Add", [typeof(IValidator<>).MakeGenericType(entityType)])!
                        .Invoke(args, [validators]);
                }
                return typeof(RepositoryFactory).GetMethod(nameof(RepositoryFactory.Create))!
                    .MakeGenericMethod(entityType).Invoke(null,[ctx,  args.GetType().GetMethod(nameof(List<object>.ToArray)).Invoke(args, [])])!;
            });
            builder.Services.AddKeyedScoped(typeof(IRepository<>).MakeGenericType(entityType),serviceKey, (sp, k) => {
                var ctx = sp.GetRequiredKeyedService<DbContext>(k);
                var args = typeof(List<>).MakeGenericType(typeof(IValidator<>).MakeGenericType(entityType)).GetConstructor([]).Invoke([]);
                foreach (var validators in sp.GetKeyedServices(typeof(IValidator<>).MakeGenericType(entityType), serviceKey)){
                    args.GetType().GetMethod("Add", [typeof(IValidator<>).MakeGenericType(entityType)])!
                        .Invoke(args, [validators]);
                }
                return typeof(RepositoryFactory).GetMethod(nameof(RepositoryFactory.Create))!
                    .MakeGenericMethod(entityType).Invoke(null,[ctx,  args.GetType().GetMethod(nameof(List<object>.ToArray)).Invoke(args, [])])!;
            });
        }
    }
}



