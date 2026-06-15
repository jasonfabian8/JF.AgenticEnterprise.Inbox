namespace JF.AgenticEnterprise.Application.Agents;

public sealed record ClassificationResult(
    string Category,
    float Confidence,
    string Reasoning);

public static class EmailCategory
{
    public const string Invoice = "Invoice";
    public const string Contract = "Contract";
    public const string CommercialProposal = "Commercial Proposal";
    public const string InformationRequest = "Information Request";
    public const string Marketing = "Marketing";
    public const string BankStatement = "Bank Statement";
    public const string Unknown = "Unknown";

    public static readonly IReadOnlyList<string> All =
    [
        Invoice, Contract, CommercialProposal,
        InformationRequest, Marketing, BankStatement, Unknown
    ];
}
