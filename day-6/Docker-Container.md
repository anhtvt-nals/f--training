# Docker & Container

## Tổng quan

**Docker** là một nền tảng mã nguồn mở để phát triển, vận chuyển và chạy ứng dụng trong các **containers**. Container cho phép đóng gói ứng dụng với tất cả dependencies vào một đơn vị chuẩn hóa để deploy.

### Container là gì?

Container là một đơn vị phần mềm chuẩn hóa bao gồm:
- Code của ứng dụng
- Runtime (Node.js, .NET, Python, etc.)
- System tools và libraries
- System settings

**Khác biệt chính**: Container chia sẻ OS kernel với host, trong khi VM chạy OS riêng.

### Docker vs Virtual Machine

```
┌─────────────────────────┐     ┌─────────────────────────┐
│      Container          │     │   Virtual Machine       │
├─────────────────────────┤     ├─────────────────────────┤
│  App A  │  App B  │App C│     │ App A │ App B │ App C   │
├─────────┼─────────┼─────┤     ├───────┼───────┼─────────┤
│  Deps   │  Deps   │Deps │     │ Deps  │ Deps  │ Deps    │
├─────────┴─────────┴─────┤     ├───────┴───────┴─────────┤
│   Docker Engine         │     │ Guest OS│Guest OS│Guest │
├─────────────────────────┤     ├─────────┴───────┴───────┤
│      Host OS            │     │    Hypervisor           │
├─────────────────────────┤     ├─────────────────────────┤
│    Infrastructure       │     │      Host OS            │
└─────────────────────────┘     ├─────────────────────────┤
                                │    Infrastructure       │
                                └─────────────────────────┘
```

| Đặc điểm | Container | Virtual Machine |
|----------|-----------|-----------------|
| **Khởi động** | Giây | Phút |
| **Kích thước** | MB | GB |
| **Performance** | Native | Overhead |
| **Isolation** | Process-level | Complete |
| **OS** | Shared kernel | Separate OS |

## Ưu điểm của Docker

✅ **Portable**: Chạy ở mọi nơi (local, cloud, data center)  
✅ **Lightweight**: Nhẹ hơn VM nhiều lần  
✅ **Fast**: Start/stop trong vài giây  
✅ **Consistent**: Môi trường giống nhau từ dev đến production  
✅ **Scalable**: Dễ dàng scale up/down  
✅ **Version Control**: Có thể version và rollback images  
✅ **Microservices**: Lý tưởng cho kiến trúc microservices  

## Kiến trúc Docker

### Docker Components

1. **Docker Client** (`docker` CLI)
   - Interface để tương tác với Docker
   - Gửi commands tới Docker Daemon

2. **Docker Daemon** (`dockerd`)
   - Service chạy background
   - Quản lý containers, images, networks, volumes

3. **Docker Registry** (Docker Hub, Azure Container Registry)
   - Nơi lưu trữ Docker images
   - Public hoặc private

4. **Docker Objects**
   - **Images**: Template read-only để tạo containers
   - **Containers**: Instance chạy từ image
   - **Networks**: Kết nối giữa containers
   - **Volumes**: Lưu trữ dữ liệu persistent

```
┌──────────────┐
│ Docker Client│ (CLI commands)
└──────┬───────┘
       │ REST API
┌──────▼──────────────────┐
│   Docker Daemon         │
│  ┌─────────────────┐    │
│  │ Container Mgmt  │    │
│  │ Image Mgmt      │    │
│  │ Network Mgmt    │    │
│  │ Volume Mgmt     │    │
│  └─────────────────┘    │
└─────────────────────────┘
       │
┌──────▼──────┐
│   Registry  │ (Docker Hub, ACR)
└─────────────┘
```

## Cài đặt Docker

### macOS

```bash
# Cách 1: Download Docker Desktop
# https://www.docker.com/products/docker-desktop

# Cách 2: Sử dụng Homebrew
brew install --cask docker

# Verify cài đặt
docker --version
docker run hello-world
```

### Windows

```powershell
# Cách 1: Download Docker Desktop
# https://www.docker.com/products/docker-desktop

# Cách 2: Sử dụng Chocolatey
choco install docker-desktop

# Verify
docker --version
docker run hello-world
```

### Linux (Ubuntu)

