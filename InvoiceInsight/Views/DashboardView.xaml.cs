
using System;
using System.Windows;
using System.Windows.Controls;
using InvoiceInsight.Services;
using InvoiceInsight.ViewModels;

namespace InvoiceInsight.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardView()
        {
            InitializeComponent();

            _viewModel = new DashboardViewModel(
                new DashboardService());

            DataContext = _viewModel;

            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            // Avoid loading the dashboard more than once
            Loaded -= DashboardView_Loaded;

            try
            {
                await _viewModel.LoadDashboardAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to load dashboard data.\n\n{ex.Message}",
                    "Dashboard Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
