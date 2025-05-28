using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ClosedXML.Excel;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RequestBotLinux.Models;
using Telegram.Bot.Types;

namespace RequestBotLinux;

public partial class CabinetsWindow : UserControl
{
    private string _currentFilter = string.Empty;

    public CabinetsWindow()
    {
        InitializeComponent();
        LoadCabinets();
        FilterTextBox.TextChanged += FilterTextBox_TextChanged;

        treeViewCabinets.KeyDown += TreeViewCabinets_KeyDown;
    }
    private async void TreeViewCabinets_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (treeViewCabinets.SelectedItem is TreeViewItem selectedItem
                && selectedItem.Tag is NodeInfo nodeInfo)
            {
                await DeleteNode(nodeInfo);
            }
        }
    }
    private async Task DeleteNode(NodeInfo nodeInfo)
    {
        string message = "";
        string title = "";

        switch (nodeInfo.Type)
        {
            case NodeType.Employee:
                title = "Удаление сотрудника";
                message = "Вы точно хотите удалить сотрудника?";
                break;

            case NodeType.EquipmentItem:
                title = "Удаление оборудования";
                message = "Вы точно хотите удалить оборудование?";
                break;

            default:
                return; // Неподдерживаемый тип
        }

        var confirmBox = MessageBoxManager.GetMessageBoxStandard(
            new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentTitle = title,
                ContentMessage = message,
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

        var result = await confirmBox.ShowWindowDialogAsync((Window)VisualRoot);

        if (result == ButtonResult.Yes)
        {
            switch (nodeInfo.Type)
            {
                case NodeType.Employee:
                    App.Database.DeleteEmployee(nodeInfo.Id);
                    break;

                case NodeType.EquipmentItem:
                    App.Database.DeleteEquipment(nodeInfo.Id);
                    break;
            }

            LoadCabinets(); // Обновляем список
        }
    }
    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentFilter = FilterTextBox.Text?.Trim() ?? string.Empty;
        LoadCabinets();
    }

    private void ClearFilterClick(object sender, RoutedEventArgs e)
    {
        FilterTextBox.Text = string.Empty;
    }


    private void LoadCabinets()
    {
        treeViewCabinets.Items.Clear();
        var cabinets = App.Database.GetAllCabinets();
        bool hasFilter = !string.IsNullOrWhiteSpace(_currentFilter);

        if (cabinets.Count == 0)
        {
            treeViewCabinets.Items.Add(new TreeViewItem
            {
                Header = new TextBlock
                {
                    Text = "Кабинеты не найдены",
                    Foreground = Brushes.White
                }
            });
            return;
        }

        foreach (var cab in cabinets)
        {
            // Проверка соответствия кабинета фильтру
            bool cabMatches = !hasFilter ||
                ContainsIgnoreCase(cab.Number, _currentFilter) ||
                ContainsIgnoreCase(cab.Description, _currentFilter);

            // Фильтрация сотрудников
            var employees = App.Database.GetEmployeesForCabinet(cab.Id)
                .Where(e => !hasFilter ||
                    ContainsIgnoreCase(e.FirstName, _currentFilter) ||
                    ContainsIgnoreCase(e.LastName, _currentFilter) ||
                    ContainsIgnoreCase(e.Position, _currentFilter))
                .ToList();

            // Фильтрация оборудования
            var equipment = App.Database.GetEquipmentForCabinet(cab.Id)
                .Where(eq => !hasFilter ||
                    ContainsIgnoreCase(eq.Model, _currentFilter) ||
                    ContainsIgnoreCase(eq.OS, _currentFilter))
                .ToList();

            // Показывать кабинет если он или его содержимое совпадает с фильтром
            if (!hasFilter || cabMatches || employees.Count > 0 || equipment.Count > 0)
            {
                var cabinetNode = CreateCabinetNode(cab, employees, equipment, hasFilter);
                treeViewCabinets.Items.Add(cabinetNode);
            }
        }
    }


    private TreeViewItem CreateCabinetNode(
        Cabinet cab,
        List<Employees> employees,
        List<Equipment> equipment,
        bool isFiltered)
    {
        var cabinetNode = new TreeViewItem
        {
            Tag = new NodeInfo { Type = NodeType.Cabinet, Id = cab.Id },
            Header = CreateCabinetHeader(cab)
        };

        // Создание узла "Сотрудники" только если есть данные или нет фильтра
        if (!isFiltered || employees.Count > 0)
        {
            var employeesNode = new TreeViewItem
            {
                Header = new TextBlock { Text = "Сотрудники", Foreground = Brushes.LightSkyBlue },
                Tag = new NodeInfo { Type = NodeType.Employees, ParentId = cab.Id }
            };

            if (isFiltered)
            {
                // При фильтрации сразу загружаем данные
                foreach (var emp in employees)
                {
                    employeesNode.Items.Add(new TreeViewItem
                    {
                        Header = CreateEmployeeHeader(emp),
                        Tag = new NodeInfo { Type = NodeType.Employee, Id = emp.Id, ParentId = cab.Id }
                    });
                }
            }
            else
            {
                employeesNode.Items.Add(new TreeViewItem()); // Фиктивный элемент для стрелки
                employeesNode.Expanded += OnEmployeesOrEquipmentExpanded;
            }

            // Контекстное меню для добавления сотрудника
            var addEmployeeMenuItem = new MenuItem { Header = "Добавить Сотрудника" };
            addEmployeeMenuItem.Click += async (sender, e) =>
            {
                await ShowAddEmployeeDialog(cab.Id);
                LoadCabinets(); // Обновляем весь список с фильтрацией
            };
            employeesNode.ContextMenu = new ContextMenu
            {
                Items = { addEmployeeMenuItem }
            };

            cabinetNode.Items.Add(employeesNode);
        }

        // Создание узла "Оборудование" только если есть данные или нет фильтра
        if (!isFiltered || equipment.Count > 0)
        {
            var equipmentNode = new TreeViewItem
            {
                Header = new TextBlock { Text = "Оборудование", Foreground = Brushes.LightGreen },
                Tag = new NodeInfo { Type = NodeType.Equipment, ParentId = cab.Id }
            };

            if (isFiltered)
            {
                // При фильтрации сразу загружаем данные
                foreach (var eq in equipment)
                {
                    equipmentNode.Items.Add(new TreeViewItem
                    {
                        Header = CreateEquipmentHeader(eq),
                        Tag = new NodeInfo { Type = NodeType.EquipmentItem, Id = eq.Id, ParentId = cab.Id }
                    });
                }
            }
            else
            {
                equipmentNode.Items.Add(new TreeViewItem()); // Фиктивный элемент для стрелки
                equipmentNode.Expanded += OnEmployeesOrEquipmentExpanded;
            }

            // Контекстное меню для добавления оборудования
            var addEquipmentMenuItem = new MenuItem { Header = "Добавить Оборудование" };
            addEquipmentMenuItem.Click += async (sender, e) =>
            {
                await ShowAddEquipmentDialog(cab.Id);
                LoadCabinets(); // Обновляем весь список с фильтрацией
            };
            equipmentNode.ContextMenu = new ContextMenu
            {
                Items = { addEquipmentMenuItem }
            };

            cabinetNode.Items.Add(equipmentNode);
        }

        return cabinetNode;
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false;
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
                Text = $"Кабинет {cab.Number}",
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
            // Загружаем данные только при первом раскрытии
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
        // Полностью очищаем узел перед загрузкой новых данных
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

        // Если нет данных - добавляем заглушку
        if (employees.Count == 0)
        {
            node.Items.Add(new TreeViewItem
            {
                Header = new TextBlock { Text = "Нет сотрудников", Foreground = Brushes.Gray }
            });
        }
    }

    private void LoadEquipment(TreeViewItem node, int cabinetId)
    {
        // Полностью очищаем узел перед загрузкой новых данных
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
                Header = new TextBlock { Text = "Нет оборудования", Foreground = Brushes.Gray }
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
                Foreground = Brushes.DarkGray
            }
        }
        };

        if (eq.Type == "Компьютер")
        {
            stack.Children.Add(new TextBlock
            {
                Text = $" ({eq.OS})",
                Foreground = Brushes.Gray
            });
        }

        return stack;
    }

    private async Task ShowAddEmployeeDialog(int cabinetId)
    {
        var users = App.Database.GetAllUsers();
        var dialog = new Window
        {
            Title = "Добавить сотрудника",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var cmbUser = new ComboBox { ItemsSource = users, DisplayMemberBinding = new Binding("Username") };
        var tbFirstName = new TextBox { Watermark = "Имя" };
        var tbLastName = new TextBox { Watermark = "Фамилия" };
        var tbPosition = new TextBox { Watermark = "Должность" };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "Сохранить" };
        var cancelButton = new Button { Content = "Отмена" };

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(cmbUser);
        panel.Children.Add(tbFirstName);
        panel.Children.Add(tbLastName);
        panel.Children.Add(tbPosition);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var result = await ShowDialog(dialog, saveButton, cancelButton);
        if (result == "Сохранить")
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(tbFirstName.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Имя не может быть пустым")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }



            if (string.IsNullOrWhiteSpace(tbLastName.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Фамилия не может быть пустой")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            if (string.IsNullOrWhiteSpace(tbPosition.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Должность не может быть пустой")
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
            LoadCabinets();
        }
    }

    private async Task ShowAddEquipmentDialog(int cabinetId)
    {
        var dialog = new Window
        {
            Title = "Добавить оборудование",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var cmbType = new ComboBox
        {
            ItemsSource = new List<string> { "Компьютер", "Монитор", "Принтер", "Другое" },
            SelectedIndex = 0
        };
        var tbModel = new TextBox { Watermark = "Модель" };
        var tbOS = new TextBox { Watermark = "ОС", IsVisible = false };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "Сохранить" };
        var cancelButton = new Button { Content = "Отмена" };

        cmbType.SelectionChanged += (s, e) =>
            tbOS.IsVisible = cmbType.SelectedItem?.ToString() == "Компьютер";

        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        panel.Children.Add(cmbType);
        panel.Children.Add(tbModel);
        panel.Children.Add(tbOS);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        var result = await ShowDialog(dialog, saveButton, cancelButton);
        if (result == "Сохранить")
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(tbModel.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Модель не может быть пустой")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            if (cmbType.SelectedItem?.ToString() == "Компьютер" && string.IsNullOrWhiteSpace(tbOS.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Для компьютера необходимо указать ОС")
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
            LoadCabinets();
        }
    }
    
    private async Task<string> ShowDialog(Window dialog, Button saveButton, Button cancelButton)
    {
        var tcs = new TaskCompletionSource<string>();

        saveButton.Click += (s, e) =>
        {
            tcs.TrySetResult("Сохранить");
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            tcs.TrySetResult("Отмена");
            dialog.Close();
        };

        dialog.ShowDialog((Window)VisualRoot);
        return await tcs.Task;
    }


    private async void OnAddCabinetClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Добавить кабинет",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var tbNumber = new TextBox { Watermark = "Номер кабинета" };
        var tbDescription = new TextBox { Watermark = "Описание кабинета" };
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "Сохранить" };
        var cancelButton = new Button { Content = "Отмена" };

        buttonsPanel.Children.Add(saveButton);
        buttonsPanel.Children.Add(cancelButton);
        panel.Children.Add(tbNumber);
        panel.Children.Add(tbDescription);
        panel.Children.Add(buttonsPanel);
        dialog.Content = panel;

        var tcs = new TaskCompletionSource<string>();

        saveButton.Click += async (s, e) =>
        {
            // Валидация номера кабинета
            if (string.IsNullOrWhiteSpace(tbNumber.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Номер кабинета не может быть пустым")
                    .ShowAsync();
                return;
            }

            if (!int.TryParse(tbNumber.Text, out _))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Номер кабинета должен быть числом")
                    .ShowAsync();
                return;
            }

            // Валидация описания
            if (string.IsNullOrWhiteSpace(tbDescription.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Описание кабинета не может быть пустым")
                    .ShowAsync();
                return;
            }

            tcs.TrySetResult("Сохранить");
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            tcs.TrySetResult("Отмена");
            dialog.Close();
        };

        dialog.ShowDialog((Window)this.VisualRoot);
        var result = await tcs.Task;

        if (result == "Сохранить")
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
            Title = "Редактировать кабинет",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Spacing = 10 };
        var tbNumber = new TextBox { Text = cabinet.Number };
        var tbDescription = new TextBox { Text = cabinet.Description };
        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var saveButton = new Button { Content = "Сохранить" };
        var cancelButton = new Button { Content = "Отмена" };

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
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Номер кабинета не может быть пустым")
                    .ShowAsync();
                return;
            }

            if (!int.TryParse(tbNumber.Text, out _))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Номер кабинета должен быть числом")
                    .ShowAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(tbDescription.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Описание кабинета не может быть пустым")
                    .ShowAsync();
                return;
            }

            tcs.TrySetResult("Сохранить");
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            tcs.TrySetResult("Отмена");
            dialog.Close();
        };

        dialog.ShowDialog((Window)this.VisualRoot);
        var result = await tcs.Task;

        if (result == "Сохранить")
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

        // Игнорируем двойной клик на узлах "Сотрудники" и "Оборудование"
        if (info.Type == NodeType.Employees || info.Type == NodeType.Equipment)
            return;

        switch (info.Type)
        {
            case NodeType.Employees:
                var users = App.Database.GetUsersForCabinet(info.ParentId);

                var empDialog = new Window
                {
                    Title = "Сотрудники",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var empPanel = new StackPanel { Spacing = 10 };
                var cmbUsername = new ComboBox { ItemsSource = users, SelectedIndex = 0 };
                var tbFirstName = new TextBox { Watermark = "Имя" };
                var tbLastName = new TextBox { Watermark = "Фамилия" };
                var tbPosition = new TextBox { Watermark = "Должность" };
                var empButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var empSave = new Button { Content = "Сохранить" };
                var empCancel = new Button { Content = "Отмена" };

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
                        await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Имя не может быть пустым")
                            .ShowAsync();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(tbLastName.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Фамилия не может быть пустой")
                            .ShowAsync();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(tbPosition.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Должность не может быть пустой")
                            .ShowAsync();
                        return;
                    }

                    tcs1.TrySetResult("Сохранить");
                    empDialog.Close();
                };

                empCancel.Click += (s, e) =>
                {
                    tcs1.TrySetResult("Отмена");
                    empDialog.Close();
                };

                empDialog.ShowDialog((Window)this.VisualRoot);
                var empResult = await tcs1.Task;

                if (empResult == "Сохранить")
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
                break;

            case NodeType.Equipment:
                var eqDialog = new Window
                {
                    Title = "Оборудование",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var eqPanel = new StackPanel { Spacing = 10 };
                var cmbType = new ComboBox
                {
                    ItemsSource = new List<string> { "Компьютер", "Монитор", "Принтер", "Другое" },
                    SelectedIndex = 0
                };
                var tbModel = new TextBox { Watermark = "Модель" };
                var tbOS = new TextBox { Watermark = "ОС", IsVisible = false };
                var eqButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var eqSave = new Button { Content = "Сохранить" };
                var eqCancel = new Button { Content = "Отмена" };

                cmbType.SelectionChanged += (s, args) =>
                    tbOS.IsVisible = cmbType.SelectedItem?.ToString() == "Компьютер";

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
                    // Валидация данных оборудования
                    if (string.IsNullOrWhiteSpace(tbModel.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Модель не может быть пустой")
                            .ShowAsync();
                        return;
                    }

                    if (cmbType.SelectedItem?.ToString() == "Компьютер" &&
                        string.IsNullOrWhiteSpace(tbOS.Text))
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Ошибка",
                            "Для компьютера необходимо указать ОС")
                            .ShowAsync();
                        return;
                    }

                    tcs.TrySetResult("Сохранить");
                    eqDialog.Close();
                };

                eqCancel.Click += (s, e) =>
                {
                    tcs.TrySetResult("Отмена");
                    eqDialog.Close();
                };

                eqDialog.ShowDialog((Window)this.VisualRoot);
                var eqResult = await tcs.Task;

                if (eqResult == "Сохранить")
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
                break;
        }
    }

    private async void OnExportToExcelClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var cabinets = App.Database.GetAllCabinets();
            if (cabinets.Count == 0)
            {
                await MessageBoxManager.GetMessageBoxStandard("Информация", "Нет данных для экспорта")
                    .ShowWindowDialogAsync((Window)VisualRoot);
                return;
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Кабинеты");
                int row = 1;

                // Заголовки
                worksheet.Cell(row, 1).Value = "Номер кабинета";
                worksheet.Cell(row, 2).Value = "Описание кабинета";
                worksheet.Cell(row, 3).Value = "Тип";
                worksheet.Cell(row, 4).Value = "Имя/Тип оборудования";
                worksheet.Cell(row, 5).Value = "Должность/Модель";
                worksheet.Cell(row, 6).Value = "ОС";

                // Стилизация заголовков
                var headerRange = worksheet.Range(row, 1, row, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;

                // Фиксированные заголовки
                worksheet.SheetView.FreezeRows(1);

                // Автофильтры
                worksheet.Range(row, 1, row, 6).SetAutoFilter();

                row++;


                foreach (var cab in cabinets)
                {
                    // Строка для кабинета
                    worksheet.Cell(row, 1).Value = cab.Number;
                    worksheet.Cell(row, 2).Value = cab.Description;
                    worksheet.Cell(row, 3).Value = "Кабинет";
                    row++;

                    // Сотрудники
                    var employees = App.Database.GetEmployeesForCabinet(cab.Id);
                    foreach (var emp in employees)
                    {
                        worksheet.Cell(row, 1).Value = cab.Number;
                        worksheet.Cell(row, 2).Value = cab.Description;
                        worksheet.Cell(row, 3).Value = "Сотрудник";
                        worksheet.Cell(row, 4).Value = $"{emp.FirstName} {emp.LastName}";
                        worksheet.Cell(row, 5).Value = emp.Position;
                        row++;
                    }

                    // Оборудование
                    var equipment = App.Database.GetEquipmentForCabinet(cab.Id);
                    foreach (var eq in equipment)
                    {
                        worksheet.Cell(row, 1).Value = cab.Number;
                        worksheet.Cell(row, 2).Value = cab.Description;
                        worksheet.Cell(row, 3).Value = "Оборудование";
                        worksheet.Cell(row, 4).Value = eq.Type;
                        worksheet.Cell(row, 5).Value = eq.Model;
                        worksheet.Cell(row, 6).Value = eq.OS ?? string.Empty;
                        row++;
                    }

                    // Пустая строка для разделения
                    row++;
                }

                // Динамическая ширина колонок
                worksheet.Columns().AdjustToContents();

                // Диалог сохранения
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Сохранить Excel файл";
                saveFileDialog.Filters.Add(new FileDialogFilter { Name = "Excel Files", Extensions = { "xlsx" } });

                var filePath = await saveFileDialog.ShowAsync((Window)VisualRoot);

                if (!string.IsNullOrEmpty(filePath))
                {
                    workbook.SaveAs(filePath);
                    await MessageBoxManager.GetMessageBoxStandard("Успех", "Данные экспортированы в Excel!")
                        .ShowWindowDialogAsync((Window)VisualRoot);
                }
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Ошибка экспорта: {ex.Message}")
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