```bash
# Update package index
sudo apt-get update

# Install prerequisites
sudo apt-get install \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

# Add Docker's official GPG key
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

# Set up repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine
sudo apt-get update
sudo apt-get install docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Verify
sudo docker run hello-world

# (Optional) Add user to docker group
sudo usermod -aG docker $USER
```

## Docker Commands cơ bản

### Working with Images

```bash
# Pull image từ registry
docker pull nginx
docker pull nginx:1.21  # specific version

# List images
docker images
docker image ls

# Build image từ Dockerfile
docker build -t myapp:1.0 .
docker build -t myapp:1.0 -f Dockerfile.prod .

# Tag image
docker tag myapp:1.0 myregistry.azurecr.io/myapp:1.0

# Push image to registry
docker push myregistry.azurecr.io/myapp:1.0

# Remove image
docker rmi nginx
docker image rm nginx

# Remove unused images
docker image prune

# Inspect image
docker image inspect nginx
```

### Working with Containers

```bash
# Run container
docker run nginx
docker run -d nginx                    # detached mode (background)
docker run -d -p 8080:80 nginx         # port mapping
docker run -d -p 8080:80 --name my-nginx nginx  # với tên
docker run -d -e API_KEY=secret nginx  # environment variables
docker run -it ubuntu /bin/bash        # interactive mode

# List containers
docker ps          # running containers
docker ps -a       # all containers (including stopped)

# Stop container
docker stop <container-id>
docker stop my-nginx

# Start stopped container
docker start my-nginx

# Restart container
docker restart my-nginx

# Remove container
docker rm <container-id>
docker rm -f <container-id>  # force remove running container

# Remove all stopped containers
docker container prune

# View logs
docker logs my-nginx
docker logs -f my-nginx         # follow logs
docker logs --tail 100 my-nginx # last 100 lines

# Execute command trong container
docker exec my-nginx ls -la
docker exec -it my-nginx /bin/bash  # interactive shell

# Copy files
docker cp file.txt my-nginx:/app/
docker cp my-nginx:/app/file.txt ./

# View container stats
docker stats
docker stats my-nginx

# Inspect container
docker inspect my-nginx

# View processes trong container
docker top my-nginx
```

### System Commands

```bash
# View Docker info
docker info

# View disk usage
docker system df

# Clean up (remove unused data)
docker system prune       # containers, networks, images
docker system prune -a    # remove all unused images
docker system prune --volumes  # also remove volumes

# View version
docker version
```

## Dockerfile

**Dockerfile** là file text chứa instructions để build Docker image.

### Dockerfile cơ bản

```dockerfile
# Base image
FROM node:18-alpine

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Expose port
EXPOSE 3000

# Start command
CMD ["npm", "start"]
```

### Dockerfile Instructions

| Instruction | Mô tả | Ví dụ |
|------------|-------|-------|
| `FROM` | Base image | `FROM node:18` |
| `WORKDIR` | Set working directory | `WORKDIR /app` |
| `COPY` | Copy files from host to image | `COPY . .` |
| `ADD` | Copy + extract (tar, url) | `ADD file.tar.gz /app` |
| `RUN` | Execute command khi build | `RUN npm install` |
| `CMD` | Default command khi run | `CMD ["npm", "start"]` |
| `ENTRYPOINT` | Main command | `ENTRYPOINT ["dotnet", "App.dll"]` |
| `ENV` | Set environment variable | `ENV NODE_ENV=production` |
| `EXPOSE` | Document port | `EXPOSE 80` |
| `VOLUME` | Create mount point | `VOLUME /data` |
| `USER` | Set user | `USER appuser` |
| `ARG` | Build-time variable | `ARG VERSION=1.0` |
| `LABEL` | Add metadata | `LABEL version="1.0"` |

### Multi-stage Build (Best Practice)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyApp.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

**Lợi ích**: Image nhỏ hơn (chỉ chứa runtime, không có build tools)

### Dockerfile cho Node.js

```dockerfile
FROM node:18-alpine

WORKDIR /app

# Copy package files first (better caching)
COPY package*.json ./
RUN npm ci --only=production

# Copy source code
COPY . .

# Non-root user
USER node

EXPOSE 3000
CMD ["node", "server.js"]
```

### Dockerfile cho Python

```dockerfile
FROM python:3.11-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY . .

# Run as non-root
RUN useradd -m appuser
USER appuser

EXPOSE 8000
CMD ["python", "app.py"]
```

### .dockerignore

File `.dockerignore` chỉ định files/folders không copy vào image:

