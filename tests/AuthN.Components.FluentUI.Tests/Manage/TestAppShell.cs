using Norse.Abstractions.Components.Primitives;

namespace Norse.AuthN.Components.FluentUI.Tests.Manage;

sealed class TestAppShell : IAppShellLayout
{
	public Type LayoutType => typeof(TestShellLayout);
}
