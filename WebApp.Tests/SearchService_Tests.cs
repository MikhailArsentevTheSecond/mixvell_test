using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace WebApp.Tests;

public class SearchService_Tests
{
    //Кривой мок DateTime. Честно не тестировал что-то с временем.
    private static class DateTime
    {
        private static System.DateTime _cachedTime = System.DateTime.Now;

        public static System.DateTime Now => _cachedTime;
    }
    
    //Запрос возвращает результат двух провайдеров
    [Fact]
    public async Task Search_Aggregation_Ok()
    {
        var memCache = new SearchServiceCache(new MemoryCache(new MemoryCacheOptions()));
        
        var providerOne = new Mock<IProviderOneService>();
        var timeLimit = DateTime.Now.AddMinutes(1);
        providerOne.Setup(x => x.Search(new SearchRequest())).Returns(
            Task.FromResult(new ProviderOneSearchResponse
            {
                Routes = 
                [
                    new() {From = "FirstEntry", TimeLimit = timeLimit}, 
                    new () {From = "SecondEntry", TimeLimit = timeLimit}
                ]
            }));
        
        var providerTwo = new Mock<IProviderTwoService>();
        providerTwo.Setup(x => x.Search(new SearchRequest())).Returns(
            Task.FromResult(new ProviderTwoSearchResponse()
            {
                Routes = 
                [
                    new() {Arrival = new ProviderTwoPoint {Point = "Третья запись"}, Departure = new ProviderTwoPoint(), TimeLimit = timeLimit}, 
                    new() {Arrival = new ProviderTwoPoint {Point = "Четвёртая запись"}, Departure = new ProviderTwoPoint(), TimeLimit = timeLimit}
                ]
            }));
        
        var searchService = new SearchService(providerOne.Object, providerTwo.Object, memCache);
        var result = await searchService.Search(new SearchRequest());
        
        Assert.True(result.Routes.Length == 4);
    }
    
    //Агрегированные записи корректны.
    [Fact]
    public async Task Search_Aggregation_Aggregation_Correct()
    {
        var memCache = new SearchServiceCache(new MemoryCache(new MemoryCacheOptions()));
        
        var providerOne = new Mock<IProviderOneService>();
        var timeLimit = DateTime.Now.AddMinutes(1);
        
        const int maxPrice = 101;
        const int minPrice = 10;
        var minMinutesRoute = (int)DateTime.Now.AddDays(5).Subtract(DateTime.Now).TotalMinutes;
        var maxMinutesRoute = (int)DateTime.Now.AddDays(100).Subtract(DateTime.Now).TotalMinutes;
        
        providerOne.Setup(x => x.Search(new SearchRequest())).Returns(
            Task.FromResult(new ProviderOneSearchResponse
            {
                Routes = 
                [
                    new() {From = "FirstEntry", TimeLimit = timeLimit, DateFrom = DateTime.Now, DateTo = DateTime.Now.AddDays(5), Price = 100, To="Str"}, 
                    new () {From = "SecondEntry", TimeLimit = timeLimit, DateFrom = DateTime.Now, DateTo = DateTime.Now.AddDays(5), Price = maxPrice, To="X"}
                ]
            }));
        
        var providerTwo = new Mock<IProviderTwoService>();
        providerTwo.Setup(x => x.Search(new SearchRequest())).Returns(
            Task.FromResult(new ProviderTwoSearchResponse
            {
                Routes = 
                [
                    new() {Arrival = new ProviderTwoPoint {Date = DateTime.Now.AddDays(100), Point = "Третья запись"}, Departure = new ProviderTwoPoint {Date = DateTime.Now}, TimeLimit = timeLimit, Price = minPrice}, 
                    new() {Arrival = new ProviderTwoPoint {Date = DateTime.Now.AddDays(100), Point = "Четвёртая запись"}, Departure = new ProviderTwoPoint {Date = DateTime.Now}, TimeLimit = timeLimit, Price = 100}
                ]
            }));
        
        var searchService = new SearchService(providerOne.Object, providerTwo.Object, memCache);
        var result = await searchService.Search(new SearchRequest());

        Assert.All(
            new []
            {
                result.MinPrice == minPrice, 
                result.MaxPrice == maxPrice, 
                result.MaxMinutesRoute == maxMinutesRoute, 
                result.MinMinutesRoute == minMinutesRoute
            }, Assert.True);
    }

