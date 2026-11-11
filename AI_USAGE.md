# AI usage

## AI Tools Used

Claude (Anthropic) was used as an AI development assistant during the initial implementation and refinement of this project.

AI assistance was used for:
- Initial project structure and implementation planning.
- Backend API development using C#, ASP.NET Core Minimal API, EF Core, and PostgreSQL.
- Frontend implementation using React and Vite.
- Docker and Docker Compose configuration.
- Automated test design and implementation.
- Technical documentation, including the README and assumptions. (Chatgpt Documentation)

## AI-Assisted Design and Implementation

### 1. Technology Stack Decision

The project requirements included a C#/.NET backend.EF Core (ORM) with the Npgsql PostgreSQL provider was selected for the C# backend.

### 2. Seat Allocation and Concurrency

AI assistance was used to explore approaches for preventing overbooking when multiple students apply simultaneously.

The implementation uses a PostgreSQL transaction with row-level locking on the class record, along with a partial unique index to prevent duplicate active applications by the same student.

### 3. Hold Expiration

The implementation includes a background service that processes expired holds. The application also excludes overdue holds from seat availability calculations, preventing an expired hold from blocking a seat while waiting for the background service.

### 4. Frontend Implementation

AI assistance was used to develop the React components, API communication, application state, polling behavior, and countdown display.

### 5. Automated Testing

AI assistance was used to design automated tests for concurrent applications, duplicate prevention, confirmation, cancellation, expiration, and validation.

The tests use a real PostgreSQL database because database-level concurrency behavior cannot be reliably validated using only an in-memory database.

## Human Review and Technical Decisions

AI-generated suggestions and code were reviewed and adjusted during implementation. The final implementation choices include:

- Using EF Core (ORM) to meet the C# backend requirement.
- Using PostgreSQL transactions and row-level locking to protect seat capacity.
- Using a partial unique index to enforce the active-application constraint.
- Using a background service for hold expiration.
- Using a separate PostgreSQL database for automated tests.
- Using Docker Compose to run the application and its supporting services.

## Verification Status

- Docker Compose image build: Successful.
- Frontend manual UI checks: Class creation, student ID validation, application countdown, confirmation, and cancellation checked manually.
- Automated backend tests: Successful

AI assistance was used as a development aid.