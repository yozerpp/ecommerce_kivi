using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;

public class JwtManager : IJwtManager
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _credentials ;
    private readonly IRepository<Session> _sessionRepository;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IRepository<User> _userRepository;
    public JwtManager(TokenValidationParameters parameters, SigningCredentials signingCredentials, IRepository<User> userRepository, IRepository<Session> sessionRepository) {
        _tokenValidationParameters = parameters;
        _userRepository = userRepository;
        this._sessionRepository = sessionRepository;
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
    public string CreateAuthToken(string key, TimeSpan lifetime) {
        var claims = new[]{ new Claim(ClaimTypes.Authentication, key) };
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
        var claims = _tokenHandler.ValidateToken(tokenValue, tokenValidationParameters, out _);
        return claims.FindFirstValue(ClaimTypes.Authentication);
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
    public SecurityToken CreateToken(Session session, bool rememberMe = false) {
        var claims = GetClaims(session);
        if(rememberMe)
            claims.Add(new Claim(ClaimTypes.IsPersistent, true.ToString()));
        return _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims,JwtBearerDefaults.AuthenticationScheme),
            Expires = rememberMe?DateTime.UtcNow.AddDays(3):DateTime.UtcNow.AddHours(3),
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
            session = _sessionRepository.First(s => s.Id == id);
            user = null;
        }
        else{
            user = _userRepository.First(u => u.Id == id, includes:[[nameof(Staff.Session)]]);
            session = null;
        }
    }
}