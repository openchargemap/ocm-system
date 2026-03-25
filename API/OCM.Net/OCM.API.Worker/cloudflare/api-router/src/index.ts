type RemoteKeyResult = {
	appId?: string;
	title?: string;
};

class OCMRouter {
	private readonly primaryApiHost = "api-01.openchargemap.io";
	private readonly logTimeoutMS = 2000;

	private readonly corsHeaders: Record<string, string> = {
		"Access-Control-Allow-Methods": "GET,HEAD,POST,OPTIONS,DELETE",
		"Access-Control-Max-Age": "86400"
	};

	async fetch(request: Request, env: Env, context: ExecutionContext): Promise<Response> {
		if (request.method === "OPTIONS") {
			return this.handleOptions(request);
		}

		const requestUrl = new URL(request.url);

		if (requestUrl.pathname.endsWith("/robots.txt")) {
			const response = new Response("user-agent: * \r\ndisallow: /", {
				headers: {
					"content-type": "text/plain"
				}
			});
			return this.finalizeResponse(request, env, context, response, null, "ROBOTS", false);
		}

		if (requestUrl.pathname.includes("/.well-known/")) {
			try {
				const upstream = await this.proxyToPrimary(request);
				const rewritten = this.rewriteResponseHeaders(request, upstream, this.primaryApiHost);
				return this.finalizeResponse(
					request,
					env,
					context,
					rewritten,
					null,
					`WELLKNOWN_${upstream.status}`,
					false
				);
			} catch {
				const response = new Response("Upstream API unavailable", { status: 502 });
				return this.finalizeResponse(
					request,
					env,
					context,
					response,
					null,
					"WELLKNOWN_UPSTREAM_ERROR",
					false
				);
			}
		}

		if (this.isBlockedUserAgent(request)) {
			const response = this.rejectRequest(
				"Blocked for API abuse. Repeated duplicate calls may be automatically banned."
			);
			return this.finalizeResponse(
				request,
				env,
				context,
				response,
				null,
				"REJECTED_SPAM_UA",
				false
			);
		}

		const rawApiKey = this.getAPIKey(request);
		if (!rawApiKey) {
			const response = this.rejectRequest(
				"You must specify an API key using the key query parameter or x-api-key header."
			);
			return this.finalizeResponse(
				request,
				env,
				context,
				response,
				null,
				"REJECTED_APIKEY_MISSING",
				false
			);
		}

		const apiKey = rawApiKey.toLowerCase();
		const isValidKey = await this.isAPIKeyValid(env, apiKey);
		if (!isValidKey) {
			const response = this.rejectRequest("Invalid API key.");
			return this.finalizeResponse(
				request,
				env,
				context,
				response,
				apiKey,
				"REJECTED_APIKEY_INVALID",
				false
			);
		}

		try {
			const upstream = await this.proxyToPrimary(request);
			const rewritten = this.rewriteResponseHeaders(request, upstream, this.primaryApiHost);
			rewritten.headers.set("x-forwarded-host", this.primaryApiHost);
			return this.finalizeResponse(
				request,
				env,
				context,
				rewritten,
				apiKey,
				`OK_${upstream.status}`,
				true
			);
		} catch {
			const response = new Response("Upstream API unavailable", { status: 502 });
			return this.finalizeResponse(
				request,
				env,
				context,
				response,
				apiKey,
				"UPSTREAM_ERROR",
				true
			);
		}
	}

	private appendVaryHeader(headers: Headers, value: string): void {
		const existing = headers.get("Vary");
		if (!existing) {
			headers.set("Vary", value);
			return;
		}

		const varyValues = existing.split(",").map((entry) => entry.trim().toLowerCase());
		if (!varyValues.includes(value.toLowerCase())) {
			headers.set("Vary", `${existing}, ${value}`);
		}
	}

	private applyCORSHeaders(request: Request, response: Response): Response {
		const origin = request.headers.get("Origin");
		const corsResponse = new Response(response.body, response);

		if (origin) {
			corsResponse.headers.set("Access-Control-Allow-Origin", origin);
			corsResponse.headers.set("Access-Control-Allow-Credentials", "true");
			this.appendVaryHeader(corsResponse.headers, "Origin");
		} else {
			corsResponse.headers.set("Access-Control-Allow-Origin", "*");
			corsResponse.headers.delete("Access-Control-Allow-Credentials");
		}

		return corsResponse;
	}

	private handleOptions(request: Request): Response {
		if (
			request.headers.get("Origin") !== null &&
			request.headers.get("Access-Control-Request-Method") !== null
		) {
			const responseHeaders = new Headers({
				...this.corsHeaders,
				"Access-Control-Allow-Origin": request.headers.get("Origin") ?? "*",
				"Access-Control-Allow-Credentials": "true"
			});

			this.appendVaryHeader(responseHeaders, "Origin");
			const requestHeaders = request.headers.get("Access-Control-Request-Headers");
			if (requestHeaders) {
				responseHeaders.set("Access-Control-Allow-Headers", requestHeaders);
			}

			return new Response(null, { headers: responseHeaders });
		}

		return new Response(null, {
			headers: {
				Allow: "GET, HEAD, POST, OPTIONS"
			}
		});
	}

	private buildProxyRequest(request: Request, url: URL): Request {
		return new Request(url.toString(), {
			method: request.method,
			headers: request.headers,
			body: request.method === "GET" || request.method === "HEAD" ? undefined : request.body,
			redirect: "manual"
		});
	}

