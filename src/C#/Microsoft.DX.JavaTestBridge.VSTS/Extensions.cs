using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace Microsoft.DX.JavaTestBridge.VSTS
{
    public static class VSTSExtensions
    {
        public static void SetAssociatedAutomation(this ITestCase testCase, ITestManagementTeamProject project, AutomatedTestMethod testMethod)
        {
            if (testCase == null)
                return;

            //create a GUID ID
            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var hash = cryptoServiceProvider.ComputeHash(Encoding.Unicode.GetBytes(testMethod.Name));
            var bytes = new byte[16];
            Array.Copy(hash, bytes, 16);

            var automationGuid = new Guid(bytes);
            testCase.Implementation = project.CreateTmiTestImplementation(testMethod.FullName, testMethod.Type, testMethod.Assembly, automationGuid);
        }
    }
}