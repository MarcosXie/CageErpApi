# CageOuts - Endpoints e Payloads

Este documento descreve os endpoints do modulo CageOuts implementado no FlyGates.Api.

## Halloween (area web independente)

Nao altera os contratos do self-checkout WPF. Endpoints publicos para uma festa
informal; o UUID aleatorio da sala e a chave de acesso. Nao ha listagem de salas
nem autenticacao de jogadores. Quem possui o link pode alterar pontos.

- `POST /api/halloween/sessions`: cria sala, retorna 201 com `Location`.
- `GET /api/halloween/sessions/{id}`: retorna 200 com o placar ou 404.
- `POST /api/halloween/sessions/{id}/scores`: atualiza de 1 a 4 ajustes de pontos
  atomicamente e retorna o snapshot (200).

Corpo do ajuste:

```json
{
  "operationId": "c1be29f7-7de1-400e-8ec2-6db6086f7a40",
  "changes": [
    { "player": "Andre", "game": "timer", "delta": 4 }
  ]
}
```

Snapshot: `{ "id": "<uuid>", "revision": 1, "scores": { "<jogo>":
{ "Andre": 0, "Duda": 0, "Leh": 0, "Maicon": 0 } }, "appliedOperations":
["<uuid>"] }`. `scores` sempre inclui os sete jogos: `missoes`, `mimica`,
`drink-russo`, `quem-sou-eu`, `timer`, `sobrevivencia`, `fantasia`. Os jogadores
sao fixos. O geral e a soma dos sete jogos no cliente. `revision` cresce a cada
operacao nova para impedir que respostas atrasadas revertam o placar visual.

`operationId` deve ser um UUID nao vazio, novo por acao, reutilizado em reenvios
da MESMA acao: se ja aplicado, retorna o estado atual sem reaplicar, mesmo que o
corpo seja diferente. `delta` deve ser inteiro de -100 a 100, exceto zero.
Valores/jogadores/jogos invalidos retornam 400, sala inexistente 404 e limite de
saldo por jogador/jogo (+/-1.000.000) retorna 409. Erros de armazenamento retornam
503 e nunca simulam placar vazio. Erros seguem o middleware existente da API.
Exemplos executaveis em `FlyGates.Api/Halloween.http`.

### Persistencia e publicacao

Um JSON por sala em `App_Data/halloween` relativo ao diretorio do executavel.
Override por `Halloween__DataDirectory` (caminho absoluto recomendado).
Gravacoes usam arquivo temporario e substituicao atomica; um singleton serializa
leituras/escritas para nao perder incrementos simultaneos. Reenvios permanecem
idempotentes apos reiniciar o processo. Nao usa MySQL e nao exige migrations.
Este armazenamento suporta **uma unica instancia/processo da API**; nao escalar
replicas compartilhando arquivos sem substituir o mecanismo de coordenacao.

O Compose em `deploy/docker-compose.yml` monta
`/opt/cage-erp-api/halloween:/app/App_Data/halloween`. Na VPS, criar/autorizar esse
diretorio persistente e incluir os JSONs no backup; aplicar o Compose atualizado,
nao apenas trocar a imagem. Se usar outro caminho via variavel, ajustar tambem
o volume. Sem volume, recriar o container apaga as salas.

Publicar API e frontend juntos. A UI usa a base ja configurada em
`VITE_API_BASE_URL`, sem credenciais administrativas. Missoes, cartas e progresso
do Timer ficam privados no navegador; somente os pontos sao enviados a API.

## Imagens de referencia compartilhadas de produtos

Rota base:
- `/api/cageouts/product-image-references`

As imagens sao armazenadas no bucket S3 configurado em `AwsS3:BucketName`, sob o
prefixo isolado `AwsS3:ProductImageReferencePrefix` (padrao
`product-image-references/`). Objetos de imagem ficam em
`images/{sku}/{fileName}`; marcadores permanentes de exclusao ficam em
`tombstones/{sku}/{fileName}`. A criacao usa escrita condicional para impedir
que um objeto existente seja sobrescrito. Marcadores nunca sao removidos e
prevalecem sobre qualquer imagem orfa; assim, exclusoes sincronizadas chegam a
todos os checkouts que consultam o snapshot.

