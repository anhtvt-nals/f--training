# Skaffold

## Tổng quan

**Skaffold** là một command-line tool từ Google giúp tự động hóa workflow phát triển ứng dụng trên Kubernetes. Nó xử lý việc build, push, và deploy application, giúp developers tập trung vào code thay vì lo về deployment process.

### Vấn đề Skaffold giải quyết

Khi phát triển ứng dụng Kubernetes, bạn thường phải:

1. ✏️ Viết code
2. 🔨 Build Docker image
3. 📤 Push image lên registry
4. 📝 Update Kubernetes manifests với image mới
5. 🚀 Deploy lên cluster
6. 📊 Xem logs và debug

**Skaffold tự động hóa toàn bộ quy trình này!**

### Đặc điểm chính

✅ **Continuous Development**: Tự động rebuild và redeploy khi code thay đổi  
✅ **Fast Local Development**: Optimize cho development loop nhanh  
✅ **Tool Agnostic**: Hỗ trợ Docker, Buildpacks, Bazel, Helm, Kustomize  
✅ **CI/CD Ready**: Sử dụng cùng config cho local và CI/CD  
✅ **Portable**: Config file đơn giản, dễ share trong team  
✅ **Pluggable**: Có thể customize từng stage  

## Workflow với Skaffold

```
┌──────────────────────────────────────────────────┐
│  Developer thay đổi code                         │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Skaffold phát hiện thay đổi (file watching)    │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Build: Tạo Docker image                         │
│  - Docker build                                  │
│  - Buildpacks                                    │
│  - Jib, Bazel, etc.                              │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Tag: Tag image với version/hash                 │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Push: Push image lên registry (optional)       │
│  - Docker Hub, ACR, ECR, GCR                     │
│  - Skip cho local development                    │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Deploy: Deploy lên Kubernetes                   │
│  - kubectl apply                                 │
│  - Helm                                          │
│  - Kustomize                                     │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Test: Run tests (optional)                      │
└───────────────┬──────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────┐
│  Logs: Stream logs từ pods                       │
└──────────────────────────────────────────────────┘
```

## Cài đặt Skaffold

### macOS

```bash
# Sử dụng Homebrew
brew install skaffold

# Hoặc download binary
curl -Lo skaffold https://storage.googleapis.com/skaffold/releases/latest/skaffold-darwin-amd64
chmod +x skaffold
sudo mv skaffold /usr/local/bin
```

### Windows

```powershell
# Sử dụng Chocolatey
choco install skaffold

# Hoặc download binary
# https://github.com/GoogleContainerTools/skaffold/releases
```

### Linux

```bash
# Download binary
curl -Lo skaffold https://storage.googleapis.com/skaffold/releases/latest/skaffold-linux-amd64
chmod +x skaffold
sudo mv skaffold /usr/local/bin

# Verify cài đặt
skaffold version
```

### Prerequisites

Trước khi sử dụng Skaffold, bạn cần:

- ✅ **Docker**: Để build images
- ✅ **kubectl**: Để interact với Kubernetes
- ✅ **Kubernetes cluster**: Minikube, Docker Desktop, hoặc cloud cluster

```bash
# Kiểm tra prerequisites
docker --version
kubectl version --client
kubectl cluster-info
```

## Configuration File: skaffold.yaml

File `skaffold.yaml` là trung tâm của Skaffold configuration.

### Config cơ bản

```yaml
apiVersion: skaffold/v4beta7
kind: Config
metadata:
  name: my-app

build:
  artifacts:
  - image: myapp
    docker:
      dockerfile: Dockerfile

deploy:
  kubectl:
    manifests:
    - k8s/deployment.yaml
    - k8s/service.yaml
```

### Config với Helm

```yaml
apiVersion: skaffold/v4beta7
kind: Config
metadata:
  name: my-app

build:
  artifacts:
  - image: myregistry.azurecr.io/myapp
    docker:
      dockerfile: Dockerfile

deploy:
  helm:
    releases:
    - name: myapp
      chartPath: helm/myapp
      valuesFiles:
      - helm/values.yaml
      setValues:
        image.repository: myregistry.azurecr.io/myapp
```

### Config với Kustomize

```yaml
apiVersion: skaffold/v4beta7
kind: Config
metadata:
  name: my-app

build:
  artifacts:
  - image: myapp

deploy:
  kustomize:
    paths:
    - k8s/overlays/dev
```

