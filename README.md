# Task Management System

Cohort 10 — .NET Fullstack (.NET + ReactJS) assignment for **Salman Fayyaz**.

A web-based task management system where users log in and organise, assign, prioritise and track tasks. Admins can create users (including other admins) and see all tasks; regular users see their own.

## Tech Stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10) |
| Data access | Entity Framework Core 10 + SQL Server |
| Auth | ASP.NET Core Identity + JWT (roles: Admin, User) |
| Logging | Serilog (console + daily rolling log files) |
| Testing | xUnit + Moq |
| Frontend | React.js (Vite) |
| Code quality | SonarQube, ESLint, CodeRabbit |
| Version control | Git (GitHub) |

## Project Structure

```
cohort-10-dotnet-17226-salman/
├── Backend/
│   ├── TaskManager.slnx
│   ├── Api/
│   │   ├── Controllers/    # API endpoints
│   │   ├── Data/           # AppDBContext, migrations, seeder, database setup
│   │   ├── DTOs/           # Request/response models
│   │   ├── Enums/          # UserRole, ...
│   │   ├── Extensions/     # Service registration (JWT, ...)
│   │   ├── Middleware/     # Global exception handling
│   │   ├── Models/         # Database entities (AppUser, ...)
│   │   ├── Repositories/   # Data access
│   │   ├── Services/       # Business logic (AuthService, TokenService, ...)
│   │   └── Program.cs
│   └── Api.Tests/          # xUnit unit tests
└── frontend/               # React (Vite) app
```

## Features

- [x] Database setup with EF Core migrations (Identity tables)
- [x] Roles (Admin, User) and first Admin account seeded on startup
- [x] Login with JWT (`POST /api/auth/login`)
- [x] Current user endpoint (`GET /api/auth/me`)
- [x] Logging with Serilog
- [x] Unit tests for login (TokenService, AuthService, AuthController)
- [ ] User registration
- [ ] Admin creates users (Admin or User)
- [ ] Global exception handling
- [ ] Task CRUD with priority, category, due date and assignee
- [ ] Dashboard with Pending / In Progress / Completed counts
- [ ] React pages: login, signup, dashboard, task list, task detail, new task, profile
- [ ] SonarQube analysis
- [ ] Optional: SignalR real-time updates, export/import, search

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js (LTS)](https://nodejs.org/)
- SQL Server (Express or Developer edition) + SSMS
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- Git

### 1. Clone

```bash
git clone https://github.com/Salmanfayyaz123/cohort-10-dotnet-17226-salman.git
cd cohort-10-dotnet-17226-salman
```

### 2. Configure local secrets

Secrets are **not** stored in `appsettings.json`. Set them with .NET user-secrets (stored on your machine only):

```bash
cd Backend/Api
dotnet user-secrets init

# SQL Server connection (use your own server name and login)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=TasksManagerDB;Trusted_Connection=True;TrustServerCertificate=True"

# Password for the seeded Admin account
# (min 8 chars, with uppercase, lowercase, number and symbol)
dotnet user-secrets set "SeedAdmin:Password" "YOUR_ADMIN_PASSWORD"

# JWT signing key (random, at least 32 characters)
dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_SECRET_KEY"
```

Check them with `dotnet user-secrets list`.

The seeded Admin's name and email are in `appsettings.json` under `SeedAdmin` (default email: `admin@taskmanager.com`).

### 3. Run the backend

```bash
cd Backend/Api
dotnet run
```

On startup the API applies database migrations and creates the roles and the Admin account automatically. The URL is shown in the terminal (e.g. `http://localhost:5xxx`).

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173

### 5. Run the tests

```bash
cd Backend
dotnet test
```

## API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/health` | — | Health check |
| POST | `/api/auth/login` | — | Log in, returns a JWT |
| GET | `/api/auth/me` | Bearer token | Current user info |

## Branching Strategy

- `main` — stable, reviewed code
- `feature/<name>` — one branch per feature, merged via pull request (reviewed by CodeRabbit)

Commit messages follow: `feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`.