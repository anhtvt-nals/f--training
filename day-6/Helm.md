# Helm - The Package Manager for Kubernetes

## Tổng quan

**Helm** là package manager cho Kubernetes, giúp define, install, và upgrade các ứng dụng Kubernetes phức tạp một cách dễ dàng. Helm được gọi là "apt/yum/npm của Kubernetes".

### Vấn đề Helm giải quyết

Khi deploy ứng dụng lên Kubernetes, bạn thường có nhiều YAML files:
- Deployment
- Service
- ConfigMap
- Secret
- Ingress
- PersistentVolumeClaim
- ...

**Thách thức:**
- ❌ Quản lý nhiều YAML files
- ❌ Duplicate code giữa environments (dev, staging, prod)
- ❌ Khó version và rollback
- ❌ Khó share và reuse
- ❌ Khó parameterize values

**Helm giải quyết:**
- ✅ Package tất cả resources thành một Chart
- ✅ Parameterize bằng values
- ✅ Version control và rollback dễ dàng
- ✅ Share qua Helm repositories
- ✅ Reusable templates

## Kiến trúc Helm

### Helm 3 Architecture (Current)

```
┌─────────────┐
│Helm Client  │ (CLI)
└──────┬──────┘
       │
       │ REST/gRPC
       │
┌──────▼──────────────┐
│ Kubernetes API      │
│ Server              │
└──────┬──────────────┘
       │
┌──────▼──────────────┐
│ Kubernetes Cluster  │
│ - ConfigMaps        │
│ - Secrets           │
│ - Releases          │
└─────────────────────┘
```

**Note:** Helm 2 có Tiller (server component), nhưng Helm 3 đã loại bỏ Tiller để tăng security.

## Core Concepts

### 1. Chart

**Chart** là package của Helm, chứa tất cả resource definitions cần thiết để run ứng dụng trên Kubernetes.

```
mychart/
├── Chart.yaml          # Metadata về chart
├── values.yaml         # Default values
├── templates/          # Template files
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── ingress.yaml
│   └── _helpers.tpl
├── charts/             # Dependent charts
└── README.md
```

### 2. Release

**Release** là một instance của chart running trong Kubernetes cluster.

Ví dụ:
- Cài đặt `mysql` chart → tạo release `my-mysql`
- Cài đặt `mysql` chart lần nữa → tạo release `another-mysql`

### 3. Repository

**Repository** là nơi lưu trữ và share charts.

