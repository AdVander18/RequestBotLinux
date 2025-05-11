using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace RequestBotLinux.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            App.Database.MessageAdded += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (MainContent.Content is MainFormInstance mainForm)
                    {
                        LoadMessages(); // јвтообновление при новых сообщени€х
                    }
                });
            };



        }
        private void OnHomeButtonClicked(object sender, RoutedEventArgs e)
        {
            // —оздаем экземпл€р UserControl и помещаем его в ContentControl
            MainContent.Content = new MainFormInstance();
            LoadMessages();
        }
        private void TestDatabase()
        {
            var tasks = App.Database.GetAllTasks();
            foreach (var task in tasks)
            {
                Console.WriteLine($"Task {task.Id}: {task.MessageText}");
            }
        }
        private void LoadMessages()
        {
            var messages = App.Database.GetAllTasks();
            var messagesText = string.Join(
                Environment.NewLine,
                messages.Select(t => $"[{t.Username}] {t.Timestamp}: {t.MessageText}")
            );

            var uniqueUsers = messages.Select(t => t.Username).Distinct().ToList();

            if (MainContent.Content is MainFormInstance mainForm)
            {
                mainForm.UpdateMessages(messagesText);
                mainForm.UpdateUsers(uniqueUsers);
            }
        }
    }
}