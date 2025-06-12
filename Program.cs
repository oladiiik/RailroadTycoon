using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using Core.Generation.Map;          
using Core.Generation.City;         
using Core.Render.Raster;           
using Core.Render.Vector;          
using Core.Game;                    
using Core.Algorithms.Graph;
using Core.Geometry.Contour;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.Cookie.Name     = ".map.session";
    opt.Cookie.HttpOnly = true;
    opt.IdleTimeout     = TimeSpan.FromMinutes(30);
});
var app = builder.Build();

app.UseStaticFiles();
app.UseSession();

const int   gridW      = 600;
const int   gridH      = 400;
const int   pxPerCell  = 4;
const float threshold  = 0.44f;

static async Task<float[,]> GetHeightAsync(
    IMemoryCache cache,
    int w, int h,
    int seed, float freq, int oct, float lac, float gain)
{
    string key = $"h:{seed}:{freq}:{oct}:{lac}:{gain}";
    return await cache.GetOrCreateAsync(key, async e =>
    {
        e.SlidingExpiration = TimeSpan.FromMinutes(15);
        return await Task.Run(() =>
            HeightMapGenerator.Generate(w, h, seed, freq, oct, lac, gain));
    });
}

static async Task<(List<Point>, List<string>)> GetCitiesAsync(
    IMemoryCache cache,
    float[,] hmap,
    int seed, int count, float thr)
{
    string key = $"c:{seed}:{count}:{thr}";
    return await cache.GetOrCreateAsync(key, async e =>
    {
        e.SlidingExpiration = TimeSpan.FromMinutes(15);
        return await Task.Run(() =>
            CityGenerator.Generate(hmap, seed, count, thr));
    });
}

static GameState GetGameState(
    HttpContext http,
    IMemoryCache cache,
    float[,] hmap,
    List<Point> cities,
    double initialBudget,
    double costPerKm,
    double rewardPerCity,
    float threshold)
{
    const string aliveFlag = "alive";
    if (!http.Session.TryGetValue(aliveFlag, out _))
        http.Session.SetInt32(aliveFlag, 1);       
    
    string cacheKey = $"gamestate_{http.Session.Id}";
    if (cache.TryGetValue(cacheKey, out GameState? state))
        return state!;

    var newState = new GameState(
        hmap, cities, initialBudget, costPerKm, rewardPerCity, threshold);

    cache.Set(cacheKey, newState, TimeSpan.FromMinutes(30));
    return newState;
}

app.MapGet("/map", async (
        HttpContext http,
        IMemoryCache cache, IWebHostEnvironment env,
        int   seed             = 42,
        float freq             = 0.008f,
        int   octaves          = 4,
        float lacunarity       = 1.4f,
        float gain             = 0.55f,
        int   cityCount        = 450,
        double initialBudget   = 3_000_000,
        double costPerKm       = 50_000,
        double rewardPerCity   = 10_000,
        string levels          = "0.4,0.44") =>
{
    var heightTask = GetHeightAsync(cache, gridW, gridH,
                                    seed, freq, octaves, lacunarity, gain);

    var cloudTask  = Task.Run(() =>
        CloudMapGenerator.Generate(gridW, gridH, seed,
                                   freq * 2, octaves, lacunarity, gain));

    var waterTask  = Task.Run(() =>
        WaterMapGenerator.Generate(gridW, gridH, seed + 1,
                                   0.01f, 1.5f, 0.4f));

    await Task.WhenAll(heightTask, cloudTask, waterTask);
    var hmap      = heightTask.Result;
    var cloudMap  = cloudTask.Result;
    var waterMask = waterTask.Result;
    
    var (cities, names) = await GetCitiesAsync(cache, hmap, seed, cityCount, threshold);
    
    var lvls = levels.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                     .ToArray();

    var isolTask = Task.Run(() =>
        SvgRenderer.ToSvg(
            MarchingSquares.Extract(hmap, lvls),
            gridW, gridH, pxPerCell,
            strokeWidth: pxPerCell / 2, strokeColor: "#423b35"));

    var pngTask = Task.Run(() =>
        Convert.ToBase64String(
            RasterHeightRenderer.RenderPng(hmap, lvls, pxPerCell, seed)));

    await Task.WhenAll(isolTask, pngTask);
    string isolinesSvg = isolTask.Result;
    string pngB64      = pngTask.Result;
    
    string citiesSvg = CityRenderer.RenderCities(
        cities, names, hmap,
        threshold, initialBudget, costPerKm, rewardPerCity, pxPerCell);

    string gridSvg  = GridRenderer.ToSvgGrid(gridW, gridH, pxPerCell, "#555", 0.5f);
    string gridB64  = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(gridSvg));

    string merged   = GridRenderer.MergeSvgs(isolinesSvg, citiesSvg);
    string mergedB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(merged));

    string cloudsB64 = Convert.ToBase64String(
        RasterCloudRenderer.RenderPng(cloudMap, pxPerCell, 0.3f, 0.45f));

    string waterB64  = Convert.ToBase64String(
        RasterWaterRenderer.RenderPng(waterMask, pxPerCell));
    
    string tpl  = await File.ReadAllTextAsync(
        Path.Combine(env.WebRootPath, "pages", "index.html"));

    string html = tpl
        .Replace("{{W}}",   (gridW * pxPerCell).ToString())
        .Replace("{{H}}",   (gridH * pxPerCell).ToString())
        .Replace("{{PNG}}",    pngB64)
        .Replace("{{SVG}}",    mergedB64)
        .Replace("{{GRID}}",   gridB64)
        .Replace("{{CLOUDS}}", cloudsB64)
        .Replace("{{WATER}}",  waterB64)
        .Replace("{px}",       pxPerCell.ToString());
    
    
    http.Session.Clear();                                   
    http.Response.Cookies.Delete(".map.session");           
    
    return Results.Content(html, "text/html");
});