### Multi-artifact Build

```yaml
apiVersion: skaffold/v4beta7
kind: Config

build:
  artifacts:
  - image: frontend
    context: ./frontend
    docker:
      dockerfile: Dockerfile
  
  - image: backend
    context: ./backend
    docker:
      dockerfile: Dockerfile
  
  - image: worker
    context: ./worker
    docker:
      dockerfile: Dockerfile

deploy:
  kubectl:
    manifests:
    - k8s/*.yaml
```

## Skaffold Commands

### Development Mode

```bash
# Start development với auto-rebuild
skaffold dev

# Dev với cleanup khi exit
skaffold dev --cleanup=true

# Dev với specific profile
skaffold dev --profile=local

# Dev với port forwarding
skaffold dev --port-forward

# Dev với verbose logging
skaffold dev -v info
```

### Build Commands

```bash
# Build images
skaffold build

# Build và push lên registry
skaffold build --push

# Build với specific tag
skaffold build -t v1.2.3

# Build với custom file
skaffold build -f skaffold-prod.yaml
```

### Run (Build + Deploy một lần)

```bash
# Build và deploy
skaffold run

# Run với cleanup sau khi exit
skaffold run --tail

# Run với specific namespace
skaffold run -n production
```

### Deploy Commands

```bash
# Deploy pre-built images
skaffold deploy

# Deploy với specific tag
skaffold deploy -t v1.2.3

# Render manifests (không deploy)
skaffold render

# Render và save to file
skaffold render > manifests.yaml
```

### Debug Mode

```bash
# Start với debugging enabled
skaffold debug

# Debug specific container
skaffold debug --profile=debug
```

### Other Useful Commands

```bash
# Initialize skaffold.yaml
skaffold init

# Validate config
skaffold validate

# Delete deployed resources
skaffold delete

# View config
skaffold config list

# Diagnose issues
skaffold diagnose
```

## File Sync (Nhanh hơn Rebuild)

Skaffold có thể sync files trực tiếp vào container thay vì rebuild image.

```yaml
apiVersion: skaffold/v4beta7
kind: Config

build:
  artifacts:
  - image: myapp
    docker:
      dockerfile: Dockerfile
    sync:
      manual:
      - src: "src/**/*.js"
        dest: /app/src
      - src: "views/**/*.html"
        dest: /app/views

deploy:
  kubectl:
    manifests:
    - k8s/*.yaml
```

**Lợi ích**: Thay đổi code được sync ngay lập tức, không cần rebuild image.

## Profiles

Profiles cho phép customize config cho các môi trường khác nhau.

```yaml
apiVersion: skaffold/v4beta7
kind: Config

build:
  artifacts:
  - image: myapp

deploy:
  kubectl:
    manifests:
    - k8s/base/*.yaml

profiles:
# Local development
- name: local
  build:
    local:
      push: false
  deploy:
    kubectl:
      manifests:
      - k8s/base/*.yaml

# Staging environment
- name: staging
  build:
    artifacts:
    - image: myregistry.azurecr.io/myapp
  deploy:
    helm:
      releases:
      - name: myapp
        chartPath: helm/myapp
        namespace: staging

# Production
- name: production
  build:
    artifacts:
    - image: myregistry.azurecr.io/myapp
  deploy:
    helm:
      releases:
      - name: myapp
        chartPath: helm/myapp
        namespace: production
        setValues:
          replicaCount: 5
```

Sử dụng profile:

```bash
skaffold dev --profile=local
skaffold run --profile=staging
skaffold run --profile=production
```

## Port Forwarding

Skaffold có thể tự động forward ports từ services/pods.

```yaml
apiVersion: skaffold/v4beta7
kind: Config

build:
  artifacts:
  - image: myapp

deploy:
  kubectl:
    manifests:
    - k8s/*.yaml

portForward:
- resourceType: service
  resourceName: myapp-service
  port: 8080
  localPort: 3000  # optional, tự động pick nếu không chỉ định
```

Chạy với port forwarding:

```bash
skaffold dev --port-forward
```

## Build Strategies

### 1. Docker Build (Default)

```yaml
build:
  artifacts:
  - image: myapp
    docker:
      dockerfile: Dockerfile
      target: production  # multi-stage build target
      buildArgs:
        NODE_ENV: production
```

