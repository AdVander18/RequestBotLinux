using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RequestBotLinux.Models;

namespace RequestBotLinux;

public class TaskStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not TaskData task) return Brushes.Transparent;

        if (task.Status == "Завершено") return Brushes.Green;

        var remaining = task.Deadline - DateTime.Now;
        var total = task.Deadline - task.Timestamp;
        return remaining.TotalSeconds < 0 || (total.TotalSeconds > 0 && remaining.TotalSeconds / total.TotalSeconds < 0.1)
            ? Brushes.Red
            : Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}

public class TaskTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
    {
        if (value is not TaskData task)
            return Application.Current.FindResource("TextControlForeground");

        var remaining = task.Deadline - DateTime.Now;
        var total = task.Deadline - task.Timestamp;

        return remaining.TotalSeconds < 0 ||
              (total.TotalSeconds > 0 && remaining.TotalSeconds / total.TotalSeconds < 0.1)
            ? Brushes.Red
            : Application.Current.FindResource("TextControlForeground");
    }

    public object ConvertBack(object value, Type targetType, object parameter,
                            System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class TasksWindow : UserControl
{
    public ObservableCollection<TaskData> Tasks { get; } = new();
    public TasksWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadTasks();

        listViewTasks.DoubleTapped += OnTaskDoubleTapped;
        listViewTasks.SelectionChanged += OnTaskSelected;
        btnDeleteTask.Click += BtnDeleteTask_Click;
    }


    private void LoadTasks()
    {
        Tasks.Clear();
        var tasks = App.Database.GetAllTasks();

        // Временная проверка
        Debug.WriteLine($"Loaded tasks count: {tasks.Count}");
        foreach (var task in tasks)
        {
            Debug.WriteLine($"Task: {task.MessageText}");
        }

        foreach (var task in tasks) Tasks.Add(task);
    }

    private void OnTaskDoubleTapped(object sender, RoutedEventArgs e)
    {
        if (listViewTasks.SelectedItem is TaskData selectedTask)
        {
            var newStatus = selectedTask.Status == "Завершено" ? "В работе" : "Завершено";
            App.Database.UpdateTaskStatus(selectedTask.Id, newStatus);
            LoadTasks();
        }
    }

    private void OnTaskSelected(object sender, SelectionChangedEventArgs e)
    {
        if (listViewTasks.SelectedItem is not TaskData selectedTask) return;

        var info = $"ID: {selectedTask.Id}\n" +
                   $"Пользователь: {selectedTask.Username}\n" +
                   $"Имя: {selectedTask.FirstName}\n" +
            $"Фамилия: {selectedTask.LastName}\n" +
            $"Кабинет: {selectedTask.CabinetNumber}\n" +
            $"Описание: {selectedTask.MessageText}\n" +
            $"Статус: {selectedTask.Status}\n" +
            $"Дата создания задачи: {selectedTask.Timestamp:dd.MM.yyyy HH:mm}\n" +
                   $"Дедлайн: {selectedTask.Deadline:dd.MM.yyyy HH:mm}";

        textBoxMessages.Text = info;
    }

    private async void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
    {
        // Проверка, выбрана ли задача
        if (listViewTasks.SelectedIndex == -1)
        {
            var errorBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                new MsBox.Avalonia.Dto.MessageBoxStandardParams
                {
                    ContentTitle = "Ошибка",
                    ContentMessage = "Пожалуйста, выберите задачу для удаления.",
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = Icon.Error,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });

            await errorBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
            return;
        }

        var selectedTask = App.Database.GetAllTasks()[listViewTasks.SelectedIndex];

        var messageBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
            new MsBox.Avalonia.Dto.MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentTitle = "Подтверждение удаления",
                ContentMessage = "Вы уверены, что хотите удалить задачу?",
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

        var result = await messageBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);

        if (result == ButtonResult.Yes)
        {
            App.Database.DeleteTask(selectedTask.Id);
            LoadTasks();
            textBoxMessages.Text = string.Empty;
        }
    }
}