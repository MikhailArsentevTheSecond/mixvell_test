namespace WebApp;

public class ProviderOneRoute
{
    // Mandatory
    // Start point of route
    public string From { get; set; }
    
    // Mandatory
    // End point of route
    public string To { get; set; }
    
    // Mandatory
    // Start date of route
    public DateTime DateFrom { get; set; }
    
    // Mandatory
    // End date of route
    public DateTime DateTo { get; set; }
    
    // Mandatory
    // Price of route
    public decimal Price { get; set; }
    
    // Mandatory
    // Timelimit. After it expires, route became not actual
    public DateTime TimeLimit { get; init; }

    public Route ToAggregatedResponse()
    {
        return new Route
        {
            Origin = From,
            Destination= To,
            Price = Price,
            OriginDateTime = DateFrom,
            DestinationDateTime = DateTo,
            TimeLimit = TimeLimit,
            Id = new Guid()
        };
    }
}