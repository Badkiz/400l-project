using Microsoft.AspNetCore.Mvc;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IPaymentService paymentService, ILogger<WebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("paystack")]
    public async Task<IActionResult> PaystackWebhook()
    {
        // Read raw body for HMAC verification
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault() ?? string.Empty;

        _logger.LogInformation("Paystack webhook received. Event payload length: {Len}", payload.Length);

        var success = await _paymentService.VerifyAndProcessWebhookAsync(payload, signature);

        // Always return 200 to Paystack (even on signature failure, to avoid retries for bad payloads)
        return Ok(new { received = true });
    }
}
