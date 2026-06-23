Ecommerce API
<div align="center">
.NET
C#
PostgreSQL
Cloudinary
Docker
License

A production-ready, layered ASP.NET Core 8 Web API for an e-commerce platform

Features • Tech Stack • Getting Started • API Docs • Deployment

</div>
Table of Contents
Overview
Features
Tech Stack
Architecture
Project Structure
Prerequisites
Getting Started
Configuration
Database Setup
Running the Application
API Documentation
Authentication
Image Upload
Deployment
Testing
Troubleshooting
License
Overview
A modern, scalable e-commerce backend featuring product management with Cloudinary image uploads, JWT authentication, Paystack payment integration, background job processing, and comprehensive observability through Serilog and OpenTelemetry.

Features
<table> <tr> <td>
Core Commerce

Product CRUD with categories
Shopping cart management
Order lifecycle tracking
Discount system with snapshots
</td> <td>
Security

JWT authentication
Role-based authorization
Rate limiting
Secure password hashing
</td> </tr> <tr> <td>
Media and Files

Cloudinary image uploads
Multipart file handling
3 images per product (front, back, side)
Auto cleanup on replace
</td> <td>
Payments

Paystack integration
Webhook verification
Payment reconciliation
Transaction snapshots
</td> </tr> <tr> <td>
Performance

In-memory caching (Redis ready)
Background jobs (Hangfire)
Hosted services
Outbox pattern
</td> <td>
Observability

Structured logging (Serilog)
Distributed tracing (OpenTelemetry)
Health checks
API versioning
</td> </tr> </table>
Tech Stack
Category	Technology
Framework	.NET 8, C# 12, ASP.NET Core Web API
Database	PostgreSQL with Entity Framework Core
Authentication	JWT Bearer Tokens
File Storage	Cloudinary
Payments	Paystack
Background Jobs	Hangfire + IHostedService
Caching	In-Memory (Redis ready)
Logging	Serilog (Console + File sinks)
Tracing	OpenTelemetry
Validation	FluentValidation
Documentation	Swagger / OpenAPI
Email	SMTP
Architecture
The project follows a clean layered architecture with strict separation of concerns:

text

┌─────────────────────────────────────────────┐
│         Ecommerce.API (Web Layer)           │
│   Controllers · Middleware · Program.cs     │
└─────────────────────┬───────────────────────┘
                      │
┌─────────────────────▼───────────────────────┐
│    Ecommerce.Application (Use Cases)        │
│  Services · DTOs · Interfaces · Validators  │
└─────────────────────┬───────────────────────┘
                      │
┌─────────────────────▼───────────────────────┐
│        Ecommerce.Domain (Core)              │
│      Entities · Domain Logic · Enums        │
└─────────────────────▲───────────────────────┘
                      │
┌─────────────────────┴───────────────────────┐
│   Ecommerce.Infrastructure (Adapters)       │
│   EF Core · Repositories · External APIs    │
└─────────────────────────────────────────────┘
Dependency direction: API → Application → Domain ← Infrastructure

Project Structure
<details> <summary>Click to expand full structure</summary>
text

Ecommerce/
├── Ecommerce.API/                  # Web API entry point
│   ├── Controllers/                # API endpoints
│   ├── Middleware/                 # Custom middleware
│   ├── Program.cs                  # App bootstrap and DI
│   ├── appsettings.json
│   └── Dockerfile
│
├── Ecommerce.Application/          # Application services
│   ├── Common/                     # Shared utilities
│   ├── DTOs/                       # Request/response models
│   ├── Events/                     # Domain events
│   ├── Interfaces/                 # Service contracts
│   ├── Services/                   # Business logic
│   └── Validators/                 # FluentValidation rules
│
├── Ecommerce.Domain/               # Core domain
│   ├── Common/                     # Base entities
│   ├── Entities/                   # Domain entities
│   └── Enums/                      # Domain enums
│
├── Ecommerce.Infrastructure/       # External integrations
│   ├── Caching/                    # Cache implementations
│   ├── Cloudinary/                 # Image upload service
│   ├── Email/                      # SMTP service
│   ├── Identity/                   # Admin seeder
│   ├── Middleware/                 # Webhook middleware
│   ├── Migrations/                 # EF Core migrations
│   ├── Payments/                   # Paystack integration
│   ├── Persistence/                # DbContext
│   ├── Repositories/               # Data access
│   ├── Security/                   # JWT generator
│   └── DependencyInjection.cs      # DI registration
│
└── Ecommerce.sln
</details>
Prerequisites
Before getting started, ensure you have:

.NET 8 SDK
PostgreSQL 14+
Cloudinary account (free tier)
Paystack account (optional)
An IDE such as Visual Studio 2022, Rider, or VS Code
Getting Started
1. Clone the Repository
Bash

git clone https://github.com/yourusername/ecommerce.git
cd ecommerce
2. Restore Dependencies
Bash

dotnet restore
3. Build the Solution
Bash

dotnet build
Configuration
The application uses User Secrets for local development and environment variables for production.

Required Configuration Keys
<details> <summary><b>Click to view all configuration keys</b></summary><br>
Key	Description	Example
ConnectionStrings:DefaultConnection	PostgreSQL connection	Host=localhost;Database=EcommerceDb;Username=postgres;Password=...
Jwt:Key	JWT signing key (min 32 chars)	your-super-secret-key-min-32-chars
Jwt:Issuer	JWT issuer	EcommerceApi
Jwt:Audience	JWT audience	EcommerceClient
Cloudinary:CloudName	Cloudinary cloud name	your-cloud-name
Cloudinary:ApiKey	Cloudinary API key	123456789012345
Cloudinary:ApiSecret	Cloudinary API secret	your-secret
Cloudinary:Folder	Base folder	ecommerce/products
Paystack:SecretKey	Paystack secret key	sk_test_...
Paystack:PublicKey	Paystack public key	pk_test_...
Email:SmtpHost	SMTP server	smtp.gmail.com
Email:SmtpPort	SMTP port	587
Email:Username	SMTP username	your@email.com
Email:Password	SMTP app password	your-app-password
Admin:Email	Default admin email	admin@example.com
Admin:Password	Default admin password	StrongPassword123!
</details>
Setting Up Local Secrets
Bash

cd Ecommerce.API
dotnet user-secrets init
Then set each secret:

Bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=EcommerceDb;Username=postgres;Password=yourpassword;"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-min-32-chars"
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
Verify your secrets:

Bash

dotnet user-secrets list
Database Setup
1. Create the Database
SQL

CREATE DATABASE "EcommerceDb";
2. Apply Migrations
Bash

cd Ecommerce.API
dotnet ef database update --project ..\Ecommerce.Infrastructure --startup-project .
3. Verify Tables
The following tables will be created:

Tables Created
Users · Products · Categories · Orders · OrderItems
Carts · CartItem · Discounts · OutboxMessages · __EFMigrationsHistory
Running the Application
Development Mode
Bash

cd Ecommerce.API
dotnet run
The API will be available at:

Service	URL
HTTPS API	https://localhost:7xxx
HTTP API	http://localhost:5xxx
Swagger UI	https://localhost:7xxx/swagger
Health Check	https://localhost:7xxx/health
Running with Docker
Bash

docker build -t ecommerce-api .
docker run -p 8080:8080 ecommerce-api
API Documentation
Interactive API documentation is available via Swagger UI at /swagger when the app is running.

Authentication Endpoints
Method	Endpoint	Description	Auth
POST	/api/v1/auth/register	Register new user	No
POST	/api/v1/auth/login	Login and get JWT	No
Product Endpoints
Method	Endpoint	Description	Auth
GET	/api/v1/products	List products (paginated)	No
GET	/api/v1/products/{id}	Get product by ID	No
POST	/api/v1/products	Create product	Admin
PUT	/api/v1/products/{id}	Update product	Admin
PUT	/api/v1/products/{id}/images	Upload product images	Admin
DELETE	/api/v1/products/{id}	Delete product	Admin
POST	/api/v1/products/discounts	Create discount	Admin
Cart Endpoints
Method	Endpoint	Description	Auth
GET	/api/v1/cart	Get user's cart	User
POST	/api/v1/cart/items	Add item to cart	User
DELETE	/api/v1/cart/items/{id}	Remove item	User
Order Endpoints
Method	Endpoint	Description	Auth
POST	/api/v1/orders	Create order from cart	User
GET	/api/v1/orders	List user's orders	User
GET	/api/v1/orders/{id}	Get order details	User
Payment Endpoints
Method	Endpoint	Description	Auth
POST	/api/v1/payments/initiate	Initiate payment	User
POST	/api/v1/payments/webhook	Paystack webhook	No
Authentication
Getting a Token
Request:

http

POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "StrongPassword123!"
}
Response:

JSON

{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-05-21T10:00:00Z"
  }
}
Using the Token
Include the token in all protected requests:

http

Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Authorizing in Swagger
Click the Authorize button at top right
Paste your token
Click Authorize then Close
Image Upload
Products support up to 3 images: front, back, and side. Images are uploaded directly to Cloudinary.

Constraints
Constraint	Value
Allowed types	image/jpeg, image/png, image/webp
Max file size	5 MB per image
Max images	3 per product (one per label)
Upload Flow
text

