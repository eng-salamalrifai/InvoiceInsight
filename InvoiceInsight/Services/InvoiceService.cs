using InvoiceInsight.Data;
using InvoiceInsight.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceInsight.Services;

public class InvoiceService
{
    // Newest invoices first, with the supplier loaded for display.
    public async Task<List<Invoice>> GetInvoicesAsync()
    {
        using var db = new AppDbContext();

        return await db.Invoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }
}
