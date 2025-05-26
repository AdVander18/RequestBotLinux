using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia.Models;
using MsBox.Avalonia;
using System.Collections.Generic;
using RequestBotLinux.Models;
using MsBox.Avalonia.Dto;
using Avalonia.Layout;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Data.Converters;
using System.Globalization;
using ClosedXML.Excel;
using System.IO;
using System;

namespace RequestBotLinux;

public partial class CabinetsWindow : UserControl
{
    public CabinetsWindow()
    {
        InitializeComponent();
        LoadCabinets();
    }
    private void LoadCabinets()
    {
        treeViewCabinets.Items.Clear();
        var cabinets = App.Database.GetAllCabinets();

        if (cabinets.Count == 0)
        {
            treeViewCabinets.Items.Add(new TreeViewItem
            {
                Header = new TextBlock
                {
                    Text = "�������� �� �������",
                    Foreground = Brushes.White
                }
            });
            return;
        }

        foreach (var cab in cabinets)
        {
            // ������ ������������ �������� cabinetNode
            var cabinetNode = CreateCabinetNode(cab);
            treeViewCabinets.Items.Add(cabinetNode);
        }
    }


    private TreeViewItem CreateCabinetNode(Cabinet cab)
    {
        var cabinetNode = new TreeViewItem
        {
            Tag = new NodeInfo { Type = NodeType.Cabinet, Id = cab.Id },
            Header = CreateCabinetHeader(cab)
        };

        // �������� ���� "����������"
        var employeesNode = new TreeViewItem
        {
            Header = new TextBlock { Text = "����������", Foreground = Brushes.LightSkyBlue },
            Tag = new NodeInfo { Type = NodeType.Employees, ParentId = cab.Id }
        };
        employeesNode.Items.Add(new TreeViewItem()); // ��������� ������� ��� �������
        employeesNode.Expanded += OnEmployeesOrEquipmentExpanded;

        // ����������� ���� ��� ���������� ����������
        var addEmployeeMenuItem = new MenuItem { Header = "�������� ����������" };
        addEmployeeMenuItem.Click += async (sender, e) =>
        {
            await ShowAddEmployeeDialog(cab.Id);
            RefreshEmployeesNode(cab.Id);
        };
        employeesNode.ContextMenu = new ContextMenu
        {
            Items = { addEmployeeMenuItem }
        };

        // �������� ���� "������������"
        var equipmentNode = new TreeViewItem
        {
            Header = new TextBlock { Text = "������������", Foreground = Brushes.LightGreen },
            Tag = new NodeInfo { Type = NodeType.Equipment, ParentId = cab.Id }
        };
        equipmentNode.Items.Add(new TreeViewItem()); // ��������� ������� ��� �������
        equipmentNode.Expanded += OnEmployeesOrEquipmentExpanded;

        // ����������� ���� ��� ���������� ������������
        var addEquipmentMenuItem = new MenuItem { Header = "�������� ������������" };
        addEquipmentMenuItem.Click += async (sender, e) =>
        {
            await ShowAddEquipmentDialog(cab.Id);
            RefreshEquipmentNode(cab.Id);
        };
        equipmentNode.ContextMenu = new ContextMenu
        {
            Items = { addEquipmentMenuItem }
        };

        cabinetNode.Items.Add(employeesNode);
        cabinetNode.Items.Add(equipmentNode);

        return cabinetNode;
    }

