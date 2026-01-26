# Docker & Docker Compose - Usage Guide

## Prerequisites
- **Docker Desktop** (Windows/Mac) or **Docker Engine** (Linux) installed.
- **Git** (optional, to clone repo).

## Running the Application Locally

We use `docker-compose` to simplify building and running the application in a containerized environment.

### 1. Start the Application
Run the following command in the root directory (where `docker-compose.yml` is located):

```bash
docker compose up --build
```

- `--build`: Forces a rebuild of the image (useful if you changed code).
- `-d`: (Optional) Run in detached mode (background).

### 2. Access the Application
Once the container is running, access the API at:
- **Swagger UI**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **API Endpoint**: `http://localhost:8080/api/...`

### 3. Stop the Application
To stop the containers:

```bash
docker compose down
```

## Configuration

The `docker-compose.yml` is configured with:
- **Port**: `8080` mapped to host `8080`.
- **Environment**: `Development` (enables Swagger).
- **Database**: Connects to the external Supabase instance defined in `appsettings.json`. No local DB container is spun up.

## Troubleshooting

- **Port Conflict**: If port 8080 is in use, modify `docker-compose.yml`:
  ```yaml
  ports:
    - "8081:8080" # Maps host 8081 to container 8080
  ```
- **Database Connection**: Ensure your machine has internet access to reach the Supabase instance.
