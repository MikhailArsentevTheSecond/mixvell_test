using Microsoft.AspNetCore.Mvc;

namespace WebApp;

[ApiController]
[Route("/v1/aggregateSearch")]
public class SearchServiceController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchServiceController(ISearchService searchService)
    {
        _searchService = searchService;
    }
    
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody]SearchRequest request)
    {
        if (ModelState.IsValid)
        {
            return Ok(await _searchService.Search(request));
        }
        else
        {
            return BadRequest(ModelState);
        }
    }

    [HttpGet("ping")]
    public async Task<bool> Ping()
    {
        return await _searchService.PingAsync();
    }
}