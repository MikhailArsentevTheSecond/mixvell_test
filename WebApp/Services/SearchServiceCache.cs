using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace WebApp;

/// <summary>
/// Задача обёртки IMemoryCache в том, чтобы увеличить вероятность попадания в кэш.
/// Если посмотреть на SearchRequest станет ясно, что любое множество маршрутов из запроса с опциональными фильтрами
/// полностью входит в множество маршрутов из запроса без опциональных фильтров
/// Логика такая:
/// Получаем Хэш запроса -> Ищем хэш в словаре _cacheMap, который связывает запрос и множество маршрутов -> Если находим запись, то проверяем опциональные фильтры
/// -> Если в закэшированном запросе не было фильтров или фильтры совпадают, то используем кэш, иначе промах.
///
/// Временем жизни конкретного маршрута управляет IMemoryCache.
/// Т.к отношение один ко многим (один запрос -> много маршрутов), то надо каким-то образом решать когда кэш не валиден:
/// 1. Когда не осталось ни одной записи
/// 2. Половина записей
/// 3. Хотя бы одной.
/// Выбрал второй.
///
/// Есть проблема:
/// 1. _cacheMap разрастается бесконечно 
/// Решил оставить эту проблему за скобками.
/// </summary>
public class SearchServiceCache : ISearchServiceCache
{
    private readonly IMemoryCache _cache;

    private readonly ConcurrentDictionary<int, CacheMap> _cacheMap;
    
    public SearchServiceCache(IMemoryCache cache)
    {
        _cache = cache;
        _cacheMap = new ConcurrentDictionary<int, CacheMap>();
    }

    /// <summary>
    /// Получить маршруты из кэша
    /// </summary>
    /// <param name="request">Запрос</param>
    /// <returns>список маршрутов или null, если в кэше нет валидных данных</returns>
    public IReadOnlyList<Route>? Get(SearchRequest request)
    {
        var cacheMap = GetCacheMapValue(request);

        if (cacheMap == null)
        {
            return null;
        }
        
        if (cacheMap.Filters == null)
        {
            return GetRoutesWithValidation(cacheMap, request.Filters);
        }
        else if (cacheMap.Filters.Equals(request.Filters))
        {
            return GetRoutesWithValidation(cacheMap, request.Filters);
        }
        else
        {
            //Пытался оптимзировать эту инвалидацию.
            //Но решение было громоздким.
            return null;
        }
    }
    

    
    /// <summary>
    /// Записывает значение в кэш.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    public void Set(SearchRequest request, SearchResponse response)
    {
        if (response.Routes.Length == 0)
        {
            var guid = SetFakeCacheMapValue(request, response);
            _cache.Set(guid, (Route?)null,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        }
        else
        {
            SetCacheMapValue(request, response);
            foreach (var route in response.Routes)
            {
                _cache.Set(route.Id, route, new MemoryCacheEntryOptions {AbsoluteExpiration = route.TimeLimit});
            }
        }

    }

    /// <summary>
    /// Делает запись в словарь, который связывает запрос и маршруты.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    private void SetCacheMapValue(SearchRequest request, SearchResponse response)
    {
        var cacheMapIds = response.Routes.Select(x => x.Id).ToArray();
        
        _cacheMap.AddOrUpdate(request.GetHashCode(), 
            _ => new CacheMap(DateTime.Now, cacheMapIds)
                {Filters = request.Filters},
            (_, existingValue) =>
            {
                if (existingValue.RequestDateTime < DateTime.Now)
                {
                    return new CacheMap(DateTime.Now, cacheMapIds) 
                        {Filters = request.Filters};
                }
                else
                {
                    return existingValue;
                }
            });
    }

    private Guid SetFakeCacheMapValue(SearchRequest request, SearchResponse response)
    {
        var cacheMapIds = new [] {Guid.NewGuid()};
        
        _cacheMap.AddOrUpdate(request.GetHashCode(), 
            _ => new CacheMap(DateTime.Now, cacheMapIds)
                {Filters = request.Filters},
            (_, existingValue) =>
            {
                if (existingValue.RequestDateTime < DateTime.Now)
                {
                    return new CacheMap(DateTime.Now, cacheMapIds) 
                        {Filters = request.Filters};
                }
                else
                {
                    return existingValue;
                }
            });
        return cacheMapIds[0];
    }
    
    /// <summary>
    /// Чтение маршрутов из кэша с валидацией
    /// </summary>
    /// <param name="cacheMap"></param>
    /// <param name="requestFilter"></param>
    /// <returns>Список валидных маршрутов или null, если таковых нет или недостаточно</returns>
    private IReadOnlyList<Route>? GetRoutesWithValidation(CacheMap cacheMap, SearchFilters? requestFilter)
    {
        var result = new List<Route>();
        foreach (var routeId in cacheMap.RouteIds)
        {
            if(_cache.TryGetValue(routeId, out Route? route))
            {
                if (route == null)
                {
                    //route = null когда нужно закэшировать пустой ответ от провайдеров.
                    return [];
                }
                else if (requestFilter == null || requestFilter.IsCorrectRoute(route))
                {
                    result.Add(route);
                }
            }
        }
        
        //Больше половины кэша протухла (или не актуальна для этого запроса). Считаем весь кэш неактуальным
        //Половина - значение "с потолка". Правильное решение можно принять зная природу данных/бизнес требования etc.
        if ((cacheMap.RouteIds.Length != 0 && result.Count == 0) || cacheMap.RouteIds.Length - result.Count < cacheMap.RouteIds.Length / 2)
        {
            return null;
        }
        else
        {
            return result;
        }
    }
    
    /// <summary>
    /// Получает список id маршрутов из кэша, которые связаны с данным запросом.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private CacheMap? GetCacheMapValue(SearchRequest request)
    {
        if (_cacheMap.TryGetValue(request.GetHashCode(), out var cacheMap))
        {
            if(cacheMap.Filters == null)
            {
                //В базе хранится максимально широкий запрос.
                return cacheMap;
            }
            else if (Equals(request.Filters, cacheMap.Filters))
            {
                //Попали прямо в кэш
                return cacheMap;
            }
            else
            {
                //Хранящийся в базе кэш отличается.
                ////Я пытался оптимизировать инвалидацию в этом сценарии,
                ////но получался неподдерживаемый монстр.
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Элемент словаря, связывающий запрос и список маршрутов.
    /// </summary>
    private class CacheMap
    {
        public CacheMap(DateTime requestDateTime, Guid[] routeIds)
        {
            RequestDateTime = requestDateTime;
            RouteIds = routeIds;
        }
        
        /// <summary>
        /// Для решения конфликтов записи
        /// </summary>
        public DateTime RequestDateTime { get; init; }
        
        /// <summary>
        /// Id маршрутов, привязанных к этому запросу.
        /// </summary>
        public Guid[] RouteIds { get; init; }
        
        /// <summary>
        /// С какими дополнительными фильтрами был получен результат?
        /// Если запрос более широкий чем тот, что читает кэш, то можно переиспользовать результат.
        /// </summary>
        public SearchFilters? Filters { get; set; }
    }
}