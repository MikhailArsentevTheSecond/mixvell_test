using System.ComponentModel.DataAnnotations;

namespace WebApp;

public class SearchRequest
{
    // Mandatory
    // Start point of route, e.g. Moscow 
    [Required]
    public string Origin { get; init; }
    
    // Mandatory
    // End point of route, e.g. Sochi
    [Required]
    public string Destination { get; init; }
    
    // Mandatory
    // Start date of route
    [Required]
    public DateTime OriginDateTime { get; init; }
    
    // Optional
    public SearchFilters? Filters { get; set; }
    
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Origin, Destination, OriginDateTime);
    }

    public override bool Equals(object? obj)
    {
        if (obj is SearchRequest req)
        {
            return req.Origin == Origin && req.Destination == Destination && req.OriginDateTime == OriginDateTime;
        }
        else
        {
            return false;
        }
    }
    
    //Если изменился DestinationDateTime -> Делать запрос
    //Если DestinationDateTime нет -> Делать запрос
    
    //Если MaxPrice < или равен null -> делать запрос
    //Если MinTimeLimit > или равен null -> делать запрос
    public bool ShouldRedoRequest(SearchFilters? oldFilter)
    {
        if (oldFilter == null)
        {
            return false;
        }
        if (Filters == null)
        {
            return true;
        }
        
        if (oldFilter.MaxPrice != null)
        {
            if (Filters.MaxPrice == null)
            {
                return true;
            }
            else
            {
                //Если новый maxPrice меньше, то это менее строгий поиск
                return oldFilter.MaxPrice < Filters.MaxPrice;
            }
        }
        else if (oldFilter.MinTimeLimit != null)
        {
            if (Filters.MinTimeLimit == null)
            {
                return true;
            }
            else
            {
                //Если новый maxPrice меньше, то это менее строгий поиск
                return oldFilter.MinTimeLimit > Filters.MinTimeLimit;
            }
        }
        
        //Старый запрос был менее строгим.
        return false;
    }
}

public class SearchFilters
{
    // Optional
    // End date of route
    public DateTime? DestinationDateTime { get; set; }
    
    // Optional
    // Maximum price of route
    public decimal? MaxPrice { get; set; }
    
    // Optional
    // Minimum value of timelimit for route
    public DateTime? MinTimeLimit { get; set; }
    
    // Optional
    // Forcibly search in cached data
    public bool? OnlyCached { get; set; }


    public override bool Equals(object? obj)
    {
        if (obj is SearchFilters filter)
        {
            return filter.DestinationDateTime == DestinationDateTime && 
                   filter.MinTimeLimit == MinTimeLimit &&
                   filter.MaxPrice == MaxPrice;
        }
        else
        {
            return false;
        }
    }

    public bool IsCorrectRoute(Route route)
    {
        return (DestinationDateTime == null || route.DestinationDateTime == DestinationDateTime) &&
               (MaxPrice == null || route.Price < MaxPrice)  &&
               (MinTimeLimit == null || route.TimeLimit > MinTimeLimit);
    }
}