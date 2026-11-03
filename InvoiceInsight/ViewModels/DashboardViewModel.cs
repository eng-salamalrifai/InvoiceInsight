using InvoiceInsight.Helpers;
using InvoiceInsight.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows;

namespace InvoiceInsight.ViewModels;

public class DashboardViewModel(DashboardService service) : ViewModelBase
{
    // The supplier charts show the biggest suppliers only, so they stay readable.
    private const int MaxSuppliersInChart = 10;
    private const int MaxBarWidth = 22;

    private static readonly SKColor LineColor = SKColor.Parse("#8064A2");
    private static readonly SKColor LineFillColor = SKColor.Parse("#338064A2");
    private static readonly SKColor SpendingBarColor = SKColor.Parse("#E59ABF");
    private static readonly SKColor CountBarColor = SKColor.Parse("#65B9B0");
    private static readonly SKColor LabelColor = SKColor.Parse("#77718A");

    // Dashboard cards

    private string _totalInvoices = "0";
    public string TotalInvoices
    {
        get => _totalInvoices;
        private set => SetProperty(ref _totalInvoices, value);
    }

    private string _totalValue = TextFormat.Money(0m);
    public string TotalValue
    {
        get => _totalValue;
        private set => SetProperty(ref _totalValue, value);
    }

    private string _totalSuppliers = "0";
    public string TotalSuppliers
    {
        get => _totalSuppliers;
        private set => SetProperty(ref _totalSuppliers, value);
    }

    // Chart data

    public ObservableCollection<MonthlyChartData> MonthlyChartData { get; } = [];
    public ObservableCollection<SupplierChartData> SupplierSpendingData { get; } = [];
    public ObservableCollection<SupplierChartData> SupplierCountData { get; } = [];

    // Chart series and axes

    public ISeries[] MonthlySeries { get; private set; } = [];
    public Axis[] MonthlyXAxes { get; private set; } = [];
    public Axis[] MonthlyYAxes { get; private set; } = [];

    public ISeries[] SpendingSeries { get; private set; } = [];
    public Axis[] SpendingXAxes { get; private set; } = [];
    public Axis[] SpendingYAxes { get; private set; } = [];

    public ISeries[] SupplierCountSeries { get; private set; } = [];
    public Axis[] SupplierCountXAxes { get; private set; } = [];
    public Axis[] SupplierCountYAxes { get; private set; } = [];

    // Table data

    public ObservableCollection<TopInvoiceData> TopInvoices { get; } = [];

    private Visibility _topInvoicesEmptyVisibility = Visibility.Visible;
    public Visibility TopInvoicesEmptyVisibility
    {
        get => _topInvoicesEmptyVisibility;
        private set => SetProperty(ref _topInvoicesEmptyVisibility, value);
    }

    // ---------- Loading ----------