### 2. Cloud Native Buildpacks

```yaml
build:
  artifacts:
  - image: myapp
    buildpacks:
      builder: gcr.io/buildpacks/builder:v1
      env:
      - NODE_ENV=production
```

### 3. Jib (Java/Kotlin/Scala)

```yaml
build:
  artifacts:
  - image: myapp
    jib:
      project: pom.xml
      args:
      - --no-daemon
```

### 4. Custom Build Script

```yaml
build:
  artifacts:
  - image: myapp
    custom:
      buildCommand: ./build.sh
      dependencies:
        paths:
        - src/**
```

## Ví dụ thực tế

### Project Structure

```
my-app/
├── skaffold.yaml
├── Dockerfile
├── k8s/
│   ├── deployment.yaml
│   └── service.yaml
└── src/
    └── app.js
```

### skaffold.yaml

```yaml
apiVersion: skaffold/v4beta7
kind: Config
metadata:
  name: my-nodejs-app

build:
  artifacts:
  - image: my-nodejs-app
    docker:
      dockerfile: Dockerfile
    sync:
      manual:
      - src: "src/**/*.js"
        dest: /app/src

deploy:
  kubectl:
    manifests:
    - k8s/deployment.yaml
    - k8s/service.yaml

portForward:
- resourceType: service
  resourceName: my-nodejs-app
  port: 3000
  localPort: 3000
```

### Dockerfile

```dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

EXPOSE 3000
CMD ["npm", "start"]
```

### k8s/deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-nodejs-app
spec:
  replicas: 1
  selector:
    matchLabels:
      app: my-nodejs-app
  template:
    metadata:
      labels:
        app: my-nodejs-app
    spec:
      containers:
      - name: app
        image: my-nodejs-app
        ports:
        - containerPort: 3000
```

### k8s/service.yaml

```yaml
apiVersion: v1
kind: Service
metadata:
  name: my-nodejs-app
spec:
  selector:
    app: my-nodejs-app
  ports:
  - port: 3000
    targetPort: 3000
  type: LoadBalancer
```

### Development Workflow

```bash
# 1. Initialize (nếu chưa có skaffold.yaml)
skaffold init

# 2. Start development
skaffold dev --port-forward

# 3. Edit code trong src/app.js
# Skaffold sẽ tự động sync changes hoặc rebuild

# 4. View logs trong terminal
# Logs từ pods sẽ được stream tự động

# 5. Stop development (Ctrl+C)
# Resources sẽ được cleanup tự động
```

## Testing với Skaffold

```yaml
apiVersion: skaffold/v4beta7
kind: Config

build:
  artifacts:
  - image: myapp

test:
- image: myapp
  structureTests:
  - ./test/structure-test.yaml
  custom:
  - command: npm test
    
deploy:
  kubectl:
    manifests:
    - k8s/*.yaml
```

Chạy tests:

```bash
# Run tests trước khi deploy
skaffold dev --test

# Run chỉ build và test
skaffold test
```

## CI/CD Integration

Skaffold rất phù hợp cho CI/CD pipelines.

### GitHub Actions

```yaml
name: Deploy to Kubernetes

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Install Skaffold
      run: |
        curl -Lo skaffold https://storage.googleapis.com/skaffold/releases/latest/skaffold-linux-amd64
        chmod +x skaffold
        sudo mv skaffold /usr/local/bin
    
    - name: Configure kubectl
      uses: azure/k8s-set-context@v3
      with:
        kubeconfig: ${{ secrets.KUBE_CONFIG }}
    
    - name: Build and Deploy
      run: |
        skaffold run --profile=production
```

### Azure DevOps

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- script: |
    curl -Lo skaffold https://storage.googleapis.com/skaffold/releases/latest/skaffold-linux-amd64
    chmod +x skaffold
    sudo mv skaffold /usr/local/bin
  displayName: 'Install Skaffold'

- task: KubernetesManifest@0
  displayName: 'Set Kubernetes Context'
  inputs:
    action: 'setContext'
    kubernetesServiceConnection: 'k8s-connection'

- script: skaffold run --profile=production
  displayName: 'Build and Deploy with Skaffold'
```

## Best Practices

### 1. Use Profiles

Tạo profiles riêng cho local, dev, staging, production:

```yaml
profiles:
- name: local
  build:
    local:
      push: false
      
- name: production
  build:
    artifacts:
    - image: registry.example.com/myapp
```

### 2. File Sync cho Development

Sử dụng file sync cho faster feedback loop:

```yaml
sync:
  manual:
  - src: "**/*.js"
    dest: /app
