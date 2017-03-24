using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;

namespace Shiminy.API.Messaging {
    //TODO: couldnt get marshal by value to work here...would love to know why
    public class ShimInvokerMessage : MarshalByRefObject, IMessage {

        public IDictionary Properties { get; set; }

        public string Name {
            get {
                return (string)Properties["name"];
            }
            private set {
                Properties["name"] = value;
            }
        }

        public object[] Args {
            get {
                int argcount = (int)Properties["argcount"];
                object[] args = new object[argcount];
                for (int ii = 0; ii < argcount; ++ii) {
                    string key = $"arg{ii}";
                    args[ii] = Properties[key];
                }
                return args;
            }
            private set {
                Properties["argcount"] = value.Length;
                for (int ii = 0; ii < value.Length; ++ii) {
                    string key = $"arg{ii}";
                    Properties[key] = value[ii];
                }
            }
        }

        public object Response {
            get {
                return Properties.Contains("response") ? Properties["response"] : null;
            }
            set {
                Properties["response"] = value;
            }
        }

        public ShimInvokerMessage(string name, object[] args) {
            Properties = new Dictionary<string, object>();
            Name = name;
            Args = args;
        }
    }
}
