using Shiminy;
using Shiminy.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicLoadingTest {
    static class Program {
        private static AssemblyShim _shim;
        private static dynamic _form;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
            ShiminyFactory.AddAssemblySearchPath(@"..\..\..\..\Forms\bin\x64\Debug");
#else
            ShiminyFactory.AddAssemblySearchPath(@"..\..\..\..\Forms\bin\x64\Release");
#endif
            _shim = ShiminyFactory.MakeAssemblyShim("Forms");
            _shim.BeforeUnload += new BeforeUnloadDelegate((assm) => {
                _form.Close();
            });
            _shim.AfterLoad += new AfterLoadDelegate((assm) => {
                _form = assm.New("Forms.Form1");
                assm.New("Forms.Class1");
                _form.ShowDialog();
            });
            _shim.Load();
            Application.Run();
        }
    }
}
