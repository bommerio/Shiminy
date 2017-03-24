using Shiminy;
using Shiminy.API;
using Shiminy.Implementation;
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

namespace Thing1 {

    public partial class ThingForm1 : Form, ShimInvoker {
        public ThingForm1() {
            InitializeComponent();
        }

        public object Invoke(string name, object[] args) {
            return ShimInvokerImpl.InvokeMember(this, name, args);
        }

        private void button1_Click(object sender, EventArgs e) {
            var inst = new Class1();
            MessageBox.Show(inst.GetValue());
        }
    }
}
