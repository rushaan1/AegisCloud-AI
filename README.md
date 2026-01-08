# AegisCloud AI

<div align="center">

![AegisCloud AI](https://img.shields.io/badge/Platform-Cloud%20Storage-blue)
![.NET](https://img.shields.io/badge/.NET-6.0-purple)
![Angular](https://img.shields.io/badge/Angular-18-red)
![License](https://img.shields.io/badge/License-MIT-green)

**A modern, cozy full-featured cloud storage platform with advanced file management, real-time synchronization, and AI-powered enhancements.**

[Features](#features) • [Tech Stack](#tech-stack) • [Getting Started](#getting-started) • [API Documentation](#api-documentation) • [Deployment](#deployment)

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Security](#security)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

---

## 🎯 Overview

AegisCloud AI is a comprehensive, enterprise-grade cloud storage solution built with modern web technologies. It provides secure file storage, advanced file management capabilities, real-time synchronization, and AI-powered features to enhance user productivity.

### Key Highlights

- **Secure & Scalable**: Built with security-first principles, featuring JWT authentication, encrypted storage, and role-based access control
- **Real-Time Updates**: Server-Sent Events (SSE) for instant file synchronization across devices
- **AI Integration**: Google Cloud AI Platform integration for image upscaling and enhancement
- **Modern UI**: Responsive Angular frontend with intuitive user experience
- **RESTful API**: Well-structured REST API with comprehensive Swagger documentation

---

## ✨ Features

### Core Functionality

- **📁 File & Folder Management**
  - Upload, download, rename, move, and delete files and folders
  - Batch operations for multiple files/folders
  - Hierarchical folder structure with unlimited nesting
  - Drag-and-drop file organization

- **🔍 Search & Organization**
  - Full-text search across files and folders
  - Advanced filtering by file type, date, size
  - Favorites system for quick access
  - Trash/Recycle bin with restore functionality
  - Recent files tracking

- **📊 File Types Support**
  - Images (JPEG, PNG, GIF, WebP, etc.)
  - Videos (MP4, AVI, MOV, etc.)
  - Audio files (MP3, WAV, FLAC, etc.)
  - Documents (PDF, DOCX, XLSX, etc.)
  - Automatic file type detection and categorization

### Advanced Features

- **🔗 Sharing & Collaboration**
  - Public link sharing with expiration dates
  - Secure share links with access control
  - Folder-level sharing
  - Download and preview permissions

- **🖼️ Media Features**
  - Automatic thumbnail generation for images and videos
  - In-browser media preview (images, videos, audio, PDF etc)
  - PDF viewer integration
  - Video thumbnail extraction with FFmpeg

- **🤖 AI-Powered Features**
  - Image upscaling using Google Cloud AI Platform (Imagen 4.0)
  - Background processing with RAM optimization
  - Automatic quality enhancement
  - File-based prompting (in progress — API/UX may change)
  - AI-based search (in development)

- **📈 Analytics & Insights**
  - Storage usage tracking and visualization
  - File type distribution analytics
  - Top files by size
  - Usage statistics and trends

- **🔐 Security & Authentication**
  - JWT-based authentication with refresh tokens
  - Google OAuth integration
  - Email verification with OTP
  - Session management
  - Secure cookie-based token storage
  - User-specific data isolation

- **⚡ Real-Time Features**
  - Server-Sent Events (SSE) for live updates
  - Real-time file synchronization
  - Instant notifications for file operations
  - Multi-device support

---

## 🛠️ Tech Stack

### Backend

- **Framework**: ASP.NET Core 6.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens
- **File Processing**: 
  - FFmpeg for video processing
  - ImageSharp for image manipulation
  - PdfiumViewer for PDF handling
- **AI Services**: Google Cloud AI Platform (Imagen 4.0)
- **Architecture**: Clean Architecture with Repository Pattern
- **API Documentation**: Swagger/OpenAPI

### Frontend

- **Framework**: Angular 18
- **UI Components**: Angular Material CDK
- **Charts**: ngx-charts
- **Icons**: Font Awesome
- **State Management**: RxJS Observables
- **HTTP Client**: Angular HttpClient with interceptors

### Infrastructure

- **Containerization**: Docker
- **Cloud Integration**: Azure App Service ready
- **Email Service**: SMTP (Gmail)
- **Storage**: File system (configurable for cloud storage)

---

## 🏗️ Architecture

### Project Structure

```
Cloud Storage Platform/
├── Cloud Storage Platform/          # Web API Project
│   ├── Controllers/                # API Controllers
│   ├── Filters/                    # Custom filters and middleware
│   ├── CustomModelBinders/         # Custom model binding
│   └── AngularApplication/         # Frontend application
│
├── CloudStoragePlatform.Core/      # Business Logic Layer
│   ├── Domain/                     # Domain entities and contracts
│   ├── Services/                   # Business services
│   ├── DTO/                        # Data Transfer Objects
│   └── ServiceContracts/           # Service interfaces
│
├── CloudStoragePlatform.Infrastructure/  # Data Access Layer
│   ├── DbContext/                  # Entity Framework context
│   ├── Repositories/               # Repository implementations
│   └── Migrations/                 # Database migrations
│
└── ServiceTests/                   # Unit tests
```

### Design Patterns

- **Clean Architecture**: Separation of concerns across layers
- **Repository Pattern**: Abstracted data access
- **Dependency Injection**: Loose coupling and testability
- **Service Layer**: Business logic encapsulation
- **DTO Pattern**: Data transfer optimization

### Data Flow

1. **Request** → Angular Frontend
2. **Authentication** → JWT Token Validation
3. **Authorization** → User Identification & Authorization
4. **Business Logic** → Service Layer Processing
5. **Data Access** → Repository Pattern → Entity Framework
6. **Response** → DTO Mapping → JSON Response
7. **Real-Time Updates** → SSE Events

---

## 🚀 Getting Started

### Prerequisites

- **.NET 6.0 SDK** or later
- **Node.js 18+** and npm
- **SQL Server** (LocalDB or full instance)
- **Visual Studio 2022** or **VS Code** (recommended)
- **FFmpeg** (included in project for video processing)

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/aegiscloud-ai.git
cd aegiscloud-ai
```

#### 2. Database Setup

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CloudStoragePlatform;Integrated Security=True;..."
  }
}
```

Run database migrations:

```bash
cd "Cloud Storage Platform"
dotnet ef database update --project ../CloudStoragePlatform.Infrastructure
```

#### 3. Backend Configuration

Update `appsettings.json` with your configuration:

```json
{
  "InitialPathForStorage": "C:\\CloudStoragePlatform",
  "Jwt": {
    "Issuer": "https://localhost:7219",
    "Audience": "https://localhost:4200",
    "EXPIRATION_MINUTES": 60
  },
  "RefreshToken": {
    "EXPIRATION_MINUTES": 44640
  },
  "JwtCloudStorageWebApi": "your-secret-key-here",
  "SMTPEmail": "your-email@gmail.com",
  "pwdsmtp": "your-smtp-password",
  "Google_Auth_Client_ID": "your-google-client-id",
  "GoogleServiceAccountJsonKey": "base64-encoded-service-account-key"
}
```

#### 4. Run the Backend

```bash
cd "Cloud Storage Platform"
dotnet restore
dotnet run
```

The API will be available at `https://localhost:7219`
Swagger documentation: `https://localhost:7219/swagger`

#### 5. Frontend Setup

```bash
cd "Cloud Storage Platform/AngularApplication/CloudStoragePlatformUI"
npm install
npm start
```

The frontend will be available at `https://localhost:4200`

### Docker Deployment

Build and run using Docker:

```bash
docker build -t aegiscloud-ai .
docker run -p 8080:80 aegiscloud-ai
```

---

## 📚 API Documentation

### Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Account/register` | User registration |
| POST | `/api/Account/login` | User login |
| POST | `/api/Account/google-login` | Google OAuth login |
| POST | `/api/Account/logout` | User logout |
| POST | `/api/Account/regenerate-jwt-token` | Refresh access token |
| POST | `/api/Account/send-verification-otp` | Send email verification OTP |
| POST | `/api/Account/verify-email` | Verify email with OTP |

### File Management Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Modifications/upload` | Upload files (multipart) |
| GET | `/api/Retrievals/filePreview` | Preview file |
| GET | `/api/Retrievals/download` | Download files/folders |
| PATCH | `/api/Modifications/rename` | Rename file/folder |
| DELETE | `/api/Modifications/batchDelete` | Delete files/folders |
| PATCH | `/api/Modifications/batchMove` | Move files/folders |

### Folder Management Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Modifications/add` | Create folder |
| GET | `/api/Retrievals/getAllInHome` | Get home folder contents |
| GET | `/api/Retrievals/getAllChildrenById` | Get folder children |
| GET | `/api/Retrievals/getAllFiltered` | Search files/folders |

### Sharing Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Shares/CreateShare` | Create share link |
| DELETE | `/api/Shares/RemoveShare` | Remove share link |
| GET | `/api/Shares/FetchSharedContent` | Access shared content |
| GET | `/api/Shares/DownloadSharedContent` | Download shared content |

### Real-Time Events

| Endpoint | Description |
|----------|-------------|
| GET | `/api/Modifications/sseauth` | Get SSE authentication token |
| GET | `/api/Modifications/sse` | Server-Sent Events stream |

**Event Types:**
- `added` - File/folder added
- `renamed` - File/folder renamed
- `moved` - File/folder moved
- `deleted` - File/folder deleted
- `favorite_updated` - Favorite status changed
- `trash_updated` - Trash status changed

### Complete API Documentation

Visit `/swagger` when running the application for interactive API documentation with request/response examples.

---

## 🔒 Security

### Authentication & Authorization

- **JWT Tokens**: Secure token-based authentication
- **Refresh Tokens**: Long-lived refresh tokens with automatic renewal
- **Cookie Security**: HttpOnly, Secure, SameSite cookies
- **Password Policy**: Configurable password requirements
- **Session Management**: Multi-device session tracking

### Data Protection

- **User Isolation**: Each user's data is completely isolated
- **Path Validation**: Server-side path validation to prevent directory traversal
- **File Type Validation**: MIME type checking for uploads
- **Input Sanitization**: Model binding with validation

### Security Best Practices

- HTTPS enforced in production
- CORS configured for allowed origins
- SQL injection prevention via parameterized queries
- XSS protection through Angular's built-in sanitization
- CSRF protection via SameSite cookies

### Encryption

- **At Rest**: Files stored with user-specific isolation
- **In Transit**: TLS/SSL encryption for all communications
- **Sensitive Data**: Configuration secrets stored securely

---

## 🚢 Deployment

### Azure App Service

1. **Create App Service** in Azure Portal
2. **Configure Application Settings**:
   - Connection strings
   - JWT secrets
   - Storage paths
   - SMTP credentials
3. **Deploy** using Visual Studio Publish or Azure DevOps

### Docker Deployment

```bash
# Build image
docker build -t aegiscloud-ai:latest .

# Run container
docker run -d \
  -p 8080:80 \
  -e ConnectionStrings__Default="your-connection-string" \
  -e InitialPathForStorage="/app/storage" \
  aegiscloud-ai:latest
```

### Environment Variables

Required environment variables for production:

```bash
ConnectionStrings__Default=<sql-connection-string>
InitialPathForStorage=<storage-path>
Jwt__Issuer=<jwt-issuer>
Jwt__Audience=<jwt-audience>
JwtCloudStorageWebApi=<jwt-secret>
SMTPEmail=<smtp-email>
pwdsmtp=<smtp-password>
Google_Auth_Client_ID=<google-client-id>
GoogleServiceAccountJsonKey=<base64-service-account-key>
```

### Production Checklist

- [ ] Update connection strings
- [ ] Configure HTTPS certificates
- [ ] Set up cloud storage (Azure Blob Storage recommended)
- [ ] Configure CORS for production domains
- [ ] Set up monitoring and logging
- [ ] Configure backup strategy
- [ ] Review security settings
- [ ] Set up CI/CD pipeline

---

## 🧪 Testing

### Running Tests

```bash
cd ServiceTests
dotnet test
```

### Test Coverage

- Unit tests for service layer
- Integration tests for API endpoints
- Repository pattern testing
- Authentication flow testing

---

## 📊 Performance

### Optimizations

- **Streaming**: Large file uploads/downloads use streaming
- **Lazy Loading**: Entity Framework lazy loading for related entities
- **Thumbnail Caching**: Generated thumbnails cached for performance
- **Background Processing**: AI operations run in background with RAM monitoring
- **Efficient Queries**: Optimized database queries with proper indexing

### Scalability

- Stateless API design for horizontal scaling
- Database connection pooling
- File storage abstraction for cloud migration
- SSE connection management for real-time features

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# and TypeScript coding conventions
- Write unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Google Cloud AI Platform** for image upscaling capabilities
- **FFmpeg** for video processing
- **ImageSharp** for image manipulation
- **Entity Framework Core** for data access
- **Angular** team for the excellent framework

---

## 📧 Contact & Support

For questions, issues, or contributions:

- **Issues**: [GitHub Issues](https://github.com/yourusername/aegiscloud-ai/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/aegiscloud-ai/discussions)

---

<div align="center">

**Built with ❤️ using .NET and Angular**

⭐ Star this repo if you find it helpful!

</div>