Validacao de identidade: SKU ate 128 caracteres e nome de arquivo ate 180,
somente ASCII letras/numeros, `_`, `-` e `.`; nao aceita `.`/`..`, ponto final,
separadores ou o SKU reservado `.shared-sync`. Upload aceita apenas `.foreground.png`
com `image/png` e assinatura PNG ou `.jpg` com `image/jpeg` e assinatura JPEG,
ate 10 MiB. JPGs legados podem ser enviados com o nome original; a API apenas
armazena a referencia, nao os classifica como dados de reconhecimento.

### Listar snapshot completo
- `GET /api/cageouts/product-image-references`

Response 200 (`application/json`), array contendo imagens ativas e tombstones:

```json
[
  {
    "sku": "7891234567890",
    "fileName": "7891234567890.foreground.png",
    "deleted": false,
    "length": 18342,
    "lastModifiedUtc": "2026-11-17T11:13:00+00:00"
  },
  {
    "sku": "7891234567890",
    "fileName": "legacy-photo.jpg",
    "deleted": true,
    "length": 0,
    "lastModifiedUtc": "2026-11-17T11:14:00+00:00"
  }
]
```

O snapshot percorre todas as paginas do S3. Falhas no bucket retornam 503; a API
nao trata indisponibilidade como snapshot vazio.

### Baixar imagem
- `GET /api/cageouts/product-image-references/{sku}/{fileName}`
- Retorna bytes com `image/png` ou `image/jpeg`.
- 404 se inexistente; 410 se removida; 400 para identidade invalida.

### Enviar imagem
- `PUT /api/cageouts/product-image-references/{sku}/{fileName}`
- Corpo HTTP: bytes puros do arquivo (nao JSON nem multipart); `Content-Type`
  deve corresponder a extensao e assinatura.
- Retorna 201 para criacao, com `Location` apontando para o GET da imagem; 200
  quando o mesmo nome ja contem exatamente os mesmos bytes; 409 se o nome ja
  estiver associado a bytes diferentes; 410 se houver tombstone; 413 acima de
  10 MiB; 415 para tipo nao aceito.
- Nomes sao imutaveis. Para manter duas imagens locais com o mesmo nome, o
  cliente deve enviar uma delas com outro nome (por exemplo, um GUID).

### Excluir imagem globalmente
- `DELETE /api/cageouts/product-image-references/{sku}/{fileName}`
- Retorna 204 e e idempotente. Primeiro grava o tombstone permanente e depois
  remove o objeto de imagem; se a remocao falhar, o tombstone continua
  prevalecendo e uma nova tentativa pode concluir a limpeza.
- Clientes devem manter exclusoes offline em fila duravel e nunca apagar
  tombstones do servidor.

Timeout de cada operacao S3: 30 segundos. **Estas quatro rotas exigem
`X-Product-Images-Key` em todas as requisicoes**, inclusive GET. Configure o
segredo compartilhado na secao `AwsS3`, chave `ProductImageReferenceApiKey`,
preferencialmente pela variavel de ambiente
`AwsS3__ProductImageReferenceApiKey`. Nao grave o segredo no repositorio nem em
logs. Use uma chave aleatoria forte e configure o mesmo valor no cliente WPF
em `ProductImageSharing:ApiKey`. **HTTPS e obrigatorio em ambiente de
implantacao**, terminado na API ou proxy confiavel; o header contem um segredo
de acesso e nao pode trafegar em HTTP publico. Nunca registre esse header no
proxy/telemetria. HTTP somente nos testes locais de loopback.

Sem chave configurada no servidor (ou contendo somente espacos), as rotas
falham fechadas com 503 `application/problem+json`. Header ausente, duplicado
ou incorreto retorna 401 `application/problem+json`. A comparacao usa hashes
SHA-256 de tamanho fixo e comparacao em tempo constante; a chave nao e
retornada nem registrada. Esta protecao e exclusiva das rotas de referencias:
a autenticacao/autorizacao dos endpoints existentes nao foi alterada.

Permissoes IAM minimas para o principal da API:
- `s3:ListBucket` no bucket, limitado por `s3:prefix` a
  `product-image-references/*` (ou ao prefixo configurado).
- `s3:GetObject`, `s3:PutObject` e `s3:DeleteObject` em
  `arn:aws:s3:::<bucket>/<prefixo-configurado>*`.

