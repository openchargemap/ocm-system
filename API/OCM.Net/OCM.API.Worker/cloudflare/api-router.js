/**
 * This cloudflare worker performs the following functions:
 * - rejects banned clients
 * - enforce presence of API Key 
 * - responds to read queries using mirror api hosts (if available, tracking unavailable hosts)
 * - sends writes to master API
 * - logs request summary to logs.openchargemap.org for debug purposes (rejections, API abuse etc)
 * */

const enableAPIKeyRules = true;
const enableLogging = true;
const enablePrimaryForReads = true;
const requireAPIKeyForAllRequests = false;
const logTimeoutMS = 2000;

addEventListener('fetch', event => {

    // check API key
    const apiKey = getAPIKey(event.request);
    let status = "OK";

    const clientIP = event.request.headers.get('cf-connecting-ip');
    const userAgent = event.request.headers.get('user-agent');

    if (userAgent && userAgent.indexOf("FME/2020") > -1) {
        event.respondWith(rejectRequest(event, "Blocked for API Abuse. Callers spamming API with repeated duplicate calls may be auto banned."));
        return;
    }

    if (apiKey == null && userAgent && userAgent.indexOf("python-requests") > -1) {
        event.respondWith(rejectRequest(event, "Generic user agents must use an API Key. API Keys are mandatory and this rule will be enforced soon."));
        return;
    }

    if (enableAPIKeyRules) {
        let maxresults = getParameterByName(event.request.url, "maxresults");

        if (apiKey == null && requireAPIKeyForAllRequests == true && event.request.method != "OPTIONS") {
            status = "REJECTED_APIKEY_MISSING";
            event.respondWith(rejectRequest(event));

        }
        else if (apiKey == null && maxresults != null && parseInt(maxresults) > 250) {
            status = "REJECTED_APIKEY_MISSING";
            event.respondWith(rejectRequest(event));

        } else {
            console.log("Passing request with API Key or key not required:" + apiKey);

            //respond
            event.respondWith(fetchAndApply(event, apiKey, status));
        }
    } else {
        //respond
        event.respondWith(fetchAndApply(event, apiKey, status));
    }

    //log
    if (enableLogging) {
        event.waitUntil(attemptLog(event, apiKey, status));
    }

});

async function attemptLog(event, apiKey, status) {
    // attempt to log but timeout after 1000 ms if no response from log
    // Initiate the fetch but don't await it yet, just keep the promise.
    let logPromise = logRequest(event.request, apiKey, status)

    // Create a promise that resolves to `undefined` after 10 seconds.
    let timeoutPromise = new Promise(resolve => setTimeout(resolve, logTimeoutMS))

    // Wait for whichever promise completes first.
    return Promise.race([logPromise, timeoutPromise])

}

async function rejectRequest(event, reason) {
    if (!reason || reason == null) {
        reason = "You must specify an API Key, either in an X-API-Key header or key= query string parameter.";
    }
    console.log(reason);

    return new Response(reason, {
        status: 403,
    });
}

async function fetchAndApply(event, apiKey, status) {

    let banned_ua = await OCM_CONFIG_KV.get("API_BANNED_UA", "json");
    let banned_ip = await OCM_CONFIG_KV.get("API_BANNED_IP", "json");
    let banned_keys = await OCM_CONFIG_KV.get("API_BANNED_KEYS", "json");

    const clientIP = event.request.headers.get('cf-connecting-ip');
    const userAgent = event.request.headers.get('user-agent');

    let abuseMsg = "Blocked for API Abuse. Callers spamming API with repeated duplicate calls may be auto banned.";

    if (banned_ua.includes(userAgent)) {
        status = "BANNED_UA";
        return rejectRequest(event, abuseMsg)
    }

    if (banned_ip.includes(clientIP)) {
        status = "BANNED_IP";
        return rejectRequest(event, abuseMsg)
    }

    if (banned_keys.includes(apiKey)) {
        status = "BANNED_KEY";
        return rejectRequest(event, abuseMsg)
    }

    let mirrorHosts = [
        "api-01.openchargemap.io"
    ];

    // check if we think this user is currently an editor (has posted)

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
        !url.href.includes("/map") &&
        !url.href.includes("/.well-known")
    ) {

        let mirrorIndex = getRandomInt(0, mirrorHosts.length);

        // force query to first mirror if there is one available;
        if (!enablePrimaryForReads && mirrorIndex == 0 && mirrorHosts.length > 1) {
            mirrorIndex = 1;
        }

        if (mirrorIndex > 0) {
            console.log("Using mirror API " + mirrorIndex + " for request. " + mirrorHosts[mirrorIndex]);
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
                if (!response.ok && response.status >= 500) {
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
                    await OCM_CONFIG_KV.put("API_SKIPPED_MIRRORS", JSON.stringify([mirrorHosts[mirrorIndex]]), { expirationTtl: 60 * 15 });
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

function getParameterByName(url, name) {
    name = name.replace(/[\[\]]/g, '\\$&')
    name = name.replace(/\//g, '')
    var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)'),
        results = regex.exec(url)

    if (!results) return null
    else if (!results[2]) return ''
    else if (results[2]) {
        results[2] = results[2].replace(/\//g, '')
    }

    return decodeURIComponent(results[2].replace(/\+/g, ' '));
}

function getAPIKey(request) {

    let apiKey = getParameterByName(request.url, "key");

    console.log("API Key From URL:" + apiKey);

    if (apiKey == null || apiKey == '') {
        apiKey = request.headers.get('X-API-Key');

        console.log("API Key From Uppercase header:" + apiKey);

    }

    if (apiKey == null || apiKey == '') {
        apiKey = request.headers.get('x-api-key');

        console.log("API Key From Lowercase header:" + apiKey);
    }


    if (apiKey == '') apiKey = null;
    return apiKey;
}

async function logRequest(request, apiKey, status) {
    // log the url of the request and the API key used

    var ray = request.headers.get('cf-ray') || '';
    var id = ray.slice(0, -4);
    var data = {
        'timestamp': Date.now(),
        'url': request.url,
        'referer': request.referrer,
        'method': request.method,
        'ray': ray,
        'ip': request.headers.get('cf-connecting-ip') || '',
        'host': request.headers.get('host') || '',
        'ua': request.headers.get('user-agent') || '',
        'cc': request.headers.get('Cf-Ipcountry') || '',
        'ocm_key': apiKey,
        'status': status
    };

    var url = `http://log.openchargemap.org/?status=${data.status}&key=${data.ocm_key}&ua=${data.ua}&ip=${data.ip}&url=${encodeURI(request.url)}`;

    try {
        await fetch(url, {
            method: 'PUT',
            body: JSON.stringify(data),
            headers: new Headers({
                'Content-Type': 'application/json',
            })
        });

    } catch (exp) {
        console.log("Failed to log request");
    }
}