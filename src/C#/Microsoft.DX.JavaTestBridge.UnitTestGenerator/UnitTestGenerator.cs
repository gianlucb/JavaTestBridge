using Microsoft.DX.JavaTestBridge.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DX.JavaTestBridge
{
    public abstract class  UnitTestGenerator
    {
        #region Properties
        public String ClassName { get; set; }
        public String NamespaceName { get; set; }
        public List<JavaTestItem> Tests { get; set; }
        #endregion
        public abstract bool CreateTestAssembly(FileInfo assemblyFile);
    }
}
