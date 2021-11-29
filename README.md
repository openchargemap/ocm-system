Open Charge Map (OCM)
==========

### About the project

[Open Charge Map](https://openchargemap.org) is the global public registry of electric vehicle charging locations.
OCM was first established in 2011 and aims to crowdsource a high quality, well maintained open data set with the greatest breadth possible. Our data set is a mixture of manually entered information and imported open data sources (such as government-run registries and charging networks with an open data license). OCM provides data to drivers (via hundreds of apps and sites), as well to researchers, EV market analysts, and government policy makers. 

The code in this repository represents the backend systems ([API](https://openchargemap.org/site/develop/), [Web Site](https://openchargemap.org) and server-side import processing) for the project. Server-side code is developed mostly in C#, currently building under Visual Studio 2019 Community Edition with .Net Core 3.1, Data is primarily stored in SQL Server, using Entity Framework Core, with an additional caching layer using MongoDB. Most API reads are services by the MongoDB cache.

Developers can use our [API](https://openchargemap.org/site/develop/) to access the data set and build their own [apps](https://openchargemap.org/site/develop/apps/). The map [app](https://map.openchargemap.io) source (using latest Ionic/Angular/TypeScript) can be found in its own repo at https://github.com/openchargemap/ocm-app


### Basic build prerequisites

- dotnet 6.x sdk (windows/linux)

### Deployment 

 - Configure MongoDB as services and initialise ocm_mirror database
 - Enable read/write for app pool user for \Temp folders
 - Configure web.config

 To run an API mirror, see the OCM.API.Worker readme.

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