```
node_modules
npm-debug.log
.git
.gitignore
README.md
.env
.vscode
*.md
```

## Docker Compose

**Docker Compose** là tool để define và run multi-container applications.

### docker-compose.yml cơ bản

```yaml
version: '3.8'

services:
  web:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: secret
      POSTGRES_DB: myapp
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  postgres-data:
```

### Docker Compose Commands

```bash
# Start services
docker-compose up
docker-compose up -d              # detached mode
docker-compose up --build         # rebuild images

# Stop services
docker-compose down
docker-compose down -v            # also remove volumes

# View logs
docker-compose logs
docker-compose logs -f            # follow
docker-compose logs web           # specific service

# List services
docker-compose ps

# Execute command trong service
docker-compose exec web /bin/bash

# Scale service
docker-compose up -d --scale web=3

# Build images
docker-compose build
docker-compose build --no-cache
```

### Ví dụ: Full-stack Application

```yaml
version: '3.8'

services:
  # Frontend
  frontend:
    build: ./frontend
    ports:
      - "3000:3000"
    environment:
      - REACT_APP_API_URL=http://localhost:5000
    depends_on:
      - backend
  
  # Backend API
  backend:
    build: ./backend
    ports:
      - "5000:5000"
    environment:
      - DATABASE_URL=postgresql://user:password@db:5432/myapp
      - REDIS_URL=redis://cache:6379
    depends_on:
      - db
      - cache
  
  # Database
  db:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: user
      POSTGRES_PASSWORD: password
      POSTGRES_DB: myapp
    volumes:
      - db-data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
  
  # Cache
  cache:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  db-data:
```

## Docker Networking

### Network Drivers

1. **bridge** (default): Containers trên cùng host giao tiếp với nhau
2. **host**: Container dùng network của host
3. **none**: Disable networking
4. **overlay**: Multi-host networking (Swarm, K8s)

### Network Commands

```bash
# List networks
docker network ls

# Create network
docker network create my-network
docker network create --driver bridge my-bridge

# Inspect network
docker network inspect my-network

# Connect container to network
docker network connect my-network my-container

# Disconnect
docker network disconnect my-network my-container

# Remove network
docker network rm my-network

# Run container trên specific network
docker run -d --network my-network nginx
```

### Ví dụ: Container Communication

```bash
# Tạo network
docker network create app-network

# Run database
docker run -d \
  --name db \
  --network app-network \
  -e POSTGRES_PASSWORD=secret \
  postgres:15

# Run app (có thể connect tới db bằng hostname 'db')
docker run -d \
  --name app \
  --network app-network \
  -e DATABASE_URL=postgresql://postgres:secret@db:5432/myapp \
  myapp:1.0
```

## Docker Volumes

**Volumes** được dùng để persist data và share data giữa containers.

### Volume Types

1. **Named Volumes**: Được Docker quản lý
2. **Bind Mounts**: Mount directory từ host
3. **tmpfs**: Temporary filesystem trong memory (Linux only)

### Volume Commands

```bash
# Create volume
docker volume create my-data

# List volumes
docker volume ls

# Inspect volume
docker volume inspect my-data

# Remove volume
docker volume rm my-data

# Remove unused volumes
docker volume prune
```

### Sử dụng Volumes

```bash
# Named volume
docker run -d \
  -v my-data:/app/data \
  nginx

# Bind mount (absolute path)
docker run -d \
  -v /Users/me/code:/app \
  nginx

# Bind mount (relative path với $PWD)
docker run -d \
  -v $PWD:/app \
  nginx

# Read-only volume
docker run -d \
  -v my-data:/app/data:ro \
  nginx
```

### Trong Docker Compose

```yaml
services:
  db:
    image: postgres:15
    volumes:
      # Named volume
      - db-data:/var/lib/postgresql/data
      
      # Bind mount (config file)
      - ./postgres.conf:/etc/postgresql/postgresql.conf
      
      # Bind mount (init scripts)
      - ./init-scripts:/docker-entrypoint-initdb.d

volumes:
  db-data:  # declare named volume
```

## Best Practices

### 1. Image Optimization

```dockerfile
# ✅ GOOD: Multi-stage build
FROM node:18 AS build
WORKDIR /app
COPY . .
RUN npm install && npm run build

FROM node:18-alpine
WORKDIR /app
COPY --from=build /app/dist ./dist
CMD ["node", "dist/server.js"]

# ❌ BAD: Single stage with build tools
FROM node:18
WORKDIR /app
COPY . .
RUN npm install && npm run build
CMD ["node", "dist/server.js"]
```

