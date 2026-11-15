# FCG.Catalog — Microsserviço de Catálogo

Este repositório contém o **microsserviço de Catálogo** do sistema FIAP Cloud Games (FCG).
Ele é uma das quatro peças independentes que formam o sistema — para entender como ele se
conecta com as outras, veja o repositório
[`FCG.Orchestration`](../FCG.Orchestration),
que explica o projeto como um todo em linguagem simples.

Este README explica só a parte deste serviço, também em linguagem simples, assumindo que quem
lê não tem experiência técnica.

---

## 1. O que este serviço faz

Este serviço é a **loja** da plataforma: ele cuida do catálogo de jogos disponíveis, do início
de uma compra e da biblioteca de jogos de cada usuário (a lista de jogos que ele já possui).

Diferente dos serviços de Pagamentos e Notificações, este **tem uma API HTTP** — ou seja, outros
sistemas (como um site ou aplicativo) se conectam diretamente nele para consultar jogos, iniciar
compras e ver a biblioteca de um usuário. Ele também guarda seus próprios dados em um banco de
dados (SQL Server), assim como o serviço de Usuários.

Ele oferece cinco grupos de funcionalidades:

1. **Catálogo de jogos** — listar, consultar, cadastrar, atualizar e remover jogos. Consultar é
   liberado para qualquer um; cadastrar, atualizar e remover exigem estar autenticado (login).
2. **Compra de um jogo** — quando um usuário logado decide comprar um jogo, este serviço registra
   o pedido e avisa o resto do sistema para que o pagamento seja processado.
3. **Biblioteca do usuário** — consultar quais jogos um usuário já possui, depois de compras
   aprovadas.
4. **Avaliações de jogos** — qualquer usuário autenticado pode avaliar um jogo (nota de 1 a 5 +
   comentário livre); consultar as avaliações é público. Ver seção 8.
5. **Busca avançada de jogos** — procurar jogos por texto, mesmo com erros de digitação
   ("zeldda" encontra "Zelda"), com os resultados mais relevantes primeiro. Ver seção 9.

### O fluxo de compra, passo a passo

1. Um usuário autenticado pede para comprar um jogo (`POST /api/v1/purchases`).
2. Este serviço registra o pedido e publica um aviso, `OrderPlacedEvent`, contando o ID do
   pedido, do usuário, do jogo e o preço.
3. O microsserviço de Pagamentos **recebe** esse aviso, processa (simula) o pagamento, e publica
   de volta um `PaymentProcessedEvent` com o resultado (`Approved` ou `Rejected`).
4. Este serviço **recebe** esse segundo aviso. Se o pagamento foi aprovado, o jogo é adicionado
   à biblioteca do usuário. Se foi rejeitado, nada acontece.

```
POST /purchases  ──►  OrderPlacedEvent  ──►  [Pagamentos processa]  ──►  PaymentProcessedEvent
                                                                                    │
                                                                    Approved? ──► adiciona à biblioteca
```

Antes de registrar o pedido, o serviço faz algumas verificações e pode responder com erro:

- **Comprar para outro usuário** — se o `UserId` enviado no corpo do pedido não for o mesmo do
  usuário logado (do token), responde `403` com `"Você não pode realizar compras para outro
  usuário."`. Isso impede que alguém logado compre em nome de terceiros.
- **Jogo já na biblioteca** — se o usuário já possui o jogo, responde `409` com `"Você já possui
  esse jogo em sua biblioteca."`, sem publicar nenhum evento.
- **Jogo inexistente** — se o jogo informado não existir, responde `404` com `"Jogo não
  encontrado."`.

---

## 2. Como as peças se conectam (RabbitMQ e HTTP)

Este serviço se conecta ao resto do sistema de duas formas:

- **HTTP**, para receber pedidos diretos (consultar jogos, comprar, ver biblioteca) — protegido
  por autenticação via **JWT** (um "crachá digital" que comprova que o usuário está logado).
- **RabbitMQ**, um "correio" compartilhado entre todos os microsserviços, para avisar e ser
  avisado sobre o andamento de uma compra (veja a explicação completa no README do repositório
  de orquestração).

| Direção | Evento | Exchange no RabbitMQ | Fila (queue) |
|---|---|---|---|
| Envia (publica) | `OrderPlacedEvent` | `order-placed-event` | `payments-order-placed-event` (do lado do Pagamentos) |
| Recebe (consome) | `PaymentProcessedEvent` | `payment-processed-event` | `catalog-payment-processed-event` |

