namespace WebApp;

public interface ISearchService
{
    public Task<SearchResponse> Search(SearchRequest request);
    
    public ValueTask<bool> PingAsync();
}