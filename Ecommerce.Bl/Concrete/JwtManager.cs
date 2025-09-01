using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Ecommerce.Entity.Views;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
namespace Ecommerce.Bl.Concrete;

public class JwtManager : IJwtManager
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _credentials ;
    private readonly IRepository<Session> _sessionRepository;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IRepository<User> _userRepository;
    private readonly DbContext _dbContext;
    public JwtManager(TokenValidationParameters parameters, SigningCredentials signingCredentials, IRepository<User> userRepository, IRepository<Session> sessionRepository,[FromKeyedServices("DefaultDbContext")] DbContext dbContext) {
        _tokenValidationParameters = parameters;
        _userRepository = userRepository;
        this._sessionRepository = sessionRepository;
        _dbContext = dbContext;
        _credentials = signingCredentials;
    }
    public string Serialize(SecurityToken securityToken) {
        return _tokenHandler.WriteToken(securityToken);
    }
    public void Deserialize(string token, out User? user, out Session? session) {
        try{
            var p = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            ParsePrincipal(out user, out session, p);
        }
        catch (SecurityTokenValidationException e){
            user = null;
            session = null;
            return;
        }
    }
    private const string AuthTokenKey = "^+?'QaoAaHAS2*23qwe";
    public string CreateAuthToken(string value, TimeSpan lifetime) {
        var claims = new[]{ new Claim(ClaimTypes.Authentication, value) };
        var salt = DateTime.UtcNow + TimeSpan.FromHours(3);
        var k = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(AuthTokenKey+ "!"+salt.ToBinary())));
        var t = _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme),
            Expires = DateTime.UtcNow + lifetime,
            SigningCredentials = new SigningCredentials(k, SecurityAlgorithms.HmacSha256),
            Audience = _tokenValidationParameters.ValidAudience,
            Issuer = _tokenValidationParameters.ValidIssuer,
        });
        return salt.ToBinary() + "|" +  _tokenHandler.WriteToken(t);
    }
    public string? ReadAuthToken(string token) {
        var (salt, tokenValue) = ParseAuthToken(token);
        var tokenValidationParameters = _tokenValidationParameters.Clone();
        tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
            SHA256.HashData(Encoding.UTF8.GetBytes(AuthTokenKey + "!" + salt.ToBinary())));
        try{
            var claims = _tokenHandler.ValidateToken(tokenValue, tokenValidationParameters, out _);
            return claims.FindFirstValue(ClaimTypes.Authentication);
        }
        catch (SecurityTokenExpiredException e){
            return null;
        }
        
    }
    public (DateTime, string) ParseAuthToken(string withSalt) {
        var s = withSalt.Split('|', 2, StringSplitOptions.RemoveEmptyEntries);
        return (DateTime.FromBinary(long.Parse(s[0])), s[1]);
    }
    public List<Claim> GetClaims(Session session) {
        var claims = new List<Claim>();
        switch (session.User){
            case Seller s:
                claims.Add(new Claim(ClaimTypes.Role,nameof(Seller)));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, s.Id.ToString()));
                break;
            case Customer u:
                claims.Add(new Claim(ClaimTypes.Role,nameof(Customer)));
                claims.Add(new Claim(ClaimTypes.NameIdentifier , u.Id.ToString()));
                break;
            case Staff s:
                claims.Add(new Claim(ClaimTypes.Role,nameof(Staff)));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, s.Id.ToString()));
                break;
            case null:
                claims.Add(new Claim(ClaimTypes.NameIdentifier, session.Id.ToString()));
                break;
        }
        return claims;
    }
    public SecurityToken CreateToken(Session session, TimeSpan lifetime = default) {
        if(lifetime==TimeSpan.Zero) lifetime = TimeSpan.FromHours(1);
        var claims = GetClaims(session);
        return _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims,JwtBearerDefaults.AuthenticationScheme),
            Expires = DateTime.Now + lifetime,
            SigningCredentials = _credentials,
            Audience = _tokenValidationParameters.ValidAudience,
            Issuer = _tokenValidationParameters.ValidIssuer,
        });
    }
    

    public void UnwrapToken(SecurityToken token, out User? user, out Session? session) {
        var principal = _tokenHandler.ValidateToken( _tokenHandler.WriteToken(token),_tokenValidationParameters, out var _);
        ParsePrincipal(out user, out session, principal);
    }

    public void ParsePrincipal(out User? user, out Session? session, ClaimsPrincipal principal) {
        if (principal == null){ 
            user=null;
            session = null;
            return;
        }
        user= null;
        uint id = Convert.ToUInt32(
            principal.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.NameIdentifier))?.Value);
        var role = principal.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Role));
        if (id == 0){
            session = null;
            user = null;
            return;
        }

        if (role == null){
            var s = _dbContext.Database.SqlQueryRaw<SessionSimple>(
                $"SELECT s.Id, s.{nameof(Session.CartId)}, s.{nameof(Session.UserId)}, c.{nameof(CartAggregates.ItemCount)} as {nameof(Session.ItemCount)} FROM [data].[{nameof(Session)}] s LEFT JOIN [data].[{nameof(CartAggregates)}] c ON c.{nameof(CartAggregates.CartId)} = s.{nameof(Session.CartId)} WHERE s.Id = {{0}}",
                id).FirstOrDefault();
            session = new Session(){
                Id = s.Id,
                CartId = s.CartId,
                ItemCount = s.ItemCount,
                UserId = s.UserId
            };
            _dbContext.Attach(session);
            user = null;
        }
        else{
            user = _userRepository.First(u => u.Id == id, includes:[[nameof(User.Session), nameof(Session.Cart)], [nameof(User.ProfilePicture)]], nonTracking:true);
            if (user is Customer){
                var uid = user.SessionId;
                user.Session.Cart.Aggregates = new CartAggregates(){
                    ItemCount = _sessionRepository.FirstP(s => s.Cart.Aggregates.ItemCount,
                        s => s.Id == uid, nonTracking: true)
                };    
            }
            session = null;
        }
    }

    private class SessionSimple
    {
        public ulong Id { get; set; }
        public uint CartId { get; set; }
        public uint? UserId { get; set; }
        public uint? ItemCount { get; set; }
    }
}