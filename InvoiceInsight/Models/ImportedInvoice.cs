using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace InvoiceInsight.Models
{
    public class ImportedInvoice : INotifyPropertyChanged
    {
        private string _invoiceNumber = string.Empty;
        private string _supplierName = string.Empty;
        private DateTime? _invoiceDate;
        private decimal? _amount;
        private bool _isSelectedForImport;

        public int ExcelRowNumber { get; set; }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set
            {
                if (_invoiceNumber == value)
                    return;

                _invoiceNumber = value;
                OnPropertyChanged();
            }
        }

        public string SupplierName
        {
            get => _supplierName;
            set
            {
                if (_supplierName == value)
                    return;

                _supplierName = value;
                OnPropertyChanged();
            }
        }

        public DateTime? InvoiceDate
        {
            get => _invoiceDate;
            set
            {
                if (_invoiceDate == value)
                    return;

                _invoiceDate = value;
                OnPropertyChanged();
            }
        }

        public decimal? Amount
        {
            get => _amount;
            set
            {
                if (_amount == value)
                    return;

                _amount = value;
                OnPropertyChanged();
            }
        }

        public List<string> ValidationErrors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool IsSelectedForImport
        {
            get => _isSelectedForImport;
            set
            {
                if (!CanImport)
                    value = false;

                if (_isSelectedForImport == value)
                    return;

                _isSelectedForImport = value;
                OnPropertyChanged();
            }
        }

        public bool CanImport => ValidationErrors.Count == 0;

        public string ValidationStatus =>
            ValidationErrors.Count > 0 ? "Error" :
            Warnings.Count > 0 ? "Warning" :
            "Valid";

        public string ValidationDetails
        {
            get
            {
                var messages = ValidationErrors
                    .Select(message => "Error: " + message)
                    .Concat(Warnings.Select(message => "Warning: " + message));

                return string.Join(Environment.NewLine, messages);
            }
        }

        public string ValidationSummary
        {
            get
            {
                if (ValidationErrors.Count > 0)
                    return $"{ValidationErrors.Count} error(s)";

                if (Warnings.Count > 0)
                    return $"{Warnings.Count} warning(s)";

                return "No issues found";
            }
        }

        public void ClearValidation()
        {
            ValidationErrors.Clear();
            Warnings.Clear();

            if (!CanImport)
                IsSelectedForImport = false;

            NotifyValidationChanged();
        }

        public void AddError(string message)
        {
            ValidationErrors.Add(message);
            IsSelectedForImport = false;
            NotifyValidationChanged();
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
            NotifyValidationChanged();
        }

        private void NotifyValidationChanged()
        {
            OnPropertyChanged(nameof(CanImport));
            OnPropertyChanged(nameof(ValidationStatus));
            OnPropertyChanged(nameof(ValidationDetails));
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(IsSelectedForImport));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}