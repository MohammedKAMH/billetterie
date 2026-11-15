FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Billetterie.slnx .
COPY Billetterie.Domain/Billetterie.Domain.csproj Billetterie.Domain/
COPY Billetterie.Application/Billetterie.Application.csproj Billetterie.Application/
COPY Billetterie.Infrastructure/Billetterie.Infrastructure.csproj Billetterie.Infrastructure/
COPY Billetterie.Api/Billetterie.Api.csproj Billetterie.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish Billetterie.Api/Billetterie.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Billetterie.Api.dll"]