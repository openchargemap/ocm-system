Open Charge Map - Mobile and Web App
=======

This is the Open Charge Map web application for general use on Mobile and Desktop browsers or packaging as Mobile app. The live version of this app is available at [http://openchargemap.org/app/](http://openchargemap.org/app/)

The app is HTML/CSS/JavaScript (TypeScript) and makes use of various third party libraries and frameworks.

The production build of the app is built using the Jake build system under NodeJS. The mobile version is distributed via mobile app stores and is the standard build wrapped in a default PhoneGap/Apache Cordova wrapper. The app can easily be adapted to produce Network/Operator specific versions which are filtered to specific branded charging networks etc.

# Build

To build, create the folder ./build/output/ then run *jake* from app root folder.

The following tools are required to perform a multi-platform build (web & cordova):

- *Nodejs* (http://nodejs.org/)
- *npm install* to install the build dependencies

To perform the standard web app build run:

- *jake* (this reads jakefile.js script to perform build steps)
- updates should change the releaseVersion in jakefile.js

To test run the following and open a web browser to http://localhost:8080 :
- *http-server build/output/web* 

To generate docs: 
- *jake generate-docs*
