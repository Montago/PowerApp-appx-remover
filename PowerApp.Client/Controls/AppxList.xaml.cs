using DebounceThrottle;
using PowerApp.Client.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PowerApp.Client.Controls
{
	// https://mahapps.com/docs/styles/datagrid

	public partial class AppxList : UserControl, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged

		public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion

		#region IsLoading INPC Property
		private bool _IsLoading;

		public bool IsLoading
		{
			get { return _IsLoading; }
			set { _IsLoading = value; OnPropertyChanged(nameof(IsLoading)); }
		}
		#endregion

		public ObservableCollection<ListItem> AppxPackages { get; set; } = [];

		public ObservableCollection<string> Kinds { get; set; } = [];

		public AppxFilter Filter { get; set; } = new AppxFilter();

		private readonly DebounceDispatcher _debounce = new(TimeSpan.FromMicroseconds(100));

		public AppxList()
		{
			InitializeComponent();

			Kinds.Add("*");

			_ = GetPackages();

			(Filter as INotifyPropertyChanged).PropertyChanged += AppxList_PropertyChanged;
		}

		private async void AppxList_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "AllUsers")
			{
				await GetPackages();
			}

			var sorting = (CollectionViewSource)gbRoot.Resources["SortedItems"];
			sorting.View.Refresh();
		}

		public void ResetSorting()
		{
			var sorting = (CollectionViewSource)gbRoot.Resources["SortedItems"];

			sorting.SortDescriptions.RemoveAt(0);
			sorting.SortDescriptions.Add(new SortDescription("ModelNavn", ListSortDirection.Ascending));
		}

		private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
		{
			if (e.Item is ListItem item)
			{
				var package = (item as dynamic).Package;

				e.Accepted = (!Filter.Removable || (package.NonRemovable == false))
					&& (Filter.Kind == "*" || Filter.Kind == null || package.SignatureKind == Filter.Kind)
					&& (String.IsNullOrWhiteSpace(Filter.TextFilter) || (package.Name as string ?? "").Contains(Filter.TextFilter, StringComparison.CurrentCultureIgnoreCase));

			}
		}

		private async Task GetPackages()
		{
			AppxPackages.Clear();

			IsLoading = true;

			var allusers = Filter.AllUsers ? " -AllUsers" : "";

			try
			{
				var packages = await Task.Run(() => PowerShellWrapper.RunCommand($"Get-AppxPackage{allusers}"))
					?? throw new Exception("No packages found.");

				foreach (var package in packages)
				{
					if (!Kinds.Contains((package as dynamic).SignatureKind as string ?? ""))
					{
						Kinds.Add((package as dynamic).SignatureKind as string ?? "");
					}

					var item = new ListItem { Package = package };

					item.PropertyChanged += (o, e) => CommandManager.InvalidateRequerySuggested();

					AppxPackages.Add(item);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			IsLoading = false;
		}

		#region DeleteSelectedCommand
		public ICommand DeleteSelectedCommand => new RelayCommand(ExecuteDeleteSelected, CanDeleteSelected);

		public bool CanDeleteSelected() => AppxPackages.Any(a => a.Uninstall);

		private void ExecuteDeleteSelected(object? obj)
		{
			Dispatcher.Invoke(() =>
			{
				logRow.Height = new GridLength(0);
				tbErrorMessage.Text = "";
			});

			foreach (var item in AppxPackages.Where(w => w.Uninstall).ToList())
			{
				if (item.Package != null && UninstallPackage(item.Package))
				{
					AppxPackages.Remove(item);
				}
			}
		}
		#endregion

		private bool UninstallPackage(PSObject AppxPackage)
		{
			IsLoading = true;
			IsEnabled = false;

			string packageName = (AppxPackage as dynamic).Name;
			var allusers = Filter.AllUsers ? "-AllUsers" : "";

			try
			{
				PowerShellWrapper.RunCommand($"Get-AppxPackage {packageName} {allusers} | Remove-AppxPackage");

				return true;
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(() =>
				{
					logRow.Height = new GridLength(150);
					tbErrorMessage.Text += ex.Message + "\n\n";
				});

				return false;
			}
			finally
			{
				IsLoading = false;
				IsEnabled = true;
			}
		}

		private void CopyInstallLocationToClipboard(object sender, MouseButtonEventArgs e)
		{
			if (sender is TextBlock text)
			{
				Clipboard.SetText(text.Text);
				MessageBox.Show(text.Text + "\n\nCopied to clipboard");
			}
		}


		//private void OpenFileLocation(string filePath)
		//{
		//    if (Directory.Exists(filePath))
		//    {
		//        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
		//    }
		//    else
		//    {
		//        MessageBox.Show("File path not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		//    }
		//}

		//private void DataGridRow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//    if (sender is DataGridRow row && row.DataContext is ListItem item)
		//    {
		//        ContextMenu contextMenu = new ContextMenu();

		//        MenuItem openFileMenuItem = new MenuItem
		//        {
		//            Header = "Open File Location"
		//        };
		//        openFileMenuItem.Click += (s, args) => OpenFileLocation(((dynamic)item.Package).InstallLocation);

		//        contextMenu.Items.Add(openFileMenuItem);
		//        row.ContextMenu = contextMenu;
		//        contextMenu.IsOpen = true;
		//    }
		//}
	}
}
