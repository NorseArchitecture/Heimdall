using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Norse.Abstractions.Components;
using Norse.Abstractions.Contracts;
using Norse.AuthN.Components.FluentUI.Manage;
using Norse.AuthN.Services;

namespace Norse.AuthN.Components.FluentUI.Tests;

// Derives from BunitContext, not InteractiveBunitContext: that base would overwrite the static
// renderer these facts are about. A prerendered form a user can reach before Blazor boots (or for
// good, if the interactive runtime never starts) was never bound from the post, so its submit would
// dispatch the initial model. Every form offers no submit, and a submit that arrives anyway refuses.
public sealed class FormStaticRenderLockTests : BunitContext
{
	readonly IAccountService _account = Substitute.For<IAccountService>();

	public FormStaticRenderLockTests()
	{
		Services.AddFluentUIComponents();
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services
			.AddSingleton(_account)
			.AddSingleton(Substitute.For<IAuthenticationService>())
			.AddSingleton(Substitute.For<ITwoFactorService>())
			.AddSingleton(Substitute.For<IPasskeyService>())
			.AddSingleton(Substitute.For<ISessionTransition>())
			.AddSingleton(SignInMethods());
	}

	// Login renders SignInMethodLinks, which loads on render, static or not.
	static ISignInMethodsService SignInMethods()
	{
		var service = Substitute.For<ISignInMethodsService>();
		service.GetSignInMethods(Arg.Any<GetSignInMethodsRequest>(), Arg.Any<CancellationToken>())
			.Returns(Outcome<SignInMethodsResponse>.Ok(new()));
		return service;
	}

	public static TheoryData<Type> Forms =>
	[
		typeof(Login),
		typeof(Register),
		typeof(ForgotPassword),
		typeof(ResendEmailConfirmation),
		typeof(ResetPasswordForm),
		typeof(TwoFactorForm),
		typeof(RecoveryCodeForm),
		typeof(ProfileForm),
		typeof(ChangeEmailForm),
		typeof(ChangePasswordForm),
		typeof(SetPasswordForm),
		typeof(DeletePersonalDataForm),
		typeof(VerifyAuthenticatorForm),
		typeof(RenamePasskeyForm),
	];

	// The theory is only as good as its list: a new form that never joins it is never locked.
	[Fact]
	void The_lock_lists_every_form_in_the_assembly()
	{
		var forms = typeof(GateLayout).Assembly.GetTypes()
			.Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(OutcomeFormComponentBase)))
			.ToHashSet();

		forms.ShouldBe(Forms.Select(row => row.Data).ToHashSet(), ignoreOrder: true);
	}

	[Theory]
	[MemberData(nameof(Forms))]
	void A_statically_rendered_form_offers_no_submit(Type form)
	{
		SetRendererInfo(new("Static", false));

		var page = Render(builder =>
		{
			builder.OpenComponent(0, form);
			// The two challenge forms render only an expiry notice without one.
			if (form.GetProperty(nameof(TwoFactorForm.Challenge)) is not null)
				builder.AddAttribute(1, nameof(TwoFactorForm.Challenge), "challenge");
			builder.CloseComponent();
		});

		page.Find("fluent-button[type='submit']").HasAttribute("disabled").ShouldBeTrue(form.Name);
	}

	[Fact]
	async Task A_statically_rendered_submit_never_calls_the_service_and_asks_the_user_to_retry()
	{
		SetRendererInfo(new("Static", false));
		var form = Render<ProfileForm>(p => p.Add(f => f.Email, "user@example.com").Add(f => f.PhoneNumber, "+15551234567"));

		await form.Find("form").SubmitAsync();

		await _account.DidNotReceiveWithAnyArgs().SetPhoneNumber(default!, Xunit.TestContext.Current.CancellationToken);
		form.Find(".norse-model-errors").TextContent.Trim().ShouldBe("This page hasn't finished loading — try again.");
	}
}