O nome da fila é prefixado com o nome do serviço (`catalog-`, via `SetEndpointNameFormatter`) para
que dois serviços independentes que consomem o mesmo tipo de evento não acabem compartilhando uma
única fila física — é o caso do `PaymentProcessedEvent`, consumido tanto por este serviço quanto
pelo de Notificações. Sem o prefixo, os dois disputariam a mesma fila e as mensagens seriam
divididas entre eles, em vez de cada serviço receber todas.

A biblioteca usada para falar com o RabbitMQ é o **MassTransit**, um "tradutor" que evita ter
que lidar com os detalhes técnicos do protocolo do RabbitMQ na mão. A serialização das mensagens
usa `UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true)` em vez do envelope padrão do
MassTransit, porque cada serviço define sua própria cópia local de cada evento (em seu próprio
namespace), então a correspondência é feita pela estrutura do JSON, não pelo nome técnico da classe.

---

## 3. Estrutura de pastas

```
FCG.Catalog/
├── FCG.Catalog.Domain/           # regras de negócio (não sabe nada sobre banco/RabbitMQ/Web)
│   ├── Dto/                       # formato dos eventos, requisições e respostas (GameRequest, GameSearchResult...)
│   ├── Entities/                  # Game, LibraryItem, Review — o que é salvo no banco
│   ├── Enums/                     # PaymentStatus (Approved, Rejected)
│   ├── Interfaces/                # ICacheService e ISearchService (contratos do cache e da busca,
│   │                              #   sem saber que por trás estão o Redis e o Elasticsearch)
│   ├── Interfaces/IRepository/    # contratos de acesso a dados (SQL Server e MongoDB)
│   ├── Interfaces/IService/       # contratos dos serviços
│   ├── Services/                  # regras de catálogo, compra, biblioteca e avaliações
│   └── Validators/                # validação das requisições (FluentValidation)
├── FCG.Catalog.Infra/             # acesso a dados e infraestrutura
│   ├── Config/                     # mapeamento das entidades EF Core para tabelas (SQL Server)
│   ├── Context/                    # CatalogContext — a conexão com o SQL Server
│   ├── Migrations/                 # histórico de alterações no banco relacional
│   ├── Mongo/                      # mapeamento da entidade Review para o MongoDB
│   ├── Cache/                      # implementação do cache (RedisCacheService)
│   ├── Search/                     # implementação da busca (ElasticsearchService + ElasticsearchSettings)
│   └── Repository/                 # implementação do acesso a dados (SQL Server e MongoDB)
├── FCG.Catalog.Api/                # ponto de entrada do programa (API HTTP)
│   ├── Controllers/                 # GamesController, PurchasesController, LibraryController, ReviewsController
│   ├── Consumers/                   # "ouvintes" dos eventos do RabbitMQ (PaymentProcessedEvent)
│   ├── Extensions/                  # configuração (banco, Mongo, Redis, Elasticsearch, JWT, RabbitMQ, Swagger, logs)
│   ├── Middleware/                  # tratamento de erros
│   └── Program.cs                   # arquivo que liga tudo e inicia o serviço
├── FCG.Catalog.Tests/              # testes automatizados
├── k8s/                             # manifestos de deploy no Kubernetes (ver seção 6)
│   ├── secret.yaml.example          # MODELO do Secret (sem valores reais) — o secret.yaml de verdade não vai para o Git
│   └── local/elasticsearch.yaml     # Elasticsearch dentro do cluster, só para uso local / plano B
├── .github/workflows/ci-cd.yml      # pipeline de CI/CD no GitHub Actions (ver seção 10)
├── Dockerfile                       # receita para empacotar este serviço em um container
└── .dockerignore                    # o que NÃO entra na imagem (bin/, obj/, k8s/, logs/...)
```

---

## 4. Como rodar sozinho (sem os outros microsserviços)

Você pode rodar este serviço isolado, desde que tenha um RabbitMQ e um SQL Server disponíveis —
útil para testar sem precisar subir o projeto inteiro. (MongoDB, Redis e Elasticsearch também são
usados; sem o Elasticsearch a API sobe normalmente, só a busca da seção 9 fica indisponível.)

