# Product Management App

Aplicação full stack para cadastro e gerenciamento de produtos. O frontend em
Angular consome uma API ASP.NET Core, que persiste os dados em Oracle. Todo o
ambiente pode ser iniciado com um único Docker Compose.

## Funcionalidades

- Listagem de produtos com estados de carregamento, vazio e erro.
- Cadastro com formulário reativo e validação no frontend e no backend.
- Edição e exclusão com feedback visual.
- Atualização da listagem sem ocultar os dados existentes.
- Interface responsiva nas cores azul e laranja.
- API com exatamente os cinco endpoints solicitados.
- Testes automatizados de frontend e backend.

## Tecnologias

### Frontend

- Angular 18 e TypeScript
- Reactive Forms
- HttpClient e RxJS
- Jasmine e Karma
- Nginx na imagem de produção

### Backend

- .NET 9 e ASP.NET Core Web API
- Entity Framework Core 9
- Oracle Entity Framework Core
- xUnit e `WebApplicationFactory`

### Infraestrutura

- Oracle Database Free
- Docker e Docker Compose
- Imagens multi-stage para frontend e backend

## Estrutura do repositório

```text
.
├── backend/
│   ├── Controllers/          # Endpoints HTTP
│   ├── Data/                 # DbContext e migrations
│   ├── DTOs/                 # Contratos de entrada e saída
│   ├── Models/               # Entidades persistidas
│   ├── ProductApi.Tests/     # Testes da API
│   ├── Services/             # Regras do CRUD
│   └── Dockerfile
├── frontend/
│   ├── src/app/models/       # Tipos TypeScript
│   ├── src/app/products/     # Tela, formulário e testes do CRUD
│   ├── src/app/services/     # Comunicação HTTP e testes
│   ├── Dockerfile
│   └── nginx.conf
└── infra/
    ├── .env.example
    └── compose.yaml
```

O fluxo principal é:

```text
Navegador
   ↓ HTTP/JSON
Angular :4200
   ↓
ASP.NET Core :5297
   ↓
ProductService → EF Core
   ↓
Oracle :1521
```

No backend, o fluxo de uma requisição é:

```text
HTTP → ProductsController → ProductService → AppDbContext → Oracle
```

## Execução completa com Docker Compose

### Pré-requisitos

- Docker Desktop ou Docker Engine com Docker Compose.
- Portas `4200`, `5297` e `1521` disponíveis.

Node.js, .NET e Oracle não precisam estar instalados para a execução pelo
Compose.

### 1. Configure as credenciais locais

No PowerShell, a partir da raiz do repositório:

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

O arquivo `.env` está ignorado pelo Git e não deve ser versionado.

### 2. Construa e inicie a aplicação

Ainda dentro de `infra/`:

```powershell
docker compose up -d --build
```

Na primeira execução, o download e a preparação do Oracle podem levar alguns
minutos. O backend aguarda o healthcheck do banco e aplica as migrations
pendentes ao iniciar. O frontend é iniciado depois que o container do backend
entra em execução.

### 3. Acesse os serviços

| Serviço | Endereço |
|---|---|
| Frontend | `http://localhost:4200` |
| Backend | `http://localhost:5297` |
| Oracle | `localhost:1521/FREEPDB1` |

O navegador executa o JavaScript do Angular e chama
`http://localhost:5297/api`. Por isso a URL da API usa `localhost`, mesmo que o
frontend seja servido por um container Nginx.

### 4. Verifique containers e logs

```powershell
docker compose ps
docker compose logs -f
```

Para acompanhar um serviço específico:

```powershell
docker compose logs -f frontend
docker compose logs -f backend
docker compose logs -f oracle
```

### 5. Pare a aplicação

```powershell
docker compose down
```

Esse comando remove containers e rede, mas preserva o volume do Oracle. Não
use `docker compose down --volumes` se quiser manter os dados.

## API

A API possui exatamente cinco endpoints:

| Método | Rota | Sucesso | Outros resultados |
|---|---|---:|---|
| `GET` | `/api/products` | `200 OK` | — |
| `GET` | `/api/products/{id}` | `200 OK` | `404 Not Found` |
| `POST` | `/api/products` | `201 Created` | `400 Bad Request` |
| `PUT` | `/api/products/{id}` | `204 No Content` | `400 Bad Request`, `404 Not Found` |
| `DELETE` | `/api/products/{id}` | `204 No Content` | `404 Not Found` |

Não existem endpoints adicionais para produtos.

### Criação e atualização

```json
{
  "name": "Notebook",
  "price": 3500.00
}
```

Validações:

- `name` é obrigatório e aceita no máximo 120 caracteres.
- `name` não pode conter somente espaços.
- `price` deve estar entre `0.01` e `9999999999999999.99`.

### Resposta

```json
{
  "id": 1,
  "name": "Notebook",
  "price": 3500.00
}
```

Requests inválidos utilizam o formato Problem Details
(`application/problem+json`). Erros inesperados retornam `500 Internal Server
Error` sem expor stack trace.

## Exemplos com cURL

Os exemplos usam `curl.exe` para evitar o alias `curl` do Windows PowerShell.

```powershell
# Listar
curl.exe http://localhost:5297/api/products

# Buscar por ID
curl.exe http://localhost:5297/api/products/1

# Criar
curl.exe -i -X POST http://localhost:5297/api/products `
  -H "Content-Type: application/json" `
  -d '{"name":"Notebook","price":3500.00}'

# Atualizar
curl.exe -i -X PUT http://localhost:5297/api/products/1 `
  -H "Content-Type: application/json" `
  -d '{"name":"Notebook Pro","price":4200.00}'

# Excluir
curl.exe -i -X DELETE http://localhost:5297/api/products/1
```

## Desenvolvimento local do frontend

Pré-requisitos:

- Node.js 22
- npm
- Backend disponível em `http://localhost:5297`

Se quiser executar apenas Oracle e backend pelo Compose:

```powershell
cd infra
docker compose up -d oracle backend
cd ..
```

```powershell
# Se ainda estiver na raiz do repositório:
cd frontend
npm ci
npm start
```

Acesse `http://localhost:4200`. O servidor recarrega a página quando os arquivos
são modificados.

Se o PowerShell bloquear `npm.ps1`, utilize `npm.cmd` nos mesmos comandos.

Detalhes adicionais estão em [frontend/README.md](frontend/README.md).

## Testes automatizados

### Backend

Requer o SDK do .NET 9, mas não depende do Docker nem do Oracle:

```powershell
dotnet test backend\ProductApi.Tests\ProductApi.Tests.csproj
```

Os nove testes utilizam xUnit e `WebApplicationFactory`. Eles cobrem os cinco
endpoints, validações, status codes, Problem Details, CORS, tratamento de erros
e a garantia de que não existem rotas adicionais.

### Frontend

Requer Node.js 22 e Chrome ou Edge:

```powershell
cd frontend
npm ci
$env:CHROME_BIN = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Os 14 testes cobrem o serviço HTTP, os cinco endpoints consumidos, validação do
formulário, criação, edição, exclusão e estados de erro.

## Migrations

As migrations ficam em `backend/Data/Migrations`. No Compose, migrations
pendentes são aplicadas automaticamente por meio de:

```text
Database__ApplyMigrationsOnStartup=true
```

Para criar uma migration durante o desenvolvimento:

```powershell
dotnet tool restore
$env:ConnectionStrings__Oracle = "User Id=PRODUCT_APP;Password=sua-senha;Data Source=localhost:1521/FREEPDB1"
dotnet ef migrations add NomeDaMigration `
  --project backend\ProductApi.csproj `
  --output-dir Data\Migrations
```

## CORS

O backend aceita a origem `http://localhost:4200` e os métodos `GET`, `POST`,
`PUT` e `DELETE`. Em produção real, origens, HTTPS e credenciais devem ser
configurados de acordo com o ambiente.

## Solução de problemas

### Docker não inicia

Confirme se o Docker Desktop está aberto:

```powershell
docker version
```

### Porta já está em uso

```powershell
netstat -ano | findstr :4200
netstat -ano | findstr :5297
netstat -ano | findstr :1521
```

Encerre o processo conflitante ou altere apenas o mapeamento da porta no
Compose.

### Frontend exibe “API indisponível”

```powershell
cd infra
docker compose ps
docker compose logs backend
```

Confirme também que a aplicação foi acessada por `http://localhost:4200`, que é
a origem permitida pelo CORS.

### Oracle demora na primeira inicialização

É esperado que a primeira preparação leve alguns minutos. Acompanhe com:

```powershell
docker compose logs -f oracle
```

## Decisões técnicas

- Arquitetura deliberadamente simples para uma task técnica.
- Um service no backend concentra o CRUD e o acesso via EF Core.
- Um service Angular concentra as cinco chamadas HTTP.
- Reactive Forms mantém validação e estado do formulário explícitos.
- O frontend atualiza a lista local após mutações para evitar `GETs`
  desnecessários.
- O build multi-stage não leva Node.js nem o SDK .NET para as imagens finais.
- O Nginx possui fallback de SPA e cache longo para assets estáticos.
