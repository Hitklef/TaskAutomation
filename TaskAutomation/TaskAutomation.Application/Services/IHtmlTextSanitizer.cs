namespace TaskAutomation.Application.Services;

public interface IHtmlTextSanitizer
{
    string Sanitize(string? html);
}