Frontend  ──FormData──▶  API  ──Stream──▶  Cloudinary
                          ▲                     │
                          │                     │
                          └──────URL────────────┘
                          │
                          ▼
                       Database
Create the product (JSON) → POST /api/v1/products
Upload images (multipart) → PUT /api/v1/products/{id}/images
Example: cURL
Bash

curl -X PUT 'https://your-api.com/api/v1/products/{id}/images' \
  -H 'Authorization: Bearer YOUR_TOKEN' \
  -H 'Content-Type: multipart/form-data' \
  -F 'FrontImage=@/path/to/front.jpg' \
  -F 'BackImage=@/path/to/back.jpg' \
  -F 'SideImage=@/path/to/side.jpg'
Example: JavaScript
JavaScript

const formData = new FormData();
formData.append('FrontImage', frontFile);
formData.append('BackImage', backFile);
formData.append('SideImage', sideFile);

const response = await fetch(`/api/v1/products/${productId}/images`, {
  method: 'PUT',
  headers: { 'Authorization': `Bearer ${token}` },
  body: formData
});
Removing Images
Set Remove* flag to true:

JavaScript

formData.append('RemoveFront', 'true');
Note: Cannot upload and remove the same image in one request.

Deployment
Deploying to Render
<details> <summary><b>Step-by-step Render deployment guide</b></summary><br>
1. Create a Web Service

Go to Render Dashboard
Click New + then Web Service
Connect your GitHub repository
2. Configure Build

Runtime: Docker
Dockerfile path: Ecommerce.API/Dockerfile
3. Provision PostgreSQL

Click New + then PostgreSQL
Copy the Internal Database URL
4. Add Environment Variables

Use __ (double underscore) instead of : for nested keys:

Bash

ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
Jwt__Key=your-secret-key
Jwt__Issuer=EcommerceApi
Jwt__Audience=EcommerceClient
Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-api-key
Cloudinary__ApiSecret=your-api-secret
Cloudinary__Folder=ecommerce/products
Paystack__SecretKey=sk_live_...
Paystack__PublicKey=pk_live_...
Admin__Email=admin@example.com
Admin__Password=StrongPassword!
ASPNETCORE_ENVIRONMENT=Production
5. Deploy

Render auto-builds and deploys on every push to main.

</details>
Note: Migrations run automatically on startup via db.Database.MigrateAsync()

Testing
Bash

dotnet test
Testing Strategy
Type	Description
Unit Tests	Mock repositories, test services and domain logic
Integration Tests	Use in-memory DB or test PostgreSQL instance
Manual Testing	Use Swagger UI or Postman
Contributing
Adding a New Feature
text

1. Domain changes      ->  Ecommerce.Domain/Entities/
2. Application logic   ->  Ecommerce.Application/Services/
3. Persistence         ->  Ecommerce.Infrastructure/Repositories/
4. Register DI         ->  DependencyInjection.cs
5. Expose endpoint     ->  Ecommerce.API/Controllers/
6. Add validation      ->  Ecommerce.Application/Validators/
7. Create migration    ->  dotnet ef migrations add Name
Conventions
Keep domain logic inside domain entities
Application services orchestrate use cases
Repositories are thin EF abstractions
Use scoped lifetime for DbContext and repositories
Use FluentValidation for input validation
Log meaningful events via ILogger<T>
Troubleshooting
<details> <summary><b>Migration: "relation already exists"</b></summary><br>
Manually insert into __EFMigrationsHistory:

SQL

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('YourMigrationId', '9.0.0');
</details><details> <summary><b>401 Unauthorized in Swagger</b></summary><br>
Login via /api/v1/auth/login
Copy the JWT token
Click Authorize in Swagger
Paste the token
</details><details> <summary><b>Cloudinary Upload Fails</b></summary><br>
Verify all 3 Cloudinary credentials are set
Check file type is jpeg/png/webp
Check file size is under 5 MB
</details><details> <summary><b>"Keyword not supported: 'host'"</b></summary><br>
You are using PostgreSQL connection string with SQL Server provider. Switch to UseNpgsql() in Program.cs and AppDbContextFactory.cs.

</details><details> <summary><b>404 at root URL</b></summary><br>
Expected behavior. No route mapped to /. Use /swagger or /health.

</details>
License
This project is licensed under the MIT License. See the LICENSE file for details.

Acknowledgments
Built with:

ASP.NET Core — Web framework
Cloudinary — Image storage and CDN
Paystack — Payment processing
Hangfire — Background jobs
Serilog — Structured logging
<div align="center">
Star this repo if you find it helpful

Made with .NET 8

Report Bug · Request Feature

</div>