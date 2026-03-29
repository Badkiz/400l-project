using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using HostelMS.DTOs;
using Microsoft.AspNetCore.Mvc;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var result = await _paymentService.InitiatePaymentAsync(userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyPayments()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var payments = await _paymentService.GetUserPaymentsAsync(userId);
        return Ok(payments);
    }

    [HttpGet("reference/{reference}")]
    [Authorize]
    public async Task<IActionResult> GetByReference(string reference)
    {
        var payment = await _paymentService.GetPaymentByReferenceAsync(reference);
        if (payment == null) return NotFound(new { message = "Payment not found." });
        return Ok(payment);
    }
    // Mock-only endpoint: called by paystack-mock.html to confirm payment without real Paystack
    [HttpPost("mock-confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> MockConfirm([FromBody] MockConfirmRequest request)
    {
        var success = await _paymentService.ConfirmMockPaymentAsync(request.Reference);
        if (!success) return NotFound(new { message = "Payment reference not found." });
        return Ok(new { message = "Payment confirmed.", reference = request.Reference });
    }

}
