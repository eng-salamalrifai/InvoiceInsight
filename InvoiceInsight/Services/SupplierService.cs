using InvoiceInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceInsight.Services;

public class SupplierListData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SupplierService
{
    public async Task<List<SupplierListData>> GetSuppliersAsync()
    {
        using var db = new AppDbContext();

        return await db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SupplierListData
            {
                Id = s.Id,
                Name = s.Name,
                InvoiceCount = s.Invoices.Count(),
                TotalAmount = s.Invoices.Sum(i => (decimal?)i.Amount) ?? 0
            })
            .ToListAsync();
    }
}
