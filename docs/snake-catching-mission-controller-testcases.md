# SnakeCatchingMissionController Test Cases

## Scope
- Controller: `SnakeCatchingMissionController`
- Route base: `/api/snakecatching/missions`
- Endpoint count: 4
- Source verified from:
  - Controller: `SnakeAid.Api/Controllers/SnakeCatchingMissionController.cs`
  - Service: `SnakeAid.Service/Implements/SnakeCatchingMissionService.cs`
  - Request models:
    - `SnakeAid.Core/Requests/SnakeCatchingMission/UpdateMissionStatusRequest.cs`
    - `SnakeAid.Core/Requests/SnakeCatchingMission/AbortSnakeCatchingMissionRequest.cs`
  - Exception middleware: `SnakeAid.Core/Middlewares/ApiExceptionHandlerMiddleware.cs`

## Success Message Catalog
1. PATCH `/api/snakecatching/missions/{missionId}/start`
- `Mission started! En route to location.`

2. PATCH `/api/snakecatching/missions/{missionId}/arrived`
- `Mission marked as arrived!`

3. PATCH `/api/snakecatching/missions/{missionId}/complete`
- `Mission completed successfully! Request marked as completed.`

4. PATCH `/api/snakecatching/missions/{missionId}/abort`
- `Mission aborted successfully.`

## Error/Exception Mapping
## Global response envelope when exception happens
- Status code: from exception type
- `is_success = false`
- `message = exception.Message`
- `error.errorCode` mapping:
  - `BadRequestException` -> `BAD_REQUEST`
  - `NotFoundException` -> `NOT_FOUND`
  - `UnauthorizedException` -> `UNAUTHORIZED`
  - `ValidationException` -> `VALIDATION_ERROR`
  - Unknown `Exception` -> `INTERNAL_SERVER_ERROR`

## Endpoint-specific message candidates from service/base controller

### StartMission
- `User ID not found in token`
- `Mission not found or you don't have permission to access it.`
- `Cannot start mission. Current status: {status}. Mission must be in Preparing status.`

### MarkAsArrived
- `User ID not found in token`
- `Mission not found or you don't have permission to access it.`
- `Cannot mark as arrived. Current status: {status}. Mission must be EnRoute.`

### CompleteMission
- `User ID not found in token`
- `Mission not found or you don't have permission to access it.`
- `Cannot complete mission. Current status: {status}. Mission must be in Arrived status.`
- `Cannot complete mission. SnakeCatchingMission must have at least one evidence media.`
- `Catching environment with ID {id} not found.`

### AbortMission
- `Reason is required`
- `Reason cannot exceed 500 characters`
- `User ID not found in token`
- `Mission not found or you don't have permission to access it.`
- `Cannot abort mission. Current status: {status}. Mission can only be aborted from Preparing or EnRoute status.`

## Test Matrix by API

## A. PATCH /api/snakecatching/missions/{missionId}/start
- TC-A01: Valid mission in `Preparing` owned by current rescuer, optional notes omitted -> 200 + start success message + status becomes `EnRoute`.
- TC-A02: Valid mission in `Preparing` with `notes` provided -> 200 + notes persisted.
- TC-A03: `missionId` not found -> 404 `NOT_FOUND`.
- TC-A04: Mission exists but belongs to another rescuer -> 404 `NOT_FOUND` (permission by ownership filter).
- TC-A05: Mission status is not `Preparing` (e.g., `EnRoute`, `Arrived`, `MissionCompleted`, `MissionAborted`) -> 400 `BAD_REQUEST`.
- TC-A06: Missing/invalid auth token -> 401 from auth pipeline.
- TC-A07: Token valid but missing `NameIdentifier` claim -> 401 `UNAUTHORIZED` with `User ID not found in token`.
- TC-A08: Service unexpected exception (e.g., notification dependency error) -> 500 `INTERNAL_SERVER_ERROR`.

