using InvoiceInsight.Helpers;
using InvoiceInsight.Models;

namespace InvoiceInsight.Services;


public class InvoiceValidator
{
    // Compare with a supplier's average only when there is enough history.
    private const int MinimumHistoryCount = 3;
    private const decimal UnusualAmountMultiplier = 3m;

    /// <param name="existingInvoices">Invoices already saved. Supplier must be loaded.</param>
    /// <param name="existingSupplierNames">Names of the suppliers already saved.</param>
    public void Validate(
        IReadOnlyCollection<ImportedInvoice> importedInvoices,
        IReadOnlyCollection<Invoice> existingInvoices,
        IReadOnlyCollection<string> existingSupplierNames)
    {
        foreach (var invoice in importedInvoices)
            invoice.ClearValidation();

        CheckRequiredFields(importedInvoices);
        CheckDuplicatesInFile(importedInvoices);
        CheckDuplicatesInDatabase(importedInvoices, existingInvoices);
        CheckSupplierIsKnown(importedInvoices, existingSupplierNames);
        CheckUnusualAmounts(importedInvoices, existingInvoices);
    }

    private static void CheckRequiredFields(IEnumerable<ImportedInvoice> invoices)
    {
        foreach (var invoice in invoices)
        {
            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                invoice.AddError("Invoice number is required.");

            if (string.IsNullOrWhiteSpace(invoice.SupplierName))
                invoice.AddError("Supplier name is required.");

            if (!invoice.InvoiceDate.HasValue)
                invoice.AddError("Invoice date is missing or invalid.");
            else if (invoice.InvoiceDate.Value.Date > DateTime.Today)
                invoice.AddWarning("Invoice date is in the future. Please verify it.");

            if (!invoice.Amount.HasValue)
                invoice.AddError("Invoice amount is missing or invalid.");
            else if (invoice.Amount.Value <= 0)
                invoice.AddError("Invoice amount must be greater than zero.");
        }
    }

    private static void CheckDuplicatesInFile(IEnumerable<ImportedInvoice> invoices)
    {
        var duplicateGroups = invoices
            .Where(HasNumberAndSupplier)
            .GroupBy(invoice => KeyOf(invoice.InvoiceNumber, invoice.SupplierName))
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            foreach (var invoice in group)
            {
                // Show the other rows only, not the row being flagged.
                var otherRows = group
                    .Where(other => other != invoice)
                    .Select(other => other.ExcelRowNumber);

                invoice.AddWarning(
                    $"The invoice number and supplier also appear in Excel row(s): {string.Join(", ", otherRows)}.");
            }
        }
    }

    private static void CheckDuplicatesInDatabase(
        IEnumerable<ImportedInvoice> importedInvoices,
        IEnumerable<Invoice> existingInvoices)
    {
        // Built once, so each imported row is a quick lookup instead of a scan.
        var existingByKey = existingInvoices.ToLookup(
            existing => KeyOf(existing.InvoiceNumber, existing.Supplier.Name));

        foreach (var invoice in importedInvoices.Where(HasNumberAndSupplier))
        {
            var matches = existingByKey[KeyOf(invoice.InvoiceNumber, invoice.SupplierName)];

            if (!matches.Any())
                continue;

            bool sameDateAndAmount = invoice.InvoiceDate.HasValue &&
                invoice.Amount.HasValue &&
                matches.Any(existing =>
                    existing.InvoiceDate.Date == invoice.InvoiceDate.Value.Date &&
                    existing.Amount == invoice.Amount.Value);

            invoice.AddWarning(sameDateAndAmount
                ? "An invoice with the same number, supplier, date and amount already exists in the database."
                : "An invoice with the same number and supplier already exists in the database. Check whether this is a duplicate.");
        }
    }

    private static void CheckSupplierIsKnown(
        IEnumerable<ImportedInvoice> importedInvoices,
        IEnumerable<string> existingSupplierNames)
    {
        var knownSuppliers = existingSupplierNames
            .Select(TextFormat.Normalize)
            .ToHashSet();

        foreach (var invoice in importedInvoices)
        {
            if (string.IsNullOrWhiteSpace(invoice.SupplierName))
                continue;

            if (!knownSuppliers.Contains(TextFormat.Normalize(invoice.SupplierName)))
            {
                invoice.AddWarning(
                    "This supplier is not registered in the database. It will be added automatically during import.");
            }
        }
    }

    private static void CheckUnusualAmounts(
        IEnumerable<ImportedInvoice> importedInvoices,
        IEnumerable<Invoice> existingInvoices)
    {
        var averageBySupplier = existingInvoices
            .Where(existing => existing.Amount > 0)
            .GroupBy(existing => TextFormat.Normalize(existing.Supplier.Name))
            .Where(group => group.Count() >= MinimumHistoryCount)
            .ToDictionary(
                group => group.Key,
                group => group.Average(existing => existing.Amount));

        foreach (var invoice in importedInvoices)
        {
            if (!invoice.Amount.HasValue || invoice.Amount.Value <= 0)
                continue;

            bool hasHistory = averageBySupplier.TryGetValue(
                TextFormat.Normalize(invoice.SupplierName),
                out decimal average);

            if (hasHistory && invoice.Amount.Value > average * UnusualAmountMultiplier)
            {
                invoice.AddWarning(
                    $"The amount is more than {UnusualAmountMultiplier:0.#} times this supplier's historical average ({average:N2}).");
            }
        }
    }

    private static bool HasNumberAndSupplier(ImportedInvoice invoice) =>
        !string.IsNullOrWhiteSpace(invoice.InvoiceNumber) &&
        !string.IsNullOrWhiteSpace(invoice.SupplierName);

    // Two invoices are "the same" when number and supplier match (ignoring case and spaces).
    private static (string Number, string Supplier) KeyOf(string? number, string? supplier) =>
        (TextFormat.Normalize(number), TextFormat.Normalize(supplier));
}
