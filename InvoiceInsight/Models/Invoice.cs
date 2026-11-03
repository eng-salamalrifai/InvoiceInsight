using System;
using System.Collections.Generic;
using System.Text;

namespace InvoiceInsight.Models;

public class Invoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public DateTime InvoiceDate { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Supplier Supplier { get; set; } = null!;

}
