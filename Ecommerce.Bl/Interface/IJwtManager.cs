using System.Security.Claims;
using Ecommerce.Entity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Bl.Interface;

public interface IJwtManager
{
    public void ParsePrincipal(out User? user, out Session? session, ClaimsPrincipal principal);
    public void UnwrapToken(SecurityToken token, out User? user, out Session? session);
    public SecurityToken CreateToken(Session session, bool rememberMe = false);
    public List<Claim> GetClaims(Session session);
    public string CreateAuthToken(string key, TimeSpan lifetime);
    public string? ReadAuthToken(string token);
    public void Deserialize(string token, out User? user, out Session? session);
    public string Serialize(SecurityToken securityToken);
}