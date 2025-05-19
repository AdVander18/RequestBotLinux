using Avalonia.Controls;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.AspNetCore.Hosting.Server;
using RequestBotLinux.Models;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace RequestBotLinux;

public partial class AnalyticsView : UserControl
{
    private readonly DataBase _database;
    private ComboBox _statusFilterComboBox;
    private ComboBox _timeFilterComboBox;
    private PieChart _statusChart;
    private CartesianChart _cabinetChart;
    private string _currentTimeFilter = "month";

    public AnalyticsView(DataBase database)
    {
        _database = database;
        InitializeComponent();
        InitializeControls();
        LoadData();
    }

    private void InitializeControls()
    {
        // Инициализация фильтров
        _statusFilterComboBox = this.Find<ComboBox>("StatusFilterComboBox");
        _timeFilterComboBox = this.Find<ComboBox>("TimeFilterComboBox");

        _statusFilterComboBox.SelectionChanged += (s, e) => UpdateCharts();
        _timeFilterComboBox.SelectionChanged += (s, e) =>
        {
            _currentTimeFilter = _timeFilterComboBox.SelectedIndex == 0 ? "month" : "week";
            UpdateCharts();
        };

        // Инициализация графиков
        _statusChart = new PieChart();
        _cabinetChart = new CartesianChart();

        // Добавление графиков в XAML-контейнеры
        var statusTabContent = this.Find<Border>("StatusChartContainer");
        statusTabContent.Child = _statusChart;

        var cabinetTabContent = this.Find<Border>("CabinetChartContainer");
        cabinetTabContent.Child = _cabinetChart;
    }

    private void LoadData()
    {
        UpdateStatusChart(GetCurrentStatusFilter());
        UpdateCabinetChart(GetCurrentStatusFilter());
    }

    private void UpdateCharts()
    {
        var filter = GetCurrentStatusFilter();
        UpdateStatusChart(filter);
        UpdateCabinetChart(filter);
    }

    private string GetCurrentStatusFilter()
    {
        return _statusFilterComboBox.SelectedItem?.ToString() ?? "Все";
    }

    private void UpdateStatusChart(string statusFilter)
    {
        var stats = GetStatusStatistics(statusFilter);
        var series = new List<ISeries>();

        if (stats.Count == 0)
        {
            // Добавляем заглушку, если данных нет
            series.Add(new PieSeries<int>
            {
                Values = new[] { 1 },
                Name = "Нет данных",
                Fill = new SolidColorPaint(SKColors.LightGray)
            });
        }
        else
        {
            foreach (var stat in stats)
            {
                series.Add(new PieSeries<int>
                {
                    Values = new[] { stat.Value },
                    Name = stat.Key,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsFormatter = point => $"{point.StackedValue.Share:P1}",
                    Fill = new SolidColorPaint(GetColorForStatus(stat.Key))
                });
            }
        }

        _statusChart.Series = series;
    }
    private SKColor GetColorForStatus(string status)
    {
        return status switch
        {
            "Завершено" => SKColors.LightGreen,
            "В работе" => SKColors.Orange,
            _ => SKColors.Gray
        };
    }


    private void UpdateCabinetChart(string statusFilter)
    {
        var cabinetStats = GetCabinetStatistics(statusFilter);

        // Используйте тип double вместо object
        var series = new ColumnSeries<double> // <-- Измените здесь
        {
            Values = cabinetStats.Select(c => c.Value).ToArray(), // <-- Уберите приведение к object
            Name = "Задачи",
            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}%"
        };

        // Остальной код остается без изменений
        _cabinetChart.XAxes = new List<Axis>
    {
        new Axis
        {
            Labels = cabinetStats.Select(c => c.Key).ToArray(),
            LabelsRotation = 45,
            TextSize = 12
        }
    };

        _cabinetChart.YAxes = new List<Axis>
    {
        new Axis
        {
            Labeler = value => $"{value}%",
            MaxLimit = 100
        }
    };

        _cabinetChart.Series = new List<ISeries> { series };
    }

    private Dictionary<string, int> GetStatusStatistics(string filter)
    {
        var allTasks = _database.GetAllTasks();
        var filtered = allTasks.Where(t =>
            filter == "Все" ||
            (filter == "Завершено" && t.Status == "Завершено") ||
            (filter == "Не завершено" && t.Status != "Завершено")
        );

        return filtered
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<string, double> GetCabinetStatistics(string filter)
    {
        var allTasks = _database.GetAllTasks();
        var total = allTasks.Count;

        return allTasks
            .GroupBy(t => t.CabinetNumber)
            .ToDictionary(
                g => g.Key,
                g => total > 0 ? (g.Count() / (double)total) * 100 : 0
            );
    }

}