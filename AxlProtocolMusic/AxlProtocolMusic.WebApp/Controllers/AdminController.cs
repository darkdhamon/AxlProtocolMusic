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

    public AdminController(IChatbotBudgetService chatbotBudgetService)
    {
        _chatbotBudgetService = chatbotBudgetService;
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
}
