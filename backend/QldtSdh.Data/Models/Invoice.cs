using System;
using System.Collections.Generic;

namespace QldtSdh.Data.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int StudentId { get; set; }
        public string Semester { get; set; } = string.Empty; // e.g., HK1_2025_2026
        public string InvoiceNo { get; set; } = string.Empty; // e.g., INV-2026-001
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty; // e.g., Draft, Issued, PartiallyPaid, Paid
        public DateTime DueDate { get; set; }

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
