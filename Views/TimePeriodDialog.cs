using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using System;
using Avalonia.Media;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace RequestBotLinux.Views
{
    public class TimePeriodDialog : Window
    {
        private readonly TextBlock _tbSelectedPeriod;

        public TimePeriodDialog()
        {
            this.Width = 150;
            this.Height = 330;
            this.Title = "Выберите период";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stack = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10
            };
            _tbSelectedPeriod = new TextBlock
            {
                Text = "Выберите период удаления:",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.White
            };

            AddButton(stack, "1 день", TimeSpan.FromDays(1));
            AddButton(stack, "1 неделя", TimeSpan.FromDays(7));
            AddButton(stack, "1 месяц", TimeSpan.FromDays(30));
            AddButton(stack, "1 год", TimeSpan.FromDays(365));
            AddCancelButton(stack);
            this.Content = stack;
        }

        private void AddButton(Panel parent, string text, TimeSpan period)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10),
            };

            button.Click += (s, e) =>
            {
                if (period <= TimeSpan.Zero)
                {
                    // Показываем ошибку, если период некорректный
                    var errorBox = MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка",
                        "Некорректный период",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    );
                    errorBox.ShowWindowDialogAsync(this);
                    return;
                }

                Close(period);
            };

            parent.Children.Add(button);
        }

        private void AddCancelButton(Panel parent)
        {
            var button = new Button { Content = "Отмена" };
            button.Margin = new Thickness(0, 5, 0, 5);
            button.Padding = new Thickness(10);
            button.Click += (s, e) => Close(null);
            parent.Children.Add(button);
        }
    }
}