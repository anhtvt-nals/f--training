# Kubernetes (K8s)

## Tổng quan

Kubernetes (viết tắt K8s) là một nền tảng mã nguồn mở để tự động hóa việc triển khai (deployment), mở rộng (scaling), và quản lý các ứng dụng container hóa.

### Tại sao cần Kubernetes?

Khi bạn có nhiều container chạy trên nhiều máy chủ khác nhau, bạn cần:
- Tự động deploy containers
- Scale containers lên/xuống theo nhu cầu
- Load balancing giữa các containers
- Tự động restart containers khi chúng fail
- Zero-downtime deployments

**Kubernetes giải quyết tất cả những vấn đề trên!**

### Đặc điểm chính

- **Container Orchestration**: Quản lý và điều phối containers
- **Auto-scaling**: Tự động tăng/giảm số lượng containers
- **Self-healing**: Tự động restart, replace containers khi có lỗi
- **Load Balancing**: Phân phối traffic đều giữa các containers
- **Rolling Updates**: Deploy phiên bản mới không downtime
- **Secret & Configuration Management**: Quản lý sensitive data
- **Storage Orchestration**: Tự động mount storage systems

## Kiến trúc Kubernetes

### Control Plane (Master Node)

Bộ não của cluster, gồm các components:

#### 1. API Server
- Điểm truy cập chính của K8s cluster
- Xử lý REST operations
- Giao tiếp qua `kubectl`

#### 2. etcd
- Database lưu trữ trạng thái của cluster
- Key-value store phân tán
- Backup và restore cluster state

#### 3. Scheduler
- Quyết định Pod sẽ chạy trên Node nào
- Dựa trên resources, constraints, affinity rules

#### 4. Controller Manager
- Chạy các controllers để maintain desired state
- Node Controller, Replication Controller, Endpoints Controller, etc.

#### 5. Cloud Controller Manager (optional)
- Tích hợp với cloud providers (AWS, Azure, GCP)

### Worker Nodes

Các máy chủ chạy containers, gồm:

#### 1. Kubelet
- Agent chạy trên mỗi node
- Đảm bảo containers đang chạy trong Pods
- Giao tiếp với API Server

#### 2. Kube-proxy
- Network proxy chạy trên mỗi node
- Maintain network rules
- Load balancing cho Services

#### 3. Container Runtime
- Phần mềm chạy containers
- VD: Docker, containerd, CRI-O

```
┌─────────────────────────────────────┐
│         Control Plane               │
│  ┌────────┐  ┌──────┐  ┌─────────┐ │
│  │API     │  │Sched │  │Contr.   │ │
│  │Server  │  │-uler │  │Manager  │ │
│  └────────┘  └──────┘  └─────────┘ │
│       │                             │
│  ┌────▼────┐                        │
│  │  etcd   │                        │
│  └─────────┘                        │
└─────────────────────────────────────┘
            │
     ┌──────┴──────┬──────────┐
     │             │          │
┌────▼───┐   ┌────▼───┐  ┌──▼─────┐
│ Node 1 │   │ Node 2 │  │ Node 3 │
│        │   │        │  │        │
│ Kubelet│   │Kubelet │  │Kubelet │
│ Proxy  │   │Proxy   │  │Proxy   │
│ Pods   │   │Pods    │  │Pods    │
└────────┘   └────────┘  └────────┘
```

## Các khái niệm cơ bản

### 1. Pod

**Pod** là đơn vị nhỏ nhất trong K8s, chứa một hoặc nhiều containers.

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: nginx-pod
spec:
  containers:
  - name: nginx
    image: nginx:1.21
    ports:
    - containerPort: 80
```

### 2. Deployment

**Deployment** quản lý ReplicaSets và Pods, hỗ trợ rolling updates.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: nginx-deployment
spec:
  replicas: 3
  selector:
    matchLabels:
      app: nginx
  template:
    metadata:
      labels:
        app: nginx
    spec:
      containers:
      - name: nginx
        image: nginx:1.21
        ports:
        - containerPort: 80
```

### 3. Service

**Service** expose Pods ra bên ngoài hoặc giữa các Pods với nhau.

```yaml
apiVersion: v1
kind: Service
metadata:
  name: nginx-service
spec:
  type: LoadBalancer
  selector:
    app: nginx
  ports:
  - port: 80
    targetPort: 80
```

**Các loại Service:**
- **ClusterIP**: Chỉ truy cập được trong cluster (default)
- **NodePort**: Expose qua port của Node
- **LoadBalancer**: Expose qua cloud load balancer
- **ExternalName**: Map service tới DNS name

### 4. ConfigMap

Lưu trữ configuration data dạng key-value.

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  database_url: "mongodb://localhost:27017"
  api_key: "your-api-key"
```

### 5. Secret

Lưu trữ sensitive data (passwords, tokens).

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: db-secret
type: Opaque
data:
  username: YWRtaW4=  # base64 encoded
  password: cGFzc3dvcmQ=
```

