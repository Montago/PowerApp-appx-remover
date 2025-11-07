using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace PowerApp.Client
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (!IsRunAsAdministrator())
			{
				// Restart the application with administrator rights
				var processInfo = new ProcessStartInfo
				{
					FileName = Process.GetCurrentProcess().MainModule.FileName!,
					UseShellExecute = true,
					Verb = "runas"
				};

				try
				{
					Process.Start(processInfo);
				}
				catch
				{
					// User refused the elevation
					Shutdown();
					return;
				}

				Shutdown();
				return;
			}
		}

		private static bool IsRunAsAdministrator()
		{
			if (Debugger.IsAttached)
			{
				// If debugging, we assume the user has the necessary permissions
				return true;
			}

			using (var identity = WindowsIdentity.GetCurrent())
			{
				var principal = new WindowsPrincipal(identity);
				return principal.IsInRole(WindowsBuiltInRole.Administrator);
			}
		}
	}
}
