# JustGo Platform - System Context Diagram

> **Document Version:** 1.0
> **Last Updated:** 2026-04-26
> **Author:** System Architecture Team

## Overview

This document provides a comprehensive System Context Diagram for the JustGo Platform, illustrating the relationships between user roles, the JustGo Platform system, external services, and cloud infrastructure.

## System Context Diagram

```mermaid
flowchart TD
    %% Central System
    JustGo((("🏢 JustGo Platform")))

    %% User Roles (Circular around JustGo)
    SystemAdmin[👤 System Admin]
    NGBAdmin[👤 NGB Admin]
    NGBFinance[👤 NGB Finance]
    ClubAdmin[👤 Club Admin]
    Member[👤 Member]

    %% External Services
    PaymentGateway[💳 Payment Gateway]
    EmailProvider[📧 Email Provider]
    AzurePlatform[☁️ Azure Platform]
    ExternalWebhooks[🔗 External Webhooks]

    %% Payment Providers (connected to abstraction)
    Adyen[Adyen]
    Stripe[Stripe]

    %% Email Providers (connected to abstraction)
    SendGrid[SendGrid]
    SMTP[SMTP]

    %% User Relationships (two-way each)
    SystemAdmin <--->|API requests<br/>System configuration| JustGo
    NGBAdmin <--->|API requests<br/>Governance & policies| JustGo
    NGBFinance <--->|API requests<br/>Financial operations| JustGo
    ClubAdmin <--->|API requests<br/>Club management| JustGo
    Member <--->|API requests<br/>Bookings & payments| JustGo

    %% JustGo to External Services
    JustGo -->|Payment operations| PaymentGateway
    JustGo -->|Email sending| EmailProvider
    JustGo -->|Data storage<br/>Infrastructure| AzurePlatform
    JustGo -->|Event publishing| ExternalWebhooks

    %% Payment Gateway to Providers
    PaymentGateway -->|Payment processing| Adyen
    PaymentGateway -->|Payment processing| Stripe

    %% Email Provider to Providers
    EmailProvider -->|Email delivery| SendGrid
    EmailProvider -->|Email delivery| SMTP

    %% Styling
    classDef justGoStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:4px,color:#4a148c
    classDef userStyle fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    classDef externalStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef providerStyle fill:#fff8e1,stroke:#f57f17,stroke-width:1px

    class JustGo justGoStyle
    class SystemAdmin,NGBAdmin,NGBFinance,ClubAdmin,Member userStyle
    class PaymentGateway,EmailProvider,AzurePlatform,ExternalWebhooks externalStyle
    class Adyen,Stripe,SendGrid,SMTP providerStyle
```

## Architecture Description

### System Overview

The JustGo Platform is a central REST API system that serves as the core business logic hub, connecting users, external services, and cloud infrastructure.

### User Roles

The platform supports five distinct user types, each with bidirectional API communication with JustGo:

1. **System Admin**: Global system configuration and monitoring
2. **NGB Admin**: National governing body governance and policy management
3. **NGB Finance**: Financial operations and payment reconciliation
4. **Club Admin**: Club-level management and member administration
5. **Member**: End users for bookings, payments, and participation

### External Services

#### Payment Gateway (Abstraction)
- **Purpose**: Unified payment processing interface
- **Implementations**: Adyen, Stripe
- **Configuration**: Toggled via `SYSTEM.PAYMENT.EnableAdyenPayment`

#### Email Provider (Abstraction)
- **Purpose**: Unified email communication interface
- **Implementations**: SendGrid (API-based), SMTP (server-based)
- **Configuration**: Toggled via `SYSTEM.MAIL.SENDGRID`

#### Azure Platform
- **Purpose**: Cloud infrastructure and data storage
- **Services**: SQL Database, Blob Storage, Service Bus, Functions, Redis Cache

#### External Webhooks
- **Purpose**: Event publishing to external systems
- **Architecture**: Outbox pattern via Azure Service Bus and Functions

## Key Design Patterns

### 1. Abstraction Layer
Payment and email services are abstracted to enable provider switching without code changes.

### 2. Event-Driven Architecture
Webhook system uses outbox pattern for reliable event publishing to external systems.

### 3. Multi-tenancy
Supports multiple organizations (NGB, Clubs) with data isolation.

## System Relationships

### User ↔ JustGo (Bidirectional)
- **Inbound**: API requests, authentication, data submissions
- **Outbound**: API responses, notifications, data retrieval

### JustGo ↔ External Services
- **Payment Gateway**: Payment processing and reconciliation
- **Email Provider**: Transactional emails and notifications
- **Azure Platform**: Data storage, caching, and infrastructure
- **External Webhooks**: Business event publishing

## Related Documentation

- [[2.1-product-perspective|Product Perspective]]
- [[4.3-software-interfaces|Software Interfaces]]
- [[DDD-Modular-Monolith-Architecture|Architecture Overview]]

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-04-26 | System Architecture Team | Initial simplified context diagram |
| 1.1 | 2026-04-26 | System Architecture Team | Added payment and email provider abstractions |

---

> **Note**: This simplified context diagram follows C4 Model standards, showing JustGo as the central system with clear bidirectional relationships to users and external services.
