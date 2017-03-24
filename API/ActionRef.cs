using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
