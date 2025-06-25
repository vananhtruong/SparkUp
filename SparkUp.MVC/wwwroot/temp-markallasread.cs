[HttpPost]
[Route("MarkAllAsRead/{chatRoomId}")]
[Route("Chat/MarkAllAsRead/{chatRoomId}")]
public async Task<IActionResult> MarkAllAsRead(int chatRoomId)
{
    var userId = GetCurrentUserId();
    
    // Check if user is authenticated
    if (userId == 0)
    {
        return Unauthorized("User not authenticated");
    }
    
    // Check if user can access this chat room
    var canAccess = await _chatService.CanUserAccessChatAsync(chatRoomId, userId);
    if (!canAccess)
    {
        return Forbid();
    }
    
    try
    {
        await _chatService.MarkAllMessagesAsReadAsync(chatRoomId, userId);
        return Ok(new { success = true });
    }
    catch (Exception ex)
    {
        return BadRequest($"Error marking messages as read: {ex.Message}");
    }
}
