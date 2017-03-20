using Microsoft.DX.JavaTestBridge.Common;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Microsoft.DX.JavaTestBridge.VSTS
{
    public class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Trace.Listeners.Add(new ConsoleTraceListener());

                CheckArguments(args);

                var project = GetProject(new Uri(args[0]), args[1], args[3], args[4]);

                if (project != null)
                {
                    List<AutomatedTestMethod> tests = DiscoverAutomatedTests(new FileInfo(args[2]));

                    foreach (var t in tests)
                        AssociateTestCase(project, t);

                    int found = tests.Count;
                    int associated = tests.Count((x) => x.Associated);
                    Trace.TraceInformation($"Found {found} tests, associated {associated} ({(double)associated / found * 100}%)");
                    return 0;
                }

                Trace.TraceInformation("Done.");
                return -1;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
                return -2;
            }
        }

        private static void CheckArguments(string[] args)
        {

            if (args.Length != 5)
            {
                Trace.TraceError($"usage: <VSTS_URI> <PROJECT_NAME> <TEST_ASSEMBLY_FILE> <USERNAME> <PASSWORD> ");
                throw new InvalidProgramException("wrong usage! missing arguments ");
            }
            if (!File.Exists(args[2]))
            {
                throw new Exception("Assembly file invalid or missing");
            }
        }

        public static List<AutomatedTestMethod> DiscoverAutomatedTests(FileInfo assemblyFile)
        {
            if (!assemblyFile.Exists || assemblyFile == null)
                throw new FileNotFoundException($"Assembly file not found");

            List<AutomatedTestMethod> foundTests = new List<AutomatedTestMethod>();

            Assembly testAssembly = Assembly.LoadFrom(assemblyFile.FullName);

            foreach (Type type in testAssembly.GetTypes())
            {
                if (type.IsClass)
                {
                    foreach (MethodInfo methodInfo in type.GetMethods())
                    {
                        var automatedTestAttribute = Attribute.GetCustomAttribute(methodInfo, typeof(AutomatedTestIDAttribute));
                        if (automatedTestAttribute != null)
                        {
                            AutomatedTestMethod t = new AutomatedTestMethod(methodInfo.Name, methodInfo.DeclaringType.FullName + "." + methodInfo.Name, assemblyFile.Name);
                            t.TestID = ((AutomatedTestIDAttribute)automatedTestAttribute).WorkItemID;
                            foundTests.Add(t);
                            Trace.TraceInformation($"Found TEST method: {t}" );
                        }
                    }
                }
            }

            return foundTests;

        }

        public static bool AssociateTestCase(ITestManagementTeamProject project, AutomatedTestMethod testMethod)
        {
            try
            {
                var automatedTest = project.TestCases.Find(testMethod.TestID);
                if (automatedTest != null)
                {
                    automatedTest.SetAssociatedAutomation(project, testMethod);
                    automatedTest.Save();
                    testMethod.Associated = true;
                    Trace.TraceInformation($"Automated test method {testMethod.FullName} associated to {testMethod.TestID} | {automatedTest.Title}");
                    return true;
                }
                Trace.TraceWarning($"Test with ID {testMethod.TestID} not found");
                return false;
            }
            catch (DeniedOrNotExistException ex)
            {
                Trace.TraceWarning(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return false;
            }
        }

        public static ITestManagementTeamProject GetProject(Uri tfsUri, string projectName, string username, string password)
        {
            try
            {
                Trace.TraceInformation($"Connecting to VSTS {tfsUri.AbsoluteUri}, Project: {projectName}");

                var networkCredential = new NetworkCredential(username, password);
                BasicAuthCredential basicCred = new BasicAuthCredential(networkCredential);
                TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
                tfsCred.AllowInteractive = false;

                TfsTeamProjectCollection projectsCollection = new TfsTeamProjectCollection(tfsUri,tfsCred);
                ITestManagementService service = (ITestManagementService)projectsCollection.GetService(typeof(ITestManagementService));
                ITestManagementTeamProject project = service.GetTeamProject(projectName);

                Trace.TraceInformation($"project {projectName} found");

                return project;
            }
            catch (TestObjectNotFoundException)
            {
                Trace.TraceError($"Project {projectName} not found");
                return null;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return null;
            }
        }
    }
}
