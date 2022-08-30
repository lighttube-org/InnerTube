using System.Text.Json;

namespace InnerTube;

public class InnerTubeSearchAutocomplete
{
	public string Query { get; }
	public List<string> Autocomplete { get; }

	public InnerTubeSearchAutocomplete(string jsonpResult)
	{
		int firstParantheses = jsonpResult.IndexOf('(') + 1;
		string jsonString = jsonpResult.Substring(jsonpResult.IndexOf('(') + 1, jsonpResult.Length - firstParantheses - 1);
		JsonElement[] list = JsonSerializer.Deserialize<JsonElement[]>(jsonString)!;
		
		Query = list[0].ToString();
		Autocomplete = new List<string>();

		string autocompleteListJson = list[1].ToString();
		JsonElement[] autocompleteList = JsonSerializer.Deserialize<JsonElement[]>(autocompleteListJson)!;
		foreach (JsonElement.ArrayEnumerator obj in autocompleteList.Select(x => x.EnumerateArray())) {
			obj.MoveNext();
			Autocomplete.Add(obj.Current.ToString());
		}
	}
}