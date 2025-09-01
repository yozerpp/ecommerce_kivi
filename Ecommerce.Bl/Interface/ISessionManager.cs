using Ecommerce.Entity;

namespace Ecommerce.Bl.Interface;

public interface ISessionManager
{
    public Session newSession(User? user, bool flush = false);
}