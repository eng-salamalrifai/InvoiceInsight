using System;
using System.Collections.Generic;
using System.Text;
using InvoiceInsight.Models;

namespace InvoiceInsight.Seeds;

public static class InvoiceSeed
{
    public static List<Invoice> GetInvoices(List<Supplier> suppliers)
    {
        var invoices = new List<Invoice>();

        var random = new Random(42);
        var today = DateTime.Today;

        for (int i = 1; i <= 60; i++)
        {
            var supplier = suppliers[random.Next(suppliers.Count)];

            invoices.Add(new Invoice
            {
                InvoiceNumber = $"INV-{i:0000}",
                SupplierId = supplier.Id,
                InvoiceDate = today.AddDays(-random.Next(0, 180)),
                Amount = random.Next(500, 15000)
                    + random.Next(0, 100) / 100m
            });
        }

        // High-value invoice
        invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-HIGH-001",
            SupplierId = suppliers[0].Id,
            InvoiceDate = today.AddDays(-5),
            Amount = 45000
        });

        // Duplicate invoices
        invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-DUP-001",
            SupplierId = suppliers[1].Id,
            InvoiceDate = today.AddDays(-10),
            Amount = 2500
        });

        invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-DUP-001",
            SupplierId = suppliers[1].Id,
            InvoiceDate = today.AddDays(-8),
            Amount = 2500
        });

        return invoices;
    }
}
