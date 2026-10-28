using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

public sealed class RenamePasskeyTests : InteractiveBunitContext
{
	readonly IPasskeyService _passkeys = Substitute.For<IPasskeyService>();

	public RenamePasskeyTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_passkeys)
			.AddScoped<IValidator<RenamePasskeyRequest>, RenamePasskeyRequestValidator>();
	}

	[Fact]
	void The_current_name_is_loaded_and_prefilled()
	{
		_passkeys.GetPasskey(Arg.Any<GetPasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<PasskeyEntry>.Ok(new() { CredentialId = "a", Name = "Laptop", RenameUrl = "/Account/Manage/RenamePasskey/a" }));

		var page = Render<RenamePasskey>(p => p.Add(r => r.Id, "a"));

		_passkeys.Received(1).GetPasskey(Arg.Is<GetPasskeyRequest>(r => r.CredentialId == "a"), Arg.Any<CancellationToken>());
		var form = page.FindComponent<RenamePasskeyForm>();
		form.Instance.CredentialId.ShouldBe("a");
		form.Instance.CurrentName.ShouldBe("Laptop");
		page.Find("fluent-text-input").GetAttribute("value").ShouldBe("Laptop");
	}

	[Fact]
	void An_unknown_id_renders_not_found_and_no_form()
	{
		const string NotFound = "The passkey was not found.";
		_passkeys.GetPasskey(Arg.Any<GetPasskeyRequest>(), Arg.Any<CancellationToken>())
			.Returns(new Outcome<PasskeyEntry>(new Failed(Problem.ModelError(ErrorCategory.NotFound, NotFound))));

		var page = Render<RenamePasskey>(p => p.Add(r => r.Id, "missing"));

		page.Find("[role=alert]").TextContent.Trim().ShouldBe(NotFound);
		page.FindComponents<RenamePasskeyForm>().ShouldBeEmpty();
		page.FindAll("form").ShouldBeEmpty();
	}
}
