Ecommerce API
A production-ready, layered ASP.NET Core 8 Web API for an e-commerce platform featuring product management with Cloudinary image uploads, JWT authentication, payment integration with Paystack, background job processing, and comprehensive observability.

Table of Contents
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
Image Upload Feature
Deployment
Testing
Contributing
Troubleshooting
License
Features
Product Management — Full CRUD operations with categories, pricing, and stock tracking
Image Uploads — Multipart file uploads to Cloudinary (front, back, side images per product)
Shopping Cart — Persistent user carts with quantity management
Order Management — Complete order lifecycle with status tracking
Discount System — Active discount calculation with snapshot pricing
JWT Authentication — Secure token-based auth with role-based authorization (Admin/User)
Payment Integration — Paystack payment gateway with webhook verification
Background Jobs — Hangfire for scheduled tasks plus hosted services
Outbox Pattern — Reliable event publishing with retry support
Caching — In-memory caching (Redis ready)
Rate Limiting — Built-in API throttling
Health Checks — Database and Redis health monitoring
Structured Logging — Serilog with console and file sinks
Distributed Tracing — OpenTelemetry instrumentation
API Versioning — Versioned endpoints starting at v1
Validation — FluentValidation for all request DTOs
Global Exception Handling — Centralized error response middleware
Tech Stack
Category	Technology
Framework	.NET 8, C# 12
Web	ASP.NET Core Web API
Database	PostgreSQL with EF Core
Authentication	JWT Bearer Tokens
File Storage	Cloudinary
Payments	Paystack
Background Jobs	Hangfire + IHostedService
Caching	In-Memory (Redis ready)
Logging	Serilog
Tracing	OpenTelemetry
Validation	FluentValidation
Documentation	Swagger / OpenAPI
Email	SMTP
Architecture
The project follows a clean layered architecture with strict separation of concerns:

text

┌───────────────────────────────────────────┐
│         Ecommerce.API (Web Layer)         │
│  Controllers · Middleware · Program.cs    │
└───────────────────┬───────────────────────┘
                    │
┌───────────────────▼───────────────────────┐
│   Ecommerce.Application (Use Cases)       │
│  Services · DTOs · Interfaces · Validators│
└───────────────────┬───────────────────────┘
                    │
┌───────────────────▼───────────────────────┐
│      Ecommerce.Domain (Core)              │
│  Entities · Domain Logic · Enums          │
└───────────────────▲───────────────────────┘
                    │
┌───────────────────┴───────────────────────┐
│   Ecommerce.Infrastructure (Adapters)     │
│  EF Core · Repositories · External APIs   │
└───────────────────────────────────────────┘
Dependency direction: API → Application → Domain ← Infrastructure

Project Structure
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
Prerequisites
Before getting started, ensure you have the following installed:

.NET 8 SDK
PostgreSQL 14+ (or use a hosted instance)
Cloudinary account (free tier works)
Paystack account (optional, for payments)
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
Key	Description	Example
ConnectionStrings:DefaultConnection	PostgreSQL connection string	Host=localhost;Database=EcommerceDb;Username=postgres;Password=...
Jwt:Key	Secret key for JWT signing (min 32 chars)	your-super-secret-key-min-32-chars
Jwt:Issuer	JWT issuer	EcommerceApi
Jwt:Audience	JWT audience	EcommerceClient
Cloudinary:CloudName	Cloudinary cloud name	your-cloud-name
Cloudinary:ApiKey	Cloudinary API key	123456789012345
Cloudinary:ApiSecret	Cloudinary API secret	your-secret
Cloudinary:Folder	Base folder in Cloudinary	ecommerce/products
Paystack:SecretKey	Paystack secret key	sk_test_...
Paystack:PublicKey	Paystack public key	pk_test_...
Email:SmtpHost	SMTP server host	smtp.gmail.com
Email:SmtpPort	SMTP server port	587
Email:Username	SMTP username	your@email.com
Email:Password	SMTP password (app password)	your-app-password
Admin:Email	Default admin email (seeded on startup)	admin@example.com
Admin:Password	Default admin password	StrongPassword123!
Setting Up Local Secrets
Navigate to the API project folder:

Bash

cd Ecommerce.API
dotnet user-secrets init
Set all required secrets:

Bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=EcommerceDb;Username=postgres;Password=yourpassword;"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-min-32-chars"
dotnet user-secrets set "Jwt:Issuer" "EcommerceApi"
dotnet user-secrets set "Jwt:Audience" "EcommerceClient"
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "your-api-key"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
dotnet user-secrets set "Cloudinary:Folder" "ecommerce/products"
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "StrongPassword123!"
Verify the secrets:

Bash

dotnet user-secrets list
Database Setup
1. Create the Database
Connect to PostgreSQL and create the database:

SQL

CREATE DATABASE "EcommerceDb";
2. Apply Migrations
From the Ecommerce.API folder run:

Bash

dotnet ef database update --project ..\Ecommerce.Infrastructure --startup-project .
3. Verify Tables
The following tables should be created:

Users
Products
Categories
Orders
OrderItems
Carts
CartItem
Discounts
OutboxMessages
__EFMigrationsHistory
Running the Application
Development Mode
Bash

cd Ecommerce.API
dotnet run
The API will be available at:

HTTPS: https://localhost:7xxx
HTTP: http://localhost:5xxx
Swagger UI: https://localhost:7xxx/swagger
Health Check: https://localhost:7xxx/health
Running with Docker
Bash

docker build -t ecommerce-api .
docker run -p 8080:8080 ecommerce-api
API Documentation
Swagger UI
Once the app is running, navigate to:

