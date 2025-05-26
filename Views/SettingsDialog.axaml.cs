using System;
using System.Data.Entity;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MsBox.Avalonia.Enums;

namespace RequestBotLinux;

public partial class SettingsDialog : UserControl
{
    private readonly SettingsData _currentSettings;
    private readonly DataBase _database;


    public SettingsDialog(DataBase database)
    {
        InitializeComponent();
        _database = database;
        _currentSettings = SettingsManager.LoadSettings();

        ThemeSlider.ValueChanged -= OnThemeChanged;
        ThemeSlider.Value = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark ? 1 : 0;
        ThemeSlider.ValueChanged += OnThemeChanged;

        // ��������� ����������� �����
        BotTokenInput.Text = _currentSettings.BotToken;
    }


    private void OnThemeChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var theme = Application.Current.RequestedThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        if (Application.Current is App app)
        {
            app.RequestedThemeVariant = theme;
            app.SetTheme(theme);
        }
    }
    private async void OnSaveDataClicked(object sender, RoutedEventArgs e)
    {
        _currentSettings.BotToken = BotTokenInput.Text;
        var token = BotTokenInput.Text;
        if (string.IsNullOrWhiteSpace(token))
        {
            var errorBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                "������",
                "����� �� ����� ���� ������",
                ButtonEnum.Ok,
                Icon.Error);
            await errorBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
            return;
        }

        SettingsManager.SaveSettings(_currentSettings);
        var successBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
            "�����",
            "��������� ���������",
            ButtonEnum.Ok,
            Icon.Success);
        await successBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
    }

    private async void OnDeleteDataClicked(object sender, RoutedEventArgs e)
    {
        var messageBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
            new MsBox.Avalonia.Dto.MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentTitle = "������������� ��������",
                ContentMessage = "�� �������, ��� ������ ������� ��� ������?",
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });
        var result = await messageBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);

        if (result == ButtonResult.Yes)
        {
            try
            {
                _database.DeleteDatabase();
                var successBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                    "�����",
                    "��� ������ ������� �������.",
                    ButtonEnum.Ok,
                    Icon.Success);
                await successBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
            }
            catch (Exception ex)
            {
                var errorBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                    "������",
                    $"������ ��� �������� ������: {ex.Message}",
                    ButtonEnum.Ok,
                    Icon.Error);
                await errorBox.ShowWindowDialogAsync(TopLevel.GetTopLevel(this) as Window);
            }
        }
    }
}