### 6. Namespace

Phân chia cluster thành các virtual clusters.

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: development
```

### 7. Ingress

Quản lý external access tới services qua HTTP/HTTPS.

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: app-ingress
spec:
  rules:
  - host: myapp.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: app-service
            port:
              number: 80
```

### 8. PersistentVolume (PV) & PersistentVolumeClaim (PVC)

Quản lý storage.

```yaml
# PersistentVolumeClaim
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: app-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
```

## Cài đặt Kubernetes

### 1. Minikube (Local Development)

**Minikube** chạy K8s cluster trên máy local.

#### Cài đặt trên macOS

```bash
# Cài đặt minikube
brew install minikube

# Khởi động cluster
minikube start

# Kiểm tra status
minikube status

# Mở dashboard
minikube dashboard
```

#### Cài đặt trên Windows

```powershell
# Sử dụng Chocolatey
choco install minikube

# Hoặc download installer từ
# https://minikube.sigs.k8s.io/docs/start/

# Khởi động
minikube start
```

#### Cài đặt trên Linux

```bash
# Download và cài đặt
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube

# Khởi động
minikube start
```

### 2. kubectl (Command-line tool)

#### Cài đặt kubectl

**macOS:**
```bash
brew install kubectl
```

**Windows:**
```powershell
choco install kubernetes-cli
```

**Linux:**
```bash
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
```

#### Verify cài đặt

```bash
kubectl version --client
```

### 3. Docker Desktop với Kubernetes (macOS/Windows)

1. Mở Docker Desktop
2. Settings → Kubernetes
3. Check "Enable Kubernetes"
4. Click "Apply & Restart"

### 4. Kind (Kubernetes in Docker)

```bash
# Cài đặt
brew install kind  # macOS
# hoặc
curl -Lo ./kind https://kind.sigs.k8s.io/dl/v0.20.0/kind-linux-amd64
chmod +x ./kind
sudo mv ./kind /usr/local/bin/kind

# Tạo cluster
kind create cluster --name my-cluster

# Xóa cluster
kind delete cluster --name my-cluster
```

## Sử dụng cơ bản với kubectl

### Cluster Information

```bash
# Xem thông tin cluster
kubectl cluster-info

# Xem các nodes
kubectl get nodes

# Chi tiết về node
kubectl describe node <node-name>
```

### Working with Pods

```bash
# Tạo pod từ image
kubectl run nginx --image=nginx

# List tất cả pods
kubectl get pods

# Chi tiết về pod
kubectl describe pod nginx

# Xem logs của pod
kubectl logs nginx

# Exec vào pod
kubectl exec -it nginx -- /bin/bash

# Xóa pod
kubectl delete pod nginx
```

### Working with Deployments

```bash
# Tạo deployment
kubectl create deployment nginx --image=nginx

# List deployments
kubectl get deployments

# Scale deployment
kubectl scale deployment nginx --replicas=5

# Update image
kubectl set image deployment/nginx nginx=nginx:1.21

# Rollout status
kubectl rollout status deployment/nginx

# Rollback
kubectl rollout undo deployment/nginx

# Xóa deployment
kubectl delete deployment nginx
```

### Working with Services

```bash
# Expose deployment
kubectl expose deployment nginx --port=80 --type=LoadBalancer

# List services
kubectl get services

# Chi tiết service
kubectl describe service nginx

# Xóa service
kubectl delete service nginx
```

### Working with YAML files

```bash
# Apply configuration từ file
kubectl apply -f deployment.yaml

# Apply tất cả files trong folder
kubectl apply -f ./configs/

# Delete resources từ file
kubectl delete -f deployment.yaml

# View configuration
kubectl get deployment nginx -o yaml
```

### Namespace Operations

```bash
# List namespaces
kubectl get namespaces

# Tạo namespace
kubectl create namespace development

# Set default namespace
kubectl config set-context --current --namespace=development

# List pods trong namespace
kubectl get pods -n development

# List tất cả resources trong namespace
kubectl get all -n development
```

### ConfigMaps & Secrets

```bash
# Tạo configmap từ file
kubectl create configmap app-config --from-file=config.properties

# Tạo configmap từ literal
kubectl create configmap app-config --from-literal=api_url=http://api.example.com

# Tạo secret
kubectl create secret generic db-secret --from-literal=username=admin --from-literal=password=secret123

# View configmap
kubectl get configmap app-config -o yaml

# View secret (base64 encoded)
kubectl get secret db-secret -o yaml
```

### Debugging & Troubleshooting

