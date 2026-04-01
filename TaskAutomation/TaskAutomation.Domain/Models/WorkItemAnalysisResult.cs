namespace TaskAutomation.Domain.Models;

public sealed record WorkItemAnalysisResult(
    string Summary,
    string ImplementationNotes,
    IReadOnlyList<string> ImpactedAreas,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> SuggestedTests,
    string Provider,
    string? RawResponse)
{
    public bool IsMeaningful()
        => !string.IsNullOrWhiteSpace(Summary)
           && (Summary.Length + ImplementationNotes.Length) >= 80;
}
