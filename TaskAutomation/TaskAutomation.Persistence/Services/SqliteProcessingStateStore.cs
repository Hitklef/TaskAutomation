using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using TaskAutomation.Application.Models;
using TaskAutomation.Application.Services;
using TaskAutomation.Contracts.Messages;
using TaskAutomation.Domain.Models;
using TaskAutomation.Persistence.Configuration;

namespace TaskAutomation.Persistence.Services;

public sealed class SqliteProcessingStateStore(
    IOptions<PersistenceOptions> options,
    ITimeProvider timeProvider) : IProcessingStateStore
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS WebhookEvents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CorrelationId TEXT NOT NULL,
                WorkItemId INTEGER NOT NULL,
                EventType TEXT NOT NULL,
                Revision INTEGER NULL,
                Fingerprint TEXT NOT NULL UNIQUE,
                PayloadJson TEXT NOT NULL,
                ReceivedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS WorkItemStates (
                WorkItemId INTEGER PRIMARY KEY,
                LastObservedRevision INTEGER NOT NULL,
                LastProcessedRevision INTEGER NULL,
                Status INTEGER NOT NULL,
                Details TEXT NULL,
                LeaseCorrelationId TEXT NULL,
                LeaseExpiresAtUtc TEXT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProcessingAttempts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WorkItemId INTEGER NOT NULL,
                Revision INTEGER NOT NULL,
                CorrelationId TEXT NOT NULL,
                Status INTEGER NOT NULL,
                Details TEXT NULL,
                StartedAtUtc TEXT NOT NULL,
                FinishedAtUtc TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS AuditTrail (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CorrelationId TEXT NOT NULL,
                WorkItemId INTEGER NULL,
                Stage TEXT NOT NULL,
                Status INTEGER NOT NULL,
                Message TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RecordWebhookAsync(WorkItemWebhookMessage message, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO WebhookEvents (
                CorrelationId,
                WorkItemId,
                EventType,
                Revision,
                Fingerprint,
                PayloadJson,
                ReceivedAtUtc)
            VALUES (
                $correlationId,
                $workItemId,
                $eventType,
                $revision,
                $fingerprint,
                $payloadJson,
                $receivedAtUtc);
            """;

        command.Parameters.AddWithValue("$correlationId", message.CorrelationId);
        command.Parameters.AddWithValue("$workItemId", message.WorkItemId);
        command.Parameters.AddWithValue("$eventType", message.EventType);
        command.Parameters.AddWithValue("$revision", (object?)message.Revision ?? DBNull.Value);
        command.Parameters.AddWithValue("$fingerprint", message.Fingerprint);
        command.Parameters.AddWithValue("$payloadJson", message.PayloadJson);
        command.Parameters.AddWithValue("$receivedAtUtc", message.ReceivedAtUtc.UtcDateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ProcessingLeaseResult> TryAcquireLeaseAsync(
        int workItemId,
        int revision,
        string correlationId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.UtcNow;

        await using var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText =
            """
            SELECT LastProcessedRevision, LeaseExpiresAtUtc, Status
            FROM WorkItemStates
            WHERE WorkItemId = $workItemId;
            """;
        selectCommand.Parameters.AddWithValue("$workItemId", workItemId);

        int? lastProcessedRevision = null;
        DateTimeOffset? leaseExpiresAtUtc = null;
        ProcessingStatus? status = null;

        await using (var reader = await selectCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                lastProcessedRevision = reader.IsDBNull(0) ? null : reader.GetInt32(0);
                leaseExpiresAtUtc = reader.IsDBNull(1)
                    ? null
                    : DateTimeOffset.Parse(reader.GetString(1));
                status = (ProcessingStatus)reader.GetInt32(2);
            }
        }

        if (lastProcessedRevision is not null && lastProcessedRevision >= revision)
        {
            await transaction.CommitAsync(cancellationToken);
            return ProcessingLeaseResult.AlreadyCompleted("A newer or equal revision is already marked as processed.");
        }

        if (status == ProcessingStatus.InProgress && leaseExpiresAtUtc is not null && leaseExpiresAtUtc > now)
        {
            await transaction.CommitAsync(cancellationToken);
            return ProcessingLeaseResult.InProgress("Another worker instance is already processing this work item.");
        }

        await using var upsertCommand = connection.CreateCommand();
        upsertCommand.Transaction = transaction;
        upsertCommand.CommandText =
            """
            INSERT INTO WorkItemStates (
                WorkItemId,
                LastObservedRevision,
                LastProcessedRevision,
                Status,
                Details,
                LeaseCorrelationId,
                LeaseExpiresAtUtc,
                UpdatedAtUtc)
            VALUES (
                $workItemId,
                $revision,
                NULL,
                $status,
                $details,
                $leaseCorrelationId,
                $leaseExpiresAtUtc,
                $updatedAtUtc)
            ON CONFLICT(WorkItemId) DO UPDATE SET
                LastObservedRevision = excluded.LastObservedRevision,
                Status = excluded.Status,
                Details = excluded.Details,
                LeaseCorrelationId = excluded.LeaseCorrelationId,
                LeaseExpiresAtUtc = excluded.LeaseExpiresAtUtc,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        upsertCommand.Parameters.AddWithValue("$workItemId", workItemId);
        upsertCommand.Parameters.AddWithValue("$revision", revision);
        upsertCommand.Parameters.AddWithValue("$status", (int)ProcessingStatus.InProgress);
        upsertCommand.Parameters.AddWithValue("$details", "Processing lease acquired.");
        upsertCommand.Parameters.AddWithValue("$leaseCorrelationId", correlationId);
        upsertCommand.Parameters.AddWithValue("$leaseExpiresAtUtc", now.Add(leaseDuration).UtcDateTime.ToString("O"));
        upsertCommand.Parameters.AddWithValue("$updatedAtUtc", now.UtcDateTime.ToString("O"));
        await upsertCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var attemptCommand = connection.CreateCommand();
        attemptCommand.Transaction = transaction;
        attemptCommand.CommandText =
            """
            INSERT INTO ProcessingAttempts (
                WorkItemId,
                Revision,
                CorrelationId,
                Status,
                Details,
                StartedAtUtc,
                FinishedAtUtc)
            VALUES (
                $workItemId,
                $revision,
                $correlationId,
                $status,
                $details,
                $startedAtUtc,
                NULL);
            """;

        attemptCommand.Parameters.AddWithValue("$workItemId", workItemId);
        attemptCommand.Parameters.AddWithValue("$revision", revision);
        attemptCommand.Parameters.AddWithValue("$correlationId", correlationId);
        attemptCommand.Parameters.AddWithValue("$status", (int)ProcessingStatus.InProgress);
        attemptCommand.Parameters.AddWithValue("$details", "Processing attempt started.");
        attemptCommand.Parameters.AddWithValue("$startedAtUtc", now.UtcDateTime.ToString("O"));
        await attemptCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return ProcessingLeaseResult.LeaseAcquired();
    }

    public async Task<bool> HasProcessedRevisionAsync(int workItemId, int revision, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT LastProcessedRevision
            FROM WorkItemStates
            WHERE WorkItemId = $workItemId;
            """;
        command.Parameters.AddWithValue("$workItemId", workItemId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            return false;
        }

        return Convert.ToInt32(result) >= revision;
    }

    public async Task MarkCompletedAsync(
        int workItemId,
        int revision,
        string correlationId,
        ProcessingStatus status,
        string details,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.UtcNow;

        await using var updateStateCommand = connection.CreateCommand();
        updateStateCommand.Transaction = transaction;
        updateStateCommand.CommandText =
            """
            INSERT INTO WorkItemStates (
                WorkItemId,
                LastObservedRevision,
                LastProcessedRevision,
                Status,
                Details,
                LeaseCorrelationId,
                LeaseExpiresAtUtc,
                UpdatedAtUtc)
            VALUES (
                $workItemId,
                $revision,
                CASE WHEN $status = $successStatus THEN $revision ELSE NULL END,
                $status,
                $details,
                NULL,
                NULL,
                $updatedAtUtc)
            ON CONFLICT(WorkItemId) DO UPDATE SET
                LastObservedRevision = $revision,
                LastProcessedRevision = CASE
                    WHEN $status = $successStatus
                    THEN MAX(COALESCE(LastProcessedRevision, 0), $revision)
                    ELSE LastProcessedRevision
                END,
                Status = $status,
                Details = $details,
                LeaseCorrelationId = NULL,
                LeaseExpiresAtUtc = NULL,
                UpdatedAtUtc = $updatedAtUtc;
            """;

        updateStateCommand.Parameters.AddWithValue("$workItemId", workItemId);
        updateStateCommand.Parameters.AddWithValue("$revision", revision);
        updateStateCommand.Parameters.AddWithValue("$status", (int)status);
        updateStateCommand.Parameters.AddWithValue("$successStatus", (int)ProcessingStatus.Succeeded);
        updateStateCommand.Parameters.AddWithValue("$details", details);
        updateStateCommand.Parameters.AddWithValue("$updatedAtUtc", now.UtcDateTime.ToString("O"));
        await updateStateCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var updateAttemptCommand = connection.CreateCommand();
        updateAttemptCommand.Transaction = transaction;
        updateAttemptCommand.CommandText =
            """
            UPDATE ProcessingAttempts
            SET Status = $status,
                Details = $details,
                FinishedAtUtc = $finishedAtUtc
            WHERE WorkItemId = $workItemId
              AND CorrelationId = $correlationId
              AND FinishedAtUtc IS NULL;
            """;

        updateAttemptCommand.Parameters.AddWithValue("$workItemId", workItemId);
        updateAttemptCommand.Parameters.AddWithValue("$correlationId", correlationId);
        updateAttemptCommand.Parameters.AddWithValue("$status", (int)status);
        updateAttemptCommand.Parameters.AddWithValue("$details", details);
        updateAttemptCommand.Parameters.AddWithValue("$finishedAtUtc", now.UtcDateTime.ToString("O"));
        await updateAttemptCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AppendAuditAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO AuditTrail (
                CorrelationId,
                WorkItemId,
                Stage,
                Status,
                Message,
                CreatedAtUtc)
            VALUES (
                $correlationId,
                $workItemId,
                $stage,
                $status,
                $message,
                $createdAtUtc);
            """;

        command.Parameters.AddWithValue("$correlationId", entry.CorrelationId);
        command.Parameters.AddWithValue("$workItemId", (object?)entry.WorkItemId ?? DBNull.Value);
        command.Parameters.AddWithValue("$stage", entry.Stage);
        command.Parameters.AddWithValue("$status", (int)entry.Status);
        command.Parameters.AddWithValue("$message", entry.Message);
        command.Parameters.AddWithValue("$createdAtUtc", entry.CreatedAtUtc.UtcDateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
