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
    public class NUnitTestGenerator : UnitTestGenerator
    {
        public NUnitTestGenerator(string className, string namespaceName) {
         
            if (String.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className));

            if (String.IsNullOrEmpty(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            ClassName = className;
            NamespaceName = namespaceName;

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

           foreach(var t in Tests)
                baseClass.Append(CreateTestMethodBody(t));

            baseClass.Append("}}");
            return baseClass.ToString();
        }

        internal virtual string CreateTestMethodBody(JavaTestItem t)
        {

            // string testBody = $"[TestMethod] [AutomatedTestID({t.WorkItemID})] public void {t.MethodName}(){{Process.Start(\"java\", @\"-cp \"{t.ClassPath}\" SingleJUnitTestRunner {t.ClassName}#{t.MethodName}\");}}";
            //string testBody = $@"
            //    [TestMethod]
            //    [AutomatedTestID({t.WorkItemID})] 
            //    public void {t.MethodName}()
            //    {{
            //        try
            //        {{
            //            var proc = new Process
            //            {{
            //                StartInfo = new ProcessStartInfo
            //                {{
            //                    FileName = ""java"",
            //                    Arguments = @""-cp """"{t.ClassPath}"""" microsoft.dx.javatestbridge.SingleJUnitTestRunner {t.ClassNameFullName}#{t.MethodName} {t.Arguments}"",
            //                    UseShellExecute = true,
            //                    CreateNoWindow = false,
            //                    LoadUserProfile = true
            //                }}
            //            }};
            //            proc.Start();
            //            proc.WaitForExit();

            //            JavaTestResult result = JsonConvert.DeserializeObject<JavaTestResult>(File.ReadAllText(""{t.ClassNameFullName}#{t.MethodName}.json""));

            //            if (!result.Result)
            //            {{
            //                Assert.Fail(result.StackTrace);
            //            }}

            //            Assert.IsTrue(result.Result);   

            //        }}
            //        catch (Exception ex)
            //        {{
            //            Assert.Fail(ex.Message);
            //        }}
            //    }}
            //";
            //return testBody;
            return String.Empty;
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

    }
}
