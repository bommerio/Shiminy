using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shiminy {
    public class ShimInvokerImpl {
        public static object InvokeMember(object thiz, string name, object[] args) {
            return thiz.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, thiz, args);
        }
    }
}
