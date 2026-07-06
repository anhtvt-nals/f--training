# Artifactory (JFrog)

## Tổng quan

**JFrog Artifactory** là một universal artifact repository manager, hỗ trợ tất cả các package formats phổ biến. Đây là central hub để lưu trữ, manage, và distribute artifacts trong DevOps pipeline.

### Artifactory là gì?

Artifactory là một repository manager cho phép:
- 📦 Lưu trữ và quản lý build artifacts
- 🔒 Control access và security
- 🚀 Tăng tốc độ build bằng caching
- 📊 Track metadata và dependencies
- 🔄 Integrate với CI/CD tools

### Vấn đề Artifactory giải quyết

**Không có Artifactory:**
```
Developer → Build → Pull từ public repos → Slow, unreliable
                  → No version control
                  → Security risks
                  → Duplicate downloads
```

**Có Artifactory:**
```
Developer → Build → Pull từ Artifactory → Fast (cached)
                  → Version controlled
                  → Security scanning
                  → Single source of truth
```

## Package Types được hỗ trợ

Artifactory hỗ trợ hơn 30+ package types:

| Category | Package Types |
|----------|---------------|
| **Container** | Docker, Helm, OCI |
| **.NET** | NuGet, .NET Core |
| **Java** | Maven, Gradle, Ivy |
| **JavaScript** | npm, Yarn, Bower |
| **Python** | PyPI, Conda |
| **Ruby** | RubyGems |
| **Go** | Go modules |
| **PHP** | Composer |
| **Rust** | Cargo |
| **Generic** | Any file type |

## Kiến trúc

```
┌────────────────────────────────────────────┐
│         Developers/CI/CD Tools             │
└───────────────┬────────────────────────────┘
                │
    ┌───────────┼───────────┬────────────┐
    │           │           │            │
┌───▼────┐  ┌──▼────┐  ┌──▼─────┐  ┌───▼────┐
│ Docker │  │ Maven │  │  npm   │  │  NuGet │
│ Client │  │ Build │  │ Client │  │ Client │
└───┬────┘  └──┬────┘  └──┬─────┘  └───┬────┘
    │          │           │            │
    └──────────┼───────────┼────────────┘
               │
    ┌──────────▼──────────────┐
    │   JFrog Artifactory     │
    │  ┌──────────────────┐   │
    │  │  Virtual Repos   │   │ (Aggregate)
    │  └────────┬─────────┘   │
    │           │              │
    │  ┌────────▼─────────┐   │
    │  │   Local Repos    │   │ (Your artifacts)
    │  └────────┬─────────┘   │
    │           │              │
    │  ┌────────▼─────────┐   │
    │  │   Remote Repos   │   │ (Proxy/Cache)
    │  └──────────────────┘   │
    └─────────┬───────────────┘
              │
    ┌─────────▼──────────┐
    │   External Repos   │
    │ (Docker Hub, npm,  │
    │  Maven Central)    │
    └────────────────────┘
```

## Repository Types

### 1. Local Repository

- Lưu trữ artifacts của bạn
- Internal builds và deployments
- Full control

**Use cases:**
- Store compiled binaries
- Internal libraries
- Release artifacts

### 2. Remote Repository

- Proxy/cache external repositories
- Giảm bandwidth và tăng tốc độ
- Control access tới external resources

**Use cases:**
- Cache npm packages từ npmjs.com
- Cache Docker images từ Docker Hub
- Cache Maven artifacts từ Maven Central

### 3. Virtual Repository

- Aggregate nhiều repositories
- Single endpoint cho developers
- Simplify configuration

**Use cases:**
- Combine local + remote npm repos
- Unified Docker registry
- Aggregate multiple Maven repos

**Example setup:**
```
Virtual Repo (npm)
├── Local Repo (npm-local)     → Internal packages
├── Remote Repo (npm-remote)   → Cache npmjs.com
└── Remote Repo (npm-private)  → Private registry
```

## Cài đặt Artifactory

### Option 1: Docker (Recommended cho testing)

