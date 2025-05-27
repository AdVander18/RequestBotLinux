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

public partial class SettingsDialog : Window
{
    private SettingsData _currentSettings;
    private DataBase _database;


    public SettingsDialog()
    {
        InitializeComponent();
    }

    public void Initialize(DataBase database)
    {
        _database = database;
        _currentSettings = SettingsManager.LoadSettings();

        ThemeSlider.ValueChanged -= OnThemeChanged;
        ThemeSlider.Value = Application.Current?.RequestedThemeVariant == ThemeVariant.Dark ? 1 : 0;
        ThemeSlider.ValueChanged += OnThemeChanged;

        BotTokenInput.Text = _currentSettings.BotToken;
    }



    private void OnThemeChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (Application.Current == null) return;

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
                "Ошибка",
                "Токен не может быть пустым",
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);
            await errorBox.ShowWindowDialogAsync(this);
            return;
        }

        SettingsManager.SaveSettings(_currentSettings);
        var successBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
            "Успех",
            "Настройки сохранены",
            ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Success);
        await successBox.ShowWindowDialogAsync(this);
    }

    private async void OnDeleteDataClicked(object sender, RoutedEventArgs e)
    {
        var messageBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
            new MsBox.Avalonia.Dto.MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentTitle = "Подтверждение удаления",
                ContentMessage = "Вы уверены, что хотите удалить все данные?",
                Icon = MsBox.Avalonia.Enums.Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });
        var result = await messageBox.ShowWindowDialogAsync(this);

        if (result == ButtonResult.Yes)
        {
            try
            {
                _database.DeleteDatabase();
                var successBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                    "Успех",
                    "Все данные успешно удалены.",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Success);
                await successBox.ShowWindowDialogAsync(this);
            }
            catch (Exception ex)
            {
                var errorBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    $"Ошибка при удалении данных: {ex.Message}",
                    ButtonEnum.Ok,
                    MsBox.Avalonia.Enums.Icon.Error);
                await errorBox.ShowWindowDialogAsync(this);
            }
        }
    }
}