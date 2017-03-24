using Shiminy.Implementation;
using System.Collections.Generic;

namespace Shiminy {
    public class ShiminyFactory {

        private static List<string> _assemblySearchPaths = new List<string>();
        public static void AddAssemblySearchPath(string path) {
            _assemblySearchPaths.Add(path);
        }

        public static AssemblyShim MakeAssemblyShim(string assemblyName) {
            return new AssemblyShim(assemblyName, _assemblySearchPaths);
        }
    }
}
