/**
 * This cloudflare worker performs the following functions:
 * - rejects banned clients
 * - enforce presence of API Key 
 * - responds to read queries using mirror api hosts (if available, tracking unavailable hosts)
 * - sends writes to master API
 * - logs request summary to logs.openchargemap.org for debug purposes (rejections, API abuse etc)
 * */

class OCMRouter {
	enableAPIKeyRules = true;
	enableMirrorChecks = false;
	enableDebug = false;
	enableLogging = true; // if true, API requests are logged to central log API
	enablePrimaryForReads = true; // if true, primary API server is skipped for reads
	requireAPIKeyForAllRequests = true;
	logTimeoutMS = 2000;

	corsHeaders = {

		"Access-Control-Allow-Methods": "GET,HEAD,POST,OPTIONS",
		"Access-Control-Max-Age": "86400",
	}

	handleOptions(request: Request) {
		// Make sure the necessary headers are present
		// for this to be a valid pre-flight request
		let headers = request.headers
		if (
			headers.get("Origin") !== null &&
			headers.get("Access-Control-Request-Method") !== null &&
			headers.get("Access-Control-Request-Headers") !== null
		) {
			// Handle CORS pre-flight request.
			// If you want to check or reject the requested method + headers
			// you can do that here.
			let respHeaders = new Headers({
				...this.corsHeaders,
				"Access-Control-Allow-Origin": headers.get("Origin") ?? "*",
				"Access-Control-Allow-Credentials": "true",

			});
			if (request.headers.get("Access-Control-Request-Headers")) {
				// Allow all future content Request headers to go back to browser
				// such as Authorization (Bearer) or X-Client-Name-Version
				respHeaders.append("Access-Control-Allow-Headers", "" + request.headers.get("Access-Control-Request-Headers"));

			}
			return new Response(null, {
				headers: respHeaders,
			});
		}
		else {
			// Handle standard OPTIONS request.
			// If you want to allow other HTTP Methods, you can do that here.
			return new Response(null, {
				headers: {
					Allow: "GET, HEAD, POST, OPTIONS",
				},
			})
		}
	}

	async getConfigKVText(env: Env, key: string) {
		return await env.OCM_CONFIG_KV.get(key);
	}

	async getConfigKVJson(env: Env, key: string) {
		return await env.OCM_CONFIG_KV.get(key, "json");
	}

	async fetch(request: Request, env: Env, context: ExecutionContext) {

		// handle basic OPTION queries     
		if (request.method == "OPTIONS") {

			// Handle CORS preflight requests
			return this.handleOptions(request);
		}


		// disallow robots
		if (request.url.toString().indexOf("/robots.txt") > -1) {
			return new Response("user-agent: * \r\ndisallow: /", {
				headers: {
					"content-type": "text/plain"
				}
			});

		}

		// pass through .well-known for acme http validation on main server

		if (request.url.toString().indexOf("/.well-known/") > -1) {

			let url = new URL(request.url);
			url.hostname = "api-01.openchargemap.io";

			console.log("Redirected to well known " + url);
			let modifiedRequest = new Request(url, {
				method: request.method,
				headers: request.headers
			});

			return fetch(modifiedRequest);
		}

		// check for banned IPs or banned User Agents
		const clientIP = request.headers.get('cf-connecting-ip');
		const userAgent = request.headers.get('user-agent');

		if (userAgent && userAgent.indexOf("FME/2020") > -1) {
			return await this.rejectRequest("Blocked for API Abuse. Callers spamming API with repeated duplicate calls may be auto banned.");
		}

		// check API key
		const apiKey = this.getAPIKey(request);

		let status = "OK";

		let response: Response;
		let logKeyUsage = true;

		if (this.enableAPIKeyRules) {
			let maxresults = this.getParameterByName(request.url, "maxresults");

			if (apiKey == null && this.requireAPIKeyForAllRequests == true && request.method != "OPTIONS") {
				status = "REJECTED_APIKEY_MISSING";
				logKeyUsage = false;
				response = this.rejectRequest(status);
			}
			else if (apiKey == null && maxresults != null && parseInt(maxresults) > 250) {
				status = "REJECTED_APIKEY_MISSING";
				logKeyUsage = false;
				response = this.rejectRequest(status);
			} else {
				if (this.enableDebug) console.log("Passing request with API Key or key not required:" + apiKey);

				if (await this.isAPIKeyValid(env, apiKey)) {
					//respond
					response = await this.fetchAndApply(request, env, context, apiKey, status);
				} else {
					status = "REJECTED_APIKEY_INVALID";
					logKeyUsage = false;
					response = this.rejectRequest(status);
				}
			}

		} else {
			//respond
			response = await this.fetchAndApply(request, env, context, apiKey, status);
		}

		// none-blocking log
		if (this.enableLogging && logKeyUsage) {
			context.waitUntil(this.attemptLog(request, apiKey, status));
			if (apiKey) {
				try {
					context.waitUntil(this.updateUsageStats(env, apiKey));
				} catch (e) {
					console.log(e);
				}
			}
		}
		return response;

	}

