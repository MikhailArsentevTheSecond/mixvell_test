namespace WebApp;

public class SearchService : ISearchService
{
    private readonly IProviderOneService _firstService;
    private readonly IProviderTwoService _secondService;
    private readonly ISearchServiceCache _searchCache;

    
    public SearchService(IProviderOneService firstService, IProviderTwoService secondService, ISearchServiceCache searchCache)
    {
        _firstService = firstService;
        _secondService = secondService;
        _searchCache = searchCache;
    }
    
    public async Task<SearchResponse> Search(SearchRequest request)
    {
        var cachedRoutes = _searchCache.Get(request);
        if (request.Filters?.OnlyCached == true && cachedRoutes == null)
        {
            return new SearchResponse([]);
        }

        if (cachedRoutes == null)
        {
            var result =await SearchRequest(request);
            _searchCache.Set(request, result);
            return result;
        }
        else
        {
            return new SearchResponse(cachedRoutes);
        }
    }

    private async Task<SearchResponse> SearchRequest(SearchRequest request)
    {
        if (request.Filters?.OnlyCached == true)
        {
            return new SearchResponse([]);
        }
        var result = await Task.WhenAll(
            AggregateProviderOne(_firstService.Search(request)), 
            AggregateProviderTwo(_secondService.Search(request)));
            
        var flattenResult = result.SelectMany(x => x)
            .Where(x => request.Filters == null || request.Filters.IsCorrectRoute(x))
            .ToArray();
        var aggregatedResult = new SearchResponse(flattenResult);
        return aggregatedResult;
    }
    
    public async ValueTask<bool> PingAsync()
    {
        var result = await Task.WhenAll(
              _firstService.Ping().AsTask(), 
              _secondService.Ping().AsTask());
        
        return result.Any(x => x);
    }
    
    private async Task<Route[]> AggregateProviderOne(Task<ProviderOneSearchResponse> response)
    {
        var wait = await response;
        return wait.Routes.Select(x => x.ToAggregatedResponse()).ToArray();
    }

    private async Task<Route[]> AggregateProviderTwo(Task<ProviderTwoSearchResponse> response)
    {
        var wait = await response;
        return wait.Routes.Select(x => x.ToAggregatedResponse()).ToArray();
    }

}
