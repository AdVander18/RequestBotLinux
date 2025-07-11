﻿using System.Collections.Generic;
using ReactiveUI;
using RequestBotLinux.Models;

namespace RequestBotLinux.ViewModels
{
    public class MessagesViewModel : ViewModelBase
    {
        private List<MessageData> _allMessages;
        public List<MessageData> AllMessages
        {
            get => _allMessages;
            set => this.RaiseAndSetIfChanged(ref _allMessages, value);
        }

        public MessagesViewModel()
        {
            LoadMessages();
        }

        private void LoadMessages()
        {
            AllMessages = App.Database.GetAllMessages();
        }

    }
}
