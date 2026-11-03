using InvoiceInsight.Services;
using InvoiceInsight.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InvoiceInsight.Views
{
    public partial class InvoicesView : UserControl
    {
        private readonly InvoicesViewModel _viewModel;

        public InvoicesView()
        {
            InitializeComponent();

            _viewModel = new InvoicesViewModel(new InvoiceService());

            DataContext = _viewModel;

            Loaded += InvoicesView_Loaded;
        }

        private async void InvoicesView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= InvoicesView_Loaded;

            try
            {
                await _viewModel.LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading invoices: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            PageScrollViewer.ScrollToVerticalOffset(
                PageScrollViewer.VerticalOffset - e.Delta);

            e.Handled = true;
        }
    }
}