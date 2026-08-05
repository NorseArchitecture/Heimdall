// Materializes a client-side JSON download — the conventional shape for a small, isolated
// JS-interop helper in a Razor class library's own wwwroot (imported via the standard
// "./_content/{AssemblyName}/downloadFile.js" module path). Used by PersonalData.razor to save the
// GetMyPersonalDataAsync response without any server-side download endpoint (spec: personal-data
// download is a gRPC call, not a form-POST). JSON.stringify runs here, client-side, deliberately —
// NORSE070 bans System.Text.Json calls from Heimdall's own C#; the edge (this script) owns the bytes.
export function downloadJson(fileName, payload) {
	const anchor = document.createElement("a");
	anchor.href = `data:application/json;charset=utf-8,${encodeURIComponent(JSON.stringify(payload))}`;
	anchor.download = fileName;
	anchor.click();
}
