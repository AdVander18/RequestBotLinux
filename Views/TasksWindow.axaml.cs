using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RequestBotLinux.Models;

namespace RequestBotLinux;

public partial class TasksWindow : UserControl
{
    public TasksWindow()
    {
        InitializeComponent();
        LoadTasks();

        // �������� �� �������
        listViewTasks.DoubleTapped += OnTaskDoubleTapped;
        listViewTasks.SelectionChanged += OnTaskSelected;
        btnDeleteTask.Click += BtnDeleteTask_Click;
    }

    private void LoadTasks()
    {
        listViewTasks.Items.Clear();
        var tasks = App.Database.GetAllTasks();

        foreach (var task in tasks)
        {
            var stack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                Tag = task.Id
            };

            // �������� ��������� ����� (������/�������/����������)
            var mainIndicator = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = GetIndicatorBrush(task), // ������ ��������� � ������ � �����
                Margin = new Thickness(0, 0, 5, 0)
            };

            var textBlock = new TextBlock
            {
                Text = task.MessageText,
                Foreground = GetTextColor(task)
            };

            stack.Children.Add(mainIndicator);
            stack.Children.Add(textBlock);

            listViewTasks.Items.Add(stack);
        }
    }



    // ����������� ����� ����������
    private IBrush GetIndicatorBrush(TaskData task)
    {
        // ��������� ������� "���������"
        if (task.Status == "���������")
            return Brushes.Green;

        // �������� ���������
        var totalDuration = task.Deadline - task.Timestamp;
        var remainingTime = task.Deadline - DateTime.Now;

        if (remainingTime.TotalSeconds < 0 ||
            (totalDuration.TotalSeconds > 0 &&
             remainingTime.TotalSeconds / totalDuration.TotalSeconds < 0.1))
        {
            return Brushes.Red;
        }

        return Brushes.Transparent;
    }



    private void OnTaskDoubleTapped(object sender, RoutedEventArgs e)
    {
        var source = e.Source as Control;
        if (source == null) return;

        var listBoxItem = source.FindAncestorOfType<ListBoxItem>();
        if (listBoxItem != null && listBoxItem.Content is StackPanel stackPanel)
        {
            int taskId = (int)stackPanel.Tag;
            var task = App.Database.GetAllTasks().FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                // ����������� ������
                var newStatus = task.Status == "���������" ? "� ������" : "���������";
                App.Database.UpdateTaskStatus(taskId, newStatus);
                LoadTasks(); // ��������� ������
            }
        }
    }

    // ���� ������ ��� ������� �����
    private IBrush GetTextColor(TaskData task)
    {
        var totalDuration = task.Deadline - task.Timestamp;
        var remainingTime = task.Deadline - DateTime.Now;

        if (remainingTime.TotalSeconds < 0 ||
            (remainingTime.TotalSeconds / totalDuration.TotalSeconds) < 0.1)
            return Brushes.Red;

        return Brushes.White;
    }

    private void OnTaskSelected(object sender, SelectionChangedEventArgs e)
    {
        if (listViewTasks.SelectedIndex == -1) return;

        var selectedTask = App.Database.GetAllTasks()[listViewTasks.SelectedIndex];
        var info = $"ID: {selectedTask.Id}\n" +
            $"������������: {selectedTask.Username}\n" +
            $"���: {selectedTask.FirstName}\n" +
            $"�������: {selectedTask.LastName}\n" +
            $"�������: {selectedTask.CabinetNumber}\n" +
            $"��������: {selectedTask.MessageText}\n" +
            $"������: {selectedTask.Status}\n" +
            $"���� �������� ������: {selectedTask.Timestamp:dd.MM.yyyy HH:mm}\n" +
            $"�������: {selectedTask.Deadline:dd.MM.yyyy HH:mm}";

        // ��������� ��������������, ���� ������ �������
        var totalDuration = selectedTask.Deadline - selectedTask.Timestamp;
        var remainingTime = selectedTask.Deadline - DateTime.Now;
        if (remainingTime.TotalSeconds > 0 &&
            (remainingTime.TotalSeconds / totalDuration.TotalSeconds) < 0.1)
        {
            info += "\n��������! �� �������� �������� ����� 10% �������!";
        }

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