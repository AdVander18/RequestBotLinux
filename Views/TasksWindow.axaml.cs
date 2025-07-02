using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using MsBox.Avalonia.Enums;
using RequestBotLinux.Models;

namespace RequestBotLinux;

public class TaskStatusToBrushConverter : IValueConverter
{



    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not TaskData task) return Brushes.Transparent;

        if (task.Status == "���������") return Brushes.Green;

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

public partial class TasksWindow : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private string _filterText = string.Empty;
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText != value)
            {
                _filterText = value;
                OnPropertyChanged();
                ApplyFilterAndSorting();
            }
        }
    }

    private int _sortIndex = 0;
    public int SortIndex
    {
        get => _sortIndex;
        set
        {
            if (_sortIndex != value)
            {
                _sortIndex = value;
                OnPropertyChanged();
                ApplyFilterAndSorting();
            }
        }
    }
    public ObservableCollection<TaskData> Tasks { get; } = new();
    private List<TaskData> _allTasks = new List<TaskData>();

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
        _allTasks = App.Database.GetAllTasks();
        ApplyFilterAndSorting();
    }

    private void ApplyFilterAndSorting()
    {
        var filtered = _allTasks.AsEnumerable();

        // ���������� ����������
        if (!string.IsNullOrWhiteSpace(_filterText))
        {
            var filter = _filterText.ToLower();
            filtered = filtered.Where(t =>
                t.MessageText.ToLower().Contains(filter) ||
                t.Username.ToLower().Contains(filter) ||
                t.FirstName.ToLower().Contains(filter) ||
                t.LastName.ToLower().Contains(filter) ||
                t.CabinetNumber.ToLower().Contains(filter)
            );
        }

        // ���������� ����������
        filtered = _sortIndex switch
        {
            0 => filtered.OrderBy(t => t.Deadline),
            1 => filtered.OrderByDescending(t => t.Deadline),
            _ => filtered
        };

        Tasks.Clear();
        foreach (var task in filtered)
        {
            Tasks.Add(task);
        }
    }

    private void OnTaskDoubleTapped(object sender, RoutedEventArgs e)
    {
        if (listViewTasks.SelectedItem is TaskData selectedTask)
        {
            var newStatus = selectedTask.Status == "���������" ? "� ������" : "���������";
            App.Database.UpdateTaskStatus(selectedTask.Id, newStatus);
            LoadTasks();
        }
    }

    private void OnTaskSelected(object sender, SelectionChangedEventArgs e)
    {
        if (listViewTasks.SelectedItem is not TaskData selectedTask) return;

        var info = $"ID: {selectedTask.Id}\n" +
                   $"������������: {selectedTask.Username}\n" +
                   $"���: {selectedTask.FirstName}\n" +
            $"�������: {selectedTask.LastName}\n" +
            $"�������: {selectedTask.CabinetNumber}\n" +
            $"��������: {selectedTask.MessageText}\n" +
            $"������: {selectedTask.Status}\n" +
            $"���� �������� ������: {selectedTask.Timestamp:dd.MM.yyyy HH:mm}\n" +
                   $"�������: {selectedTask.Deadline:dd.MM.yyyy HH:mm}";

        textBoxMessages.Text = info;
    }

    private async void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
    {
        // ��������, ������� �� ������
        if (listViewTasks.SelectedIndex == -1)
        {
            var errorBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                new MsBox.Avalonia.Dto.MessageBoxStandardParams
                {
                    ContentTitle = "������",
                    ContentMessage = "����������, �������� ������ ��� ��������.",
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
                ContentTitle = "������������� ��������",
                ContentMessage = "�� �������, ��� ������ ������� ������?",
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