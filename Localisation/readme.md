OCM Localisation Support
------------------------------

Language translations for OCM Apps and Website components are provided via language specific resource files.

To assist with translation you can either supply updates to these files or use the translation UI at https://webtranslateit.com/en/projects/6978-Open-Charge-Map


- Build Notes:
	- use 'wti pull' in /Localisation/src/ to refresh .json language files
	- use 'grunt' in /Localisation/ to build js files, language pack etc and to copy build output to other projects (app, website, map widget etc)