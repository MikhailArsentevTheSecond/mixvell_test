using Microsoft.Extensions.Caching.Memory;
using WebApp;

namespace WebApp.Tests;

public class SearchServiceCache_Tests
{
    //Кэш сохраняет данные
    //1. Записанные данные можно получить.
    //2. Менее строгие условия переписывают значения более строгих.
    //3. Get по несуществующему ключу возвращает null
    
    //Кэш читается
    [Fact]
    public void Cache_Success()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new SearchServiceCache(memCache);
        
        var request = new SearchRequest { Destination = "A", Origin = "B", OriginDateTime = DateTime.Now };
        var response = new SearchResponse([new Route
        {
            Destination = "Ok", Price = 10, Id = Guid.NewGuid(),
            //TODO: Вообще DateTime.Now в тестах это нехорошо, но у нас кэш который считает от текущего времени.
            TimeLimit = DateTime.Now.AddHours(1)
        }]);
        cache.Set(request, response);

        var cachedResponse = cache.Get(request);
        
        Assert.Equal(response.Routes, cachedResponse);
    }
    
    //Кэш освобождается от просроченных маршрутов
    [Fact]
    public void Cache_TimeLimit_Ivalidated()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new SearchServiceCache(memCache);
        
        var request = new SearchRequest { Destination = "A", Origin = "B", OriginDateTime = DateTime.Now };
        var response = new SearchResponse([new Route
        {
            Destination = "Ok", Price = 10, Id = Guid.NewGuid(),
            TimeLimit = DateTime.Now
        }]);
        cache.Set(request, response);

        var cachedResponse = cache.Get(request);
        
        Assert.Null(cachedResponse);
    }
    
    //Непрямое попадание (по несовпадающим опциональным фильтрам)
    [Fact]
    public void Cache_Inderect_Hit()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new SearchServiceCache(memCache);
        
        var destinationDateTime = DateTime.FromOADate(42842.370277778d);

        var request = new SearchRequest { Destination = "A", Origin = "B", OriginDateTime = DateTime.Today };
        var response = new SearchResponse([new Route
        {
            Destination = "Ok", Price = 10, Id = Guid.NewGuid(),
            DestinationDateTime = destinationDateTime,
            TimeLimit = DateTime.Now.AddHours(1)
        }]);
        
        cache.Set(request, response);

        var requestWithOptional = new SearchRequest
        {
            Destination = "A", Origin = "B", OriginDateTime = DateTime.Today,
            Filters = new SearchFilters { DestinationDateTime = destinationDateTime }
        };

        if (requestWithOptional.GetHashCode() != request.GetHashCode())
        {
            Assert.Fail("HashCode error");
        }
        
        var cachedResponse = cache.Get(requestWithOptional);
        
        Assert.Equal(response.Routes, cachedResponse);
    }


    //Непрямое попадаение. Опциональные фильтры соблюдены.
    [Fact]
    public void Cache_Inderect_Hit_Filtered()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new SearchServiceCache(memCache);
        
        var destinationDateTime = DateTime.FromOADate(42842.370277778d);

        var request = new SearchRequest { Destination = "A", Origin = "B", OriginDateTime = DateTime.Today };
        var response = new SearchResponse([new Route
        {
            Destination = "Ok", Price = 10, Id = Guid.NewGuid(),
            DestinationDateTime = destinationDateTime,
            TimeLimit = DateTime.Now.AddHours(1)
        },
        new Route
        {
            Destination = "Not Ok", Price = 999, Id = Guid.NewGuid(),
            TimeLimit = DateTime.Now.AddHours(1)
        }]);
        
        cache.Set(request, response);

        var requestWithOptional = new SearchRequest
        {
            Destination = "A", Origin = "B", OriginDateTime = DateTime.Today,
            Filters = new SearchFilters { DestinationDateTime = destinationDateTime }
        };

        if (requestWithOptional.GetHashCode() != request.GetHashCode())
        {
            Assert.Fail("HashCode error");
        }
        
        var cachedResponse = cache.Get(requestWithOptional);

        Assert.All(cachedResponse, x => x.Destination = "Ok");
    }
    
    //Протухшая на половину запись в кэше возвращается
    [Fact]
    public void Cache_HalfInvalidated_Sucess()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new SearchServiceCache(memCache);
        
        var request = new SearchRequest { Destination = "A", Origin = "B", OriginDateTime = DateTime.Today };
        var response = new SearchResponse([new Route
            {
                Destination = "Invalidated", Price = 10, Id = Guid.NewGuid(),
                TimeLimit = DateTime.Now
            },
            new Route
            {
                Destination = "Invalidated", Price = 999, Id = Guid.NewGuid(),
                TimeLimit = DateTime.Now
            },
            new Route
            {
                Destination = "Ok", Price = 999, Id = Guid.NewGuid(),
                TimeLimit = DateTime.Now.AddHours(1)
            },
            new Route
            {
                Destination = "Ok", Price = 999, Id = Guid.NewGuid(),
                TimeLimit = DateTime.Now.AddHours(1)
            },
        ]);
        
        cache.Set(request, response);
        
        var cachedResponse = cache.Get(request);

        Assert.True(cachedResponse.Count == response.Routes.Length/2);
    }
    
    //Пустой набор возвращается как пустой набор, а не null
    [Fact]
    public void EmptyRequestNotNull_Sucess()
    {
        var memCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new SearchServiceCache(memCache);
        
        var request = new SearchRequest { Destination = "A", Origin = "B", OriginDateTime = DateTime.Today };
        var response = new SearchResponse([]);
        
        cache.Set(request, response);
        
        var cachedResponse = cache.Get(request);

        Assert.NotNull(cachedResponse);
    }
    
    //Кэш отдаёт данные
    //1. Кэш отдаёт данные при "прямом" попадании
    //2. Кэш отдаёт данные из кэша менее строгого запроса.
    //2.1 При этом происходит фильтрация данных (для всех элементов выполняются условия из SearchFilters)
    //TODO: а как проверить коллизию??
    //3. Нельзя получить данные не из того кэша 
    
    //Кэш инвалидируется
    //1. Значения в кэше при просрочке удаляются
    //2. Значение протухшие не до конца используются корректно.
    
    
}