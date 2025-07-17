using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Bl.Concrete;
using Ecommerce.Entity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.DesktopImpl
{
    public class LocalStorage
    {
        private static readonly string _appDataPath= Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _loginTokensPath = Path.Combine(_appDataPath, "login_tokens.txt");
        private static readonly string _anonymousTokensPath = Path.Combine(_appDataPath, "anonymous_tokens.txt");
        private readonly JwtManager _jwtManager;

        public LocalStorage(JwtManager jwtManager) {
            _jwtManager=jwtManager;
            Directory.CreateDirectory(_appDataPath);
        }
        public (User?,Session)? GetSavedSessionInfo() {
            var l = GetLoggedSessionInfo();
            if (l != null) return l;
            var a = GetAnonymousSessionInfo();
            if (a != null) return (null,a);
            return null;
        }
        public Session? GetAnonymousSessionInfo() {
            if(!File.Exists(_anonymousTokensPath)) return null;
            var t = File.ReadAllText(_anonymousTokensPath);
            _jwtManager.Deserialize(t, out _, out var s);
            return s;
        }
        public (User, Session)? GetLoggedSessionInfo() {
            if (!File.Exists(_loginTokensPath)) return null;
            var t = File.ReadAllText(_loginTokensPath);
            _jwtManager.Deserialize(t, out var u, out var s);
            return (u!, s!);
        }
        public void PersistLoginInfo(SecurityToken token) {
            File.WriteAllText(_loginTokensPath, _jwtManager.Serialize(token));   
        }

        public void PersistAnonymousSession(Session session) {
            File.WriteAllText(_anonymousTokensPath, _jwtManager.Serialize(_jwtManager.CreateToken(session)));
        }
        public void DeleteLoginInfo() { 
            File.Delete(_loginTokensPath);
        }
    }
}
