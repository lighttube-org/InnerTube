using Google.Protobuf;
using Google.Protobuf.Collections;
using InnerTube.Exceptions;
using InnerTube.Models;
using InnerTube.Parsers;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;

namespace InnerTube;

public class SimpleInnerTubeClient
{
	public readonly InnerTube InnerTube;

	public SimpleInnerTubeClient(InnerTubeConfiguration? config = null)
	{
		InnerTube = new InnerTube(config);
		ValueParser.Init();
	}

	public async Task<InnerTubePlayer> GetVideoPlayerAsync(string videoId, bool contentCheckOk, string language = "en",
		string region = "US")
	{
		// in the worst case scenario, this will do 4 http requests :3
		try
		{
			PlayerResponse player = await InnerTube.GetPlayerAsync(videoId, contentCheckOk, false, language, region);
			return new InnerTubePlayer(player, false, language);
		}
		catch (PlayerException e)
		{
			if (e.Code != PlayabilityStatus.Types.Status.LiveStreamOffline) throw;

			PlayerResponse player = await InnerTube.GetPlayerAsync(videoId, contentCheckOk, true, language, region);
			return new InnerTubePlayer(player, true, language);
		}
	}

	public async Task<InnerTubeVideo> GetVideoDetailsAsync(string videoId, bool contentCheckOk, string? playlistId,
		int? playlistIndex, string? playlistParams, string language = "en", string region = "US")
	{
		NextResponse next = await InnerTube.GetNextAsync(videoId, contentCheckOk, true, playlistId, playlistIndex,
			playlistParams, language, region);
		return new InnerTubeVideo(next, language);
	}

	public async Task<ContinuationResponse> ContinueVideoRecommendationsAsync(string continuationKey,
		string language = "en", string region = "US")
	{
		NextResponse next = await InnerTube.ContinueNextAsync(continuationKey, language, region);
		RepeatedField<RendererWrapper> allItems = next.OnResponseReceivedEndpoints[0].AppendContinuationItemsAction
			.ContinuationItems;
		IEnumerable<RendererWrapper> items = allItems.Where(x =>
			x.RendererCase != RendererWrapper.RendererOneofCase.ContinuationItemRenderer);
		ContinuationItemRenderer? continuation = allItems.LastOrDefault(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)?.ContinuationItemRenderer;
		return new ContinuationResponse
		{
			ContinuationToken = continuation?.ContinuationEndpoint.ContinuationCommand.Token,
			Results = Utils.ConvertRenderers(items, language)
		};
	}

	// doesn't take language/region because comments don't have anything to localize server side
	public async Task<ContinuationResponse> GetVideoCommentsAsync(string videoId,
		CommentsContext.Types.SortOrder sortOrder) =>
		await ContinueVideoCommentsAsync(Utils.PackCommentsContinuation(videoId, sortOrder));

