# CI/CD on Azure DevOps

## Tổng quan

**Azure DevOps** là một bộ công cụ DevOps services từ Microsoft giúp teams plan, develop, deliver, và operate software. Azure DevOps cung cấp CI/CD pipelines, version control, work tracking, testing, và artifact management.

### Azure DevOps Services

| Service | Mô tả |
|---------|-------|
| **Azure Boards** | Work tracking với Kanban boards, backlogs, sprints |
| **Azure Repos** | Git repositories hoặc TFVC |
| **Azure Pipelines** | CI/CD pipelines (Build & Release) |
| **Azure Test Plans** | Manual và exploratory testing |
| **Azure Artifacts** | Package management (NuGet, npm, Maven, Python) |

**Focus của tài liệu này: Azure Pipelines** (CI/CD)

## CI/CD là gì?

### Continuous Integration (CI)

**CI** là practice tự động build và test code mỗi khi có commit.

```
Developer commit → Trigger build → Compile → Run tests → Report
```

**Benefits:**
- ✅ Phát hiện bugs sớm
- ✅ Code quality tốt hơn
- ✅ Faster development
- ✅ Less integration issues

### Continuous Delivery/Deployment (CD)

**Continuous Delivery:** Code luôn sẵn sàng để deploy  
**Continuous Deployment:** Tự động deploy lên production

```
Build success → Deploy to Dev → Deploy to Staging → Deploy to Production
```

**Benefits:**
- ✅ Faster time to market
- ✅ Reliable releases
- ✅ Reduced manual errors
- ✅ Easy rollback

## Azure Pipelines Architecture

```
┌─────────────────────────────────────────────┐
│           Trigger                           │
│  (Push, PR, Schedule, Manual)               │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│         Pipeline Definition                 │
│         (azure-pipelines.yml)               │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│            Agent Pool                       │
│  (Microsoft-hosted / Self-hosted)           │
└───────────────┬─────────────────────────────┘
                │
    ┌───────────┼──────────┬─────────────┐
    │           │          │             │
┌───▼────┐  ┌──▼────┐  ┌──▼─────┐  ┌───▼────┐
│ Stage  │  │Stage  │  │ Stage  │  │ Stage  │
│  Dev   │  │ Test  │  │Staging │  │  Prod  │
└───┬────┘  └──┬────┘  └──┬─────┘  └───┬────┘
    │          │           │            │
┌───▼────┐  ┌──▼────┐  ┌──▼─────┐  ┌───▼────┐
│ Jobs   │  │ Jobs  │  │  Jobs  │  │  Jobs  │
└───┬────┘  └──┬────┘  └──┬─────┘  └───┬────┘
    │          │           │            │
┌───▼────┐  ┌──▼────┐  ┌──▼─────┐  ┌───▼────┐
│ Tasks  │  │ Tasks │  │ Tasks  │  │ Tasks  │
└────────┘  └───────┘  └────────┘  └────────┘
```

### Key Concepts

- **Pipeline**: Workflow definition (CI/CD process)
- **Stage**: Logical boundary (e.g., Build, Test, Deploy)
- **Job**: Collection of steps chạy trên một agent
- **Step/Task**: Individual action (build, test, deploy)
- **Agent**: Machine chạy jobs
- **Artifact**: Output của build (binaries, packages)

## Cài đặt Azure DevOps

### 1. Tạo Azure DevOps Organization

1. Truy cập: https://dev.azure.com
2. Sign in với Microsoft account
3. Create new organization
4. Chọn region (location)

### 2. Tạo Project

```
1. Click "New Project"
2. Project name: MyApp
3. Visibility: Private
4. Version control: Git
5. Work item process: Agile
6. Create
```

### 3. Setup Git Repository

#### Clone repository

```bash
# Clone từ Azure Repos
git clone https://dev.azure.com/yourorg/MyApp/_git/MyApp
cd MyApp

# Add files
echo "# My App" > README.md
git add .
git commit -m "Initial commit"
git push origin main
```

#### Connect existing repo

```bash
# Add Azure DevOps as remote
git remote add azure https://dev.azure.com/yourorg/MyApp/_git/MyApp
git push azure main
```

