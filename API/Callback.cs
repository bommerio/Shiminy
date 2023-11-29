using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiminy.API {
    public interface Callback {
        void Call();
    }


    public interface Callback<T> {
        T Call();
    }

    public interface Callback1<TArg,T> {
        T Call(TArg arg);
    }
}
