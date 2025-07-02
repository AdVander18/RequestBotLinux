using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RequestBotLinux.ViewModels
{
    public class MainFormViewModel : INotifyPropertyChanged
    {
        private string _messagesText = "";
        public string MessagesText
        {
            get => _messagesText;
            set
            {
                _messagesText = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Users { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

}
