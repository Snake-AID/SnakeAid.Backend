# SnakeCatchingRequestController Test Cases

## Scope
- Controller: `SnakeCatchingRequestController`
- Route base: `/api/snakecatching/requests`
- Endpoint count: 7
- Source verified from:
  - Controller: `SnakeAid.Api/Controllers/SnakeCatchingRequestController.cs`
  - Service: `SnakeAid.Service/Implements/SnakeCatchingRequestService.cs`
  - Exception middleware: `SnakeAid.Core/Middlewares/ApiExceptionHandlerMiddleware.cs`

## Success Message Catalog
1. GET `/api/snakecatching/requests`
- `Retrieved {count} snake catching request(s) successfully.`

2. GET `/api/snakecatching/requests/active`
- `Active snake catching requests retrieved successfully.`

3. POST `/api/snakecatching/requests`
- `Snake catching request created successfully! Our team will review and assign a rescuer soon.`

4. PATCH `/api/snakecatching/requests/confirm/{requestId}`
- `Snake catching request confirmed successfully!`

5. POST `/api/snakecatching/requests/assign/{requestId}`
- `Snake catching request assigned successfully! Mission created.`

6. GET `/api/snakecatching/requests/{requestId}`
- `Snake catching request details retrieved successfully.`

7. PATCH `/api/snakecatching/requests/cancel/{requestId}`
- `Snake catching request cancelled successfully.`

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

### CreateSnakeCatchingRequest
- `User ID not found in token`
- `Request data cannot be null.`
- `Account not found.`
- `Member information could not be found for the current account.`
- `Snake species with ID {id} not found.`
- `Media with ID {id} not found.`
- `Failed to retrieve created request.`

### ConfirmSnakeCatchingRequest
- `Snake catching request not found.`
- `Request cannot be accepted. Current status: {status}`
- `Failed to retrieve updated request.`

### AssignSnakeCatchingRequest
- `Request data cannot be null.`
- `Rescuer ID is required.`
- `Account not found.`
- `Rescuer profile not found. Only rescuers can accept requests.`
- `Rescuer must be online to accept requests.`
- `Snake catching request not found.`
- `Request cannot be accepted. Current status: {status}`
- `This request has already been assigned to another rescuer.`
- `An active mission already exists for this request with status: {status}`
- `Failed to retrieve updated request.`

### GetSnakeCatchingRequestDetail
- `Snake catching request with ID {requestId} not found.`

### CancelSnakeCatchingRequest
- `User ID not found in token`
- `Cancellation reason is required.`
- `Snake catching request with ID {requestId} not found.`
- `You are not authorized to cancel this request.`
- `Active mission not found for this assigned request.`
- `Cannot cancel request. The rescuer is already on the way (En Route).`
- `Cannot cancel request. Mission status is {status}.`
- `Cannot cancel request with status {status}.`
- `Failed to retrieve updated request.`

## Test Matrix by API

## A. GET /api/snakecatching/requests
- TC-A01: No query filters -> 200, success envelope, list ordered newest first.
- TC-A02: Filter by `userId` valid existing -> 200, data only matching user.
- TC-A03: Filter by `handlingOperatorId` valid -> 200, filtered list.
- TC-A04: Filter by `assignedRescuerId` valid -> 200, filtered list.
- TC-A05: Filter by `status` valid enum -> 200, filtered list.
- TC-A06: Combined filters all valid -> 200, AND condition respected.
- TC-A07: Unauthorized token missing/invalid -> 401 from auth pipeline.
- TC-A08: Service throws unexpected exception -> 500, `INTERNAL_SERVER_ERROR`.

## B. GET /api/snakecatching/requests/active
- TC-B01: No status/since/until/page/pageSize -> 200, default status set in service (Pending/Confirmed/Assigned).
- TC-B02: `status=pending,confirmed` -> 200, parsed enum list forwarded.
- TC-B03: `status=unknown` -> 200, status parse -> null -> default status set.
- TC-B04: `status=pending,UNKNOWN,assigned` -> 200, unknown token ignored.
- TC-B05: with `since` only -> 200, records >= since.
- TC-B06: with `until` only -> 200, records <= until.
- TC-B07: with `since` + `until` -> 200, records in window.
- TC-B08: role Member tries access -> 403 (Authorize role guard).
- TC-B09: no token -> 401.
- TC-B10: service exception -> mapped status/error envelope.

