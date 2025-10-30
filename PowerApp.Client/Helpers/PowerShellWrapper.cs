using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerApp.Client.Helpers
{
    public static class PowerShellWrapper
    {
        public static List<PSObject>? RunCommand(string command)
        {
            var sessionState = InitialSessionState.CreateDefault();
            sessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
            string script = "";

            using (PowerShell powershell = PowerShell.Create(sessionState))
            {
                script += "Import-Module -Name Appx -UseWIndowsPowershell;";
                script += command; //"Get-AppxPackage";

                powershell.AddScript(script);

                var results = powershell.Invoke();

                if (powershell.HadErrors)
                {
                    foreach (var error in powershell.Streams.Error)
                    {
                        throw error.Exception;
                    }
                }
                else
                {
                    return results.ToList();
                }
            }

            return null;
        }
    }
}