### 4. Cài đặt Azure CLI (Optional)

```bash
# macOS
brew install azure-cli

# Windows
choco install azure-cli

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login
az login

# Install DevOps extension
az extension add --name azure-devops
```

## Pipeline Configuration

### YAML vs Classic Editor

| Feature | YAML Pipeline | Classic Editor |
|---------|---------------|----------------|
| **Configuration** | Code (YAML file) | GUI (web interface) |
| **Version Control** | ✅ Yes | ❌ No |
| **Reusability** | ✅ Templates | Limited |
| **Multi-stage** | ✅ Yes | Via Release |
| **Recommended** | ✅ Yes | Legacy |

**Best Practice:** Sử dụng YAML pipelines

## YAML Pipeline cơ bản

### Simple Pipeline

**azure-pipelines.yml:**
```yaml
# Trigger: Khi nào pipeline chạy
trigger:
- main
- develop

# Agent: Máy chạy pipeline
pool:
  vmImage: 'ubuntu-latest'

# Steps: Các bước thực hiện
steps:
- script: echo Hello, world!
  displayName: 'Run a one-line script'

- script: |
    echo Add other commands here
    echo This is multi-line script
  displayName: 'Run a multi-line script'
```

### .NET Core Application Pipeline

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dotnetSdkVersion: '8.x'

steps:
# Install .NET SDK
- task: UseDotNet@2
  displayName: 'Install .NET SDK'
  inputs:
    version: $(dotnetSdkVersion)

# Restore dependencies
- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

# Build
- task: DotNetCoreCLI@2
  displayName: 'Build project'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration)'

# Run tests
- task: DotNetCoreCLI@2
  displayName: 'Run tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage"'

# Publish code coverage
- task: PublishCodeCoverageResults@1
  displayName: 'Publish code coverage'
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'

# Publish artifacts
- task: DotNetCoreCLI@2
  displayName: 'Publish application'
  inputs:
    command: 'publish'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'
    publishWebProjects: true

# Upload artifacts
- task: PublishBuildArtifacts@1
  displayName: 'Publish build artifacts'
  inputs:
    PathtoPublish: '$(Build.ArtifactStagingDirectory)'
    ArtifactName: 'drop'
```

### Multi-Stage Pipeline

```yaml
trigger:
- main

stages:
# Build Stage
- stage: Build
  displayName: 'Build Application'
  jobs:
  - job: BuildJob
    displayName: 'Build'
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Build'
      inputs:
        command: 'build'
        projects: '**/*.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Publish'
      inputs:
        command: 'publish'
        publishWebProjects: true
        arguments: '--output $(Build.ArtifactStagingDirectory)'
    
    - publish: '$(Build.ArtifactStagingDirectory)'
      artifact: drop

# Test Stage
- stage: Test
  displayName: 'Run Tests'
  dependsOn: Build
  jobs:
  - job: TestJob
    displayName: 'Test'
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Run Unit Tests'
      inputs:
        command: 'test'
        projects: '**/*Tests.csproj'

# Deploy to Dev
- stage: DeployDev
  displayName: 'Deploy to Dev'
  dependsOn: Test
  jobs:
  - deployment: DeployWeb
    displayName: 'Deploy Web App'
    pool:
      vmImage: 'ubuntu-latest'
    environment: 'development'
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: drop
          
          - task: AzureWebApp@1
            displayName: 'Deploy to Azure Web App'
            inputs:
              azureSubscription: 'Azure-Subscription'
              appName: 'myapp-dev'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'

# Deploy to Production
- stage: DeployProd
  displayName: 'Deploy to Production'
  dependsOn: DeployDev
  jobs:
  - deployment: DeployWeb
    displayName: 'Deploy Web App'
    pool:
      vmImage: 'ubuntu-latest'
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: drop
          
          - task: AzureWebApp@1
            displayName: 'Deploy to Azure Web App'
            inputs:
              azureSubscription: 'Azure-Subscription'
              appName: 'myapp-prod'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

## Docker Pipeline

### Build và Push Docker Image