Nao e necessario banco/migration. Nenhuma lifecycle rule e criada para este
prefixo; a regra existente continua limitada ao prefixo de videos.

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

### 8) Atualizar digital do funcionario
Metodo:
- PATCH /api/CageOutEmployee/{id}/fingerprint

Path params:
- id (Guid)

Request body (application/json):
```json
{
  "fingerprintData": "template-textFIR-retornado-pelo-sdk-do-leitor"
}
```

Notas:
- `fingerprintData` e o valor de `TextFIR` retornado pelo SDK do leitor (Nitgen eNBioBSP) apos o Enroll. Enviar string vazia remove a digital cadastrada.
- Nao altera nenhum outro campo do funcionario (nome, cracha, senha, permissoes).

Response 204:
- Sem body

## Clientes (CageOutClient)

Rota base:
- /api/CageOutClient

CRUD padrao (GET, GET/{id}, POST, PUT/{id}, DELETE/{id}).

### Imagem de fundo da tela Idle
- POST /api/CageOutClient/{id}/background-image
  - multipart/form-data, campo `file`. Aceita apenas JPG/PNG, ate 5MB.
  - Substitui a imagem anterior (apaga a antiga do S3) e retorna o `CageOutClientResponseDto`
    atualizado com `backgroundImageKey` (chave S3 estavel, usada pelo CageOuts so para detectar
    troca) e `backgroundImageUrl` (URL pre-assinada de leitura, gerada a cada consulta).
- DELETE /api/CageOutClient/{id}/background-image
  - Remove a imagem configurada (apaga do S3 e zera `backgroundImageKey`), revertendo o CageOuts
    para o layout padrao (logo) na proxima vez que o terminal entrar na tela Idle.

O CageOuts WPF resolve o Client via `CageOutUnit.clientId` (unidade configurada localmente em
`CageIdentity.UnitId`) e consulta `GET /api/CageOutClient/{clientId}` a cada retorno a tela Idle,
comparando `backgroundImageKey` com o ultimo valor conhecido para decidir se baixa a imagem de novo.

## Cage IDs (CageOutId)

Um Cage ID identifica de forma unica um terminal CageOuts e pertence a uma unidade.
O campo `identifier` agora e gerado automaticamente pela API no formato `CageId_004`
(prefixo fixo + numero sequencial global). A sequencia e unica para toda a base,
independente de cliente ou unidade, e o `identifier` nao pode ser editado apos criacao.

Rota base:
- /api/CageOutId

### Listar Cage IDs
- GET /api/CageOutId

### Buscar Cage ID
- GET /api/CageOutId/{id}

### Criar Cage ID
- POST /api/CageOutId

```json
{
  "unitId": "4f2e4bc2-5036-47d0-8f52-f247fd8e58f0",
  "isActive": true
}
```

Resposta `201 Created` retorna o `CageOutIdResponseDto` com `identifier` ja preenchido
automaticamente (ex.: `CageId_004`).

### Atualizar Cage ID
- PUT /api/CageOutId/{id}
- Corpo igual ao da criacao (`unitId` e `isActive`).
- O campo `identifier` nao e aceito para alteracao neste endpoint.

### Excluir Cage ID
- DELETE /api/CageOutId/{id}

### Vincular Cage ID a um terminal
- POST /api/CageOutId/{id}/bind
- Marca `boundAt` com a hora atual. Retorna 409 se o Cage ID já estiver vinculado
  (`boundAt` não nulo) a outro terminal. O vínculo é permanente: só é liberado por
  `/unbind` (chamado automaticamente pelo WPF ao reconfigurar para outro Cage ID,
  ou manualmente por um admin no CageErpUI para liberar um terminal desativado).

### Desvincular Cage ID
- POST /api/CageOutId/{id}/unbind
- Zera `boundAt`, tornando o Cage ID disponível novamente. Idempotente.

O CageOuts WPF consulta unidades e Cage IDs ativos no primeiro inicio, filtrando da
lista qualquer Cage ID com `boundAt` preenchido (exceto o já configurado no próprio
terminal). A selecao e persistida localmente na secao `CageIdentity` de
`appsettings.Production.json` e o `identifier` passa a ser enviado como `checkoutId`
em vendas e rejeitos.

