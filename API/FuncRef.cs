using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