```yaml
trigger:
- main

variables:
  dockerRegistryServiceConnection: 'ACR-Connection'
  imageRepository: 'myapp'
  containerRegistry: 'myregistry.azurecr.io'
  dockerfilePath: '$(Build.SourcesDirectory)/Dockerfile'
  tag: '$(Build.BuildId)'

pool:
  vmImage: 'ubuntu-latest'

stages:
- stage: Build
  displayName: 'Build and Push Docker Image'
  jobs:
  - job: Build
    displayName: 'Build'
    steps:
    # Build Docker image
    - task: Docker@2
      displayName: 'Build Docker image'
      inputs:
        command: 'build'
        repository: $(imageRepository)
        dockerfile: $(dockerfilePath)
        containerRegistry: $(dockerRegistryServiceConnection)
        tags: |
          $(tag)
          latest
    
    # Push to ACR
    - task: Docker@2
      displayName: 'Push image to ACR'
      inputs:
        command: 'push'
        repository: $(imageRepository)
        containerRegistry: $(dockerRegistryServiceConnection)
        tags: |
          $(tag)
          latest
    
    # Scan image for vulnerabilities
    - task: AquaSecurityScanner@4
      displayName: 'Scan image'
      inputs:
        image: '$(containerRegistry)/$(imageRepository):$(tag)'

- stage: Deploy
  displayName: 'Deploy to Kubernetes'
  dependsOn: Build
  jobs:
  - deployment: DeployToAKS
    displayName: 'Deploy to AKS'
    environment: 'production'
    pool:
      vmImage: 'ubuntu-latest'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: KubernetesManifest@0
            displayName: 'Deploy to Kubernetes'
            inputs:
              action: 'deploy'
              kubernetesServiceConnection: 'AKS-Connection'
              namespace: 'production'
              manifests: |
                $(Pipeline.Workspace)/manifests/deployment.yml
                $(Pipeline.Workspace)/manifests/service.yml
              containers: |
                $(containerRegistry)/$(imageRepository):$(tag)
```

## Variables & Secrets

### Pipeline Variables

```yaml
variables:
  # Simple variable
  buildConfiguration: 'Release'
  
  # Group variables (defined in Library)
  - group: 'dev-variables'
  - group: 'prod-variables'
  
  # Computed variables
  - name: imageTag
    value: '$(Build.BuildId)-$(Build.SourceBranchName)'
```

### Variable Groups

```
1. Pipelines → Library → Variable groups
2. Click "+ Variable group"
3. Name: dev-variables
4. Add variables:
   - apiUrl: https://api-dev.example.com
   - dbConnection: [secret]
5. Save
```

### Azure Key Vault Integration

```yaml
variables:
- group: 'my-variable-group'
- task: AzureKeyVault@2
  inputs:
    azureSubscription: 'Azure-Subscription'
    KeyVaultName: 'my-keyvault'
    SecretsFilter: '*'
    RunAsPreJob: true

steps:
- script: |
    echo "Using secret from Key Vault"
    echo $(SecretFromKeyVault)
  env:
    SECRET: $(SecretFromKeyVault)
```

## Service Connections

### Create Service Connection

#### Azure Resource Manager

```
1. Project Settings → Service connections
2. New service connection → Azure Resource Manager
3. Service principal (automatic)
4. Subscription: Select subscription
5. Resource group: (optional)
6. Service connection name: Azure-Subscription
7. Grant access permission to all pipelines
8. Save
```

#### Azure Container Registry

```
1. New service connection → Docker Registry
2. Registry type: Azure Container Registry
3. Subscription: Select subscription
4. Registry: Select ACR
5. Service connection name: ACR-Connection
6. Save
```

#### Kubernetes

```
1. New service connection → Kubernetes
2. Authentication method: Azure Subscription
3. Subscription: Select subscription
4. Cluster: Select AKS cluster
5. Namespace: production
6. Service connection name: AKS-Connection
7. Save
```

## Tasks phổ biến

### Build Tasks

```yaml
# .NET Build
- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration Release'

# npm install
- task: Npm@1
  inputs:
    command: 'install'
    workingDir: '$(Build.SourcesDirectory)'

# Maven build
- task: Maven@3
  inputs:
    mavenPomFile: 'pom.xml'
    goals: 'clean package'
```