```bash
# Pull image
docker pull releases-docker.jfrog.io/jfrog/artifactory-oss:latest

# Run Artifactory
docker run -d \
  --name artifactory \
  -p 8081:8081 \
  -p 8082:8082 \
  -v artifactory-data:/var/opt/jfrog/artifactory \
  releases-docker.jfrog.io/jfrog/artifactory-oss:latest

# Wait for startup (khoảng 1-2 phút)
docker logs -f artifactory
```

Access: http://localhost:8082/ui/  
Default credentials: `admin` / `password`

### Option 2: Docker Compose

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  artifactory:
    image: releases-docker.jfrog.io/jfrog/artifactory-oss:latest
    container_name: artifactory
    ports:
      - "8081:8081"
      - "8082:8082"
    environment:
      - JF_SHARED_DATABASE_TYPE=postgresql
      - JF_SHARED_DATABASE_USERNAME=artifactory
      - JF_SHARED_DATABASE_PASSWORD=password
      - JF_SHARED_DATABASE_URL=jdbc:postgresql://postgres:5432/artifactory
    volumes:
      - artifactory-data:/var/opt/jfrog/artifactory
    depends_on:
      - postgres

  postgres:
    image: postgres:15-alpine
    container_name: artifactory-postgres
    environment:
      POSTGRES_DB: artifactory
      POSTGRES_USER: artifactory
      POSTGRES_PASSWORD: password
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  artifactory-data:
  postgres-data:
```

```bash
docker-compose up -d
```

### Option 3: Kubernetes (với Helm)

```bash
# Add JFrog Helm repository
helm repo add jfrog https://charts.jfrog.io
helm repo update

# Install Artifactory
helm install artifactory jfrog/artifactory-oss \
  --namespace artifactory \
  --create-namespace

# Get service URL
kubectl get svc -n artifactory
```

### Option 4: Cloud (Managed)

- **JFrog Cloud**: https://jfrog.com/start-free/
- Free tier available
- No installation required

## Setup cơ bản

### 1. First Login

1. Access UI: http://localhost:8082/
2. Login: `admin` / `password`
3. Change password (bắt buộc)
4. Complete setup wizard

### 2. Create Repositories

#### Docker Repository

**Local Repository:**
```
Administration → Repositories → Add Repository
→ Local → Docker
Repository Key: docker-local
```

**Remote Repository:**
```
Add Repository → Remote → Docker
Repository Key: docker-remote
URL: https://registry-1.docker.io/
```

**Virtual Repository:**
```
Add Repository → Virtual → Docker
Repository Key: docker
Include Repositories:
  - docker-local
  - docker-remote