text

https://localhost:7xxx/swagger
All endpoints are documented with request/response schemas and example payloads.

Main Endpoints Overview
Authentication
Method	Endpoint	Description	Auth
POST	/api/v1/auth/register	Register new user	No
POST	/api/v1/auth/login	Login and get JWT	No
Products
Method	Endpoint	Description	Auth
GET	/api/v1/products	List products (paginated)	No
GET	/api/v1/products/{id}	Get product by ID	No
POST	/api/v1/products	Create product	Admin
PUT	/api/v1/products/{id}	Update product	Admin
PUT	/api/v1/products/{id}/images	Upload product images	Admin
DELETE	/api/v1/products/{id}	Delete product	Admin
POST	/api/v1/products/discounts	Create discount	Admin
Cart
Method	Endpoint	Description	Auth
GET	/api/v1/cart	Get current user's cart	User
POST	/api/v1/cart/items	Add item to cart	User
DELETE	/api/v1/cart/items/{id}	Remove item from cart	User
Orders
Method	Endpoint	Description	Auth
POST	/api/v1/orders	Create order from cart	User
GET	/api/v1/orders	List user's orders	User
GET	/api/v1/orders/{id}	Get order details	User
Payments
Method	Endpoint	Description	Auth
POST	/api/v1/payments/initiate	Initiate payment	User
POST	/api/v1/payments/webhook	Paystack webhook	No
Authentication
Getting a Token
Send a POST request to /api/v1/auth/login:

JSON

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
Include the token in the Authorization header for all protected endpoints:

text

Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Authorizing in Swagger UI
Click the "Authorize" button at the top right
Paste the token (with or without Bearer prefix depending on configuration)
Click "Authorize" then "Close"
Image Upload Feature
Products support up to three images: front, back, and side. Images are uploaded directly to Cloudinary as actual files (not URLs).

Constraints
Allowed types: image/jpeg, image/png, image/webp
Max file size: 5 MB per image
Max images per product: 3 (one of each label)
Upload Flow
Create the product (JSON) via POST /api/v1/products
Upload images (multipart) via PUT /api/v1/products/{id}/images
Example: Upload Images (cURL)
Bash

curl -X PUT 'https://your-api.com/api/v1/products/{id}/images' \
  -H 'Authorization: Bearer YOUR_TOKEN' \
  -H 'Content-Type: multipart/form-data' \
  -F 'FrontImage=@/path/to/front.jpg' \
  -F 'BackImage=@/path/to/back.jpg' \
  -F 'SideImage=@/path/to/side.jpg'
Example: Upload Images (JavaScript)
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
To delete an image set the corresponding Remove* flag to true:

JavaScript

formData.append('RemoveFront', 'true');
Note: Cannot upload and remove the same image in one request.

Deployment
Deploying to Render
Create a Web Service on Render
Connect your GitHub repository
Set the Dockerfile path: Ecommerce.API/Dockerfile
Add Environment Variables (use __ instead of : for nested keys):
text

ConnectionStrings__DefaultConnection=Host=...;Database=...;...
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
Provision a PostgreSQL database on Render
Copy the internal connection string to your environment variables
Deploy — Render will build and run automatically
Database Migrations on First Deploy
Migrations run automatically on app startup via:

csharp

await db.Database.MigrateAsync();
For pre-existing databases, the migration history is auto-registered to avoid conflicts.

Testing
Run All Tests
Bash

dotnet test
Recommended Testing Strategy
Unit Tests: Mock repository interfaces, test application services and domain logic
Integration Tests: Use in-memory database or test PostgreSQL instance
Manual Testing: Use Swagger UI or Postman with the included environment
Contributing
Adding a New Feature
Domain changes → Add/modify entities in Ecommerce.Domain/Entities/
Application logic → Add service interface + implementation in Ecommerce.Application/
Persistence → Add repository in Ecommerce.Infrastructure/Repositories/
Register DI → Add to AddInfrastructure() in DependencyInjection.cs
Expose endpoint → Add controller method in Ecommerce.API/Controllers/
Validation → Add FluentValidator in Ecommerce.Application/Validators/
Migrations → Run dotnet ef migrations add YourMigrationName
Conventions
Keep domain logic inside domain entities
Application services orchestrate use cases
Repositories are thin EF abstractions
Use scoped lifetime for DbContext and repositories
Use FluentValidation for input validation
Throw domain-specific exceptions, let middleware map to HTTP responses
Log meaningful events via ILogger<T>
Troubleshooting
Migration: "relation already exists"
Drop the database and re-apply migrations, or manually insert into __EFMigrationsHistory:

SQL

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('YourMigrationId', '9.0.0');
401 Unauthorized in Swagger
Login first via /api/v1/auth/login, then click "Authorize" in Swagger and paste your JWT token.

Cloudinary Upload Fails
Verify all three Cloudinary credentials are set
Check the file type is jpeg/png/webp
Check the file size is under 5 MB
Connection String "Keyword not supported: 'host'"
You are using a PostgreSQL connection string but EF is configured for SQL Server. Switch to UseNpgsql() in Program.cs and AppDbContextFactory.cs.

404 at Root URL
This is expected. The API has no route at /. Try /swagger or /health instead.

License
This project is licensed under the MIT License. See the LICENSE file for details.

Acknowledgments
Built with ASP.NET Core
Image storage powered by Cloudinary
Payments powered by Paystack
Background jobs via Hangfire
Contact
For questions or support, please open an issue on the GitHub repository.