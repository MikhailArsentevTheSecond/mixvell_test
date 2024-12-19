namespace WebApp;

public interface ISearchServiceCache
{
    public IReadOnlyList<Route>? Get(SearchRequest request);

    public void Set(SearchRequest request, SearchResponse response);
}