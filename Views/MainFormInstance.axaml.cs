using System;
using System.Collections.Generic;
using System.Data.SQLite;    // ��� SQLite
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using RequestBotLinux.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums; // ��� TimePeriodDialog
using System.Diagnostics;
using Avalonia.Input;


namespace RequestBotLinux;

public partial class MainFormInstance : UserControl
{
    public MainFormInstance()
    {
        InitializeComponent();
        listViewUsers.SelectionChanged += OnUserSelected;
    }
    private void OnUserSelected(object sender, SelectionChangedEventArgs e)
    {
        if (listViewUsers.SelectedItem is string selectedUser)
        {
            var messages = App.Database.GetMessagesByUsername(selectedUser)
                .OrderBy(t => t.Timestamp)
                .Select(t => t.IsFromAdmin
                    ? $"[{t.Timestamp:yyyy-MM-dd HH:mm}] [��] {t.MessageText}"
                    : $"[{t.Timestamp:yyyy-MM-dd HH:mm}] {t.MessageText}");

            UpdateMessages(string.Join(Environment.NewLine, messages));
        }
    }
    public void UpdateMessages(string messages)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // ��������� ����� ���������, � �� �������� ��
            textBoxMessages.Text = messages;
            scrollViewer.ScrollToEnd();
        });
    }
    public void UpdateUsers(IEnumerable<string> users)
    {
        listViewUsers.ItemsSource = users;
    }
    public void AppendMessage(string message)
    {
        textBoxMessages.Text += $"\n{message}";
    }
    private async void OnSendMessageClick(object sender, RoutedEventArgs e)
    {
        var message = tbSendMessage.Text?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (listViewUsers.SelectedItem is not string selectedUser)
        {
            AppendMessage("[������] �������� ������������!");
            return;
        }

        try
        {
            var chatId = App.Database.GetChatIdByUsername(selectedUser);
            if (chatId == 0)
            {
                AppendMessage("[������] �� ������� ����� chatId");
                return;
            }

            // ���������� � ���� ������ ����� ��������� (�����������)
            await App.Database.AddOutgoingMessageAsync(
                username: selectedUser,
                chatId: chatId,
                messageText: message);

            // �������� ����� Telegram API
            await App.BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: message);

            tbSendMessage.Text = string.Empty;

            // ��������� ���������
            var messages = App.Database.GetMessagesByUsername(selectedUser)
                .OrderBy(t => t.Timestamp)
                .Select(t => t.IsFromAdmin
                    ? $"[{t.Timestamp:yyyy-MM-dd HH:mm}] [��] {t.MessageText}"
                    : $"[{t.Timestamp:yyyy-MM-dd HH:mm}] {t.MessageText}");

            UpdateMessages(string.Join(Environment.NewLine, messages));
        }
        catch (Exception ex)
        {
            AppendMessage($"[������] {ex.Message}");
            // ���� ��������� ���� ���������, �� �� ����������, ����� �������� ������
        }
    }
    private async void OnDeleteButtonClick(object sender, RoutedEventArgs e)
    {
        if (listViewUsers.SelectedItem is not string selectedUser)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                title: "������",
                text: "�������� ������������!",
                ButtonEnum.Ok,
                icon: Icon.Warning
            );
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
            return;
        }

        var dialog = new TimePeriodDialog();
        var mainWindow = (Window)this.VisualRoot;
        var result = await dialog.ShowDialog<TimeSpan?>(mainWindow);

        if (result.HasValue)
        {
            // ��������� �������� �� ����������� ������
            if (result.Value.TotalSeconds <= 0)
            {
                var errorBox = MessageBoxManager.GetMessageBoxStandard(
                "������",
                "������������ ������ ��� ��������",
                ButtonEnum.Ok,
                Icon.Error
            );
                await errorBox.ShowWindowDialogAsync(mainWindow);
                return;
            }

            try
            {
                var deletedCount = App.Database.DeleteMessagesByPeriod(selectedUser, result.Value);

                // �������� ���������
                Debug.WriteLine($"Deleted {deletedCount} messages older than {result.Value}");

                // ��������� ���������
                var messages = App.Database.GetMessagesByUsername(selectedUser)
                    .OrderBy(t => t.Timestamp)
                    .Select(t => $"[{t.Timestamp:yyyy-MM-dd HH:mm}] {(t.IsFromAdmin ? "[��] " : "")}{t.MessageText}");
                // ����������� �� �������� ��������
                var successBox = MessageBoxManager.GetMessageBoxStandard(
                    "�����",
                    $"������� ���������: {deletedCount}",
                    ButtonEnum.Ok,
                    Icon.Info
                );
                await successBox.ShowWindowDialogAsync(mainWindow);
                UpdateMessages(string.Join(Environment.NewLine, messages));

            }
            catch (Exception ex)
            {
                // �������� ������
                Debug.WriteLine($"Deletion failed: {ex}");
                var errorBox = MessageBoxManager.GetMessageBoxStandard(
                    "������",
                    $"������ ��������: {ex.Message}",
                    ButtonEnum.Ok,
                    Icon.Error
                );
                await errorBox.ShowWindowDialogAsync(mainWindow);
            }
        }
    }
    private void OnSendMessageKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                if (sender is TextBox textBox)
                {
                    // ������� ����� ������ ������ ���� TextBox ��������
                    var text = textBox.Text ?? "";
                    var caretIndex = textBox.CaretIndex;

                    // ��������� ����� ������
                    textBox.Text = text.Insert(
                        Math.Clamp(caretIndex, 0, text.Length),
                        Environment.NewLine
                    );

                    // ��������� ������� �������
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                    e.Handled = true;
                }
            }
            else if (e.KeyModifiers == KeyModifiers.None)
            {
                OnSendMessageClick(sender, null);
                e.Handled = true;
            }
        }
    }
}