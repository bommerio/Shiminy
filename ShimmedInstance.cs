using Shiminy.API;
using System.Dynamic;

namespace Shiminy {
    public class ShimmedInstance : DynamicObject {
        private ShimInvoker _invoker;

        public ShimmedInstance(ShimInvoker invoker) {
            _invoker = invoker;
        }
        /*
        public override bool TryConvert(ConvertBinder binder, out object result) {
            return base.TryConvert(binder, out result);

        }

        public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result) {
            return base.TryCreateInstance(binder, args, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
            return base.TryGetIndex(binder, indexes, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = _invoker.Get(binder.Name);
            return true;
        }
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) {
            return base.TrySetIndex(binder, indexes, value);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value) {
            _invoker.Set(binder.Name, value);
            return true;
        }
        */
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            if (_invoker != null) {
                result = _invoker.Invoke(binder.Name, args);
                return true;
            } else {
                return base.TryInvokeMember(binder, args, out result);
            }
        }
    }
}
