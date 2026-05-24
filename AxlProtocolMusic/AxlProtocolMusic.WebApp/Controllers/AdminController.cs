using System.Text;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AxlProtocolMusic.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Roles = "Admin")]
[Route("admin")]
public sealed class AdminController : Controller
{
    private readonly IChatbotBudgetService _chatbotBudgetService;
    private readonly IChatbotConversationLogService _chatbotConversationLogService;

    public AdminController(
        IChatbotBudgetService chatbotBudgetService,
        IChatbotConversationLogService chatbotConversationLogService)
    {
        _chatbotBudgetService = chatbotBudgetService;
        _chatbotConversationLogService = chatbotConversationLogService;
    }

    [HttpPost("chatbot/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetChatbotBudget(CancellationToken cancellationToken)
    {
        await _chatbotBudgetService.ResetAsync(cancellationToken);
        return Redirect("/admin?chatbotReset=true");
    }

    [HttpPost("chatbot/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableChatbot(CancellationToken cancellationToken)
    {
        await _chatbotBudgetService.SetManualDisabledAsync(true, cancellationToken);
        return Redirect("/admin?chatbotOverrideChanged=true");
    }

    [HttpPost("chatbot/enable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableChatbot(CancellationToken cancellationToken)
    {
        await _chatbotBudgetService.SetManualDisabledAsync(false, cancellationToken);
        return Redirect("/admin?chatbotOverrideChanged=true");
    }

    [HttpGet("chatbot/messages.csv")]
    public async Task<IActionResult> DownloadChatbotMessagesCsv(CancellationToken cancellationToken)
    {
        var entries = await _chatbotConversationLogService.GetExportAsync(cancellationToken: cancellationToken);
        var csv = BuildChatbotMessagesCsv(entries);
        var fileName = $"chatbot-messages-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    private static string BuildChatbotMessagesCsv(IReadOnlyList<Models.Chatbot.ChatbotConversationLogEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("CreatedAtUtc,Outcome,PagePath,PageTitle,UserMessage,AssistantReply");

        foreach (var entry in entries)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(entry.CreatedAtUtc.ToString("O")),
                EscapeCsv(entry.Outcome),
                EscapeCsv(entry.PagePath),
                EscapeCsv(entry.PageTitle),
                EscapeCsv(entry.UserMessage),
                EscapeCsv(entry.AssistantReply)));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        var normalized = value ?? string.Empty;
        var escaped = normalized.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