## C. POST /api/snakecatching/requests
- TC-C01: Minimal valid payload -> 200 + create success message.
- TC-C02: Valid payload with snake species + media -> 200 + AI/media fields in data when available.
- TC-C03: `address=null` -> model validation failure.
- TC-C04: `address=""` -> model validation failure.
- TC-C05: `address.length=1000` -> pass.
- TC-C06: `address.length=1001` -> model validation failure.
- TC-C07: `lng=-180` -> pass.
- TC-C08: `lng=180` -> pass.
- TC-C09: `lng=-181` -> model validation failure.
- TC-C10: `lng=181` -> model validation failure.
- TC-C11: `lat=-90` -> pass.
- TC-C12: `lat=90` -> pass.
- TC-C13: `lat=-91` -> model validation failure.
- TC-C14: `lat=91` -> model validation failure.
- TC-C15: `additionalDetails.length=2000` -> pass.
- TC-C16: `additionalDetails.length=2001` -> model validation failure.
- TC-C17: `notes.length=1000` -> pass.
- TC-C18: `notes.length=1001` -> model validation failure.
- TC-C19: `snakeSpeciesList[].quantity=1` -> pass.
- TC-C20: `snakeSpeciesList[].quantity=100` -> pass.
- TC-C21: `snakeSpeciesList[].quantity=0` -> model validation failure.
- TC-C22: `snakeSpeciesList[].quantity=101` -> model validation failure.
- TC-C23: snake species ID not found -> 400 `BAD_REQUEST`.
- TC-C24: media ID not found -> 400 `BAD_REQUEST`.
- TC-C25: user not in token -> 401 `UNAUTHORIZED`.
- TC-C26: account not found/member profile not found -> mapped 404/400.
- TC-C27: service unexpected exception -> 500.

## D. PATCH /api/snakecatching/requests/confirm/{requestId}
- TC-D01: request pending exists -> 200 + confirm success message.
- TC-D02: requestId not found -> 404 `NOT_FOUND`.
- TC-D03: request status not Pending -> 400 with current status message.
- TC-D04: token missing/invalid -> 401.
- TC-D05: service unexpected exception -> 500.

## E. POST /api/snakecatching/requests/assign/{requestId}
- TC-E01: valid request + valid rescuer online/available + request Confirmed -> 200 + assign success message.
- TC-E02: body null -> 400 `Request data cannot be null.`
- TC-E03: `rescuerId=Guid.Empty` -> 400 `Rescuer ID is required.`
- TC-E04: rescuer account not found -> 404.
- TC-E05: rescuer profile missing -> 400.
- TC-E06: rescuer offline while available -> 400.
- TC-E07: requestId not found -> 404.
- TC-E08: request not Confirmed -> 400 with current status.
- TC-E09: request already assigned -> 400.
- TC-E10: active mission existed -> 400.
- TC-E11: token missing/invalid -> 401.
- TC-E12: service unexpected exception -> 500.

## F. GET /api/snakecatching/requests/{requestId}
- TC-F01: existing request -> 200 + detail success message.
- TC-F02: requestId not found -> 404.
- TC-F03: token missing/invalid -> 401.
- TC-F04: service unexpected exception -> 500.

## G. PATCH /api/snakecatching/requests/cancel/{requestId}
- TC-G01: owner cancels request status Pending -> 200 + cancelled message.
- TC-G02: owner cancels status Assigned + mission Preparing -> 200 and mission cancelled.
- TC-G03: reason null/empty/whitespace -> 400 `Cancellation reason is required.`
- TC-G04: reason length > 500 -> model validation failure.
- TC-G05: requestId not found -> 404.
- TC-G06: user is not owner -> 400 unauthorized-cancel message.
- TC-G07: status Assigned but no active mission -> 400.
- TC-G08: status Assigned + mission EnRoute -> 400 cannot cancel.
- TC-G09: status Assigned + mission in other status -> 400 mission status message.
- TC-G10: request status not Pending/Assigned -> 400 cannot cancel by request status.
- TC-G11: token missing/invalid -> 401.
- TC-G12: service unexpected exception -> 500.

## Recommended Execution Order
1. Smoke success per endpoint (A01, B01, C01, D01, E01, F01, G01)
2. Auth/role checks (A07, B08, B09, C25, D04, E11, F03, G11)
3. Request validation boundaries (C03-C22, G03-G04)
4. Business error matrix from service (C23-C27, D02-D05, E02-E12, F02-F04, G05-G12)
5. Exception fallback checks for 500 envelope per endpoint

## Notes
- Controller itself mostly delegates to service and wraps success response; business errors are thrown in service and formatted by global exception middleware.
- `AssignSnakeCatchingRequestRequest` currently has no data annotation on `rescuerId`; validation for empty GUID is done in service.
