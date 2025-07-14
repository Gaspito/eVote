using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using VotingApp.Web.Migrations;
using VotingApp.Web.Models;

namespace VotingApp.Web.Controllers;

public class VoteController : Controller
{
    private readonly ILogger<VoteController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    private readonly IVoteService _voteService;
    private readonly IRoleService _roleService;

    private readonly int _userVoteLimit;

    public VoteController(IConfiguration config, ILogger<VoteController> logger, AppDbContext dbContext, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IVoteService voteService, IRoleService roleService)
    {
        _userVoteLimit = config.GetValue<int>("ServiceConfig:CastVoteLimit");
        _logger = logger;
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _voteService = voteService;
        _roleService = roleService;
    }

    /// <summary>
    /// Dashboard view for any user. Home page redirects to this when logged in.
    /// </summary>
    [Authorize(Roles = "Voter,Candidate,Admin")]
    public IActionResult Index()
    {
        ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.UserEmail = User.FindFirstValue(ClaimTypes.Email);
        ViewBag.UserRole = User.IsInRole("Voter") ? "Voter" : "Candidate";
        return View();
    }

    /// <summary>
    /// Returns a list of all candidates, sorted by vote count
    /// </summary>
    public async Task<IActionResult> Candidates()
    {
        var candidates = await _userManager.GetUsersInRoleAsync("Candidate");

        // Sort and extract info about the candidates
        var rankedCandidates = candidates
            .OrderByDescending(i => i.VoteCount)
            .Select(i => new
            {
                VoteCount = i.VoteCount,
                Email = i.Email,
                Id = i.Id,
            })
            .ToList();

        var totalVotes = await _dbContext.VoteTokens.CountAsync();

        // Include remaining votes info if user is a Voter
        var remainingUserVotes = 0;
        if (_signInManager.IsSignedIn(User) && User.IsInRole("Voter"))
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var voter = await _userManager.FindByEmailAsync(userEmail);
            if (voter != null)
                remainingUserVotes = _userVoteLimit - voter.VoteCount;
        }