    private StackPanel CreateCabinetHeader(Cabinet cab)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
        {
            new TextBlock
            {
                Text = $"������� {cab.Number}",
                Foreground = (IBrush)Application.Current.FindResource("TextControlForeground"),
                FontWeight = FontWeight.Bold
            },
            new TextBlock
            {
                Text = $" ({cab.Description})",
                Foreground = (IBrush)Application.Current.FindResource("TextSecondaryBrush")
            }
        }
        };
    }

    private void OnEmployeesOrEquipmentExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem node && node.Tag is NodeInfo info)
        {
            // ��������� ������ ������ ��� ������ ���������
            if (node.Items.Count == 1 && node.Items[0] is TreeViewItem temp && temp.Header == null)
            {
                switch (info.Type)
                {
                    case NodeType.Employees:
                        LoadEmployees(node, info.ParentId);
                        break;
                    case NodeType.Equipment:
                        LoadEquipment(node, info.ParentId);
                        break;
                }
            }
        }
    }
    public class BoolToObjectConverter : IValueConverter
    {
        public static BoolToObjectConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameters = parameter?.ToString()?.Split('|');
            if (parameters?.Length == 2 && value is bool boolValue)
                return boolValue ? parameters[0] : parameters[1];

            return " ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    private void LoadEmployees(TreeViewItem node, int cabinetId)
    {
        // ��������� ������� ���� ����� ��������� ����� ������
        node.Items.Clear();

        var employees = App.Database.GetEmployeesForCabinet(cabinetId);
        foreach (var emp in employees)
        {
            node.Items.Add(new TreeViewItem
            {
                Header = CreateEmployeeHeader(emp),
                Tag = new NodeInfo { Type = NodeType.Employee, Id = emp.Id, ParentId = cabinetId }
            });
        }

        // ���� ��� ������ - ��������� ��������
        if (employees.Count == 0)
        {
            node.Items.Add(new TreeViewItem
            {
                Header = new TextBlock { Text = "��� �����������", Foreground = Brushes.Gray }
            });
        }
    }

    private void LoadEquipment(TreeViewItem node, int cabinetId)
    {
        // ��������� ������� ���� ����� ��������� ����� ������
        node.Items.Clear();

        var equipment = App.Database.GetEquipmentForCabinet(cabinetId);
        foreach (var eq in equipment)
        {
            node.Items.Add(new TreeViewItem
            {
                Header = CreateEquipmentHeader(eq),
                Tag = new NodeInfo { Type = NodeType.EquipmentItem, Id = eq.Id, ParentId = cabinetId }
            });
        }

        if (equipment.Count == 0)
        {
            node.Items.Add(new TreeViewItem
            {
                Header = new TextBlock { Text = "��� ������������", Foreground = Brushes.Gray }
            });
        }
    }

    private StackPanel CreateEmployeeHeader(Employees emp)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
        {
            new TextBlock
            {
                Text = $"{emp.FirstName} {emp.LastName}",
                Foreground = Brushes.DarkGray 
            },
            new TextBlock
            {
                Text = $" ({emp.Position})",
                Foreground = Brushes.Gray
            }
        }
        };
    }

    private StackPanel CreateEquipmentHeader(Equipment eq)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
        {
            new TextBlock
            {
                Text = $"{eq.Type}: {eq.Model}",
                Foreground = Brushes.White
            }
        }
        };

        if (eq.Type == "���������")
        {
            stack.Children.Add(new TextBlock
            {
                Text = $" ({eq.OS})",
                Foreground = Brushes.LightGray
            });
        }

        return stack;
    }

    private async Task ShowAddEmployeeDialog(int cabinetId)
    {
        var users = App.Database.GetAllUsers();
        var dialog = new Window
        {
            Title = "�������� ����������",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var cmbUser = new ComboBox { ItemsSource = users, DisplayMemberBinding = new Binding("Username") };
        var tbFirstName = new TextBox { Watermark = "���" };
        var tbLastName = new TextBox { Watermark = "�������" };
        var tbPosition = new TextBox { Watermark = "���������" };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "���������" };
        var cancelButton = new Button { Content = "������" };

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(cmbUser);
        panel.Children.Add(tbFirstName);
        panel.Children.Add(tbLastName);
        panel.Children.Add(tbPosition);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var result = await ShowDialog(dialog, saveButton, cancelButton);
        if (result == "���������")
        {
            // ��������� ������
            if (string.IsNullOrWhiteSpace(tbFirstName.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "��� �� ����� ���� ������")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }



            if (string.IsNullOrWhiteSpace(tbLastName.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "������� �� ����� ���� ������")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbPosition.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "��������� �� ����� ���� ������")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            App.Database.AddEmployee(new Employees
            {
                Username = (cmbUser.SelectedItem as Models.User)?.Username ?? "",
                FirstName = tbFirstName.Text,
                LastName = tbLastName.Text,
                Position = tbPosition.Text,
                CabinetId = cabinetId
            });
        }
    }
    private async Task ShowEditEmployeeDialog(int employeeId, int cabinetId)
    {
        var employee = App.Database.GetEmployeeById(employeeId);
        if (employee == null) return;

        var dialog = new Window
        {
            Title = "������������� ����������",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var tbFirstName = new TextBox { Text = employee.FirstName };
        var tbLastName = new TextBox { Text = employee.LastName };
        var tbPosition = new TextBox { Text = employee.Position };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "���������" };
        var cancelButton = new Button { Content = "������" };

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(tbFirstName);
        panel.Children.Add(tbLastName);
        panel.Children.Add(tbPosition);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var result = await ShowDialog(dialog, saveButton, cancelButton);
        if (result == "���������")
        {
            employee.FirstName = tbFirstName.Text;
            employee.LastName = tbLastName.Text;
            employee.Position = tbPosition.Text;
            App.Database.UpdateEmployee(employee);
        }
    }
    private async Task ShowAddEquipmentDialog(int cabinetId)
    {
        var dialog = new Window
        {
            Title = "�������� ������������",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var cmbType = new ComboBox
        {
            ItemsSource = new List<string> { "���������", "�������", "�������", "������" },
            SelectedIndex = 0
        };
        var tbModel = new TextBox { Watermark = "������" };
        var tbOS = new TextBox { Watermark = "��", IsVisible = false };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "���������" };
        var cancelButton = new Button { Content = "������" };

        cmbType.SelectionChanged += (s, e) =>
            tbOS.IsVisible = cmbType.SelectedItem?.ToString() == "���������";

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(cmbType);
        panel.Children.Add(tbModel);
        panel.Children.Add(tbOS);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var result = await ShowDialog(dialog, saveButton, cancelButton);
        if (result == "���������")
        {
            // ��������� ������
            if (string.IsNullOrWhiteSpace(tbModel.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "������ �� ����� ���� ������")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            if (cmbType.SelectedItem?.ToString() == "���������" && string.IsNullOrWhiteSpace(tbOS.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "��� ���������� ���������� ������� ��")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            App.Database.AddEquipment(new Equipment
            {
                Type = cmbType.SelectedItem?.ToString() ?? "",
                Model = tbModel.Text,
                OS = tbOS.Text,
                CabinetId = cabinetId
            });
        }
    }
    private async Task ShowEditEquipmentDialog(int equipmentId, int cabinetId)
    {
        var equipment = App.Database.GetEquipmentById(equipmentId);
        if (equipment == null) return;

        var dialog = new Window
        {
            Title = "������������� ������������",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var cmbType = new ComboBox
        {
            ItemsSource = new List<string> { "���������", "�������", "�������", "������" },
            SelectedItem = equipment.Type
        };
        var tbModel = new TextBox { Text = equipment.Model };
        var tbOS = new TextBox { Text = equipment.OS, IsVisible = equipment.Type == "���������" };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "���������" };
        var cancelButton = new Button { Content = "������" };

        cmbType.SelectionChanged += (s, e) =>
            tbOS.IsVisible = cmbType.SelectedItem?.ToString() == "���������";

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(cmbType);
        panel.Children.Add(tbModel);
        panel.Children.Add(tbOS);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var result = await ShowDialog(dialog, saveButton, cancelButton);
        if (result == "���������")
        {
            equipment.Type = cmbType.SelectedItem?.ToString() ?? "";
            equipment.Model = tbModel.Text;
            equipment.OS = tbOS.Text;
            App.Database.UpdateEquipment(equipment);
        }
    }
    private async Task<string> ShowDialog(Window dialog, Button saveButton, Button cancelButton)
    {
        var tcs = new TaskCompletionSource<string>();

        saveButton.Click += (s, e) =>
        {
            tcs.TrySetResult("���������");
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            tcs.TrySetResult("������");
            dialog.Close();
        };

        dialog.ShowDialog((Window)VisualRoot);
        return await tcs.Task;
    }


    private async void OnAddCabinetClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "�������� �������",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var tbNumber = new TextBox { Watermark = "����� ��������" };
        var tbDescription = new TextBox { Watermark = "�������� ��������" };
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "���������" };
        var cancelButton = new Button { Content = "������" };

        buttonsPanel.Children.Add(saveButton);
        buttonsPanel.Children.Add(cancelButton);
        panel.Children.Add(tbNumber);
        panel.Children.Add(tbDescription);
        panel.Children.Add(buttonsPanel);
        dialog.Content = panel;

        var tcs = new TaskCompletionSource<string>();

        saveButton.Click += async (s, e) =>
        {
            // ��������� ������ ��������
            if (string.IsNullOrWhiteSpace(tbNumber.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "����� �������� �� ����� ���� ������")
                    .ShowAsync();
                return;
            }

            if (!int.TryParse(tbNumber.Text, out _))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "����� �������� ������ ���� ������")
                    .ShowAsync();
                return;
            }

            // ��������� ��������
            if (string.IsNullOrWhiteSpace(tbDescription.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "�������� �������� �� ����� ���� ������")
                    .ShowAsync();
                return;
            }

            tcs.TrySetResult("���������");
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            tcs.TrySetResult("������");
            dialog.Close();
        };

        dialog.ShowDialog((Window)this.VisualRoot);
        var result = await tcs.Task;

        if (result == "���������")
        {
            App.Database.AddCabinet(new Cabinet
            {
                Number = tbNumber.Text,
                Description = tbDescription.Text
            });
            LoadCabinets();
        }
    }

    private async void OnEditCabinetClick(object sender, RoutedEventArgs e)
    {
        var selected = treeViewCabinets.SelectedItem as TreeViewItem;
        if (selected?.Tag is not NodeInfo info || info.Type != NodeType.Cabinet) return;

        var cabinet = App.Database.GetCabinetById(info.Id);

        var dialog = new Window
        {
            Title = "������������� �������",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var tbNumber = new TextBox { Text = cabinet.Number };
        var tbDescription = new TextBox { Text = cabinet.Description };
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "���������" };
        var cancelButton = new Button { Content = "������" };

        buttonsPanel.Children.Add(saveButton);
        buttonsPanel.Children.Add(cancelButton);
        panel.Children.Add(tbNumber);
        panel.Children.Add(tbDescription);
        panel.Children.Add(buttonsPanel);
        dialog.Content = panel;

        var tcs = new TaskCompletionSource<string>();

        saveButton.Click += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(tbNumber.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "����� �������� �� ����� ���� ������")
                    .ShowAsync();
                return;
            }

            if (!int.TryParse(tbNumber.Text, out _))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "����� �������� ������ ���� ������")
                    .ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(tbDescription.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("������", "�������� �������� �� ����� ���� ������")
                    .ShowAsync();
                return;
            }

            tcs.TrySetResult("���������");
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            tcs.TrySetResult("������");
            dialog.Close();
        };

        dialog.ShowDialog((Window)this.VisualRoot);
        var result = await tcs.Task;

        if (result == "���������")
        {
            cabinet.Number = tbNumber.Text;
            cabinet.Description = tbDescription.Text;
            App.Database.UpdateCabinet(cabinet);
            LoadCabinets();
        }
    }

    private async void OnTreeViewDoubleTapped(object sender, RoutedEventArgs e)
    {
        if (treeViewCabinets.SelectedItem is not TreeViewItem selectedItem ||
        selectedItem.Tag is not NodeInfo info) return;

        // ���������� ������� ���� �� ����� "����������" � "������������"
        if (info.Type == NodeType.Employees || info.Type == NodeType.Equipment)
            return;

        switch (info.Type)
        {
            case NodeType.Employees:
                var users = App.Database.GetUsersForCabinet(info.ParentId);

                var empDialog = new Window
                {
                    Title = "����������",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var empPanel = new StackPanel { Spacing = 10 };
                var cmbUsername = new ComboBox { ItemsSource = users, SelectedIndex = 0 };
                var tbFirstName = new TextBox { Watermark = "���" };
                var tbLastName = new TextBox { Watermark = "�������" };
                var tbPosition = new TextBox { Watermark = "���������" };
                var empButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var empSave = new Button { Content = "���������" };
                var empCancel = new Button { Content = "������" };

                empButtons.Children.Add(empSave);
                empButtons.Children.Add(empCancel);
                empPanel.Children.Add(cmbUsername);
                empPanel.Children.Add(tbFirstName);
                empPanel.Children.Add(tbLastName);
                empPanel.Children.Add(tbPosition);
                empPanel.Children.Add(empButtons);
                empDialog.Content = empPanel;

                var tcs1 = new TaskCompletionSource<string>();

                empSave.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(tbFirstName.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("������", "��� �� ����� ���� ������")
                            .ShowAsync();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(tbLastName.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("������", "������� �� ����� ���� ������")
                            .ShowAsync();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(tbPosition.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("������", "��������� �� ����� ���� ������")
                            .ShowAsync();
                        return;
                    }

                    tcs1.TrySetResult("���������");
                    empDialog.Close();
                };

                empCancel.Click += (s, e) =>
                {
                    tcs1.TrySetResult("������");
                    empDialog.Close();
                };

                empDialog.ShowDialog((Window)this.VisualRoot);
                var empResult = await tcs1.Task;

                if (empResult == "���������")
                {
                    App.Database.AddEmployee(new Employees
                    {
                        Username = cmbUsername.SelectedItem?.ToString() ?? "",
                        FirstName = tbFirstName.Text,
                        LastName = tbLastName.Text,
                        Position = tbPosition.Text,
                        CabinetId = info.ParentId
                    });
                    LoadCabinets();
                }
                await ShowAddEmployeeDialog(info.ParentId);
                RefreshEmployeesNode(info.ParentId);
                break;

            case NodeType.Equipment:
                var eqDialog = new Window
                {
                    Title = "������������",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var eqPanel = new StackPanel { Spacing = 10 };
                var cmbType = new ComboBox
                {
                    ItemsSource = new List<string> { "���������", "�������", "�������", "������" },
                    SelectedIndex = 0
                };
                var tbModel = new TextBox { Watermark = "������" };
                var tbOS = new TextBox { Watermark = "��", IsVisible = false };
                var eqButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var eqSave = new Button { Content = "���������" };
                var eqCancel = new Button { Content = "������" };

                cmbType.SelectionChanged += (s, args) =>
                    tbOS.IsVisible = cmbType.SelectedItem?.ToString() == "���������";

                eqButtons.Children.Add(eqSave);
                eqButtons.Children.Add(eqCancel);
                eqPanel.Children.Add(cmbType);
                eqPanel.Children.Add(tbModel);
                eqPanel.Children.Add(tbOS);
                eqPanel.Children.Add(eqButtons);
                eqDialog.Content = eqPanel;

                var tcs = new TaskCompletionSource<string>();

                eqSave.Click += async (s, e) =>
                {
                    // ��������� ������ ������������
                    if (string.IsNullOrWhiteSpace(tbModel.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("������", "������ �� ����� ���� ������")
                            .ShowAsync();
                        return;
                    }

                    if (cmbType.SelectedItem?.ToString() == "���������" &&
                        string.IsNullOrWhiteSpace(tbOS.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("������",
                            "��� ���������� ���������� ������� ��")
                            .ShowAsync();
                        return;
                    }

                    tcs.TrySetResult("���������");
                    eqDialog.Close();
                };

                eqCancel.Click += (s, e) =>
                {
                    tcs.TrySetResult("������");
                    eqDialog.Close();
                };

                eqDialog.ShowDialog((Window)this.VisualRoot);
                var eqResult = await tcs.Task;

                if (eqResult == "���������")
                {
                    App.Database.AddEquipment(new Equipment
                    {
                        Type = cmbType.SelectedItem?.ToString() ?? "",
                        Model = tbModel.Text,
                        OS = tbOS.Text,
                        CabinetId = info.ParentId
                    });
                    LoadCabinets();
                }
                await ShowAddEquipmentDialog(info.ParentId);
                RefreshEquipmentNode(info.ParentId);
                break;
        }
    }

    private void RefreshEmployeesNode(int cabinetId)
    {
        foreach (TreeViewItem cabinetNode in treeViewCabinets.Items)
        {
            if (cabinetNode.Tag is NodeInfo cabInfo &&
                cabInfo.Type == NodeType.Cabinet &&
                cabInfo.Id == cabinetId)
            {
                foreach (TreeViewItem childNode in cabinetNode.Items)
                {
                    if (childNode.Tag is NodeInfo childInfo &&
                        childInfo.Type == NodeType.Employees)
                    {
                        LoadEmployees(childNode, cabinetId);
                        return;
                    }
                }
            }
        }
    }

    private void RefreshEquipmentNode(int cabinetId)
    {
        foreach (TreeViewItem cabinetNode in treeViewCabinets.Items)
        {
            if (cabinetNode.Tag is NodeInfo cabInfo &&
                cabInfo.Type == NodeType.Cabinet &&
                cabInfo.Id == cabinetId)
            {
                foreach (TreeViewItem childNode in cabinetNode.Items)
                {
                    if (childNode.Tag is NodeInfo childInfo &&
                        childInfo.Type == NodeType.Equipment)
                    {
                        LoadEquipment(childNode, cabinetId);
                        return;
                    }
                }
            }
        }
    }

    private async void OnExportToExcelClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var cabinets = App.Database.GetAllCabinets();
            if (cabinets.Count == 0)
            {
                await MessageBoxManager.GetMessageBoxStandard("����������", "��� ������ ��� ��������")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("��������");
                int row = 1;

                // ���������
                worksheet.Cell(row, 1).Value = "����� ��������";
                worksheet.Cell(row, 2).Value = "�������� ��������";
                worksheet.Cell(row, 3).Value = "���";
                worksheet.Cell(row, 4).Value = "���/��� ������������";
                worksheet.Cell(row, 5).Value = "���������/������";
                worksheet.Cell(row, 6).Value = "��";

                // ���������� ����������
                var headerRange = worksheet.Range(row, 1, row, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;

                // ������������� ���������
                worksheet.SheetView.FreezeRows(1);

                // �����������
                worksheet.Range(row, 1, row, 6).SetAutoFilter();

                row++;


                foreach (var cab in cabinets)
                {
                    // ������ ��� ��������
                    worksheet.Cell(row, 1).Value = cab.Number;
                    worksheet.Cell(row, 2).Value = cab.Description;
                    worksheet.Cell(row, 3).Value = "�������";
                    row++;

                    // ����������
                    var employees = App.Database.GetEmployeesForCabinet(cab.Id);
                    foreach (var emp in employees)
                    {
                        worksheet.Cell(row, 1).Value = cab.Number;
                        worksheet.Cell(row, 2).Value = cab.Description;
                        worksheet.Cell(row, 3).Value = "���������";
                        worksheet.Cell(row, 4).Value = $"{emp.FirstName} {emp.LastName}";
                        worksheet.Cell(row, 5).Value = emp.Position;
                        row++;
                    }

                    // ������������
                    var equipment = App.Database.GetEquipmentForCabinet(cab.Id);
                    foreach (var eq in equipment)
                    {
                        worksheet.Cell(row, 1).Value = cab.Number;
                        worksheet.Cell(row, 2).Value = cab.Description;
                        worksheet.Cell(row, 3).Value = "������������";
                        worksheet.Cell(row, 4).Value = eq.Type;
                        worksheet.Cell(row, 5).Value = eq.Model;
                        worksheet.Cell(row, 6).Value = eq.OS ?? string.Empty;
                        row++;
                    }

                    // ������ ������ ��� ����������
                    row++;
                }

                // ������������ ������ �������
                worksheet.Columns().AdjustToContents();

                // ������ ����������
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "��������� Excel ����";
                saveFileDialog.Filters.Add(new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx" } });

                var filePath = await saveFileDialog.ShowAsync((Window)VisualRoot);

                if (!string.IsNullOrEmpty(filePath))
                {
                    workbook.SaveAs(filePath);
                    await MessageBoxManager.GetMessageBoxStandard("�����", "������ �������������� � Excel!")
                        .ShowWindowDialogAsync((Window)VisualRoot);
                }
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("������", $"������ ��������: {ex.Message}")
                .ShowWindowDialogAsync((Window)VisualRoot);
        }
    }

    public class NodeInfo
    {
        public NodeType Type { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
    }

    public enum NodeType
    {
        Cabinet,
        Employees,
        Equipment,
        Employee,
        EquipmentItem
    }
}