Campos relevantes de retorno de Cage ID:
- `cageClusterId` (Guid|null): cluster vinculado.
- `clusterBoxNumber` (int|null): numero do caixa dentro do cluster (1..N).
- `lastSeenAt` (datetime|null): ultimo heartbeat.

### Heartbeat do Cage ID
- POST /api/CageOutId/{identifier}/heartbeat

Body opcional (application/json):
```json
{
  "operationalStatus": 1,
  "currentMode": "Normal",
  "liveSession": {
    "isActive": true,
    "checkoutId": "CageId_004",
    "currentTotalAmount": 18.9,
    "scannedCount": 2,
    "approvedCount": 1,
    "sessionStartedAt": "2026-09-22T16:10:22.123Z",
    "lastUpdatedAt": "2026-09-22T16:10:24.311Z",
    "photoSnapshotBase64": "...base64-jpeg...",
    "videoSnapshotBase64": "...base64-jpeg...",
    "items": [
      {
        "itemId": "f0b7fe95-c9cb-4a1d-9b5f-5753028db65a",
        "productCode": "124535012",
        "productName": "Coca",
        "quantity": 1,
        "matchStatus": "Match",
        "expectedWeightKg": 0.350,
        "realWeightKg": 0.352,
        "unitPrice": 8.90,
        "scannedAt": "2026-09-22T16:10:23.988Z"
      }
    ]
  }
}
```

Se enviado sem body, atualiza somente `lastSeenAt`.

### Sessao ao vivo por Cage ID
- GET /api/CageOutId/{id}/live-session

Retorna o snapshot mais recente da sessao de compra em andamento para o CageOut,
incluindo itens, total atual e snapshots (foto/video). Quando nao houver sessao
ativa recente, `session.isActive` vem `false` e `items` vazio.

### Demanda de sessao ao vivo por Cage ID
- GET /api/CageOutId/{identifier}/live-session-demand

Retorna se existe consumidor ativo da telemetria ao vivo para aquele Cage ID
(`shouldPublishLiveSession`). O terminal WPF usa este endpoint para so enviar
payload pesado (itens + snapshots) quando houver tela de monitoramento aberta.

## CageClusters (CageCluster)

Rota base:
- /api/CageCluster

Um cluster agrupa Cage IDs de uma mesma unidade e define a numeracao de caixas.
Essa numeracao e persistida no vinculo (`clusterBoxNumber`) e sempre compactada pela API
em `1..N` conforme a ordem recebida.

Regras:
- Todos os Cage IDs devem existir.
- Todos os Cage IDs devem pertencer a mesma `unitId` do cluster.
- `code` deve ser unico por unidade.
- Ao salvar, a API renumera sempre sem buracos.

### Listar clusters
- GET /api/CageCluster

### Buscar cluster
- GET /api/CageCluster/{id}

### Criar cluster
- POST /api/CageCluster

```json
{
  "unitId": "4f2e4bc2-5036-47d0-8f52-f247fd8e58f0",
  "name": "Frente Loja",
  "code": "CL-001",
  "isActive": true,
  "members": [
    {
      "cageOutId": "8d6be73b-43ef-4760-a0d0-702cafaf2478",
      "boxNumber": 1
    },
    {
      "cageOutId": "5c935a98-b4f9-47fb-9da4-6cbf6cf58a67",
      "boxNumber": 2
    }
  ],
  "cageOutIds": [
    "8d6be73b-43ef-4760-a0d0-702cafaf2478",
    "5c935a98-b4f9-47fb-9da4-6cbf6cf58a67"
  ]
}
```

Compatibilidade:
- `members` e o contrato recomendado para create/update.
- `cageOutIds` e mantido para clientes legados.
- Se ambos forem enviados, a API prioriza a ordem de `members`.

### Atualizar cluster
- PUT /api/CageCluster/{id}
- Corpo igual ao da criacao.

### Excluir cluster
- DELETE /api/CageCluster/{id}

Ao excluir, o cluster e removido e os Cage IDs vinculados sao desassociados.

### Snapshot de status operacional do cluster
- GET /api/CageCluster/{id}/status

Cada item de `cages` inclui:
- `boxNumber`: numero do caixa dentro do cluster.
- `identifier`: identificador do Cage ID.
- `isAvailable`: disponibilidade operacional atual.

## Monitoramento em tempo real (SignalR)

Hub:
- /hubs/cagecluster
- /hubs/cageouts/live-session

