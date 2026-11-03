using InvoiceInsight.Models;
using InvoiceInsight.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace InvoiceInsight.ViewModels;

public class ExcelImportViewModel(ExcelImportService service) : ViewModelBase
{
    private string _fileName = "No file selected";
    private int _totalInvoices;

    private bool _isBulkUpdating;

    public ObservableCollection<ImportedInvoice> Invoices { get; } = [];

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public int TotalInvoices
    {
        get => _totalInvoices;
        private set => SetProperty(ref _totalInvoices, value);
    }

    public int ErrorCount =>
        Invoices.Count(invoice => invoice.ValidationErrors.Count > 0);

    public int WarningCount =>
        Invoices.Count(invoice =>
            invoice.ValidationErrors.Count == 0 &&
            invoice.Warnings.Count > 0);

    public int ValidCount =>
        Invoices.Count(invoice =>
            invoice.ValidationErrors.Count == 0 &&
            invoice.Warnings.Count == 0);

    public int SelectedCount =>
        Invoices.Count(invoice => invoice.IsSelectedForImport);

    public int SelectedWarningCount =>
        Invoices.Count(invoice =>
            invoice.IsSelectedForImport &&
            invoice.Warnings.Count > 0);

    public bool HasSelectedInvoices => SelectedCount > 0;

    public void LoadInvoices(string filePath)
    {
        // Read first: if the file is bad, the current list stays untouched.
        var importedData = service.ReadAndValidateInvoices(filePath);

        foreach (var invoice in Invoices)
            invoice.PropertyChanged -= Invoice_PropertyChanged;

        Invoices.Clear();

        foreach (var invoice in importedData)
        {
            invoice.PropertyChanged += Invoice_PropertyChanged;
            Invoices.Add(invoice);
        }

        TotalInvoices = Invoices.Count;
        NotifyCountsChanged();
    }

    public void RevalidateInvoices()
    {
        _isBulkUpdating = true;

        try
        {
            service.RevalidateInvoices(Invoices.ToList());
        }
        finally
        {
            _isBulkUpdating = false;
            NotifyCountsChanged();
        }
    }

    public int SaveSelectedInvoices()
    {
        var selectedInvoices = Invoices
            .Where(invoice => invoice.IsSelectedForImport)
            .ToList();

        int savedCount = service.SaveInvoices(selectedInvoices);

        foreach (var invoice in selectedInvoices)
        {
            invoice.PropertyChanged -= Invoice_PropertyChanged;
            Invoices.Remove(invoice);
        }

        TotalInvoices = Invoices.Count;

        RefreshValidationAfterSave();

        return savedCount;
    }

     private void RefreshValidationAfterSave()
    {
        try
        {
            service.RefreshExistingData();
            RevalidateInvoices();
        }
        catch (Exception ex)
        {
            
            Debug.WriteLine($"Revalidation after save failed: {ex.Message}");
            NotifyCountsChanged();
        }
    }

    private void Invoice_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isBulkUpdating)
            return;

        if (e.PropertyName == nameof(ImportedInvoice.IsSelectedForImport) ||
            e.PropertyName == nameof(ImportedInvoice.InvoiceNumber) ||
            e.PropertyName == nameof(ImportedInvoice.SupplierName) ||
            e.PropertyName == nameof(ImportedInvoice.InvoiceDate) ||
            e.PropertyName == nameof(ImportedInvoice.Amount) ||
            e.PropertyName == nameof(ImportedInvoice.ValidationStatus) ||
            e.PropertyName == nameof(ImportedInvoice.ValidationDetails) ||
            e.PropertyName == nameof(ImportedInvoice.CanImport))
        {
            NotifyCountsChanged();
        }
    }

    private void NotifyCountsChanged()
    {
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(ValidCount));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedWarningCount));
        OnPropertyChanged(nameof(HasSelectedInvoices));
    }
}
