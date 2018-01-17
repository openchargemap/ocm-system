Open Charge Map
==========

[Open Charge Map](http://openchargemap.org) is the global public registry of electric vehicle charging locations. 

The aim of the project is to move away from the need to maintain silos of privately held charging equipment location data and instead crowd source a high quality, well maintained public Open Data set with the greatest breadth possible. Access to the data set is provided via a web [API](http://openchargemap.org/site/develop/) and developers have access to the API to in order to build their own [apps](http://openchargemap.org/site/develop/apps/).

The code in this repository represents the backend systems ([API](http://openchargemap.org/site/develop/), [Web Site](http://openchargemap.org) and server-side Import Processing) for the project. Also included is the source for the [mobile/web app](http://openchargemap.org/app/) and example client code in various programming languages.

Server-side code is developed mostly in C#, currently building under Visual Studio 2015 Community Edition with .Net 4.5.

The [Web/Mobile app](https://openchargemap.org/app/) is built purely with HTML/CSS/JavaScript and requires the NodeJS based Jake build system (see build readme) to generated the minified version of the app. This is gradually being replaced by an Ionic (Angular/TypeScript) based app https://map.openchargemap.io the source for which is at https://github.com/openchargemap/ocm-app

Contributing
-----------
Please contribute in any way you can:
  - Improve the data:
    - Submit comments, checkins and photos via the web site
    - Submit new data, become an editor for your country
  - Improve the system:
    - Help translate the system into other languages using the [webtranslateit](https://webtranslateit.com/en/projects/6978-Open-Charge-Map) project 
    - Help write documentation
    - Web App (HTML/CSS/JavaScript)
    - Website development (MVC5)
    - Map widget for embedding
    - Sample Code for developers
    - Graphic Design
    - Testing
  - Grow the user base
    - Advocacy, tell people about [Open Charge Map](http://openchargemap.org) and help them use it.
  - Get involved: [Discussion Forum](http://openchargemap.org/forum/)
