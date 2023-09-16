using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Minimal_chat_application.Context;
using Minimal_chat_application.Model;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Minimal_chat_application.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Add the Authorize attribute to protect this controller
    public class MessageController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public MessageController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("SendMessages")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageModel sendMessageModel)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(senderId))
            {
                return Unauthorized(new { error = "Unauthorized access" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            // Check if the receiver exists
            var receiver = await _userManager.FindByIdAsync(sendMessageModel.ReceiverId);
            if (receiver == null)
            {
                return BadRequest(new { error = "Receiver user not found" });
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = sendMessageModel.ReceiverId,
                Content = sendMessageModel.Content,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                messageId = message.Id,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                timestamp = message.Timestamp
            });
        }

        //Edit message with messageId
        [HttpPost("EditMessage/{messageId}")]
        [Authorize]

        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageModel editMessageModel)
        {

            // Check if the message with the given messageId exists.
            var message = await _context.Messages.FindAsync(messageId);
            var loginUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (message.SenderId != loginUserId)
            {
                return Unauthorized(new { error = "Unauthorized access - Try to edit message send by you not others" });
            }

            if (message == null)
            {
                return NotFound(new { error = "Message not found" });
            }

            // Update the message content.
            message.Content = editMessageModel.Content;

            // Save the changes to the database.
            await _context.SaveChangesAsync();

            return Ok(new
            {
                messageId = message.Id,
                content = message.Content,
                timestamp = message.Timestamp
            });
        }

        //Delete message
        [HttpDelete("DeleteMessage/{messageId}")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            // Your code here to fetch and delete the message with the given messageId.

            // Check if the message with the given messageId exists.
            var message = await _context.Messages.FindAsync(messageId);

            var loginUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (message.SenderId != loginUserId)
            {
                return Unauthorized(new { error = "Unauthorized access - Try to delete message send by you not others" });
            }

            if (message == null)
            {
                return NotFound(new { error = "Message not found" });
            }

            // Check if the user making the request is the sender of the message.
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId != message.SenderId)
            {
                return Unauthorized(new { error = "Unauthorized access" });
            }

            // Remove the message from the database.
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                messageDeleted = true
            });
        }


    }
}