    public async Task LoadDashboardAsync()
    {
        // Independent queries, each with its own DbContext, so they can run together.
        var cardsTask = service.GetDashboardCardsAsync();
        var monthlyTask = service.GetMonthlyChartDataAsync();
        var suppliersTask = service.GetSupplierChartDataAsync();
        var topInvoicesTask = service.GetTopInvoicesAsync();

        await Task.WhenAll(cardsTask, monthlyTask, suppliersTask, topInvoicesTask);

        var cards = await cardsTask;
        TotalInvoices = TextFormat.Number(cards.TotalInvoices);
        TotalValue = TextFormat.Money(cards.TotalValue);
        TotalSuppliers = TextFormat.Number(cards.TotalSuppliers);

        // The service returns suppliers sorted by spending; sort again for the count chart.
        var suppliers = await suppliersTask;

        UpdateCollection(MonthlyChartData, await monthlyTask);
        UpdateCollection(SupplierSpendingData, suppliers.Take(MaxSuppliersInChart));
        UpdateCollection(SupplierCountData, suppliers
            .OrderByDescending(s => s.InvoiceCount)
            .ThenBy(s => s.SupplierName)
            .Take(MaxSuppliersInChart));
        UpdateCollection(TopInvoices, await topInvoicesTask);

        BuildCharts();

        TopInvoicesEmptyVisibility = TopInvoices.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static void UpdateCollection<T>(
        ObservableCollection<T> collection,
        IEnumerable<T> items)
    {
        collection.Clear();

        foreach (var item in items)
            collection.Add(item);
    }

    // ---------- Charts ----------

    private void BuildCharts()
    {
        (MonthlySeries, MonthlyXAxes, MonthlyYAxes) = BuildMonthlyChart();
        (SpendingSeries, SpendingXAxes, SpendingYAxes) = BuildSpendingChart();
        (SupplierCountSeries, SupplierCountXAxes, SupplierCountYAxes) = BuildSupplierCountChart();

        // These setters are private and don't raise events, so tell the view.
        OnPropertyChanged(nameof(MonthlySeries));
        OnPropertyChanged(nameof(MonthlyXAxes));
        OnPropertyChanged(nameof(MonthlyYAxes));
        OnPropertyChanged(nameof(SpendingSeries));
        OnPropertyChanged(nameof(SpendingXAxes));
        OnPropertyChanged(nameof(SpendingYAxes));
        OnPropertyChanged(nameof(SupplierCountSeries));
        OnPropertyChanged(nameof(SupplierCountXAxes));
        OnPropertyChanged(nameof(SupplierCountYAxes));
    }

    // Line chart: invoice totals per month.
    private (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) BuildMonthlyChart()
    {
        ISeries[] series =
        [
            new LineSeries<double>
            {
                Name = "Invoice Amount",
                Values = MonthlyChartData.Select(x => (double)x.TotalAmount).ToArray(),
                Stroke = new SolidColorPaint(LineColor) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(LineFillColor),
                GeometrySize = 9,
                GeometryFill = new SolidColorPaint(LineColor),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                LineSmoothness = 0.5
            }
        ];

        Axis[] xAxes =
        [
            new Axis
            {
                Labels = MonthlyChartData.Select(x => x.Month).ToArray(),
                LabelsPaint = new SolidColorPaint(LabelColor),
                TextSize = 12
            }
        ];

        Axis[] yAxes =
        [
            new Axis
            {
                Labeler = value => TextFormat.Money(value, "N0"),
                LabelsPaint = new SolidColorPaint(LabelColor),
                TextSize = 11
            }
        ];

        return (series, xAxes, yAxes);
    }

    // Bar chart: total spending per supplier.
    private (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) BuildSpendingChart()
    {
        ISeries[] series =
        [
            new RowSeries<double>
            {
                Name = "Total Spending",
                Values = SupplierSpendingData.Select(x => (double)x.TotalAmount).ToArray(),
                Fill = new SolidColorPaint(SpendingBarColor),
                MaxBarWidth = MaxBarWidth,
                XToolTipLabelFormatter = point => TextFormat.Money(point.Coordinate.PrimaryValue)
            }
        ];

        // About three grid lines. Fall back to 1 when there is nothing to show,
        // because a step of 0 breaks the axis.
        double maxAmount = SupplierSpendingData.Count > 0
            ? SupplierSpendingData.Max(x => (double)x.TotalAmount)
            : 0;

        Axis[] xAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MinStep = maxAmount > 0 ? maxAmount / 3 : 1,
                Labeler = value => value.ToString("N0", TextFormat.Culture),
                LabelsPaint = new SolidColorPaint(LabelColor),
                TextSize = 10
            }
        ];

        Axis[] yAxes = [SupplierNamesAxis(SupplierSpendingData)];

        return (series, xAxes, yAxes);
    }

    // Bar chart: number of invoices per supplier.
    private (ISeries[] Series, Axis[] XAxes, Axis[] YAxes) BuildSupplierCountChart()
    {
        ISeries[] series =
        [
            new RowSeries<double>
            {
                Name = "Invoice Count",
                Values = SupplierCountData.Select(x => (double)x.InvoiceCount).ToArray(),
                Fill = new SolidColorPaint(CountBarColor),
                MaxBarWidth = MaxBarWidth,
                XToolTipLabelFormatter = point =>
                    point.Coordinate.PrimaryValue.ToString("N0", TextFormat.Culture)
            }
        ];

        Axis[] xAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MinStep = 1,
                Labeler = value => value.ToString("N0", TextFormat.Culture),
                LabelsPaint = new SolidColorPaint(LabelColor),
                TextSize = 10
            }
        ];

        Axis[] yAxes = [SupplierNamesAxis(SupplierCountData)];

        return (series, xAxes, yAxes);
    }

    // The vertical axis of both bar charts: one label per supplier.
    private static Axis SupplierNamesAxis(IEnumerable<SupplierChartData> data)
    {
        return new Axis
        {
            Labels = data.Select(x => x.SupplierName).ToArray(),
            ForceStepToMin = true,
            MinStep = 1,
            LabelsPaint = new SolidColorPaint(LabelColor),
            TextSize = 10
        };
    }
}
