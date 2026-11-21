An accommodation booking web app where guests browse and book stays, hosts apply to list properties, and admins review applications and manage users.

Built with ASP.NET Core MVC, Entity Framework Core and ASP.NET Core Identity as a Richfield BSc IT programming project.
Features

	•	User accounts: register and log in with ASP.NET Core Identity (password rules enforced, minimum 8 characters).
	•	Browse stays: guests explore available properties and send booking requests.
	•	Host applications: users apply to become hosts and list their properties.
	•	Admin dashboard: shows total, pending and rejected host applications, with tools to review applications, handle booking requests and manage users.
	•	Role-based access: guest, host and admin areas are separated by role.

Tech Stack

	•	C# and ASP.NET Core MVC (Controllers, Views, Models, Services, Areas)
	•	Entity Framework Core with ASP.NET Core Identity
	•	SQLite database
	•	HTML, CSS and Razor views

Project Structure

Controllers/   request handling for stays, bookings, applications and admin
Models/        entities and view models
Data/          ApplicationDbContext (Identity + EF Core)
Services/      business logic
Views/         Razor pages
Areas/         Identity and admin areas
wwwroot/       static files (CSS, JS, images)

Getting Started

Requirements: .NET SDK installed.

	1.	Clone the repository:

git clone https://github.com/Matimu-Hlongwane/staybnb.git
cd staybnb

	2.	Restore packages:

dotnet restore

	3.	Run the app:

dotnet run

	4.	Open the local URL shown in the terminal (for example https://localhost:5001).

The local database file (app.db) is created on your machine and is not included in this repository.

What I Learned

	•	Building a full-stack MVC application with authentication and role-based access.
	•	Modelling relational data (users, properties, applications, bookings) with Entity Framework Core.
	•	Structuring an application into controllers, services and views.
	•	Building an admin workflow for reviewing and approving host applications.

Author

Matimu Hlongwane — BSc Information Technology, Richfield
LinkedIn · GitHub 
