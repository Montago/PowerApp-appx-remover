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

        public ObservableCollection<ListItem> AppxPackages { get; set; } = new ObservableCollection<ListItem>();

        public ObservableCollection<string> Kinds { get; set; } = new ObservableCollection<string>();

        public AppxFilter Filter { get; set; } = new AppxFilter();

        public AppxList()
        {
            InitializeComponent();

            Kinds.Add("*");

            GetPackages();

            (Filter as INotifyPropertyChanged).PropertyChanged += AppxList_PropertyChanged;
        }

        private void AppxList_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AllUsers")
            {
                GetPackages();
            }
            else
            {
                var sorting = (CollectionViewSource)gbRoot.Resources["SortedItems"];
                sorting.View.Refresh();
            }
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

        private void GetPackages()
        {
            AppxPackages.Clear();

            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = true;
                });

                var allusers = Filter.AllUsers ? " -AllUsers" : "";

                try
                {
                    var Packages = PowerShellWrapper.RunCommand($"Get-AppxPackage{allusers}")
                        ?? throw new Exception("No packages found.");

                    foreach (var package in Packages)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (!Kinds.Contains((package as dynamic).SignatureKind as string ?? ""))
                            {
                                Kinds.Add((package as dynamic).SignatureKind as string ?? "");
                            }

                            var item = new ListItem { Package = package };

                            item.PropertyChanged += (s, e) => CommandManager.InvalidateRequerySuggested();

                            AppxPackages.Add(item);
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                });
            });
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
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

    public class ListItem : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Uninstall INPC Property
        private bool _uninstall;

        public bool Uninstall
        {
            get { return _uninstall; }
            set { _uninstall = value; OnPropertyChanged("Uninstall"); }
        }
        #endregion

        #region Package INPC Property
        private PSObject? _package;

        public PSObject? Package
        {
            get { return _package; }
            set { _package = value; OnPropertyChanged("Package"); }
        }
        #endregion
    }

    public class AppxFilter : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Removable INPC Property
        private bool _removable;

        public bool Removable
        {
            get { return _removable; }
            set { _removable = value; OnPropertyChanged("Removable"); }
        }
        #endregion

        #region Kind INPC Property
        private string _kind = "*";

        public string Kind
        {
            get { return _kind; }
            set { _kind = value; OnPropertyChanged("Kind"); }
        }
        #endregion

        #region TextFilter INPC Property
        private string? _textfilter;

        public string? TextFilter
        {
            get { return _textfilter; }
            set { _textfilter = value; OnPropertyChanged("TextFilter"); }
        }
        #endregion

        #region AllUsers INPC Property
        private bool _allusers;

        public bool AllUsers
        {
            get { return _allusers; }
            set { _allusers = value; OnPropertyChanged("AllUsers"); }
        }
        #endregion
    }
}
