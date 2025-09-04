using System.Data.Common;
using Ecommerce.Bl.Interface;
using Ecommerce.Dao.Spi;
using Ecommerce.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Bl.Concrete;

public class SessionManager : ISessionManager
{
    private readonly DbContext _dbContext;

    public SessionManager([FromKeyedServices("DefaultDbContext")] DbContext dbContext) {
        _dbContext = dbContext;

    }

    public Session newSession(User? newUser, bool flush = false) {
        var session = new Session(){ Cart = new Cart{ } };
        session.Cart.Session = session;
        session = _dbContext.Add(session).Entity;
        if (newUser != null){
            session.User = newUser.Id != 0 ? null! : newUser;
            newUser.Session = session;
        }

        if (!flush) return session;
        _dbContext.SaveChanges();
        return session;
    }
}