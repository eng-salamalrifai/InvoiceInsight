
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InvoiceInsight.Views;

namespace InvoiceInsight;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowDashboard();
    }

    private void ShowDashboard()
    {
        PageTitle.Text = "Dashboard";
        MainContent.Content = new DashboardView();
    }

    private void Dashboard_Checked(object sender, RoutedEventArgs e)
    {
        if (MainContent != null)
            ShowDashboard();
    }

    private void Invoices_Checked(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Invoices";
        MainContent.Content = new InvoicesView();
   
    }

    private void Suppliers_Checked(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Suppliers";
        MainContent.Content = new SuppliersView();
    }

    private void Alerts_Checked(object sender, RoutedEventArgs e)
    {
        PageTitle.Text = "Import Excel";
        MainContent.Content = new ExcelImportView();
    }

    private void ShowPlaceholder(string pageName)
    {
        PageTitle.Text = pageName;

        MainContent.Content = new TextBlock
        {
            Text = pageName + " page - Coming soon",
            FontSize = 22,
            Foreground = Brushes.SlateGray,
            VerticalAlignment = VerticalAlignment.Top
        };
    }
}
