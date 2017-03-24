using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Shiminy.API {
    public abstract class Shimmable : MarshalByRefObject, ShimInvoker {
        public object Invoke(string name, object[] args) {
            return GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, this, args);
        }
    }
}
