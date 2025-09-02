using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Ecommerce.Bl.Concrete;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Default;
using Ecommerce.Dao.Default.Validation;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Events;
using Ecommerce.Entity.Views;
using Ecommerce.Mail;
using Ecommerce.Notifications;
using Ecommerce.Shipping;
using Ecommerce.Shipping.Dummy;
using Ecommerce.Shipping.Geliver;
using Microsoft.EntityFrameworkCore;
using Ecommerce.WebImpl.Data;
using Ecommerce.WebImpl.Data.Identity;
using Ecommerce.WebImpl.Middleware;
using Ecommerce.WebImpl.Pages.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Customer = Ecommerce.Entity.Customer;
using ICompressionProvider = Microsoft.AspNetCore.ResponseCompression.ICompressionProvider;
using Product = Ecommerce.Entity.Product;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DefaultDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(nameof(DefaultDbContext)),
            c=> {
                c.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                c.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                c.MigrationsAssembly(typeof(DefaultDbContext).Assembly.GetName().Name);
            }).EnableDetailedErrors(builder.Environment.IsDevelopment())
        .EnableServiceProviderCaching(),ServiceLifetime.Scoped,ServiceLifetime.Singleton);
builder.Services.AddDbContext<ShippingContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString(nameof(ShippingContext)),
            c => {
                c.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                c.MigrationsAssembly(typeof(ShippingContext).Assembly.GetName().Name); 
            }).EnableServiceProviderCaching().EnableSensitiveDataLogging(builder.Environment.IsDevelopment()),
    ServiceLifetime.Scoped, ServiceLifetime.Singleton);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var razorPageOptions = builder.Services.AddRazorPages();
razorPageOptions.Services.AddScoped<AuthorizationFilter>(sp =>
    new AuthorizationFilter(sp.GetRequiredKeyedService<ICartManager>(nameof(AuthorizationFilter)),
        sp.GetRequiredKeyedService<IJwtManager>(nameof(AuthorizationFilter))));
razorPageOptions.AddMvcOptions(o=>o.Filters.AddService<AuthorizationFilter>());
builder.Services.AddScoped<DbContext, DefaultDbContext>();
builder.Services.AddKeyedScoped<DbContext, DefaultDbContext>(nameof(DefaultDbContext));
builder.Services.AddKeyedScoped<DbContext, ShippingContext>(nameof(ShippingContext));
builder.Services.AddKeyedScoped<IModel>(nameof(DefaultDbContext),
    (sp, k) => sp.GetRequiredKeyedService<DbContext>(k).Model);
builder.Services.AddKeyedScoped<IModel>(nameof(ShippingContext),
    (sp, k) => sp.GetRequiredKeyedService<DbContext>(k).Model);
if (builder.Environment.IsProduction()){
    if (Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE") != null){ //proxy
        builder.Services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders =
                ForwardedHeaders.All;
            // options.KnownProxies.Clear();
        });
    }
    else{
        builder.WebHost.ConfigureKestrel(k => {
            k.ListenAnyIP(443, options => {
                options.UseHttps(
                    builder.Configuration["Certificate:Path"] ?? throw new ArgumentNullException("Certificate:Path"),
                    builder.Configuration["Certificate:Password"]);
            });
            k.ListenAnyIP(80);
        });
    }
}

var blContext = new DefaultDbContext(new DbContextOptionsBuilder<DefaultDbContext>().UseSqlServer(builder.Configuration.GetConnectionString(nameof(DefaultDbContext)),
        c=> {
            c.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            c.MigrationsAssembly(typeof(DefaultDbContext).Assembly.GetName().Name);
        }).EnableDetailedErrors()
    .EnableServiceProviderCaching().Options);

builder.Services.AddSingleton<UserManager.HashFunction>(s => s);
builder.Services.AddSingleton(BuildLocalizer);

//register BL
var blEntities = GetEntityTypes(Assembly.GetAssembly(typeof(Cart)), "Ecommerce.Entity").ToList();
var blValidators = Assembly.GetAssembly(typeof(CartItemValidator)).GetTypes()
    .Where(t => t.Namespace?.Split('.').Last().Equals("Validation") ?? false).ToList();