	private rewriteResponseHeaders(request: Request, response: Response, upstreamHost: string): Response {
		const rewrittenResponse = new Response(response.body, response);
		const location = rewrittenResponse.headers.get("Location");

		if (!location) {
			return rewrittenResponse;
		}

		try {
			const requestUrl = new URL(request.url);
			const locationUrl = new URL(location, request.url);
			if (locationUrl.hostname === upstreamHost) {
				locationUrl.hostname = requestUrl.hostname;
				locationUrl.protocol = requestUrl.protocol;
				locationUrl.port = requestUrl.port;
				rewrittenResponse.headers.set("Location", locationUrl.toString());
			}
		} catch {
			// Keep upstream location if parsing fails.
		}

		return rewrittenResponse;
	}

	private async proxyToPrimary(request: Request): Promise<Response> {
		const url = new URL(request.url);
		url.hostname = this.primaryApiHost;
		url.protocol = "https:";
		url.port = "";

		return fetch(this.buildProxyRequest(request, url));
	}

	private isBlockedUserAgent(request: Request): boolean {
		const userAgent = request.headers.get("user-agent");
		if (!userAgent) {
			return false;
		}

		return userAgent.includes("FME/2020");
	}

	private getAPIKey(request: Request): string | null {
		const url = new URL(request.url);
		const fromQuery = url.searchParams.get("key");
		const fromHeader = request.headers.get("x-api-key");
		const apiKey = (fromQuery ?? fromHeader ?? "").trim();

		if (!apiKey || apiKey === "test" || apiKey === "null") {
			return null;
		}

		return apiKey;
	}

	private rejectRequest(reason: string): Response {
		return new Response(reason, { status: 403 });
	}

	private async finalizeResponse(
		request: Request,
		env: Env,
		context: ExecutionContext,
		response: Response,
		apiKey: string | null,
		status: string,
		trackUsage: boolean
	): Promise<Response> {
		// context.waitUntil(this.attemptLog(request, apiKey, status));
		if (trackUsage && apiKey) {
			context.waitUntil(this.updateUsageStats(env, apiKey));
		}

		return this.applyCORSHeaders(request, response);
	}

	private async isAPIKeyValid(env: Env, apiKey: string): Promise<boolean> {
		if (apiKey === "statuscake") {
			return true;
		}

		const keyRegex = /^(?:\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\}{0,1})$/;
		if (!keyRegex.test(apiKey)) {
			return false;
		}

		try {
			const cached = await env.OCM_API_STATS.prepare(
				"SELECT apikey FROM apikey WHERE apikey = ?1 LIMIT 1"
			)
				.bind(apiKey)
				.all<{ apikey: string }>();

			if (cached.results.length > 0) {
				return true;
			}

			const keyLookupUrl = `https://${this.primaryApiHost}/v3/key?key=${encodeURIComponent(apiKey)}`;
			const response = await fetch(keyLookupUrl);
			if (!response.ok) {
				return false;
			}

			const keyData = (await response.json()) as RemoteKeyResult;
			if (!keyData?.appId) {
				return false;
			}

			await env.OCM_API_STATS
				.prepare("INSERT INTO apikey (apikey, title, datemodified) VALUES (?1, ?2, ?3)")
				.bind(apiKey, keyData.title ?? "", new Date().toISOString())
				.run();

			return true;
		} catch (error: unknown) {
			console.log(`API key validation fallback allow: ${this.formatError(error)}`);
			return true;
		}
	}

	private async updateUsageStats(env: Env, apiKey: string): Promise<void> {
		const trimmedKey = apiKey.trim();
		if (!trimmedKey) {
			return;
		}

		const now = new Date();
		const keyDate = `${trimmedKey}_${now.getUTCFullYear()}${String(now.getUTCMonth() + 1).padStart(2, "0")}`;

		try {
			const updateResult = await env.OCM_API_STATS.prepare(
				"UPDATE apiusage SET usage = usage + 1 WHERE keydate = ?1"
			)
				.bind(keyDate)
				.run();

			if ((updateResult.meta?.changes ?? 0) === 0) {
				await env.OCM_API_STATS.prepare("INSERT INTO apiusage (keydate, usage) VALUES (?1, 1)")
					.bind(keyDate)
					.run();
			}
		} catch (error: unknown) {
			console.log(`Failed to update API usage stats: ${this.formatError(error)}`);
		}
	}

	private async attemptLog(request: Request, apiKey: string | null, status: string): Promise<void> {
		const payload = {
			timestamp: Date.now(),
			url: request.url,
			referer: request.headers.get("Referer"),
			method: request.method,
			ray: request.headers.get("cf-ray") ?? "",
			ip: request.headers.get("cf-connecting-ip") ?? "",
			host: request.headers.get("host") ?? "",
			ua: request.headers.get("user-agent") ?? "",
			cc: request.headers.get("cf-ipcountry") ?? "",
			ocm_key: apiKey,
			status
		};

		const logUrl = new URL("http://log.openchargemap.org/");
		logUrl.searchParams.set("status", status);
		logUrl.searchParams.set("key", apiKey ?? "");
		logUrl.searchParams.set("ua", payload.ua);
		logUrl.searchParams.set("ip", payload.ip);
		logUrl.searchParams.set("url", request.url);

		const controller = new AbortController();
		const timeoutId = setTimeout(() => controller.abort(), this.logTimeoutMS);

		try {
			await fetch(logUrl.toString(), {
				method: "PUT",
				body: JSON.stringify(payload),
				headers: {
					"Content-Type": "application/json"
				},
				signal: controller.signal
			});
		} catch (error: unknown) {
			console.log(`Failed to log request: ${this.formatError(error)}`);
		} finally {
			clearTimeout(timeoutId);
		}
	}

	private formatError(error: unknown): string {
		if (error instanceof Error) {
			return error.message;
		}

		return String(error);
	}
}

export default {
	async fetch(request: Request, env: Env, context: ExecutionContext): Promise<Response> {
		return new OCMRouter().fetch(request, env, context);
	}
};