> **Senhas e chaves não ficam mais no código.** O `appsettings.json` não traz a senha do banco, a
> chave do JWT nem usuário/senha do RabbitMQ. Antes de rodar, você precisa informar esses valores
> por fora — senão a API **nem inicia** (ela para logo na subida com o erro
> `JwtSettings:SecretKey não configurada.`). Há duas formas: o `dotnet user-secrets` (um "cofre"
> local do .NET, guardado fora da pasta do projeto) ou variáveis de ambiente.

### Opção A — via .NET direto (para quem tem o SDK instalado)

Na primeira vez, guarde os segredos no cofre local (troque os valores entre `<...>`):

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "<mesma-chave-jwt-do-servico-de-usuarios>" --project FCG.Catalog.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=catalog;User Id=sa;Password=<senha-do-sa>;TrustServerCertificate=True" --project FCG.Catalog.Api
```

Depois, é só rodar:

```bash
dotnet run --project FCG.Catalog.Api
```

> O cofre do `user-secrets` só é lido quando a API roda no ambiente `Development` (que é o padrão do
> `dotnet run`, via `Properties/launchSettings.json`). Se preferir, os mesmos valores podem ser
> passados como variáveis de ambiente (`JwtSettings__SecretKey`, `ConnectionStrings__DefaultConnection`).
> Usuário/senha do RabbitMQ, se não forem informados, ficam no padrão `guest`/`guest`.

### Opção B — via Docker

```bash
docker build -t fcg-catalog-api:latest .
docker run --rm \
  -e RABBITMQ__HOST=host.docker.internal \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=catalog;User Id=sa;Password=<senha-do-sa>;TrustServerCertificate=True" \
  -e JwtSettings__SecretKey="<mesma-chave-jwt-do-servico-de-usuarios>" \
  -e Elasticsearch__Url=http://host.docker.internal:9200 \
  -p 5002:8080 \
  fcg-catalog-api:latest
```

> A imagem roda com um **usuário sem privilégios de administrador** (não-root), uma boa prática de
> segurança: mesmo que alguém explore uma falha na aplicação, ele não "manda" no container.

> Para rodar o sistema completo (com RabbitMQ, o banco de dados e os serviços de Pagamentos e
> Notificações juntos, prontos para o fluxo de compra funcionar de ponta a ponta), use o
> `docker-compose.yml` do repositório
> [`FCG.Orchestration`](../FCG.Orchestration) —
> é o caminho recomendado.

---

## 5. Variáveis de ambiente

A coluna **Segredo?** indica o que é dado sensível. Esses valores **não têm padrão no código** (ou
têm só um padrão de laboratório) e, no Kubernetes, ficam no Secret `catalog-secret` — nunca no Git.

| Variável | Descrição | Valor padrão (local) | Segredo? |
|---|---|---|---|
| `RabbitMQ__Host` | Endereço do servidor RabbitMQ | `localhost` | Sim (no k8s fica no Secret) |
| `RabbitMQ__VirtualHost` | "Vhost" (espaço isolado) do RabbitMQ a usar | `/` | Sim (no k8s fica no Secret) |
| `RabbitMQ__Port` | Porta do RabbitMQ (opcional; vazio = porta padrão) | — | Sim (no k8s fica no Secret) |
| `RabbitMQ__UseSsl` | Se a conexão com o RabbitMQ é criptografada (ex.: CloudAMQP) | `false` | Sim (no k8s fica no Secret) |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | `guest` (padrão do código) | **Sim** |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | `guest` (padrão do código) | **Sim** |
| `ConnectionStrings__DefaultConnection` | Endereço e credenciais do banco de dados SQL Server | sem padrão — informar via user-secrets/variável | **Sim** |
| `JwtSettings__SecretKey` | Chave secreta usada para validar o "crachá digital" (JWT) do usuário | sem padrão — **obrigatória**, a API não sobe sem ela | **Sim** |
| `JwtSettings__Issuer` | Quem deve ter emitido o token (comparado com o `iss` do JWT) | `FCG.Catalog.Api` | Não |
| `JwtSettings__Audience` | Para quem o token precisa ter sido emitido (comparado com o `aud` do JWT) | `FCG.Catalog.Client` | Não |
| `Mongo__ConnectionString` | Endereço do MongoDB (guarda as avaliações de jogos) | `mongodb://localhost:27017` | Não |
| `Mongo__Database` | Nome do banco dentro do MongoDB | `catalog_reviews` | Não |
| `Redis__ConnectionString` | Endereço do Redis (cache da listagem de jogos) | `localhost:6379` | Não |
| `Elasticsearch__Url` | Endereço do Elasticsearch (busca avançada) | `http://localhost:9200` | Sim (no k8s fica no Secret) |
| `Elasticsearch__ApiKey` | Chave de acesso ao Elastic Cloud (produção); vazia localmente, onde o Elasticsearch roda sem senha | vazio | **Sim** |
| `Elasticsearch__IndexName` | Nome do "índice" (a "gaveta") onde os jogos ficam guardados para busca | `catalog-games` | Não |

