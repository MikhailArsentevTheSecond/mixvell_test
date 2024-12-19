namespace WebApp;

public interface IProviderTwoService
{
    public Task<ProviderTwoSearchResponse> Search(SearchRequest request);
    
    public ValueTask<bool> Ping();
}