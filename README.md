Open Charge Map
==========

[Open Charge Map](http://openchargemap.org) is the global public registry of electric vehicle charging locations. 

The aim of the project is to crowd source a high quality, well maintained public Open Data set with the greatest breadth possible. Our data set is a mixture of manually entered information and imported open data sources (such as government run registries or charging networks with an open data license).

OCM was first established in 2011 and freely provides data to drivers via hundreds of apps and sites, as well to researchers, EV market analysts and government policy makers.

Access to the data set is provided via our [API](http://openchargemap.org/site/develop/). Developers can use this to build their own [apps](http://openchargemap.org/site/develop/apps/).

The code in this repository represents the backend systems ([API](http://openchargemap.org/site/develop/), [Web Site](http://openchargemap.org) and server-side Import Processing) for the project.

Server-side code is developed mostly in C#, currently building under Visual Studio 2017 Community Edition with .Net 4.5. Data is stored in SQL Server with an additional caching layer using MongoDB.

The [app](https://map.openchargemap.io) source can be found in it's own repo at https://github.com/openchargemap/ocm-app


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


OCM Data Model V3
------------------

The following changes are part of the V3 data model:

ChargePoint has been renamed Site
ChargingDevice is a new item to optionally parent ConnectionInfo into a single device (multiple per site)
MetadataValue can now be optionally associated with a specific ChargingDevice as well as a Site.

Charging Device may support one or more vehicle types - defaults to Car)
Network Operator is now identified at ChargingDevice level

Metadata Values


Contributing
-----------
Please contribute in any way you can:
  - Improve the data (anyone can do this):
    - Submit comments, checkins and photos via the web site
    - Submit new data, become an editor for your country
  - Grow the user base
    - Advocacy, tell people about [Open Charge Map](https://openchargemap.org) and help them use it.
  - Get involved: [Discussion Forum](https://plus.google.com/u/0/communities/112113799071360649945)
  - Improve the system:
    - Help translate the system into other languages using the [webtranslateit](https://webtranslateit.com/en/projects/6978-Open-Charge-Map) project 
    - Help write documentation
    - Web App (HTML/CSS/JavaScript)
    - Website development (MVC)
    - Map widget for embedding
    - Sample Code for developers
    - Graphic Design
    - Testing
