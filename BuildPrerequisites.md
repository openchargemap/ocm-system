Basic build prerequisites

- Windows 7 or Higher (or Windows Server 2008 or higher)
- Visual Studio 2015 Professional or the equivalent Express Editions (Web, Windows) with latest NuGet package manager installed and latest TypeScript add-in installed
- MS SQL Express 2008 R2 onwards
- NodeJS
- MongoDB
	- 

Note: it is possible to most of the /App development on non-windows machines without Visual Studio however it is not officially supported.

Deployment:
 - Configure MongoDB as services and initialise ocm_mirror database
 - Set ASP.net State Services to Automatic Startup and Start services
 - Install SQL Server 2012 CLR Data Types (Version 11.x) - required by entity framework
 - Install URL Reqwrite 2.0 - required for handler mapping
 - Enable read/write for app pool user for \Temp folders
 - Configure web.config