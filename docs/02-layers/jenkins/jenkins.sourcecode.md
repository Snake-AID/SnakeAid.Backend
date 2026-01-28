# Jenkins Pipeline - Source Code

## Jenkinsfile
**Location**: [Jenkinsfile](../../Jenkinsfile)

This file defines the CI/CD pipeline using Declarative Pipeline syntax.

```groovy
pipeline {
    agent any

    environment {
        // ... (Environment variables)
        IMAGE = 'snakeaid/backend' 
    }

    stages {
        stage('Checkout') { ... }

        stage('Build Code (PR)') {
            when { expression { env.CHANGE_ID != null } }
            steps {
                script {
                    docker.build("${IMAGE}:${tag}", "-f Dockerfile .")
                }
            }
        }

        stage('Build & Push Docker (Release)') {
            when {
                allOf {
                    branch 'main'
                    not { changeRequest() }
                }
            }
            steps {
                script {
                    docker.withRegistry(REGISTRY_URL, REGISTRY_CREDENTIAL) {
                        img.push()
                        img.push('latest')
                    }
                }
            }
        }
    }
}
```

## Dockerfile
**Location**: [Dockerfile](../../Dockerfile)

Multi-stage file for building ASP.NET Core application.

```dockerfile
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SnakeAid.Api/SnakeAid.Api.csproj", "SnakeAid.Api/"]
# ... (Copy other projects)
RUN dotnet restore "SnakeAid.Api/SnakeAid.Api.csproj"
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
```
