using Application.DTOs.Communication;
using Application.Interfaces.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Notification API

    [ApiController]
    [Route("api/notification")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("my/{userId}")]
        public async Task<IActionResult> GetMy(string userId)
        {
            var data = await _service.GetMyNotificationsAsync(userId);
            return Ok(data);
        }

        [HttpGet("unread-count/{userId}")]
        public async Task<IActionResult> GetUnreadCount(string userId)
        {
            var count = await _service.GetUnreadCountAsync(userId);
            return Ok(new { Count = count });
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] NotificationCreateDto dto)
        {
            var id = await _service.CreateBroadcastAsync(dto);
            return Ok(new { Message = "Notification Sent Successfully", Id = id });
        }

        [HttpPut("read/{recipientId}")]
        public async Task<IActionResult> MarkRead(string recipientId, [FromQuery] string userId)
        {
            var result = await _service.MarkReadAsync(recipientId, userId);
            return Ok(new { Success = result });
        }

        [HttpPut("read-all/{userId}")]
        public async Task<IActionResult> MarkAllRead(string userId)
        {
            var count = await _service.MarkAllReadAsync(userId);
            return Ok(new { Message = $"{count} notifications marked read" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok(new { Message = "Notification Deleted Successfully" });
        }
    }

    #endregion
}
