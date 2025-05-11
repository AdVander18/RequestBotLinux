using System;
using System.IO;
using System.Data.Entity;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RequestBotLinux.ViewModels;
using RequestBotLinux.Views;

namespace RequestBotLinux
{
    public partial class App : Application
    {
        public static DataBase Database { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            base.Initialize();

            // ѕуть к базе данных
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RequestBotLinux",
                "database.db");

            Database = new DataBase(dbPath);

        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = (Exception)e.ExceptionObject;
                new Window { Content = new TextBlock { Text = $"Critical error: {ex.Message}" } }.Show();
            };

        }

    }
}