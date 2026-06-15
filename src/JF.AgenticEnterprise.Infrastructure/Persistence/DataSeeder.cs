using JF.AgenticEnterprise.Domain.Common;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly InboxDbContext _context;

    public DataSeeder(InboxDbContext context) => _context = context;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _context.Database.EnsureCreatedAsync(ct);

        if (await _context.TaxonomyCategories.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;

        var categories = new List<TaxonomyCategory>
        {
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "Invoice",
                Description = "Supplier invoices for payment",
                SignalsJson = """["invoice","due date","payment terms","amount due","vendor","bill","receipt"]""",
                Routing = "finance",
                SuggestedExtractionFieldsJson = """["vendorName","invoiceNumber","totalAmount","dueDate","poReference"]""",
                CreatedAt = now,
            },
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "Contract",
                Description = "Legal agreements and contracts",
                SignalsJson = """["agreement","contract","parties","whereas","witnesseth","governing law","term"]""",
                Routing = "legal",
                SuggestedExtractionFieldsJson = """["partyA","partyB","effectiveDate","expiryDate","agreementType"]""",
                CreatedAt = now,
            },
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "Proposal",
                Description = "Commercial proposals and quotations",
                SignalsJson = """["proposal","quotation","quote","pricing","offer","bid","rfq"]""",
                Routing = "sales",
                SuggestedExtractionFieldsJson = """["vendorName","totalAmount","validUntil"]""",
                CreatedAt = now,
            },
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "Information Request",
                Description = "Requests for information or documentation",
                SignalsJson = """["requesting","please provide","could you","information","documentation","inquiry"]""",
                Routing = "operations",
                CreatedAt = now,
            },
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "Marketing",
                Description = "Marketing and promotional emails",
                SignalsJson = """["sale","discount","promotion","newsletter","unsubscribe","offer expires","limited time"]""",
                Routing = "spam_filter",
                CreatedAt = now,
            },
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "Bank Statement",
                Description = "Bank and financial account statements",
                SignalsJson = """["statement","account number","balance","transactions","bank","financial institution"]""",
                Routing = "finance",
                SuggestedExtractionFieldsJson = """["accountNumber","periodStart","periodEnd","closingBalance"]""",
                CreatedAt = now,
            },
            new()
            {
                Id = UlidGenerator.NewUlid(),
                Label = "UNKNOWN",
                Description = "Unclassified email requiring review",
                SignalsJson = "[]",
                Routing = "operations",
                CreatedAt = now,
            },
        };

        _context.TaxonomyCategories.AddRange(categories);
        await _context.SaveChangesAsync(ct);
    }
}