Default Deployment Repository: docker-local
```

#### NuGet Repository

```
Local: nuget-local
Remote: nuget-remote (https://api.nuget.org/v3/index.json)
Virtual: nuget (aggregate above)
```

#### npm Repository

```
Local: npm-local
Remote: npm-remote (https://registry.npmjs.org)
Virtual: npm (aggregate)
```

### 3. Create Users

```
Administration → Identity and Access → Users
→ New User
Username: developer
Email: dev@example.com
Password: ********
```

### 4. Create Permissions

```
Administration → Identity and Access → Permissions
→ New Permission

Permission Name: developers-read-write
Resources:
  Repositories: docker-local, npm-local
  Actions: Read, Write, Deploy
Users/Groups: developer group
```

## Sử dụng Artifactory

### Docker

#### Configure Docker Client

```bash
# Login to Artifactory Docker registry
docker login <artifactory-url>:8081

# Example:
docker login myartifactory.example.com:8081
Username: admin
Password: ********
```

#### Push Image

```bash
# Tag image
docker tag myapp:1.0.0 myartifactory.example.com:8081/docker-local/myapp:1.0.0

# Push
docker push myartifactory.example.com:8081/docker-local/myapp:1.0.0
```

#### Pull Image

```bash
# Pull từ virtual repository
docker pull myartifactory.example.com:8081/docker/myapp:1.0.0
```

#### Docker Daemon Config

Thêm vào `/etc/docker/daemon.json`:

```json
{
  "insecure-registries": ["myartifactory.example.com:8081"],
  "registry-mirrors": ["http://myartifactory.example.com:8081"]
}
```

### NuGet (.NET)

#### Configure NuGet Source

```bash
# Add Artifactory as NuGet source
dotnet nuget add source http://myartifactory.example.com:8081/artifactory/api/nuget/v3/nuget \
  --name Artifactory \
  --username admin \
  --password password \
  --store-password-in-clear-text
```

#### Push Package

```bash
# Pack project
dotnet pack -c Release

# Push to Artifactory
dotnet nuget push MyPackage.1.0.0.nupkg \
  --source Artifactory \
  --api-key <API_KEY>
```

#### Restore Packages

```bash
# Restore từ Artifactory
dotnet restore --source Artifactory
```

#### nuget.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Artifactory" value="http://myartifactory.example.com:8081/artifactory/api/nuget/v3/nuget" />
  </packageSources>
  <packageSourceCredentials>
    <Artifactory>
      <add key="Username" value="admin" />
      <add key="ClearTextPassword" value="password" />
    </Artifactory>
  </packageSourceCredentials>
</configuration>
```

### npm

#### Configure npm Registry

```bash
# Set registry
npm config set registry http://myartifactory.example.com:8081/artifactory/api/npm/npm/

# Set authentication
npm login --registry=http://myartifactory.example.com:8081/artifactory/api/npm/npm/
```

#### .npmrc

```
registry=http://myartifactory.example.com:8081/artifactory/api/npm/npm/
//myartifactory.example.com:8081/artifactory/api/npm/npm/:_auth=YWRtaW46cGFzc3dvcmQ=
email=admin@example.com
always-auth=true
```

#### Publish Package

```bash
# Publish to Artifactory
npm publish --registry http://myartifactory.example.com:8081/artifactory/api/npm/npm-local/
```

### Maven

#### settings.xml

```xml
<settings>
  <servers>
    <server>
      <id>artifactory</id>
      <username>admin</username>
      <password>password</password>
    </server>
  </servers>
  
  <profiles>
    <profile>
      <id>artifactory</id>
      <repositories>
        <repository>
          <id>artifactory</id>
          <url>http://myartifactory.example.com:8081/artifactory/maven-virtual</url>
          <releases>
            <enabled>true</enabled>
          </releases>
          <snapshots>
            <enabled>true</enabled>
          </snapshots>
        </repository>
      </repositories>
    </profile>
  </profiles>
  
  <activeProfiles>
    <activeProfile>artifactory</activeProfile>
  </activeProfiles>
</settings>
```

#### pom.xml

```xml
<distributionManagement>
  <repository>
    <id>artifactory</id>
    <name>Artifactory Release Repository</name>
    <url>http://myartifactory.example.com:8081/artifactory/maven-local</url>
  </repository>
</distributionManagement>
```

### Helm

#### Add Artifactory as Helm Repo

```bash
# Add repo
helm repo add artifactory \
  http://myartifactory.example.com:8081/artifactory/helm-virtual \
  --username admin \
  --password password

# Update
helm repo update
```

#### Push Helm Chart

```bash
# Package chart
helm package mychart

# Push to Artifactory (Helm 3)
curl -u admin:password \
  -T mychart-1.0.0.tgz \
  "http://myartifactory.example.com:8081/artifactory/helm-local/mychart-1.0.0.tgz"
```

## JFrog CLI

### Cài đặt JFrog CLI

```bash
# macOS
brew install jfrog-cli

# Windows
choco install jfrog-cli

# Linux
curl -fL https://install-cli.jfrog.io | sh

# Verify
jf --version
```

### Configure CLI

```bash
# Configure connection
jf config add

Server ID: my-artifactory
JFrog Platform URL: http://myartifactory.example.com:8081
Username: admin
Password: ********
```

### Common Commands

```bash
# Ping Artifactory
jf rt ping

# Upload file
jf rt upload "*.jar" maven-local/com/mycompany/myapp/1.0.0/

# Download file
jf rt download maven-local/com/mycompany/myapp/1.0.0/*.jar

# Copy artifacts
jf rt copy maven-local/com/mycompany/myapp/1.0.0/ maven-release/

# Move artifacts
jf rt move maven-local/snapshots/ maven-local/archive/

# Delete artifacts
jf rt delete maven-local/old-versions/

# Search artifacts
jf rt search "maven-local/*.jar"

# Build info
jf rt build-collect myapp 1.0.0
jf rt build-publish myapp 1.0.0
```

## Integration với CI/CD

### Azure DevOps

#### Install Extension

1. Azure DevOps → Organization Settings → Extensions
2. Browse Marketplace
3. Install "Artifactory"

#### Pipeline Configuration

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  artifactoryService: 'Artifactory-Connection'

steps:
# Configure Artifactory
- task: ArtifactoryToolsInstaller@1
  inputs:
    artifactoryService: $(artifactoryService)

# Build .NET application
- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    projects: '**/*.csproj'

# Pack NuGet package
- task: DotNetCoreCLI@2
  inputs:
    command: 'pack'
    packagesToPack: '**/*.csproj'
    versioningScheme: 'off'

# Push to Artifactory
- task: ArtifactoryNuGet@2
  inputs:
    command: 'push'
    artifactoryService: $(artifactoryService)
    targetRepo: 'nuget-local'
    pathToNupkg: '$(Build.ArtifactStagingDirectory)/*.nupkg'

# Build Docker image
- task: Docker@2
  inputs:
    command: 'build'
    Dockerfile: '**/Dockerfile'
    tags: |
      $(Build.BuildId)
      latest

# Push Docker to Artifactory
- task: ArtifactoryDocker@1
  inputs:
    command: 'push'
    artifactoryService: $(artifactoryService)
    targetRepo: 'docker-local'
    imageName: 'myapp:$(Build.BuildId)'
```

### GitHub Actions

```yaml
name: Build and Push to Artifactory

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    # Setup JFrog CLI
    - uses: jfrog/setup-jfrog-cli@v3
      env:
        JF_URL: ${{ secrets.JF_URL }}
        JF_ACCESS_TOKEN: ${{ secrets.JF_ACCESS_TOKEN }}
    
    # Build .NET
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: dotnet build
    
    - name: Pack
      run: dotnet pack -c Release -o ./packages
    
    # Push to Artifactory
    - name: Upload to Artifactory
      run: |
        jf rt upload "./packages/*.nupkg" nuget-local/ \
          --build-name=myapp \
          --build-number=${{ github.run_number }}
    
    # Publish build info
    - name: Publish Build Info
      run: |
        jf rt build-collect-env myapp ${{ github.run_number }}
        jf rt build-publish myapp ${{ github.run_number }}
```

### Jenkins

```groovy
pipeline {
    agent any
    
    environment {
        ARTIFACTORY_SERVER = 'my-artifactory'
        ARTIFACTORY_REPO = 'docker-local'
    }
    
    stages {
        stage('Build') {
            steps {
                sh 'dotnet build'
            }
        }
        
        stage('Push to Artifactory') {
            steps {
                script {
                    def server = Artifactory.server(ARTIFACTORY_SERVER)
                    def rtDocker = Artifactory.docker server: server
                    
                    // Build Docker image
                    def dockerImage = rtDocker.build("myapp:${BUILD_NUMBER}")
                    
                    // Push to Artifactory
                    def buildInfo = rtDocker.push(
                        "myartifactory.example.com:8081/${ARTIFACTORY_REPO}/myapp:${BUILD_NUMBER}",
                        ARTIFACTORY_REPO
                    )
                    
                    // Publish build info
                    server.publishBuildInfo buildInfo
                }
            }
        }
    }
}
```

## Security

### Access Tokens

```bash
# Create access token
jf access-token-create my-token --user admin

# Use token in CLI
jf config add --access-token <TOKEN>

# Use in Docker
docker login myartifactory.example.com:8081 -u admin -p <TOKEN>
```

### Permission Targets

```
Administration → Security → Permissions

Name: developers-docker
Resources:
  - docker-local
  - docker-remote (read only)
Actions:
  - Read
  - Write
  - Delete (optional)
Users/Groups:
  - developers group
```

### RBAC (Role-Based Access Control)

Default roles:
- **Admin**: Full access
- **DevOps**: Deploy, delete artifacts
- **Developer**: Read, deploy
- **Viewer**: Read-only

## Backup & Maintenance

### Backup

```bash
# System backup
jf rt export /path/to/backup

# Repository backup (via API)
curl -u admin:password \
  -X POST \
  http://myartifactory.example.com:8081/api/export/system
```

### Storage Management

```bash
# Check storage
jf rt storage-info

# Cleanup old artifacts (via UI)
Administration → Artifactory → Cleanup Rules
```

### Health Check

```bash
# Ping
jf rt ping

# System health (API)
curl -u admin:password \
  http://myartifactory.example.com:8081/api/system/ping
```

## Best Practices

### 1. Repository Structure

```
docker/
├── docker-local          # Your images
├── docker-remote         # Docker Hub cache
└── docker                # Virtual (aggregate)

npm/
├── npm-local            # Private packages
├── npm-remote           # npmjs.org cache
└── npm                  # Virtual

nuget/
├── nuget-local          # Private packages
├── nuget-remote         # nuget.org cache
└── nuget                # Virtual
```

### 2. Use Virtual Repositories

✅ Configure clients to use virtual repositories  
✅ Easy to add/remove backing repos  
✅ Single URL for developers  

### 3. Retention Policies

Set cleanup rules:
- Keep last N versions
- Delete artifacts older than X days
- Keep artifacts with specific patterns

### 4. Access Control

- ✅ Use groups, not individual users
- ✅ Principle of least privilege
- ✅ Use access tokens, not passwords
- ✅ Regular security audits

### 5. Monitoring

- Track download statistics
- Monitor storage usage
- Set up alerts for failures
- Regular backups

## Troubleshooting

### Connection Issues

```bash
# Test connection
curl -u admin:password http://myartifactory.example.com:8081/api/system/ping

# Check logs
docker logs artifactory

# In Kubernetes
kubectl logs -n artifactory <pod-name>
```

### Permission Denied

```bash
# Verify credentials
jf rt ping

# Check permissions in UI
Administration → Security → Permissions
```

### Slow Performance

- Check disk space
- Review cache settings
- Increase resources (CPU/RAM)
- Enable CDN for large files

## Tài liệu tham khảo

- [JFrog Artifactory Documentation](https://www.jfrog.com/confluence/display/JFROG/JFrog+Artifactory)
- [JFrog CLI Documentation](https://www.jfrog.com/confluence/display/CLI/JFrog+CLI)
- [JFrog University](https://academy.jfrog.com/) - Free courses
- [REST API Documentation](https://www.jfrog.com/confluence/display/JFROG/Artifactory+REST+API)
- [JFrog Community](https://jfrog.com/community/)

## Quick Reference

```bash
# JFrog CLI
jf config add                          # Add server
jf rt ping                             # Test connection
jf rt upload "*.jar" maven-local/      # Upload
jf rt download maven-local/*.jar       # Download

# Docker
docker login <artifactory>:8081
docker tag myapp:1.0 <artifactory>:8081/docker-local/myapp:1.0
docker push <artifactory>:8081/docker-local/myapp:1.0

# NuGet
dotnet nuget add source <url> --name Artifactory
dotnet nuget push package.nupkg --source Artifactory

# npm
npm config set registry <artifactory-url>
npm publish --registry <artifactory-url>
```

## Summary

Artifactory giúp bạn:
- 📦 Central repository cho tất cả artifacts
- 🔒 Security và access control
- ⚡ Faster builds với caching
- 🔄 CI/CD integration
- 📊 Tracking và auditing
- 🌐 Multi-region distribution

**Universal Binary Repository** cho mọi package type! 🚀
