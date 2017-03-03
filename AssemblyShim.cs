using Shiminy.API;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Shiminy {

    public delegate void AfterLoadDelegate(AssemblyShim shim);
    public delegate void BeforeUnloadDelegate(AssemblyShim shim);

    //TODO             Shiminy.SeparateAppDomainForEachAssembly = true;

    [Serializable]
    public class AssemblyShim {

        private string _domainName;
        private string _assemblyName;
        private List<string> _assemblySearchPaths;
        private AppDomain _domain;

        public event AfterLoadDelegate AfterLoad;
        public event BeforeUnloadDelegate BeforeUnload;

        public bool IsLoaded {
            get {
                return _domain != null;
            }
        }

        public AssemblyShim(string assemblyName, List<string> assemblySearchPaths) {
            _domainName = assemblyName.ToLower();
            _assemblyName = assemblyName;
            _assemblySearchPaths = assemblySearchPaths;
        }

        public dynamic New(string className) {
            if (!IsLoaded) {
                Reload();
            }
            return new ShimmedInstance((ShimInvoker)_domain.CreateInstanceAndUnwrap(_assemblyName, className));
        }
        // Loads the content of a file to a byte array. 
        static byte[] loadFile(string filename) {
            FileStream fs = new FileStream(filename, FileMode.Open);
            byte[] buffer = new byte[(int)fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            return buffer;
        }

        private Assembly AssemblyRevolver(object sender, ResolveEventArgs args) {
            AppDomain domain = (AppDomain)sender;

            string assemblyFileName = args.Name;
            if (!assemblyFileName.EndsWith(".dll")) {
                assemblyFileName += ".dll";
            }

            // Search for the assembly file (assumed to be the same name as the assembly, and a dll) in the search path.
            Queue<string> frontier = new Queue<string>(_assemblySearchPaths);
            var path = "";
            while (frontier.Count > 0) {
                var next = frontier.Dequeue();
                var containsAssemblyFile = Directory.GetFiles(next).Any(i => i.EndsWith("\\" + assemblyFileName));
                if (containsAssemblyFile) {
                    path = $"{next}\\{assemblyFileName}";
                    break;
                }
                foreach (var child in Directory.GetDirectories(next)) {
                    frontier.Enqueue(child);
                }
            }

            Assembly assembly = null;
            if (!string.IsNullOrEmpty(path)) {
                byte[] rawAssembly = loadFile(path);
                //byte[] rawSymbolStore = loadFile("temp.pdb");
                assembly = domain.Load(rawAssembly);
            }

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
            ads.ApplicationBase = System.Environment.CurrentDirectory;
            ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = true;
            ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            _domain = AppDomain.CreateDomain(_domainName, null, ads);
            _domain.AssemblyResolve += new ResolveEventHandler(AssemblyRevolver);

            AfterLoad?.Invoke(this);
        }

        private void Unload() {
            BeforeUnload?.Invoke(this);
            AppDomain.Unload(_domain);
        }
    }
}
