﻿using Microsoft.AspNetCore.Authorization;
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


    }
}