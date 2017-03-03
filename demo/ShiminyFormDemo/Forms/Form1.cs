using Shiminy;
using Shiminy.API;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Forms {

    public partial class Form1 : Form, ShimInvoker {
        private AssemblyShim _assy;
        private AssemblyShim _assy2;
        private dynamic _inst;
        private dynamic _inst2;

        public Form1() {
            InitializeComponent();
            FormClosed += new FormClosedEventHandler((sender, e) => Application.Exit());
#if DEBUG
            ShiminyFactory.AddAssemblySearchPath(@"..\..\..\Thing1\bin\Debug");
            ShiminyFactory.AddAssemblySearchPath(@"..\..\..\Thing2\bin\Debug");
#else
            ShiminyFactory.AddAssemblySearchPath(@"..\..\..\Thing1\bin\Release");
            ShiminyFactory.AddAssemblySearchPath(@"..\..\..\Thing2\bin\Release");
#endif
            _assy = ShiminyFactory.MakeAssemblyShim("Thing1");
            _assy2 = ShiminyFactory.MakeAssemblyShim("Thing2");
        }
        /*
        public object Get(string name) {
            return GetType().GetEvent(name);
            //return GetType().GetProperty(name).GetValue(this);
        }
        */
        public object Invoke(string name, object[] args) {
            return ShimInvokerImpl.InvokeMember(this, name, args);
        }
        /*
        public void Set(string name, object value) {
            var evnt = GetType().GetEvent(name);
            evnt.AddEventHandler(this, (Delegate)value);
        }
        */
        private void button1_Click(object sender, EventArgs e) {
            _inst = _assy.New("Thing1.ThingForm1");
            _inst2 = _assy2.New("Thing2.ThingForm2");
            _inst.Show();
            _inst2.Show();
        }

        private void button2_Click(object sender, EventArgs e) {
            if (_inst != null) {
                _inst.Close();
                _inst2.Close();
            }
            _assy.Reload();
            _assy2.Reload();
        }
    }
}
