# Documentação do Sistema de Reserva de Voos com Múltiplos Passageiros

## Visão Geral

Este documento descreve o funcionamento do sistema de reserva de voos através de um chatbot, com foco na nova funcionalidade de reserva para múltiplos passageiros. O sistema permite que o usuário reserve uma determinada quantidade de assentos e forneça os dados completos de cada passageiro que irá viajar.

## Funcionalidades Principais

1. **Busca de Voos**: O usuário pode buscar voos informando origem, destino e data.
2. **Visualização de Assentos Disponíveis**: O sistema mostra a quantidade de assentos disponíveis para cada voo.
3. **Reserva para Múltiplos Passageiros**: O usuário pode reservar vários assentos de uma vez.
4. **Coleta de Dados dos Passageiros**: O sistema coleta sequencialmente os dados completos de cada passageiro.
5. **Validação de Dados**: Todos os dados dos passageiros (nome, RG, CPF, data de nascimento) são validados.
6. **Resumo da Reserva**: Antes da confirmação final, o sistema apresenta um resumo completo da reserva.

## Fluxo de Reserva

### 1. Busca de Voos
O usuário inicia a conversa solicitando a busca de voos. Exemplo:
```
Usuário: "Buscar voo de São Paulo para Rio de Janeiro em 28/05/2025"
```

O sistema responde com os voos disponíveis, incluindo informações como:
- Número do voo
- Companhia aérea
- Horários de partida e chegada
- Preço
- Quantidade de assentos disponíveis

### 2. Seleção do Voo
O usuário seleciona um voo pelo número. Exemplo:
```
Usuário: "GO34094"
```

### 3. Informação da Quantidade de Passageiros
O sistema pergunta quantos passageiros serão incluídos na reserva, respeitando o limite de assentos disponíveis. Exemplo:
```
Sistema: "Você selecionou o voo GO34094 de SÃO PAULO para RIO DE JANEIRO em 28/05/2025 10:00.
Quantos passageiros deseja incluir nesta reserva? (Máximo: 120 assentos disponíveis)"

Usuário: "3"
```

### 4. Coleta de Dados dos Passageiros
O sistema coleta sequencialmente os dados de cada passageiro:

#### Para cada passageiro:

**Nome:**
```
Sistema: "Passageiro 1 de 3:
Por favor, informe o nome completo do passageiro:"

Usuário: "João da Silva"
```

**RG:**
```
Sistema: "Passageiro 1 de 3:
Por favor, informe o RG do passageiro (apenas números ou no formato XX.XXX.XXX-X):"

Usuário: "12.345.678-9"
```

**CPF:**
```
Sistema: "Passageiro 1 de 3:
Por favor, informe o CPF do passageiro (apenas números ou no formato XXX.XXX.XXX-XX):"

Usuário: "123.456.789-00"
```

**Data de Nascimento:**
```
Sistema: "Passageiro 1 de 3:
Por favor, informe a data de nascimento do passageiro (formato DD/MM/AAAA):"

Usuário: "01/01/1990"
```

O sistema repete este processo para cada passageiro, coletando todos os dados antes de passar para o próximo.

### 5. Resumo e Confirmação
Após coletar os dados de todos os passageiros, o sistema apresenta um resumo completo da reserva:

```
Sistema: "Resumo da reserva:
Voo: GO34094 - GOL
De: SÃO PAULO para RIO DE JANEIRO
Data/Hora: 28/05/2025 10:00
Preço por passageiro: R$ 350,00
Preço total: R$ 1.050,00

Passageiros:
1. João da Silva
   RG: 12.345.678-9
   CPF: 123.456.789-00
   Data de Nascimento: 01/01/1990
2. Maria Oliveira
   RG: 98.765.432-1
   CPF: 987.654.321-00
   Data de Nascimento: 15/05/1985
3. Pedro Santos
   RG: 45.678.901-2
   CPF: 456.789.012-34
   Data de Nascimento: 10/10/1995

Deseja confirmar esta reserva? (sim/não)"
```

### 6. Finalização da Reserva
O usuário confirma ou cancela a reserva:

```
Usuário: "sim"

Sistema: "Reserva confirmada com sucesso! Seu código de reserva é ABC123XYZ.
Obrigado por utilizar nosso serviço de reservas. Tenha uma ótima viagem!"
```

## Validações Implementadas

### Validação de CPF
- Verifica se o CPF tem 11 dígitos
- Verifica se todos os dígitos não são iguais
- Calcula e verifica os dígitos verificadores
- Aceita CPF com ou sem formatação (pontos e traço)

### Validação de RG
- Verifica se o RG tem pelo menos 8 caracteres
- Aceita RG com ou sem formatação

### Validação de Data de Nascimento
- Verifica se a data não é futura
- Verifica se a pessoa tem pelo menos 2 anos de idade

### Validação de Quantidade de Passageiros
- Verifica se a quantidade solicitada não excede o número de assentos disponíveis
- Verifica se a quantidade é pelo menos 1

## Controle de Assentos

O sistema mantém um controle de assentos disponíveis por voo:
- Cada aeronave tem uma capacidade máxima definida
- A cada reserva, os assentos são deduzidos da disponibilidade
- O sistema impede reservas que excedam a capacidade disponível

## Testes Implementados

O sistema inclui testes unitários e de integração para garantir o funcionamento correto:

1. **Testes do Modelo de Passageiro**
   - Validação de CPF (válido, inválido, formatado)
   - Validação de RG (válido, inválido)
   - Validação de data de nascimento (válida, futura, muito recente)

2. **Testes do Serviço de Reserva**
   - Busca de voos com parâmetros válidos
   - Verificação de assentos disponíveis
   - Reserva com passageiros válidos
   - Reserva com voo inválido
   - Reserva com excesso de passageiros

3. **Testes do Fluxo de Conversa**
   - Detecção de saudações
   - Solicitação de ajuda
   - Validação da quantidade de passageiros
   - Tratamento de quantidade inválida de passageiros

## Considerações Técnicas

- O sistema utiliza armazenamento em memória (sem banco de dados ou cache)
- As validações são realizadas em tempo real durante a interação
- O sistema é flexível para futuras expansões, como integração com banco de dados
