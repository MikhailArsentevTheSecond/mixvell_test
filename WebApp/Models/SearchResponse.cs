namespace WebApp;

public class SearchResponse
{
    public SearchResponse(IEnumerable<Route> routes):this(routes.ToArray())
    {
    }
    
    public SearchResponse(Route[] routes)
    {
        Routes = routes;
        //TODO: 4 раза итерируем коллекцию. Читаемо, но не эффективно.
        MinPrice = Routes.DefaultIfEmpty().Min(x => x?.Price ?? 0);
        MaxPrice = Routes.DefaultIfEmpty().Max(x => x?.Price ?? 0);
        MinMinutesRoute = (int)Routes.DefaultIfEmpty().Min(x => x?.DestinationDateTime.Subtract(x.OriginDateTime).TotalMinutes ?? 0);
        MaxMinutesRoute = (int)Routes.DefaultIfEmpty().Max(x => x?.DestinationDateTime.Subtract(x.OriginDateTime).TotalMinutes ?? 0);
    }
    
    
    // Mandatory
    // Array of routes
    public Route[] Routes { get; init; }
    
    // Mandatory
    // The cheapest route
    public decimal MinPrice { get; private set; }
    
    // Mandatory
    // Most expensive route
    public decimal MaxPrice { get; private set; }
    
    // Mandatory
    // The fastest route
    public int MinMinutesRoute { get; private set; }
    
    // Mandatory
    // The longest route
    public int MaxMinutesRoute { get; private set; }
}

public class Route
{
    // Mandatory
    // Identifier of the whole route
    public Guid Id { get; set; }
    
    // Mandatory
    // Start point of route
    public string Origin { get; set; }
    
    // Mandatory
    // End point of route
    public string Destination { get; set; }
    
    // Mandatory
    // Start date of route
    public DateTime OriginDateTime { get; set; }
    
    // Mandatory
    // End date of route
    public DateTime DestinationDateTime { get; set; }
    
    // Mandatory
    // Price of route
    public decimal Price { get; set; }
    
    // Mandatory
    // Timelimit. After it expires, route became not actual
    public DateTime TimeLimit { get; set; }
}