	async isAPIKeyValid(env: Env, apiKey: string | null) {
		if (apiKey == null) return false;

		if (apiKey == 'statuscake') {
			return true;
		}

		if (this.enableLogging) console.debug("Checking key:" + apiKey);

		if (! /^(?:\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\}{0,1})$/.test(apiKey)) {
			console.debug("Failed regex:" + apiKey);
			return false;
		}

		let query: D1Result;
		try {
			const stmt = env.OCM_API_STATS.prepare('SELECT * FROM apikey WHERE apikey = ?1').bind(apiKey);
			query = await stmt.all();

			if (query.results && query.results.length > 0 == true) {
				if (this.enableLogging) console.debug("Found key in d1 db:" + apiKey);
				return true;
			}
			else {
				// key not found in our cache, check our API
				let url = new URL("https://api-01.openchargemap.io/v3/key?key=" + apiKey);
				let response = await fetch(url);
				if (response.ok) {
					let data: any = await response.json();
					// {"appId":"fa3cacf4-4773-4ecc-bee4-0cddf5e91e93","title":"Open Charge Ma","url":"https://opencollective.com/openchargemap"}
					if (data && data.appId) {

						if (this.enableLogging) console.debug("Got key from API, storing in DB:" + apiKey);
						// add key to cache
						await env.OCM_API_STATS
							.prepare('INSERT INTO apikey (apikey,title,datemodified) VALUES (?1,?2,?3)')
							.bind(apiKey.toLowerCase(), data.title, new Date().toISOString())
							.run();

						return true;
					}
				}

				return false;
			}
		} catch (e: any) {
			console.log({
				message: e.message,
				cause: e.cause.message,
			});

			// if db query fails, allow key
			return true;
		}
	}

	async updateUsageStats(env: Env, key: string) {
		if (key.trim().length == 0) return;

		let date = new Date();

		let keydate = `${key.trim().toLowerCase()}_${date.getFullYear()}${(date.getMonth() + 1).toString().padStart(2, '0')}`;

		let query: D1Result;
		try {
			const stmt = env.OCM_API_STATS.prepare('SELECT * FROM apiusage WHERE keydate = ?1').bind(keydate);
			query = await stmt.all();

			if (query.results && query.results.length > 0 == true) {
				//update existing
				await env.OCM_API_STATS
					.prepare('UPDATE apiusage SET usage = usage+1 WHERE keydate=?1')
					.bind(keydate)
					.run();
			}
			else {
				//add new
				await env.OCM_API_STATS
					.prepare('INSERT INTO apiusage (keydate, usage) VALUES (?1, 1)')
					.bind(keydate)
					.run();
			}
		} catch (e: any) {
			console.log({
				message: e.message,
				cause: e.cause.message,
			});
		}
	}

	async attemptLog(request: Request, apiKey: string | null, status: string) {

		// attempt to log but timeout after 1000 ms if no response from log
		// Initiate the fetch but don't await it yet, just keep the promise.
		let logPromise = this.logRequest(request, apiKey, status)

		// Create a promise that resolves to `undefined` after 10 seconds.
		let timeoutPromise = new Promise(resolve => setTimeout(resolve, this.logTimeoutMS))

		// Wait for whichever promise completes first.
		return Promise.race([logPromise, timeoutPromise])

	}

	rejectRequest(reason: string) {
		if (!reason || reason == null) {
			reason = "You must specify an API Key, either in an X-API-Key header or key= query string parameter.";
		}

		return new Response(reason, {
			status: 403,
		});
	}

	async fetchAndApply(request: Request, env: Env, context: ExecutionContext, apiKey: string | null, status: string) {

		let banned_ua: string[] = []; //await OCM_CONFIG_KV.get("API_BANNED_UA", "json");
		let banned_ip: string[] = []; //await OCM_CONFIG_KV.get("API_BANNED_IP", "json");
		let banned_keys: string[] = <string[]>await this.getConfigKVJson(env, "API_BANNED_KEYS");

		const clientIP = request.headers.get('cf-connecting-ip');
		const userAgent = request.headers.get('user-agent');

		let abuseMsg = "API call blocked. Callers with repeated duplicate calls may be auto banned and you must ensure you are using a real API key.";

		if (userAgent != null && banned_ua.includes(userAgent)) {
			status = "BANNED_UA";
			return this.rejectRequest(abuseMsg)
		}

		if (clientIP != null && banned_ip?.includes(clientIP)) {
			status = "BANNED_IP";
			return this.rejectRequest(abuseMsg)
		}

		if (apiKey != null && banned_keys?.includes(apiKey)) {
			status = "BANNED_KEY";
			return this.rejectRequest(abuseMsg)
		}

		let mirrorHosts = [
			"api-01.openchargemap.io"
		];

		// check if we think this user is currently an editor (has posted)

		let ip_key = clientIP != null ? "API_EDITOR_" + clientIP.replace(".", "_") : null;
		let isEditor = false;
		if (ip_key != null) {
			if (await this.getConfigKVText(env, ip_key) != null) {
				isEditor = true;
			}
		}

		// get list of mirrors from KV store and append to our working list
		if (this.enableMirrorChecks) {
			let kv_mirrors: string[] | null = <string[]>await this.getConfigKVJson(env, "API_MIRRORS");
			if (kv_mirrors) {
				mirrorHosts.push(...kv_mirrors);
			}

			let kv_skipped_mirrors: string[] | null = <string[]>await this.getConfigKVJson(env, "API_SKIPPED_MIRRORS");

			// remove any mirrors temporarily not in use.
			if (kv_skipped_mirrors != null && kv_mirrors && kv_mirrors?.length > 1) {
				if (this.enableDebug) console.log("Skipped Mirrors:");
				if (this.enableDebug) console.log(kv_skipped_mirrors);
				mirrorHosts = mirrorHosts.filter(k => !kv_skipped_mirrors?.includes(k) == true)
			}
		}

		if (this.enableDebug) console.log("Viable mirrors:");
		if (this.enableDebug) console.log(mirrorHosts);

		////////////////

		let enableCache = false;
		// redirect request to backend api
		let url = new URL(request.url);

		if (
			request.method != "POST" &&
			!url.href.includes("/geocode") &&
			!url.href.includes("/map") &&
			!url.href.includes("/openapi") &&
			!url.href.includes("/.well-known")
		) {

			let mirrorIndex = this.getRandomInt(0, mirrorHosts.length);

			// force query to first mirror if there is one available;
			if (!this.enablePrimaryForReads && mirrorIndex == 0 && mirrorHosts.length > 1) {
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

			if (enableCache) response = await cache.match(request);

			if (!response) {
				let modifiedRequest = new Request(url, {
					method: request.method,
					headers: request.headers
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
					if (response.headers.get('Access-Control-Allow-Credentials') === null) {
						response.headers.append("Access-Control-Allow-Credentials", "true");
					}
					if (enableCache) context.waitUntil(cache.put(request, response.clone()));

					return response;

				} catch {
					// failed to fetch from mirror, try primary
					console.log("Forwarded request failed. Retrying to primary API");

					// add failed mirror to skipped mirrors
					if (mirrorIndex != 0) {
						await env.OCM_CONFIG_KV.put("API_SKIPPED_MIRRORS", JSON.stringify([mirrorHosts[mirrorIndex]]), { expirationTtl: 60 * 15 });
					}

					response = await fetch(request);

					if (enableCache) context.waitUntil(cache.put(request, response.clone()));

					return response;
				}

			} else {
				console.log("Using cached response");
				return response;
			}

		} else {

			// POSTs (and certain GETs) only go to primary API

			if (this.enableDebug) console.log("Using primary API for request. " + url.origin);

			if (request.method == "POST") {
				if (ip_key != null) {
					await env.OCM_CONFIG_KV.put(ip_key, "true", { expirationTtl: 60 * 60 * 12 });
					if (this.enableDebug) console.log("Logged user as an editor:" + ip_key);
				}
			}

			let response = await fetch(request);
			return response
		}
	}

	getRandomInt(min: number, max: number) {
		min = Math.ceil(min);
		max = Math.floor(max);
		return Math.floor(Math.random() * (max - min)) + min; //The maximum is exclusive and the minimum is inclusive
	}

	getParameterByName(url: string, name: string) {
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

	getAPIKey(request: Request) {

		let apiKey = this.getParameterByName(request.url, "key");

		//console.log("API Key From URL:" + apiKey);

		if (apiKey == null || apiKey == '') {
			apiKey = request.headers.get('X-API-Key');

			if (this.enableDebug) console.log("API Key From Uppercase header:" + apiKey);

		}

		if (apiKey == null || apiKey == '') {
			apiKey = request.headers.get('x-api-key');

			if (this.enableDebug) console.log("API Key From Lowercase header:" + apiKey);
		}


		if (apiKey == '' || apiKey == 'test' || apiKey == 'null') apiKey = null;
		return apiKey;
	}

	async logRequest(request: Request, apiKey: string | null, status: string) {
		// log the url of the request and the API key used

		var ray = request.headers.get('cf-ray') || '';
		var id = ray.slice(0, -4);
		var data = {
			'timestamp': Date.now(),
			'url': request.url,
			'referer': request.headers.get('Referer'),
			'method': request.method,
			'ray': ray,
			'ip': request.headers.get('cf-connecting-ip') || '',
			'host': request.headers.get('host') || '',
			'ua': request.headers.get('user-agent') || '',
			'cc': request.headers.get('cf-ipcountry') || '',
			'ocm_key': apiKey,
			'status': status
		};

		var url = `http://log.openchargemap.org/?status=${data.status}&key=${data.ocm_key}&ua=${data.ua}&ip=${data.ip}&url=${encodeURI(request.url)}`;

		//console.log("Logging Request "+url+" ::"+JSON.stringify(data));

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
}

export default {
	async fetch(
		request: Request,
		env: Env,
		ctx: ExecutionContext
	): Promise<Response> {
		return new OCMRouter().fetch(request, env, ctx);
	},
};