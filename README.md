# Bookstore API

A .NET 10 REST API for managing and searching books, developed as a backend technical assignment.

The solution uses ASP.NET Core, Entity Framework Core, SQLite, Keycloak and Docker. It follows a layered architecture with separation of concerns, dependency injection, validation, OAuth2 authentication and automated tests.

## Technologies

- **Backend:** C#, .NET 10, ASP.NET Core Web API
- **Database:** SQLite, Entity Framework Core (Code First)
- **Authentication:** Keycloak, OAuth2, JWT Bearer
- **API documentation:** Swagger / OpenAPI
- **Containerization:** Docker and Docker Compose
- **Testing:** xUnit, Moq, EF Core SQLite in-memory
- **Test client:** .NET 10 Console Application

## Solution Structure

```text
Bookstore/
├── Bookstore.Api/             # REST endpoints, authentication, Swagger
├── Bookstore.Application/     # DTOs and application interfaces
├── Bookstore.Domain/          # Domain entities
├── Bookstore.Infrastructure/  # EF Core, migrations, services
├── Bookstore.Tests/           # Automated tests
├── Bookstore.TestClient/      # OAuth2 Client Credentials test client
├── keycloak/                  # Development realm configuration
├── tools/                     # Development authentication scripts
├── docker-compose.yml
├── Bookstore.slnx
└── README.md
```

## Features

- Create, read, update and delete books
- Search books by title and author
- Case-insensitive search
- Pagination
- Validation of request models
- SQLite database with EF Core migrations
- OAuth2 Client Credentials authentication for CRUD operations
- OAuth2 Implicit Flow authentication for book search
- Swagger UI with authentication support
- Console test client with automatic token acquisition
- Automated service tests using an isolated SQLite in-memory database
- Docker Compose environment for the API and Keycloak

## Prerequisites

- Docker Desktop
- Docker Compose (included with Docker Desktop)

For local development outside Docker:

- .NET 10 SDK
- Visual Studio 2026 or Visual Studio Code

## Quick Start — Docker

The recommended way to run the application is with Docker Compose.

**1. Clone the repository**

```bash
git clone <repository-url>
cd Bookstore
```

Replace `<repository-url>` with the GitHub repository URL.

**2. Start the application**

```bash
docker compose up -d --build
```

Docker Compose starts the Bookstore API and Keycloak. On the first startup, Keycloak imports the development realm configuration, and the application initializes its SQLite database.

Wait until Keycloak has finished starting before authenticating.

**3. Check the containers**

```bash
docker compose ps
```

**4. Open Swagger**

http://localhost:8081/swagger

**5. Keycloak Administration Console**

http://localhost:8080/admin

Development administrator credentials:

- Username: `admin`
- Password: `admin`

These credentials are intended only for the local demonstration environment.

## API Endpoints

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| GET | `/api/books/{id}` | Get a book by ID | Client Credentials |
| POST | `/api/books` | Create a book | Client Credentials |
| PUT | `/api/books/{id}` | Update a book | Client Credentials |
| DELETE | `/api/books/{id}` | Delete a book | Client Credentials |
| POST | `/api/books/search` | Search books with pagination | Implicit Flow |

### Book Response

```json
{
  "bookId": 1,
  "title": "Clean Code",
  "subTitle": "A Handbook of Agile Software Craftsmanship",
  "author": {
    "authorId": 1,
    "name": "Robert C. Martin"
  }
}
```

### Create Book

**POST** `/api/books`

```json
{
  "title": "The Clean Coder",
  "subTitle": "A Code of Conduct for Professional Programmers",
  "authorId": 1
}
```

The `authorId` must reference an existing author.

### Update Book

**PUT** `/api/books/1`

```json
{
  "title": "Clean Code - Updated Edition",
  "subTitle": "A Handbook of Agile Software Craftsmanship",
  "authorId": 1
}
```

### Search Books

**POST** `/api/books/search`

```json
{
  "title": "Clean",
  "author": null,
  "page": 1,
  "pageSize": 10
}
```

Both title and author filters are optional. Pagination is applied on the server.

## Authentication and Authorization

Keycloak is used as the OAuth2 identity provider.

The API supports two OAuth2 flows as required by the assignment.

### 1. CRUD — OAuth2 Client Credentials Flow

CRUD endpoints use OAuth2 Client Credentials authentication.

This flow is intended for machine-to-machine communication, where an application obtains an access token using its client credentials without a human user signing in.

Development configuration:

- Client ID: `bookstore-crud-client`
- Client secret: `bookstore-demo-secret-change-for-production`
- Scope: `bookstore.manage`
- Audience: `bookstore-api`

#### Option A — Console Test Client (Recommended)

The solution includes `Bookstore.TestClient`, a .NET console application that requests an access token from Keycloak and automatically sends it to the protected API.

Make sure the Docker containers are running.

In PowerShell, from the solution root:

```powershell
$env:BOOKSTORE_CLIENT_SECRET = "bookstore-demo-secret-change-for-production"
dotnet run --project Bookstore.TestClient
```

