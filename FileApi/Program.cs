using FileApi.Data;
using FileApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Overwrite Kestrel max request body size to be unlimited.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;
});

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("FileApiDbConnectionString");
builder.Services.AddDbContext<FileApiDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IFileRepository, FileRepository>();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

app.MapFallbackToFile("index.html"); ;

app.Run();