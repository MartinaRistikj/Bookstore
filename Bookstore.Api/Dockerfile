
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Bookstore.Api/Bookstore.Api.csproj", "Bookstore.Api/"]
COPY ["Bookstore.Application/Bookstore.Application.csproj", "Bookstore.Application/"]
COPY ["Bookstore.Domain/Bookstore.Domain.csproj", "Bookstore.Domain/"]
COPY ["Bookstore.Infrastructure/Bookstore.Infrastructure.csproj", "Bookstore.Infrastructure/"]

RUN dotnet restore "Bookstore.Api/Bookstore.Api.csproj"

COPY . .

RUN dotnet publish "Bookstore.Api/Bookstore.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
