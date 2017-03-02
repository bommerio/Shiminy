using System;
using System.Reflection;

namespace Shiminy.API {
    public abstract class ShimBase : MarshalByRefObject, ShimInvoker {
        public object Get(string name) {
            throw new NotImplementedException();
        }

        public object Invoke(string name, object[] args) {
            return GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, this, args);
        }

        public void Set(string name, object value) {
            throw new NotImplementedException();
        }
    }
}
