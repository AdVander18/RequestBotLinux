using System;
using System.Data.Entity;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace RequestBotLinux.Views
{
    public partial class MainWindow : Window
    {
        public MainFormInstance MainForm { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            MainForm = mainForm;
            App.BotStatusChanged += OnBotStatusChanged;
            App.Database.MessageAdded += DatabaseUpdated;
        }

        private void DatabaseUpdated()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadUsers();
                LoadMessages();

                if (MainContent.Content is MainFormInstance mainForm)
                {
                    mainForm.RefreshMessages();
                }
            });
        }

        public void RefreshMessagesFromDb()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var messages = App.Database.GetAllTasks()
                    .Select(t => $"[{t.Username}] {t.Timestamp}: {t.MessageText}");
                if (MainContent.Content is MainFormInstance mainForm)
                {
                    mainForm.UpdateMessages(string.Join(Environment.NewLine, messages));
                }
            });
        }


        private void OnBotStatusChanged(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (MainContent.Content is MainFormInstance mainForm)
                {
                    mainForm.AppendMessage(message);
                }
            });
        }

        private async void OnHomeButtonClicked(object sender, RoutedEventArgs e)
        {
            var mainForm = new MainFormInstance();
            MainContent.Content = mainForm;

            // Выполняем проверку подключения
            var isConnected = await App.CheckBotConnectionAsync();
            var statusMessage = isConnected
                ? "[Система] Бот успешно подключен!"
                : "[Ошибка] Не удалось подключиться к боту";

            mainForm.UpdateMessages(statusMessage);

            // Загружаем остальные данные
            LoadMessages();
            LoadUsers();

        }

        public void LoadMessages()
        {
            if (MainContent.Content is MainFormInstance mainForm)
            {
                var selectedUser = mainForm.CurrentUser;
                if (!string.IsNullOrEmpty(selectedUser))
                {
                    var messages = App.Database.GetMessagesByUsername(selectedUser)
                        .OrderBy(t => t.Timestamp)
                        .Select(t => FormatMessage(t));

                    mainForm.UpdateMessages(string.Join(Environment.NewLine, messages));
                }
            }
        }
        private static string FormatMessage(Models.MessageData msg)
        {
            return $"[{msg.Timestamp:HH:mm}] {(msg.IsFromAdmin ? "[Вы] " : "")}{msg.MessageText}";
        }

        public void LoadUsers()
        {
            var users = App.Database.GetUniqueUsers();

            if (MainContent.Content is MainFormInstance mainForm)
            {
                mainForm.UpdateUsers(users);
            }
        }
        private void OnMessagesButtonClicked(object sender, RoutedEventArgs e)
        {
            var messagesWindow = new MessagesWindow();
            MainContent.Content = messagesWindow;
        }
        private void OnTasksButtonClicked(object sender, RoutedEventArgs e)
        {
            var tasksWindow = new TasksWindow();
            MainContent.Content = tasksWindow;
        }
        private void OnCabinetsButtonClicked(object sender, RoutedEventArgs e)
        {
            var cabinetsWindow = new CabinetsWindow();
            MainContent.Content = cabinetsWindow;
        }
        private void OnAnalyticsButtonClicked(object sender, RoutedEventArgs e)
        {
            var analyticsWindow = new AnalyticsView(App.Database);
            MainContent.Content = analyticsWindow;
        }
        private void OnFAQButtonClicked(object sender, RoutedEventArgs e)
        {
            var faqWindow = new FAQWindow();
            MainContent.Content = faqWindow;
        }
        private void OnQRButtonClicked(object sender, RoutedEventArgs e)
        {
            var qrWindow = new QrcodesWindow();
            MainContent.Content = qrWindow;
        }
        private async void OnSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog();
            dialog.Initialize(App.Database);
            await dialog.ShowDialog(this); // Исправлено
        }

    }
}