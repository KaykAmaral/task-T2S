# Product API

Backend de uma aplicação de cadastro de produtos, desenvolvido com ASP.NET Core
Web API e persistência em Oracle. A infraestrutura local é executada com Docker
Compose.

O escopo atual deste documento é o backend. A pasta `frontend/` está reservada
para a aplicação Angular.

## Tecnologias

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core 9
- Oracle Entity Framework Core
- Oracle Database Free
- Docker e Docker Compose
- xUnit

## Estrutura do repositório

```text
.
├── backend/
│   ├── Controllers/          # Endpoints HTTP
│   ├── Data/                 # DbContext e migrations
│   ├── DTOs/                 # Contratos de entrada e saída da API
│   ├── Models/               # Entidades persistidas
│   ├── ProductApi.Tests/     # Testes funcionais da API
│   ├── Services/             # Regras do CRUD e acesso via EF Core
│   ├── Dockerfile
│   └── Program.cs            # Configuração e pipeline da aplicação
├── frontend/                 # Reservado para o Angular
└── infra/
    ├── .env.example
    └── compose.yaml
```

O fluxo principal de uma requisição é:

```text
HTTP → ProductsController → ProductService → AppDbContext → Oracle
```

- O controller trata o contrato HTTP e os status codes.
- O service executa o CRUD e converte entidades em DTOs.
- O `AppDbContext` mapeia `Product` para a tabela `PRODUCTS`.
- O EF Core gera e executa os comandos SQL para o Oracle.

## Como executar com Docker Compose

### Pré-requisitos

- Docker Desktop ou Docker Engine com Docker Compose.
- Portas `5297` e `1521` disponíveis.

### 1. Configure as credenciais locais

No PowerShell:

```powershell
cd infra
Copy-Item .env.example .env
```

Edite `infra/.env` e substitua os valores de exemplo:

```dotenv
ORACLE_PASSWORD=uma-senha-de-administrador
APP_USER=PRODUCT_APP
APP_USER_PASSWORD=uma-senha-da-aplicacao
```

O arquivo `.env` está no `.gitignore` e não deve ser versionado.

### 2. Construa e inicie os containers

Ainda dentro de `infra/`:

```powershell
docker compose up -d --build
```

Na primeira inicialização, o download e a preparação do Oracle podem levar
alguns minutos. O Compose aguarda o healthcheck do banco antes de iniciar a API.

As migrations do EF Core são aplicadas automaticamente pelo backend quando ele
é iniciado pelo Compose.

### 3. Verifique os containers

```powershell
docker compose ps
docker compose logs -f backend
```

A API estará disponível em:

```text
http://localhost:5297
```

O tráfego do Compose funciona da seguinte maneira:

```text
localhost:5297 → backend:8080 → oracle:1521/FREEPDB1
```

Dentro da rede do Compose, `oracle` é o hostname do banco. `localhost` dentro
do container do backend apontaria para o próprio container.

## Endpoints

A API possui exatamente cinco endpoints:

| Método | Rota | Sucesso | Outros resultados |
|---|---|---:|---|
| `GET` | `/api/products` | `200 OK` | — |
| `GET` | `/api/products/{id}` | `200 OK` | `404 Not Found` |
| `POST` | `/api/products` | `201 Created` | `400 Bad Request` |
| `PUT` | `/api/products/{id}` | `204 No Content` | `400 Bad Request`, `404 Not Found` |
| `DELETE` | `/api/products/{id}` | `204 No Content` | `404 Not Found` |

O `POST` também retorna o header `Location` apontando para o recurso criado.

### Contrato de criação e atualização

```json
{
  "name": "Notebook",
  "price": 3500.00
}
```

Validações:

- `name` é obrigatório e aceita no máximo 120 caracteres.
- `price` deve estar entre `0.01` e `9999999999999999.99`.

### Contrato de resposta

```json
{
  "id": 1,
  "name": "Notebook",
  "price": 3500.00
}
```

## Exemplos com cURL

Os exemplos abaixo usam `curl.exe` para evitar o alias `curl` do Windows
PowerShell.

### Listar produtos

```powershell
curl.exe http://localhost:5297/api/products
```

### Buscar um produto

```powershell
curl.exe http://localhost:5297/api/products/1
```

### Criar um produto

```powershell
curl.exe -i -X POST http://localhost:5297/api/products `
  -H "Content-Type: application/json" `
  -d '{"name":"Notebook","price":3500.00}'
```

### Atualizar um produto

```powershell
curl.exe -i -X PUT http://localhost:5297/api/products/1 `
  -H "Content-Type: application/json" `
  -d '{"name":"Notebook Pro","price":4200.00}'
```

### Excluir um produto

```powershell
curl.exe -i -X DELETE http://localhost:5297/api/products/1
```

## Erros e validação

Requests inválidos e erros HTTP são retornados no formato Problem Details
(`application/problem+json`). Exemplo simplificado de validação:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "The Name field is required."
    ]
  }
}
```

Exceções inesperadas produzem `500 Internal Server Error` sem expor stack trace
ou detalhes internos na resposta.

## CORS e HTTPS

O backend permite requisições do frontend em `http://localhost:4200` para os
métodos `GET`, `POST`, `PUT` e `DELETE`.

O Compose publica HTTP para o ambiente local. Em uma implantação real, HTTPS
deve ser terminado e redirecionado por um proxy reverso ou ingress antes de a
requisição chegar ao container.

## Testes automatizados

Os testes exigem o SDK do .NET 9, mas não precisam do Docker ou do Oracle:

```powershell
dotnet test backend\ProductApi.Tests\ProductApi.Tests.csproj
```

A suíte utiliza xUnit e `WebApplicationFactory`, que inicia a aplicação em
memória. O `IProductService` real é substituído por uma implementação fake, por
isso os testes verificam a camada HTTP sem depender do banco.

Os cenários cobertos incluem:

- Os cinco endpoints e seus principais status codes.
- Criação, leitura, atualização e exclusão.
- Validação automática dos DTOs.
- Respostas Problem Details para `400`, `404` e `500`.
- Política de CORS.
- Ausência de detalhes internos em erros inesperados.
- Garantia de que somente os cinco endpoints obrigatórios estão mapeados.

## Migrations

As migrations estão em `backend/Data/Migrations`. Para criar uma nova migration
durante o desenvolvimento:

```powershell
dotnet tool restore
$env:ConnectionStrings__Oracle = "User Id=PRODUCT_APP;Password=sua-senha;Data Source=localhost:1521/FREEPDB1"
dotnet ef migrations add NomeDaMigration `
  --project backend\ProductApi.csproj `
  --output-dir Data\Migrations
```

No Compose, migrations pendentes são aplicadas automaticamente na inicialização
da API por meio da configuração:

```text
Database__ApplyMigrationsOnStartup=true
```

## Encerrando os containers

Dentro de `infra/`:

```powershell
docker compose down
```

Esse comando remove os containers e a rede, mas preserva o volume com os dados
do Oracle. Para parar somente o backend:

```powershell
docker compose stop backend
```

> Não use `docker compose down -v` se quiser preservar os dados. A opção `-v`
> remove também o volume do Oracle.
