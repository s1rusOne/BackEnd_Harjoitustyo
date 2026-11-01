using Harjoitustyö.Models;
using Harjoitustyö.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Harjoitustyö.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        private string CurrentUserName => User.Identity!.Name!;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessages([FromQuery] int count = 20)
        {
            count = Math.Clamp(count, 1, 100);
            return Ok(await _messageService.GetPublicMessagesAsync(count));
        }

        [HttpGet("inbox")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetInbox()
        {
            return Ok(await _messageService.GetReceivedMessagesAsync(CurrentUserName));
        }

        [HttpGet("sent")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetSent()
        {
            return Ok(await _messageService.GetSentMessagesAsync(CurrentUserName));
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<MessageDTO>> GetMessage(long id)
        {
            MessageDTO? message = await _messageService.GetMessageAsync(id, CurrentUserName);
            if (message == null)
            {
                return NotFound();
            }
            return message;
        }

        [HttpGet("{id:long}/replies")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetReplies(long id)
        {
            var result = await _messageService.GetRepliesAsync(id, CurrentUserName);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return Ok(result.Value);
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> PostMessage(CreateMessageDTO message)
        {
            var result = await _messageService.NewMessageAsync(message, CurrentUserName);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return CreatedAtAction(nameof(GetMessage), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<MessageDTO>> PutMessage(long id, UpdateMessageDTO message)
        {
            var result = await _messageService.UpdateMessageAsync(id, message, CurrentUserName);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return result.Value!;
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteMessage(long id)
        {
            var result = await _messageService.DeleteMessageAsync(id, CurrentUserName);
            if (result.Status != ServiceStatus.Success)
            {
                return this.ErrorResult(result);
            }
            return NoContent();
        }
    }
}
