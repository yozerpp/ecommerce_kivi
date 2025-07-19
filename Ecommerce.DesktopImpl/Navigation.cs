using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.DesktopImpl
{
    public class Navigation()
    {
        private readonly Stack<UserControl> _back= new();
        private readonly Stack<UserControl> _forward=new();
        private UserControl current;
        public UserControl? MainPage { get; set; } = null;
        public void Go(UserControl? from, UserControl? to, bool forward = true) {
            from ??= current;
            from.Visible = false;
            from.Enabled = false;
            from.SendToBack();
            if(forward)
            _back.Push(from);
            else _forward.Push(from);
            to ??= MainPage;
            current = to;
            to.Enabled = true;
            to.Visible = true;
            to.BringToFront();
            to.Select();
            ((IPage)to).Go();
        }

        public void Refresh()
        {
            ((IPage)current).Go();
        }
        public void Back()
        {
            if(_back.Count==0) return;
            var top = _back.Pop();
            Go(current, top, false);
            current = top;
        }

        public void Forward()
        {
            if(_forward.Count==0) return;
            var top = _forward.Pop();
            Go(current, top);
            current = top;
        }
        
    }
}
