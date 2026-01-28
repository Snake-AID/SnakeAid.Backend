# Jenkins Pipeline - Usage Guide

## Prerequisites

1.  **Jenkins Server**: Running on your ZimaOS or other server.
2.  **Docker**: Installed on the Jenkins agent (host or node). The pipeline runs `docker build` commands.
3.  **Discord Webhook**: A webhook URL for the Discord channel where you want notifications.
4.  **Docker Hub Account**: For pushing the images.

## Setup Instructions

### 1. Configure Credentials in Jenkins

Go to **Manage Jenkins** -> **Credentials** -> **System** -> **Global credentials (unrestricted)** -> **Add Credentials**.

#### A. Docker Hub Credentials
- **Kind**: Username with password
- **Scope**: Global
- **ID**: `docker-hub-credentials` (matches `Jenkinsfile`)
- **Username**: Your Docker Hub username
- **Password**: Your Docker Hub password or Access Token
- **Description**: Docker Hub Credentials

#### B. Discord Webhook
- **Kind**: Secret text
- **Scope**: Global
- **ID**: `discord_webhook_capstone` (matches `Jenkinsfile`)
- **Secret**: Paste your Discord Webhook URL here (e.g., `https://discord.com/api/webhooks/...`)
- **Description**: Discord Webhook URL

### 2. Configure Pipeline Project

1.  **New Item**: Click "New Item" on Jenkins Dashboard.
2.  **Name**: Enter a name (e.g., `SnakeAid-Backend`).
3.  **Type**: Select **Pipeline**.
4.  **Configuration**:
    - **Definition**: Pipeline from SCM.
    - **SCM**: Git.
    - **Repository URL**: `https://github.com/YourUser/SnakeAid.Backend.git` (Use your actual URL).
    - **Credentials**: Select git credentials if the repo is private.
    - **Branch Specifier**: `*/main` or leave empty for all branches (to support PR building with multibranch plugin, typically you use "Multibranch Pipeline" for PRs, but for simple Pipeline, this watches main).
    - **Script Path**: `Jenkinsfile`
5.  **Save**.

### 3. Run Pipeline
- Click **Build Now** to trigger manually.
- Or configure **Webhooks** in GitHub Settings to trigger Jenkins on push.

## Customization

### Changing Image Name
Edit `Jenkinsfile`:
```groovy
environment {
    IMAGE = 'your-username/snakeaid-backend'
}
```

### Discord IDs
Update the user IDs in `Jenkinsfile` to mention specific users on Discord:
```groovy
HOANG_DISCORD_ID = '...'
MINH_DISCORD_ID = '...'
```
