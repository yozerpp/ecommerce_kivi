using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Dao;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;

public class JwtManager
{
    private readonly SecurityKey _secretKey;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _credentials ;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Session> _sessionRepository;
    private readonly IRepository<Seller> _sellerRepository;
    public JwtManager(IRepository<User> userRepository, IRepository<Seller> sellerRepository, IRepository<Session> sessionRepository) {
        const string secretKey = "uDF$Gldpgl3*-4-ags";
        this._sellerRepository = sellerRepository;
        this._userRepository = userRepository;
        this._sessionRepository = sessionRepository;
        _secretKey = new SymmetricSecurityKey(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(secretKey)));
        _credentials = new SigningCredentials(_secretKey, SecurityAlgorithms.HmacSha256);
    }

    public string Serialize(SecurityToken securityToken) {
        return _tokenHandler.WriteToken(securityToken);
    }

    public void Deserialize(string token, out User? user, out Session? session) {
        try{
            var p = _tokenHandler.ValidateToken(token, GetTokenValidationParameters(), out _);
            ParsePrincipal(out user, out session, p);
        }
        catch (SecurityTokenValidationException e){
            user = null;
            session = null;
            return;
        }
    }
    public SecurityToken CreateToken(Session session) {
        var claims = new List<Claim>();
        switch (session.User){
            case Seller s:
                claims.Add(new Claim("sellerId",s.Id.ToString()));
                break;
            case User u:
                claims.Add(new Claim("userId",u.Id.ToString()));
                break;
            case null:
                claims.Add(new Claim("sessionId", session.Id.ToString()));
                break;
        }
        return _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = _credentials
        });
    }

    private TokenValidationParameters GetTokenValidationParameters() {
        return new TokenValidationParameters(){
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = _secretKey,
            ValidateLifetime = true
        };
    }
    public void UnwrapToken(SecurityToken token, out User? user, out Session? session) {
        var principal = _tokenHandler.ValidateToken( _tokenHandler.WriteToken(token),GetTokenValidationParameters(), out var _);
        ParsePrincipal(out user, out session, principal);
    }

    private void ParsePrincipal(out User? user, out Session? session, ClaimsPrincipal principal) {
        if (principal == null){ 
            user=null;
            session = null;
            return;
        }
        user= null;
        uint id;
        ulong sessionId;
        if ((id= Convert.ToUInt32(principal.Claims.FirstOrDefault(c => c.Type.Equals("sellerId"))?.Value))!=0){
            user = _sellerRepository.First(s => s.Id == id, includes:[[nameof(User.Session)]]);
            session = null;
        }
        else if ((id= Convert.ToUInt32(principal.Claims.FirstOrDefault(c => c.Type.Equals("userId"))?.Value))!=0){
            user = _userRepository.First(u => u.Id == id, includes:[[nameof(User.Session)]]);
            session = null;
        } else if ((sessionId = Convert.ToUInt64(principal.Claims.FirstOrDefault(c => c.Type.Equals("sessionId"))!.Value))!=0){
            session = _sessionRepository.First(s => s.Id == sessionId);
        }
        else{
            session = null;
            user = null;
        }
    }
}