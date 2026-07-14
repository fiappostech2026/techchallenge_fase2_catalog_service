using FCG.Catalog.Domain.Interfaces.IRepository;
using FCG.Catalog.Domain.Interfaces.IService;
using FCG.Catalog.Domain.Services;
using FCG.Catalog.Domain.Validators;
using FCG.Catalog.Infra.Repository;
using FluentValidation;

namespace FCG.Catalog.Api.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ILibraryService, LibraryService>();

        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();

        services.AddValidatorsFromAssemblyContaining<GameRequestValidator>();

        return services;
    }
}