        return Json(new
        {
            candidates = rankedCandidates,
            totalVotes = totalVotes,
            remainingUserVotes = remainingUserVotes,
        });
    }

    /// <summary>
    /// Returns a list of all votes of the user logged in
    /// </summary>
    [Authorize(Roles = "Voter")]
    public async Task<IActionResult> Votes()
    {
        var candidates = await _userManager.GetUsersInRoleAsync("Candidate");

        // Get votes cast by the user
        var voterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var votes = await _dbContext.VoteTokens
            .Where(i => i.VoterId == voterId)
            .ToListAsync();

        // Find candidate of each vote and extract their info
        var voteInfo = votes.Select(i => new
        {
            Candidate = candidates.First(c => c.Id == i.CandidateId)
        })
        .Select(i => new
        {
            CandidateEmail = i.Candidate.Email,
            CandidateId = i.Candidate.Id
        }).ToList();

        return Json(new
        {
            voterId = voterId,
            votes = voteInfo,
            remainingVoteCount = _userVoteLimit - voteInfo.Count,
            maxVoteCount = _userVoteLimit,
        });
    }

    /// <summary>
    /// Computes and returns the total number of possible votes, based on voters count,
    /// and the total number of cast votes so far.
    /// </summary>
    public async Task<IActionResult> Remaining()
    {
        var voters = await _userManager.GetUsersInRoleAsync("Voter");
        var votersCount = voters.Count();
        var totalVotes = votersCount * _userVoteLimit;
        var castVotes = voters.Sum(i => i.VoteCount);
        return Json(new
        {
            totalVotes = totalVotes,
            castVotes = castVotes,
            remainingVotes = totalVotes - castVotes,
        });
    }

    /// <summary>
    /// Returns the total number of voters that are registered in the app
    /// </summary>
    public async Task<IActionResult> Voters()
    {
        var voters = await _userManager.GetUsersInRoleAsync("Voter");
        var votersCount = voters.Count();
        return Json(new
        {
            totalVoters = votersCount,
        });
    }

    /// <summary>
    /// Returns the total votes for a specific candidate.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Count([FromBody] VoteRequestModel req)
    {
        var candidateEmail = req.Email;
        var user = await _dbContext.Users.FirstAsync(i => i.Email == candidateEmail);
        var count = user.VoteCount; // Using the cached count to go faster
        return Json(new
        {
            email = candidateEmail,
            votes = count
        });
    }


    /// <summary>
    /// Does actual vote count by counting each ballot (cast vote) for each candidate.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> FinalCount()
    {
        var candidates = await _userManager.GetUsersInRoleAsync("Candidate");

        // Gather vote count per candidate
        var votes = candidates
            .GroupJoin(_dbContext.VoteTokens, i => i.Id, j => j.CandidateId, (i, j) => new
            {
                VoteCount = j.Count(),
                CandidateId = i.Id,
                CandidateEmail = i.Email,
            })
            .OrderByDescending(i => i.VoteCount)
            .ToList();

        // Gather how many voters have used all their votes
        var completedVotersCount = await _dbContext.VoteTokens
            .GroupBy(i => i.VoterId)
            .Where(i => i.Count() == _userVoteLimit)
            .CountAsync();

        // Gather total number of voters
        var voters = await _userManager.GetUsersInRoleAsync("Voter");
        var votersCount = voters.Count;

        return Json(new
        {
            candidates = votes,
            remainingVoters = votersCount - completedVotersCount,
            totalVoters = votersCount,
            completedVoters = completedVotersCount,
        });
    }

    /// <summary>
    /// Requests a vote from the current user, if a voter,
    /// to the user identified by email, if a candidate.
    /// </summary>
    [Authorize(Roles = "Voter")]
    [HttpPost]
    public async Task<IActionResult> AddVote([FromBody] VoteRequestModel data)
    {   
        // Get current user's email
        var voterEmail = User.FindFirstValue(ClaimTypes.Email);
        if (voterEmail == null)
            return StatusCode(403, "Invalid user Email. Are you logged in?");

        // Get email from request body
        var candidateEmail = data.Email;
        if (candidateEmail == null)
            return StatusCode(403, "Invalid candidate Email");

        var result = await _voteService.TryAddVoteAsync(voterEmail, candidateEmail);

        if (result.Success)
            return Ok("Vote Added");
        else
            return StatusCode(500, result.Message);
    }


    /// <summary>
    /// Removes a vote from the current logged in voter to the candidate identified by email.
    /// </summary>
    [Authorize(Roles = "Voter")]
    [HttpDelete]
    public async Task<IActionResult> RemoveVote([FromBody] VoteRequestModel data)
    {   
        // Get current user's email
        var voterEmail = User.FindFirstValue(ClaimTypes.Email);
        if (voterEmail == null)
            return StatusCode(403, "Invalid user Email. Are you logged in?");

        // Get email from request body
        var candidateEmail = data.Email;
        if (candidateEmail == null)
            return StatusCode(403, "Invalid candidate Email");

        var result = await _voteService.TryRemoveVoteAsync(voterEmail, candidateEmail);

        if (result.Success)
            return Ok("Vote Renoved");
        else
            return StatusCode(500, result.Message);
    }


    /// <summary>
    /// Changes the role of a voter into a candidate, if possible.
    /// </summary>
    [Authorize(Roles = "Voter")]
    [HttpPut]
    public async Task<IActionResult> RegisterAsCandidate()
    {
        // Get user's email
        var voterEmail = User.FindFirstValue(ClaimTypes.Email);
        if (voterEmail == null)
            return BadRequest("Invalid Voter User");

        var result = await _roleService.SwitchToCandidate(voterEmail);
        if (!result.Success)
            return StatusCode(500, result.Message);

        // If switch was successful, force a refresh of their authorization cookies
        var user = await _userManager.FindByEmailAsync(voterEmail);
        if (user == null)
            return NotFound("User not found");

        // Refresh the user's security stamp
        await _userManager.UpdateSecurityStampAsync(user);

        // Sign in the user again to reissue the auth cookie
        await _signInManager.SignOutAsync(); // optional, clears current cookie
        await _signInManager.SignInAsync(user, isPersistent: false);

        return Ok();
    }


    /// <summary>
    /// Changes the role of a candidate into a voter, if possible.
    /// </summary>
    [Authorize(Roles = "Candidate")]
    [HttpPut]
    public async Task<IActionResult> RegisterAsVoter()
    {
        // Get candidate's email
        var candidateEmail = User.FindFirstValue(ClaimTypes.Email);
        if (candidateEmail == null)
            return NotFound("User email not Found");

        var result = await _roleService.SwitchToVoter(candidateEmail);
        if (!result.Success)
            return StatusCode(500, result.Message);

        // If switch was successful, force a refresh of their authorization cookies
        var user = await _userManager.FindByEmailAsync(candidateEmail);
        if (user == null)
            return NotFound("User not found");

        // Refresh the user's security stamp
        await _userManager.UpdateSecurityStampAsync(user);

        // Sign in the user again to reissue the auth cookie
        await _signInManager.SignOutAsync(); // optional, clears current cookie
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Redirect to dashboard
        return Ok();
    }
}
