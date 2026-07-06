# Microservices

## Tổng quan

Microservices (Kiến trúc vi dịch vụ) là một phong cách kiến trúc phần mềm trong đó ứng dụng được xây dựng như một tập hợp các dịch vụ nhỏ, độc lập, có thể triển khai riêng biệt.

### Đặc điểm chính

- **Độc lập**: Mỗi service chạy trong process riêng và độc lập
- **Phân tán**: Các service giao tiếp qua network (HTTP/REST, gRPC, Message Queue)
- **Tự chủ**: Mỗi service có thể được phát triển, triển khai và scale độc lập
- **Tập trung vào nghiệp vụ**: Mỗi service tập trung vào một business capability cụ thể
- **Decentralized**: Dữ liệu và quản lý được phân tán

### Ưu điểm

✅ **Scalability**: Có thể scale từng service riêng biệt theo nhu cầu  
✅ **Flexibility**: Có thể sử dụng công nghệ/ngôn ngữ khác nhau cho mỗi service  
✅ **Resilience**: Lỗi ở một service không làm sập toàn bộ hệ thống  
✅ **Deployment**: Deploy nhanh và độc lập từng service  
✅ **Team organization**: Các team nhỏ có thể làm việc độc lập trên từng service  

### Nhược điểm

❌ **Complexity**: Hệ thống phức tạp hơn với nhiều service  
❌ **Network latency**: Giao tiếp qua network chậm hơn in-process calls  
❌ **Data consistency**: Khó duy trì tính nhất quán dữ liệu giữa các service  
❌ **Testing**: Khó test integration giữa các service  
❌ **Monitoring**: Cần công cụ monitoring phức tạp hơn  

## So sánh với Monolithic Architecture

| Tiêu chí | Monolithic | Microservices |
|----------|------------|---------------|
| Deployment | Toàn bộ ứng dụng | Từng service riêng |
| Scalability | Scale toàn bộ app | Scale từng service |
| Technology | Một stack duy nhất | Đa dạng công nghệ |
| Development | Team lớn, một codebase | Nhiều team nhỏ, nhiều repo |
| Complexity | Đơn giản ban đầu | Phức tạp hơn |

## Các pattern phổ biến

### 1. API Gateway Pattern
- Điểm truy cập duy nhất cho client
- Xử lý routing, authentication, rate limiting
- VD: Kong, AWS API Gateway, Azure API Management

### 2. Database per Service
- Mỗi service có database riêng
- Đảm bảo loose coupling
- Thách thức: Data consistency

### 3. Event-Driven Architecture
- Services giao tiếp qua events
- Sử dụng Message Broker (RabbitMQ, Kafka, Azure Service Bus)
- Asynchronous communication

### 4. Circuit Breaker Pattern
- Ngăn chặn cascade failures
- Fallback mechanism khi service không available
- Library: Polly, Hystrix

### 5. Service Discovery
- Tự động phát hiện service instances
- VD: Consul, Eureka, Kubernetes Service Discovery

## Communication giữa các Services

### Synchronous (đồng bộ)
```
Service A --HTTP/REST--> Service B
Service A --gRPC--> Service B
```
- **REST API**: Phổ biến, dễ sử dụng
- **gRPC**: Hiệu suất cao, binary protocol

### Asynchronous (bất đồng bộ)
```
Service A --Message--> Queue --> Service B
```
- **Message Queue**: RabbitMQ, Apache Kafka
- **Service Bus**: Azure Service Bus, AWS SQS
- Ưu điểm: Loose coupling, resilience

## Cài đặt môi trường phát triển cơ bản

### 1. Thiết lập project structure

```bash
# Tạo thư mục cho microservices project
mkdir my-microservices-app
cd my-microservices-app

# Tạo các service
mkdir services
cd services
mkdir user-service
mkdir order-service
mkdir product-service
mkdir api-gateway
```

### 2. Ví dụ: User Service với .NET

```bash
# Tạo User Service
cd user-service
dotnet new webapi -n UserService
cd UserService

# Cài đặt packages cần thiết
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Swashbuckle.AspNetCore
```

