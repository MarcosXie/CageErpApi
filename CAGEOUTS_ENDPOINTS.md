# CageOuts - Endpoints e Payloads

Este documento descreve os endpoints do modulo CageOuts implementado no FlyGates.Api.

Base URL:
- https://api.cageouts.com.br
- Em ambiente local: https://localhost:<porta>

## Enum de Motivo (CageOutRejectReason)

Valores aceitos para o campo Reason:
1. Peso
2. Imagem
3. Estorno
4. NaoPassouNoLeitor
5. NaoCadastrado

No payload JSON, envie o valor numerico do enum.

Exemplo:
- 1 para Peso
- 2 para Imagem

## Funcionarios (CageOutEmployee)

Rota base:
- /api/CageOutEmployee

### 1) Listar funcionarios
Metodo:
- GET /api/CageOutEmployee

Request body:
- Nao possui

Response 200 (application/json):
[
  {
    "id": "4f2e4bc2-5036-47d0-8f52-f247fd8e58f0",
    "name": "Joao Silva",
    "badgeCode": "A12345",
    "fingerprintData": "template-base64-ou-json",
    "createdAt": "2026-07-16T12:15:00Z",
    "updatedAt": "2026-07-16T12:15:00Z"
  }
]

### 2) Buscar funcionario por ID
Metodo:
- GET /api/CageOutEmployee/{id}

Path params:
- id (Guid)

Request body:
- Nao possui

Response 200 (application/json):
{
  "id": "4f2e4bc2-5036-47d0-8f52-f247fd8e58f0",
  "name": "Joao Silva",
  "badgeCode": "A12345",
  "fingerprintData": "template-base64-ou-json",
  "createdAt": "2026-07-16T12:15:00Z",
  "updatedAt": "2026-07-16T12:15:00Z"
}

Response 404:
- Quando o registro nao existir

### 3) Criar funcionario
Metodo:
- POST /api/CageOutEmployee

Request body (application/json):
{
  "name": "Joao Silva",
  "badgeCode": "A12345",
  "password": "1234",
  "fingerprintData": "template-base64-ou-json"
}

Notas:
- Password e armazenada com hash no servidor.
- BadgeCode NAO e unico na tabela.

Response 201 (application/json):
{
  "id": "4f2e4bc2-5036-47d0-8f52-f247fd8e58f0",
  "name": "Joao Silva",
  "badgeCode": "A12345",
  "fingerprintData": "template-base64-ou-json",
  "createdAt": "2026-07-16T12:15:00Z",
  "updatedAt": "2026-07-16T12:15:00Z"
}

### 4) Atualizar funcionario
Metodo:
- PUT /api/CageOutEmployee/{id}

Path params:
- id (Guid)

Request body (application/json):
{
  "name": "Joao Silva Atualizado",
  "badgeCode": "A12345",
  "password": "5678",
  "fingerprintData": "novo-template-base64-ou-json"
}

Notas:
- Password enviada no update tambem e armazenada com hash.

Response 204:
- Sem body

### 5) Remover funcionario
Metodo:
- DELETE /api/CageOutEmployee/{id}

Path params:
- id (Guid)

Request body:
- Nao possui

Response 204:
- Sem body

### 6) Validar cracha de funcionario
Metodo:
- POST /api/CageOutEmployee/validate-badge

Request body:
```json
{
  "badgeCode": "A12345"
}
```

Response 200:
```json
{
  "isValid": true
}
```

Este endpoint apenas identifica se o cracha esta cadastrado. A entrada no modo atendente ainda exige senha no endpoint de autenticacao.

### 7) Autenticar funcionario
Metodo:
- POST /api/CageOutEmployee/authenticate

Request body:
```json
{
  "badgeCode": "A12345",
  "password": "5678"
}
```

Response 200:
```json
{
  "id": "fb6ca428-e7b0-4ccd-a64a-7c9d6184f307",
  "allowedProcedures": ["Refund", "Cleaning"]
}
```

Credenciais invalidas retornam 404.

## Rejeitos (CageOutReject)

Rota base:
- /api/CageOutReject

`productImage`/`productVideo` armazenam a **chave do objeto no S3** (ex.: `rejects/images/{id}.jpg`), nunca caminho local. Nas respostas, a API calcula `productImageUrl`/`productVideoUrl` (URLs pre-assinadas, validas por tempo limitado — ver `AwsS3:PresignedUrlExpirationMinutes`) a partir dessas chaves; essas URLs nao devem ser persistidas pelo consumidor. Apenas rejeitos com `reason = 3` (Estorno) possuem video.

### 1) Listar rejeitos
Metodo:
- GET /api/CageOutReject

Request body:
- Nao possui

Response 200 (application/json):
[
  {
    "id": "8519f7ab-29e2-40f8-8f43-5cb9bdaf8e9c",
    "productCode": "7891234567890",
    "productName": "Arroz Tipo 1",
    "schedule": "2026-07-16T11:30:00Z",
    "checkoutId": "CX-01",
    "expectedWeight": 1.000,
    "realWeight": 0.842,
    "productImage": "rejects/images/8519f7ab-29e2-40f8-8f43-5cb9bdaf8e9c.jpg",
    "productVideo": "",
    "reason": 1,
    "createdAt": "2026-07-16T11:31:00Z",
    "isResolved": false,
    "resolvedAt": null,
    "productImageUrl": "https://cageouts-media-prod.s3.us-east-1.amazonaws.com/rejects/images/8519f7ab...jpg?X-Amz-Signature=...",
    "productVideoUrl": null
  }
]

