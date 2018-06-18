Open Charge Map
==========

[Open Charge Map](http://openchargemap.org) is the global public registry of electric vehicle charging locations. 

The aim of the project is to crowd source a high quality, well maintained public Open Data set with the greatest breadth possible. 

OCM was first established in 2011 and freely provides data to drivers via hundreds of apps and sites, as well to researchers, EV market analysts and government policy makers.

Access to the data set is provided via our [API](http://openchargemap.org/site/develop/). Developers can use this to build their own [apps](http://openchargemap.org/site/develop/apps/).

The code in this repository represents the backend systems ([API](http://openchargemap.org/site/develop/), [Web Site](http://openchargemap.org) and server-side Import Processing) for the project.

Server-side code is developed mostly in C#, currently building under Visual Studio 2017 Community Edition with .Net 4.5. Data is stored in SQL Server with an additional caching layer using MongoDB.

The [app](https://map.openchargemap.io) source can be found in it's own repo at https://github.com/openchargemap/ocm-app

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