var blImpls = GetServices(true,Assembly.GetAssembly(typeof(ICartManager)), "Ecommerce.Bl.Concrete").ToDictionary(t=>t.Name, t=>t);
var blInterfaces = GetServices(false, Assembly.GetAssembly(typeof(ICartManager)), "Ecommerce.Bl.Interface")
    .Order(Comparer<Type>.Create(CreateComparerLambda(typeof(IJwtManager), typeof(ICartManager)).Compile())).ToList();
new DependencyRegisterer(builder, typeof(DefaultDbContext), blValidators, blEntities, blImpls, blInterfaces,
    nameof(AuthorizationFilter)).Register();
//register Shipping
builder.Services.AddSingleton<GeliverClient>(sp =>
    new GeliverClient(builder.Configuration.GetSection("Shipping")["ApiKey"] ?? throw new ArgumentException("Missing shipping API key"))
);
builder.Services.AddScoped<JwtMiddleware>();
builder.Services.AddScoped<IShippingService, GeliverService>();
builder.Services.AddKeyedScoped<DbContext, DefaultDbContext>(nameof(NotificationService));
builder.Services.AddScoped<IRepository<Notification>>(sp => RepositoryFactory.Create<Notification>(
    sp.GetRequiredKeyedService<DbContext>(nameof(NotificationService))));
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserManager>(sp => sp.GetRequiredKeyedService<IUserManager>(nameof(IUserManager)));
builder.Services.AddScoped<IRepository<User>>(sp =>
    sp.GetRequiredKeyedService<IRepository<User>>(nameof(IUserManager)));