### 3. Docker Compose để chạy nhiều services

Tạo file `docker-compose.yml`:

```yaml
version: '3.8'

services:
  user-service:
    build: ./services/user-service
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      
  order-service:
    build: ./services/order-service
    ports:
      - "5002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      
  product-service:
    build: ./services/product-service
    ports:
      - "5003:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
```

### 4. Chạy tất cả services

```bash
docker-compose up --build
```

## Best Practices

### 1. Design Principles

- ✅ **Single Responsibility**: Mỗi service làm một việc và làm tốt
- ✅ **Loose Coupling**: Giảm thiểu dependencies giữa các service
- ✅ **High Cohesion**: Logic liên quan nên ở cùng một service
- ✅ **Autonomous**: Service có thể hoạt động độc lập

### 2. Data Management

- Mỗi service quản lý data của riêng mình
- Tránh shared database giữa các service
- Sử dụng Event Sourcing hoặc CQRS cho complex scenarios
- Implement eventual consistency

### 3. Security

- Authentication/Authorization tại API Gateway
- Service-to-service authentication (JWT, mTLS)
- Encrypt sensitive data
- API rate limiting

### 4. Monitoring & Logging

- Centralized logging (ELK Stack, Azure Application Insights)
- Distributed tracing (Jaeger, Zipkin, OpenTelemetry)
- Health checks cho mỗi service
- Metrics collection (Prometheus, Grafana)

### 5. Testing

- Unit tests cho từng service
- Integration tests giữa các service
- Contract testing (Pact)
- End-to-end testing cho critical flows

## Tools và Technologies

| Category | Tools/Technologies |
|----------|-------------------|
| **Containerization** | Docker, Podman |
| **Orchestration** | Kubernetes, Docker Swarm |
| **API Gateway** | Kong, Ocelot, Azure API Management |
| **Service Mesh** | Istio, Linkerd, Consul |
| **Message Queue** | RabbitMQ, Kafka, Azure Service Bus |
| **Monitoring** | Prometheus, Grafana, Application Insights |
| **Logging** | ELK Stack, Fluentd, Loki |
| **Tracing** | Jaeger, Zipkin, OpenTelemetry |
| **CI/CD** | Jenkins, GitLab CI, Azure DevOps |

## Khi nào nên sử dụng Microservices?

### ✅ Nên sử dụng khi:

- Ứng dụng lớn, phức tạp với nhiều domains khác nhau
- Cần scale các phần khác nhau của ứng dụng độc lập
- Có nhiều teams phát triển song song
- Cần flexibility về technology stack
- Yêu cầu deployment frequency cao

### ❌ Không nên sử dụng khi:

- Ứng dụng nhỏ, đơn giản
- Team nhỏ (< 5 người)
- Chưa có kinh nghiệm với distributed systems
- Infrastructure chưa sẵn sàng (Docker, K8s)
- Budget và resources hạn chế

## Tài liệu tham khảo

- [Microservices.io](https://microservices.io/) - Patterns và best practices
- [Martin Fowler - Microservices](https://martinfowler.com/articles/microservices.html)
- [Microsoft - .NET Microservices Architecture](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/)
- [AWS - What are Microservices?](https://aws.amazon.com/microservices/)
- [Building Microservices by Sam Newman](https://www.oreilly.com/library/view/building-microservices/9781491950340/)

## Ví dụ thực tế

### E-Commerce Platform

```
┌─────────────────┐
│   API Gateway   │
└────────┬────────┘
         │
    ┌────┴────┬──────────┬──────────┐
    │         │          │          │
┌───▼──┐  ┌──▼───┐  ┌───▼────┐  ┌─▼─────┐
│ User │  │Order │  │Product │  │Payment│
└──────┘  └──────┘  └────────┘  └───────┘
```

- **User Service**: Quản lý users, authentication
- **Order Service**: Xử lý orders, order history
- **Product Service**: Catalog, inventory management
- **Payment Service**: Payment processing, refunds

Mỗi service có database riêng và giao tiếp qua API Gateway hoặc message queue.
