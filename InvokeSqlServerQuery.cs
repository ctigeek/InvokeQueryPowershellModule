using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Management.Automation;

namespace InvokeQuery
{
    [Cmdlet("Invoke", "SqlServerQuery", SupportsTransactions = true, SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
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
            string localInstanceName = string.Empty;
            try
            {
                if (IsWindows)
                {
                    var key = GetRegistryKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\Instance Names\\SQL");
                    using (key)
                    {
                        if (key.ValueCount > 0)
                        {
                            localInstanceName = key.GetValueNames()[0];
                            if (localInstanceName.ToLower() == "default" || localInstanceName.ToLower() == "mssqlserver") localInstanceName = string.Empty;
                        }
                    }
                }
            }
            catch
            {
            }
            return localInstanceName;
        }

        private static Microsoft.Win32.RegistryKey GetRegistryKey(string keyPath)
        {
            var localMachineRegistry
                = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
                                          Environment.Is64BitOperatingSystem
                                              ? Microsoft.Win32.RegistryView.Registry64
                                              : Microsoft.Win32.RegistryView.Registry32);

            return string.IsNullOrEmpty(keyPath)
                ? localMachineRegistry
                : localMachineRegistry.OpenSubKey(keyPath);
        }
    }
}
