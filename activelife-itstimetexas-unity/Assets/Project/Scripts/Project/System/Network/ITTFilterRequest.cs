using UnityEngine;
using System.Collections.Generic;
using System;
using System.Web;
using System.Linq;

namespace ITT.System {
	
	#region Filter Enum
	[Flags]
	public enum Filter_Flags {
		None = 0,
		Admission = 1,
		Audience = 2,
		SkillLevel = 4,
		Category = 8,
		Type = 16,
		Distance = 32,
		MinDate = 64,
		MaxDate = 128,
		Date = 256
	}

	public enum Admission_Value {
		Free = 0,
		Fee = 1,
		All = 2
	}

	[Flags]
	public enum Audience_Value {
		All = 0,
		Senior_Citizen = 1,
		Adults = 2,
		Teenagers = 4,
		Children = 8
	}

	[Flags]
	public enum SkillLevel_Value {
		All = 0,
		Beginner_Friendly = 1,
		Intermediate = 2,
		Advanced = 4,
		Expert = 8
	}

	[Flags]
	public enum Category_Value {
		All = 0,
		Physical_Activities = 1,
		Health_Wellness = 2,
		Food_Nutrition = 4
	}

	public enum Type_Value {
		Deal,
		Activities,
		All
	}

	#endregion

	#region Filter Helper Classes
	public abstract class FilterValue {
		protected const string FIELD_GEOFIELD_DISTANCE = "distance";
		protected const string FIELD_ADMISSION_VALUE_ADULT = "admission_adults";
		protected const string FIELD_ADMISSION_VALUE_CHILDREN = "admission_children";
		protected const string FIELD_AUDIENCE_SENIORS = "audience_seniors:true";
		protected const string FIELD_AUDIENCE_ADULTS = "audience_adults:true";
		protected const string FIELD_AUDIENCE_TEENAGERS = "audience_teenagers:true";
		protected const string FIELD_AUDIENCE_KIDS = "audience_kids:true";
		protected const string FIELD_SKILL_LEVEL_BEGINNER = "level_beginner:true";
		protected const string FIELD_SKILL_LEVEL_INTERMEDIATE = "level_intermediate:true";
		protected const string FIELD_SKILL_LEVEL_ADVANCED = "level_advanced:true";
		protected const string FIELD_SKILL_LEVEL_EXPERT = "level_expert:true";
		protected const string FIELD_CATEGORY_PHYSICAL_ACTIVITY = "category_physical_activities:true";
		protected const string FIELD_CATEGORY_HEALTH_WELLNESS = "category_health_wellness:true";
		protected const string FIELD_CATEGORY_FOOD_NUTRITION = "category_food_nutrition:true";
		protected const string FIELD_TYPE = "[type]=";
		protected const string FIELD_DATE_TIME_VALUE = "[field_date_repeat_value]";
		protected const string FIELD_DATE_MIN = "[min][date]=";
		protected const string FIELD_DATE_MAX = "[max][date]=";

		abstract public Filter_Flags FilterFlag {get;}
	}

	class AdmissionValue : FilterValue {