Metodos cliente -> servidor:
- `SubscribeCluster(Guid clusterId)`
- `UnsubscribeCluster(Guid clusterId)`

Evento servidor -> cliente:
- `ClusterStatusUpdated(CageClusterStatusSnapshotDto snapshot)`

Hub live-session (cliente -> servidor):
- `SubscribeCageOut(Guid cageOutId)`
- `UnsubscribeCageOut(Guid cageOutId)`

Hub live-session (servidor -> cliente):
- `SessionUpdated(CageOutLiveSessionResponseDto snapshot)`

No telão, ordene por `boxNumber` e priorize o menor numero livre.

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

## Instaladores / SDKs (Sdk)

Rota base:
- /api/Sdk

Lista e disponibiliza para download os instaladores Windows (CageOuts e CageCluster) publicados no S3. A fonte e o manifesto `sdks/manifest.json` no bucket configurado em `AwsS3:BucketName`, mantido pelo script `_deploy-local-CageErp/publish-sdk.ps1` (os `.exe` ficam em `sdks/<app>/<arquivo>`). Consumido pela tela "Instaladores" do CageErpUI.

### 1) Listar aplicativos e versoes
Metodo:
- GET /api/Sdk

Response 200 (application/json) — sempre inclui CageOuts e CageCluster, versoes em ordem decrescente:
[
  {
    "app": "CageOuts",
    "latestVersion": "1.0.1",
    "releases": [
      {
        "app": "CageOuts",
        "version": "1.0.1",
        "fileName": "CageOuts-Setup-1.0.1.exe",
        "sizeBytes": 74447187,
        "sha256": "2ded81de...",
        "releaseDate": "2026-11-11T10:29:30.1722174+00:00"
      }
    ]
  },
  { "app": "CageCluster", "latestVersion": null, "releases": [] }
]

Erros: 503 se o bucket estiver inacessivel ou o manifesto for invalido.

### 2) Gerar link de download
Metodo:
- GET /api/Sdk/{app}/{version}/download

Response 200 (application/json) — URL pre-assinada do S3 valida por 15 minutos, com `Content-Disposition: attachment`:
{
  "url": "https://cageouts-media-prod.s3.amazonaws.com/sdks/cageouts/CageOuts-Setup-1.0.1.exe?X-Amz-...",
  "fileName": "CageOuts-Setup-1.0.1.exe",
  "expiresAt": "2026-11-11T11:15:00+00:00"
}

Erros: 404 se a versao nao estiver no manifesto; 503 se nao for possivel gerar a URL.


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
# Sortimento de referências por loja

Os endpoints abaixo exigem `X-Product-Images-Key`. Para operações administrativas,
também é exigido `X-Product-Assortment-Key`, configurado no servidor como
`AwsS3__ProductImageAssortmentAdminKey`. Use HTTPS.

- `PUT /api/cageouts/product-image-assortments/{loja}`: substitui atomicamente
  o sortimento da loja. Corpo: `{"skus":["07891000100103","7891234567895"]}`.
  Até 100 mil SKUs por importação, como strings para preservar zeros à esquerda.
  Lista vazia remove todos os SKUs da loja. Retorna `204`.
- `GET /api/cageouts/product-image-assortments/{loja}/references?after=0&limit=250`:
  retorna `{generation,items,hasMore,nextAfter}`. Cada item contém `revision`,
  `sku`, `fileName`, `deleted`, `length` e `lastModifiedUtc`. Use `nextAfter` na
  próxima chamada; quando `generation` mudar, reinicie com `after=0`. Lojas
  sem sortimento retornam `404`. O limite permitido é 1 a 1000.
- `POST /api/cageouts/product-image-assortments/backfill`: indexa no MySQL as
  referências e exclusões existentes no S3 antes da ativação do novo sincronizador.
  É idempotente, mas usa a listagem completa legada: use para migração de bases
  pequenas, não no polling nem como carga inicial de uma base já massiva.
  A migração precisa terminar com `204` antes de ativar os caixas.

Uploads e exclusões continuam nas rotas de referências existentes. Novas
alterações recebem revisões no MySQL; imagens permanecem no S3. A listagem
completa anterior continua disponível para clientes legados, mas não é usada no
modo por sortimento. Aplique a migration `AddProductImageAssortments` antes de
publicar a API.
