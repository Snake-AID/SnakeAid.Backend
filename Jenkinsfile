// ---------- Helpers ----------
def utcCreated() {
    sh(script: "date -u +%Y-%m-%dT%H:%M:%SZ", returnStdout: true).trim()
}

def normalizeRepoUrl(String rawRepo) {
    def repoUrl = rawRepo ?: ''
    if (repoUrl.startsWith('git@github.com:')) {
        repoUrl = repoUrl.replace('git@github.com:', 'https://github.com/')
    }
    repoUrl.replaceAll(/\.git$/, '')
}

def commitUrl(String repoUrl, String commitSha) {
    (repoUrl && commitSha) ? "${repoUrl}/commit/${commitSha}" : (repoUrl ?: '')
}

def guessRefName() {
    env.CHANGE_ID ? "PR-${env.CHANGE_ID}" : (env.BRANCH_NAME ?: env.GIT_BRANCH ?: 'unknown')
}

@NonCPS
Map resolveDeployContext(String branchName, String changeId, String changeTarget) {
    final boolean isPr = (changeId != null)

    if (isPr) {
        if (changeTarget == 'main') return [stage: 'staging',     environment: 'preview']
        if (changeTarget == 'dev')  return [stage: 'development', environment: 'pr']
        return [stage: 'development', environment: 'pr']
    }

    if (branchName == 'main') return [stage: 'production',  environment: 'production']
    if (branchName == 'dev')  return [stage: 'staging',     environment: 'staging']
    return [stage: 'development', environment: 'development']
}

def ociLabelArgs(String tag) {
    def created = utcCreated()
    def repoUrl = normalizeRepoUrl(env.GIT_URL ?: '')
    def url     = commitUrl(repoUrl, env.GIT_COMMIT ?: '')
    def refName = guessRefName()
    def ctx     = resolveDeployContext(env.BRANCH_NAME, env.CHANGE_ID, env.CHANGE_TARGET)

    def baseImage = "mcr.microsoft.com/dotnet/aspnet:8.0"
    def docsUrl   = "https://snake-aid.github.io/SnakeAid.Docs"

    def labels = [
        "org.opencontainers.image.source=${repoUrl}",
        "org.opencontainers.image.revision=${env.GIT_COMMIT ?: ''}",
        "org.opencontainers.image.url=${url}",
        "org.opencontainers.image.created=${created}",
        "org.opencontainers.image.version=${tag}",
        "org.opencontainers.image.ref.name=${refName}",

        "org.opencontainers.image.title=snakeaid-api",
        "org.opencontainers.image.description=SnakeAid Backend API",
        "org.opencontainers.image.vendor=SnakeAid",
        "org.opencontainers.image.documentation=${docsUrl}",
        "org.opencontainers.image.authors=thekhiem7",

        "org.opencontainers.image.base.name=${baseImage}",
        "org.opencontainers.image.build.source=jenkins",
        "org.opencontainers.image.build.version=${env.JENKINS_VERSION ?: 'unknown'}",

        "org.opencontainers.image.stage=${ctx.stage}",
        "org.opencontainers.image.environment=${ctx.environment}",
    ].collect { "--label ${it}" }.join(' ')

    return "-f Dockerfile ${labels} ."
}

def dockerBuildOnly(String tag) {
    docker.build("${env.IMAGE}:${tag}", ociLabelArgs(tag))
}

def dockerBuildAndPush(String tag) {
    def img = docker.build("${env.IMAGE}:${tag}", ociLabelArgs(tag))
    docker.withRegistry(env.REGISTRY_URL, env.REGISTRY_CREDENTIAL) {
        img.push(tag)
    }
}

@NonCPS
int resolveCacheTtlHours(String branchName, String changeId) {
    final int PR_TTL_HOURS      = 24
    final int DEV_TTL_HOURS     = 48
    final int MAIN_TTL_HOURS    = 48
    final int DEFAULT_TTL_HOURS = 24

    if (changeId != null)     return PR_TTL_HOURS
    if (branchName == 'main') return MAIN_TTL_HOURS
    if (branchName == 'dev')  return DEV_TTL_HOURS
    return DEFAULT_TTL_HOURS
}

void dockerCleanup(int cacheTtlHours) {
    sh """
        set +e

        echo "[cleanup] docker image prune (dangling only)"
        docker image prune -f

        echo "[cleanup] docker builder prune (until=${cacheTtlHours}h)"
        docker builder prune -f --filter "until=${cacheTtlHours}h"

        echo "[cleanup] docker container prune (stopped)"
        docker container prune -f

        exit 0
    """
}

// ---------- Pipeline ----------
pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        timestamps()
    }

    environment {
        IMAGE               = 'thekhiem7/snakeaid-api'
        REGISTRY_CREDENTIAL = 'thekhiem7-dockerhub-credentials'
        REGISTRY_URL        = 'https://index.docker.io/v1/'
    }

    stages {
        stage('Checkout') {
            steps { checkout scm }
        }

        stage('Build Check (PR -> dev)') {
            when { expression { env.CHANGE_ID != null && env.CHANGE_TARGET == 'dev' } }
            steps {
                script { dockerBuildOnly("pr-${env.CHANGE_ID}") }
            }
        }

        stage('Publish Dev (Merged to dev)') {
            when { allOf { branch 'dev'; not { changeRequest() } } }
            steps {
                script { dockerBuildAndPush('dev') }
            }
        }

        stage('Publish Preview (PR to main)') {
            when { expression { env.CHANGE_ID != null && env.CHANGE_TARGET == 'main' } }
            steps {
                script { dockerBuildAndPush("preview-${env.CHANGE_ID}") }
            }
        }

        stage('Publish Latest (Merged to main)') {
            when { allOf { branch 'main'; not { changeRequest() } } }
            steps {
                script { dockerBuildAndPush('latest') }
            }
        }

        stage('Deploy (Portainer Webhook)') {
            when { allOf { branch 'main'; not { changeRequest() } } }
            steps {
                withCredentials([string(credentialsId: 'portainer-snakeaid-webhook', variable: 'PORTAINER_WEBHOOK')]) {
                    sh 'curl -fsS -X POST "$PORTAINER_WEBHOOK"'
                }
            }
        }
    }

    post {
        always {
            script {
                stage('Docker Cleanup') {
                    catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {
                        def ttl = resolveCacheTtlHours(env.BRANCH_NAME, env.CHANGE_ID)
                        echo "[cleanup] ttl=${ttl}h"
                        dockerCleanup(ttl)
                    }
                }
            }
        }
    }
}
