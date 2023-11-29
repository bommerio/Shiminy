using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Shiminy.API {
    public class FuncRef<T> : MarshalByRefObject, Callback<T> {
        private Func<T> _func;

        public FuncRef(Func<T> func) {
            _func = func;
        }

        public T Call() {
            return _func();
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService() {
            return null;
        }
    }

    public class FuncRef1<TArg, T> : MarshalByRefObject, Callback1<TArg, T> {
        private Func<TArg, T> _func;

        public FuncRef1(Func<TArg, T> func) {
            _func = func;
        }

        public T Call(TArg arg) {
            return _func(arg);
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService() {
            return null;
        }
    }
}
