# Docker - Introduction

## What is it?
Docker is a platform for developing, shipping, and running applications in containers. `docker-compose` is a tool for defining and running multi-container Docker applications.

## Why use it?
- **Consistency**: Ensures the application runs the same way on every machine (local dev, Jenkins, production).
- **Simplicity**: `docker compose up` is all you need to start the application with all its dependencies (or configuration).
- **Isolation**: Keeps dependencies (like .NET runtime) inside the container, keeping your host machine clean.

## Use Cases
- **Local Development**: Quickly start the backend without installing the exact .NET SDK version locally.
- **Testing**: Verify that the application builds and runs correctly in a Linux environment matching production.
