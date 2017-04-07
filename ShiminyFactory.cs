using Shiminy.Implementation;
using System.Collections.Generic;

namespace Shiminy {
    public class ShiminyFactory {

        private List<string> _assemblySearchPaths = new List<string>();
        public void AddAssemblySearchPath(string path) {
            _assemblySearchPaths.Add(path);
        }

        public AssemblyShim ShimAssembly(string assemblyName) {
            return new AssemblyShim(assemblyName, _assemblySearchPaths);
        }
    }
}
