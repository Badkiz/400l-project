using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HostelMS.DTOs;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Message cannot be empty." });

        try
        {
            var message = await _messageService.SendMessageAsync(CurrentUserId, request);
            return Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversation/{partnerId}")]
    public async Task<IActionResult> GetConversation(int partnerId)
    {
        var messages = await _messageService.GetConversationAsync(CurrentUserId, partnerId);
        await _messageService.MarkAsReadAsync(partnerId, CurrentUserId);
        return Ok(messages);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _messageService.GetConversationsAsync(CurrentUserId);
        return Ok(conversations);
    }

    [HttpPost("read/{senderId}")]
    public async Task<IActionResult> MarkAsRead(int senderId)
    {
        await _messageService.MarkAsReadAsync(senderId, CurrentUserId);
        return Ok(new { message = "Messages marked as read." });
    }
}
