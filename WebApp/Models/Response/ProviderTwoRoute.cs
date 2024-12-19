namespace WebApp;

public class ProviderTwoRoute
{
    // Mandatory
    // Start point of route
    public ProviderTwoPoint Departure { get; set; }
    
    
    // Mandatory
    // End point of route
    public ProviderTwoPoint Arrival { get; set; }
    
    // Mandatory
    // Price of route
    public decimal Price { get; set; }
    
    // Mandatory
    // Timelimit. After it expires, route became not actual
    public DateTime TimeLimit { get; set; }

    public Route ToAggregatedResponse()
    {
        return new Route
        {
            Origin = Departure.Point,
            OriginDateTime = Departure.Date,
            Destination = Arrival.Point,
            DestinationDateTime = Arrival.Date,
            Price = Price,
            TimeLimit = TimeLimit,
            Id = new Guid()
        };
    }
}

public class ProviderTwoPoint
{
    // Mandatory
    // Name of point, e.g. Moscow\Sochi
    public string Point { get; set; }
    
    // Mandatory
    // Date for point in Route, e.g. Point = Moscow, Date = 2023-01-01 15-00-00
    public DateTime Date {get; set; }
}