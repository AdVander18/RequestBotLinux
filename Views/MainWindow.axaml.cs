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
            App.BotStatusChanged += OnBotStatusChanged;
            App.Database.MessageAdded += () => LoadMessages();
            RefreshMessagesFromDb();
        }

        public void RefreshMessagesFromDb()
        {
            var messages = App.Database.GetAllTasks()
                .Select(t => $"[{t.Username}] {t.Timestamp}: {t.MessageText}");
            if (MainContent.Content is MainFormInstance mainForm)
            {
                mainForm.UpdateMessages(string.Join(Environment.NewLine, messages));
            }
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
        private void TestDatabase()
        {
            var tasks = App.Database.GetAllTasks();
            foreach (var task in tasks)
            {
                Console.WriteLine($"Task {task.Id}: {task.MessageText}");
            }
        }
        public void LoadMessages()
        {
            var messages = App.Database.GetAllTasks();
            var messagesText = string.Join(
                Environment.NewLine,
                messages.Select(t => $"[{t.Username}] {t.Timestamp}: {t.MessageText}")
            );

            if (MainContent.Content is MainFormInstance mainForm)
            {
                mainForm.UpdateMessages(messagesText);
            }
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
            var analyticsWindow = new AnalyticsView();
            MainContent.Content = analyticsWindow;
        }
    }
}