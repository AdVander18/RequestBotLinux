using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia.Enums;

namespace RequestBotLinux;

public partial class SettingsDialog : UserControl
{
    public SettingsDialog()
    {
        InitializeComponent();
    }
    private void OnThemeChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var theme = e.NewValue == 0 ? "Light" : "Dark";
        // ���������� ����� ����
        Application.Current.Resources.TryGetResource(theme, out var themeResource);
        Application.Current.Resources["SystemTheme"] = themeResource;
    }

    private async void OnDeleteDataClicked(object sender, RoutedEventArgs e)
    {
        // ������ �������� ������
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
            // ����� ������ �������� ������
        }
    }
}