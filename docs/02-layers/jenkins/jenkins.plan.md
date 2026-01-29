# Jenkins Pipeline for ASP.NET Core - Implementation Plan

## Current State
- Existing `Jenkinsfile` is a template for Java (Maven).
- Project is ASP.NET Core (`SnakeAid.Api`).
- No `Dockerfile` exists in the repository.

## Proposed Changes
1.  **Modify `Jenkinsfile`**:
    - Replace Maven commands with `dotnet` CLI commands (`restore`, `build`, `test`).
    - Update Docker image naming to `snakeaid-backend`.
    - Update build stages to reflect ASP.NET Core workflow.
    - Keep Discord notification logic but mark credentials as needing update.
2.  **Create `Dockerfile`**:
    - Create a multi-stage `Dockerfile` for building and running the ASP.NET Core API (`SnakeAid.Api`).
3.  **Create Documentation**:
    - Follow the structure defined in `docs/README.md`.
    - `jenkins.introduction.md`: Overview of the pipeline.
    - `jenkins.usageguide.md`: Setup instructions for Jenkins on ZimaOS.
    - `jenkins.sourcecode.md`: Explanation of the pipeline code.

## Files to Modify
- `Jenkinsfile`

## Files to Create
- `Dockerfile`
- `docs/Jenkins/jenkins.introduction.md`
- `docs/Jenkins/jenkins.plan.md` (this file)
- `docs/Jenkins/jenkins.sourcecode.md`
- `docs/Jenkins/jenkins.usageguide.md`
