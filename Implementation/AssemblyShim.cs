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
        private bool _shadowCopy;

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

        private bool StrictReferenceMatchesDefinition(AssemblyName reference, AssemblyName definition) {
            return AssemblyName.ReferenceMatchesDefinition(reference, definition) &&
                    reference.Version.Equals(definition.Version) &&
                    Enumerable.SequenceEqual(reference.GetPublicKeyToken(), definition.GetPublicKeyToken());

        }

        /// <summary>
        /// Check that definition matches a reference, with as much fidelity as is specified
        /// in the reference.  This means that if reference only contains a name, this will find
        /// the first definition wherein the name matches.  If it contains a name and a version,
        /// it will find the first definition that matches on both the name and version, etc.
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        private bool ReferenceMatchesDefinition(AssemblyName reference, AssemblyName definition) {
            if (!AssemblyName.ReferenceMatchesDefinition(reference, definition)) {
                return false;
            }

            if (definition.Version != null && definition.Version.CompareTo(reference.Version) != 0) {
                return false;
            }

            if (definition.GetPublicKeyToken() != null && !Enumerable.SequenceEqual(reference.GetPublicKeyToken(), definition.GetPublicKeyToken())) {
                return false;
            }

            return true;
        }

        private string FindAssembly(AssemblyName target) {
            string assemblyFileName = target.Name;
            if (!assemblyFileName.EndsWith(".dll")) {
                assemblyFileName += ".dll";
            }

            // Search for the assembly file (assumed to be the same name as the assembly, and a dll) in the search path.
            Queue<string> frontier = new Queue<string>(_assemblySearchPaths);
            var path = "";
            Func<string, bool> findFilePred = i => i.EndsWith("\\" + assemblyFileName, StringComparison.InvariantCultureIgnoreCase);
            while (frontier.Count > 0) {
                var next = frontier.Dequeue();
                var foundAssemblyFile = Directory.GetFiles(next).FirstOrDefault(findFilePred);
                if (foundAssemblyFile != null) {
                    var candidate = AssemblyName.GetAssemblyName(foundAssemblyFile);
                    if (ReferenceMatchesDefinition(candidate, target)) {
                        path = foundAssemblyFile;
                        break;
                    }
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
            Debug.Print($"Attempting to resolve {args.Name}, requested from {(args.RequestingAssembly != null ? args.RequestingAssembly.FullName : "<none>")} in app domain {AppDomain.CurrentDomain.FriendlyName}");
            var path = FindAssembly(an);
            if (string.IsNullOrEmpty(path)) {
                var index = an.Name.LastIndexOf('.');
                if (index >= 0 && !an.Name.Substring(index).Equals(".resources")) {
                    an.Name = an.Name.Substring(0, index);
                    path = FindAssembly(an);
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
            return New(className, new object[] { });
        }

        public dynamic New(string className, params object[] args) {
            if (!IsLoaded) {
                Reload();
            }
            // Throws System.TypeLoadException if it cant find the type.  Let that be thrown to the caller.
            dynamic wrapped = _domain.CreateInstance(_assemblyName, className, false, BindingFlags.Default, null, args, null, null);

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
            _shadowCopy = false;
            _domain.AssemblyResolve += AssemblyResolver;
            AfterLoad?.Invoke(this);
        }

        public void Load(bool shadowCopy) {
            if (IsLoaded) {
                if (!_managedDomain) {
                    throw new InvalidOperationException("This shim is attached to an app domain it does not manage.  Call Unload to detach the shim before calling Load");
                }
                Unload();
            }

            _managedDomain = true;
            _shadowCopy = shadowCopy;
            AppDomainSetup ads = new AppDomainSetup();
            //FIXME: ApplicationBase must include the referring assembly for _domain.AssemblyResolve to work,
            // but must be pointed at the referred assembly to load dependencies...I think this is ok. -- JR 03/21/17
            ads.ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // AppDomain.CurrentDomain.BaseDirectory;
            ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = true;
            ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            ads.ShadowCopyFiles = _shadowCopy.ToString();

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
            Load(_shadowCopy);
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
