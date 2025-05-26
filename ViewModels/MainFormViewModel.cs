using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
