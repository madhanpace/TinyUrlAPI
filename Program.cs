using Microsoft.EntityFrameworkCore;
using Serilog;
using TinyUrlAPI.Data;
using TinyUrlAPI.Models;



Log.Logger = new LoggerConfiguration().WriteTo.File("logs/tinyurl-log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();


builder.Services.AddDbContext<TinyUrlDbContext>(options => options.UseSqlite("Data Source=tinyurl.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TinyUrlDbContext>();
    db.Database.EnsureCreated();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TinyURL API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();  
app.UseStaticFiles();   

//app.MapGet("/", () => "TinyURL API is running!");


var secretToken = builder.Configuration["SecretToken"];

app.MapPost("/api/add", async (TinyUrlAddDto dto, TinyUrlDbContext db,HttpRequest request, ILogger<Program> logger) =>
{
    var shortCode = GenerateShortCode();
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var tinyUrl = new TinyUrl
    {
        Code = shortCode,
        OriginalURL = dto.OriginalURL,
        IsPrivate = dto.IsPrivate,
        TotalClicks = 0,
        ShortURL = $"{baseUrl}/{shortCode}"
    };

    db.TinyUrls.Add(tinyUrl);
    await db.SaveChangesAsync();

    logger.LogInformation("New URL added: {ShortURL} -> {OriginalURL}", tinyUrl.ShortURL, tinyUrl.OriginalURL);

    return Results.Ok(tinyUrl);
})
.Produces<TinyUrl>(StatusCodes.Status200OK);


app.MapDelete("/api/delete/{code}", async (string code,TinyUrlDbContext db, ILogger<Program> logger) =>
{
    var url = await db.TinyUrls.FirstOrDefaultAsync(x => x.Code == code);
    if (url == null) return Results.NotFound("Not Found");

    db.TinyUrls.Remove(url);
    await db.SaveChangesAsync();

    logger.LogInformation("Deleted URL with code: {Code}", code);
    return Results.Ok("Deleted");
})
.Produces<string>(StatusCodes.Status200OK);


app.MapDelete("/api/delete-all", async (TinyUrlDbContext db,ILogger<Program> logger) =>
{
    db.TinyUrls.RemoveRange(db.TinyUrls);
    await db.SaveChangesAsync();

    logger.LogInformation("All URLs deleted");
    return Results.Ok("All Deleted");
})
.Produces<string>(StatusCodes.Status200OK);


app.MapPut("/api/update/{code}", async (string code,TinyUrlAddDto dto,TinyUrlDbContext db,ILogger<Program> logger) =>
{
    var url = await db.TinyUrls.FirstOrDefaultAsync(x => x.Code == code);
    if (url == null) return Results.NotFound();

    url.OriginalURL = dto.OriginalURL;
    url.IsPrivate = dto.IsPrivate;
    await db.SaveChangesAsync();

    logger.LogInformation("Updated URL with code: {Code} -> {OriginalURL}", code, dto.OriginalURL);
    return Results.Ok(url);
})
.Produces<TinyUrl>(StatusCodes.Status200OK);


app.MapGet("/{code}", async (string code,TinyUrlDbContext db,ILogger<Program> logger) =>
{
    var url = await db.TinyUrls.FirstOrDefaultAsync(x => x.Code == code);
    if (url == null) return Results.NotFound();

    url.TotalClicks++;
    await db.SaveChangesAsync();

    logger.LogInformation("Redirected code: {Code} -> {OriginalURL}", code, url.OriginalURL);
    return Results.Redirect(url.OriginalURL, false);
})
.Produces<string>(StatusCodes.Status302Found);


app.MapGet("/api/public", async (TinyUrlDbContext db,ILogger<Program> logger) =>
{
    var urls = await db.TinyUrls.Where(x => !x.IsPrivate).ToListAsync();
    logger.LogInformation("Fetched public URLs: {Count}", urls.Count);
    return Results.Ok(urls);
})
.Produces<List<TinyUrl>>(StatusCodes.Status200OK);



app.Run();


static string GenerateShortCode()
{
    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    var random = new Random();
    return new string(Enumerable.Range(0, 6)
        .Select(_ => chars[random.Next(chars.Length)]).ToArray());
}
