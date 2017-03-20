using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DX.JavaTestBridge.Common
{
    public class JavaTestItem
    {
        public JavaTestItem() { }
        public String MethodName { get; set; }
        public String ClassName { get; set; }
        public Int32 WorkItemID { get; set; }
    }
}