> **Onde ficam os segredos em cada ambiente:** rodando local com `dotnet run` → `dotnet
> user-secrets` ou variáveis de ambiente (seção 4); no Kubernetes local (Docker Desktop) → um
> `k8s/secret.yaml` copiado de `k8s/secret.yaml.example` e que não vai para o Git; no AKS
> (produção) → criado pela pipeline a partir dos Secrets do GitHub Actions (seção 10).

> **Acoplamento com o serviço de Usuários:** este serviço só valida (não emite) tokens JWT — quem
> emite é o [`FCG.Usuario`](../techchallenge_fase2_user). `SecretKey`, `Issuer` e `Audience` têm
> que ser **idênticos** dos dois lados, senão toda chamada autenticada recebe `401`. Isso já é
> garantido hoje em cada ambiente — ver a nota equivalente no README do Usuario (seção 5) para o
> detalhe de por que `Issuer`/`Audience` mudam de valor entre k8s/local e `docker-compose`.

> No Docker/Kubernetes, essas variáveis são escritas com **duplo underscore**
> (`RABBITMQ__HOST`), que é a forma como o .NET entende "seção : chave" vindo de variáveis de
> ambiente do sistema operacional.

---

## 6. Deploy no Kubernetes

Os manifestos estão na pasta `/k8s` deste repositório:

| Arquivo | O que faz |
|---|---|
| `deployment.yaml` | Sobe o container deste serviço em **2 cópias** (réplicas), reiniciando sozinho se cair |
| `service.yaml` | Dá o nome de rede `catalog-api` para outros serviços acharem este |
| `configmap.yaml` | Guarda as configurações que **não** são segredo (endereços do Mongo e do Redis, nome do índice de busca...) |
| `secret.yaml.example` | **Modelo** do Secret `catalog-secret`, com `<...>` no lugar dos valores reais: senha do SQL Server, string de conexão, chave do JWT, dados do RabbitMQ (host, vhost, porta, SSL, usuário, senha) e do Elasticsearch (URL, API Key) |
| `sqlserver-deployment.yaml` | Sobe o banco de dados SQL Server usado por este serviço, com disco persistente |
| `sqlserver-service.yaml` | Dá o nome de rede `catalog-sqlserver` para este serviço achar o banco |
| `mongo-deployment.yaml` | Sobe o MongoDB usado para guardar as avaliações de jogos, com disco persistente |
| `mongo-service.yaml` | Dá o nome de rede `catalog-mongo` para este serviço achar o Mongo |
| `redis-deployment.yaml` | Sobe o Redis usado como cache da listagem de jogos (sem disco: cache pode ser perdido) |
| `redis-service.yaml` | Dá o nome de rede `catalog-redis` para este serviço achar o Redis |
| `local/elasticsearch.yaml` | Sobe um Elasticsearch **dentro do cluster** — só para uso local ou como plano B (a pipeline não aplica este arquivo) |

### O Secret não fica mais no Git

O arquivo `k8s/secret.yaml` (com as senhas de verdade) **saiu do repositório** e está no
`.gitignore`. No lugar dele existe o `k8s/secret.yaml.example`, que é só um modelo. Isso importa
porque o Secret do Kubernetes guarda os dados apenas em base64, que **não é criptografia** — se o
arquivo estivesse no Git, qualquer um com acesso ao repositório leria as senhas.

- **No AKS (produção):** quem cria o Secret é a pipeline, a cada deploy, com os valores guardados
  nos Secrets do GitHub Actions (ver seção 10). Ninguém precisa mexer nesse arquivo.
- **No Kubernetes local (Docker Desktop):** copie o modelo, preencha e aplique:

```bash
cp k8s/secret.yaml.example k8s/secret.yaml   # preencha os valores <...>
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/
```

Para ter a busca funcionando localmente sem o Elastic Cloud, suba também o Elasticsearch de
laboratório e aponte `Elasticsearch__Url` para ele no seu `secret.yaml`
(`http://catalog-elasticsearch:9200`, deixando `Elasticsearch__ApiKey` vazio):

```bash
kubectl apply -f k8s/local/elasticsearch.yaml
```

### Atualização sem tirar o serviço do ar (Rolling Update)

Como a API roda em **2 réplicas**, uma versão nova é trocada aos poucos: o Kubernetes sobe **1 Pod
novo por vez** (`maxSurge: 1`) e só derruba um antigo depois que o novo responde "estou pronto" no
`/healthz` (`maxUnavailable: 0`). Ou seja, durante a troca sempre há pelo menos 2 cópias atendendo.
Cada cópia também tem um limite de CPU e memória (`resources`), para que um problema numa delas não
consuma o servidor inteiro.

### Bancos com disco persistente (PVC)

SQL Server e MongoDB guardam os dados num **disco persistente** (`PersistentVolumeClaim`): se o Pod
do banco reiniciar, os dados continuam lá. No AKS esse disco vira um disco gerenciado do Azure; no
Docker Desktop, uma pasta do computador. Como um disco desses só pode ser usado por um Pod de cada
vez, esses dois bancos são atualizados com a estratégia `Recreate` (desliga o antigo e só depois
liga o novo), e não com Rolling Update.

> **Imagem do container:** o `deployment.yaml` aponta para `fcg-catalog-api:latest`, que serve para
> rodar local. Na pipeline, esse valor é trocado automaticamente pela imagem publicada no Azure
> Container Registry com o número da execução, por exemplo `<ACR_NAME>.azurecr.io/fcg-catalog-api:42`
> — assim dá para saber exatamente qual versão está no ar.

> **Pré-requisito:** o RabbitMQ precisa estar acessível no endereço informado em `RABBITMQ__HOST`
> no Secret — seja o RabbitMQ do cluster (Service `rabbitmq`, definido no repositório
> [`FCG.Orchestration`](../FCG.Orchestration)), seja um RabbitMQ externo.

---

## 7. Testes

São **55 testes unitários**, que conferem as regras de negócio sem precisar de banco, RabbitMQ,
Redis ou Elasticsearch de verdade (esses são substituídos por "dublês" nos testes). Entre eles estão
os testes da busca: o jogo é indexado ao ser criado/atualizado, removido do índice ao ser apagado,
um termo de busca vazio não chega a consultar o Elasticsearch, o tamanho pedido é limitado entre 1
e 50, e a reindexação envia todos os jogos do banco.

```bash
dotnet test FCG.Catalog.Tests
```

Para gerar também o relatório de **cobertura de código** (quanto do código os testes exercitam),
como a pipeline faz:

```bash
dotnet test FCG.Catalog.Tests --collect "XPlat Code Coverage"
```

---

## 8. NoSQL (MongoDB) e cache de persistência (Redis)

Além do SQL Server (dados estruturados: jogos, biblioteca), este serviço usa mais dois bancos,
cada um resolvendo um problema diferente:

### MongoDB — avaliações de jogos

Um banco de dados **SQL** (como o SQL Server) exige que toda linha de uma tabela tenha as mesmas
colunas. Um banco **NoSQL** como o MongoDB guarda "documentos" (parecido com JSON) sem essa
exigência — cada avaliação é salva como está, sem precisar de uma tabela rígida. Foi escolhido
porque "avaliação de jogo" é um exemplo clássico de dado que pode variar (nem todo comentário tem
o mesmo tamanho ou os mesmos campos) e porque isolar esse dado do SQL Server evita mexer no schema
já existente do catálogo.

| Rota | Autenticação | O que faz |
|---|---|---|
| `POST /api/v1/games/{gameId}/reviews` | Exige login (JWT) | Cria uma avaliação (nota 1-5 + comentário) para o jogo |
| `GET /api/v1/games/{gameId}/reviews` | Pública | Lista as avaliações daquele jogo |

### Redis — cache da listagem de jogos

`GET /api/v1/games` é uma consulta que muita gente faz (alta leitura) e que muda pouco (baixa
escrita) — candidata perfeita para cache. O serviço aplica o padrão **cache-aside**:

