
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using InvoiceInsight.Models;

namespace InvoiceInsight.Data;

public class AppDbContext : DbContext
{
    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<Invoice> Invoices { get; set; }


    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string connectionString =
                "server=localhost;port=3306;database=invoice_insight_db;user=root;password=;";

            optionsBuilder.UseMySQL(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Supplier configuration
        modelBuilder.Entity<Supplier>()
            .Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Name);

        // Invoice configuration
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Supplier)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.InvoiceNumber)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Invoice>()
            .Property(i => i.Amount)
            .HasPrecision(18, 2);

    }
}