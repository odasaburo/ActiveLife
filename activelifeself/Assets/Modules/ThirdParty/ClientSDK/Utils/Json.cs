using System;
using System.Collections;
using System.Collections.Generic;
using JsonFx.Json;

namespace ClientSDK.Utils
{
	public static class Json
	{
		public static T GetObjectFromJson<T>(string json)
		{
			JsonReaderSettings settings = new JsonReaderSettings ();
			settings.TypeHintName = "__type";
			
			JsonReader reader = new JsonReader (json, settings);
			return reader.Deserialize<T> ();
		}
		
		public static string GetJsonString (object o, bool prettyPrint = false, bool useTypeHint = true)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			JsonWriterSettings settings = new JsonWriterSettings();
			settings.MaxDepth = 50;
			if (useTypeHint) settings.TypeHintName = "__type";
			settings.UseXmlSerializationAttributes = true;
			settings.PrettyPrint = prettyPrint;
			using (JsonWriter writer = new JsonWriter(sb, settings))
			{
				writer.Write(o);
			}
			string json = sb.ToString();
			return json;
		}		
	}
}
