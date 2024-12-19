namespace WebApp;

public class ProviderOneSearchRequest
{
    public ProviderOneSearchRequest(SearchRequest request)
    {
        From = request.Origin;
        To = request.Destination;
        DateFrom = request.OriginDateTime;
        DateTo = request.Filters?.DestinationDateTime;
        MaxPrice = request.Filters?.MaxPrice;    
    }
    
    // Mandatory
    // Start point of route, e.g. Moscow 
    public string From { get; set; }
    
    // Mandatory
    // End point of route, e.g. Sochi
    public string To { get; set; }
    
    // Mandatory
    // Start date of route
    public DateTime DateFrom { get; set; }
    
    // Optional
    // End date of route
    public DateTime? DateTo { get; set; }
    
    // Optional
    // Maximum price of route
    public decimal? MaxPrice { get; set; }
}