### 2. Layer Caching

```dockerfile
# ✅ GOOD: Copy dependencies first
COPY package*.json ./
RUN npm install
COPY . .

# ❌ BAD: Copy everything first
COPY . .
RUN npm install
```

### 3. Security

```dockerfile
# ✅ Use specific versions
FROM node:18.16.0-alpine

# ✅ Run as non-root user
RUN addgroup -g 1001 -S nodejs
RUN adduser -S nodejs -u 1001
USER nodejs

# ✅ Scan for vulnerabilities
# docker scan myapp:1.0

# ✅ Use minimal base images
FROM alpine:3.18
FROM distroless/base
```

### 4. Image Size

```dockerfile
# ✅ Use alpine images
FROM node:18-alpine

# ✅ Remove unnecessary files
RUN apt-get update && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

# ✅ Multi-stage builds
# (Chỉ copy artifacts cần thiết)
```

### 5. Environment Configuration

```yaml
# ✅ Use .env file
# docker-compose.yml
services:
  app:
    env_file:
      - .env
```

```bash
# .env
DATABASE_URL=postgresql://localhost:5432/mydb
API_KEY=secret123
```

### 6. Health Checks

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s \
  CMD curl -f http://localhost/health || exit 1
```

```yaml
# docker-compose.yml
services:
  web:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Container Registries

### Docker Hub

```bash
# Login
docker login

# Push image
docker tag myapp:1.0 username/myapp:1.0
docker push username/myapp:1.0

# Pull image
docker pull username/myapp:1.0
```

### Azure Container Registry (ACR)

```bash
# Login
az acr login --name myregistry

# Tag and push
docker tag myapp:1.0 myregistry.azurecr.io/myapp:1.0
docker push myregistry.azurecr.io/myapp:1.0

# Pull
docker pull myregistry.azurecr.io/myapp:1.0
```

### AWS ECR

```bash
# Get login password
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  123456789012.dkr.ecr.us-east-1.amazonaws.com

# Tag and push
docker tag myapp:1.0 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapp:1.0
docker push 123456789012.dkr.ecr.us-east-1.amazonaws.com/myapp:1.0
```

## Troubleshooting

### Common Issues

```bash
# Container exits immediately
docker logs <container-id>
docker inspect <container-id>

# Port already in use
docker ps  # check running containers
lsof -i :8080  # check what's using the port

# Permission denied
sudo usermod -aG docker $USER  # add user to docker group
newgrp docker

# Out of disk space
docker system prune -a --volumes
docker image prune -a

# Network issues
docker network inspect bridge
docker exec <container> ping <other-container>
```

## Tools hỗ trợ

| Tool | Mục đích |
|------|----------|
| **Docker Desktop** | GUI cho Docker |
| **Portainer** | Web-based management UI |
| **Dive** | Analyze image layers |
| **Lazydocker** | Terminal UI cho Docker |
| **Hadolint** | Dockerfile linter |
| **Docker Bench** | Security audit |
| **Trivy** | Vulnerability scanner |
| **ctop** | Top-like interface for containers |

## Tài liệu tham khảo

- [Docker Official Documentation](https://docs.docker.com/)
- [Docker Hub](https://hub.docker.com/)
- [Docker Samples](https://github.com/docker/awesome-compose)
- [Dockerfile Best Practices](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)
- [Play with Docker](https://labs.play-with-docker.com/) - Thử nghiệm online miễn phí
- [Docker Cheat Sheet](https://docs.docker.com/get-started/docker_cheatsheet.pdf)

## Quick Reference

```bash
# Lifecycle
docker run --name web -d -p 8080:80 nginx   # Create & start
docker stop web                              # Stop
docker start web                             # Start
docker restart web                           # Restart
docker rm web                                # Remove

# Images
docker build -t myapp:1.0 .                  # Build
docker images                                # List
docker rmi myapp:1.0                         # Remove
docker pull nginx                            # Download
docker push myapp:1.0                        # Upload

# Logs & Debug
docker logs -f web                           # Follow logs
docker exec -it web /bin/bash                # Shell access
docker inspect web                           # Detailed info
docker stats                                 # Resource usage

# Cleanup
docker system prune                          # Clean unused
docker volume prune                          # Clean volumes
docker image prune -a                        # Clean images
```
