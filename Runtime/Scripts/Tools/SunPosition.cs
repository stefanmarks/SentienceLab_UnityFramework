#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEditor;
using System;

namespace SentienceLab
{
	/// <summary>
	/// Script to control the sun based on latitude/longigude/date/time
	/// </summary>
	/// 
	[RequireComponent(typeof(Light))]
	[ExecuteInEditMode]
	[AddComponentMenu("SentienceLab/Tools/Sun Position")]

	public class SunPosition : MonoBehaviour
	{
		[Tooltip("Longitude (E/W) in degrees")]
		[Range(-180, 180)]
		public float Longitude;
		
		[Tooltip("Latitude (N/S) in degrees")]
		[Range(-90, 90)]
		public float Latitude;

		[Tooltip("Year")]
		[Range(1900, 2100)]
		public int Year = 2023;
		
		[Tooltip("Day of year (0..366)")]
		[Range(0, 365)]
		public int Day = 0;
		
		[Tooltip("Time of day (0.0 - 24.0)")]
		[Range(0,24)]
		public float Time;

		[Tooltip("Timezone")]
		public STimeZone Timezone;

		[Tooltip("Sun colour over the azimuth (0% = -90 degrees, 100% = 90 degrees")]
		public Gradient SunColour;


		public void Start()
		{
			m_light = GetComponent<Light>();
		}

		public void SetLocation(float _longitude, float _latitude)
		{
			Longitude   = _longitude;
			Latitude    = _latitude;
		}

		public void SetDate(DateTime _dateTime)
		{
			Year = _dateTime.Year;
			Day  = _dateTime.DayOfYear;
			SetTime(_dateTime.Hour + (_dateTime.Minute / 60.0f));
		}

		public void SetTime(float _time)
		{
			Time = _time;
		}

		public void OnValidate()
		{
			UpdateSunPosition();
		}

		public void Update()
		{
			// only bother when parameters have changed
			if ((m_oldTime != Time) || (m_oldTimezoneIdx != Timezone._arrIndex) ||
				(m_oldYear != Year) || (m_oldDay != Day) ||
			    (m_oldLat  != Latitude) || (m_oldLong != Longitude)			    
			   )
			{
				UpdateSunPosition();

				m_oldLat         = Latitude;
				m_oldLong        = Longitude;
				m_oldYear        = Year;
				m_oldDay         = Day;
				m_oldTime        = Time;
				m_oldTimezoneIdx = Timezone._arrIndex;
			}
		}

		/**
		 * Calculates the suns "position" based on a given date and time in local time, latitude and longitude 
		 * expressed in decimal degrees. It is based on the method found here: 
		 * http://www.astro.uio.no/~bgranslo/aares/calculate.html 
		 * The calculation is only satisfiably correct for dates in the range March 1 1900 to February 28 2100. 
		 */
		void UpdateSunPosition()
		{
			if (m_light == null) return;

			const double Deg2Rad = Math.PI / 180.0;

			// Merge date and time and convert to UTC
			DateTime dateTime = new DateTime(Year, 1, 1, 0, 0, 0);
			dateTime = dateTime.AddDays(Day).AddHours(Time);
			dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, Timezone.zone);

			// Number of days from J2000.0.  
			double julianDate = 367 * dateTime.Year -
				(int)((7.0 / 4.0) * (dateTime.Year +
				(int)((dateTime.Month + 9.0) / 12.0))) +
				(int)((275.0 * dateTime.Month) / 9.0) +
				dateTime.Day - 730531.5;
			double julianCenturies = julianDate / 36525.0;

			// Sidereal Time  
			double siderealTimeHours = 6.6974 + 2400.0513 * julianCenturies;

			double siderealTimeUT = siderealTimeHours +
				(366.2422 / 365.2422) * (double)dateTime.TimeOfDay.TotalHours;

			double siderealTime = siderealTimeUT * 15 + Longitude;

			// Refine to number of days (fractional) to specific time.  
			julianDate += (double)dateTime.TimeOfDay.TotalHours / 24.0;
			julianCenturies = julianDate / 36525.0;

			// Solar Coordinates  
			double meanLongitude      = CorrectAngle(Deg2Rad * (280.466 + 36000.77 * julianCenturies));
			double meanAnomaly        = CorrectAngle(Deg2Rad * (357.529 + 35999.05 * julianCenturies));
			double equationOfCenter   = Deg2Rad * ((1.915 - 0.005 * julianCenturies) *
			                            Math.Sin(meanAnomaly) + 0.02 * Math.Sin(2 * meanAnomaly));
			double elipticalLongitude = CorrectAngle(meanLongitude + equationOfCenter);
			double obliquity          = (23.439 - 0.013 * julianCenturies) * Deg2Rad;

