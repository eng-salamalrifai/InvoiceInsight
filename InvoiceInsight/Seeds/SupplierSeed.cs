using System;
using System.Collections.Generic;
using System.Text;
using InvoiceInsight.Models;

namespace InvoiceInsight.Seeds;

public static class SupplierSeed
{
    public static List<Supplier> GetSuppliers()
    {
        return new List<Supplier>
        {
            new() { Name = "Global Supplies", Email = "global@example.com", Phone = "111111111" },
            new() { Name = "Tech Solutions", Email = "tech@example.com", Phone = "222222222" },
            new() { Name = "Office World", Email = "office@example.com", Phone = "333333333" },
            new() { Name = "Modern Equipment", Email = "modern@example.com", Phone = "444444444" },
            new() { Name = "Digital Services", Email = "digital@example.com", Phone = "555555555" },
            new() { Name = "Smart Trading",  Email = "smart@example.com", Phone = "666666666" },
            new() { Name = "Prime Solutions", Email = "prime@example.com", Phone = "777777777" },
            new() { Name = "Future Systems", Email = "future@example.com", Phone = "888888888" }
        };
    }
}