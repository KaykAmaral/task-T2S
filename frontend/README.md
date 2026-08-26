# Frontend Angular

Interface web do CRUD de produtos, desenvolvida com Angular 18 e TypeScript. O
frontend consome a API ASP.NET Core local durante o desenvolvimento e a API do
Render no build de produção.

Aplicação publicada: `https://task-t2s.pages.dev`.

## Funcionalidades

- Listagem de produtos.
- Cadastro e edição com o mesmo formulário reativo.
- Exclusão com confirmação.
- Validação de nome e preço.
- Estados de carregamento, atualização, vazio, sucesso e erro.
- Bloqueio de ações concorrentes.
- Layout responsivo com formulário à esquerda e tabela à direita no desktop.

## Estrutura principal

```text
src/
├── app/
│   ├── models/
│   │   └── product.ts                 # Product e DTOs de request
│   ├── products/
│   │   ├── products.component.ts      # Estado e comportamento da tela
│   │   ├── products.component.html    # Formulário e tabela
│   │   ├── products.component.css     # Tema e responsividade
│   │   └── products.component.spec.ts # Testes do componente
│   ├── services/
│   │   ├── product.service.ts         # Cinco chamadas HTTP
│   │   └── product.service.spec.ts    # Testes do contrato HTTP
│   ├── app.component.ts
│   └── app.config.ts
├── environments/
│   ├── environment.ts
│   └── environment.development.ts
├── main.ts
└── styles.css
```

## Fluxo da tela

```text
Template HTML
    ↕ bindings e eventos
ProductsComponent
    ↓ Observable
ProductService
    ↓ HttpClient
API ASP.NET Core
```

Comparando com Java/Spring:

- `ProductService` funciona como um client HTTP injetável.
- `Product`, `CreateProductRequest` e `UpdateProductRequest` equivalem aos DTOs.
- `FormGroup` representa o formulário completo.
- `FormControl` representa cada campo.
- `Validators` tem papel semelhante ao Bean Validation no cliente, mas o
  backend continua sendo responsável pela validação definitiva.
- `Observable.subscribe()` trata sucesso e erro de uma operação assíncrona.

## Pré-requisitos para desenvolvimento

- Node.js 22
- npm
- Backend executando em `http://localhost:5297`

Confirme as versões:

```powershell
node --version
npm --version
```

## Execução local

Você pode iniciar apenas Oracle e backend pelo Compose antes do Angular:

```powershell
cd ..\infra
docker compose up -d oracle backend
cd ..\frontend
```

```powershell
npm ci
npm start
```

Acesse:

```text
http://localhost:4200
```

O `ng serve` recompila e recarrega a aplicação após alterações no código. Para
parar, pressione `Ctrl + C` no terminal.

Se o PowerShell bloquear `npm.ps1`, use `npm.cmd`:

```powershell
npm.cmd ci
npm.cmd start
```

## Configuração da API

Os arquivos em `src/environments/` separam cada forma de execução:

| Arquivo | Uso | API |
|---|---|---|
| `environment.development.ts` | `npm start` | `http://localhost:5297/api` |
| `environment.compose.ts` | Docker Compose | `http://localhost:5297/api` |
| `environment.ts` | Produção/deploy | `https://task-t2s.onrender.com/api` |

O service acrescenta `/products` e expõe exatamente cinco métodos:

```text
getAll()        → GET    /api/products
getById(id)     → GET    /api/products/{id}
create(body)    → POST   /api/products
update(id, body)→ PUT    /api/products/{id}
delete(id)      → DELETE /api/products/{id}
```

## Formulário reativo

O formulário possui controles tipados para `name` e `price`.

Validações no navegador:

- Nome obrigatório.
- Nome não pode conter somente espaços.
- Nome com até 120 caracteres.
- Preço obrigatório e mínimo de `0.01`.

Ao selecionar **Editar**, o formulário recebe os dados do produto e passa para
o modo de edição. **Cancelar** limpa os controles e volta ao modo de cadastro.

## Estados da interface

- `isLoading`: primeira consulta, sem dados disponíveis.
- `isRefreshing`: nova consulta mantendo a tabela visível.
- `isSubmitting`: criação ou edição em andamento.
- `deletingProductId`: identifica a linha em exclusão.
- `isInterfaceBusy`: impede operações concorrentes.

Falhas de refresh preservam os produtos exibidos. Falhas de criação ou edição
preservam os valores do formulário para correção ou nova tentativa.

## Build de produção

```powershell
npm run build
```

Os arquivos são gerados em:

```text
dist/frontend/browser
```

## Testes

Para executar uma única vez com Edge em modo headless:

```powershell
$env:CHROME_BIN = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
npm test -- --watch=false --browsers=ChromeHeadless
```

Os 14 testes verificam:

- Renderização principal.
- Criação, edição e exclusão.
- Validação e cancelamento de ações.
- Estados de erro e preservação da lista.
- URL, método e body das cinco chamadas HTTP.

O `HttpTestingController` intercepta as chamadas do `ProductService`; os testes
não dependem do backend nem do Oracle.

## Docker

O Dockerfile possui duas etapas:

```text
node:22-alpine → npm ci + ng build
nginx:alpine   → arquivos estáticos de produção
```

Por padrão, o Dockerfile usa a configuração `production`, que aponta para o
Render. O Compose informa `production,compose` como argumento de build para
substituir somente a URL e continuar usando o backend local.

Para construir e testar somente o frontend:

```powershell
cd frontend
docker build -t product-app-frontend .
docker run --rm -p 4200:80 product-app-frontend
```

Acesse `http://localhost:4200` e pressione `Ctrl + C` para parar. A porta 4200
também corresponde à origem liberada pelo CORS do backend.

Na execução normal do projeto, prefira o Compose descrito no README da raiz:

```powershell
cd infra
docker compose up -d --build
```

O Nginx serve os arquivos estáticos, aplica cache longo aos assets e
redireciona rotas desconhecidas para `index.html`, permitindo navegação de SPA.
