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

Ele oferece três grupos de funcionalidades:

1. **Catálogo de jogos** — listar, consultar, cadastrar, atualizar e remover jogos. Consultar é
   liberado para qualquer um; cadastrar, atualizar e remover exigem estar autenticado (login).
2. **Compra de um jogo** — quando um usuário logado decide comprar um jogo, este serviço registra
   o pedido e avisa o resto do sistema para que o pagamento seja processado.
3. **Biblioteca do usuário** — consultar quais jogos um usuário já possui, depois de compras
   aprovadas.

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
│   ├── Dto/                       # formato dos eventos e das requisições (GameRequest, PurchaseRequest...)
│   ├── Entities/                  # Game, LibraryItem — o que é salvo no banco
│   ├── Enums/                     # PaymentStatus (Approved, Rejected)
│   ├── Interfaces/IRepository/    # contratos de acesso a dados
│   ├── Interfaces/IService/       # contratos dos serviços
│   ├── Services/                  # regras de catálogo, compra e biblioteca
│   └── Validators/                # validação das requisições (FluentValidation)
├── FCG.Catalog.Infra/             # acesso a dados (Entity Framework Core + SQL Server)
│   ├── Config/                     # mapeamento das entidades para tabelas
│   ├── Context/                    # CatalogContext — a conexão com o banco
│   ├── Migrations/                 # histórico de alterações no banco
│   └── Repository/                 # implementação do acesso a dados
├── FCG.Catalog.Api/                # ponto de entrada do programa (API HTTP)
│   ├── Controllers/                 # GamesController, PurchasesController, LibraryController
│   ├── Consumers/                   # "ouvintes" dos eventos do RabbitMQ (PaymentProcessedEvent)
│   ├── Extensions/                  # configuração (banco, JWT, RabbitMQ, Swagger, logs)
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

## 8. Limitações conhecidas

- A aprovação/rejeição do pagamento é decidida pelo serviço de Pagamentos **por sorteio
  aleatório** — não há integração com gateway de pagamento real.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
- Compras duplicadas são barradas em **duas camadas**: (1) na entrada, o `POST /api/v1/purchases`
  recusa com `409` (`"Você já possui esse jogo em sua biblioteca."`) antes mesmo de publicar
  qualquer evento; (2) como rede de segurança, se ainda assim o mesmo `PaymentProcessedEvent`
  chegar mais de uma vez (por exemplo, duas requisições concorrentes na janela rara entre a
  verificação e a gravação), o consumidor ignora a duplicata com um log de aviso, sem adicionar o
  jogo duas vezes à biblioteca.
