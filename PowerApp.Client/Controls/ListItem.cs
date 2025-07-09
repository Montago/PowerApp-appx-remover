using System.ComponentModel;
using System.Management.Automation;

namespace PowerApp.Client.Controls
{
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
            set { _uninstall = value; OnPropertyChanged(nameof(Uninstall)); }
        }
        #endregion

        #region Package INPC Property
        private PSObject? _package;

        public PSObject? Package
        {
            get { return _package; }
            set { _package = value; OnPropertyChanged(nameof(Package)); }
        }
        #endregion
    }
}