		private Admission_Value _admissionValue;
		public AdmissionValue(Admission_Value admissionValue = Admission_Value.All) {
			_admissionValue = admissionValue;
		}

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.Admission;
			}
		}

		public override string ToString ()
		{
			string valueConversionString = null;
			if (_admissionValue == Admission_Value.Free)
			{
				valueConversionString = "is_free_admission:true";
			}
			else if (_admissionValue == Admission_Value.Fee)
			{
				valueConversionString = "is_free_admission:false";
			}

			return valueConversionString;
		}
	}
	
	class AudienceValue : FilterValue {
		private Audience_Value _audienceValue;
		public AudienceValue(Audience_Value audienceValue = Audience_Value.All) {
			_audienceValue = audienceValue;
		}

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.Audience;
			}
		}

		public override string ToString ()
		{

			string concatenatedFilter = null;
			foreach (Audience_Value av in Enum.GetValues(typeof(Audience_Value)))
			{
				if (av == Audience_Value.All)
					continue;

				if ((_audienceValue & av) == av)
				{
					switch (av)
					{
					case Audience_Value.Senior_Citizen:
						concatenatedFilter += FIELD_AUDIENCE_SENIORS + " ";
						break;
					case Audience_Value.Adults:
						concatenatedFilter += FIELD_AUDIENCE_ADULTS + " ";
						break;
					case Audience_Value.Teenagers:
						concatenatedFilter += FIELD_AUDIENCE_TEENAGERS + " ";
						break;
					case Audience_Value.Children:
						concatenatedFilter += FIELD_AUDIENCE_KIDS + " ";
						break;
					}
				}

			}

			return concatenatedFilter;

		}
	}
	
	class SkillLevelValue : FilterValue {
		private SkillLevel_Value _skillLevelValue;
		public SkillLevelValue(SkillLevel_Value skillLevelValue = SkillLevel_Value.All) {
			_skillLevelValue = skillLevelValue;
		}

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.SkillLevel;
			}
		}

		public override string ToString ()
		{
			string concatenatedFilter = null;
			foreach (SkillLevel_Value sv in Enum.GetValues(typeof(SkillLevel_Value)))
			{
				if (sv == SkillLevel_Value.All)
					continue;

				if ((_skillLevelValue & sv) == sv)
				{
					switch (_skillLevelValue)
					{
					case SkillLevel_Value.Beginner_Friendly:
						concatenatedFilter += FIELD_SKILL_LEVEL_BEGINNER + " ";
						break;
					case SkillLevel_Value.Intermediate:
						concatenatedFilter += FIELD_SKILL_LEVEL_INTERMEDIATE + " ";
						break;
					case SkillLevel_Value.Advanced:
						concatenatedFilter += FIELD_SKILL_LEVEL_ADVANCED + " ";
						break;
					case SkillLevel_Value.Expert:
						concatenatedFilter += FIELD_SKILL_LEVEL_EXPERT + " ";
						break;
					}
				}

			}
			
			return concatenatedFilter;
		}
	}
	
	class CategoryValue : FilterValue {
		private Category_Value _categoryValue;
		public CategoryValue(Category_Value categoryValue = Category_Value.All) {
			_categoryValue = categoryValue;
		}

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.Category;
			}
		}

		public override string ToString ()
		{
			string concatenatedFilter = null;
			foreach (Category_Value cv in Enum.GetValues(typeof(Category_Value)))
			{
				if (cv == Category_Value.All)
					continue;

				if ((_categoryValue & cv) == cv)
				{
					switch (cv)
					{
					case Category_Value.Physical_Activities:
						concatenatedFilter += FIELD_CATEGORY_PHYSICAL_ACTIVITY;
						break;
					case Category_Value.Health_Wellness:
						concatenatedFilter += FIELD_CATEGORY_HEALTH_WELLNESS;
						break;
					case Category_Value.Food_Nutrition:
						concatenatedFilter += FIELD_CATEGORY_FOOD_NUTRITION;
						break;
					}

					if (_categoryValue == cv)
						concatenatedFilter += " ";
					else
						concatenatedFilter += " OR ";
				}
			}

			if (!string.IsNullOrEmpty(concatenatedFilter))
			{
				// Remove any trailing " OR "s
				int indexOfOr = concatenatedFilter.LastIndexOf(" OR ");
				if (indexOfOr > -1 && indexOfOr == concatenatedFilter.Length-4)
				{
					concatenatedFilter = concatenatedFilter.Remove(indexOfOr);
				}
			}
			return concatenatedFilter;
		}
	}
	
	class TypeValue : FilterValue {
		private Type_Value _typeValue;
		public TypeValue(Type_Value typeValue = Type_Value.All) {
			_typeValue = typeValue;
		}

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.Type;
			}
		}

		public override string ToString ()
		{
			return "";
		}
	}

	class DistanceValue : FilterValue {
		private int _distanceValue;
		public DistanceValue(int distanceValue = 5) {
			_distanceValue = distanceValue * (int)HelperMethods.meterMileConversion;
		}

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.Distance;
			}
		}

		public override string ToString ()
		{
			LocationDataModel locationDataModel = ITTDataCache.Instance.RetrieveLocationData();
			if (null == locationDataModel)
			{
				locationDataModel = ITTDataCache.Instance.DefaultLocationData();
			}

			return ("distance(address, geopoint(" + locationDataModel.lastLatitude + ", " + locationDataModel.lastLongitude + ")) < " + _distanceValue);
		}
	}

	public abstract class DateValue : FilterValue {

		private const int ROUNDING_MINUTES = 30;
		protected DateTime _dateValue;
		
		public DateValue() {
			_dateValue = DateTime.Now;
		}

		public DateValue(DateTime dateTime) {
			_dateValue = dateTime;
		}

		public long ConvertToUnixTime(DateTime dateTime)
		{
			dateTime = RoundUp(dateTime, TimeSpan.FromMinutes(ROUNDING_MINUTES));
			dateTime = TimeZoneInfo.ConvertTimeToUtc (dateTime);
			DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return (long)(dateTime - startTime).TotalSeconds;
		}

		private DateTime RoundUp(DateTime dt, TimeSpan d) {
			return new DateTime(((dt.Ticks + d.Ticks - 1) / d.Ticks) * d.Ticks);
		}
	}
	
	public class MinDateValue : DateValue {

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.MinDate;
			}
		}

		public MinDateValue(DateTime dateTime) : base(dateTime) {}

		public override string ToString ()
		{	
			return "event_date >= posix:" + ConvertToUnixTime(_dateValue).ToString();
		}
	}

	public class MaxDateValue : DateValue {

		public override Filter_Flags FilterFlag {
			get {
				return Filter_Flags.MaxDate;
			}
		}

		public MaxDateValue(DateTime dateTime) : base(dateTime) {}

		public override string ToString ()
		{	
			return "event_date <= posix:" + ConvertToUnixTime(_dateValue.AddDays(1)).ToString();
		}
	}

	#endregion

	public class ITTFilterRequest {
		private List<FilterValue> _filterList;

		public ITTFilterRequest() {
			_filterList = new List<FilterValue>();
		}

		#region Adding Filter Values
		public void AddFilterValue(FilterValue filterValue) {
			_filterList.Add(filterValue);
		}

		#endregion

		#region Retrieve Filter Values
		public string RetrieveFilterURLString() {
			List<Filter_Flags> foundFlags = new List<Filter_Flags>();
			string combinedFilterString = "";
			foreach (var filterItem in _filterList) {
				foundFlags.Add(filterItem.FilterFlag);
				combinedFilterString += filterItem.ToString();
				combinedFilterString += " ";
			}

			string uriEscape = Uri.EscapeUriString(combinedFilterString);
			return uriEscape.Replace("(", "%28").Replace(")", "%29").Replace(",", "%2C").Replace(":", "%3A").Replace("=", "%3D");

		}

		#endregion

	}
}