builder.Services.AddScoped<ISessionManager>(sp => sp.GetRequiredKeyedService<ISessionManager>(nameof(ISessionManager)));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddResponseCompression(o => {
    o.EnableForHttps = true;
    o.Providers.Add(new BrotliCompressionProvider(new BrotliCompressionProviderOptions(){
        Level = CompressionLevel.Fastest,
    }));
});
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
builder.Services.AddAuthentication(options => {
        options.DefaultScheme = "Cookie";
        options.DefaultChallengeScheme = "Cookie";
        options.DefaultSignInScheme = "Cookie";
        options.DefaultAuthenticateScheme ="Cookie";
    }).AddCookie("Cookie", options => {
        options.Cookie.SameSite = SameSiteMode.Lax; // Not Strict!
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // For dev
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Login?handler=logout";
        options.Events.OnRedirectToReturnUrl = f => {
            Console.WriteLine("Cookie redirects to return url: " + f.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnSigningIn = c => {
            Console.WriteLine("Signing in Cookie auth" + c.Principal?.Identity?.Name);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = f => {
            Debug.WriteLine("Cookie redirects to login: " + f.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,options => {
        options.EventsType = typeof(JwtMiddleware);
        options.ForwardSignIn = "Cookie";
        options.TokenValidationParameters = tokenValidationParameters;
    })
    .AddGoogle(nameof(Google), cb => {
        var googleConf = builder.Configuration.GetSection("Oauth").GetSection("Google");
        cb.ClientId = googleConf["ClientId"] ?? throw new ArgumentException("Missing client ID");
        cb.ClientSecret = googleConf["ClientSecret"] ?? throw new ArgumentException("Missing client secret");
        cb.CallbackPath = "/Account/Oauth/Google";
        cb.AccessType = "online";
        // cb.Events.OnRedirectToAuthorizationEndpoint = f => {
        //     Debug.WriteLine("Google redirects: " + f.RedirectUri);
        //     return Task.CompletedTask;
        // };
        cb.Events.OnCreatingTicket = f=> {
            Debug.WriteLine("Google ticket created: " + f.Identity?.Name);
            return Task.CompletedTask;
        };
        cb.AccessDeniedPath = "/Account/Unauthorized";
        cb.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
        cb.ClaimActions.MapJsonKey(ClaimTypes.Name, "name", "string" );
        cb.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", "string" );
        cb.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub", "string");
        cb.Scope.Add("openid");
        cb.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
        cb.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
        cb.SaveTokens = false;
    });
var sKey = builder.Configuration.GetSection("Oauth").GetSection("Google")["StateKey"] ?? throw new ArgumentException("Missing state encryption key");
builder.Services.AddKeyedSingleton<string>("GoogleStateKey", sKey);
object lockobj = new();
builder.Services.AddSingleton<StaffBag>(GetStaves());
builder.Services.AddSingleton<IDictionary<uint,Category>>( GetCategories());
var mailConfig = builder.Configuration.GetSection("Mail");
builder.Services.AddSingleton<IMailService, SMTPService>(_ => new SMTPService(mailConfig["Server"] ?? throw new ArgumentNullException(),
    int.Parse(mailConfig["Port"] ?? throw new ArgumentNullException()), mailConfig["Username"] ?? throw new ArgumentNullException(), mailConfig["Password"] ?? throw new ArgumentNullException()));
builder.Services.AddAuthorization(options => {
    options.AddPolicy(nameof(Seller), policy => policy.RequireRole(nameof(Seller), nameof(Staff)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.AddPolicy(nameof(Customer), policy => policy.RequireRole(nameof(Customer), nameof(Staff)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.AddPolicy(nameof(AnonymousCustomer), policy=>policy.RequireAssertion(c=>!c.User.HasClaim(c=>c.Type==ClaimTypes.Role) || c.User.HasClaim(ClaimTypes.Role, nameof(Customer))));
    options.AddPolicy(nameof(Staff), policy=> policy.RequireRole(nameof(Staff)).AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
    options.AddPolicy(nameof(User), p=>p.RequireAssertion(f=>f.User.HasClaim(c=>c.Type == ClaimTypes.Role)));
    // options.DefaultPolicy = //anonymous
    //     new AuthorizationPolicyBuilder().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    //         .RequireAuthenticatedUser().Build();
});
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"] ?? throw new KeyNotFoundException("Stripe Secret Key not found in configuration.");
// builder.Services.AddScoped<SessionMiddleware>();
Environment.SetEnvironmentVariable("STRIPE_PK",builder.Configuration.GetSection("Stripe")["PublishableKey"]?? throw new KeyNotFoundException("Stripe Publishable Key not found in configuration."));
blContext.Dispose();
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

string? pathBase;
if (app.Environment.IsProduction()){
    if (!string.IsNullOrEmpty(pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE"))){
        Console.WriteLine($"Pathbase: {pathBase}");
        app.UsePathBase(pathBase);
        app.UseForwardedHeaders();
        app.MapGet("/debug-forwarding", (HttpRequest request) => {
            var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var result = new{
                Message = "This is the state of the request INSIDE the ASP.NET Core app.",
                Scheme = request.Scheme,
                Host = request.Host.ToString(),
                PathBase = request.PathBase.ToString(),
                Path = request.Path.ToString(),
                FullUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}",
                Headers = headers
            };
            // Use System.Text.Json for clean, indented output
            return Results.Json(result, new JsonSerializerOptions{ WriteIndented = true });
        });
    }
    else{
        app.UseHttpsRedirection();
    }
}

app.UseResponseCompression();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();
app.UseMiddleware<GlobalExceptionHandler>();
// app.UseMiddleware<SessionMiddleware>();
app.Run();
return;

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

StaffBag GetStaves() {
    lock (lockobj){
        return new StaffBag(blContext.Set<Staff>().AsNoTracking()
            .Include(s => s.PermissionClaims).ToArray());
    }
}

IDictionary<uint, Category> GetCategories() {
    IDictionary<uint, Category> d;
    lock (lockobj){
        d = new ConcurrentDictionary<uint, Category>( blContext.Set<Category>().AsNoTracking()
            .Include(c=>c.CategoryProperties).ToDictionary(c => c.Id, c => c));
    }
    foreach (var category in d.Values){
        if (category.ParentId != null){
            var p = d[category.ParentId.Value];
            p.Children.Add(category);
            category.Parent = p;
        }
    }
    return d;
}

Localizer BuildLocalizer(IServiceProvider sp) {
    return new Localizer.Builder()
        .Add<ProductStats>(
            (p=>p.MaxPrice, "Maksimum Fiyat"),
            (p=>p.MinPrice, "Minimum Fiyat"),
            (p=>p.SaleCount, "Satış Sayısı"),
            (p=>p.ReviewCount, "Yorum Sayısı"),
            (p=>p.RatingAverage, "Yorum Ortalaması"),
            (p=>p.FavorCount , "Favori Sayısı"), 
            (p=>p.OrderCount, "Sipariş Sayısı"),
            (p=>p.RefundCount, "İade Sayısı"))
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



