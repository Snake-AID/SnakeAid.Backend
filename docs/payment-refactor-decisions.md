# Payment Refactor Decisions

Date: 2026-03-20

## Scope

This note records the payment-layer decisions approved during the PayOS refactor discussion.

## Approved Decisions

1. `SnakeCatchingPaymentOrchestrator` is treated as a domain payment service that sits beside other domain services such as request and mission services.
2. The class is renamed to `SnakeCatchingPaymentService` to match its real role in the N-tier service layer.
3. Payment handling for each business domain should use `<Domain>PaymentService` naming consistently instead of `Orchestrator`.
4. `ConsultationPaymentService` is considered a real domain payment service, not a generic payment service and not an orchestration-only layer.
5. `WalletTopupService` is treated as its own domain service for wallet funding, not as a shared background utility owned by other domains.
6. A generic `PaymentOrchestrator` should not remain as a primary abstraction when it cannot stay domain-neutral.
7. The codebase should avoid splitting payment flow into too many micro-services such as separate intent and settlement services because that is beyond the team's operating capacity right now.
8. A payment provider seam is still needed for future multi-provider support, but the seam must stay minimal.
9. The provider seam should use a single gateway abstraction, `IPaymentGateway`, with provider implementations such as `PayOsGateway` and future `VNPayGateway`.
10. `Gateway -> Client` double abstraction is not approved for the current stage because it creates unnecessary layering; one gateway adapter per provider is enough.

## Practical Refactor Rules

- Keep domain rules inside domain payment services.
- Keep provider SDK adaptation inside a gateway implementation.
- Avoid introducing generic payment services that also perform domain validation or domain state transitions.
- Prefer consistent naming over theoretical abstraction purity when the team must maintain the codebase day to day.

## Current Naming Target

- `SnakeCatchingPaymentService`
- `ConsultationPaymentService`
- `WalletTopupService`
- `IPaymentGateway`
- `PayOsGateway`

## Deferred Topics

- Generic payment request/response model cleanup across providers.
- Splitting shared escrow or settlement logic into smaller services, if duplication becomes a real maintenance problem later.
- Callback controller redesign and domain-specific payment controllers beyond the current refactor scope.
