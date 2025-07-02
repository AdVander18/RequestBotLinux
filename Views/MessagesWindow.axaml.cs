using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace RequestBotLinux;

public partial class MessagesWindow : UserControl
{
    public MessagesWindow()
    {
        InitializeComponent();
        LoadMessages();
    }
    private void LoadMessages()
    {
        var messages = App.Database.GetAllMessages()
            .OrderByDescending(m => m.Timestamp);

        MessagesContainer.Children.Clear();

        foreach (var msg in messages)
        {
            var messageBlock = new StackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var header = new TextBlock
            {
                Text = $"[{msg.Timestamp:dd.MM.yyyy HH:mm}] {(msg.IsFromAdmin ? "[ADMIN]" : "")} {msg.Username}",
                Foreground = Avalonia.Media.Brushes.Gray
            };

            var content = new TextBlock
            {
                Text = msg.MessageText,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            messageBlock.Children.Add(header);
            messageBlock.Children.Add(content);

            MessagesContainer.Children.Add(messageBlock);
        }
    }

}