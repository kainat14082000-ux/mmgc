# MMGC Hospital Management System - Setup Guide

## Overview
This is a comprehensive Hospital Management System built with ASP.NET Core MVC, Entity Framework Core, and Identity for authentication.

## Features Implemented

### ✅ Core Modules
1. **Admin Dashboard** - Overview cards, revenue statistics, daily appointments
2. **Appointments Module** - CRUD operations, SMS/WhatsApp notifications, doctor/nurse assignment
3. **Doctors Management** - CRUD operations, doctor profiles with statistics
4. **Patients Management** - CRUD operations, patient history (visits, prescriptions, lab reports, invoices)
5. **Procedures & Treatments** - Medical procedures management, treatment notes, prescriptions
6. **Laboratory Management** - Test categories, lab test booking, report uploads
7. **Transactions & Invoices** - Financial records, payment modes, invoice generation

### ✅ Technical Implementation
- Repository pattern for data access (DRY principle)
- Service layer for business logic
- Modern Bootstrap UI with Bootstrap Icons
- Entity Framework Core with SQL Server
- Identity for authentication and authorization
- Scalable architecture

## Database Setup

### Step 1: Install EF Core Tools (if not already installed)
```bash
dotnet tool install --global dotnet-ef
```

### Step 2: Create Migration
```bash
cd /var/www/html/dotnetcoremvc/MMGC
dotnet ef migrations add HospitalManagementSystem --context ApplicationDbContext
```

### Step 3: Apply Migration
```bash
dotnet ef database update --context ApplicationDbContext
```

## Database Models

The system includes the following entities:
- **Patient** - Patient information with MR Number
- **Doctor** - Doctor profiles with specialization
- **Nurse** - Nurse information
- **Appointment** - Appointment scheduling
- **Procedure** - Medical procedures and treatments
- **LabTestCategory** - Lab test categories
- **LabTest** - Laboratory tests and reports
- **Transaction** - Financial transactions
- **Prescription** - Patient prescriptions
- **DoctorSchedule** - Doctor availability schedules

## Running the Application

1. **Update Connection String** in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  }
}
```

2. **Run the application**:
```bash
dotnet run
```

3. **Access the application**:
   - Default route redirects to `/Admin/Dashboard` (requires authentication)
   - Register/Login using Identity pages at `/Identity/Account/Register` or `/Identity/Account/Login`

## Default Routes

- `/Admin/Dashboard` - Admin dashboard (default)
- `/Patients` - Patient management
- `/Doctors` - Doctor management
- `/Appointments` - Appointment management
- `/Procedures` - Procedure management
- `/LabTests` - Laboratory test management
- `/Transactions` - Transaction management

## Next Steps (Optional Enhancements)

1. **SMS/WhatsApp Integration**:
   - Implement actual SMS sending using Twilio or similar service
   - Implement WhatsApp Business API integration
   - Update `AppointmentService.SendSMSNotificationAsync()` and `SendWhatsAppNotificationAsync()`

2. **Invoice Generation**:
   - Implement PDF generation for invoices (using libraries like QuestPDF or iTextSharp)
   - Update `TransactionService.GenerateInvoiceAsync()`

3. **Reports Module**:
   - Create Reports controller and views
   - Implement medical reports generation
   - Implement financial reports
   - Implement patient-wise reports

4. **Additional Features**:
   - Email notifications
   - File upload for lab reports (already scaffolded)
   - Advanced search and filtering
   - Export to Excel/PDF
   - Print functionality

## Architecture

The application follows a clean architecture pattern:

```
MMGC/
├── Controllers/     # MVC Controllers
├── Models/          # Domain Models/Entities
├── Data/            # DbContext and Identity
├── Repositories/    # Data Access Layer (Repository Pattern)
├── Services/        # Business Logic Layer
└── Views/           # Razor Views
```

## Notes

- All controllers require authentication (`[Authorize]` attribute)
- The system uses Bootstrap 5 for UI
- Bootstrap Icons are included for modern icons
- The code follows DRY (Don't Repeat Yourself) principles
- Services are registered in `Program.cs` using dependency injection

## Support

For issues or questions, please refer to the codebase documentation or contact the development team.
