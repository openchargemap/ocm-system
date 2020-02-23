const mirrorHosts = [
    "api-01.openchargemap.io",
    "api-mirror-01.openchargemap.io"
];

addEventListener('fetch', event => {
    event.respondWith(fetchAndApply(event))
});

async function fetchAndApply(event) {

    let enableCache = false;
    // redirect request to backend api
    let url = new URL(event.request.url);

    console.log(url.href);
    console.log(event.request.method);

    if (
      event.request.method != "POST" && 
      !url.href.includes("/geocode") && 
      !url.href.includes("/.well-known")
      ) {

        console.log("Using mirror API for request.");

        // if not doing a POST request reads can be served randomly from mirrors or from cache
        //url.hostname = mirrorHosts[getRandomInt(mirrorHosts.length)];
        url.hostname = mirrorHosts[1];

        if (url.hostname != mirrorHosts[0]) {
            url.protocol = "http";
            url.port = "80";
        }

        // get cached response if available, otherwise fetch new response and cache it
        let cache = caches.default;
        let response = null;

        if (enableCache) response = await cache.match(event.request);

        if (!response) {
            let modifiedRequest = new Request(url, {
                method: event.request.method,
                headers: event.request.headers
            });

            try {
                response = await fetch(modifiedRequest);

                // Make the headers mutable by re-constructing the Response.

                response = new Response(response.body, response);
                response.headers.set('x-forwarded-host', url.hostname);
                if (response.headers.get('Access-Control-Allow-Origin') === null) {
                    response.headers.append("Access-Control-Allow-Origin", "*");
                }

                if (enableCache) event.waitUntil(cache.put(event.request, response.clone()));

                return response;

            } catch {
                // failed to fetch from mirror, try primary
                console.log("Forwarded request failed. Retrying to primary API");

                response = await fetch(event.request);

                if (enableCache)  event.waitUntil(cache.put(event.request, response.clone()));
                
                return response;
            }

        } else {
            console.log("Using cached response");
            return response;
        }

    } else {

        // POSTs (and certain GETs) only go to primary API
        
        console.log("Using primary API for request. " + url.origin);
     
        response = await fetch(event.request);
        return response
    }
}

function getRandomInt(max) {
    return Math.floor(Math.random() * max);
}