			// Right Ascension  
			double rightAscension = Math.Atan2(
				Math.Cos(obliquity) * Math.Sin(elipticalLongitude),
				Math.Cos(elipticalLongitude));

			double declination = Math.Asin(Math.Sin(rightAscension) * Math.Sin(obliquity));

			// Horizontal Coordinates  
			double hourAngle = CorrectAngle(siderealTime * Deg2Rad) - rightAscension;

			if (hourAngle > Math.PI)
			{
				hourAngle -= 2 * Math.PI;
			}

			double altitude = Math.Asin(Math.Sin(Latitude * Deg2Rad) *
				Math.Sin(declination) + Math.Cos(Latitude * Deg2Rad) *
				Math.Cos(declination) * Math.Cos(hourAngle));

			// Nominator and denominator for calculating Azimuth  
			// angle. Needed to test which quadrant the angle is in.  
			double aziNom = -Math.Sin(hourAngle);
			double aziDenom =
				Math.Tan(declination) * Math.Cos(Latitude * Deg2Rad) -
				Math.Sin(Latitude * Deg2Rad) * Math.Cos(hourAngle);

			double azimuth = Math.Atan(aziNom / aziDenom);

			if (aziDenom < 0) // In 2nd or 3rd quadrant  
			{
				azimuth += Math.PI;
			}
			else if (aziNom < 0) // In 4th quadrant  
			{
				azimuth += 2 * Math.PI;
			}

			Vector3 angles = Vector3.zero;
			angles.x = (float)altitude * Mathf.Rad2Deg;
			angles.y = (float)azimuth  * Mathf.Rad2Deg + 180; // 180 to have Z+=N, X+=E
			
			m_light.transform.localRotation = Quaternion.Euler(angles);
			m_light.color = SunColour.Evaluate((angles.x + 90.0f) / 180.0f);
		}

		/** 
		 * Corrects an angle. 
		 * 
		 * \param angleInRadians An angle expressed in radians. 
		 * \return An angle in the range 0 to 2*PI. 
		 */
		private double CorrectAngle(double angleInRadians)
		{
			if (angleInRadians < 0)
			{
				return 2 * Math.PI - (Math.Abs(angleInRadians) % (2 * Math.PI));
			}
			else if (angleInRadians > 2 * Math.PI)
			{
				return angleInRadians % (2 * Math.PI);
			}
			else
			{
				return angleInRadians;
			}
		}


		[Serializable]
		public struct STimeZone
		{
			public int _arrIndex;


			public TimeZoneInfo zone { get { return GetTimezones()[_arrIndex]; } }


			public static TimeZoneInfo[] GetTimezones()
			{
				if (m_sTimeZones == null)
				{
					var zones = TimeZoneInfo.GetSystemTimeZones();
					m_sTimeZones = new TimeZoneInfo[zones.Count];
					for (int i = 0; i < m_sTimeZones.Length; i++)
					{
						m_sTimeZones[i] = zones[i];
					}
					Array.Sort(m_sTimeZones, SortTimeZoneInfo);
					m_sTimeZoneNames = new string[zones.Count];
					for (int i = 0; i < m_sTimeZoneNames.Length; i++)
					{
						m_sTimeZoneNames[i] = m_sTimeZones[i].DisplayName;
					}
				}
				return m_sTimeZones;
			}


			public static int SortTimeZoneInfo(TimeZoneInfo a, TimeZoneInfo b)
			{
				return a.DisplayName.CompareTo(b.DisplayName);
			}


			public static string[] GetTimezoneNames()
			{
				GetTimezones();
				return m_sTimeZoneNames;
			}


			private static TimeZoneInfo[] m_sTimeZones     = null;
			private static string[]       m_sTimeZoneNames = null;
		}


		private Light     m_light;
		private float     m_oldLat,  m_oldLong, m_oldTime;
		private int       m_oldYear, m_oldDay,  m_oldTimezoneIdx;
	}


#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SunPosition.STimeZone))]
	public class PropertyDrawer_TimeZone : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			SerializedProperty userIndexProperty = property.FindPropertyRelative("_arrIndex");

			// Draw label
			position = EditorGUI.PrefixLabel(position, label);

			EditorGUI.BeginChangeCheck();
			int _newIdx = EditorGUI.Popup(position, userIndexProperty.intValue, SunPosition.STimeZone.GetTimezoneNames());
			if (EditorGUI.EndChangeCheck())
			{
				userIndexProperty.intValue = _newIdx;
			}
		}
	}
#endif
}
