// ---------- Helpers ----------
def utcCreated() {
    return sh(script: "date -u +%Y-%m-%dT%H:%M:%SZ", returnStdout: true).trim()
}

def normalizeRepoUrl(String rawRepo) {
    def repoUrl = rawRepo ?: ''
    if (repoUrl.startsWith('git@github.com:')) {
        repoUrl = repoUrl.replace('git@github.com:', 'https://github.com/')
    }
    return repoUrl.replaceAll(/\.git$/, '')
}

def commitUrl(String repoUrl, String commitSha) {
    return (repoUrl && commitSha) ? "${repoUrl}/commit/${commitSha}" : (repoUrl ?: '')
}

def ociLabelArgs(String tag) {
    def created = utcCreated()
    def repoUrl = normalizeRepoUrl(env.GIT_URL ?: '')
    def url = commitUrl(repoUrl, env.GIT_COMMIT ?: '')

    // IMPORTANT: must be a SINGLE LINE to avoid Jenkins `sh` interpreting `--label` as a separate command
    def labels = [
        "org.opencontainers.image.source=${repoUrl}",
        "org.opencontainers.image.revision=${env.GIT_COMMIT ?: ''}",
        "org.opencontainers.image.url=${url}",
        "org.opencontainers.image.created=${created}",
        "org.opencontainers.image.version=${tag}"
    ].collect { "--label ${it}" }.join(' ')

    // `docker.build(name, args)` already sets `-t name`, so only pass Dockerfile + labels + context
    return "-f Dockerfile ${labels} ."
}

// ---------- Pipeline ----------
pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        timestamps()
    }

    environment {
        TIME_STAMP_FORMAT = "dd-MM-yyyy HH:mm:ss"
        GITHUB_PR_URL = 'https://github.com/Snake-AID/SnakeAid.Backend/pull/'
        IMAGE = 'thekhiem7/snakeaid-api'
        REGISTRY_CREDENTIAL = 'thekhiem7-dockerhub-credentials'
        REGISTRY_URL = 'https://index.docker.io/v1/'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Code (PR & Dev)') {
            when {
                anyOf {
                    // PRs and direct pushes to 'dev' (build only; do not push).
                    expression { env.CHANGE_ID != null }
                    allOf {
                        branch 'dev'
                        not { changeRequest() }
                    }
                }
            }

            steps {
                script {
                    def tag = env.CHANGE_ID ? "pr-${env.CHANGE_ID}" : env.BUILD_NUMBER
                    docker.build("${IMAGE}:${tag}", ociLabelArgs(tag))

                    // Preserve original cleanup behavior
                    sh "docker rmi ${IMAGE}:${tag} --force || true"
                }
            }
        }

        stage('Build & Push Docker (PR -> main)') {
            when {
                // PR targeting 'main': push pr-<id> only (no latest)
                expression { env.CHANGE_ID != null && env.CHANGE_TARGET == 'main' }
            }

            steps {
                script {
                    def tag = "pr-${env.CHANGE_ID}"
                    def img = docker.build("${IMAGE}:${tag}", ociLabelArgs(tag))

                    docker.withRegistry(REGISTRY_URL, REGISTRY_CREDENTIAL) {
                        img.push()
                    }

                    // Preserve original cleanup behavior
                    sh "docker rmi ${IMAGE}:${tag} --force || true"
                    sh "docker rmi ${IMAGE}:latest --force || true"
                }
            }
        }

        stage('Build & Push Docker (Release main)') {
            when {
                // Only real pushes/merges to main (non-PR) can push latest
                allOf {
                    branch 'main'
                    not { changeRequest() }
                }
            }

            steps {
                script {
                    def tag = "latest"
                    def img = docker.build("${IMAGE}:${tag}", ociLabelArgs(tag))

                    docker.withRegistry(REGISTRY_URL, REGISTRY_CREDENTIAL) {
                        img.push('latest')
                    }

                    // Trigger Portainer redeploy AFTER latest is pushed
                    withCredentials([
                        string(credentialsId: 'portainer-snakeaid-webhook', variable: 'PORTAINER_WEBHOOK')
                    ]) {
                        sh '''
                            set -e
                            curl -fsS -X POST "$PORTAINER_WEBHOOK"
                        '''
                    }

                    // Preserve original cleanup behavior
                    sh "docker rmi ${IMAGE}:latest --force || true"
                }
            }
        }
    }
}