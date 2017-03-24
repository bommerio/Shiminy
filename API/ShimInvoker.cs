using System;
using System.Runtime.InteropServices;

namespace Shiminy.API {
    [ComVisible(true)]
    public interface ShimInvoker {
        object Invoke(string name, object[] args);
    }
}