### Test Tasks

```yaml
# .NET Test
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--collect:"XPlat Code Coverage"'

# Jest (JavaScript)
- task: Npm@1
  inputs:
    command: 'custom'
    customCommand: 'test -- --coverage'
```

### Deployment Tasks

```yaml
# Azure Web App
- task: AzureWebApp@1
  inputs:
    azureSubscription: 'Azure-Subscription'
    appName: 'myapp'
    package: '$(Pipeline.Workspace)/drop/**/*.zip'

# Kubernetes Deployment
- task: KubernetesManifest@0
  inputs:
    action: 'deploy'
    kubernetesServiceConnection: 'AKS-Connection'
    manifests: 'k8s/deployment.yml'

# SSH Deployment
- task: SSH@0
  inputs:
    sshEndpoint: 'SSH-Connection'
    runOptions: 'commands'
    commands: |
      cd /var/www/myapp
      git pull
      pm2 restart myapp
```

## Environments & Approvals

### Create Environment

```
1. Pipelines → Environments
2. New environment
3. Name: production
4. Resource: Kubernetes / Virtual machines / None
5. Create
```

### Add Approval

```
1. Open environment (production)
2. Click "..." → Approvals and checks
3. Add Approvals
4. Approvers: Select users/groups
5. Advanced:
   - Timeout: 30 days
   - Minimum number of approvers: 1
6. Create
```

### Use in Pipeline

```yaml
- stage: DeployProd
  jobs:
  - deployment: Deploy
    environment: production  # Requires approval
    strategy:
      runOnce:
        deploy:
          steps:
          - script: echo Deploying to production
```

## Triggers

### Push Trigger

```yaml
# Trigger trên specific branches
trigger:
  branches:
    include:
    - main
    - releases/*
    exclude:
    - experimental/*
  paths:
    include:
    - src/*
    exclude:
    - docs/*
```

### Pull Request Trigger

```yaml
pr:
  branches:
    include:
    - main
    - develop
  paths:
    include:
    - src/*
```

### Scheduled Trigger

```yaml
schedules:
- cron: "0 0 * * *"  # Midnight daily
  displayName: Daily midnight build
  branches:
    include:
    - main
  always: true  # Run even if no changes
```

### Pipeline Trigger

```yaml
resources:
  pipelines:
  - pipeline: upstream-pipeline
    source: 'MyProject-CI'
    trigger:
      branches:
      - main

trigger: none  # Disable CI trigger

steps:
- script: echo Triggered by upstream pipeline
```

## Templates

### Template file: build-template.yml

```yaml
parameters:
- name: buildConfiguration
  type: string
  default: 'Release'
- name: projectPath
  type: string

steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'
    projects: '${{ parameters.projectPath }}'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    projects: '${{ parameters.projectPath }}'
    arguments: '--configuration ${{ parameters.buildConfiguration }}'
```

### Use template: azure-pipelines.yml

```yaml
trigger:
- main

stages:
- stage: Build
  jobs:
  - job: BuildJob
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - template: templates/build-template.yml
      parameters:
        buildConfiguration: 'Release'
        projectPath: '**/*.csproj'
```

## Artifacts

### Publish Artifacts

```yaml
# Build artifacts
- publish: $(Build.ArtifactStagingDirectory)
  artifact: drop
  displayName: 'Publish build artifacts'

# Multiple artifacts
- publish: $(Build.SourcesDirectory)/config
  artifact: config
  
- publish: $(Build.SourcesDirectory)/scripts
  artifact: scripts
```

### Download Artifacts

```yaml
# Download từ current pipeline
- download: current
  artifact: drop

# Download từ specific pipeline
- download: upstream-pipeline
  artifact: release

# Use downloaded artifacts
- script: |
    ls $(Pipeline.Workspace)/drop
```

### Azure Artifacts (Package Feed)

```yaml
# Publish NuGet package
- task: DotNetCoreCLI@2
  inputs:
    command: 'pack'
    packagesToPack: '**/*.csproj'
    versioningScheme: 'byBuildNumber'

- task: NuGetCommand@2
  inputs:
    command: 'push'
    packagesToPush: '$(Build.ArtifactStagingDirectory)/**/*.nupkg'
    nuGetFeedType: 'internal'
    publishVstsFeed: 'MyFeed'
```

