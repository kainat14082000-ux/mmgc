using MMGC.Models;
using MMGC.Repositories;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class TransactionService : ITransactionService
{
    private readonly IRepository<Transaction> _repository;
    private readonly ApplicationDbContext _context;

    public TransactionService(IRepository<Transaction> repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetAllTransactionsAsync()
    {
        return await _context.Transactions
            .Include(t => t.Patient)
            .Include(t => t.Appointment)
            .Include(t => t.Procedure)
            .Include(t => t.LabTest)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        return await _context.Transactions
            .Include(t => t.Patient)
            .Include(t => t.Appointment)
            .Include(t => t.Procedure)
            .Include(t => t.LabTest)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        transaction.CreatedDate = DateTime.Now;
        if (transaction.TransactionDate == default)
        {
            transaction.TransactionDate = DateTime.Now;
        }
        // Use context directly to avoid navigation property issues
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        // Use context directly to avoid navigation property issues
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTransactionAsync(int id)
    {
        var transaction = await _repository.GetByIdAsync(id);
        if (transaction != null)
        {
            await _repository.DeleteAsync(transaction);
        }
    }

    public async Task<string> GenerateInvoiceAsync(int transactionId)
    {
        var transaction = await GetTransactionByIdAsync(transactionId);
        if (transaction == null)
            throw new Exception("Transaction not found");

        // Create invoices directory if it doesn't exist
        var invoicesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "invoices");
        if (!Directory.Exists(invoicesFolder))
        {
            Directory.CreateDirectory(invoicesFolder);
        }

        // Generate invoice file name (HTML format that can be printed as PDF)
        var fileName = $"invoice_{transactionId}_{DateTime.Now:yyyyMMddHHmmss}.html";
        var filePath = Path.Combine(invoicesFolder, fileName);
        var relativePath = $"/invoices/{fileName}";

        // Get related records for itemized charges
        var appointment = transaction.Appointment;
        var procedure = transaction.Procedure;
        var labTest = transaction.LabTest;
        
        var charges = new List<(string Description, decimal Amount)>();
        
        if (appointment != null)
        {
            charges.Add(($"Appointment #{appointment.Id} - {appointment.AppointmentDate:dd MMM yyyy} - Dr. {appointment.Doctor?.FullName ?? "N/A"}", appointment.ConsultationFee));
        }
        
        if (procedure != null)
        {
            charges.Add(($"Procedure: {procedure.ProcedureName}", procedure.ProcedureFee));
        }
        
        if (labTest != null)
        {
            charges.Add(($"Lab Test: {labTest.TestName}", labTest.TestFee));
        }
        
        // If no specific charges, use description
        if (charges.Count == 0 && !string.IsNullOrEmpty(transaction.Description))
        {
            charges.Add((transaction.Description, transaction.Amount));
        }
        
        // Build itemized charges HTML
        var chargesHtml = "";
        if (charges.Count > 0)
        {
            foreach (var charge in charges)
            {
                chargesHtml += $@"
                <div class=""charge-row"">
                    <div class=""charge-description"">{charge.Description}</div>
                    <div class=""charge-amount"">PKR {charge.Amount:N2}</div>
                </div>";
            }
        }
        else
        {
            chargesHtml = $@"
                <div class=""charge-row"">
                    <div class=""charge-description"">{transaction.Description}</div>
                    <div class=""charge-amount"">PKR {transaction.Amount:N2}</div>
                </div>";
        }

        // Create HTML invoice that can be opened in browser and printed as PDF
        var invoiceContent = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invoice #{transactionId:00000}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            padding: 40px;
            background: #f5f5f5;
        }}
        .invoice-container {{
            max-width: 800px;
            margin: 0 auto;
            background: white;
            padding: 40px;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{
            border-bottom: 3px solid #667eea;
            padding-bottom: 20px;
            margin-bottom: 30px;
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }}
        .hospital-info {{
            flex: 1;
        }}
        .hospital-name {{
            color: #667eea;
            font-size: 28px;
            margin-bottom: 10px;
        }}
        .hospital-details {{
            color: #666;
            font-size: 14px;
            line-height: 1.6;
        }}
        .hospital-details p {{
            margin: 4px 0;
        }}
        .invoice-info {{
            text-align: right;
        }}
        .invoice-info h2 {{
            color: #667eea;
            font-size: 24px;
            margin-bottom: 10px;
        }}
        .invoice-number {{
            color: #666;
            font-size: 14px;
        }}
        .invoice-date {{
            color: #666;
            font-size: 14px;
        }}
        .charges-table {{
            margin-top: 15px;
        }}
        .charge-row {{
            display: flex;
            justify-content: space-between;
            padding: 12px;
            border-bottom: 1px solid #f0f0f0;
            background: #fafafa;
            margin-bottom: 8px;
            border-radius: 4px;
        }}
        .charge-description {{
            flex: 1;
            color: #333;
            font-weight: 500;
        }}
        .charge-amount {{
            color: #28a745;
            font-weight: bold;
            min-width: 120px;
            text-align: right;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            color: #333;
            font-size: 18px;
            font-weight: bold;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 2px solid #eee;
        }}
        .hospital-info {{
            flex: 1;
        }}
        .hospital-name {{
            color: #667eea;
            font-size: 28px;
            margin-bottom: 10px;
        }}
        .hospital-details {{
            color: #666;
            font-size: 14px;
            line-height: 1.6;
        }}
        .hospital-details p {{
            margin: 4px 0;
        }}
        .invoice-info {{
            text-align: right;
        }}
        .invoice-info h2 {{
            color: #667eea;
            font-size: 24px;
            margin-bottom: 10px;
        }}
        .charges-table {{
            margin-top: 15px;
        }}
        .charge-row {{
            display: flex;
            justify-content: space-between;
            padding: 12px;
            border-bottom: 1px solid #f0f0f0;
            background: #fafafa;
            margin-bottom: 8px;
            border-radius: 4px;
        }}
        .charge-description {{
            flex: 1;
            color: #333;
            font-weight: 500;
        }}
        .charge-amount {{
            color: #28a745;
            font-weight: bold;
            min-width: 120px;
            text-align: right;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-label {{
            color: #666;
            font-weight: 500;
        }}
        .info-value {{
            color: #333;
            font-weight: 600;
        }}
        .amount-section {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin-top: 30px;
        }}
        .total-amount {{
            font-size: 28px;
            color: #28a745;
            font-weight: bold;
            text-align: right;
            margin-top: 10px;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 2px solid #eee;
            text-align: center;
            color: #999;
            font-size: 12px;
        }}
        @media print {{
            body {{
                background: white;
                padding: 0;
            }}
            .invoice-container {{
                box-shadow: none;
                padding: 20px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""invoice-container"">
        <div class=""header"">
            <div class=""hospital-info"">
                <h1 class=""hospital-name"">MMGC Hospital</h1>
                <div class=""hospital-details"">
                    <p>123 Medical Center Road, Healthcare District</p>
                    <p>Karachi, Pakistan</p>
                    <p>Phone: +92-300-1234567 | Email: info@mmgc.com</p>
                </div>
            </div>
            <div class=""invoice-info"">
                <h2>INVOICE</h2>
                <div class=""invoice-number"">Invoice #: INV-{transactionId:00000}</div>
                <div class=""invoice-date"">Date: {DateTime.Now:dd MMM yyyy HH:mm}</div>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Patient Information</div>
            <div class=""info-row"">
                <span class=""info-label"">Patient Name:</span>
                <span class=""info-value"">{transaction.Patient?.FullName ?? "N/A"}</span>
            </div>
            <div class=""info-row"">
                <span class=""info-label"">MR Number:</span>
                <span class=""info-value"">{transaction.Patient?.MRNumber ?? "N/A"}</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Charges Breakdown</div>
            <div class=""charges-table"">
                {chargesHtml}
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Payment Information</div>
            <div class=""info-row"">
                <span class=""info-label"">Transaction Type:</span>
                <span class=""info-value"">{transaction.TransactionType}</span>
            </div>
            <div class=""info-row"">
                <span class=""info-label"">Payment Mode:</span>
                <span class=""info-value"">{transaction.PaymentMode}</span>
            </div>
            <div class=""info-row"">
                <span class=""info-label"">Status:</span>
                <span class=""info-value"">{transaction.Status}</span>
            </div>
            <div class=""info-row"">
                <span class=""info-label"">Transaction Date:</span>
                <span class=""info-value"">{transaction.TransactionDate:dd MMM yyyy HH:mm}</span>
            </div>
            {(string.IsNullOrEmpty(transaction.ReferenceNumber) ? "" : $@"<div class=""info-row"">
                <span class=""info-label"">Reference Number:</span>
                <span class=""info-value"">{transaction.ReferenceNumber}</span>
            </div>")}
        </div>

        <div class=""amount-section"">
            <div class=""info-row"">
                <span class=""info-label"">Total Amount:</span>
            </div>
            <div class=""total-amount"">PKR {transaction.Amount:N2}</div>
        </div>

        <div class=""footer"">
            <p>This is a computer-generated invoice. No signature required.</p>
            <p>Generated on: {DateTime.Now:dd MMM yyyy HH:mm}</p>
        </div>
    </div>
</body>
</html>";

        // Write invoice content to file
        await File.WriteAllTextAsync(filePath, invoiceContent);

        // Update transaction
        transaction.InvoiceGenerated = true;
        transaction.InvoicePath = relativePath;
        
        // Use context directly
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();

        return relativePath;
    }
}