Alternatively, run:

```powershell
dotnet run --project Bookstore.TestClient
```

The console client will prompt for the client secret if the environment variable is not configured.

The client authenticates using Client Credentials and executes an authenticated request to the Bookstore API.

The test client requires the .NET 10 SDK.

#### Option B — Swagger

A PowerShell utility is included to generate a development access token and copy it to the clipboard.

From the solution root, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\tools\get-crud-token.ps1"
```

Then:

1. Open http://localhost:8081/swagger
2. Click **Authorize**.
3. Select **CrudOAuth (HTTP Bearer)**.
4. Paste the generated access token.
5. Click **Authorize** and **Close**.
6. Execute a CRUD endpoint.

The access token is short-lived. Generate another token if it expires.

### 2. Search — OAuth2 Implicit Flow

The search endpoint uses OAuth2 Implicit Flow, as explicitly required by the assignment.

Swagger is configured to perform the browser-based OAuth2 authorization process using Keycloak.

Development client:

- Client ID: `bookstore-search-client`
- Scope: `bookstore.search`

Demo user:

- Username: `bookstore-tester`
- Password: `BookstoreDemo123!`

To authorize:

1. Open http://localhost:8081/swagger
2. Click **Authorize**.
3. Find **SearchOAuth (OAuth2, implicit)**.
4. Select the `bookstore.search` scope.
5. Click **Authorize**.
6. Sign in using the demo user credentials.
7. Return to Swagger and execute `/api/books/search`.

**Security note:** OAuth2 Implicit Flow is a legacy flow. For a modern production browser application, Authorization Code Flow with PKCE would be preferred. Implicit Flow is implemented here to meet the assignment's explicit requirements.

## Validation and Error Handling

The API validates incoming requests and uses appropriate HTTP response codes.

Examples include:

- `200 OK` — Successful retrieval, update or search
- `201 Created` — Book created successfully
- `204 No Content` — Book deleted successfully
- `400 Bad Request` — Invalid request data
- `401 Unauthorized` — Missing or invalid authentication token
- `403 Forbidden` — Authenticated client lacks the required permission
- `404 Not Found` — Requested resource does not exist

Book titles must contain 3–100 characters. Authors are referenced using an existing positive `authorId`.

## Database

The application uses SQLite with Entity Framework Core.

The database contains two main entities:

**Author**
- `AuthorId`
- `Name`
- Collection of books

**Book**
- `BookId`
- `Title`
- `SubTitle`
- `AuthorId`
- Navigation property to Author

The relationship between Author and Book is one-to-many.

EF Core migrations are included in the Infrastructure project.

In Docker, the SQLite database is stored in a persistent named volume so application data survives container recreation.

## Automated Tests

The solution includes xUnit tests for the Book service using SQLite in-memory databases.

Covered scenarios include:

- Retrieving an existing or nonexistent book
- Creating and updating books
- Handling nonexistent authors
- Deleting books
- Searching by title and author
- Combined search filters
- Pagination
- Search with no matching results

Run the tests from the solution root:

```bash
dotnet test Bookstore.Tests/Bookstore.Tests.csproj
```

The test suite runs independently of the Docker environment.

## Running Locally with Visual Studio

The solution can also be opened in Visual Studio for development and debugging.

1. Open `Bookstore.slnx`.
2. Restore NuGet packages.
3. Ensure Docker Desktop is running.
4. Start the services using `docker compose up -d --build`.
5. Open Swagger or run the console test client.

The recommended setup is Docker Compose because it provides the API, identity provider and database configuration together.

## Docker Commands

Start or rebuild:

```bash
docker compose up -d --build
```

Check container status:

```bash
docker compose ps
```

View logs:

```bash
docker compose logs -f
```

Stop containers while preserving data:

```bash
docker compose down
```

**Warning:** Running `docker compose down -v` also deletes the named volumes, including local Bookstore and Keycloak database data.

## Security Considerations

- API endpoints are protected by JWT Bearer authentication.
- Keycloak is used as the OAuth2 identity provider.
- CRUD and Search operations have separate authorization permissions.
- JWT issuer, audience and signatures are validated.
- Real client secrets should be stored in a secure secret manager or environment variables.
- The included credentials are demonstration-only and must not be reused in production.
- The Docker configuration runs Keycloak in development mode and is intended for local testing, not production deployment.
- Production deployments should use HTTPS, securely managed secrets, an appropriate database and hardened identity-provider configuration.

## Assignment Requirements

This solution implements the requested Bookstore backend functionality:

- Author and Book entities
- Book CRUD REST API
- Search by book title and author
- Pagination
- OAuth2 Client Credentials Flow
- OAuth2 Implicit Flow
- Swagger-based testing
- Console test client
- Database persistence
- Docker support
- Layered architecture, validation and automated tests

## Notes

The repository includes a local development environment intended to minimize setup requirements for reviewing and testing the solution.

The included identity-provider credentials are for demonstration only and should be replaced in any non-development deployment.