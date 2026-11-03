using InvoiceInsight.Models;
using InvoiceInsight.Services;
using System.Collections.ObjectModel;

namespace InvoiceInsight.ViewModels;

public class InvoicesViewModel(InvoiceService invoiceService) : ViewModelBase
{
    public ObservableCollection<Invoice> Invoices { get; } = [];

    public async Task LoadInvoicesAsync()
    {
        var invoices = await invoiceService.GetInvoicesAsync();

        Invoices.Clear();

        foreach (var invoice in invoices)
            Invoices.Add(invoice);
    }
}
