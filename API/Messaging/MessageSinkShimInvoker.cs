using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Shiminy.API.Messaging {
    public class MessageSinkShimInvoker : ShimInvoker {
        private IMessageSink _sink;

        public MessageSinkShimInvoker(IMessageSink sink) {
            _sink = sink;
        }
        public object Invoke(string name, object[] args) {
            var msg = _sink.SyncProcessMessage(new ShimInvokerMessage(name, args));
            if (!(msg is ShimInvokerMessage)) {
                throw new Exception("ShimInvokerMessage response must be a ShimInvokerMessageResponse");
            }
            return ((ShimInvokerMessage)msg).Response;
        }
    }
}
