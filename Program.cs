using Microsoft.EntityFrameworkCore;
using TinyUrlAPI.Data;
using TinyUrlAPI.Models;


var builder = WebApplication.CreateBuilder(args);

// Add DbContext with SQLite
builder.Services.AddDbContext<TinyUrlDbContext>(options =>
    options.UseSqlite("Data Source=tinyurl.db"));

// Enable Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/", () => "TinyURL API is running!");



// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TinyUrlDbContext>();
    db.Database.EnsureCreated();
}

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ------------------- API Endpoints -------------------

// POST /api/add
app.MapPost("/api/add", async (TinyUrlAddDto dto, TinyUrlDbContext db) =>
{
    var shortCode = GenerateShortCode();
    var tinyUrl = new TinyUrl
    {
        Code = shortCode,
        OriginalURL = dto.OriginalURL,
        IsPrivate = dto.IsPrivate,
        TotalClicks = 0,
        ShortURL = $"{app.Urls.FirstOrDefault()}/{shortCode}"
    };

    db.TinyUrls.Add(tinyUrl);
    await db.SaveChangesAsync();

    return Results.Ok(tinyUrl);
})
.Produces<TinyUrl>(StatusCodes.Status200OK);


// DELETE /api/delete/{code}
app.MapDelete("/api/delete/{code}", async (string code, TinyUrlDbContext db) =>
{
    var url = await db.TinyUrls.FirstOrDefaultAsync(x => x.Code == code);
    if (url == null) return Results.NotFound("Not Found");

    db.TinyUrls.Remove(url);
    await db.SaveChangesAsync();
    return Results.Ok("Deleted");
})
.Produces<string>(StatusCodes.Status200OK);


// DELETE /api/delete-all
app.MapDelete("/api/delete-all", async (TinyUrlDbContext db) =>
{
    db.TinyUrls.RemoveRange(db.TinyUrls);
    await db.SaveChangesAsync();
    return Results.Ok("All Deleted");
})
.Produces<string>(StatusCodes.Status200OK);


// PUT /api/update/{code}
app.MapPut("/api/update/{code}", async (string code, TinyUrlAddDto dto, TinyUrlDbContext db) =>
{
    var url = await db.TinyUrls.FirstOrDefaultAsync(x => x.Code == code);
    if (url == null) return Results.NotFound();

    url.OriginalURL = dto.OriginalURL;
    url.IsPrivate = dto.IsPrivate;
    await db.SaveChangesAsync();

    return Results.Ok(url);
})
.Produces<TinyUrl>(StatusCodes.Status200OK);


// GET /{code}
app.MapGet("/{code}", async (string code, TinyUrlDbContext db) =>
{
    var url = await db.TinyUrls.FirstOrDefaultAsync(x => x.Code == code);
    if (url == null) return Results.NotFound();

    url.TotalClicks++;
    await db.SaveChangesAsync();
    return Results.Redirect(url.OriginalURL);
})
.Produces<string>(StatusCodes.Status302Found);


// GET /api/public
app.MapGet("/api/public", async (TinyUrlDbContext db) =>
{
    var urls = await db.TinyUrls.Where(x => !x.IsPrivate).ToListAsync();
    return Results.Ok(urls);
})
.Produces<List<TinyUrl>>(StatusCodes.Status200OK);


app.Run();

// ------------------- Helpers -------------------

static string GenerateShortCode()
{
    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    var random = new Random();
    return new string(Enumerable.Range(0, 6)
        .Select(_ => chars[random.Next(chars.Length)]).ToArray());
}