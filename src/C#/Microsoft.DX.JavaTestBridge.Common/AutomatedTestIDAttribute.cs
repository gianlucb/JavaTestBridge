using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DX.JavaTestBridge.Common
{
    public class AutomatedTestIDAttribute : Attribute
    {
        public int WorkItemID { get; set; }
        public AutomatedTestIDAttribute(int workItemID) { WorkItemID = workItemID; }
    }
}
