# API de Chatbot em .NET 9

Este projeto implementa uma API de chatbot simples construída com .NET 9. A API permite conversas básicas, simula a busca e reserva de passagens aéreas e inclui um componente simulado de IA para respostas genéricas.

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

## Exemplo de Requisição (JSON)

```json
POST /api/Chat/message
Content-Type: application/json

{
  "userId": "user123",
  "message": "Olá",
  "sessionId": null 
}
```

## Exemplo de Resposta (JSON)

```json
{
  "response": "Olá! Em que posso ajudar?",
  "sessionId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "actionData": null
}
```

## Próximos Passos e Melhorias

- **IA Real**: Substituir `SimulatedAiService` por uma integração com um serviço de IA real (como Azure OpenAI, Google Gemini, etc.).
- **Sistema de Passagens Real**: Substituir `SimulatedTicketService` por uma integração com um GDS (Global Distribution System) ou API de companhia aérea real.
- **Persistência**: Implementar persistência para o estado da sessão (ex: usando Redis ou um banco de dados) em vez do dicionário estático em memória.
- **Autenticação/Autorização**: Adicionar mecanismos de segurança adequados.
- **Tratamento de Erros**: Melhorar o tratamento de erros e logging.
- **Testes Unitários/Integração**: Adicionar testes automatizados.
- **Configuração**: Mover dados simulados ou configurações para `appsettings.json`.

