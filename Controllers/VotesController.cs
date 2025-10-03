using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Project_Version1.Services;
using Project_Version1.DTOs;

namespace Project_Version1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly VoteService _voteService;

        public VotesController(VoteService voteService) => _voteService = voteService;

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Vote([FromBody] VoteDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _voteService.VoteAsync(dto, userId);
            if (!ok) return BadRequest(new { message = "Invalid vote request" });
            return Ok(new { message = "Vote processed successfully" });
        }

        // Get vote counts for a specific comment
        [HttpGet("comment/{commentId}/counts")]
        public async Task<IActionResult> GetCommentVoteCounts(int commentId)
        {
            try
            {
                var (upvotes, downvotes) = await _voteService.GetCommentVoteCountsAsync(commentId);
                return Ok(new { upvotes, downvotes });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Get user's vote on a comment (requires auth)
        [Authorize]
        [HttpGet("comment/{commentId}/user-vote")]
        public async Task<IActionResult> GetUserVoteOnComment(int commentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userVote = await _voteService.GetUserVoteOnCommentAsync(commentId, userId);
                return Ok(new { vote = userVote });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Convenience endpoint for comment upvote
        [Authorize]
        [HttpPost("comment/{commentId}/upvote")]
        public async Task<IActionResult> UpvoteComment(int commentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var dto = new VoteDto { CommentId = commentId, VoteType = "upvote" };
                var ok = await _voteService.VoteAsync(dto, userId);

                if (!ok) return BadRequest(new { message = "Failed to process upvote" });

                // Return updated vote counts
                var (upvotes, downvotes) = await _voteService.GetCommentVoteCountsAsync(commentId);
                return Ok(new { upvotes, downvotes, userVote = "upvote" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Convenience endpoint for comment downvote
        [Authorize]
        [HttpPost("comment/{commentId}/downvote")]
        public async Task<IActionResult> DownvoteComment(int commentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var dto = new VoteDto { CommentId = commentId, VoteType = "downvote" };
                var ok = await _voteService.VoteAsync(dto, userId);

                if (!ok) return BadRequest(new { message = "Failed to process downvote" });

                // Return updated vote counts
                var (upvotes, downvotes) = await _voteService.GetCommentVoteCountsAsync(commentId);
                return Ok(new { upvotes, downvotes, userVote = "downvote" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}