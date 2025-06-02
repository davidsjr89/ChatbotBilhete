using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao contêiner.
builder.Services.AddControllers();

// Adicionar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviços
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ITicketService, SimulatedTicketService>(); // Serviço simulado
builder.Services.AddScoped<IAiService, SimulatedAiService>();       // Serviço simulado
builder.Services.AddScoped<IAuthService, LocalAuthService>(); // Registrar o serviço de autenticação

// Adicionar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configurar o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Adicionar CORS para permitir requisições do frontend (ajustar origins conforme necessário)
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthorization(); // Middleware de autorização padrão (ainda não configurado para JWT, etc.)

app.MapControllers();

app.Run();