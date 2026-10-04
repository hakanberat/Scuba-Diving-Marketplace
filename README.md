# Scuba Diving Marketplace

**Developed: 2026**

A web-based scuba diving marketplace built with **.NET 10, ASP.NET Core MVC, Dapper, and Microsoft SQL Server**.

The platform connects divers with dive centers and allows users to discover diving activities, compare locations and prices, view dive center and instructor profiles, and submit reservation requests.

Dive center owners can apply to join the marketplace, manage their center profile, activities, instructors, photos, reservations, and revenue summaries. Administrators can review center applications and manage marketplace users, centers, and activity categories.

## Marketplace Concept

The project is designed around three main user roles:

### Customer

Customers can:

- Register and log in
- Browse dive centers
- Search diving activities
- Filter activities by location, category, and price
- View activity details
- View dive center profiles
- View instructors
- Create reservation requests
- Choose preferred contact methods
- Track reservation status
- Cancel eligible reservations
- Manage their profile
- Receive notifications

### Dive Center Owner

Dive center owners can:

- Register as a dive center owner
- Submit a dive center application
- Track application status
- Manage approved dive centers
- Edit center information
- Manage activities
- Manage instructors
- Upload and manage photos
- Set cover photos
- View reservation requests
- Confirm, reject, or cancel reservations
- Add reservation notes
- View dashboard statistics
- View reservation revenue summaries
- Receive notifications

### Administrator

Administrators can:

- Review dive center applications
- Approve or reject applications
- Activate or deactivate dive centers
- Manage users
- Manage dive center owner accounts
- Manage activity categories
- Assign dive center access
- View marketplace administration data
- Receive application notifications

## Activity Discovery

Users can search and filter active diving activities.

Available filters include:

- Search text
- Country
- City
- Activity category
- Minimum price
- Maximum price

Activity listings can include:

- Activity name
- Dive center
- Category
- Country
- City
- Location
- Map location
- Price
- Currency
- Duration
- Minimum age
- Minimum certification
- Maximum participants
- Included items
- Excluded items
- Requirements
- Description
- Cover photo

## Dive Centers

Public dive center profiles can contain:

- Name
- Description
- Country
- City
- Address
- Map location
- Phone
- Email
- Website
- Logo
- Cover photo
- Activities
- Instructors
- Photo gallery

Only active dive centers are shown publicly.

## Instructors

Dive centers can manage instructor profiles containing:

- First name
- Last name
- Biography
- Photo
- Certification agency
- Instructor number
- Active / inactive status

Only active instructors are shown publicly.

## Reservations

Customers can create reservation requests for active activities.

Reservation data includes:

- Reservation number
- Activity
- Dive center
- Customer
- Reservation date
- Participant count
- Unit price
- Total price
- Currency
- Status
- Customer notes
- Contact email
- Contact phone
- WhatsApp contact
- Preferred contact methods
- Dive center notes
- Creation date
- Confirmation date
- Cancellation date

Reservation numbers are generated automatically.

## Reservation Price Snapshot

The reservation stores the activity price at the time the booking request is created.

This preserves historical pricing even if the dive center later changes the activity price.

Example:

Activity price: 80 EUR
Participants: 2

Reservation total:

160 EUR

If the activity later changes to 100 EUR, the existing reservation keeps its original 80 EUR unit price.

## Reservation Status

Reservation requests can move through statuses such as:

- Pending
- Confirmed
- Rejected
- Cancelled

Dive center owners and administrators can manage reservation status according to their permissions.

Customers can cancel eligible reservations.

## Contact Preferences

Customers can provide reservation contact details including:

- Email
- Phone
- WhatsApp

They can also choose which contact methods they prefer the dive center to use.

## Dive Center Application Workflow

Dive center owners register through a dedicated application process.

Typical workflow:

Owner Registration
        |
        v
Dive Center Application
        |
        v
