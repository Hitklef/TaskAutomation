using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface IRepositoryContextProvider
{
    Task<RepositoryContextSnapshot> GetContextAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken);
}