	public async Task<ContinuationResponse> ContinueVideoCommentsAsync(string continuationToken)
	{
		NextResponse next = await InnerTube.ContinueNextAsync(continuationToken);
		RendererWrapper[]? continuationItems =
			next.OnResponseReceivedEndpoints.SelectMany(x => x.ReloadContinuationItemsCommand?.ContinuationItems ?? [])
				.Concat(next.OnResponseReceivedEndpoints.SelectMany(x => x.AppendContinuationItemsAction?.ContinuationItems ?? []))
				.Where(x => x.RendererCase is RendererWrapper.RendererOneofCase.CommentThreadRenderer
					or RendererWrapper.RendererOneofCase.CommentViewModel
					or RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
				.ToArray();
		if (continuationItems.Length == 0)
			return new ContinuationResponse
			{
				ContinuationToken = null,
				Results = []
			};
		if (continuationItems[0].CommentThreadRenderer?.Comment != null)
		{
			// CommentRenderer instead of ViewModels
			return new ContinuationResponse
			{
				ContinuationToken = continuationItems
					.LastOrDefault(x => x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
					?.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token,
				Results = continuationItems
					.Where(x => x.RendererCase == RendererWrapper.RendererOneofCase.CommentThreadRenderer)
					.Select(x => new RendererContainer
					{
						Type = "comment",
						OriginalType = "commentThreadRenderer",
						Data = new CommentRendererData(x.CommentThreadRenderer, "en")
					}).ToArray()
			};
		}

		// ViewModels <3
		Dictionary<string, Payload> mutations =
			next.FrameworkUpdates.EntityBatchUpdate.Mutations.ToDictionary(x => x.EntityKey, x => x.Payload);
		ContinuationItemRenderer? continuationItemRenderer = continuationItems
			.LastOrDefault(x => x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
			?.ContinuationItemRenderer;
		return new ContinuationResponse
		{
			ContinuationToken = continuationItemRenderer?.ContinuationEndpoint?.ContinuationCommand.Token ?? 
			                    continuationItemRenderer?.Button?.ButtonViewModel.Command?.ContinuationCommand?.Token,
			Results = continuationItems
				.Where(x => x.RendererCase is RendererWrapper.RendererOneofCase.CommentThreadRenderer
					or RendererWrapper.RendererOneofCase.CommentViewModel)
				.Select(x =>
				{
					switch (x.RendererCase)
					{
						case RendererWrapper.RendererOneofCase.CommentThreadRenderer:
						{
							return new RendererContainer
							{
								Type = "comment",
								OriginalType = "commentThreadRenderer",
								Data = new CommentRendererData(
									x.CommentThreadRenderer,
									mutations[x.CommentThreadRenderer.CommentViewModel.CommentViewModel.CommentKey]
										.CommentEntityPayload,
									mutations[x.CommentThreadRenderer.CommentViewModel.CommentViewModel.ToolbarStateKey]
										.EngagementToolbarStateEntityPayload)
							};
						}
						case RendererWrapper.RendererOneofCase.CommentViewModel:
						{
							return new RendererContainer
							{
								Type = "comment",
								OriginalType = "commentThreadRenderer",
								Data = new CommentRendererData(
									null,
									mutations[x.CommentViewModel.CommentKey].CommentEntityPayload,
									mutations[x.CommentViewModel.ToolbarStateKey].EngagementToolbarStateEntityPayload)
							};
						}
						default:
							return new RendererContainer
							{
								Type = "exception",
								OriginalType = "unexpectedRenderer",
								Data = new ExceptionRendererData
								{
									Message = "Unexpected renderer in comments response",
									RendererCase = x.RendererCase.ToString()
								}
							};
					}
				}).ToArray()
		};
	}

	public async Task<InnerTubeChannel> GetChannelAsync(string channelId, ChannelTabs tabs = ChannelTabs.Featured,
		string language = "en", string region = "US")
	{
		BrowseResponse channel = await InnerTube.BrowseAsync(channelId, tabs.GetParams(), null, language, region);
		return new InnerTubeChannel(channel, language);
	}

	public async Task<InnerTubeChannel> GetChannelAsync(string channelId, string param, string language = "en",
		string region = "US")
	{
		BrowseResponse channel = await InnerTube.BrowseAsync(channelId, Utils.GetParamsFromChannelTabName(param), null,
			language, region);
		return new InnerTubeChannel(channel, language);
	}

	public async Task<ContinuationResponse> ContinueChannelAsync(string continuationToken, string language = "en",
		string region = "US")
	{
		BrowseResponse next = await InnerTube.ContinueBrowseAsync(continuationToken, language, region);
		RepeatedField<RendererWrapper> allItems = next.OnResponseReceivedActions.AppendContinuationItemsAction
			.ContinuationItems;
		IEnumerable<RendererWrapper> items = allItems.Where(x =>
			x.RendererCase != RendererWrapper.RendererOneofCase.ContinuationItemRenderer);
		ContinuationItemRenderer? continuation = allItems.LastOrDefault(x =>
			x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)?.ContinuationItemRenderer;
		return new ContinuationResponse
		{
			ContinuationToken = continuation?.ContinuationEndpoint.ContinuationCommand.Token,
			Results = Utils.ConvertRenderers(items, language)
		};
	}

	public async Task<InnerTubeAboutChannel?> GetAboutChannelAsync(string channelId, string language = "en", string region = "US")
	{
		BrowseResponse about =
			await InnerTube.ContinueBrowseAsync(Utils.PackChannelAboutPageParams(channelId), language, region);
		AboutChannelViewModel? viewModel = about.OnResponseReceivedEndpoints?.AppendContinuationItemsAction?.ContinuationItems[0]?.AboutChannelRenderer?.Metadata?.AboutChannelViewModel;
		return viewModel == null ? null : new InnerTubeAboutChannel(viewModel, language);
	}

	public async Task<InnerTubeChannel> SearchChannelAsync(string channelId, string query,
		string language = "en", string region = "US")
	{
		BrowseResponse channel =
			await InnerTube.BrowseAsync(channelId, "EgZzZWFyY2jyBgQKAloA", query, language, region);
		return new InnerTubeChannel(channel, language);
	}

	public async Task<InnerTubePlaylist> GetPlaylistAsync(string playlistId, bool includeUnavailable = false,
		PlaylistFilter filter = PlaylistFilter.All, string language = "en", string region = "US")
	{
		BrowseResponse playlist =
			await InnerTube.BrowseAsync(!playlistId.StartsWith("VL") ? "VL" + playlistId : playlistId,
				Utils.PackPlaylistParams(includeUnavailable, filter), null, language, region);
		return new InnerTubePlaylist(playlist, language);
	}

	public async Task<ContinuationResponse> ContinuePlaylistAsync(string continuationToken, string language = "en",
		string region = "US")
	{
		BrowseResponse playlist = await InnerTube.ContinueBrowseAsync(continuationToken, language, region);
		IEnumerable<RendererWrapper> renderers = playlist.Contents.TwoColumnBrowseResultsRenderer.Tabs[0]
			                                         .TabRenderer.Content?
			                                         .ResultsContainer.Results[0].ItemSectionRenderer
			                                         .Contents[0].PlaylistVideoListRenderer?.Contents ??
		                                         playlist.Contents.TwoColumnBrowseResultsRenderer.Tabs[0]
			                                         .TabRenderer.Content?
			                                         .ResultsContainer.Results[0].ItemSectionRenderer
			                                         .Contents ??
		                                         playlist.OnResponseReceivedActions?
			                                         .AppendContinuationItemsAction?.ContinuationItems ??
		                                         [];
		RendererContainer[] items = Utils.ConvertRenderers(renderers, language);

		return new ContinuationResponse
		{
			ContinuationToken = (items.LastOrDefault(x => x.Type == "continuation")?.Data as ContinuationRendererData)
				?.ContinuationToken,
			Results = items.Where(x => x.Type != "continuation").ToArray()
		};
	}

	public async Task<InnerTubeSearchResults> SearchAsync(string query, SearchParams? param = null,
		string language = "en",
		string region = "US")
	{
		SearchResponse searchResponse = await InnerTube.SearchAsync(query, param, language, region);
		return new InnerTubeSearchResults(searchResponse, language);
	}

	public async Task<SearchContinuationResponse> ContinueSearchAsync(string continuationToken, string language = "en",
		string region = "US")
	{
		SearchResponse searchResponse = await InnerTube.ContinueSearchAsync(continuationToken, language, region);
		RendererWrapper[] continuationItems = (searchResponse.OnResponseReceivedCommands?.SelectMany(x =>
				x.AppendContinuationItemsAction?.ContinuationItems ??
				x.ReloadContinuationItemsCommand?.ContinuationItems ?? []) ?? [])
			.ToArray();
		if (continuationItems[0].RendererCase == RendererWrapper.RendererOneofCase.TwoColumnSearchResultsRenderer)
		{
			RendererWrapper[] renderers = continuationItems[0].TwoColumnSearchResultsRenderer.PrimaryContents
				.ResultsContainer.Results.SelectMany(x => x.ItemSectionRenderer?.Contents ?? []).ToArray();
			return new SearchContinuationResponse
			{
				ContinuationToken = renderers
					.LastOrDefault(x => x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
					?.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token,
				Results = Utils.ConvertRenderers(
					renderers.Where(x => x.RendererCase != RendererWrapper.RendererOneofCase.ContinuationItemRenderer),
					language),
				Chips = Utils.ConvertRenderers(
					continuationItems[1].SearchHeaderRenderer.ChipBar.ChipCloudRenderer.Chips, language)
			};
		}
		else
		{
			return new SearchContinuationResponse
			{
				ContinuationToken = continuationItems
					.LastOrDefault(x => x.RendererCase == RendererWrapper.RendererOneofCase.ContinuationItemRenderer)
					?.ContinuationItemRenderer.ContinuationEndpoint.ContinuationCommand.Token,
				Results = Utils.ConvertRenderers(continuationItems.SelectMany(x => x.ItemSectionRenderer?.Contents ?? []),
					language),
				Chips = null
			};
		}
	}

	public async Task<ResolveUrlResponse> ResolveUrl(string url) => await InnerTube.ResolveUrl(url);
}