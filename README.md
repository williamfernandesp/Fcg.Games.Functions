# Fcg.Games.Functions

Projeto Azure Functions em `.NET 8` contendo funções de exemplo.

## Descrição
Projeto com a estrutura básica de Azure Functions. Arquivos principais: `Program.cs` e `Function1.cs`.

A função principal (Function1) é acionada por uma HttpTrigger: ela é executada quando recebe uma requisição HTTP. Pontos principais:
•	Tipo: HttpTrigger (escuta requisições HTTP).
•	Métodos suportados: normalmente GET e POST (ver Function1.cs para confirmação).
•	Rota: definida no atributo Route do trigger; se não houver, usa o padrão api/Function1.
•	Nível de autorização: AuthorizationLevel (Anonymous, Function, Admin) controla se é necessária chave de função.
•	Uso: a função lê parâmetros da query string, cabeçalhos ou do corpo da requisição e retorna um HttpResponse (JSON/text) com o código HTTP apropriado.

## Pré-requisitos
- .NET 8 SDK
- Azure Functions Core Tools (opcional, para execução local completa)
- Visual Studio, Visual Studio Code ou outro editor C#

## Como executar localmente
1. Restaurar e compilar:

```
dotnet build
```

2. Executar as funções localmente (recomendado com Azure Functions Core Tools):

```
func start
```
3. Observar as chamadas no console e para uma melhor visualização do que vem da API http://localhost:7071/api/games/random
