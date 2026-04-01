using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface ICommentFormatter
{
    TaskCommentPayload Build(WorkItemSnapshot workItem, WorkItemAnalysisResult analysisResult);
}
