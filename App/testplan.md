App Test Plan
------------------------

- Open App. Splashscreen should appear then app menu should show.
- Select Search. Choose Search Near Me. App should show search progress and then display search results.
- Select Search Result. App should show POI details.
- Select Menu Icon to return to last view. Choose Map. Map should render showing markers for all search results.
- Select a POI from the map. POI Detail should show as normal.
- Select Add Comment. App should prompt user to Sign In.
- Choose Sign In from main menu. Once completed should return to app Signed in as normal user.
- Navigate to POI details. Choose Add Comment. Enter comment details and choose Submit.
- Navigate to POI details. Choose Add Photo. Enter photo details and choose Submit.
- Refresh Search results. New comments/photos should appear in POI details.
- Edit a location
- Add a new location
- Add / Remove a favourite.

General Testing Notes
------------------------
- On iOS there is no back button, so all actions must be navigable.
- When testing on iOS and Android, links to external sites (google maps etc) must open in a new window.