using System.Reflection;
using InnerTube.Renderers;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public static class RendererManager
{
	public static Dictionary<string, Type> Renderers = null!;

	internal static void LoadRenderers()
	{
		if (Renderers is not null) return;

		Renderers = new Dictionary<string, Type>();

		Type[] renderers = Assembly
			.GetAssembly(typeof(RendererManager))!
			.GetTypes()
			.Where(x => x.IsAssignableTo(typeof(IRenderer)))
			.ToArray();
		
		foreach (Type renderer in renderers)
			Renderers.Add(char.ToLower(renderer.Name[0]) + renderer.Name[1..], renderer);
	}

	public static IRenderer? ParseRenderer(JToken? renderer, string type)
	{
		try
		{
			if (renderer is null)
				return null;

			if (Renderers.TryGetValue(type, out Type? rendererType))
				return (IRenderer)Activator.CreateInstance(rendererType, renderer)!;
			return new UnknownRenderer(renderer, type);
		}
		catch (Exception e)
		{
			return new ExceptionRenderer(e, type);
		}
	}
	
	public static IEnumerable<IRenderer> ParseRenderers(JArray? rendererArray)
	{
		if (rendererArray is null)
			return Array.Empty<IRenderer>();

		return from renderer
				in rendererArray
			let type = renderer.First?.Path.Split(".").Last()!
			select ParseRenderer(renderer[type], type);
	}
}