1. Chega um pedido de listagem → primeiro pergunta ao Redis se já tem a resposta guardada.
2. Se tiver (*cache hit*), devolve na hora, sem tocar no SQL Server.
3. Se não tiver (*cache miss*), busca no SQL Server, devolve, e guarda a resposta no Redis por
   60 segundos.
4. Sempre que um jogo é criado, atualizado ou removido, o cache é apagado na hora (não espera os
   60 segundos) — assim ninguém vê uma lista desatualizada logo após uma mudança.

---

## 9. Busca avançada de jogos (Elasticsearch)

### Por que não buscar direto no SQL Server?

Um banco SQL é ótimo para guardar dados com segurança, mas é ruim em "busca do jeito que gente
digita". Se alguém procurar por `zeldda breth` (com erros de digitação), uma consulta comum no SQL
Server não encontra nada. O **Elasticsearch** é um motor de busca feito exatamente para isso — é a
mesma ideia da caixa de busca de uma loja online:

- **Tolera erros de digitação** (busca *fuzzy*): `zeldda breth` encontra *The Legend of Zelda:
  Breath of the Wild*, e `mrio` encontra *Super Mario Odyssey*. A regra (`fuzziness: AUTO`) aceita
  até 1 letra errada em palavras de 3 a 5 letras e até 2 em palavras maiores. A primeira letra
  precisa estar certa (`prefix_length: 1`), o que evita resultados muito aleatórios.
- **Procura em vários campos ao mesmo tempo** (título, gênero e descrição), dando **pesos**
  diferentes: acertar no título vale 3 vezes mais, no gênero 2 vezes mais do que na descrição.
- **Ordena por relevância**: os jogos que mais combinam com o que foi digitado vêm primeiro. Cada
  resultado traz um campo `score` (a "nota de relevância" calculada pelo Elasticsearch).

### Quem manda nos dados: SQL Server

O SQL Server **continua sendo a fonte da verdade** — o Elasticsearch é só uma "cópia para busca"
(um índice chamado `catalog-games`). Ele é mantido em dia assim:

1. Sempre que um jogo é **criado ou atualizado**, ele é enviado (indexado) ao Elasticsearch logo
   depois de ser salvo no SQL Server.
2. Sempre que um jogo é **removido**, ele também é retirado do índice.
3. Se o Elasticsearch recusar uma dessas operações, o problema é **registrado no log** e o
   cadastro no SQL Server segue normalmente — a escrita principal não é desfeita por causa da busca.
4. **Toda vez que a API inicia**, ela garante que o índice existe e o reconstrói a partir de todos
   os jogos do SQL Server. Assim, depois de um deploy novo ou de um índice apagado, a busca volta
   completa sozinha. Se o Elasticsearch estiver fora do ar nesse momento, a API **sobe mesmo assim**
   — só a busca fica indisponível.
5. Também é possível pedir a reconstrução do índice a qualquer momento pela rota de reindexação
   (abaixo).

### Rotas

| Rota | Autenticação | O que faz |
|---|---|---|
| `GET /api/v1/games/search?q=<texto>&size=<n>` | Pública | Busca jogos pelo texto `q`. `size` é quantos resultados devolver (padrão 10, mínimo 1, máximo 50). Se `q` vier vazio, responde `400` |
| `POST /api/v1/games/search/reindex` | Exige login (JWT) | Reconstrói o índice de busca a partir de todos os jogos do SQL Server e diz quantos foram indexados |

### Exemplos com `curl`

Buscar, mesmo digitando errado (porta `5002`, como no exemplo do Docker da seção 4):

```bash
curl "http://localhost:5002/api/v1/games/search?q=zeldda%20breth"
curl "http://localhost:5002/api/v1/games/search?q=mrio&size=5"
```

A resposta é uma lista de jogos, do mais relevante para o menos relevante:

```json
[
  {
    "id": "…",
    "title": "The Legend of Zelda: Breath of the Wild",
    "description": "…",
    "price": 299.90,
    "genre": "…",
    "score": 7.4
  }
]
```

(os valores de `id`, `price`, `genre` e `score` acima são ilustrativos)

Reconstruir o índice (precisa de um token JWT válido, obtido no login do serviço de Usuários):

