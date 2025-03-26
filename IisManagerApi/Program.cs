using IisManagerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviços
builder.Services.AddScoped<IWebSiteService, WebSiteService>();
builder.Services.AddScoped<SiteGroupService>();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm",
        builder =>
        {
            builder.WithOrigins(
                    "http://localhost:5001",
                    "https://localhost:5001",
                    "http://localhost:5297",
                    "https://localhost:5297"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowed(origin => true); // Permite qualquer origem em desenvolvimento
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazorWasm");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
