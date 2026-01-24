# Documentation Organization Guide

## Folder Structure

Tài liệu được tổ chức theo **Hạng mục / Công nghệ**, mỗi folder đại diện cho một feature hoặc technology stack.

```
docs/
├── ASP Identity/
│   ├── aspnet-identity.introduction.md
│   ├── aspnet-identity.plan.md
│   ├── aspnet-identity.prompt.md
│   ├── aspnet-identity.sourcecode.md
│   └── aspnet-identity.usageguilde.md
├── NuGet/
│   └── NuGet_Upgrade_Doc.md
└── README.md (this file)
```

---

## File Naming Convention

Mỗi hạng mục/công nghệ có **5 loại file** theo quy ước:

### 1. `*.introduction.md` - Giới thiệu

**Mục đích**: Giới thiệu chức năng hoặc công nghệ  
**Nguồn**: Thường lấy từ SRS (Software Requirements Specification)  
**Độ chi tiết**: High-level overview  
**Người đọc**: PM, Tech Lead, Developer mới vào dự án

**Nội dung bao gồm**:
- Tổng quan về chức năng/công nghệ
- Lý do sử dụng (Why?)
- Use cases chính
- Lợi ích và trade-offs
- Tham khảo tài liệu gốc

**Ví dụ**: `aspnet-identity.introduction.md`
```markdown
# ASP.NET Identity - Introduction

## What is it?
ASP.NET Core Identity is a membership system that adds login functionality...

## Why use it?
- Replace Auth0 to reduce external dependencies
- Full control over user data
- Support JWT tokens for mobile/web clients

## Use Cases
- User registration and login
- Role-based authorization
- External authentication (Google, Facebook)
```

---

### 2. `*.plan.md` - Kế hoạch Implementation

**Mục đích**: Kế hoạch để implement chức năng vào codebase hiện có  
**Thời điểm**: Trước khi bắt đầu coding  
**Độ chi tiết**: Medium-level design  
**Người đọc**: Developer, Tech Lead

**Nội dung bao gồm**:
- Phân tích hiện trạng codebase
- Các thay đổi cần thiết (files to modify/create)
- Dependencies cần thêm
- Migration plan (nếu có)
- Risks và mitigation
- Timeline ước tính

**Ví dụ**: `aspnet-identity.plan.md`
```markdown
# ASP.NET Identity - Implementation Plan

## Current State
- Using Auth0 for authentication
- JWT tokens issued by Auth0

## Proposed Changes
1. Remove Auth0 dependencies
2. Add Microsoft.AspNetCore.Identity.EntityFrameworkCore
3. Update DbContext to inherit IdentityDbContext
4. Create AuthController with register/login endpoints

## Files to Modify
- SnakeAidDbContext.cs
- DependencyInjection.cs
- Program.cs

## Files to Create
- Controllers/AuthController.cs
- Domains/Account.cs
```

---

### 3. `*.prompt.md` - Prompt cho Agent

**Mục đích**: Prompt để thực hiện thao tác implement  
**Thời điểm**: Ngay trước khi implement (sát với hiện trạng codebase)  
**Độ chi tiết**: Very detailed, actionable  
**Người đọc**: AI Agent (Antigravity, Copilot, etc.)

**Nội dung bao gồm**:
- Yêu cầu cụ thể từng bước
- Code snippets mẫu
- Configuration settings
- Testing requirements
- Expected output

**Đặc điểm**:
- Viết dưới dạng instructions/commands
- Bao gồm tất cả context cần thiết
- Có thể copy-paste trực tiếp cho agent

**Ví dụ**: `aspnet-identity.prompt.md`
```markdown
# Implementation Plan - ASP.NET Core Identity (Prompt)

## Sprint Goals
- Replace Auth0 with ASP.NET Core Identity
- Ensure register/login/refresh works with JWT tokens

## Implementation Steps

1) Add Identity to the project
- Add Microsoft.AspNetCore.Identity.EntityFrameworkCore package
- Update Account to inherit IdentityUser<Guid>

2) Configure DbContext
- Update SnakeAidDbContext to inherit IdentityDbContext<Account, IdentityRole<Guid>, Guid>
- Register AddIdentityCore<Account>() in DI

3) Add Auth Endpoints
- Create AuthController with register/login/refresh endpoints
- Issue JWT tokens using JwtSecurityTokenHandler
```

---

### 4. `*.sourcecode.md` - Trạng thái Codebase

**Mục đích**: Thể hiện trạng thái codebase sau khi implement  
**Thời điểm**: Sau khi implement xong  
**Độ chi tiết**: Function-level detail (gần nhất với code)  
**Người đọc**: AI Agent, Developer maintenance

**Nội dung bao gồm**:
- Toàn bộ functions/methods với signatures
- Flow chi tiết từng endpoint
- Request/Response models
- Database schema
- Configuration settings
- Code snippets đầy đủ

**Mục đích chính**: 
- ✅ **Làm context cho agent sử dụng sau này**
- ✅ **Không cần crawl lại codebase → tiết kiệm token**
- ✅ **Onboarding developer mới nhanh hơn**

**Ví dụ**: `aspnet-identity.sourcecode.md`
````markdown
# ASP.NET Identity - Source Code Documentation

## AuthController

### Register Endpoint
**Location**: AuthController.cs:46-80
**Route**: POST /api/auth/register

