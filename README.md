Продублировал текст из проекта WebApp
# Тестовое задание MixVel Арсентьев Михаил Дмитриевич
### Допущения
1. SearchService метод Ping -> возвращает 200 если хотя бы 1 провайдер доступен.

2. Кэш полностью инвалидируется когда больше половины маршрутов просрочена. 

3. До этого возвращаются данные из кэша. 

4. Если провайдеры вернули пустой список, то он будет в кэше 5 минут (hardcode).


### Как рассуждал
Решение в лоб предполагает сохранение пары SearchRequest/SearchResponse
Хэш SearchResponse считается в том числе по необязательным полям.

Плюсы:
- Легко и просто

Минусы:
- Частые "промахи"
- Маршруты хранятся не оптимальное количество времени
---

Решил **оптимизировать** кэш.

Заметил, что запрос без опциональных параметров содержит 
в себе маршруты любого запроса с опциональными параметрами.

```
SearchResponse {Filters = null} ⊃ SearchFilters {Filters != null}
```

Соответственно хэш можно считать только от обязательных параметров запроса.
А в значении хранить SearchFilters.

*Чтобы понимать с какими доп. фильтрами был сделан данный запрос и сравнивать кэш с запросом*

Но возникает другая проблема - у каждого Route-а своё время жизни.

### Логика работы кэша
Я сделал cacheMap. Это словарь, который по HashCode request-а получает массив Id маршрутов,
который хранится уже в стандартном IMemoryCache.

Набор маршрутов считается неактуальным, когда больше половины маршруты не найдена в кэше

*Условие взял с потолка. Зависит от разброса по времени жизни и бизнес требованиям*

Плюсы:
- Каждый маршрут хранится ровно столько, сколько должен
- Количество промахов уменьшено. Есть возможность улучшить результат.

Минусы:
- Решение сложно масштабировать (из-за необходимости синхронизировать CacheMap и сам кэш)
- Сложность увеличилась за счёт CacheMap

### Автотесты сделаны. Основные задачи проверить можно. Но честно уже устал на них.

### Не думал о

* Валидации данных от провайдеров.
