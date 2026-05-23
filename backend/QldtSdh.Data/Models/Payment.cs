using System;

namespace QldtSdh.Data.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public string PaymentNo { get; set; } = string.Empty; // e.g., PAY-2026-001
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string Method { get; set; } = string.Empty; // e.g., BankTransfer, Cash, Refund

        // Navigation properties
        public virtual Invoice? Invoice { get; set; }
    }
}
