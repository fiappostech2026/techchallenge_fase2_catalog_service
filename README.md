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

Ele oferece quatro grupos de funcionalidades:

1. **Catálogo de jogos** — listar, consultar, cadastrar, atualizar e remover jogos. Consultar é
   liberado para qualquer um; cadastrar, atualizar e remover exigem estar autenticado (login).
2. **Compra de um jogo** — quando um usuário logado decide comprar um jogo, este serviço registra
   o pedido e avisa o resto do sistema para que o pagamento seja processado.
3. **Biblioteca do usuário** — consultar quais jogos um usuário já possui, depois de compras
   aprovadas.
4. **Avaliações de jogos** — qualquer usuário autenticado pode avaliar um jogo (nota de 1 a 5 +
   comentário livre); consultar as avaliações é público. Ver seção 9.

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
│   ├── Dto/                       # formato dos eventos e das requisições (GameRequest, ReviewRequest...)
│   ├── Entities/                  # Game, LibraryItem, Review — o que é salvo no banco
│   ├── Enums/                     # PaymentStatus (Approved, Rejected)
│   ├── Interfaces/                # ICacheService (contrato do cache, sem saber que é Redis)
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
│   └── Repository/                 # implementação do acesso a dados (SQL Server e MongoDB)
├── FCG.Catalog.Api/                # ponto de entrada do programa (API HTTP)
│   ├── Controllers/                 # GamesController, PurchasesController, LibraryController, ReviewsController
│   ├── Consumers/                   # "ouvintes" dos eventos do RabbitMQ (PaymentProcessedEvent)
│   ├── Extensions/                  # configuração (banco, Mongo, Redis, JWT, RabbitMQ, Swagger, logs)
│   ├── Middleware/                  # tratamento de erros
│   └── Program.cs                   # arquivo que liga tudo e inicia o serviço
├── FCG.Catalog.Tests/              # testes automatizados
├── k8s/                             # manifestos de deploy no Kubernetes (ver seção 6)
└── Dockerfile                       # receita para empacotar este serviço em um container
```

---

## 4. Como rodar sozinho (sem os outros microsserviços)

Você pode rodar este serviço isolado, desde que tenha um RabbitMQ e um SQL Server disponíveis —
útil para testar sem precisar subir o projeto inteiro.

### Opção A — via .NET direto (para quem tem o SDK instalado)

```bash
dotnet run --project FCG.Catalog.Api
```

### Opção B — via Docker

```bash
docker build -t fcg-catalog-api:latest .
docker run --rm \
  -e RABBITMQ__HOST=host.docker.internal \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=catalog;User Id=sa;Password=Catalog@Strong!Pass1;TrustServerCertificate=True" \
  -p 5002:8080 \
  fcg-catalog-api:latest
```

> Para rodar o sistema completo (com RabbitMQ, o banco de dados e os serviços de Pagamentos e
> Notificações juntos, prontos para o fluxo de compra funcionar de ponta a ponta), use o
> `docker-compose.yml` do repositório
> [`FCG.Orchestration`](../FCG.Orchestration) —
> é o caminho recomendado.

---

## 5. Variáveis de ambiente

| Variável | Descrição | Valor padrão (local) |
|---|---|---|
| `RabbitMQ__Host` | Endereço do servidor RabbitMQ | `localhost` |
| `RabbitMQ__VirtualHost` | "Vhost" (espaço isolado) do RabbitMQ a usar | `/` |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | `guest` |
| `ConnectionStrings__DefaultConnection` | Endereço e credenciais do banco de dados SQL Server | `Server=localhost,1433;Database=catalog;...` |
| `JwtSettings__SecretKey` | Chave secreta usada para validar o "crachá digital" (JWT) do usuário | `tech-challenge-fase-2-fcg-chave-secreta-jwt-256bits-minimo` |
| `JwtSettings__Issuer` | Quem deve ter emitido o token (comparado com o `iss` do JWT) | `FCG.Catalog.Api` |
| `JwtSettings__Audience` | Para quem o token precisa ter sido emitido (comparado com o `aud` do JWT) | `FCG.Catalog.Client` |
| `Mongo__ConnectionString` | Endereço do MongoDB (guarda as avaliações de jogos) | `mongodb://localhost:27017` |
| `Mongo__Database` | Nome do banco dentro do MongoDB | `catalog_reviews` |
| `Redis__ConnectionString` | Endereço do Redis (cache da listagem de jogos) | `localhost:6379` |

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
| `deployment.yaml` | Sobe o container deste serviço, reiniciando sozinho se cair |
| `service.yaml` | Dá o nome de rede `catalog-api` para outros serviços acharem este |
| `configmap.yaml` | Guarda o endereço do RabbitMQ (não são segredos) |
| `secret.yaml` | Guarda usuário/senha do RabbitMQ, a string de conexão do banco e a chave do JWT (dados sensíveis) |
| `sqlserver-deployment.yaml` | Sobe o banco de dados SQL Server usado por este serviço |
| `sqlserver-service.yaml` | Dá o nome de rede `catalog-sqlserver` para este serviço achar o banco |
| `mongo-deployment.yaml` | Sobe o MongoDB usado para guardar as avaliações de jogos |
| `mongo-service.yaml` | Dá o nome de rede `catalog-mongo` para este serviço achar o Mongo |
| `redis-deployment.yaml` | Sobe o Redis usado como cache da listagem de jogos |
| `redis-service.yaml` | Dá o nome de rede `catalog-redis` para este serviço achar o Redis |

Para aplicar:

```bash
kubectl apply -f k8s/
```

> **Pré-requisito:** o RabbitMQ precisa já estar rodando no cluster com o nome de Service
> `rabbitmq` — ele é definido no repositório
> [`FCG.Orchestration`](../FCG.Orchestration).

---

## 7. Testes

```bash
dotnet test FCG.Catalog.Tests
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

## 9. Limitações conhecidas

- **Chave de assinatura JWT versionada em texto claro** no `appsettings.json` e no
  `k8s/secret.yaml` (Secret do Kubernetes usa apenas base64, que **não é criptografia**). Aceitável
  para demonstração/FIAP; num deploy real, viria de um secret manager. Mesma chave é compartilhada
  com o serviço de Usuários — ver seção 5.
- A aprovação/rejeição do pagamento é decidida pelo serviço de Pagamentos **por sorteio
  aleatório** — não há integração com gateway de pagamento real.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
- **MongoDB e Redis rodam sem autenticação** (sem usuário/senha) — mesmo padrão de simplicidade
  já usado pelo RabbitMQ (`guest`/`guest`) neste projeto. Aceitável para demonstração/FIAP; num
  deploy real, os dois teriam autenticação e não ficariam com a porta acessível de qualquer lugar.
- Compras duplicadas são barradas em **duas camadas**: (1) na entrada, o `POST /api/v1/purchases`
  recusa com `409` (`"Você já possui esse jogo em sua biblioteca."`) antes mesmo de publicar
  qualquer evento; (2) como rede de segurança, se ainda assim o mesmo `PaymentProcessedEvent`
  chegar mais de uma vez (por exemplo, duas requisições concorrentes na janela rara entre a
  verificação e a gravação), o consumidor ignora a duplicata com um log de aviso, sem adicionar o
  jogo duas vezes à biblioteca.
