using Ecommerce.Entity;

namespace Ecommerce.Bl;

public static class ContextHolder
{
    private static readonly ThreadLocal<Session> _session = new ThreadLocal<Session>();
     
    public static Session? Session
    {
        get => _session.Value;
        set => _session.Value = value;
    }
}