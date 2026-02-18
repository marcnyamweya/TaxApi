# Tax Workflow Processing API

## Overview

The **Tax Workflow Processing API** is a backend system designed to simulate an enterprise-grade tax processing workflow. The application enables structured client data management, automated tax calculations, validation enforcement, and audit logging to reflect real-world business processing environments.

This project demonstrates backend engineering principles including layered architecture, RESTful API design, relational database management, centralized error handling, and documentation standards aligned with enterprise development practices.

---

## Objectives

- Design and implement a structured tax processing backend system  
- Simulate enterprise client tax submission workflows  
- Enforce data validation and business rules  
- Maintain audit logs for traceability  
- Provide well-documented RESTful endpoints  
- Demonstrate clean architecture and maintainability  

---

## Technology Stack

### Backend
- C#
- .NET Core Web API
- Entity Framework Core

### Database
- SQL Server (Running on Docker for Mac)

### Documentation & Testing
- Swagger (OpenAPI)
- Postman

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop)

### Database Setup (Docker)

To run SQL Server on Mac using Docker, use the following command to pull and start the SQL Server 2022 container:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Password" \
   -p 1433:1433 --name sql_server_container \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

*Note: If you are on an Apple Silicon (M1/M2/M3) Mac, Docker Desktop will automatically use Rosetta 2 to run the x86_64 image.*

### Configuration

Update your `appsettings.Development.json` with the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=TaxDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True"
}
```

---

## System Architecture

The application follows a layered architecture to ensure separation of concerns and maintainability.