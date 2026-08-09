using Microsoft.AspNetCore.Components.Forms;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.ServerValidation;

namespace Norse.AuthN.Components.Tests.ServerValidation;

public sealed class EditContextServerErrorsTests
{
	static (EditContext Context, FakeModel Model) NewContext()
	{
		FakeModel model = new();
		return (new(model), model);
	}

	[Fact]
	void Applies_field_errors_against_the_named_field()
	{
		var (context, model) = NewContext();

		context.ApplyServerErrors(new()
		{
			Category = ErrorCategory.Validation,
			Errors = new Dictionary<string, string[]> { [nameof(FakeModel.Email)] = ["Taken."] }
		});

		context.GetValidationMessages(new FieldIdentifier(model, nameof(FakeModel.Email))).ShouldBe(["Taken."]);
	}

	[Fact]
	void Applies_empty_key_errors_at_model_level()
	{
		var (context, model) = NewContext();

		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."));

		context.GetValidationMessages(new FieldIdentifier(model, string.Empty))
			.ShouldBe(["Invalid email or password."]);
	}

	[Fact]
	void Renders_category_display_when_the_dictionary_is_empty()
	{
		var (context, model) = NewContext();

		context.ApplyServerErrors(new() { Category = ErrorCategory.Forbidden });

		context.GetValidationMessages(new FieldIdentifier(model, string.Empty)).ShouldHaveSingleItem();
	}

	[Fact]
	void Fault_display_carries_the_correlation_id()
	{
		var (context, model) = NewContext();
		Guid correlationId = new("11111111-2222-3333-4444-555555555555");

		context.ApplyServerErrors(new() { Category = ErrorCategory.Fault, CorrelationId = correlationId });

		context.GetValidationMessages(new FieldIdentifier(model, string.Empty))
			.ShouldHaveSingleItem()
			.ShouldContain(correlationId.ToString());
	}

	[Fact]
	void Editing_a_field_clears_only_that_fields_server_messages()
	{
		var (context, model) = NewContext();
		context.ApplyServerErrors(new()
		{
			Category = ErrorCategory.Validation,
			Errors = new Dictionary<string, string[]>
			{
				[nameof(FakeModel.Email)] = ["Taken."],
				[string.Empty] = ["Also broken."]
			}
		});

		context.NotifyFieldChanged(new FieldIdentifier(model, nameof(FakeModel.Email)));

		context.GetValidationMessages(new FieldIdentifier(model, nameof(FakeModel.Email))).ShouldBeEmpty();
		context.GetValidationMessages(new FieldIdentifier(model, string.Empty)).ShouldBe(["Also broken."]);
	}

	[Fact]
	void A_fresh_validation_pass_clears_all_server_messages()
	{
		var (context, _) = NewContext();
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."));

		context.Validate()
			.ShouldBeTrue(); // raises OnValidationRequested → coordinator clears → no store blocks validity
	}

	[Fact]
	void The_validation_request_clear_raises_its_own_state_change_notification()
	{
		var (context, _) = NewContext();
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."));
		var notified = false;
		context.OnValidationStateChanged += (_, _) => notified = true;

		context.Validate();

		notified.ShouldBeTrue(); // no other validator exists in this test — the coordinator itself must notify
	}

	[Fact]
	void Reapply_replaces_rather_than_accumulates()
	{
		var (context, model) = NewContext();
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "First."));

		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Second."));

		context.GetValidationMessages(new FieldIdentifier(model, string.Empty)).ShouldBe(["Second."]);
	}

	[Fact]
	void Clear_removes_every_server_message()
	{
		var (context, model) = NewContext();
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Invalid email or password."));

		context.ClearServerErrors();

		context.GetValidationMessages(new FieldIdentifier(model, string.Empty)).ShouldBeEmpty();
	}

	[Fact]
	void The_coordinator_is_created_once_and_cached()
	{
		var (context, _) = NewContext();

		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "First."));
		context.ApplyServerErrors(Problem.ModelError(ErrorCategory.InvalidCredentials, "Second."));

		context.Properties.TryGetValue(EditContextServerErrorsExtensions.CoordinatorKey, out var coordinator)
			.ShouldBeTrue();
		coordinator.ShouldBeOfType<ServerErrorCoordinator>();
	}

	sealed record FakeModel
	{
		public string Email { get; set; } = "";
	}
}
