using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonFx.Json
{
	public abstract class BaseEnum
	{
		public string Name { get; private set; }
		public int Id { get; private set; }

		private static readonly Dictionary<Type, List<BaseEnum>> _values = new Dictionary<Type, List<BaseEnum>>();

		protected BaseEnum(int id, string name)
		{
			Name = name;
			Id = id;
			Add(this);
		}

		private static void Add (BaseEnum value)
		{
			Type t = value.GetType();
			List<BaseEnum> valueList = null;
			if (_values.TryGetValue(t, out valueList) == false)
			{
				valueList = new List<BaseEnum>();
				_values.Add(t, valueList);
			}
			valueList.Add(value);
		}
		
		public static BaseEnum Find(Type t, string name)
		{
			List<BaseEnum> valueList = null;
			if (_values.TryGetValue(t, out valueList) == false)
			{
				return null;
			}
			return valueList.First(x => x.Name.Equals(name));
		}

		public static BaseEnum Find(Type t, int id)
		{
			List<BaseEnum> valueList = null;
			if (_values.TryGetValue(t, out valueList) == false)
			{
				return null;
			}
			return valueList.First(x => x.Id == id);
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public class TestEnum : BaseEnum
	{
		public static readonly TestEnum EnumValue0 = new TestEnum(0, "Test0");
		public static readonly TestEnum EnumValue1 = new TestEnum(1, "Test1");
		
		private TestEnum(int value, string name) : base(value, name) {}
	}
}