Pending Admin Review
        |
        +--> Approved --> Dive Center Created --> Owner Access Granted
        |
        `--> Rejected

When an application is approved:

- A new dive center record is created
- The applicant is linked to the dive center as its owner
- The owner can begin managing the center

## Notifications

The application includes an internal notification system.

Notifications are generated for events such as:

- New dive center applications
- Application approval or rejection
- New reservation requests
- Reservation status changes

Users can:

- View unread notifications
- View notification history
- Mark notifications as read
- Mark all notifications as read

## Photo Management

Dive centers and activities can have photo galleries.

Supported image formats:

- JPEG
- PNG
- WebP

Current photo rules include:

- Maximum file size: 5 MB
- Maximum photos per managed resource: 20
- Cover photo selection
- Secure generated filenames
- Server-side file validation

Photo records are stored in the database while uploaded image files are stored under the application's web assets.

## Dashboard

The dive center dashboard includes operational information such as:

- Active activities
- Active instructors
- Pending reservations
- Confirmed reservations
- Upcoming reservations
- Latest reservations
- Managed dive centers
- Revenue summaries

## Revenue Summary

Reservation revenue is grouped by:

- Dive center
- Currency

The dashboard distinguishes values such as:

- Confirmed reservation count
- Confirmed reservation amount
- Pending reservation amount

Currencies remain independent and are not automatically converted.

## Authentication

The platform uses **ASP.NET Core Cookie Authentication**.

Passwords are hashed using ASP.NET Core `PasswordHasher`.

Authentication features include:

- Customer registration
- Dive center owner registration
- Login
- Logout
- Role-based authorization
- User activation status
- Secure HTTP-only authentication cookies

Supported roles include:

- Customer
- DiveCenter
- Admin

## Security

The application includes several security-focused practices:

- Password hashing
- Cookie authentication
- Role-based authorization
- HTTP-only authentication cookies
- Secure-cookie policy outside development
- Anti-forgery validation for MVC forms
- Server-side validation
- Ownership checks for dive center resources
- Database-scoped access control
- Parameterized SQL through Dapper
- Image signature validation
- File-size limits
- Generated image filenames

## Technology Stack

### Backend

- C#
- .NET 10
- ASP.NET Core MVC
- ASP.NET Core Cookie Authentication
- ASP.NET Core PasswordHasher

### Data Access

- Dapper
- Microsoft.Data.SqlClient
- Microsoft SQL Server

### Frontend

- Razor Views
- HTML
- CSS
- JavaScript
- Bootstrap

### Development

- Visual Studio Code
- PowerShell
- SQL Server Management Studio
- Git
- GitHub

## Architecture

The application uses a service-based MVC architecture:

Browser
   |
   v
ASP.NET Core MVC
   |
   v
Controllers
   |
   v
Services
   |
   v
Dapper
   |
   v
Microsoft SQL Server

Uploaded images are stored separately in the web application's file system.

## Project Structure

DiveMarketplace
|
|-- Controllers
|-- Data
|-- Database
|-- Models
|-- Services
|-- Tests
|-- ViewModels
|-- Views
|-- wwwroot
|-- Program.cs
|-- appsettings.json
`-- DiveMarketplace.csproj

## Database

The project uses Microsoft SQL Server.

Default development database:

DiveMarketplaceDb

The repository includes SQL setup and migration scripts in the `Database` directory.

Current scripts cover areas including:

- Application users
- Dive center user relationships
- Activity categories
- Instructors
- Activities
- Reservations
- Seed data
- Demo activities
- Admin setup
- Photos
- Dive center applications
- Notifications
- Locations
- Reservation contact information
- Map locations

## Main Database Entities

The application includes data structures for:

- AppUsers
- DiveCenters
- DiveCenterUsers
- DiveCenterApplications
- ActivityCategories
- Activities
- Instructors
- Reservations
- Photos
- Notifications

## Running the Application

Requirements:

- .NET 10 SDK
- Microsoft SQL Server
- SQL Server Management Studio or another SQL Server client
- Git

Clone the repository:

git clone https://github.com/hakanberat/DiveMarketplace.git

Enter the project directory:

cd DiveMarketplace

Restore dependencies:

dotnet restore

Create the `DiveMarketplaceDb` SQL Server database and run the SQL scripts in the `Database` directory in numerical order.

Build the application:

dotnet build

Run the application:

dotnet run

ASP.NET Core will display the local application address in the terminal.

## Testing

The repository contains PowerShell smoke tests covering areas such as:

- Core marketplace workflows
- Photo management
- Notifications
- Admin notification UI
- Profile management
- Location and contact information
- Reservation revenue

These tests are stored in the `Tests` directory.

## Development Status

The project is actively developed.

The current version already includes the main marketplace workflow:

Discover Activity
        |
        v
View Dive Center
        |
        v
Create Account
        |
        v
Request Reservation
        |
        v
Dive Center Review
        |
        v
Reservation Confirmed / Rejected / Cancelled

It also includes a separate onboarding and approval workflow for dive center owners.

## Future Improvements

Potential future improvements include:

- Online payments
- Customer reviews and ratings
- Availability calendars
- Real-time booking capacity
- Email notifications
- SMS and WhatsApp integrations
- REST API
- .NET MAUI mobile client
- Advanced maps
- Favorites and wishlists
- Advanced search ranking
- Multi-language support
- Cloud image storage
- Cloud deployment
- Automated integration tests
- Docker support

## Purpose

This project demonstrates practical experience with:

- ASP.NET Core MVC
- C#
- SQL Server
- Dapper
- Marketplace architecture
- Authentication
- Authorization
- Role-based access control
- Reservation workflows
- Multi-tenant resource ownership
- Database design
- Photo upload handling
- Notifications
- Search and filtering
- Business rules
- Responsive web development
- Git and GitHub

The project is based on practical scuba diving industry workflows and explores how dive centers, instructors, activities, and customers can be brought together in a single digital marketplace.

## Author

**Hakan Berat Demircan**
