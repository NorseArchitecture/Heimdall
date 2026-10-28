// The browser half of a passkey ceremony, imported through PasskeyModule (the one C# door to this file).
// It never fetches: the page hands it the server's options JSON and hands the server the credential JSON
// it answers. JSON.parse/JSON.stringify run here, client-side, deliberately — NORSE070 bans
// System.Text.Json calls from Heimdall's own C#, so the edge (this script) owns the bytes. Every ceremony
// answers { credentialJson, error }: a credential, an error line to render, or both null for an abort.

const dismissed = "No passkey was provided by the authenticator.";

export function isSupported() {
	return typeof navigator.credentials !== "undefined" &&
		typeof window.PublicKeyCredential?.parseCreationOptionsFromJSON === "function" &&
		typeof window.PublicKeyCredential?.parseRequestOptionsFromJSON === "function";
}

export function create(optionsJson) {
	return ceremony(() => navigator.credentials.create({
		publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(JSON.parse(optionsJson)),
	}));
}

export function request(optionsJson, mediation) {
	return ceremony(() => navigator.credentials.get({
		publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(JSON.parse(optionsJson)),
		mediation: mediation ?? undefined,
	}));
}

async function ceremony(obtain) {
	try {
		const credential = await obtain();
		return credential
			? { credentialJson: JSON.stringify(credential), error: null }
			: { credentialJson: null, error: dismissed };
	} catch (error) {
		if (error?.name === "AbortError") {
			// The ceremony was canceled: nothing to show, nothing to send.
			return { credentialJson: null, error: null };
		}
		// NotAllowedError is the user's dismissal (or a timeout), not a fault. Anything else always carries a
		// line: a rejection that is not an Error (no message) must never read as a silent abort.
		return { credentialJson: null, error: error?.name === "NotAllowedError" ? dismissed : error?.message || String(error) };
	}
}
