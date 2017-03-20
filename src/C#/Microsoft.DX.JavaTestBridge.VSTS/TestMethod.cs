
using System;


namespace Microsoft.DX.JavaTestBridge.VSTS
{

    public class AutomatedTestMethod
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Assembly { get; set; }
        public string Type { get; set; }
        public bool Associated { get; set; }

        public int TestID { get; set; }

        public AutomatedTestMethod(string name, string fullname, string assembly)
        {
            this.Name = name;
            this.FullName = fullname;
            this.Type = "Unit Test";
            this.Assembly = assembly;
            Associated = false;
        }

        public override string ToString()
        {
            return String.Format("{0}, TestID={1}", Name, TestID);
        }

    }
}
