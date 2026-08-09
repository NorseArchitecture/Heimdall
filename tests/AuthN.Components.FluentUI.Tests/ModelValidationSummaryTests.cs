using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Contracts;
using Norse.Abstractions.Components.ServerValidation;

namespace Norse.AuthN.Components.FluentUI.Tests;

public sealed class ModelValidationSummaryTests : BunitContext
{
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
			context.ApplyServerErrors(
				Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password.")));
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

	[Fact]
	async Task Resubscribes_to_a_replaced_cascaded_EditContext_without_remounting()
	{
		FakeModel modelA = new();
		EditContext contextA = new(modelA);
		// CascadingEditContextHost cascades EditContext through a plain component parameter,
		// so the test can swap it on the live host instance without remounting the child —
		// bUnit's re-render extension explicitly forbids supplying a new cascading value directly.
		var host = Render<CascadingEditContextHost>(parameters => parameters.Add(p => p.Context, contextA));
		var component = host.FindComponent<ModelValidationSummary>();
		ValidationMessageStore storeA = new(contextA);
		storeA.Add(new FieldIdentifier(modelA, string.Empty), "From A.");
		await component.InvokeAsync(contextA.NotifyValidationStateChanged);
		component.Markup.ShouldContain("From A.");

		// Same component instance, new cascaded EditContext — no remount, no Dispose.
		FakeModel modelB = new();
		EditContext contextB = new(modelB);
		host.Render(parameters => parameters.Add(p => p.Context, contextB));

		ValidationMessageStore storeB = new(contextB);
		storeB.Add(new FieldIdentifier(modelB, string.Empty), "From B.");
		await component.InvokeAsync(contextB.NotifyValidationStateChanged);

		component.Markup.ShouldContain("From B.");
		component.Markup.ShouldNotContain("From A.");

		// The subscription must have genuinely moved — A's own notifications no longer do anything.
		storeA.Add(new FieldIdentifier(modelA, string.Empty), "From A again.");
		await component.InvokeAsync(contextA.NotifyValidationStateChanged);

		component.Markup.ShouldNotContain("From A again.");
		component.Markup.ShouldContain("From B.");
	}

	sealed record FakeModel;
}
