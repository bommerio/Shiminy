using Shiminy;
using Shiminy.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Thing2 {

    public partial class ThingForm2 : Form, ShimInvoker {
        public ThingForm2() {
            InitializeComponent();
        }

        public object Invoke(string name, object[] args) {
            return GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, this, args);
        }

        private void button1_Click(object sender, EventArgs e) {
            var inst = new Class1();
            MessageBox.Show(inst.GetValue());
        }
    }
}
