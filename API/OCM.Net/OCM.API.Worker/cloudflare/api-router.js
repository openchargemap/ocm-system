

addEventListener('fetch', event => {
    event.respondWith(fetchAndApply(event))
});

async function fetchAndApply(event) {

    let mirrorHosts = [
        "api-01.openchargemap.io"
    ];

    // check if we think this user is currently an editor (has posted)
    let clientIP = event.request.headers.get('CF-Connecting-IP');
    let ip_key = clientIP != null ? "API_EDITOR_" + clientIP.replace(".", "_") : null;
    let isEditor = false;
    if (ip_key != null) {
        if (await OCM_CONFIG_KV.get(ip_key) != null) {
            isEditor = true;
        }
    }

    // get list of mirrors from KV store and append to our working list
    let kv_mirrors = await OCM_CONFIG_KV.get("API_MIRRORS", "json");
    mirrorHosts.push(...kv_mirrors);

    let kv_skipped_mirrors = await OCM_CONFIG_KV.get("API_SKIPPED_MIRRORS", "json");

    // remove any mirrors temporarily not in use.
    if (kv_skipped_mirrors != null) {
        console.log("Skipped Mirrors:");
        console.log(kv_skipped_mirrors);
        mirrorHosts = mirrorHosts.filter(k => !kv_skipped_mirrors.includes(k))
    }

    console.log("Viable mirrors:");
    console.log(mirrorHosts);

    ////////////////

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

        let mirrorIndex = getRandomInt(0, mirrorHosts.length);

        if (mirrorIndex > 0) {
            console.log("Using mirror API " + mirrorIndex + " for request.");
        }
        // if not doing a POST request reads can be served randomly from mirrors or from cache
        url.hostname = mirrorHosts[mirrorIndex];

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
                if (!response.ok) {
                    console.log("Forwarded request failed. " + response.status);
                    throw "Mirror response status failed:" + response.status;
                }

                response.headers.set('x-forwarded-host', url.hostname);
                if (response.headers.get('Access-Control-Allow-Origin') === null) {
                    response.headers.append("Access-Control-Allow-Origin", "*");
                }

                if (enableCache) event.waitUntil(cache.put(event.request, response.clone()));

                return response;

            } catch {
                // failed to fetch from mirror, try primary
                console.log("Forwarded request failed. Retrying to primary API");

                // add failed mirror to skipped mirrors
                if (mirrorIndex != 0) {
                    await OCM_CONFIG_KV.put("API_SKIPPED_MIRRORS", JSON.stringify([mirrorHosts[mirrorIndex]]), { expirationTtl: 60 * 60 });
                }

                response = await fetch(event.request);

                if (enableCache) event.waitUntil(cache.put(event.request, response.clone()));

                return response;
            }

        } else {
            console.log("Using cached response");
            return response;
        }

    } else {

        // POSTs (and certain GETs) only go to primary API

        console.log("Using primary API for request. " + url.origin);

        if (event.request.method == "POST") {
            if (ip_key != null) {
                await OCM_CONFIG_KV.put(ip_key, "true", { expirationTtl: 60 * 60 * 12 });
                console.log("Logged user as an editor:" + ip_key);
            }
        }

        response = await fetch(event.request);
        return response
    }
}

function getRandomInt(min, max) {
    min = Math.ceil(min);
    max = Math.floor(max);
    return Math.floor(Math.random() * (max - min)) + min; //The maximum is exclusive and the minimum is inclusive
}