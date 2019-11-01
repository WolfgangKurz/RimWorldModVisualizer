using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;

namespace RimWorldModVisualizer.Models {
	internal class RWMSDB {
		public Dictionary<string, string> db { get; set; }

		public string timestamp { get; set; }
		public int version { get; set; }

		private const string RWMSDB_Url = "https://raw.githubusercontent.com/shakeyourbunny/RWMSDB/master/rwmsdb.json";
		private const string RWMSDB_Category_Url = "https://raw.githubusercontent.com/shakeyourbunny/RWMSDB/master/rwms_db_categories.json";

		public static async Task<RWMSDB> Fetch() {
			var raw = await new WebClient().DownloadStringTaskAsync(RWMSDB_Url);
			return JsonConvert.DeserializeObject<RWMSDB>(raw);
		}
	}

	[JsonConverter(typeof(RWMSDBCategoryInfoConverter))]
	internal class RWMSDBCategoryInfo {
		public decimal Weight { get; set; }
		public string Description { get; set; }
	}
	internal class RWMSDBCategoryInfoConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return objectType == typeof(RWMSDBCategoryInfo);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			var arr = JArray.Load(reader);
			var ret = new RWMSDBCategoryInfo();
			ret.Weight = (decimal)arr[0];
			ret.Description = (string)arr[1];
			return ret;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			JArray arr = new JArray();
			RWMSDBCategoryInfo info = (RWMSDBCategoryInfo)value;
			arr.Add(info.Weight);
			arr.Add(info.Description);
			arr.WriteTo(writer);
		}
	}
	internal class RWMSDBCategory {
		public Dictionary<string, RWMSDBCategoryInfo> cat { get; set; }

		private const string RWMSDB_Url = "https://raw.githubusercontent.com/shakeyourbunny/RWMSDB/master/rwmsdb.json";
		private const string RWMSDB_Category_Url = "https://raw.githubusercontent.com/shakeyourbunny/RWMSDB/master/rwms_db_categories.json";

		public static async Task<RWMSDBCategory> Fetch() {
			var raw = await new WebClient().DownloadStringTaskAsync(RWMSDB_Category_Url);
			var json = JsonConvert.DeserializeObject<RWMSDBCategory>($"{{\"cat\":{raw}}}"); // wrap

			var output = new RWMSDBCategory();
			output.cat = json.cat;
			return output;
		}
	}
}
