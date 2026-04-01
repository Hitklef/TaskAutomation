namespace TaskAutomation.Application.Services;

public interface IRepositorySyncService
{
    Task<string> TrySyncAsync(CancellationToken cancellationToken);
}
