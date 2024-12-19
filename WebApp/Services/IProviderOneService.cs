namespace WebApp;

public interface IProviderOneService
{
    public Task<ProviderOneSearchResponse> Search(SearchRequest request);
    
    public ValueTask<bool> Ping();
}