```bash
curl -X POST "http://localhost:5002/api/v1/games/search/reindex" \
  -H "Authorization: Bearer <seu-token-jwt>"
# resposta: { "indexados": <quantidade de jogos> }
```

### Onde o Elasticsearch roda

- **Produção (AKS):** no **Elastic Cloud**, o serviço gerenciado da própria Elastic. A API se
  conecta com `Elasticsearch__Url` + `Elasticsearch__ApiKey`, que ficam no Secret (seção 5).
- **Local:** um Elasticsearch sem senha, seja em container, seja pelo manifesto
  `k8s/local/elasticsearch.yaml` (seção 6). Nesse caso `Elasticsearch__ApiKey` fica vazio.

---

## 10. CI/CD — pipeline no GitHub Actions

O arquivo `.github/workflows/ci-cd.yml` descreve a "esteira" automática que leva o código até
produção. Funciona assim:

```
push na main ──► Build & Test ──► Imagem (build + scan + push no ACR) ──► Deploy no AKS
Pull Request ──► só Build & Test (valida antes do merge)
push na tech_fase_4 ──► mesmo fluxo (deploy de teste antes do merge na main)
Run workflow (manual) ──► mesmo fluxo; só aparece depois que o workflow estiver na main
```

Ou seja: o que está na branch `main` é exatamente o que está rodando em produção. (Mudanças só no
`README.md` não disparam a pipeline.)

### O que cada etapa faz

1. **Build & Test** — roda em todo push na `main` e também em todo **Pull Request**. Compila a
   solução (`dotnet build`), roda os testes unitários (`dotnet test`) e guarda o resultado dos
   testes e o relatório de cobertura de código como *artifact* `TestResults` (dá para baixar na
   página da execução, na aba *Actions*). Se algum teste falhar, a esteira para aqui.
2. **Imagem Docker** — só fora de Pull Request. Monta a imagem do container com uma tag única, o
   número da execução (`github.run_number`); passa a imagem pelo **Trivy**, um "antivírus" que
   lista vulnerabilidades conhecidas de gravidade alta ou crítica (`HIGH`/`CRITICAL`) — hoje ele
   **só reporta**, não bloqueia a esteira; e por fim envia a imagem ao **Azure Container Registry
   (ACR)**, o "depósito" de imagens no Azure.
3. **Deploy no AKS** — só fora de Pull Request. Usa o *environment* `production` do GitHub (onde
   dá para ver o histórico de deploys e, se quiser, exigir aprovação manual). Ele:
   1. cria/atualiza o Secret `catalog-secret` com os valores guardados nos Secrets do GitHub (nada vem do Git);
   2. aplica o ConfigMap e os manifestos do SQL Server, MongoDB, Redis e o Service da API;
   3. aplica o `deployment.yaml` trocando a imagem pela recém-publicada no ACR — isso dispara o
      Rolling Update (seção 6);
   4. espera até 5 minutos (`kubectl rollout status`) para todos os Pods novos ficarem prontos;
   5. **se algo der errado, faz rollback automático** (`kubectl rollout undo`), voltando para a
      versão anterior. Como os Pods antigos nunca saíram do ar (`maxUnavailable: 0`), o usuário não
      percebe a falha.

### Login no Azure sem senha (OIDC)

A pipeline não guarda nenhuma senha do Azure. Em vez disso, usa **OIDC**: a cada execução, o
GitHub emite um "crachá" temporário que prova "sou a pipeline deste repositório, rodando no
*environment* `production`". O Azure foi configurado para confiar só nesse crachá (uma
*credencial federada* numa *App Registration*, com o subject
`repo:fiappostech2026/techchallenge_fase2_catalog_service:environment:production`). O crachá vale
poucos minutos, então não há senha para vazar nem para renovar.

Por isso os jobs de Imagem e Deploy usam `environment: production`: sem ele, o crachá teria outro
subject e o Azure recusaria o login.

### O que precisa existir no GitHub

Tudo fica em *Settings > Secrets and variables > Actions* do repositório.

**Secrets** (valores sensíveis — o GitHub esconde nos logs e ninguém consegue ler depois de salvos):

