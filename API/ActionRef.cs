using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Shiminy.API {
    public class ActionRef : MarshalByRefObject, Callback {
        private Action _action;

        public ActionRef(Action action) {
            _action = action;
        }

        public void Call() {
            _action();
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService() {
            return null;
        }
    }
}
