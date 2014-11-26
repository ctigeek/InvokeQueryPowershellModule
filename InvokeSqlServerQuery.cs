using System;
using System.Management.Automation;
using Microsoft.Win32;

namespace InvokeQuery
{
    [Cmdlet("Invoke","SqlServerQuery")]
    public class InvokeSqlServerQuery : InvokeQueryBase
    {
        private const string SqlServerProvider = "System.Data.SqlClient";
        public InvokeSqlServerQuery()
        {
            ProviderInvariantName = SqlServerProvider;
        }

        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(Server))
            {
                var localInstanceName = FindLocalSqlInstance();
                if (localInstanceName == null)
                {
                    throw new ArgumentNullException("Server","Server name not specified and no local instance of Sql Server was found.");
                }
                if (localInstanceName == string.Empty)
                {
                    Server = "localhost";
                }
                else
                {
                    Server = "localhost\\" + localInstanceName;
                }
                WriteVerbose("Server set to " + Server);
            }
            base.BeginProcessing();
        }

        private string FindLocalSqlInstance()
        {
            var key = GetRegistryKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL");
            if (key.ValueCount > 0)
            {
                var localInstanceName = key.GetValueNames()[0];
                //TODO: what is the value if sql server is the default instance? will the value be "default"?
                if (localInstanceName == "Default")
                {
                    return string.Empty;
                }
                return localInstanceName;
            }
            return null;
        }

        public static RegistryKey GetRegistryKey(string keyPath)
        {
            RegistryKey localMachineRegistry
                = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                                          Environment.Is64BitOperatingSystem
                                              ? RegistryView.Registry64
                                              : RegistryView.Registry32);

            return string.IsNullOrEmpty(keyPath)
                ? localMachineRegistry
                : localMachineRegistry.OpenSubKey(keyPath);
        }
    }
}
