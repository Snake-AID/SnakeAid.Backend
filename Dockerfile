# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["SnakeAid.Api/SnakeAid.Api.csproj", "SnakeAid.Api/"]
COPY ["SnakeAid.Core/SnakeAid.Core.csproj", "SnakeAid.Core/"]
COPY ["SnakeAid.Repository/SnakeAid.Repository.csproj", "SnakeAid.Repository/"]
COPY ["SnakeAid.Service/SnakeAid.Service.csproj", "SnakeAid.Service/"]
COPY ["Directory.Packages.props", "."]

RUN dotnet restore "SnakeAid.Api/SnakeAid.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/SnakeAid.Api"
RUN dotnet build "SnakeAid.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SnakeAid.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SnakeAid.Api.dll"]
