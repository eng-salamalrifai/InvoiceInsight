using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using InvoiceInsight.Services;
using InvoiceInsight.ViewModels;
using Microsoft.Win32;

namespace InvoiceInsight.Views
{
    public partial class ExcelImportView : UserControl
    {
        private readonly ExcelImportViewModel _viewModel;
        private string? _selectedFilePath;

        public ExcelImportView()
        {
            InitializeComponent();

            _viewModel = new ExcelImportViewModel(
                new ExcelImportService());

            DataContext = _viewModel;
        }

        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Excel File",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            _selectedFilePath = dialog.FileName;

            _viewModel.FileName =
                System.IO.Path.GetFileName(dialog.FileName);
        }

        private void ReadFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedFilePath))
            {
                MessageBox.Show(
                    "Please choose an Excel file first.",
                    "No File Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                _viewModel.LoadInvoices(_selectedFilePath);

                MessageBox.Show(
                    $"Read {_viewModel.TotalInvoices} invoice(s)." +
                    Environment.NewLine +
                    $"Valid: {_viewModel.ValidCount} | " +
                    $"Warnings: {_viewModel.WarningCount} | " +
                    $"Errors: {_viewModel.ErrorCount}",
                    "Excel Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to read or validate the Excel file." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "Excel Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void InvoicesGrid_RowEditEnding(
            object sender,
            DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    try
                    {
                        _viewModel.RevalidateInvoices();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Unable to revalidate the edited invoices." +
                            Environment.NewLine + Environment.NewLine +
                            ex.Message,
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }));
        }

        private void SaveInvoices_Click(object sender, RoutedEventArgs e)
        {
            // Commit any active cell and row edits first.
            InvoicesGrid.CommitEdit(
                DataGridEditingUnit.Cell, true);

            InvoicesGrid.CommitEdit(
                DataGridEditingUnit.Row, true);

            try
            {
                // Revalidate all rows before saving.
                _viewModel.RevalidateInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to validate the invoices before saving." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (_viewModel.SelectedCount == 0)
            {
                MessageBox.Show(
                    "Please select the invoices you want to import.",
                    "No Invoices Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            string message =
                $"You are about to import {_viewModel.SelectedCount} invoice(s).";

            if (_viewModel.SelectedWarningCount > 0)
            {
                message += Environment.NewLine + Environment.NewLine +
                    $"{_viewModel.SelectedWarningCount} selected invoice(s) have warnings." +
                    Environment.NewLine +
                    "Please confirm that you have reviewed and accepted these warnings.";
            }

            message += Environment.NewLine + Environment.NewLine +
                "Do you want to continue?";

            var confirmation = MessageBox.Show(
                message,
                "Confirm Invoice Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                int savedCount = _viewModel.SaveSelectedInvoices();

                MessageBox.Show(
                    $"Successfully imported {savedCount} invoice(s)." +
                    Environment.NewLine +
                    "Suppliers were created when they did not already exist.",
                    "Import Completed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to save the selected invoices." +
                    Environment.NewLine + Environment.NewLine +
                    ex.Message,
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}