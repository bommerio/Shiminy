using Shiminy.API.Messaging;
using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Shiminy.API {
    public abstract class ShimmableMessageSink : Shimmable, IMessageSink {
        public IMessageSink NextSink {
            get {
                return null;
            }
        }

        public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink) {
            throw new NotImplementedException();
        }

        public IMessage SyncProcessMessage(IMessage msg) {
            var invokerMsg = (ShimInvokerMessage)msg;
            invokerMsg.Response = Invoke(invokerMsg.Name, invokerMsg.Args);
            return invokerMsg;
        }
    }
}