## B. PATCH /api/snakecatching/missions/{missionId}/arrived
- TC-B01: Valid mission in `EnRoute` owned by current rescuer -> 200 + arrived success message + status becomes `Arrived`.
- TC-B02: Valid mission in `EnRoute` with `notes` provided -> 200 + notes persisted.
- TC-B03: `missionId` not found -> 404 `NOT_FOUND`.
- TC-B04: Mission exists but belongs to another rescuer -> 404 `NOT_FOUND`.
- TC-B05: Mission status is not `EnRoute` -> 400 `BAD_REQUEST`.
- TC-B06: Missing/invalid auth token -> 401.
- TC-B07: Token valid but missing `NameIdentifier` claim -> 401 `UNAUTHORIZED`.
- TC-B08: Service unexpected exception -> 500.

## C. PATCH /api/snakecatching/missions/{missionId}/complete
- TC-C01: Valid mission in `Arrived`, has at least one evidence media, no `catchingEnvironmentId` -> 200 + complete success message + request status auto updated to `Finished`.
- TC-C02: Valid mission in `Arrived`, evidence exists, valid `catchingEnvironmentId` -> 200 + response includes `catchingEnvironment`.
- TC-C03: Mission has mission details quantity > 0 -> 200 + `actualCost = basePrice + (quantity * additionalSnakePrice) + envCost`.
- TC-C04: Mission has no mission details (or total quantity <= 0) -> 200 + `actualCost = 0`.
- TC-C05: `missionId` not found -> 404 `NOT_FOUND`.
- TC-C06: Mission exists but belongs to another rescuer -> 404 `NOT_FOUND`.
- TC-C07: Mission status is not `Arrived` -> 400 `BAD_REQUEST`.
- TC-C08: Missing evidence media -> 400 `BAD_REQUEST`.
- TC-C09: `catchingEnvironmentId` provided but not found -> 404 `NOT_FOUND`.
- TC-C10: Missing/invalid auth token -> 401.
- TC-C11: Token valid but missing `NameIdentifier` claim -> 401 `UNAUTHORIZED`.
- TC-C12: Service unexpected exception (pricing/settings/notification dependency) -> 500.

## D. PATCH /api/snakecatching/missions/{missionId}/abort
- TC-D01: Valid mission in `Preparing`, valid reason -> 200 + abort success message + mission status `MissionAborted`.
- TC-D02: Valid mission in `EnRoute`, valid reason -> 200 + abort success message.
- TC-D03: Abort also resets related request assignment (`status=Confirmed`, `assignedRescuerId=null`, `assignedAt=null`) -> 200 with state verified in DB.
- TC-D04: Reason `null` -> 400 model validation (`Reason is required`).
- TC-D05: Reason empty string -> 400 model validation (`Reason is required`).
- TC-D06: Reason length = 500 -> 200 (boundary pass).
- TC-D07: Reason length = 501 -> 400 model validation (`Reason cannot exceed 500 characters`).
- TC-D08: `missionId` not found -> 404 `NOT_FOUND`.
- TC-D09: Mission exists but belongs to another rescuer -> 404 `NOT_FOUND`.
- TC-D10: Mission status not `Preparing` or `EnRoute` -> 400 `BAD_REQUEST`.
- TC-D11: Missing/invalid auth token -> 401.
- TC-D12: Token valid but missing `NameIdentifier` claim -> 401 `UNAUTHORIZED`.
- TC-D13: Service unexpected exception -> 500.

## Recommended Execution Order
1. Smoke success per endpoint (A01, B01, C01, D01)
2. Auth checks (A06-A07, B06-B07, C10-C11, D11-D12)
3. Validation boundaries (D04-D07)
4. Business error matrix (A03-A05, B03-B05, C05-C09, D08-D10)
5. Exception fallback checks for 500 envelope per endpoint (A08, B08, C12, D13)

## Notes
- `SnakeCatchingMissionController` mainly delegates business logic to service layer and wraps success envelope.
- `UpdateMissionStatusRequest` has no data annotation validations; most business validation is in service.
- `AbortSnakeCatchingMissionRequest.Reason` is validated by data annotation (`Required`, `MaxLength(500)`).
- In `AbortMissionAsync`, service behavior currently resets request status to `Confirmed` (code-verified), even though some older docs may mention `Pending`.
