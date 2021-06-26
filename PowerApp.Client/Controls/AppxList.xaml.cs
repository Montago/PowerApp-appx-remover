using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PostSharp.Patterns.Model;
using PostSharp.Patterns.Xaml;

namespace PowerApp.Client.Controls
{
    [NotifyPropertyChanged]
    public partial class AppxList : UserControl
    {
        [AggregateAllChanges]
        public ObservableCollection<ListItem> AppxPackages { get; set; } = new ObservableCollection<ListItem>();

        public ObservableCollection<string> Kinds { get; set; } = new ObservableCollection<string>();

        public AppxFilter Filter { get; set; } = new AppxFilter();

        public AppxList()
        {
            InitializeComponent();

            Kinds.Add("*");

            DataContext = this;

            GetPackages();

            (Filter as INotifyPropertyChanged).PropertyChanged += AppxList_PropertyChanged;
        }

        private void AppxList_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
            var item = ((e.Item as ListItem) as dynamic).Package as dynamic;

            e.Accepted = (!Filter.Removable || (item.NonRemovable == false))
                &&
                (Filter.Kind == "*" || Filter.Kind == null || item.SignatureKind == Filter.Kind)
                &&
                (
                    String.IsNullOrWhiteSpace(Filter.TextFilter) ||
                    (item.Name as string).ToLower().Contains(Filter.TextFilter.ToLower())
                );
        }

        private void GetPackages()
        {
            AppxPackages.Clear();

            Task.Run(() =>
            {
                var allusers = Filter.AllUsers ? " -AllUsers" : "";

                var Packages = PowerShellWrapper.RunCommand($"Get-AppxPackage{allusers}");

                foreach (var package in Packages)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!Kinds.Contains((package as dynamic).SignatureKind as string))
                        {
                            Kinds.Add((package as dynamic).SignatureKind as string);
                        }

                        var item = new ListItem { Package = package };

                        (item as INotifyPropertyChanged).PropertyChanged += ListItem_PropertyChanged1;

                        AppxPackages.Add(item);
                    });
                }
            });
        }

        private void ListItem_PropertyChanged1(object sender, PropertyChangedEventArgs e)
        {
            CanExecuteUnInstall = AppxPackages.Any(a => a.Uninstall);
        }

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
                    tbErrorMessage.Text += ex.Message + "\n\n";
                });

                return false;
            }
        }

        public bool CanExecuteUnInstall { get; set; }
        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                tbErrorMessage.Text = "";
            });

            foreach (var item in AppxPackages.Where(w => w.Uninstall).ToList())
            {
                if (UninstallPackage(item.Package))
                {
                    AppxPackages.Remove(item);
                }
            }
        }
    }

    [NotifyPropertyChanged]
    public class ListItem
    {
        public bool Uninstall { get; set; }

        public PSObject Package { get; set; }

    }

    [NotifyPropertyChanged]
    public class AppxFilter
    {
        public bool Removable { get; set; }

        public string Kind { get; set; }

        public string TextFilter { get; set; }

        public bool AllUsers { get; set; }
    }
}
