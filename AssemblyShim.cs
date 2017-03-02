using Shiminy.API;
using System;
using System.Dynamic;
using System.IO;
using System.Reflection;

namespace Shiminy {

    public delegate void AfterLoadDelegate(AssemblyShim shim);
    public delegate void BeforeUnloadDelegate(AssemblyShim shim);

    //TODO             Shiminy.SeparateAppDomainForEachAssembly = true;


    public class AssemblyShim {

        private AppDomain _domain;
        private string _assyName;
        private string _domainName;

        public event AfterLoadDelegate AfterLoad;
        public event BeforeUnloadDelegate BeforeUnload;

        public bool IsLoaded {
            get {
                return _domain != null;
            }
        }

        public AssemblyShim(string assyName) {
            _domainName = assyName.ToLower();
            _assyName = assyName;
        }

        public dynamic New(string className) {
            if (!IsLoaded) {
                Reload();
            }
            return new ShimmedInstance((ShimInvoker)_domain.CreateInstanceAndUnwrap(_assyName, className));
        }
        // Loads the content of a file to a byte array. 
        static byte[] loadFile(string filename) {
            FileStream fs = new FileStream(filename, FileMode.Open);
            byte[] buffer = new byte[(int)fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            return buffer;
        }
        static Assembly MyResolver(object sender, ResolveEventArgs args) {
            AppDomain domain = (AppDomain)sender;

            var path = "";
            if (args.Name.Contains("Form")) {
                path = "c:\\Users\\Engineering\\Documents\\Development\\Bommer\\DynamicLoadingTest\\Forms\\bin\\Debug\\Forms.dll";
            } else if (args.Name.Contains("Thing2")) {
                path = "c:\\Users\\Engineering\\Documents\\Development\\Bommer\\DynamicLoadingTest\\Thing2\\bin\\Debug\\Thing2.dll";
            } else {
                path = "c:\\Users\\Engineering\\Documents\\Development\\Bommer\\DynamicLoadingTest\\Thing1\\bin\\Debug\\Thing1.dll";
            }
            byte[] rawAssembly = loadFile(path);
            //byte[] rawSymbolStore = loadFile("temp.pdb");
            Assembly assembly = domain.Load(rawAssembly);

            return assembly;
        }

        public void Load() {
            if (!IsLoaded) {
                Reload();
            }
        }

        public void Reload() {
            if (IsLoaded) {
                Unload();
            }
            AppDomainSetup ads = new AppDomainSetup();
            ads.ApplicationBase = System.Environment.CurrentDirectory; ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = true;
            ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            _domain = AppDomain.CreateDomain(_domainName, null, ads);
            _domain.AssemblyResolve += new ResolveEventHandler(MyResolver);

            AfterLoad?.Invoke(this);
        }

        private void Unload() {
            BeforeUnload?.Invoke(this);
            AppDomain.Unload(_domain);
        }
    }
}
