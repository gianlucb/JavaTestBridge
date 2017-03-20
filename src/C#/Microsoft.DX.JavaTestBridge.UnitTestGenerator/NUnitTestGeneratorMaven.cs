using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DX.JavaTestBridge.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.DX.JavaTestBridge
{
    /// <summary>
    /// Generates a .NET DLL with NUnit test methods. Each created test method searches for the output of the correspondent Java test and fails/confirm accordly to the result
    /// </summary>
    public class NUnitTestGenerator : UnitTestGenerator
    {
        #region Properties

        private string ReportDirectory { get; set; }
        #endregion

        
        public NUnitTestGenerator(string className, string namespaceName, string reportDirectory)
        {

            if (String.IsNullOrEmpty(reportDirectory))
                throw new ArgumentNullException(nameof(reportDirectory));

            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            ClassName = className;
            NamespaceName = namespaceName;
            ReportDirectory = reportDirectory;

            Tests = new List<JavaTestItem>();
        }

        private string CreateTestClass()
        {
            if (String.IsNullOrEmpty(ClassName))
                throw new ArgumentNullException(nameof(ClassName));

            if (String.IsNullOrEmpty(NamespaceName))
                throw new ArgumentNullException(nameof(NamespaceName));

            if (Tests.Count == 0)
                throw new ArgumentNullException("No tests to create");


            StringBuilder baseClass = new StringBuilder($@"
            using System;
            using Microsoft.VisualStudio.TestTools.UnitTesting;
            using Microsoft.DX.JavaTestBridge.Common;
            using System.IO;
            using System.Linq;
            using System.Xml;
            using System.Xml.Linq;
            using System.Diagnostics;
            using Newtonsoft.Json;

            namespace {NamespaceName}
            {{
                [TestClass]
                public class {ClassName}
                {{
                   ");

            foreach (var t in Tests)
                baseClass.Append(CreateTestMethodBody(t));

            baseClass.Append("}}");
            return baseClass.ToString();
        }

        public override bool CreateTestAssembly(FileInfo assemblyFile)
        {
            try
            {
                //load the class -  C# code
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(CreateTestClass());

                //add all the library references
                MetadataReference[] references = new MetadataReference[]
                {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(XElement).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(XmlReader).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonConvert).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Assert).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.DX.JavaTestBridge.Common.JavaTestItem).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Process).Assembly.Location)
                };

                //set compilation options
                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyFile.Name,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    //compile it
                    EmitResult result = compilation.Emit(ms);
                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);
                        foreach (Diagnostic diagnostic in failures)
                        {
                            Trace.TraceError($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                        return false;
                    }
                    else
                    {
                        //correct, flush to disk
                        ms.Seek(0, SeekOrigin.Begin);
                        File.WriteAllBytes(assemblyFile.FullName, ms.GetBuffer());
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Trace.TraceError(ex.InnerException.Message);
                    Trace.TraceError(ex.InnerException.StackTrace);
                }
                return false;
            }
        }

        internal string CreateTestMethodBody(JavaTestItem t)
        {
            //this body search for the test result XML, that by naming convention is in the form TEST-ClassName.xml
            //the XML contains the results for each executed tests, we are here failing/confirm the tests based on the last ran result

            string testBody = $@"
                [TestMethod]
                [AutomatedTestID({t.WorkItemID})] 
                public void {t.MethodName}()
                {{
                    try
                    {{
                        string reportDirectory = @""{ReportDirectory}"";
                        string testClassName = ""TEST-{t.ClassName}"";
                        string testMethodName = ""{t.MethodName}"";

                        var xDoc = XDocument.Load(new StreamReader(reportDirectory + Path.DirectorySeparatorChar + testClassName + "".xml""));

                        var testCases = from t in xDoc.Element(""testsuite"").Elements(""testcase"")
                            where t.Attribute(""name"").Value == testMethodName
                            select t;

                        var test = testCases.FirstOrDefault();
                        if (test == null)
                            throw new Exception(""Java Method not found"");

                        var failures = test.Descendants(""failure"");
                        if (failures.Count() > 0)
                        {{
                            Assert.Fail(failures.First().Attribute(""message"").Value);
                        }}

                    }}
                    catch (Exception ex)
                    {{
                        Assert.Fail(ex.Message);
                    }}
                }}
            ";

            return testBody;
        }
    

    }
}