Popular repositories:
- [Artifact Hub](https://artifacthub.io/) - Public charts
- [Bitnami](https://charts.bitnami.com/) - Production-ready charts
- Azure Container Registry, Harbor - Private repositories

## Cài đặt Helm

### macOS

```bash
# Sử dụng Homebrew
brew install helm

# Verify
helm version
```

### Windows

```powershell
# Sử dụng Chocolatey
choco install kubernetes-helm

# Hoặc Scoop
scoop install helm

# Verify
helm version
```

### Linux

```bash
# Script install
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

# Hoặc download binary
curl -LO https://get.helm.sh/helm-v3.12.0-linux-amd64.tar.gz
tar -zxvf helm-v3.12.0-linux-amd64.tar.gz
sudo mv linux-amd64/helm /usr/local/bin/helm

# Verify
helm version
```

### Auto-completion

```bash
# Bash
helm completion bash > /etc/bash_completion.d/helm

# Zsh
helm completion zsh > "${fpath[1]}/_helm"

# Fish
helm completion fish > ~/.config/fish/completions/helm.fish
```

## Helm Commands cơ bản

### Repository Management

```bash
# Add repository
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo add stable https://charts.helm.sh/stable

# List repositories
helm repo list

# Update repositories
helm repo update

# Search charts trong repo
helm search repo mysql
helm search repo bitnami

# Search trên Artifact Hub
helm search hub wordpress

# Remove repository
helm repo remove bitnami
```

### Installing Charts

```bash
# Install chart
helm install my-release bitnami/mysql

# Install với custom values
helm install my-release bitnami/mysql --set auth.rootPassword=secret

# Install từ values file
helm install my-release bitnami/mysql -f values.yaml

# Install vào specific namespace
helm install my-release bitnami/mysql -n production --create-namespace

# Install với dry-run (test)
helm install my-release bitnami/mysql --dry-run --debug

# Install và wait cho resources ready
helm install my-release bitnami/mysql --wait --timeout 5m
```

### Managing Releases

```bash
# List releases
helm list
helm list -A  # all namespaces
helm list -n production

# Get release status
helm status my-release

# Get release values
helm get values my-release

# Get release manifest
helm get manifest my-release

# Get all release info
helm get all my-release
```

### Upgrading Releases

```bash
# Upgrade release
helm upgrade my-release bitnami/mysql

# Upgrade với new values
helm upgrade my-release bitnami/mysql --set auth.rootPassword=newsecret

# Upgrade từ values file
helm upgrade my-release bitnami/mysql -f values-prod.yaml

# Upgrade với reuse values
helm upgrade my-release bitnami/mysql --reuse-values

# Install hoặc upgrade (nếu chưa tồn tại)
helm upgrade --install my-release bitnami/mysql
```

### Rollback

```bash
# View history
helm history my-release

# Rollback về previous version
helm rollback my-release

# Rollback về specific revision
helm rollback my-release 3

# Rollback với wait
helm rollback my-release --wait
```

### Uninstalling Releases

```bash
# Uninstall release
helm uninstall my-release

# Uninstall và keep history
helm uninstall my-release --keep-history

# Uninstall từ specific namespace
helm uninstall my-release -n production
```

## Tạo Chart riêng

### Create Chart

```bash
# Tạo chart mới
helm create mychart

# Structure được tạo:
# mychart/
# ├── Chart.yaml
# ├── values.yaml
# ├── templates/
# │   ├── deployment.yaml
# │   ├── service.yaml
# │   ├── ingress.yaml
# │   └── _helpers.tpl
# └── charts/
```

### Chart.yaml

```yaml
apiVersion: v2
name: mychart
description: A Helm chart for my application
type: application
version: 1.0.0        # Chart version
appVersion: "1.0"     # Application version

maintainers:
- name: Your Name
  email: you@example.com

keywords:
- web
- application

home: https://github.com/yourorg/mychart
sources:
- https://github.com/yourorg/myapp

dependencies:
- name: postgresql
  version: "12.x.x"
  repository: https://charts.bitnami.com/bitnami
```

### values.yaml

```yaml
# Default values
replicaCount: 2

image:
  repository: myregistry.azurecr.io/myapp
  tag: "1.0.0"
  pullPolicy: IfNotPresent

service:
  type: ClusterIP
  port: 80

ingress:
  enabled: false
  className: nginx
  hosts:
  - host: myapp.example.com
    paths:
    - path: /
      pathType: Prefix

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi

autoscaling:
  enabled: false
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 80

env:
  - name: NODE_ENV
    value: production
```

### templates/deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "mychart.fullname" . }}
  labels:
    {{- include "mychart.labels" . | nindent 4 }}
spec:
  {{- if not .Values.autoscaling.enabled }}
  replicas: {{ .Values.replicaCount }}
  {{- end }}
  selector:
    matchLabels:
      {{- include "mychart.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "mychart.selectorLabels" . | nindent 8 }}
    spec:
      containers:
      - name: {{ .Chart.Name }}
        image: "{{ .Values.image.repository }}:{{ .Values.image.tag | default .Chart.AppVersion }}"
        imagePullPolicy: {{ .Values.image.pullPolicy }}
        ports:
        - name: http
          containerPort: 80
          protocol: TCP
        {{- if .Values.env }}
        env:
        {{- toYaml .Values.env | nindent 8 }}
        {{- end }}
        resources:
          {{- toYaml .Values.resources | nindent 10 }}
```

### templates/service.yaml

```yaml
apiVersion: v1
kind: Service
metadata:
  name: {{ include "mychart.fullname" . }}
  labels:
    {{- include "mychart.labels" . | nindent 4 }}
spec:
  type: {{ .Values.service.type }}
  ports:
  - port: {{ .Values.service.port }}
    targetPort: http
    protocol: TCP
    name: http
  selector:
    {{- include "mychart.selectorLabels" . | nindent 4 }}
```

### templates/_helpers.tpl

```yaml
{{/*
Expand the name of the chart.
*/}}
{{- define "mychart.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "mychart.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "mychart.labels" -}}
helm.sh/chart: {{ include "mychart.chart" . }}
{{ include "mychart.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "mychart.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mychart.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
```

## Template Functions

### Built-in Functions

```yaml
# String manipulation
{{ .Values.name | upper }}
{{ .Values.name | lower }}
{{ .Values.name | title }}
{{ .Values.name | quote }}
{{ .Values.name | default "default-value" }}

# Type conversion
{{ .Values.count | toString }}
{{ .Values.value | toJson }}
{{ .Values.data | toYaml }}

# Conditionals
{{- if .Values.enabled }}
enabled: true
{{- else }}
enabled: false
{{- end }}

# Loops
{{- range .Values.items }}
- {{ . }}
{{- end }}

# With (scope)
{{- with .Values.service }}
type: {{ .type }}
port: {{ .port }}
{{- end }}
```

### Sprig Functions

Helm includes [Sprig library](http://masterminds.github.io/sprig/):

```yaml
# Random
{{ randAlpha 10 }}
{{ randNumeric 6 }}

# Date
{{ now | date "2006-01-02" }}

# Encoding
{{ .Values.password | b64enc }}
{{ .Values.encoded | b64dec }}

# Lists
{{ list "a" "b" "c" | join "," }}

# Dictionaries
{{ dict "key1" "value1" "key2" "value2" }}
```

## Override Values

### Method 1: Command line (--set)

```bash
helm install my-release mychart \
  --set replicaCount=3 \
  --set image.tag=2.0.0 \
  --set ingress.enabled=true
```

### Method 2: Values file (-f)

**values-prod.yaml:**
```yaml
replicaCount: 5

image:
  tag: "2.0.0"

ingress:
  enabled: true
  hosts:
  - host: prod.example.com
```

```bash
helm install my-release mychart -f values-prod.yaml
```

### Method 3: Multiple values files

```bash
helm install my-release mychart \
  -f values-base.yaml \
  -f values-prod.yaml \
  -f values-secrets.yaml
```

**Priority:** values-secrets.yaml > values-prod.yaml > values-base.yaml > values.yaml

## Chart Dependencies

### Define Dependencies

**Chart.yaml:**
```yaml
dependencies:
- name: postgresql
  version: "12.x.x"
  repository: https://charts.bitnami.com/bitnami
  condition: postgresql.enabled

- name: redis
  version: "17.x.x"
  repository: https://charts.bitnami.com/bitnami
  condition: redis.enabled
```

### Download Dependencies

```bash
# Download dependencies
helm dependency update

# List dependencies
helm dependency list

# Build dependencies (package từ charts/ folder)
helm dependency build
```

### Override Dependency Values

**values.yaml:**
```yaml
postgresql:
  enabled: true
  auth:
    username: myuser
    password: mypassword
    database: mydb

redis:
  enabled: true
  auth:
    password: redispass
```

## Testing Charts

### Lint Chart

```bash
# Check for issues
helm lint mychart
helm lint mychart -f values-prod.yaml
```

### Dry Run

```bash
# Generate manifests without installing
helm install my-release mychart --dry-run --debug

# See what would be upgraded
helm upgrade my-release mychart --dry-run --debug
```

### Template Command

```bash
# Render templates locally
helm template my-release mychart

# Save to file
helm template my-release mychart > manifests.yaml

# With values
helm template my-release mychart -f values-prod.yaml
```

### Chart Testing

Tạo tests trong `templates/tests/`:

**templates/tests/test-connection.yaml:**
```yaml
apiVersion: v1
kind: Pod
metadata:
  name: "{{ include "mychart.fullname" . }}-test-connection"
  annotations:
    "helm.sh/hook": test
spec:
  containers:
  - name: wget
    image: busybox
    command: ['wget']
    args: ['{{ include "mychart.fullname" . }}:{{ .Values.service.port }}']
  restartPolicy: Never
```

Chạy tests:

```bash
helm test my-release
```

## Package và Publish Charts

### Package Chart

```bash
# Package chart
helm package mychart

# Output: mychart-1.0.0.tgz

# Package với specific version
helm package mychart --version 1.2.3

# Package dependencies
helm package mychart --dependency-update
```

### Create Chart Repository

```bash
# Create index file
helm repo index .

# Output: index.yaml

# Upload tgz files và index.yaml lên web server hoặc cloud storage
```

### Publish to Azure Container Registry

```bash
# Login to ACR
az acr login --name myregistry

# Save chart to ACR
helm push mychart-1.0.0.tgz oci://myregistry.azurecr.io/helm

# Install từ ACR
helm install my-release oci://myregistry.azurecr.io/helm/mychart --version 1.0.0
```

### Publish to Harbor

```bash
# Login
helm registry login harbor.example.com

# Push
helm push mychart-1.0.0.tgz oci://harbor.example.com/myproject

# Pull
helm pull oci://harbor.example.com/myproject/mychart --version 1.0.0
```

## Ví dụ thực tế: ASP.NET Core Application

### 1. Tạo Chart

```bash
helm create aspnetcore-app
```

### 2. Chart.yaml

```yaml
apiVersion: v2
name: aspnetcore-app
description: ASP.NET Core Web Application
type: application
version: 1.0.0
appVersion: "1.0"
```

### 3. values.yaml

```yaml
replicaCount: 2

image:
  repository: myregistry.azurecr.io/aspnetcore-app
  tag: "1.0.0"
  pullPolicy: IfNotPresent

service:
  type: LoadBalancer
  port: 80

ingress:
  enabled: true
  className: nginx
  hosts:
  - host: myapp.example.com
    paths:
    - path: /
      pathType: Prefix
  tls:
  - secretName: myapp-tls
    hosts:
    - myapp.example.com

env:
- name: ASPNETCORE_ENVIRONMENT
  value: Production
- name: ConnectionStrings__DefaultConnection
  valueFrom:
    secretKeyRef:
      name: db-connection
      key: connection-string

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 500m
    memory: 512Mi
```

### 4. Deploy

```bash
# Development
helm install myapp aspnetcore-app -f values-dev.yaml -n development

# Staging
helm install myapp aspnetcore-app -f values-staging.yaml -n staging

# Production
helm install myapp aspnetcore-app -f values-prod.yaml -n production
```

## Hooks

Helm hooks cho phép can thiệp vào release lifecycle.

### Hook Types

- `pre-install`: Trước khi install
- `post-install`: Sau khi install
- `pre-delete`: Trước khi delete
- `post-delete`: Sau khi delete
- `pre-upgrade`: Trước khi upgrade
- `post-upgrade`: Sau khi upgrade
- `pre-rollback`: Trước khi rollback
- `post-rollback`: Sau khi rollback
- `test`: Khi run `helm test`

### Example: Database Migration Hook

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: {{ include "mychart.fullname" . }}-migration
  annotations:
    "helm.sh/hook": pre-upgrade,pre-install
    "helm.sh/hook-weight": "-5"
    "helm.sh/hook-delete-policy": before-hook-creation
spec:
  template:
    spec:
      containers:
      - name: migration
        image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
        command: ["dotnet", "ef", "database", "update"]
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-connection
              key: connection-string
      restartPolicy: Never
```

## Best Practices

### 1. Values Organization

```yaml
# ✅ GOOD: Structured
image:
  repository: myapp
  tag: 1.0.0
  pullPolicy: IfNotPresent

# ❌ BAD: Flat
imageRepository: myapp
imageTag: 1.0.0
imagePullPolicy: IfNotPresent
```

### 2. Use helpers

```yaml
# ✅ GOOD: Reusable labels
{{- include "mychart.labels" . | nindent 4 }}

# ❌ BAD: Duplicate labels
app: myapp
version: 1.0.0
```

### 3. Document values

```yaml
# values.yaml
## Number of replicas
replicaCount: 2

## Image configuration
image:
  ## Docker registry/image name
  repository: myapp
  ## Image tag
  tag: "1.0.0"
```

### 4. Use conditions và default values

```yaml
{{- if .Values.ingress.enabled }}
# ingress config
{{- end }}

{{ .Values.image.tag | default .Chart.AppVersion }}
```

### 5. Version properly

- Chart version: Semantic versioning (1.0.0, 1.1.0, 2.0.0)
- AppVersion: Application version

### 6. Security

```yaml
# Không hardcode secrets trong values.yaml
# ✅ GOOD: Reference secret
env:
- name: API_KEY
  valueFrom:
    secretKeyRef:
      name: api-credentials
      key: api-key

# ❌ BAD: Hardcoded
env:
- name: API_KEY
  value: "secret123"
```

## Troubleshooting

### Debug Commands

```bash
# Get values computed
helm get values my-release

# Show all computed values
helm get values my-release --all

# Render templates locally
helm template my-release mychart --debug

# Dry run install
helm install my-release mychart --dry-run --debug

# Get manifest của installed release
helm get manifest my-release

# View history
helm history my-release
```

### Common Issues

```bash
# Error: release already exists
helm uninstall my-release
# hoặc
helm upgrade my-release mychart

# Template parsing error
helm lint mychart
helm template mychart --debug

# Values not applied
helm upgrade my-release mychart --reuse-values=false
```

## Tài liệu tham khảo

- [Helm Official Documentation](https://helm.sh/docs/)
- [Artifact Hub](https://artifacthub.io/) - Find charts
- [Helm Best Practices](https://helm.sh/docs/chart_best_practices/)
- [Helm Template Developer's Guide](https://helm.sh/docs/chart_template_guide/)
- [Sprig Function Documentation](http://masterminds.github.io/sprig/)
- [Helm GitHub Repository](https://github.com/helm/helm)

## Quick Reference

```bash
# Repositories
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update
helm search repo nginx

# Install
helm install myapp bitnami/nginx
helm install myapp mychart -f values.yaml
helm install myapp mychart --set key=value

# List & Status
helm list
helm status myapp
helm get values myapp

# Upgrade
helm upgrade myapp mychart
helm upgrade --install myapp mychart

# Rollback
helm history myapp
helm rollback myapp 2

# Uninstall
helm uninstall myapp

# Chart Development
helm create mychart
helm lint mychart
helm package mychart
helm template myapp mychart
```

## Summary

Helm giúp bạn:
- 📦 Package Kubernetes applications
- 🔄 Version và rollback dễ dàng
- 🎯 Parameterize configurations
- 🚀 Deploy nhanh và consistent
- 🤝 Share và reuse charts
- 🛠️ Manage complex applications

**Start now:** `helm create mychart` → customize → `helm install` 🚀
