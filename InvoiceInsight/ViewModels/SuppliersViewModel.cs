using InvoiceInsight.Services;
using System.Collections.ObjectModel;

namespace InvoiceInsight.ViewModels;

public class SuppliersViewModel(SupplierService service) : ViewModelBase
{
    public ObservableCollection<SupplierListData> Suppliers { get; } = [];

    public async Task LoadSuppliersAsync()
    {
        var suppliers = await service.GetSuppliersAsync();

        Suppliers.Clear();

        foreach (var supplier in suppliers)
            Suppliers.Add(supplier);
    }
}