```

### 3. Port Forwarding

Enable port forwarding trong dev mode:

```yaml
portForward:
- resourceType: service
  resourceName: myapp
  port: 8080
```

### 4. Optimize Build

- Sử dụng multi-stage Dockerfiles
- Cache dependencies
- Minimize layer size

### 5. Use Tags

Tag images properly cho CI/CD:

```bash
skaffold build -t $GIT_COMMIT_SHA
skaffold build -t v1.2.3
```

## Troubleshooting

### Debug Commands

```bash
# Verbose output
skaffold dev -v debug

# Diagnose configuration
skaffold diagnose

# Validate config
skaffold validate

# Check Skaffold version
skaffold version
```

### Common Issues

#### Image not found

```bash
# Ensure image is pushed
skaffold build --push

# Check registry authentication
docker login myregistry.azurecr.io
```

#### Port already in use

```bash
# Check port forwarding config
# Skaffold sẽ tự động pick port khác nếu không chỉ định localPort
```

#### Build timeout

```yaml
build:
  artifacts:
  - image: myapp
    docker:
      dockerfile: Dockerfile
      buildArgs:
        BUILDKIT_INLINE_CACHE: "1"
```

## Skaffold vs Alternatives

| Feature | Skaffold | Tilt | Garden |
|---------|----------|------|--------|
| **Learning Curve** | Thấp | Trung bình | Cao |
| **Configuration** | YAML | Tiltfile (Python) | YAML |
| **Build Support** | Docker, Buildpacks, Jib, Custom | Docker, Custom | Docker, Container |
| **Deploy Support** | kubectl, Helm, Kustomize | kubectl, Helm | kubectl, Helm, Terraform |
| **File Sync** | ✅ | ✅ | ✅ |
| **Debugging** | ✅ | ✅ | ✅ |
| **CI/CD** | ✅ Excellent | ✅ Good | ✅ Good |

## Tài liệu tham khảo

- [Skaffold Official Documentation](https://skaffold.dev/)
- [Skaffold GitHub Repository](https://github.com/GoogleContainerTools/skaffold)
- [Skaffold Examples](https://github.com/GoogleContainerTools/skaffold/tree/main/examples)
- [Skaffold API Reference](https://skaffold.dev/docs/references/yaml/)
- [Google Cloud - Skaffold Tutorial](https://cloud.google.com/kubernetes-engine/docs/how-to/skaffold)

## Quick Reference

```bash
# Initialize
skaffold init --force

# Development
skaffold dev                              # Start dev mode
skaffold dev --port-forward               # With port forwarding
skaffold dev --cleanup                    # Auto cleanup on exit

# Build
skaffold build                            # Build images
skaffold build --push                     # Build & push
skaffold build -t v1.0.0                  # With tag

# Deploy
skaffold run                              # Build & deploy once
skaffold deploy                           # Deploy only
skaffold delete                           # Delete resources

# Debug
skaffold debug                            # Debug mode
skaffold diagnose                         # Diagnose config

# Profiles
skaffold dev -p local                     # Use profile
skaffold run -p production                # Production deploy
```

## Khi nào nên dùng Skaffold?

### ✅ Nên sử dụng khi:

- Phát triển ứng dụng trên Kubernetes
- Muốn tự động hóa build-deploy workflow
- Cần fast development feedback loop
- Team sử dụng nhiều công cụ khác nhau (Docker, Helm, Kustomize)
- Muốn consistency giữa local dev và CI/CD

### ❌ Có thể không cần khi:

- Ứng dụng đơn giản, không dùng Kubernetes
- Đã có workflow hoàn chỉnh và hoạt động tốt
- Team chỉ làm ops, không develop

## Summary

Skaffold là công cụ mạnh mẽ để:
- 🔄 Tự động hóa development workflow
- ⚡ Tăng tốc độ phát triển trên Kubernetes
- 🛠️ Hỗ trợ nhiều build và deploy tools
- 🚀 Sẵn sàng cho CI/CD
- 📦 Đơn giản hóa complex deployments

**Bắt đầu ngay**: `skaffold init` → `skaffold dev` 🚀
