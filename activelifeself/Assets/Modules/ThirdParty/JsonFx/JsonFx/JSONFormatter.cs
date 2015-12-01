using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;

namespace ChaoticMoon.JsonFx
{
	// http://geekswithblogs.net/luskan/archive/2007/07/16/113956.aspx
	public class JSONFormatter : IFormatter 
	{
		#region Currently Only here to keep interface consistency
		SerializationBinder binder;
        StreamingContext context;
        ISurrogateSelector surrogateSelector;
		
        public ISurrogateSelector SurrogateSelector 
		{
            get { return surrogateSelector; }
            set { surrogateSelector = value; }
        }
        public SerializationBinder Binder 
		{
            get { return binder; }
            set { binder = value; }
        }
        public StreamingContext Context 
		{
            get { return context; }
            set { context = value; }
        }		
		#endregion

        public JSONFormatter() 
		{
            context = new StreamingContext(StreamingContextStates.All);
        }       

        public object Deserialize(System.IO.Stream serializationStream) 
		{
			global::JsonFx.Json.JsonReaderSettings settings = new global::JsonFx.Json.JsonReaderSettings();
			settings.TypeHintName = "__type";
			global::JsonFx.Json.JsonReader reader = new global::JsonFx.Json.JsonReader(serializationStream, settings);
			return reader.Deserialize();
		}

        public void Serialize(System.IO.Stream serializationStream, object graph) 
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			global::JsonFx.Json.JsonWriterSettings settings = new global::JsonFx.Json.JsonWriterSettings();
			settings.TypeHintName = "__type";
			settings.PrettyPrint = true;
			global::JsonFx.Json.JsonWriter writer = new global::JsonFx.Json.JsonWriter(sb, settings);
			
			writer.Write(graph);
			string s = sb.ToString();
			using(System.IO.StreamWriter sw = new System.IO.StreamWriter(serializationStream))
			{
				sw.Write(s);
			}
        }
	}
}
