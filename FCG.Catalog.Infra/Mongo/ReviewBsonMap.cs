using FCG.Catalog.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace FCG.Catalog.Infra.Mongo;

// MongoDB.Driver 3.x exige que a representação de Guid seja explícita
// (senão lança exceção ao serializar). Registrar o mapeamento aqui, em vez
// de anotar a entidade Review com atributos do driver, mantém a entidade
// (camada Domain) livre de dependência de infraestrutura — mesmo princípio
// já usado pelo EF Core neste projeto (ver FCG.Catalog.Infra/Config).
public static class ReviewBsonMap
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(Review)))
            return;

        BsonClassMap.RegisterClassMap<Review>(map =>
        {
            map.AutoMap();
            map.MapIdProperty(r => r.Id).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            map.MapProperty(r => r.GameId).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            map.MapProperty(r => r.UserId).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
        });
    }
}
