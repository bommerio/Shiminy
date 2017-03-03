using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
