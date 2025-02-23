﻿using Newtonsoft.Json;
using RatScanner.FetchModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace RatScanner
{
	public static class ApiManager
	{
		public enum Language
		{
			English,
			Russian,
			German,
			French,
			Spanish,
			Chinese,
		}

		private static readonly Dictionary<Language, string> LanguageMapping = new Dictionary<Language, string>()
		{
			{Language.English, "en"},
			{Language.Russian, "ru"},
			{Language.German, "de"},
			{Language.French, "fr"},
			{Language.Spanish, "es"},
			{Language.Chinese, "cn"},
		};

		public enum ResourceType
		{
			ClientVersion,
			DownloadLink,
			PatreonLink,
			GithubLink,
			DiscordLink,
			FAQLink,
			ItemDataLink,
			ItemDataVersion,
		}

		private static readonly Dictionary<ResourceType, string> ResMapping = new Dictionary<ResourceType, string>
		{
			{ResourceType.ClientVersion, "RSClientVersion"},
			{ResourceType.DownloadLink, "RSDownloadLink"},
			{ResourceType.PatreonLink, "RSPatreonLink"},
			{ResourceType.GithubLink, "RSGithubLink"},
			{ResourceType.DiscordLink, "RSDiscordLink"},
			{ResourceType.FAQLink, "RSFAQLink"},
			{ResourceType.ItemDataLink, "RSItemDataLink"},
			{ResourceType.ItemDataVersion, "RSItemDataVersion"},
		};

		private static readonly Dictionary<ResourceType, string> ResCache = new Dictionary<ResourceType, string>();

		// Official RatScanner API URL
		private const string BaseUrl = "https://api.ratscanner.com/v3";

		// GitHub page for the tarkovdata repository, serves the master branch
		private const string TarkovDataUrl = "https://tarkovtracker.github.io/tarkovdata";

		// Base URL for the TarkovTracker URL
		private const string TarkovTrackerUrl = "https://tarkovtracker.io/api/v1";

		public static MarketItem[] GetMarketDB(Language language = Language.English)
		{
			try
			{
				var langString = LanguageMapping[language];
				var json = GetFromTarkovMarket($"https://tarkov-market.com/api/v1/items/all");
				return JsonConvert.DeserializeObject<MarketItem[]>(json);
			}
			catch (Exception e)
			{
				Logger.LogError($"Loading of market data failed.\n{e}");
				return null;
			}
		}

		// Checks the token metadata endpoint for TarkovTracker
		public static string? GetTarkovTrackerToken()
		{
			try
			{
				return Get($"{TarkovTrackerUrl}/token", RatConfig.Tracking.TarkovTracker.Token);
			}
			catch (WebException e)
			{
				var status = (e.Response as HttpWebResponse)?.StatusCode;
				if (status is HttpStatusCode.Unauthorized)
					// We can work with a 401
					throw new FetchModels.TarkovTracker.UnauthorizedTokenException("Token was rejected by the API", e);

				if (status is HttpStatusCode.TooManyRequests)
					throw new FetchModels.TarkovTracker.RateLimitExceededException("Rate Limiting reached for token", e);
				// Unknown error, continue throwing
				Logger.LogError($"Retrieving token metadata failed.", e);
				throw;
			}
			catch (Exception e)
			{
				Logger.LogError($"Retrieving token metadata failed.", e);
				throw;
			}
		}

		// Checks the token metadata endpoint for TarkovTracker
		public static string GetTarkovTrackerTeam()
		{
			try
			{
				return Get($"{TarkovTrackerUrl}/team/progress", RatConfig.Tracking.TarkovTracker.Token);
			}
			catch (WebException e)
			{
				var status = (e.Response as HttpWebResponse)?.StatusCode;
				if (status is HttpStatusCode.TooManyRequests)
					throw new FetchModels.TarkovTracker.RateLimitExceededException("Rate Limiting reached for token", e);
				// Unknown error, continue throwing
				Logger.LogError($"Retrieving TarkovTracker team data failed.", e);
				throw;
			}
			catch (Exception e)
			{
				Logger.LogError($"Retrieving TarkovTracker team data failed.", e);
				return null;
			}
		}

		// Checks the token metadata endpoint for TarkovTracker
		public static string GetTarkovTrackerSolo()
		{
			try
			{
				return Get($"{TarkovTrackerUrl}/progress", RatConfig.Tracking.TarkovTracker.Token);
			}
			catch (WebException e)
			{
				var status = (e.Response as HttpWebResponse)?.StatusCode;
				if (status is HttpStatusCode.TooManyRequests)
					throw new FetchModels.TarkovTracker.RateLimitExceededException("Rate Limiting reached for token", e);
				// Unknown error, continue throwing
				Logger.LogError($"Retrieving TarkovTracker progress data failed.", e);
				throw;
			}
			catch (Exception e)
			{
				Logger.LogError($"Retrieving TarkovTracker progress data failed.", e);
				return null;
			}
		}

		// Pulls the whole quest data file from tarkovdata for processing
		public static string GetProgressDataQuest()
		{
			try
			{
				return Get($"{TarkovDataUrl}/quests.json");
			}
			catch (Exception e)
			{
				Logger.LogError($"Loading of quest data failed.", e);
				return null;
			}
		}

		// Pulls the whole hideout file form tarkovdata for processing
		public static string GetProgressDataHideout()
		{
			try
			{
				return Get($"{TarkovDataUrl}/hideout.json");
			}
			catch (Exception e)
			{
				Logger.LogError($"Loading of hideout data failed.", e);
				return null;
			}
		}

		public static string GetResource(ResourceType resource)
		{
			if (ResCache.ContainsKey(resource)) return ResCache[resource];

			if (!ResMapping.ContainsKey(resource)) Logger.LogError($"Could not find resource mapping for: {resource}");
			var resPath = ResMapping[resource];

			try
			{
				Logger.LogInfo($"Loading resource \"{resPath}\"...");
				var json = Get($"{BaseUrl}/res/{resPath}");
				var value = JsonConvert.DeserializeObject<Resource>(json)?.Value;
				ResCache.Add(resource, value);
				return value;
			}
			catch (Exception e)
			{
				Logger.LogError($"Loading of resource \"{resPath}\" failed.", e);
				return "[Loading failed]";
			}
		}

		public static void DownloadFile(string url, string destination)
		{
			try
			{
				Logger.LogInfo($"Downloading file \"{url}\"...");
				var contents = Get(url);
				File.WriteAllText(destination, contents);
			}
			catch (Exception e)
			{
				Logger.LogError($"Downloading of file \"{url}\" failed.", e);
			}
		}

		private static string Get(string url, string bearerToken = null)
		{
			var request = WebRequest.CreateHttp(url);
			request.Method = WebRequestMethods.Http.Get;
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.UserAgent = $"RatScanner-Client/{RatConfig.Version}";
			if (bearerToken != null) request.Headers.Add("Authorization", "Bearer " + bearerToken);

			using var response = (HttpWebResponse)request.GetResponse();
			using var stream = response.GetResponseStream();

			var noEncoding = string.IsNullOrEmpty(response.CharacterSet);
			var encoding = noEncoding ? Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet);
			var reader = new StreamReader(stream, encoding);
			return reader.ReadToEnd();
		}

		private static string GetFromTarkovMarket(string url, string bearerToken = null)
		{
			var request = WebRequest.CreateHttp(url);
			request.Method = WebRequestMethods.Http.Get;
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.UserAgent = $"RatScanner-Client/{RatConfig.Version}";
			request.Headers.Add("x-api-key", "");

			using var response = (HttpWebResponse)request.GetResponse();
			using var stream = response.GetResponseStream();

			var noEncoding = string.IsNullOrEmpty(response.CharacterSet);
			var encoding = noEncoding ? Encoding.UTF8 : Encoding.GetEncoding(response.CharacterSet);
			var reader = new StreamReader(stream, encoding);
			return reader.ReadToEnd();
		}
	}
}
