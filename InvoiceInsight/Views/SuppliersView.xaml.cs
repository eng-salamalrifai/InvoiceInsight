using System;
using System.Windows;
using System.Windows.Controls;
using InvoiceInsight.Services;
using InvoiceInsight.ViewModels;
using System.Windows.Input;

namespace InvoiceInsight.Views
{
    public partial class SuppliersView : UserControl
    {
        private readonly SuppliersViewModel _viewModel;

        public SuppliersView()
        {
            InitializeComponent();

            _viewModel = new SuppliersViewModel(new SupplierService());
            DataContext = _viewModel;

            Loaded += SuppliersView_Loaded;
        }
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            PageScrollViewer.ScrollToVerticalOffset(
                PageScrollViewer.VerticalOffset - e.Delta);

            e.Handled = true;
        }

        private async void SuppliersView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SuppliersView_Loaded;

            try
            {
                await _viewModel.LoadSuppliersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to load suppliers.\n\n{ex.Message}",
                    "Suppliers Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}