```csharp
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    // Step 1: Check email uniqueness
    var existingUser = await _userManager.FindByEmailAsync(request.Email);
    if (existingUser != null)
        return BadRequest(...);
    
    // Step 2: Create Account entity
    var user = new Account { ... };
    
    // Step 3: Create user with password
    var result = await _userManager.CreateAsync(user, request.Password);
    ...
}
```
**Request Model**:
- Email (required, EmailAddress)
- Password (required, MinLength 6)
````

---

### 5. `*.usageguilde.md` - Hướng dẫn Sử dụng

**Mục đích**: Hướng dẫn sử dụng API/chức năng sau khi implement  
**Thời điểm**: Sau khi implement và test xong  
**Độ chi tiết**: API documentation level  
**Người đọc**: **Frontend Developer**, Mobile Developer, QA

**Nội dung bao gồm**:
- API endpoints với examples
- Request/Response format
- Authentication flow
- Error handling
- Code examples (JavaScript/TypeScript/Dart)
- Postman collection (nếu có)

**Ví dụ**: `aspnet-identity.usageguilde.md`
```markdown
# ASP.NET Identity - Usage Guide for Frontend Developers

## Authentication Flow

### 1. Register New User

**Endpoint**: `POST /api/auth/register`

**Request**:
````javascript
const response = await fetch('https://api.snakeaid.com/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'SecurePass123',
    fullName: 'John Doe'
  })
});

const data = await response.json();
console.log(data.data.accessToken);
```

**Response**:
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "base64...",
    "accessTokenExpiresAt": "2026-01-24T18:00:00Z",
    "refreshTokenExpiresAt": "2026-02-23T17:00:00Z"
  }
}
```

### 2. Login
...
```

---

## Workflow Timeline

```mermaid
graph LR
    A[introduction.md] --> B[plan.md]
    B --> C[prompt.md]
    C --> D[Implementation]
    D --> E[sourcecode.md]
    E --> F[usageguilde.md]
    
    style A fill:#e1f5ff
    style B fill:#fff9c4
    style C fill:#f3e5f5
    style D fill:#c8e6c9
    style E fill:#ffccbc
    style F fill:#d1c4e9
````

| Phase | File | Status | Purpose |
|-------|------|--------|---------|
| **Planning** | `introduction.md` | Before coding | Understand requirements |
| **Design** | `plan.md` | Before coding | Design approach |
| **Execution** | `prompt.md` | Before coding | Agent instructions |
| **Coding** | *(actual code)* | During coding | Implementation |
| **Documentation** | `sourcecode.md` | After coding | Code reference |
| **Integration** | `usageguilde.md` | After coding | API documentation |

---

## Best Practices

### 1. Keep Files Updated
- ✅ Update `sourcecode.md` whenever code changes significantly
- ✅ Update `usageguilde.md` when API contract changes
- ❌ Don't update `prompt.md` after implementation (it's historical)

### 2. File Relationships
- `introduction.md` → `plan.md`: Requirements to design
- `plan.md` → `prompt.md`: Design to actionable steps
- `prompt.md` → `sourcecode.md`: Instructions to actual implementation
- `sourcecode.md` → `usageguilde.md`: Implementation to usage

### 3. Audience Awareness
- **Backend team**: All files
- **Frontend team**: `introduction.md` + `usageguilde.md`
- **AI Agents**: `prompt.md` (before) + `sourcecode.md` (after)
- **New developers**: `introduction.md` + `sourcecode.md`

### 4. Token Optimization
- `sourcecode.md` should be **detailed enough** to avoid crawling codebase
- Include:
  - ✅ Function signatures
  - ✅ Flow diagrams
  - ✅ Request/Response examples
  - ✅ Database schema
  - ✅ Configuration settings
- Avoid:
  - ❌ Copying entire files verbatim
  - ❌ Redundant explanations
  - ❌ Outdated information

---

## Example: ASP Identity Folder

```
docs/ASP Identity/
├── aspnet-identity.introduction.md      # What is ASP.NET Identity?
├── aspnet-identity.plan.md           # How to replace Auth0?
├── aspnet-identity.prompt.md         # Agent: implement these steps
├── aspnet-identity.sourcecode.md     # Code reference (700+ lines)
└── aspnet-identity.usageguilde.md    # Frontend: how to call APIs?
```

**Flow**:
1. PM writes `introduction.md` from SRS
2. Tech Lead writes `plan.md` after analyzing codebase
3. Developer writes `prompt.md` for AI agent
4. AI Agent implements code
5. Developer writes `sourcecode.md` documenting implementation
6. Developer writes `usageguilde.md` for frontend team

---

## Creating New Documentation

When adding a new feature/technology:

1. Create folder: `docs/[Feature Name]/`
2. Create 5 files with naming convention:
   - `[feature-name].introduction.md`
   - `[feature-name].plan.md`
   - `[feature-name].prompt.md`
   - `[feature-name].sourcecode.md`
   - `[feature-name].usageguilde.md`
3. Follow the content guidelines above
4. Update this README if needed

---

## Benefits

### For Developers
- 📚 Clear documentation structure
- 🔍 Easy to find information
- 🚀 Faster onboarding

### For AI Agents
- 🤖 `prompt.md`: Clear instructions to implement
- 📖 `sourcecode.md`: Complete context without crawling code
- 💰 **Saves thousands of tokens** per query

### For Frontend Team
- 📱 `usageguilde.md`: Ready-to-use API examples
- 🎯 No need to read backend code
- ⚡ Faster integration

---

**Last Updated**: 2026-01-24  
**Maintained By**: Backend Team
