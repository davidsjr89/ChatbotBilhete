using ChatbotApiNet9.Interfaces;
using ChatbotApiNet9.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao contêiner.
builder.Services.AddControllers();

// Adicionar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviços (com implementações simuladas/básicas por enquanto)
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ITicketService, SimulatedTicketService>(); // Serviço simulado
builder.Services.AddScoped<IAiService, SimulatedAiService>();       // Serviço simulado

var app = builder.Build();

// Configurar o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

