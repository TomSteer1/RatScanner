using System;

namespace RatScanner.FetchModels
{
	[Serializable]
	public class MarketItem
	{
		// Item data
		public string uid { get; set; }
		public string wikiLink { get; set; } = "Unknown";
		public string updated { get; set; }

		// Price data
		public int price { get; set; }
		public int basePrice { get; set; }

		// https://youtrack.jetbrains.com/issue/RSRP-468572
		// ReSharper disable InconsistentNaming
		public int avg24hPrice { get; set; }
		// ReSharper restore InconsistentNaming

		public int traderPrice { get; set; }

		public MarketItem(string id)
		{
			uid = id;
		}
	}
}
