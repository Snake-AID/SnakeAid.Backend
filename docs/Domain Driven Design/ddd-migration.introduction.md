# Domain-Driven Design Migration - Introduction

## What is Domain-Driven Design?

Domain-Driven Design (DDD) là một phương pháp tiếp cận thiết kế phần mềm tập trung vào **business domain** và **business logic** thay vì chỉ tập trung vào technical implementation. DDD giúp xây dựng các hệ thống phức tạp bằng cách:

- **Domain-centric**: Đặt business logic làm trung tâm
- **Bounded Context**: Chia domain thành các context nhỏ, độc lập
- **Ubiquitous Language**: Sử dụng ngôn ngữ chung giữa business và technical team
- **Rich Domain Model**: Entities chứa behavior, không chỉ data

## Current Architecture vs DDD

### Current N-Tier Architecture:
```
┌─────────────────────┐
│    SnakeAid.API     │ ◄── Presentation Layer
├─────────────────────┤
│   SnakeAid.Service  │ ◄── Business Logic Layer
├─────────────────────┤
│    SnakeAid.Core    │ ◄── Domain Models (Anemic)
├─────────────────────┤
│ SnakeAid.Repository │ ◄── Data Access Layer
└─────────────────────┘
```

### Target DDD Architecture:
```
┌─────────────────────────────────────────────────────────────────┐
│                     SnakeAid.API (Gateway)                     │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐     │
│ │ Emergency Rescue│ │ Snake Catching  │ │ Expert          │     │
│ │ Context         │ │ Context         │ │ Consultation    │     │
│ │                 │ │                 │ │ Context         │     │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘     │
├─────────────────────────────────────────────────────────────────┤
│                     Shared Kernel                              │
│   • Location Tracking  • AI Recognition  • Media Management    │
└─────────────────────────────────────────────────────────────────┘
```

## Why Migrate to DDD?

### Current Problems:
1. **Anemic Domain Model**: Entities chỉ chứa properties, logic nằm rải rác trong Services
2. **Cross-cutting Concerns**: Business logic phụ thuộc vào infrastructure
3. **Tight Coupling**: Changes ở 1 module ảnh hưởng toàn hệ thống
4. **Complex Business Rules**: Emergency rescue, snake catching có nhiều rule phức tạp
5. **Team Scalability**: Khó để multiple teams làm việc parallel

### Benefits of DDD:
1. **Rich Domain Models**: Business logic tập trung trong domain entities
2. **Bounded Contexts**: Độc lập deployment và scaling
3. **Domain Events**: Loose coupling giữa các contexts
4. **Testability**: Domain logic tách biệt infrastructure
5. **Business Alignment**: Code reflect business processes

## SnakeAid Domain Analysis

### Core Domains (High Business Value):
1. **Emergency Rescue** - Cấp cứu khi bị rắn cắn
2. **Snake Catching** - Dịch vụ bắt rắn theo yêu cầu
3. **Expert Consultation** - Tư vấn chuyên gia

### Supporting Domains (Important but not differentiating):
4. **Snake Knowledge** - Thông tin về rắn, nọc độc, thuốc giải
5. **Payment & Wallet** - Thanh toán, ví điện tử
6. **Identity & Profile** - User management, profiles
7. **Communication** - Chat, notifications, video call

### Generic Domains (Commodity):
8. **Reputation & Feedback** - Rating system

## Business Flows Identified

### Emergency Flow:
```
Patient reports bite → AI identifies snake → Find nearby rescuer → 
Provide first aid guidance → Rescuer dispatched → Track in realtime → 
Refer to hospital → Complete mission
```

### Snake Catching Flow:
```
Customer requests catching → Upload photos → AI identification → 
Price calculation → Find available rescuer → Accept job → 
Track progress → Complete catching → Payment
```

### Expert Consultation Flow:
```
Book consultation → Select expert → Upload snake photos → 
AI pre-analysis → Video/chat session → Expert diagnosis → 
Follow-up recommendations → Payment
```

## Migration Strategy Overview

### Phase-based Approach:
- **Phase 1**: Foundation & Core Domain (Emergency Rescue)
- **Phase 2**: Snake Catching Context
- **Phase 3**: Expert Consultation Context  
- **Phase 4**: Supporting Domains
- **Phase 5**: Integration & Optimization

### Key Principles:
- **Strangler Fig Pattern**: Gradually replace old system
- **Domain Events**: Decouple contexts
- **Shared Kernel**: Common utilities (AI, Location, Media)
- **CQRS**: Separate read/write models where beneficial
- **Event Sourcing**: For audit trails (missions, consultations)

## Expected Outcomes

### Technical Benefits:
- ✅ Cleaner, more maintainable code
- ✅ Better separation of concerns
- ✅ Easier unit testing
- ✅ Independent deployment of contexts
- ✅ Better performance through focused queries

### Business Benefits:
- ✅ Faster feature development
- ✅ Better business-tech alignment
- ✅ Easier onboarding of new developers
- ✅ Reduced bug rates
- ✅ Better system reliability

## References

- **Domain-Driven Design**: Eric Evans
- **Implementing Domain-Driven Design**: Vaughn Vernon
- **Clean Architecture**: Robert C. Martin
- **.NET DDD Examples**: https://github.com/dotnet-architecture/eShopOnContainers

---

**Next**: [Migration Plan](ddd-migration.plan.md)