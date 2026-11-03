using InvoiceInsight.Data;
using InvoiceInsight.Seeds;
using Microsoft.EntityFrameworkCore;

namespace InvoiceInsight.Services;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly bool _seedSampleData;

    /// <param name="seedSampleData">
    /// When true, empty Suppliers/Invoices tables are filled with demo data.
    /// Pass false for a real deployment.
    /// </param>
    public DatabaseInitializer(AppDbContext context, bool seedSampleData = true)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _seedSampleData = seedSampleData;
    }

    public async Task InitializeAsync()
    {
        if (!await _context.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "Cannot connect to the database. Check the connection settings and that the server is running.");
        }

        if (_seedSampleData)
            await SeedSampleDataAsync();
    }

    private async Task SeedSampleDataAsync()
    {
        if (!await _context.Suppliers.AnyAsync())
        {
            await _context.Suppliers.AddRangeAsync(SupplierSeed.GetSuppliers());
            await _context.SaveChangesAsync();
        }

        if (!await _context.Invoices.AnyAsync())
        {
            var suppliers = await _context.Suppliers.ToListAsync();

            await _context.Invoices.AddRangeAsync(InvoiceSeed.GetInvoices(suppliers));
            await _context.SaveChangesAsync();
        }
    }
}
