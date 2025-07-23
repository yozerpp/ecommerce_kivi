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
    private readonly IRepository<Customer> _userRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<Seller> _sellerRepository;
    private readonly TokenValidationParameters _tokenValidationParameters;
    public JwtManager(TokenValidationParameters parameters, SigningCredentials signingCredentials, IRepository<Customer> userRepository, IRepository<Seller> sellerRepository, IRepository<Session> sessionRepository) {
        this._sellerRepository = sellerRepository;
        this._userRepository = userRepository;
        _tokenValidationParameters = parameters;
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
            case null:
                claims.Add(new Claim(ClaimTypes.NameIdentifier, session.Id.ToString()));
                break;
        }
        return claims;
    }
    public SecurityToken CreateToken(Session session) {
        var claims = GetClaims(session);
        return _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims,JwtBearerDefaults.AuthenticationScheme),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = _credentials
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
        ulong sessionId;
        if (id == 0){
            session = null;
            user = null;
            return;
        }
        if (role.Value.Equals(nameof(Seller))){
            user = _sellerRepository.First(s => s.Id == id, includes:[[nameof(Customer.Session)]]);
            session = null;
        }
        else if (role.Value.Equals(nameof(Customer))){
            user = _userRepository.First(u => u.Id == id, includes:[[nameof(Customer.Session)]]);
            session = null;
        } else{
            session = _sessionRepository.First(s => s.Id == id);
        }
    }
}