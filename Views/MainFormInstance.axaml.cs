using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RequestBotLinux;

public partial class MainFormInstance : UserControl
{
    public MainFormInstance()
    {
        InitializeComponent();
    }
    public void UpdateMessages(string messages)
    {
        textBoxMessages.Text = messages;
    }
    public void UpdateUsers(IEnumerable<string> users)
    {
        listViewUsers.ItemsSource = users;
    }
}