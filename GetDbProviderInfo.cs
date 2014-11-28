using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace InvokeQuery
{
    [Cmdlet("Get", "DbProviderInfo")]
    public class GetDbProviderInfo : PSCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string AssemblyPath { get; set; }

        protected override void BeginProcessing()
        {
            if (!File.Exists(AssemblyPath))
            {
                throw new ArgumentException("Assembly doesn't exist.");
            }
            var providerTypes = GetDbProviderTypes(AssemblyPath).ToList();

            foreach (var providerType in providerTypes)
            {
                var providerTypeString = string.Format("<add name=\"put name here\" invariant=\"{0}\" description=\".NET Framework Data Provider for {0}\" type=\"{1}\" />", 
                    providerType.Namespace, providerType.AssemblyQualifiedName);
                WriteObject(providerTypeString);
            }

            WriteWarning("Found " + providerTypes.Count + " types that implement DbProviderFactory.");
            WriteWarning("The assembly was loaded into memory for this operation. If you need to delete or move the assembly, you'll probably have to close this powershell window.");
        }

        private IEnumerable<Type> GetDbProviderTypes(string assemblyPath)
        {
            var assembly = Assembly.LoadFile(AssemblyPath);
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof (System.Data.Common.DbProviderFactory)))
                {
                    yield return type;
                }
            }
        }
    }
}
