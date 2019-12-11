Open Charge Map (OCM)
==========

### About the project

[Open Charge Map](https://openchargemap.org) is the global public registry of electric vehicle charging locations.
OCM was first established in 2011 and aims to crowdsource a high quality, well maintained open data set with the greatest breadth possible. Our data set is a mixture of manually entered information and imported open data sources (such as government-run registries and charging networks with an open data license). OCM provides data to drivers (via hundreds of apps and sites), as well to researchers, EV market analysts, and government policy makers. 

The code in this repository represents the backend systems ([API](https://openchargemap.org/site/develop/), [Web Site](https://openchargemap.org) and server-side import processing) for the project. Server-side code is developed mostly in C#, currently building under Visual Studio 2019 Community Edition with .Net Core 3.0, Data is stored in SQL Server, using Entity Framework Core, with an additional caching layer using MongoDB.

Developers can use our [API](https://openchargemap.org/site/develop/) to access the data set and build their own [apps](https://openchargemap.org/site/develop/apps/). The map [app](https://map.openchargemap.io) source (using latest Ionic/Angular/TypeScript) can be found in its own repo at https://github.com/openchargemap/ocm-app


### Basic build prerequisites

- Windows 7 or higher (or Windows Server 2008 or higher)
- Visual Studio 2019
- MS SQL Express 2008 R2 onwards
- NodeJS
- MongoDB

### Deployment 

 - Configure MongoDB as services and initialise ocm_mirror database
 - Set ASP.net State Services to Automatic Startup and Start services
 - Install SQL Server 2012 CLR Data Types (Version 11.x) - required by entity framework
 - Install URL Rewrite 2.0 - required for handler mapping
 - Enable read/write for app pool user for \Temp folders
 - Configure web.config

### Contributing

Please contribute in any way you can:
  - Improve the data (anyone can do this):
    - Submit comments, checkins, and photos via the website
    - Submit new data, become an editor for your country
  - Grow the user base
    - Advocacy, tell people about [Open Charge Map](https://openchargemap.org) and help them use it.
  - Improve the system:
    - Help translate the system into other languages using the [webtranslateit](https://webtranslateit.com/en/projects/6978-Open-Charge-Map) project 
    - Help write documentation
    - Web App (HTML/CSS/JavaScript)
    - Website development (MVC)
    - Map widget for embedding
    - Sample Code for developers
    - Graphic Design
    - Testing


	### Linux build
	- Install dotnet core 3.x sdk for your system (~350MB), check with `dotnet --version`
		- `sudo snap install dotnet-sdk --channel=3.1/stable --classic`
	- Install latest monogdb for your system, set service to run on startup
	`git clone https://github.com/openchargemap/ocm-system`
	
	- `cd ocm-system/API/OCM.Net/OCM.API.Web`
	- `dotnet build`

	To run API server on port 5000 bound to default public network interface:
	- Debug: `dotnet run --urls http://0.0.0.0:5000`
    - Release: `dotnet run -c Release --urls http://0.0.0.0:5000`
    
    To deploy service worker as systemd managed service:
    
    - Build release: 
        - `cd :~/ocm-system/API/OCM.Net/OCM.API.Worker`
        - `dotnet publish -c Release`
    - Copy ocm-api.service file to /etc/systemd/system/ocm-api.service
    - Create symlink in /usr/sbin for the build
        `sudo ln -s ~/ocm-system/API/OCM.Net/OCM.API.Worker/bin/Release/netcoreapp3.1/publish/OCM.API.Worker /usr/sbin/ocm-api`


