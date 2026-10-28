using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;

namespace Norse.AuthN.Components.FluentUI.Tests;

// Every form here reads RendererInfo (Disabled="!CanSubmit", and the base's submit gate), and bUnit
// throws MissingRendererInfoException until one is set. It cannot be set in a constructor: setting it
// creates the renderer, which locks the service provider before the test registers its services. So
// it is applied on render, through bUnit's virtual Render overloads (the parameter-builder overload
// routes through the fragment one). Interactive is the state a form is in once Blazor boots; a
// static-render test derives from BunitContext directly, since this base would overwrite its choice.
public abstract class InteractiveBunitContext : BunitContext
{
	public override IRenderedComponent<TComponent> Render<TComponent>(RenderFragment renderFragment)
	{
		SetRendererInfo(new("Server", true));
		return base.Render<TComponent>(renderFragment);
	}

	public override IRenderedComponent<ContainerFragment> Render(RenderFragment renderFragment)
	{
		SetRendererInfo(new("Server", true));
		return base.Render(renderFragment);
	}
}
