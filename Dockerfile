FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish FCG.Catalog.Api/FCG.Catalog.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Usuário sem privilégios de root (já existe na imagem oficial do .NET).
# O Serilog grava em logs/, então essa pasta precisa pertencer a ele.
RUN mkdir -p /app/logs && chown $APP_UID /app/logs
USER $APP_UID
ENTRYPOINT ["dotnet", "FCG.Catalog.Api.dll"]
