using System.Reflection;

namespace Shiminy.Implementation {
    public class ShimInvokerImpl {
        public static object InvokeMember(object thiz, string name, object[] args) {
            return thiz.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, thiz, args);
        }
    }
}
