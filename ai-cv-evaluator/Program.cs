using ai_cv_evaluator.BackgroundJobs;
using ai_cv_evaluator.Data;
using ai_cv_evaluator.Services;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

var builder = WebApplication.CreateBuilder(args);

// Ambil connection string dari appsettings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Registrasi DbContext dengan provider PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Tambahkan service lainnya
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<EvaluationWorker>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "ai-cv-evaluator");
});
builder.Services.AddScoped<AiEvaluationService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<RAGService>();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<EvaluationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
