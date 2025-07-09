using System.ComponentModel;

namespace PowerApp.Client.Controls
{
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
            set { _removable = value; OnPropertyChanged(nameof(Removable)); }
        }
        #endregion

        #region Kind INPC Property
        private string _kind = "*";

        public string Kind
        {
            get { return _kind; }
            set { _kind = value; OnPropertyChanged(nameof(Kind)); }
        }
        #endregion

        #region TextFilter INPC Property
        private string? _textfilter;

        public string? TextFilter
        {
            get { return _textfilter; }
            set { _textfilter = value; OnPropertyChanged(nameof(TextFilter)); }
        }
        #endregion

        #region AllUsers INPC Property
        private bool _allusers;

        public bool AllUsers
        {
            get { return _allusers; }
            set { _allusers = value; OnPropertyChanged(nameof(AllUsers)); }
        }
        #endregion
    }
}
