using InvoiceInsight.Data;
using InvoiceInsight.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InvoiceInsight.Services;

// Data shapes returned by DashboardService

public class DashboardCardsData
{
    public int TotalInvoices { get; set; }
    public decimal TotalValue { get; set; }
    public int TotalSuppliers { get; set; }
}

public class MonthlyChartData
{
    public string Month { get; set; } = "";
    public decimal TotalAmount { get; set; }
}

public class SupplierChartData
{
    public string SupplierName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
}

public class TopInvoiceData
{
    public string InvoiceNumber { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public decimal Amount { get; set; }
}

public class DashboardService
{
    private const int MonthsInTrend = 6;
    private const int TopInvoicesCount = 5;

    public async Task<DashboardCardsData> GetDashboardCardsAsync()
    {
        using var db = new AppDbContext();

        return new DashboardCardsData
        {
            TotalInvoices = await db.Invoices.CountAsync(),

            TotalValue = await db.Invoices.SumAsync(i => (decimal?)i.Amount) ?? 0,

            TotalSuppliers = await db.Suppliers.CountAsync()
        };
    }

    // Totals for the last six months, oldest first.
    public async Task<List<MonthlyChartData>> GetMonthlyChartDataAsync()
    {
        using var db = new AppDbContext();

        DateTime thisMonth = FirstDayOfCurrentMonth();
        DateTime startDate = thisMonth.AddMonths(-(MonthsInTrend - 1));
        DateTime endDate = thisMonth.AddMonths(1);

        var totals = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate < endDate)
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(i => i.Amount) })
            .ToListAsync();

        var result = new List<MonthlyChartData>();

        // Every month gets a point, even without invoices, so the chart has no gaps.
        for (int i = 0; i < MonthsInTrend; i++)
        {
            DateTime month = startDate.AddMonths(i);

            var monthTotal = totals.FirstOrDefault(t =>
                t.Year == month.Year && t.Month == month.Month);

            result.Add(new MonthlyChartData
            {
                Month = month.ToString("MMM yyyy", TextFormat.Culture),
                TotalAmount = monthTotal?.Total ?? 0
            });
        }

        return result;
    }

    
    public async Task<List<SupplierChartData>> GetSupplierChartDataAsync()
    {
        using var db = new AppDbContext();

        return await db.Invoices
            .AsNoTracking()
            .GroupBy(i => new { i.SupplierId, i.Supplier.Name })
            .Select(g => new SupplierChartData
            {
                SupplierName = g.Key.Name,
                TotalAmount = g.Sum(i => i.Amount),
                InvoiceCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.SupplierName)
            .ToListAsync();
    }

    // The five largest invoices of the current month.
    public async Task<List<TopInvoiceData>> GetTopInvoicesAsync()
    {
        using var db = new AppDbContext();

        DateTime start = FirstDayOfCurrentMonth();
        DateTime end = start.AddMonths(1);

        return await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate < end)
            .OrderByDescending(i => i.Amount)
            .ThenBy(i => i.InvoiceNumber)
            .Take(TopInvoicesCount)
            .Select(i => new TopInvoiceData
            {
                InvoiceNumber = i.InvoiceNumber,
                SupplierName = i.Supplier.Name,
                InvoiceDate = i.InvoiceDate,
                Amount = i.Amount
            })
            .ToListAsync();
    }

    private static DateTime FirstDayOfCurrentMonth()
    {
        DateTime today = DateTime.Today;
        return new DateTime(today.Year, today.Month, 1);
    }
}
