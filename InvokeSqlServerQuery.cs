using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Management.Automation;
using Microsoft.Win32;

namespace InvokeQuery
{
    [Cmdlet("Invoke","SqlServerQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public class InvokeSqlServerQuery : InvokeQueryBase
    {
        protected override DbProviderFactory GetProviderFactory()
        {
            return SqlClientFactory.Instance;
        }

        protected override void ConfigureServerProperty()
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
        }

        protected override void ConfigureConnectionString()
        {
            base.ConfigureConnectionString();
            if (!ConnectionString.Contains("Password="))
            {
                ConnectionString = ConnectionString + "Integrated Security=SSPI;";
            }
        }

        private string FindLocalSqlInstance()
        {
            string localInstanceName = null;
            var key = GetRegistryKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL");
            using (key)
            {
                if (key != null && key.ValueCount > 0)
                {
                    localInstanceName = key.GetValueNames()[0];
                    if (localInstanceName.ToLower() == "default" || localInstanceName.ToLower() == "mssqlserver") localInstanceName = string.Empty;
                }
            }
            return localInstanceName;
        }

        private static RegistryKey GetRegistryKey(string keyPath)
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
