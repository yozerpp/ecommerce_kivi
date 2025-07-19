using Ecommerce.Entity;

namespace Ecommerce.Bl;

/// <summary>
/// This store is the shared context between the container implementation (be it a web app or a desktop app)
/// and the business layer components. Web app can set the session, from here.
/// </summary>
public static class ContextHolder
{
    // private static readonly ThreadLocal<Session> _session = new ThreadLocal<Session>();
    private static Session _session;
    public static Session? Session
    {
        get => _session;
        set => _session = value;
    }

    public static  User GetUserOrThrow() {
        return Session?.User??throw new UnauthorizedAccessException("You aren't logged in.");
    }
}