namespace TaskAutomation.Application.Services;

public interface IWorkItemQueryService
{
    Task<IReadOnlyList<int>> QueryTodoCandidateIdsAsync(CancellationToken cancellationToken);
}