app.MapGet("/cities", async (
        IMemoryCache cache,
        int   seed, float freq, int octaves, float lacunarity, float gain,
        int   cityCount        = 450,
        double initialBudget   = 3_000_000,
        double costPerKm       = 50_000,
        double rewardPerCity   = 10_000) =>
{
    var hmap           = await GetHeightAsync(cache, gridW, gridH,
                                              seed, freq, octaves, lacunarity, gain);
    var (cities, names)= await GetCitiesAsync(cache, hmap, seed, cityCount, threshold);

    var (startIdx, links) = EconomicAnalyzer.GetBestEconomicTree(
        cities, hmap, threshold, initialBudget, costPerKm, rewardPerCity);

    var cityDto = cities.Select((p, i) => new {
        x = p.X, y = p.Y, name = names[i], index = i
    }).ToArray();

    return Results.Json(new {
        cities   = cityDto,
        threshold,
        costPerKm,
        budget       = initialBudget,
        rewardPerCity,
        bestRoute = new {
            startIdx,
            connections = links.Select(l => new { from=l.from, to=l.to })
        }
    });
});

app.MapPost("/connect", async (
        HttpContext http,
        IMemoryCache cache,
        [FromBody] ConnectRequest req,
        [FromQuery] int   seed,
        [FromQuery] float freq,
        [FromQuery] int   octaves,
        [FromQuery] float lacunarity,
        [FromQuery] float gain,
        [FromQuery] int   cityCount        = 450,
        [FromQuery] double initialBudget   = 3_000_000,
        [FromQuery] double costPerKm       = 50_000,
        [FromQuery] double rewardPerCity   = 10_000) =>
{
    var hmap         = await GetHeightAsync(cache, gridW, gridH,
                                            seed, freq, octaves, lacunarity, gain);
    var (cities, _)  = await GetCitiesAsync(cache, hmap, seed, cityCount, threshold);
    
    var gs = GetGameState(http, cache, hmap, cities,
                          initialBudget, costPerKm, rewardPerCity, threshold);

    var (ok, cost, reason) = gs.TryConnect(req.From, req.To);

    return Results.Json(new ConnectResponse(ok, cost, reason, gs.Budget));
});

app.MapGet("/", async (
    IWebHostEnvironment env,
    IMemoryCache        cache,
    int   seed       = 42,
    float freq       = 0.008f,
    int   octaves    = 4,
    float lacunarity = 1.4f,
    float gain       = 0.55f,
    int   cityCount  = 450) =>
{
    var hmapTask = Task.Run(() =>
        HeightMapGenerator.Generate(gridW, gridH, seed, freq, octaves, lacunarity, gain));
    var cloudTask  = Task.Run(() =>
        CloudMapGenerator.Generate(gridW, gridH, seed, freq*2, octaves, lacunarity, gain));
    var waterTask  = Task.Run(() =>
        WaterMapGenerator.Generate(gridW, gridH, seed+1, 0.01f, 1.5f, 0.4f));

    await Task.WhenAll(hmapTask, cloudTask, waterTask);
    var hmap   = hmapTask.Result;
    var clouds = cloudTask.Result;
    var water  = waterTask.Result;
    
    var pngB64 = Convert.ToBase64String(
        RasterHeightRenderer.RenderPng(hmap,
            new[]{0.4f,0.44f},   
            pxPerCell, seed));

    var cloudsB64 = Convert.ToBase64String(
        RasterCloudRenderer.RenderPng(clouds, pxPerCell, .3f, .45f));

    var waterB64 = Convert.ToBase64String(
        RasterWaterRenderer.RenderPng(water, pxPerCell));

    var (cities, names) = await GetCitiesAsync(cache, hmap, seed, cityCount, threshold);
    var isolinesSvg = SvgRenderer.ToSvg(
        MarchingSquares.Extract(hmap, new[] { 0.4f, 0.44f }),
        gridW, gridH, pxPerCell,
        strokeWidth:pxPerCell/2, strokeColor:"#423b35");
    
    var citiesSvg  = CityRenderer.RenderCities(
        cities, names, hmap,
        threshold, 3_000_000, 50_000, 10_000, pxPerCell);
        
    string mergedSvg = GridRenderer.MergeSvgs(isolinesSvg, citiesSvg);
    string svgB64    = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mergedSvg));
    string tpl = await File.ReadAllTextAsync(
        Path.Combine(env.WebRootPath, "pages", "menu.html"));

    string html = tpl.Replace("{{PNG}}",    pngB64)
        .Replace("{{CLOUDS}}", cloudsB64)
        .Replace("{{WATER}}",  waterB64)
        .Replace("{{SVG}}",    svgB64);

    return Results.Content(html, "text/html");
});

app.Run();
public record ConnectRequest(int From, int To);
public record ConnectResponse(bool Success, double? Cost, string? Reason, double NewBudget);