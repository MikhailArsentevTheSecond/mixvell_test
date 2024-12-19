using WebApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient(ProviderOneService.ProviderHttpClient,
    client => client.BaseAddress = 
        new Uri(builder.Configuration.GetConnectionString("ProviderOneApi") 
                                           ?? throw new Exception("ProviderOneApi could not be empty")));

builder.Services.AddHttpClient(ProviderTwoService.ProviderHttpClient,
        client => client.BaseAddress = 
            new Uri(builder.Configuration.GetConnectionString("ProviderOneApi") 
                    ?? throw new Exception("ProviderOneApi could not be empty")));

builder.Services.AddSingleton<ISearchServiceCache, SearchServiceCache>();
builder.Services.AddSingleton<ISearchService, SearchService>();

builder.Services.AddSingleton<IProviderOneService, ProviderOneService>();
builder.Services.AddSingleton<IProviderTwoService, ProviderTwoService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();