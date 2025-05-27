# ✈️ Chatbot de Reservas Aéreas - API .NET 9

Um assistente virtual inteligente para pesquisar e reservar voos com respostas naturais e fluxos conversacionais.

## Estrutura do Projeto

O projeto segue uma arquitetura padrão de API .NET, organizada da seguinte forma:

- **/Controllers**: Contém os controladores da API (ex: `ChatController`).
- **/Models**: Define os modelos de dados usados para requisições e respostas (ex: `ChatMessageRequest`, `ChatMessageResponse`, `Flight`).
- **/Services**: Contém a lógica de negócio e serviços (ex: `ChatService`, `SimulatedTicketService`, `SimulatedAiService`).
- **/Interfaces**: Define as interfaces para os serviços, promovendo baixo acoplamento (ex: `IChatService`, `ITicketService`, `IAiService`).
- **/Infrastructure**: (Opcional) Poderia conter implementações de infraestrutura, como acesso a banco de dados real, clientes HTTP para serviços externos, etc. (Neste exemplo, está vazio).
- **Program.cs**: Ponto de entrada da aplicação, configura os serviços e o pipeline HTTP.
- **ChatbotApiNet9.csproj**: Arquivo de projeto .NET, define dependências e configurações de build.
- **appsettings.json**: Arquivo de configuração da aplicação.
- **README.md**: Este arquivo.
- **.gitignore**: Especifica arquivos e diretórios a serem ignorados pelo Git.

## Funcionalidades

1.  **Conversa Geral**: O chatbot pode responder a saudações e mensagens genéricas usando um serviço de IA simulado.
2.  **Busca de Passagens**: O usuário pode solicitar a busca de voos informando destino e data (ex: "buscar voo para Lisboa em 15/07/2025"). A API retorna uma lista simulada de voos.
3.  **Reserva de Passagens**: Após a busca, o usuário pode solicitar a reserva de um voo específico informando o número do voo (ex: "reservar voo AZ101"). A API simula o processo de reserva.
4.  **Gerenciamento de Sessão**: A API mantém um contexto básico da conversa usando um ID de sessão, permitindo fluxos de múltiplos passos (como busca seguida de reserva).

## Como Executar

1.  **Pré-requisitos**: Certifique-se de ter o SDK do .NET 9 instalado.
2.  **Restaurar Dependências**: Navegue até o diretório raiz do projeto (`ChatbotApiNet9`) e execute `dotnet restore`.
3.  **Executar a API**: Execute `dotnet run` no mesmo diretório. A API estará disponível em `https://localhost:<porta>` e `http://localhost:<porta>` (as portas são definidas pelo .NET).
4.  **Testar**: Você pode usar ferramentas como Swagger (acessível em `/swagger` na URL base), Postman ou `curl` para enviar requisições POST para o endpoint `/api/Chat/message`.

## 🔍 Fluxos Disponíveis
1. Conversa Inicial

POST /api/Chat/message
Content-Type: application/json
```json

{
  "userId": "cliente123",
  "message": "Olá, preciso de ajuda",
  "sessionId": null
}
```

2. Pesquisa de Voos
   
POST /api/Chat/message
Content-Type: application/json
```json

{
  "userId": "cliente123",
  "message": "Quero voos para São Paulo em 20/06/2025",
  "sessionId": null
}
```

3. Reserva Direta (2 passos)
   
Passo 1 - Solicitação:
POST /api/Chat/message
Content-Type: application/json
```json

{
  "userId": "cliente123",
  "message": "Reservar voo LA303",
  "sessionId": null
}
```

Passo 2 - Confirmação (use o sessionId recebido):

POST /api/Chat/message
Content-Type: application/json

```json
{
  "userId": "cliente123",
  "message": "sim",
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

## 📋 Exemplos de Respostas

POST /api/Chat/message
Content-Type: application/json

```json
{
  "response": "Encontrei 2 voos para São Paulo em 20/06/2025:",
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "actionData": [
    {
      "flightNumber": "G3123",
      "origin": "RIO",
      "destination": "SAO",
      "departure": "2025-06-20T08:00:00",
      "price": 350.00
    }
  ]
}
```
## Resposta de Confirmação

```json
{
  "response": "✅ Reserva confirmada! Voo G3123 para São Paulo em 20/06. Nº do pedido: RES-2025-789",
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "actionData": null
}
```

## Exemplo de Resposta (JSON)

```json
{
    "response": "Encontrei 1 voos para Rio de Janeiro em 28/05/2025. Qual você gostaria de reservar? (Informe o número do voo)",
    "sessionId": "2baa9980-c262-4055-a573-6711c12b957a",
    "actionData": [
        {
            "flightNumber": "G3404",
            "origin": "SÃO PAULO",
            "destination": "RIO DE JANEIRO",
            "departureTime": "2025-05-28T00:00:00",
            "arrivalTime": "2025-05-27T16:29:47.1598605Z",
            "price": 350.00,
            "airline": "GOL"
        }
    ]
}
```

## 🗂 Estrutura do Projeto

## Próximos Passos e Melhorias
chatbot-api/
├── Controllers/          # Controladores API
│   └── ChatController.cs
├── Models/              # Modelos de dados
│   ├── Flight.cs        # Dados de voo
│   ├── Requests/        # Modelos de requisição
│   └── Responses/       # Modelos de resposta
├── Services/            # Lógica de negócio
│   ├── ChatService.cs   # Núcleo inteligente
│   ├── AiService/       # Processamento de linguagem
│   └── FlightService/   # Gerenciamento de voos
└── Program.cs           # Configuração inicial

## 🌟 Recursos
- Conversação natural
- Pesquisa por destino, data e companhia aérea
- Reserva em 2 passos (identificação + confirmação)
- Contexto de conversa persistente
- Simulação realista de voos