### 2) Criar rejeito
Metodo:
- POST /api/CageOutReject

Request body (application/json):
{
  "productCode": "7891234567890",
  "productName": "Arroz Tipo 1",
  "schedule": "2026-07-16T11:30:00Z",
  "checkoutId": "CX-01",
  "expectedWeight": 1.000,
  "realWeight": 0.842,
  "productImage": "rejects/images/8519f7ab-29e2-40f8-8f43-5cb9bdaf8e9c.jpg",
  "productVideo": "",
  "reason": 1
}

Response 201 (application/json):
- Corpo com o registro criado, no mesmo formato do item de listagem (necessario para o PDV anexar o video do estorno depois via PATCH .../video).

### 3) Marcar rejeito como resolvido
Metodo:
- PATCH /api/CageOutReject/{id}/resolve

Efeito: apaga a imagem/video no S3 (se existirem), zera `productImage`/`productVideo` e marca `isResolved=true`, `resolvedAt=<agora>`. Idempotente (chamar de novo em um ja resolvido nao falha). Retorna 400 se `reason = 3` (Estorno) — rejeitos de estorno nao podem ser resolvidos por aqui.

Response 200: registro atualizado (mesmo formato da listagem).

### 4) Atualizar o video de um rejeito (uso interno do PDV)
Metodo:
- PATCH /api/CageOutReject/{id}/video

Request body (application/json):
{
  "productVideo": "videos/estorno/CX-01/20260904_120530_estorno_CX-01.mp4"
}

Uso: no fluxo de estorno, o rejeito e criado sem video (a gravacao ainda esta em andamento); quando a gravacao termina e e enviada ao S3, o PDV chama este endpoint para anexar a chave do video ao(s) rejeito(s) daquela sessao.

Response 200: registro atualizado (mesmo formato da listagem).

## Configuracao de retencao de midia (MediaSettings)

Rota base:
- /api/MediaSettings

Fonte unica de verdade para os dias de retencao de video (compra e estorno); o PDV consulta este endpoint periodicamente para saber quando apagar videos localmente, e a API usa o mesmo valor para sincronizar a lifecycle rule do bucket S3.

### 1) Consultar retencao configurada
Metodo:
- GET /api/MediaSettings

Response 200 (application/json):
{
  "videoRetentionDays": 7
}


## Vendas (CageOutTransaction)

Rota base:
- /api/CageOutTransaction

Uma venda concluida no CageOuts e imutavel. A API permite criar e consultar, sem edicao ou exclusao.

### 1) Registrar venda

Metodo:
- POST /api/CageOutTransaction

Request body (application/json):

```json
{
  "clientTransactionId": "ce6d33e8-8df6-405c-950d-08dac3aa51d9",
  "checkoutId": "PDV-1",
  "completedAt": "2026-08-25T15:30:00Z",
  "items": [
    {
      "productCode": "7891234567890",
      "productName": "Arroz Tipo 1",
      "quantity": 2,
      "unitPrice": 25.9
    }
  ]
}
```

Notas:
- `clientTransactionId` e gerado pelo CageOuts e e unico. Reenviar o mesmo ID devolve a venda ja registrada, sem duplicar a compra.
- A API calcula `subtotal`, `itemCount` e `totalAmount` a partir dos itens recebidos.
- Itens devem ter codigo, descricao, quantidade maior que zero e preco nao negativo.

Response 201 (application/json):

```json
{
  "id": "69bcbb68-e1ca-43b8-ac29-97020a9361b6",
  "clientTransactionId": "ce6d33e8-8df6-405c-950d-08dac3aa51d9",
  "checkoutId": "PDV-1",
  "completedAt": "2026-08-25T15:30:00Z",
  "totalAmount": 51.8,
  "itemCount": 2,
  "createdAt": "2026-08-25T15:30:02Z",
  "items": [
    {
      "productCode": "7891234567890",
      "productName": "Arroz Tipo 1",
      "quantity": 2,
      "unitPrice": 25.9,
      "subtotal": 51.8
    }
  ]
}
```

### 2) Listar vendas

Metodo:
- GET /api/CageOutTransaction

Response 200:
- Lista de vendas em ordem decrescente de horario de conclusao. Cada registro inclui total, quantidade e itens.

### 3) Consultar venda por ID

Metodo:
- GET /api/CageOutTransaction/{id}

Path params:
- id (Guid)

Response 200:
- Uma venda com seus itens, total e horario.

Response 404:
- Quando a venda nao existir.

## Tabelas criadas por migration

Migration:
- 20260716113735_AddCageOutsModule
- 20260825110231_AddCageOutTransactions

Tabelas:
- cage_out_employee
- cage_out_reject
- transactions
- transaction_items

Colunas principais:
- cage_out_employee: Name, BadgeCode (unico), Password, FingerprintData, CreatedAt, UpdatedAt
- cage_out_reject: ProductCode, ProductName, Schedule, CheckoutId, ExpectedWeight, RealWeight, ProductImage, ProductVideo, Reason, CreatedAt, UpdatedAt
- transactions: ClientTransactionId (unico), CheckoutId, CompletedAt, TotalAmount, ItemCount, CreatedAt, UpdatedAt
- transaction_items: TransactionId, ProductCode, ProductName, Quantity, UnitPrice, Subtotal
