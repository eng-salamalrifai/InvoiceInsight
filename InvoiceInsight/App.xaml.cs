using System.Configuration;
using System.Windows;
using InvoiceInsight.Data;
using InvoiceInsight.Services;
using Microsoft.EntityFrameworkCore;

namespace InvoiceInsight;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            using var db = new AppDbContext();

            var initializer = new DatabaseInitializer(db);

            await initializer.InitializeAsync();

            MessageBox.Show(
                "Database initialized successfully!",
                "InvoiceInsight",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}