```bash
# Xem logs của pod
kubectl logs <pod-name>

# Logs của container cụ thể trong pod
kubectl logs <pod-name> -c <container-name>

# Follow logs (real-time)
kubectl logs -f <pod-name>

# Previous container logs (nếu container restart)
kubectl logs <pod-name> --previous

# Describe pod (xem events)
kubectl describe pod <pod-name>

# Port forwarding
kubectl port-forward <pod-name> 8080:80

# Copy files from/to pod
kubectl cp <pod-name>:/path/to/file ./local-file
kubectl cp ./local-file <pod-name>:/path/to/file

# Top (resource usage)
kubectl top nodes
kubectl top pods
```

### Labels & Selectors

```bash
# Add label to pod
kubectl label pods nginx env=production

# Show labels
kubectl get pods --show-labels

# Filter by label
kubectl get pods -l env=production

# Remove label
kubectl label pods nginx env-
```

## Ví dụ thực tế: Deploy ứng dụng .NET

### 1. Tạo Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyApp.csproj", "./"]
RUN dotnet restore "MyApp.csproj"
COPY . .
RUN dotnet build "MyApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### 2. Build và push Docker image

```bash
# Build image
docker build -t myapp:1.0 .

# Tag cho registry
docker tag myapp:1.0 myregistry.azurecr.io/myapp:1.0

# Push to registry
docker push myregistry.azurecr.io/myapp:1.0
```

### 3. Tạo Kubernetes manifests

**deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: myapp
        image: myregistry.azurecr.io/myapp:1.0
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

**service.yaml:**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: myapp-service
spec:
  type: LoadBalancer
  selector:
    app: myapp
  ports:
  - port: 80
    targetPort: 80
```

### 4. Deploy to Kubernetes

```bash
# Apply configurations
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml

# Kiểm tra deployment
kubectl get deployments
kubectl get pods
kubectl get services

# Xem external IP
kubectl get service myapp-service
```

## Best Practices

### 1. Resource Management

```yaml
# Luôn set resource requests và limits
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### 2. Health Checks

```yaml
# Liveness và Readiness probes
livenessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /ready
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

### 3. Security

- Không chạy containers as root
- Sử dụng SecurityContext
- Scan images cho vulnerabilities
- Sử dụng NetworkPolicies
- Enable RBAC (Role-Based Access Control)

### 4. Configuration

- Sử dụng ConfigMaps cho configuration
- Sử dụng Secrets cho sensitive data
- Không hardcode values trong YAML

### 5. Monitoring & Logging

- Setup centralized logging
- Sử dụng Prometheus & Grafana
- Configure alerts
- Track metrics

## Managed Kubernetes Services

| Provider | Service Name | Features |
|----------|-------------|----------|
| **Azure** | AKS (Azure Kubernetes Service) | Tích hợp Azure services, free control plane |
| **AWS** | EKS (Elastic Kubernetes Service) | Tích hợp AWS services, managed control plane |
| **Google Cloud** | GKE (Google Kubernetes Engine) | Auto-upgrade, auto-repair, vertical pod autoscaling |
| **DigitalOcean** | DOKS | Đơn giản, pricing rõ ràng |

## Tools hỗ trợ

| Tool | Mục đích |
|------|----------|
| **Helm** | Package manager cho K8s |
| **Kustomize** | Configuration management |
| **Skaffold** | Local development workflow |
| **Lens** | Desktop GUI cho K8s |
| **K9s** | Terminal UI cho K8s |
| **Stern** | Multi-pod log tailing |
| **kubectx/kubens** | Switch contexts/namespaces nhanh |

## Cheat Sheet

```bash
# Quick reference
kubectl get all                          # List all resources
kubectl get pods -A                      # All pods in all namespaces
kubectl get events --sort-by=.metadata.creationTimestamp  # Events
kubectl api-resources                    # List all resource types
kubectl explain pod                      # Documentation for resource
kubectl config get-contexts              # List contexts
kubectl config use-context <context>     # Switch context
kubectl apply -f <file> --dry-run=client # Test without applying
```

## Tài liệu tham khảo

- [Kubernetes Official Documentation](https://kubernetes.io/docs/)
- [Kubernetes Tutorials](https://kubernetes.io/docs/tutorials/)
- [Play with Kubernetes](https://labs.play-with-k8s.com/) - Thử nghiệm online miễn phí
- [Kubernetes Patterns Book](https://k8spatterns.io/)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [CNCF - Cloud Native Computing Foundation](https://www.cncf.io/)

## Learning Path

1. ✅ Hiểu các khái niệm cơ bản (Pod, Deployment, Service)
2. ✅ Cài đặt và sử dụng kubectl với Minikube
3. ✅ Deploy ứng dụng đơn giản
4. ✅ Học về ConfigMaps và Secrets
5. ⬜ Tìm hiểu về Ingress và networking
6. ⬜ Học về StatefulSets và Persistent Volumes
7. ⬜ Practice với managed K8s (AKS, EKS, GKE)
8. ⬜ Tìm hiểu về Helm charts
9. ⬜ Security và RBAC
10. ⬜ Monitoring và logging strategies
