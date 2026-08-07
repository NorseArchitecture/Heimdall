using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.ServerValidation;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ModelValidationSummaryTests : BunitContext
{
	sealed record FakeModel;

	public ModelValidationSummaryTests()
	{
		Services.AddFluentUIComponents();
		// FluentUI components make JS interop calls bunit has no way to know about in advance —
		// loose mode is bunit's own documented answer (same rationale as LoginTests/PersonalDataTests).
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	IRenderedComponent<ModelValidationSummary> Render(EditContext editContext) =>
		Render<ModelValidationSummary>(parameters =>
			parameters.AddCascadingValue(editContext));

	[Fact]
	void Renders_nothing_when_no_model_level_messages_exist()
	{
		EditContext context = new(new FakeModel());

		var component = Render(context);

		component.Markup.ShouldBeEmpty();
	}

	[Fact]
	async Task Renders_model_level_messages_and_ignores_field_messages()
	{
		FakeModel model = new();
		EditContext context = new(model);
		ValidationMessageStore store = new(context);
		store.Add(new FieldIdentifier(model, "Email"), "Field-scoped.");
		store.Add(new FieldIdentifier(model, string.Empty), "Model-scoped.");

		var component = Render(context);
		// bUnit's TestRenderer enforces the same dispatcher-affinity real Blazor Server/WASM hosts
		// give for free — raising the EditContext event straight from the test thread (as production
		// code never would; it always runs from within the renderer's own synchronization context)
		// trips Dispatcher.AssertAccess. InvokeAsync marshals onto the component's dispatcher.
		await component.InvokeAsync(context.NotifyValidationStateChanged);

		component.Markup.ShouldContain("Model-scoped.");
		component.Markup.ShouldNotContain("Field-scoped.");
	}

	[Fact]
	async Task Rerenders_when_validation_state_changes()
	{
		FakeModel model = new();
		EditContext context = new(model);
		var component = Render(context);
		ValidationMessageStore store = new(context);

		store.Add(new FieldIdentifier(model, string.Empty), "Appeared.");
		await component.InvokeAsync(context.NotifyValidationStateChanged);

		component.Markup.ShouldContain("Appeared.");
	}

	[Fact]
	async Task A_rendered_model_message_disappears_on_a_fresh_validation_request_with_no_other_validator_present()
	{
		FakeModel model = new();
		EditContext context = new(model);
		var component = Render(context);
		await component.InvokeAsync(() =>
			context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password.")));
		component.Markup.ShouldContain("Invalid email or password.");

		// no Blazilla in this form — the coordinator's own notification must drive the re-render
		await component.InvokeAsync(context.Validate);

		component.Markup.ShouldNotContain("Invalid email or password.");
	}

	[Fact]
	void Unsubscribes_on_dispose()
	{
		FakeModel model = new();
		EditContext context = new(model);
		var component = Render(context);

		component.Instance.Dispose();
		ValidationMessageStore store = new(context);
		store.Add(new FieldIdentifier(model, string.Empty), "After dispose.");
		context.NotifyValidationStateChanged();

		component.Markup.ShouldNotContain("After dispose.");
	}
}
