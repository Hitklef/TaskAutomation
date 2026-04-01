namespace TaskAutomation.Domain.Models;

public sealed record TaskCommentPayload(
    string Body,
    string Signature);
