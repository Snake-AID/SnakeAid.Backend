# Devcontainer Dockerfile for SnakeAid (.NET 8)
# Extends Microsoft devcontainer image for .NET 8 and installs useful dotnet global tools.
FROM mcr.microsoft.com/devcontainers/dotnet:8.0

# Install any apt packages you need (add more if required)
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl unzip git \
    && rm -rf /var/lib/apt/lists/*

# Ensure tools installed for the non-root `vscode` user
USER vscode
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="$PATH:/home/vscode/.dotnet/tools"

# Install commonly useful dotnet global tools for development
RUN dotnet tool install --global dotnet-ef || true \
    && dotnet tool install --global dotnet-watch || true

# Default workdir (VS Code will override with workspaceFolder)
WORKDIR /workspace
