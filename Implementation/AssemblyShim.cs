using Shiminy.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;

namespace Shiminy.Implementation {

    public delegate void AfterLoadDelegate(AssemblyShim shim);
    public delegate void BeforeUnloadDelegate(AssemblyShim shim);
    public delegate void FileChangedDelegate(AssemblyShim shim);

    //TODO             Shiminy.SeparateAppDomainForEachAssembly = true;

    [Serializable]
    public class AssemblyShim {

        private string _domainName;
        private string _assemblyName;
        private List<string> _assemblySearchPaths;
        private AppDomain _owningDomain;
        private AppDomain _domain;
        private bool _managedDomain;

        public string AssemblyPath { get; }

        public event AfterLoadDelegate AfterLoad;
        public event BeforeUnloadDelegate BeforeUnload;

        public bool IsLoaded {
            get {
                return _domain != null;
            }
        }

        public bool ReloadLater { get; private set; }

        public AssemblyShim(string assemblyName, List<string> assemblySearchPaths) {
            _owningDomain = AppDomain.CurrentDomain;
            _domainName = assemblyName.ToLower();
            _assemblyName = assemblyName;
            _assemblySearchPaths = new List<string>(assemblySearchPaths);
            AssemblyPath = FindAssembly(new AssemblyName(assemblyName));
        }

        public void AddAssemblySearchPath(string path) {
            _assemblySearchPaths.Add(path);
        }

        private string FindAssembly(AssemblyName assemblyName) {
            string assemblyFileName = assemblyName.Name;
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
            return path;
        }


        /*
        // Loads the content of a file to a byte array.
        static byte[] loadFile(string filename) {
            FileStream fs = new FileStream(filename, FileMode.Open);
            byte[] buffer = new byte[(int)fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            return buffer;
        }*/

        private Assembly AssemblyResolver(object sender, ResolveEventArgs args) {
            AppDomain domain = (AppDomain)sender;
            AssemblyName an = new AssemblyName(args.Name);
            Debug.Print($"Attempting to resolve {args.Name}, requested from {(args.RequestingAssembly != null ? args.RequestingAssembly.FullName : "<none>")}");
            var path = "";
            if (args.Name.Equals(_assemblyName)) {
                path = FindAssembly(an);
            } else {
                path = FindAssembly(an);
                if (string.IsNullOrEmpty(path)) {
                    var index = an.Name.LastIndexOf('.');
                    if (index >= 0 && !an.Name.Substring(index).Equals(".resources")) {
                        an.Name = an.Name.Substring(0, index);
                        path = FindAssembly(an);
                    }
                }
            }

            Assembly assembly = null;
            if (!string.IsNullOrEmpty(path)) {
                assembly = Assembly.LoadFrom(path);
            }

            return assembly;
        }

        private bool InLoadedDomain() {
            return AppDomain.CurrentDomain.Equals(_domain);
        }

        public dynamic New(string className) {
            if (!IsLoaded) {
                Reload();
            }
            // Throws System.TypeLoadException if it cant find the type.  Let that be thrown to the caller.
            dynamic wrapped = _domain.CreateInstance(_assemblyName, className);

            try {
                dynamic obj = wrapped.Unwrap();
                return obj is IMessageSink ? new ShimmedInstance(obj as IMessageSink) :
                       obj is ShimInvoker ? new ShimmedInstance(obj as ShimInvoker) :
                       obj; // For built in types, we can just return the raw object
            } catch (System.Runtime.Serialization.SerializationException) {
                //NOTE: Using Serializable means marshal by value (make an isolated copy),
                //      Using MarshalByRefObject means that we'll get an object that change state of the actual instantiated object.
                Debug.Print("Instance instantiated, but cannot unwrap type.  Type must be marked as serializable or extend MarshalByRefObject in order to get the object to return.");
                return null;
            }
        }

        public void Attach(AppDomain domain) {
            _domain = domain;
            _managedDomain = false;
            _domain.AssemblyResolve += AssemblyResolver;
            AfterLoad?.Invoke(this);
        }

        public void Load() {
            if (IsLoaded) {
                if (!_managedDomain) {
                    throw new InvalidOperationException("This shim is attached to an app domain it does not manage.  Call Unload to detach the shim before calling Load");
                }
                Unload();
            }

            _managedDomain = true;
            AppDomainSetup ads = new AppDomainSetup();
            //FIXME: ApplicationBase must include the referring assembly for _domain.AssemblyResolve to work,
            // but must be pointed at the referred assembly to load dependencies...I think this is ok. -- JR 03/21/17
            ads.ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // AppDomain.CurrentDomain.BaseDirectory;
            ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = true;
            ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            ads.ShadowCopyFiles = "true";

            Evidence securityInfo = new Evidence();
            _domain = AppDomain.CreateDomain(_domainName, securityInfo, ads);
            _domain.AssemblyResolve += AssemblyResolver;

            AfterLoad?.Invoke(this);

        }

        public void Reload() {
            if (IsLoaded) {
                if (!_managedDomain) {
                    throw new InvalidOperationException("This shim is attached to an app domain it does not manage.  Call Unload to detach the shim before calling Reload");
                }
                Unload();
            }
            //TODO: this should work, so that we can pass functions that reload across app domain boundaries.  For now, it doesnt, so we need to work around it.
            /*            if (!_owningDomain.Equals(AppDomain.CurrentDomain)) {
                            _owningDomain.DoCallBack(() => Reload());
                            return;
                        }*/
            /*
            if (InLoadedDomain()) {
                throw new InvalidOperationException("Cannot reload a domain )
                _owningDomain.
                ReloadLater = true;
                return;
            }*/
            Unload();
            Load();
        }
        
        public void Unload() {
            if (IsLoaded) {
                BeforeUnload?.Invoke(this);
                if (_managedDomain) {
                    AppDomain.Unload(_domain);
                }
                _domain = null;
            }
        }
    }
}
