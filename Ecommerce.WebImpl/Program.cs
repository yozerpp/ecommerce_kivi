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
using Ecommerce.Entity.Projections;
using Microsoft.EntityFrameworkCore;
using Ecommerce.WebImpl.Data;
using Ecommerce.WebImpl.Data.Identity;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DefaultDbContext.DefaultConnectionString;
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
        .EnableSensitiveDataLogging(),ServiceLifetime.Scoped,ServiceLifetime.Singleton);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRazorPages();
builder.Services.AddScoped<DbContext, ApplicationDbContext>();
builder.Services.AddKeyedSingleton<DbContext, ApplicationDbContext>(nameof(IModel));
builder.Services.AddSingleton<UserManager.HashFunction>(s => s);
builder.Services.AddSingleton<IModel>(sp => {
    var ctX = sp.GetRequiredKeyedService<DbContext>(nameof(IModel));
    return ctX.Model;
});
builder.Services.AddSingleton<Localizer>(BuildLocalizer);
builder.Services.AddSingleton<EntityMapper.Factory>();
builder.Services.AddSingleton<EntityMapper>(sp => sp.GetRequiredService<EntityMapper.Factory>().Create());
var implementations = Assembly.GetAssembly(typeof(CartManager)).GetTypes().Where(t =>
        !t.IsAbstract && !t.IsInterface&&t.IsClass&& !t.IsNested && t.IsPublic && t.Namespace.Equals("Ecommerce.Bl.Concrete"))
    .ToDictionary(t => t.Name, t => t);
foreach (var serviceType in Assembly.GetAssembly(typeof(ICartManager)).GetTypes()
             .Where(t=>!t.IsNested&& t.IsInterface &&t.Namespace.Equals("Ecommerce.Bl.Interface"))
             .Order(Comparer<Type>.Create(CreateComparerLambda<Type>(typeof(IJwtManager), typeof(ICartManager)).Compile()))){
    CreateManagerDeps(builder, serviceType.Name);
    var implementationType = implementations[serviceType.Name.Remove(0,1)];
    builder.Services.AddScoped(serviceType,
        provider => CreateManager(provider, serviceType, implementationType));
}
builder.Services.AddScoped<IRepository<Customer>>(sp =>
    sp.GetRequiredKeyedService<IRepository<Customer>>(nameof(IUserManager)));
builder.Services.AddIdentityCore<Customer>(options => {
        options.SignIn.RequireConfirmedAccount = false;
    }).AddUserStore<PasswordStore>()
    .AddUserManager<IdentityUserManagerAdapter>()
    .AddSignInManager();
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
builder.Services.AddAuthorization(options => {
    options.AddPolicy(nameof(Seller), policy => policy.RequireRole(nameof(Seller)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.AddPolicy(nameof(Customer), policy => policy.RequireRole(nameof(Customer),nameof(Seller)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.DefaultPolicy = //anonymous
        new AuthorizationPolicyBuilder().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).AddRequirements( new AssertionRequirement(context => 
        context.User.Identity?.IsAuthenticated != true || !context.User.IsInRole(nameof(Customer)) && !context.User.IsInRole(nameof(Seller)))).Build();
});
// builder.Services.AddScoped<SessionMiddleware>();
var app = builder.Build();
// Configure the HTTP request pipeline.
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

// app.UseMiddleware<SessionMiddleware>();
app.Run();

Localizer BuildLocalizer(IServiceProvider sp) {
    return new Localizer.Builder()
        .Add<ProductWithAggregates>(
            (p=>p.MaxPrice, "Maksimum Fiyat"),
            (p=>p.MinPrice, "Minimum Fiyat"),
            (p=>p.SaleCount, "Satış Sayısı"),
            (p=>p.ReviewCount, "Yorum Sayısı"),
            (p=>p.ReviewAverage, "Yorum Ortalaması"))
        .Build();
}

object CreateManager(IServiceProvider provider, Type ifaceType, Type implementationType) {
    var c =implementationType.GetConstructors().First();
    var deps = c.GetParameters().Select(p => p.ParameterType).Select(t=> {
        try{
            return provider.GetRequiredKeyedService(t, ifaceType.Name);
        }
        catch (Exception){
            return provider.GetRequiredService(t);
        }
    }).ToArray();
    return c.Invoke(deps);
}
void CreateManagerDeps(WebApplicationBuilder webApplicationBuilder, string managerName) {
    webApplicationBuilder.Services.AddKeyedScoped<DbContext, ApplicationDbContext>(managerName);
    webApplicationBuilder.Services.AddKeyedScoped<IValidator<CartItem>,CartItemValidator>(managerName, (sp, k) => {
        var rep = sp.GetRequiredKeyedService<IRepository<Coupon>>(k);
        return new CartItemValidator(rep);
    });
    webApplicationBuilder.Services.AddKeyedScoped<IValidator<Coupon>,CouponValidator>(managerName);
    webApplicationBuilder.Services.AddKeyedScoped<IValidator<OrderItem>,OrderItemValidator>(managerName, (sp, k) => {
        var rep = sp.GetRequiredKeyedService<IRepository<Coupon>>(k);
        return new OrderItemValidator(rep);
    });
    foreach (var entityType in Assembly.GetAssembly(typeof(Cart))!.GetTypes().Where(t=>t.Namespace?.Equals("Ecommerce.Entity") ?? false)){
        webApplicationBuilder.Services.AddKeyedScoped(typeof(IValidator<>).MakeGenericType(entityType),managerName, (sp,_) => {
            var m = sp.GetService<IModel>();
            return typeof(GenericValidator<>).MakeGenericType(entityType).GetConstructor([typeof(IModel)])!.Invoke([m]);
        });
        webApplicationBuilder.Services.AddKeyedScoped(typeof(IRepository<>).MakeGenericType(entityType),managerName, (sp, k) => {
            var ctx = sp.GetRequiredKeyedService<DbContext>(k);
            var args = typeof(List<>).MakeGenericType(typeof(IValidator<>).MakeGenericType(entityType)).GetConstructor([]).Invoke([]);
            foreach (var validators in sp.GetKeyedServices(typeof(IValidator<>).MakeGenericType(entityType), managerName)){
                args.GetType().GetMethod("Add", [typeof(IValidator<>).MakeGenericType(entityType)])!
                    .Invoke(args, [validators]);
            }
            return typeof(RepositoryFactory).GetMethod(nameof(RepositoryFactory.Create))!
                .MakeGenericMethod(entityType).Invoke(null,[ctx,  args.GetType().GetMethod(nameof(List<object>.ToArray)).Invoke(args, [])])!;
        });
    }

}