## Self-hosted Agents

### Setup Self-hosted Agent

#### Linux/macOS

```bash
# Download agent
mkdir myagent && cd myagent
wget https://vstsagentpackage.azureedge.net/agent/3.220.0/vsts-agent-linux-x64-3.220.0.tar.gz
tar zxvf vsts-agent-linux-x64-3.220.0.tar.gz

# Configure
./config.sh

# Server URL: https://dev.azure.com/yourorg
# PAT token: (create from Personal Access Tokens)
# Agent pool: Default
# Agent name: my-agent

# Run
./run.sh

# Run as service
sudo ./svc.sh install
sudo ./svc.sh start
```

#### Windows

```powershell
# Download và extract agent

# Configure
.\config.cmd

# Run
.\run.cmd

# Run as service
.\svc.cmd install
.\svc.cmd start
```

### Use Self-hosted Agent

```yaml
pool:
  name: 'Default'  # Self-hosted pool name
  demands:
  - agent.name -equals my-agent
```

## Best Practices

### 1. Pipeline Structure

```yaml
# ✅ GOOD: Organized stages
stages:
- stage: Build
- stage: Test
- stage: DeployDev
- stage: DeployProd

# ❌ BAD: Single stage với tất cả
steps:
- script: build
- script: test
- script: deploy
```

### 2. Use Templates

```yaml
# ✅ GOOD: Reusable templates
- template: templates/build.yml
- template: templates/test.yml

# ❌ BAD: Duplicate code
```

### 3. Security

- ✅ Store secrets trong Key Vault
- ✅ Use service connections
- ✅ Limit access to environments
- ✅ Enable branch policies

### 4. Variables

```yaml
# ✅ GOOD: Structured variables
variables:
  app:
    name: 'myapp'
    version: '1.0.0'

# Use variable groups cho môi trường
```

### 5. Artifacts

```yaml
# ✅ GOOD: Publish only necessary artifacts
- publish: $(Build.ArtifactStagingDirectory)/release
  artifact: release-package

# ❌ BAD: Publish everything
- publish: $(Build.SourcesDirectory)
  artifact: everything
```

## Monitoring & Troubleshooting

### View Pipeline Runs

```
Pipelines → Recent runs → Click run
- View stages, jobs, tasks
- Check logs
- Download artifacts
```

### Common Issues

#### Agent timeout

```yaml
# Increase timeout
jobs:
- job: Build
  timeoutInMinutes: 120  # Default: 60
```

#### Task failures

```yaml
# Continue on error
- script: echo This might fail
  continueOnError: true

# Conditional execution
- script: echo Only if succeeded
  condition: succeeded()
```

### Pipeline Analytics

```
Pipelines → Analytics
- Success rate
- Duration trends
- Failure analysis
```

## Tài liệu tham khảo

- [Azure Pipelines Documentation](https://docs.microsoft.com/azure/devops/pipelines/)
- [YAML Schema Reference](https://docs.microsoft.com/azure/devops/pipelines/yaml-schema)
- [Task Reference](https://docs.microsoft.com/azure/devops/pipelines/tasks/)
- [Azure DevOps Labs](https://azuredevopslabs.com/)
- [Microsoft Learn - Azure DevOps](https://learn.microsoft.com/training/azure-devops/)

## Quick Reference

```yaml
# Basic pipeline structure
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - task: DotNetCoreCLI@2
      inputs:
        command: 'build'

- stage: Deploy
  dependsOn: Build
  jobs:
  - deployment: DeployJob
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - script: echo Deploying
```

## Summary

Azure DevOps Pipelines giúp bạn:
- 🔄 Automate CI/CD workflows
- 🚀 Deploy to multiple environments
- 🔒 Secure with approvals và gates
- 📦 Manage artifacts và packages
- 📊 Monitor pipeline health
- 🤝 Integrate với Azure services
- 🛠️ Support multi-platform (.NET, Java, Node.js, Python, etc.)

**Powerful CI/CD platform cho modern DevOps!** 🚀
