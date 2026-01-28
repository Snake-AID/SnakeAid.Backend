pipeline {
    agent any

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

        /* 
        stage('Code Style Check') {
            when {
                expression { env.CHANGE_ID != null }
            }
            steps {
                // Requires .NET SDK installed on the agent
                sh 'dotnet format --verify-no-changes'
            }
        }
        */

        stage('Build Code (PR)') {
            when {
                expression { env.CHANGE_ID != null }
            }

            steps {
                script {
                    def tag = env.CHANGE_ID ? "pr-${env.CHANGE_ID}" : env.BUILD_NUMBER
                    // Build using the Dockerfile (verifies build works)
                    docker.build("${IMAGE}:${tag}", "-f Dockerfile .")
                    // '|| true' ensures the build doesn't fail if the image is already gone.
                    sh "docker rmi ${IMAGE}:${tag} --force || true"
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
                    // Build images (Assign version tag as BUILD_NUMBER)
                    // Context is '.' (root)
                    def img = docker.build("${IMAGE}:${env.BUILD_NUMBER}", "-f Dockerfile .")

                    // Push images (With credentials)
                    docker.withRegistry(REGISTRY_URL, REGISTRY_CREDENTIAL) {
                        // Push tag version (e.g., :35)
                        img.push()
                        // Push tag latest
                        img.push('latest')
                    }
                }
            }
        }
    }
}

