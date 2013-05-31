# Building App From Source

The following tools are required to perform a multi-platform build from a single App source tree:

- *Nodejs* (http://nodejs.org/)
- *Jake* (build tool): npm install -g jake
- *npm install* the following dependencies:
	- *npm install wrench*
	- *npm install uglify-js*
	- *npm install moment*

To perform the standard web app build run:

- *jake* (this reads jakefile.js script to perform build steps)
- updates should change the releaseVersion in jakefile.js