| Secret | O que é |
|---|---|
| `AZURE_CLIENT_ID` | ID (*Application/client ID*) da App Registration usada no login OIDC |
| `AZURE_TENANT_ID` | ID do tenant (diretório) do Azure |
| `AZURE_SUBSCRIPTION_ID` | ID da assinatura do Azure onde estão o ACR e o AKS |
| `CATALOG_SQL_PASSWORD` | Senha do usuário `sa` do SQL Server deste serviço |
| `JWT_SECRET_KEY` | Chave do JWT — a mesma do serviço de Usuários |
| `RABBITMQ_HOST` | Endereço do RabbitMQ |
| `RABBITMQ_VHOST` | Vhost do RabbitMQ (ex.: `/`) |
| `RABBITMQ_PORT` | Porta do RabbitMQ (pode ficar vazio = porta padrão) |
| `RABBITMQ_USE_SSL` | `true` ou `false` |
| `RABBITMQ_USER` | Usuário do RabbitMQ |
| `RABBITMQ_PASSWORD` | Senha do RabbitMQ |
| `ELASTICSEARCH_URL` | Endereço do Elasticsearch (Elastic Cloud) |
| `ELASTICSEARCH_API_KEY` | API Key do Elastic Cloud |

**Variables** (nomes de infraestrutura, não são segredo):

| Variable | O que é |
|---|---|
| `ACR_NAME` | Nome do Azure Container Registry (sem `.azurecr.io`) |
| `AKS_RESOURCE_GROUP` | Resource group onde está o cluster AKS |
| `AKS_CLUSTER_NAME` | Nome do cluster AKS |

Além disso, o *environment* `production` (em *Settings > Environments*) é usado pelos jobs de
Imagem e Deploy. Se ele ainda não existir, o GitHub cria na primeira execução.

### Como rodar manualmente

- **Antes do merge:** o deploy de teste roda com um `git push` na branch `tech_fase_4`. O
  GitHub só mostra o botão **Run workflow** depois que o arquivo do workflow existe na `main`;
  por isso, até o merge, a `tech_fase_4` também dispara a esteira (provisório, sai depois do
  merge).
- **Depois do merge:** a esteira roda com push na `main` ou manualmente: abra a aba **Actions**
  do repositório, escolha **CI/CD CatalogAPI** na lista à esquerda, clique em **Run workflow** e
  confirme.

Nos dois casos a execução faz o fluxo completo: build, testes, imagem e deploy no AKS.

---

## 11. Limitações conhecidas

- A aprovação/rejeição do pagamento é decidida pelo serviço de Pagamentos **por sorteio
  aleatório** — não há integração com gateway de pagamento real.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
- **MongoDB e Redis rodam sem autenticação** (sem usuário/senha). Aceitável para
  demonstração/FIAP; num deploy real, os dois teriam autenticação e não ficariam com a porta
  acessível de qualquer lugar. O mesmo vale para o Elasticsearch de laboratório
  (`k8s/local/elasticsearch.yaml`) — o de produção (Elastic Cloud) exige API Key.
- **SQL Server, MongoDB e Redis rodam como Pods dentro do próprio cluster**, e não como serviços
  gerenciados do Azure (Azure SQL, Cosmos DB, Azure Cache for Redis). Os dados do SQL Server e do
  MongoDB ficam em disco persistente, mas cada banco tem **uma única cópia** — sem réplica, backup
  automático ou failover. Num ambiente real, o caminho natural seria trocar por serviços gerenciados.
- O **scan de vulnerabilidades (Trivy)** da pipeline só **reporta** os problemas encontrados; ele
  não impede o deploy de uma imagem vulnerável (dá para mudar isso trocando `--exit-code 0` por
  `--exit-code 1` no `.github/workflows/ci-cd.yml`).
- Os **segredos não estão mais no código** (foram para `user-secrets`, Secret do Kubernetes e
  Secrets do GitHub Actions — seções 4, 5 e 10), mas a mesma chave JWT continua
  **compartilhada** com o serviço de Usuários, então trocar a chave exige atualizar os dois.
- Compras duplicadas são barradas em **duas camadas**: (1) na entrada, o `POST /api/v1/purchases`
  recusa com `409` (`"Você já possui esse jogo em sua biblioteca."`) antes mesmo de publicar
  qualquer evento; (2) como rede de segurança, se ainda assim o mesmo `PaymentProcessedEvent`
  chegar mais de uma vez (por exemplo, duas requisições concorrentes na janela rara entre a
  verificação e a gravação), o consumidor ignora a duplicata com um log de aviso, sem adicionar o
  jogo duas vezes à biblioteca.
