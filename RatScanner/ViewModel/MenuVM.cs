using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Web;
using RatEye;
using RatScanner.FetchModels;
using RatScanner.Scan;
using RatStash;

namespace RatScanner.ViewModel
{
	internal class MainWindowVM : INotifyPropertyChanged
	{
		private const string UpSymbol = "▲";
		private const string DownSymbol = "▼";

		private RatScannerMain _dataSource;

		public RatScannerMain DataSource
		{
			get => _dataSource;
			set
			{
				_dataSource = value;
				OnPropertyChanged();
			}
		}

		private ItemScan CurrentItemScan => DataSource?.CurrentItemScan;

		private Item[] MatchedItems => CurrentItemScan?.MatchedItems;

		public string IconPath
		{
			get
			{
				ItemExtraInfo itemExtraInfo;
				if (CurrentItemScan is ItemIconScan scan) itemExtraInfo = scan.ItemExtraInfo;
				else itemExtraInfo = new ItemExtraInfo();
				var path = CurrentItemScan.IconPath;
				return path ?? RatConfig.Paths.UnknownIcon;
			}
		}

		public string Name => MatchedItems[0].Name;

		public bool HasMods => MatchedItems[0] is CompoundItem itemC && itemC.Slots.Count > 0;

		// https://youtrack.jetbrains.com/issue/RSRP-468572
		// ReSharper disable InconsistentNaming
		public string Avg24hPrice => PriceToString(GetAvg24hPrice());

		private int GetAvg24hPrice()
		{
			return MatchedItems[0].GetAvg24hMarketPrice();
		}
		// ReSharper restore InconsistentNaming

		public string PricePerSlot => PriceToString(GetAvg24hPrice() / (MatchedItems[0].Width * MatchedItems[0].Height));

		public string TraderName => GetBestTrader();

		public string BestTraderPrice => IntToGroupedString(GetMaxTraderPrice()) + GetTraderCurrency();

		private string GetBestTrader()
		{
			return MatchedItems[0].GetBestTrader();
		}

		public string MaxTraderPrice => IntToGroupedString(GetMaxTraderPrice()) + GetTraderCurrency();

		private int GetMaxTraderPrice()
		{
			return MatchedItems[0].GetMaxTraderPrice();
		}

		private string GetTraderCurrency()
		{
			return MatchedItems[0].GetTraderCurrency();
		}

		public NeededItem TrackingNeeds => MatchedItems[0].GetTrackingNeeds();

		public NeededItem TrackingTeamNeedsSummed => MatchedItems[0].GetSummedTrackingTeamNeeds();

		public List<KeyValuePair<string, NeededItem>> TrackingTeamNeeds => MatchedItems[0].GetTrackingTeamNeeds();

		public List<KeyValuePair<string, NeededItem>> TrackingTeamNeedsFiltered => TrackingTeamNeeds.Where(x => x.Value.Remaining > 0).ToList();

		public string DiscordLink => ApiManager.GetResource(ApiManager.ResourceType.DiscordLink);

		public string GithubLink => ApiManager.GetResource(ApiManager.ResourceType.GithubLink);

		public string PatreonLink => ApiManager.GetResource(ApiManager.ResourceType.PatreonLink);

		public string Updated
		{
			get
			{
				return MatchedItems[0].GetMarketItem().updated;
			}
		}

		public string WikiLink
		{
			get
			{
				var link = MatchedItems[0].GetMarketItem().wikiLink;
				if (link.Length > 3) return link;
				return $"https://escapefromtarkov.gamepedia.com/{HttpUtility.UrlEncode(Name.Replace(" ", "_"))}";
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public MainWindowVM(RatScannerMain ratScanner)
		{
			DataSource = ratScanner;
			DataSource.PropertyChanged += ModelPropertyChanged;
		}

		protected virtual void OnPropertyChanged(string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged();
		}

		private string PriceToString(int price)
		{
			if (MatchedItems.Length == 1) return IntToGroupedString(price) + " ₽";

			// TODO make this more informative. Perhaps a value range?
			return "Uncertain";
		}

		private static string IntToGroupedString(int? value)
		{
			if (value == null) return "ERROR";

			var text = $"{value:n0}";
			var numberGroupSeparator = NumberFormatInfo.CurrentInfo.NumberGroupSeparator;
			return text.Replace(numberGroupSeparator, RatConfig.ToolTip.DigitGroupingSymbol);
		}
	}
}
