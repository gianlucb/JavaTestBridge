using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Diagnostics;
using Microsoft.DX.JavaTestBridge.Common;
using System.Xml.Linq;
using System.Xml;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.DX.JavaTestBridge
{
    //can be run with vstest.console.exe
    class Program
    {
        static int Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            try
            {
                CheckArguments(args);

                List<JavaTestItem> tests;
                string JSONMap = "";

                //the second argument can be directly the JSON string or the path of a JSON file
                //we fails fast to determine which one and in all cases we end with the content
                if (File.Exists(args[1]))
                    JSONMap = File.ReadAllText(args[1]);
                else
                    JSONMap = args[1];

                tests = JsonConvert.DeserializeObject<List<JavaTestItem>>(JSONMap);
           
                var assemblyFile = new FileInfo($"{args[0]}.dll");

                UnitTestGenerator testGenerator = new NUnitTestGenerator(args[0], "Microsoft.DX.JavaTestBridge.DynamicTests", args[2]) { Tests = tests };

                var result = testGenerator.CreateTestAssembly(assemblyFile);
                if (result)
                {
                    Trace.TraceInformation($"SUCCESS!\n{assemblyFile.FullName} written to disk.\nTests created: {tests.Count}");
                    return 0;
                }
                else
                   throw new Exception($"Assembly creation failed");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return -2;
            }
            finally
            {
                Trace.WriteLine("Done.");
            }          
        }

        private static void CheckArguments(string[] args)
        {

            if (args.Length != 3)
            {
                Trace.TraceError($"usage: <ASSEMBLY_NAME> <TESTS_JSON_MAPPING_FILE> (or escaped JSON string) <REPORT_FOLDER>");
                throw new InvalidProgramException("wrong usage! missing arguments ");
            }
            if (!Directory.Exists(args[2]))
            {
                throw new Exception("report folder does not exist");
            }
        }
    }
}
