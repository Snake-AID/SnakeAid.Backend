# Jenkins Pipeline for ASP.NET - Introduction

## What is it?
This Jenkins pipeline automates the build, test, and release process for the SnakeAid ASP.NET Core Backend. It uses Docker to ensure consistent build environments and artifacts.

## Why use it?
- **Automated Builds**: Every Pull Request (PR) is automatically built to ensure code quality before merging.
- **Docker Integration**: Builds the application into a Docker container, making it ready for deployment on any platform (ZimaOS, VPS, Cloud).
- **Notifications**: Sends status updates (Success/Failure) to Discord, alerting the team immediately.
- **Continuous Delivery**: Merges to `main` branch automatically push a new Docker image image to Docker Hub with version tags.

## Use Cases
- **PR Checks**: When a developer opens a PR, Jenkins builds the code to verify it compiles and the Docker image can be built.
- **Release Automation**: When code is merged to `main`, a new version is published to Docker Hub (`snakeaid/backend:latest` and `:BUILD_NUMBER`).
