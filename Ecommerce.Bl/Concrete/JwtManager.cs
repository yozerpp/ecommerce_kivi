using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Entity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Concrete;

public class JwtManager
{
    private readonly SecurityKey _secretKey;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _credentials ;
    public JwtManager(string? secretKey = null) {
        if (secretKey==null){
             secretKey = "uDF$Gldpgl3*-4-ags";
        }
        _secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _credentials = new SigningCredentials(_secretKey, SecurityAlgorithms.HmacSha256);
    }

    public SecurityToken CreateToken(Session session) {
        var claims = new List<Claim>(new[]{
            new Claim("sessionId", session.Id.ToString()),
        });
        if (session.User != null){
            if (session.User is Seller s){
                claims.Add(new Claim("sellerId",s.Id.ToString()));
            } else claims.Add(new Claim("userId",session.User.Id.ToString()));
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

        string? id;
        if ((id=principal.Claims.FirstOrDefault(c=>c.Type.Equals("sellerId"))?.Value)!=null){
            user = new Seller();
            user.Id = Convert.ToUInt32(id);
            session = null;
        }
        else if ((id=principal.Claims.FirstOrDefault(c=>c.Type.Equals("userId"))?.Value)!=null){
            user = new User();
            user.Id = Convert.ToUInt32(id);
            session = null;
        }
        else if((id = principal.Claims.FirstOrDefault(c => c.Type.Equals("sessionId"))?.Value) != null){
            user = null;
            session = new Session();
            session.Id = Convert.ToUInt32(id);
        }
        else{
            user = null;
            session = null;
        }
    }
}