    //Если кэш пустой, то запросы к провайдерам не уходят.
    [Fact]
    public async Task Search_Aggregation_CacheOnly_Ok()
    {
        var memCache = new SearchServiceCache(new MemoryCache(new MemoryCacheOptions()));
        
        var providerOne = new Mock<IProviderOneService>();
        
        providerOne.Setup(x => x.Search(new SearchRequest()))
            .Throws(new Exception("request to providerOne"));
        
        var providerTwo = new Mock<IProviderTwoService>();
        providerTwo.Setup(x => x.Search(new SearchRequest()))
            .Throws(new Exception("request to providerTwo"));
        
        var searchService = new SearchService(providerOne.Object, providerTwo.Object, memCache);

        var recorded = await Record.ExceptionAsync(() => searchService.Search(new SearchRequest() { Filters = new SearchFilters() { OnlyCached = true } }));
        
        Assert.Null(recorded);
    }
    
    //Кэш корректно записывается и читается
    [Fact]
    public async Task Search_Aggregation_CacheRead_Ok()
    {
        var memCache = new SearchServiceCache(new MemoryCache(new MemoryCacheOptions()));

        var request = new SearchRequest { Destination = "Dest", Origin = "Origin", OriginDateTime = DateTime.Now };
        var routes = new Route[]
        {
            new()
            {
                Origin = "Ok", Destination = "Ok", Price = 10, Id = Guid.NewGuid(), TimeLimit = DateTime.Now.AddDays(1)
            }
        };
        memCache.Set(request, new SearchResponse(routes));
        
        var providerOne = new Mock<IProviderOneService>();
        
        providerOne.Setup(x => x.Search(new SearchRequest()))
            .Throws(new Exception("request to providerOne"));
        
        var providerTwo = new Mock<IProviderTwoService>();
        providerTwo.Setup(x => x.Search(new SearchRequest()))
            .Throws(new Exception("request to providerTwo"));
        
        var searchService = new SearchService(providerOne.Object, providerTwo.Object, memCache);

        var cacheResponse = await searchService.Search(request);
        
        Assert.Equal(routes, cacheResponse.Routes);
    }
    
    
    //Если оба сервиса недоступны - false
    [Fact]
    public async Task Ping_True()
    {
        var memCache = new SearchServiceCache(new MemoryCache(new MemoryCacheOptions()));
        
        var providerOne = new Mock<IProviderOneService>();
        
        providerOne.Setup(x => x.Ping())
            .Returns(ValueTask.FromResult(false));
        
        var providerTwo = new Mock<IProviderTwoService>();
        providerTwo.Setup(x => x.Ping())
            .Returns(ValueTask.FromResult(true));
        
        var searchService = new SearchService(providerOne.Object, providerTwo.Object, memCache);

        var recorded = await searchService.PingAsync();
        
        Assert.True(recorded);
    }
    
    //Если хотя бы один сервис недоступен - true
    [Fact]
    public async Task Ping_False()
    {
        var memCache = new SearchServiceCache(new MemoryCache(new MemoryCacheOptions()));
        
        var providerOne = new Mock<IProviderOneService>();
        
        providerOne.Setup(x => x.Ping())
            .Returns(ValueTask.FromResult(false));
        
        var providerTwo = new Mock<IProviderTwoService>();
        providerTwo.Setup(x => x.Ping())
            .Returns(ValueTask.FromResult(false));
        
        var searchService = new SearchService(providerOne.Object, providerTwo.Object, memCache);

        var recorded = await searchService.PingAsync();
        
        Assert.False(recorded);
    }
}