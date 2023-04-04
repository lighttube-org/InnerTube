﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InnerTube;

public class Format
{
	public string Itag { get; }
	public int Bitrate { get; }
	public long? ContentLength { get; }
	public int? Fps { get; }
	public int? Height { get; }
	public int? Width { get; }
	public DashRange? InitRange { get; }
	public DashRange? IndexRange { get; }
	public string MimeType { get; }
	public Uri Url { get; }
	public string Quality { get; }
	public string? QualityLabel { get; }
	public string? AudioQuality { get; }
	public int? AudioSampleRate { get; }
	public int? AudioChannels { get; }
	public AudioTrack? AudioTrack { get; }

	public Format(JToken jToken)
	{
		Itag = jToken["itag"]!.ToString();
		Bitrate = jToken["bitrate"]?.ToObject<int>() ?? jToken["averageBitrate"]?.ToObject<int>() ?? 0;
		ContentLength = jToken["contentLength"]?.ToObject<long>();
		Fps = jToken["fps"]?.ToObject<int>();
		Height = jToken["height"]?.ToObject<int>();
		Width = jToken["width"]?.ToObject<int>();
		InitRange = new DashRange(jToken["initRange"]!);
		IndexRange = new DashRange(jToken["indexRange"]!);
		MimeType = jToken["mimeType"]!.ToString();
		Url = new Uri(jToken["url"]!.ToString());
		Quality = jToken["quality"]!.ToString();
		QualityLabel = jToken["qualityLabel"]?.ToString();
		AudioQuality = jToken["audioQuality"]?.ToString();
		AudioSampleRate = int.Parse(jToken["audioSampleRate"]?.ToString() ?? "0");
		AudioChannels = jToken["audioChannels"]?.ToObject<int>();
		AudioTrack = jToken["audioTrack"]?.ToObject<AudioTrack>();
	}
}

public class AudioTrack
{
	[JsonProperty("displayName")] public string DisplayName { get; set; }
	[JsonProperty("id")] public string Id { get; set; }
	[JsonProperty("audioIsDefault")] public bool AudioIsDefault { get; set; }

	public override string ToString() => $"[{Id}] {DisplayName}{(AudioIsDefault ?" (default)" : "")}";
}

public class DashRange
{
	public int Start { get; }
	public int End { get; }

	public override string ToString() => $"({Start} - {End})";

	public DashRange(JToken? jToken)
	{
		if (jToken is null)
			return;
		Start = int.Parse(jToken["start"]!.ToObject<string>()!);
		End = int.Parse(jToken["end"]!.ToObject<string>()!);
	}
}