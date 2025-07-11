using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Dao.Iface;
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
    public JwtManager(IRepository<User> userRepository, IRepository<Seller> sellerRepository, IRepository<Session> sessionRepository,string? secretKey = null) {
        if (secretKey==null){
             secretKey = "uDF$Gldpgl3*-4-ags";
        }
        this._sellerRepository = sellerRepository;
        this._userRepository = userRepository;
        this._sessionRepository = sessionRepository;
        _secretKey = new SymmetricSecurityKey(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(secretKey)));
        _credentials = new SigningCredentials(_secretKey, SecurityAlgorithms.HmacSha256);
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
            default:
            return null;
        }
        return _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = _credentials
        });
    }
    public void UnwrapToken(SecurityToken token, out User? user, out Session? session) {
        var principal = _tokenHandler.ValidateToken( _tokenHandler.WriteToken(token),new TokenValidationParameters(){
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = _secretKey,
            ValidateLifetime = true,
        }, out var _);
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