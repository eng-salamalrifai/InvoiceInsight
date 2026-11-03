using ClosedXML.Excel;
using InvoiceInsight.Data;
using InvoiceInsight.Helpers;
using InvoiceInsight.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace InvoiceInsight.Services;

public class ExcelImportService
{
    private const int NumberColumn = 1;
    private const int SupplierColumn = 2;
    private const int DateColumn = 3;
    private const int AmountColumn = 4;
    private const int FirstDataRow = 2;

    private static readonly string[] TextDateFormats =
    [
        "yyyy-MM-dd", "yyyy/MM/dd",
        "dd/MM/yyyy", "d/M/yyyy",
        "dd-MM-yyyy", "d-M-yyyy",
        "dd.MM.yyyy", "d.M.yyyy"
    ];

    // Accepts one unambiguous number with optional currency text around it.
    // Thousands use commas and decimals use dots (for example: SYR 1,250.50).
    // Invalid or ambiguous formats are rejected instead of being silently changed.
    private static readonly Regex TextAmountPattern = new(
        @"^\s*[^0-9+\-.,()]*([+-]?(?:\d+|\d{1,3}(?:,\d{3})+)(?:\.\d+)?)\s*[^0-9+\-.,()]*$",
        RegexOptions.Compiled);

    private readonly InvoiceValidator _validator = new();

    
    private List<Invoice> _existingInvoices = [];
    private List<string> _existingSupplierNames = [];


    public List<ImportedInvoice> ReadAndValidateInvoices(string filePath)
    {
        var invoices = ReadExcelFile(filePath);

        LoadExistingData();
        _validator.Validate(invoices, _existingInvoices, _existingSupplierNames);

        return invoices;
    }

    // Called after the user edits rows.
    public void RevalidateInvoices(List<ImportedInvoice> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        _validator.Validate(invoices, _existingInvoices, _existingSupplierNames);
    }

    public int SaveInvoices(IEnumerable<ImportedInvoice> selectedInvoices)
    {
        var invoicesToSave = selectedInvoices
            .Where(invoice => invoice.IsSelectedForImport)
            .ToList();

        if (invoicesToSave.Count == 0)
            throw new InvalidOperationException("Please select at least one invoice to import.");

        foreach (var invoice in invoicesToSave)
            EnsureCanBeSaved(invoice);

        using var db = new AppDbContext();

        var suppliersByName = db.Suppliers
            .ToList()
            .GroupBy(supplier => TextFormat.Normalize(supplier.Name))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var imported in invoicesToSave)
        {
            string supplierKey = TextFormat.Normalize(imported.SupplierName);

            if (!suppliersByName.TryGetValue(supplierKey, out var supplier))
            {
                supplier = new Supplier
                {
                    Name = imported.SupplierName.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                db.Suppliers.Add(supplier);
                suppliersByName[supplierKey] = supplier;
            }

          
            db.Invoices.Add(new Invoice
            {
                InvoiceNumber = imported.InvoiceNumber.Trim(),
                Supplier = supplier,
                InvoiceDate = imported.InvoiceDate!.Value,
                Amount = imported.Amount!.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        db.SaveChanges();

        return invoicesToSave.Count;
    }

    // ---------- Reading the Excel file ----------

    private static List<ImportedInvoice> ReadExcelFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Please select an Excel file.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("The selected Excel file was not found.", filePath);

        using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new XLWorkbook(stream);

        if (workbook.Worksheets.Count == 0)
            throw new InvalidDataException("The Excel file does not contain a worksheet.");

        var sheet = workbook.Worksheet(1);
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        List<ImportedInvoice> invoices = [];

        for (int row = FirstDataRow; row <= lastRow; row++)
        {
            var number = sheet.Cell(row, NumberColumn);
            var supplier = sheet.Cell(row, SupplierColumn);
            var date = sheet.Cell(row, DateColumn);
            var amount = sheet.Cell(row, AmountColumn);

            if (number.IsEmpty() && supplier.IsEmpty() && date.IsEmpty() && amount.IsEmpty())
                continue;

            invoices.Add(new ImportedInvoice
            {
                ExcelRowNumber = row,
                InvoiceNumber = number.GetString().Trim(),
                SupplierName = supplier.GetString().Trim(),
                InvoiceDate = ReadDate(date),
                Amount = ReadAmount(amount)
            });
        }

        return invoices;
    }

    private static DateTime? ReadDate(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().Date;

        bool parsed = DateTime.TryParseExact(
            cell.GetString().Trim(),
            TextDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date);

        return parsed ? date.Date : null;
    }

    private static decimal? ReadAmount(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        if (cell.DataType == XLDataType.Number && cell.TryGetValue(out decimal number))
            return number;

        string text = cell.GetString().Trim();
        Match match = TextAmountPattern.Match(text);

        if (!match.Success)
            return null;

        bool parsed = decimal.TryParse(
            match.Groups[1].Value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out decimal amount);

        return parsed ? amount : null;
    }

    // ---------- Database ----------

    public void RefreshExistingData()
    {
        LoadExistingData();
    }

    private void LoadExistingData()
    {
        using var db = new AppDbContext();

        _existingInvoices = db.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Supplier)
            .ToList();

        _existingSupplierNames = db.Suppliers
            .AsNoTracking()
            .Select(supplier => supplier.Name)
            .ToList();
    }

   
    private static void EnsureCanBeSaved(ImportedInvoice invoice)
    {
        bool isComplete =
            !string.IsNullOrWhiteSpace(invoice.InvoiceNumber) &&
            !string.IsNullOrWhiteSpace(invoice.SupplierName) &&
            invoice.InvoiceDate.HasValue &&
            invoice.Amount is > 0;

        if (!invoice.CanImport || !isComplete)
        {
            throw new InvalidOperationException(
                $"Excel row {invoice.ExcelRowNumber} contains validation errors.");
        }
    }
}
