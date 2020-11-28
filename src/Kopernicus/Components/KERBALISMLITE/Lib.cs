using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEngine;
using CommNet;
using KSP.Localization;
using KSP.UI.Screens;

namespace KERBALISMLITE
{
	public static class Lib
	{
		#region UTILS

		public enum LogLevel
		{
			Message,
			Warning,
			Error
		}

		private static void Log(MethodBase method, string message, LogLevel level)
		{
			switch (level)
			{
				default:
					UnityEngine.Debug.Log(string.Format("[Kerbalism] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
				case LogLevel.Warning:
					UnityEngine.Debug.LogWarning(string.Format("[Kerbalism] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
				case LogLevel.Error:
					UnityEngine.Debug.LogError(string.Format("[Kerbalism] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
			}
		}

		static Version kerbalismVersion = Version.Parse("0.1");
		/// <summary> current Kerbalism major/minor version</summary>
		public static Version KerbalismVersion
		{
			get
			{
				if (kerbalismVersion == null) kerbalismVersion = new Version(Assembly.GetAssembly(typeof(Kerbalism)).GetName().Version.Major, Assembly.GetAssembly(typeof(Kerbalism)).GetName().Version.Minor);
				return kerbalismVersion;
			}
		}

		/// <summary> current KSP version as a "MajorMinor" string</summary>
		public static string KSPVersionCompact
		{
			get
			{
				return Versioning.version_major.ToString() + Versioning.version_minor.ToString();
			}
		}

		///<summary>return true if an assembly with specified name is loaded</summary>
		public static bool HasAssembly(string name)
		{
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				if (a.name == name) return true;
			}
			return false;
		}

		///<summary>swap two variables</summary>
		public static void Swap<T>(ref T a, ref T b)
		{
			T tmp = b;
			b = a;
			a = tmp;
		}

		public static string KerbalismRootPath => Path.Combine(Path.Combine(Path.GetFullPath(KSPUtil.ApplicationRootPath), "GameData"), "Kerbalism");

		///<summary>find a directory in the GameData directory</summary>
		public static bool GameDirectoryExist(string findpath)
		{
			try
			{
				string gamedir = Path.Combine(Path.GetFullPath(KSPUtil.ApplicationRootPath), "GameData/" + findpath);
				findpath = Path.GetFileName(gamedir);
				gamedir = Path.GetDirectoryName(gamedir);
				string[] paths = System.IO.Directory.GetDirectories(gamedir, findpath, SearchOption.AllDirectories);
				if (paths.Length > 0)
					return true;
				else
					return false;
			}
			catch (Exception e)
			{
				return false;
			}
		}
		#endregion

		#region MATH
		///<summary>clamp a value</summary>
		public static int Clamp(int value, int min, int max)
		{
			return Math.Max(min, Math.Min(value, max));
		}

		///<summary>clamp a value</summary>
		public static float Clamp(float value, float min, float max)
		{
			return Math.Max(min, Math.Min(value, max));
		}

		///<summary>clamp a value</summary>
		public static double Clamp(double value, double min, double max)
		{
			return Math.Max(min, Math.Min(value, max));
		}

		///<summary>blend between two values</summary>
		public static float Mix(float a, float b, float k)
		{
			return a * (1.0f - k) + b * k;
		}

		///<summary>blend between two values</summary>
		public static double Mix(double a, double b, double k)
		{
			return a * (1.0 - k) + b * k;
		}
		#endregion

		#region RANDOM
		// store the random number generator
		static System.Random rng = new System.Random();

		///<summary>return random integer</summary>
		public static int RandomInt(int max_value)
		{
			return rng.Next(max_value);
		}

		///<summary>return random float [0..1]</summary>
		public static float RandomFloat()
		{
			return (float)rng.NextDouble();
		}

		///<summary>return random double [0..1]</summary>
		public static double RandomDouble()
		{
			return rng.NextDouble();
		}


		static int fast_float_seed = 1;
		/// <summary>
		/// return random float in [-1,+1] range
		/// - it is less random than the c# RNG, but is way faster
		/// - the seed is meant to overflow! (turn off arithmetic overflow/underflow exceptions)
		/// </summary>
		public static float FastRandomFloat()
		{
			fast_float_seed *= 16807;
			return fast_float_seed * 4.6566129e-010f;
		}
		#endregion

		#region HASH
		///<summary>combine two guid, irregardless of their order (eg: Combine(a,b) == Combine(b,a))</summary>
		public static Guid CombineGuid(Guid a, Guid b)
		{
			byte[] a_buf = a.ToByteArray();
			byte[] b_buf = b.ToByteArray();
			byte[] c_buf = new byte[16];
			for (int i = 0; i < 16; ++i) c_buf[i] = (byte)(a_buf[i] ^ b_buf[i]);
			return new Guid(c_buf);
		}

		///<summary>combine two guid, in a non-commutative way</summary>
		public static Guid OrderedCombineGuid(Guid a, Guid b)
		{
			byte[] a_buf = a.ToByteArray();
			byte[] b_buf = b.ToByteArray();
			byte[] c_buf = new byte[16];
			for (int i = 0; i < 16; ++i) c_buf[i] = (byte)(a_buf[i] & ~b_buf[i]);
			return new Guid(c_buf);
		}

		///<summary>get 32bit FNV-1a hash of a string</summary>
		public static UInt32 Hash32(string s)
		{
			// offset basis
			UInt32 h = 2166136261u;

			// for each byte of the buffer
			for (int i = 0; i < s.Length; ++i)
			{
				// xor the bottom with the current octet
				h ^= s[i];

				// equivalent to h *= 16777619 (FNV magic prime mod 2^32)
				h += (h << 1) + (h << 4) + (h << 7) + (h << 8) + (h << 24);
			}

			//return the hash
			return h;
		}
		#endregion

		#region TIME

		private static double hoursInDay = -1.0;
		///<summary>return hours in a day</summary>
		public static double HoursInDay
		{
			get
			{
				if (hoursInDay == -1.0)
				{
					if (FlightGlobals.ready || IsEditor())
					{
						var homeBody = FlightGlobals.GetHomeBody();
						hoursInDay = Math.Round(homeBody.rotationPeriod / 3600, 0);
					}
					else
					{
						return GameSettings.KERBIN_TIME ? 6.0 : 24.0;
					}

				}
				return hoursInDay;
			}
		}

		private static double daysInYear = -1.0;
		///<summary>return year length</summary>
		public static double DaysInYear
		{
			get
			{
				if (daysInYear == -1.0)
				{
					if (FlightGlobals.ready || IsEditor())
					{
						var homeBody = FlightGlobals.GetHomeBody();
						daysInYear = Math.Floor(homeBody.orbit.period / (HoursInDay * 60.0 * 60.0));
					}
					else
					{
						return GameSettings.KERBIN_TIME ? 426.0 : 365.0;
					}
				}
				return daysInYear;
			}
		}


		///<summary>stop time warping</summary>
		public static void StopWarp(double maxSpeed = 0)
		{
			var warp = TimeWarp.fetch;
			warp.CancelAutoWarp();
			int maxRate = 0;
			for (int i = 0; i < warp.warpRates.Length; ++i)
			{
				if (warp.warpRates[i] < maxSpeed)
					maxRate = i;
			}
			TimeWarp.SetRate(maxRate, true, false);
		}

		///<summary>disable time warping above a specified level</summary>
		public static void DisableWarp(uint max_level)
		{
			for (uint i = max_level + 1u; i < 8; ++i)
			{
				TimeWarp.fetch.warpRates[i] = TimeWarp.fetch.warpRates[max_level];
			}
		}

		///<summary>get current time</summary>
		public static UInt64 Clocks()
		{
			return (UInt64)Stopwatch.GetTimestamp();
		}

		///<summary>convert from clocks to microseconds</summary>
		public static double Microseconds(UInt64 clocks)
		{
			return clocks * 1000000.0 / Stopwatch.Frequency;
		}


		public static double Milliseconds(UInt64 clocks)
		{
			return clocks * 1000.0 / Stopwatch.Frequency;
		}


		public static double Seconds(UInt64 clocks)
		{
			return clocks / (double)Stopwatch.Frequency;
		}

		///<summary>return human-readable timestamp of planetarium time</summary>
		public static string PlanetariumTimestamp()
		{
			double t = Planetarium.GetUniversalTime();
			const double len_min = 60.0;
			const double len_hour = len_min * 60.0;
			double len_day = len_hour * Lib.HoursInDay;
			double len_year = len_day * Lib.DaysInYear;

			double year = Math.Floor(t / len_year);
			t -= year * len_year;
			double day = Math.Floor(t / len_day);
			t -= day * len_day;
			double hour = Math.Floor(t / len_hour);
			t -= hour * len_hour;
			double min = Math.Floor(t / len_min);

			return BuildString
			(
			  "[",
			  ((uint)year + 1).ToString("D4"),
			  "/",
			  ((uint)day + 1).ToString("D2"),
			  " ",
			  ((uint)hour).ToString("D2"),
			  ":",
			  ((uint)min).ToString("D2"),
			  "]"
			);
		}

		///<summary>return true half the time</summary>
		public static int Alternate(int seconds, int elements)
		{
			return ((int)Time.realtimeSinceStartup / seconds) % elements;
		}
		#endregion

		#region REFLECTION
		private static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		///<summary>
		/// return a value from a module using reflection
		/// note: useful when the module is from another assembly, unknown at build time
		/// note: useful when the value isn't persistent
		/// note: this function break hard when external API change, by design
		/// </summary>
		public static T ReflectionValue<T>(PartModule m, string value_name)
		{
			return (T)m.GetType().GetField(value_name, flags).GetValue(m);
		}

		public static T? SafeReflectionValue<T>(PartModule m, string value_name) where T : struct
		{
			FieldInfo fi = m.GetType().GetField(value_name, flags);
			if (fi == null)
				return null;
			return (T)fi.GetValue(m);
		}

		///<summary>
		/// set a value from a module using reflection
		/// note: useful when the module is from another assembly, unknown at build time
		/// note: useful when the value isn't persistent
		/// note: this function break hard when external API change, by design
		///</summary>
		public static void ReflectionValue<T>(PartModule m, string value_name, T value)
		{
			m.GetType().GetField(value_name, flags).SetValue(m, value);
		}

		///<summary> Sets the value of a private field via reflection </summary>
		public static void ReflectionValue<T>(object instance, string value_name, T value)
		{
			instance.GetType().GetField(value_name, flags).SetValue(instance, value);
		}

		///<summary> Returns the value of a private field via reflection </summary>
		public static T ReflectionValue<T>(object instance, string field_name)
		{
			return (T)instance.GetType().GetField(field_name, flags).GetValue(instance);
		}

		public static void ReflectionCall(object m, string call_name)
		{
			m.GetType().GetMethod(call_name, flags).Invoke(m, null);
		}

		public static T ReflectionCall<T>(object m, string call_name)
		{
			return (T)(m.GetType().GetMethod(call_name, flags).Invoke(m, null));
		}

		public static void ReflectionCall(object m, string call_name, Type[] types, object[] parameters)
		{
			m.GetType().GetMethod(call_name, flags, null, types, null).Invoke(m, parameters);
		}

		public static T ReflectionCall<T>(object m, string call_name, Type[] types, object[] parameters)
		{
			return (T)(m.GetType().GetMethod(call_name, flags, null, types, null).Invoke(m, parameters));
		}
		#endregion

		#region STRING
		/// <summary> return string limited to len, with ... at the end</summary>
		public static string Ellipsis(string s, uint len)
		{
			len = Math.Max(len, 3u);
			return s.Length <= len ? s : Lib.BuildString(s.Substring(0, (int)len - 3), "...");
		}

		/// <summary> return string limited to len, with ... in the middle</summary>
		public static string EllipsisMiddle(string s, int len)
		{
			if (s.Length > len)
			{
				len = (len - 3) / 2;
				return Lib.BuildString(s.Substring(0, len), "...", s.Substring(s.Length - len));
			}
			return s;
		}

		///<summary>tokenize a string</summary>
		public static List<string> Tokenize(string txt, char separator)
		{
			List<string> ret = new List<string>();
			string[] strings = txt.Split(separator);
			foreach (string s in strings)
			{
				string trimmed = s.Trim();
				if (trimmed.Length > 0) ret.Add(trimmed);
			}
			return ret;
		}

		///<summary>
		/// return message with the macro expanded
		///- variant: tokenize the string by '|' and select one
		///</summary>
		public static string ExpandMsg(string txt, Vessel v = null, ProtoCrewMember c = null, uint variant = 0)
		{
			// get variant
			var variants = txt.Split('|');
			if (variants.Length > variant) txt = variants[variant];

			// macro expansion
			string v_name = v != null ? (v.isEVA ? "EVA" : v.vesselName) : "";
			string c_name = c != null ? c.name : "";
			return txt
			  .Replace("@", "\n")
			  .Replace("$VESSEL", BuildString("<b>", v_name, "</b>"))
			  .Replace("$KERBAL", "<b>" + c_name + "</b>")
			  .Replace("$ON_VESSEL", v != null && v.isActiveVessel ? "" : BuildString("On <b>", v_name, "</b>, "))
			  .Replace("$HIS_HER", c != null && c.gender == ProtoCrewMember.Gender.Male ? Local.Kerbal_his : Local.Kerbal_her);//"his""her"
		}

		///<summary>make the first letter uppercase</summary>
		public static string UppercaseFirst(string s)
		{
			return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : string.Empty;
		}

		///<summary>standardized kerbalism string colors</summary>
		public enum Kolor
		{
			None,
			Green,
			Yellow,
			Orange,
			Red,
			PosRate,
			NegRate,
			Science,
			Cyan,
			LightGrey,
			DarkGrey
		}

		///<summary>return a colored "[V]" or "[X]" depending on the condition. Only work if placed at the begining of a line. To align other lines, use the "<pos=5em>" tag</summary>
		public static string Checkbox(bool condition)
		{
			return condition
				? " <color=#88FF00><mark=#88FF0033><mspace=1em><b><i>V </i></b></mspace></mark></color><pos=5em>"
				: " <color=#FF8000><mark=#FF800033><mspace=1em><b><i>X </i></b></mspace></mark></color><pos=5em>";
		}

		///<summary>return the hex representation for kerbalism Kolors</summary>
		public static string KolorToHex(Kolor color)
		{
			switch (color)
			{
				case Kolor.None:		return "#FFFFFF"; // use this in the Color() methods if no color tag is to be applied
				case Kolor.Green:		return "#88FF00"; // green whith slightly less red than the ksp ui default (CCFF00), for better contrast with yellow
				case Kolor.Yellow:		return "#FFD200"; // ksp ui yellow
				case Kolor.Orange:		return "#FF8000"; // ksp ui orange
				case Kolor.Red:		    return "#FF3333"; // custom red
				case Kolor.PosRate:	    return "#88FF00"; // green
				case Kolor.NegRate:	    return "#FF8000"; // orange
				case Kolor.Science:	    return "#6DCFF6"; // ksp science color
				case Kolor.Cyan:		return "#00FFFF"; // cyan
				case Kolor.LightGrey:	return "#CCCCCC"; // light grey
				case Kolor.DarkGrey:	return "#999999"; // dark grey	
				default:				return "#FEFEFE";
			}
		}

		///<summary>return the unity Colot  for kerbalism Kolors</summary>
		public static Color KolorToColor(Kolor color)
		{
			switch (color)
			{
				case Kolor.None:      return new Color(1.000f, 1.000f, 1.000f); 
				case Kolor.Green:     return new Color(0.533f, 1.000f, 0.000f);
				case Kolor.Yellow:    return new Color(1.000f, 0.824f, 0.000f);
				case Kolor.Orange:    return new Color(1.000f, 0.502f, 0.000f);
				case Kolor.Red:       return new Color(1.000f, 0.200f, 0.200f);
				case Kolor.PosRate:   return new Color(0.533f, 1.000f, 0.000f);
				case Kolor.NegRate:   return new Color(1.000f, 0.502f, 0.000f);
				case Kolor.Science:   return new Color(0.427f, 0.812f, 0.965f);
				case Kolor.Cyan:      return new Color(0.000f, 1.000f, 1.000f);
				case Kolor.LightGrey: return new Color(0.800f, 0.800f, 0.800f);
				case Kolor.DarkGrey:  return new Color(0.600f, 0.600f, 0.600f);
				default:              return new Color(1.000f, 1.000f, 1.000f);
			}
		}

		///<summary>return string with the specified color and bold if stated</summary>
		public static string Color(string s, Kolor color, bool bold = false)
		{
			return !bold ? BuildString("<color=", KolorToHex(color), ">", s, "</color>") : BuildString("<color=", KolorToHex(color), "><b>", s, "</b></color>");
		}

		///<summary>return string with different colors depending on the specified condition. "KColor.Default" will not apply any coloring</summary>
		public static string Color(bool condition, string s, Kolor colorIfTrue, Kolor colorIfFalse = Kolor.None, bool bold = false)
		{
			return condition ? Color(s, colorIfTrue, bold) : colorIfFalse == Kolor.None ? bold ? Bold(s) : s : Color(s, colorIfFalse, bold);
		}

		///<summary>return different colored strings depending on the specified condition. "KColor.Default" will not apply any coloring</summary>
		public static string Color(bool condition, string sIfTrue, Kolor colorIfTrue, string sIfFalse, Kolor colorIfFalse = Kolor.None, bool bold = false)
		{
			return condition ? Color(sIfTrue, colorIfTrue, bold) : colorIfFalse == Kolor.None ? bold ? Bold(sIfFalse) : sIfFalse : Color(sIfFalse, colorIfFalse, bold);
		}

		///<summary>return string in bold</summary>
		public static string Bold(string s)
		{
			return BuildString("<b>", s, "</b>");
		}


		///<summary>return string in italic</summary>
		public static string Italic(string s)
		{
			return BuildString("<i>", s, "</i>");
		}

		///<summary>add spaces on caps</summary>
		public static string SpacesOnCaps(string s)
		{
			return System.Text.RegularExpressions.Regex.Replace(s, "[A-Z]", " $0").TrimStart();
		}

		///<summary>convert to smart_case</summary>
		public static string SmartCase(string s)
		{
			return SpacesOnCaps(s).ToLower().Replace(' ', '_');
		}

		///<summary>converts_from_this to this</summary>
		public static string SpacesOnUnderscore(string s)
		{
			return s.Replace('_', ' ');
		}


		///<summary>select a string at random</summary>
		public static string TextVariant(params string[] list)
		{
			return list.Length == 0 ? string.Empty : list[RandomInt(list.Length)];
		}


		/// <summary> insert lines break to have a max line length of 'maxCharPerLine' characters </summary>
		public static string WordWrapAtLength(string longText, int maxCharPerLine)
		{

			longText = longText.Replace("\n", "");
			int currentPosition = 0;
			int textLength = longText.Length;
			while (true)
			{
				// if the remaining text is shorter that maxCharPerLine, return.
				if (currentPosition + maxCharPerLine >= textLength)
					break;

				// get position of first space before maxCharPerLine
				int nextSpacePosition = longText.LastIndexOf(' ', currentPosition + maxCharPerLine);

				// we found a space in the next line, replace it with a new line
				if (nextSpacePosition > currentPosition)
				{
					char[] longTextArray = longText.ToCharArray();
					longTextArray[nextSpacePosition] = '\n';
					longText = new string(longTextArray);
					currentPosition = nextSpacePosition;

				}
				// else break the word
				else
				{
					nextSpacePosition = currentPosition + maxCharPerLine;
					longText = longText.Insert(nextSpacePosition, "-\n");
					textLength += 2;
					currentPosition = nextSpacePosition + 2;
				}
			}
			return longText;

		}
		#endregion

		#region BUILD STRING
		// compose a set of strings together, without creating temporary objects
		// note: the objective here is to minimize number of temporary variables for GC
		// note: okay to call recursively, as long as all individual concatenation is atomic
		static StringBuilder sb = new StringBuilder(256);
		public static string BuildString(string a, string b)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			return sb.ToString();
		}
		public static string BuildString(string a, string b, string c)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			return sb.ToString();
		}
		public static string BuildString(string a, string b, string c, string d)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			return sb.ToString();
		}
		public static string BuildString(string a, string b, string c, string d, string e)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			return sb.ToString();
		}
		public static string BuildString(string a, string b, string c, string d, string e, string f)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			return sb.ToString();
		}
		public static string BuildString(string a, string b, string c, string d, string e, string f, string g)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			sb.Append(g);
			return sb.ToString();
		}
		public static string BuildString(string a, string b, string c, string d, string e, string f, string g, string h)
		{
			sb.Length = 0;
			sb.Append(a);
			sb.Append(b);
			sb.Append(c);
			sb.Append(d);
			sb.Append(e);
			sb.Append(f);
			sb.Append(g);
			sb.Append(h);
			return sb.ToString();
		}
		public static string BuildString(params string[] args)
		{
			sb.Length = 0;
			foreach (string s in args) sb.Append(s);
			return sb.ToString();
		}
		#endregion

		#region HUMAN READABLE

		public const string InlineSpriteScience = "<sprite=\"CurrencySpriteAsset\" name=\"Science\" color=#6DCFF6>";
		public const string InlineSpriteFunds = "<sprite=\"CurrencySpriteAsset\" name=\"Funds\" color=#B4D455>";
		public const string InlineSpriteReputation = "<sprite=\"CurrencySpriteAsset\" name=\"Reputation\" color=#E0D503>";
		public const string InlineSpriteFlask = "<sprite=\"CurrencySpriteAsset\" name=\"Flask\" color=#CE5DAE>";

		///<summary> Pretty-print a resource rate (rate is per second). Return an absolute value if a negative one is provided</summary>
		public static string HumanReadableRate(double rate, string precision = "F3")
		{
			if (rate == 0.0) return Local.Generic_NONE;//"none"
			rate = Math.Abs(rate);
			if (rate >= 0.01) return BuildString(rate.ToString(precision), Local.Generic_perSecond);//"/s"
			rate *= 60.0; // per-minute
			if (rate >= 0.01) return BuildString(rate.ToString(precision), Local.Generic_perMinute);//"/m"
			rate *= 60.0; // per-hour
			if (rate >= 0.01) return BuildString(rate.ToString(precision), Local.Generic_perHour);//"/h"
			rate *= HoursInDay;  // per-day
			if (rate >= 0.01) return BuildString(rate.ToString(precision), Local.Generic_perDay);//"/d"
			return BuildString((rate * DaysInYear).ToString(precision), Local.Generic_perYear);//"/y"
		}

		///<summary> Pretty-print a duration (duration is in seconds, must be positive) </summary>
		public static string HumanReadableDuration(double d, bool fullprecison = false)
		{
			if (!fullprecison)
			{
				if (double.IsInfinity(d) || double.IsNaN(d)) return Local.Generic_PERPETUAL;//"perpetual"
				d = Math.Round(d);
				if (d <= 0.0) return Local.Generic_NONE;//"none"

				ulong hours_in_day = (ulong)HoursInDay;
				ulong days_in_year = (ulong)DaysInYear;
				ulong duration_seconds = (ulong)d;

				// seconds
				if (d < 60.0)
				{
					ulong seconds = duration_seconds % 60ul;
					return BuildString(seconds.ToString(), "s");
				}
				// minutes + seconds
				if (d < 3600.0)
				{
					ulong seconds = duration_seconds % 60ul;
					ulong minutes = (duration_seconds / 60ul) % 60ul;
					return BuildString(minutes.ToString(), "m ", seconds.ToString("00"), "s");
				}
				// hours + minutes
				if (d < 3600.0 * HoursInDay)
				{
					ulong minutes = (duration_seconds / 60ul) % 60ul;
					ulong hours = (duration_seconds / 3600ul) % hours_in_day;
					return BuildString(hours.ToString(), "h ", minutes.ToString("00"), "m");
				}
				ulong days = (duration_seconds / (3600ul * hours_in_day)) % days_in_year;
				// days + hours
				if (d < 3600.0 * HoursInDay * DaysInYear)
				{
					ulong hours = (duration_seconds / 3600ul) % hours_in_day;
					return BuildString(days.ToString(), "d ", hours.ToString(), "h");
				}
				// years + days
				ulong years = duration_seconds / (3600ul * hours_in_day * days_in_year);
				return BuildString(years.ToString(), "y ", days.ToString(), "d");
			}
			else
			{
				if (double.IsInfinity(d) || double.IsNaN(d)) return Local.Generic_NEVER;//"never"
				d = Math.Round(d);
				if (d <= 0.0) return Local.Generic_NONE;//"none"

				double hours_in_day = HoursInDay;
				double days_in_year = DaysInYear;

				long duration = (long)d;
				long seconds = duration % 60;
				duration /= 60;
				long minutes = duration % 60;
				duration /= 60;
				long hours = duration % (long)hours_in_day;
				duration /= (long)hours_in_day;
				long days = duration % (long)days_in_year;
				long years = duration / (long)days_in_year;

				string result = string.Empty;
				if (years > 0) result += years + "y ";
				if (years > 0 || days > 0) result += days + "d ";
				if (years > 0 || days > 0 || hours > 0) result += hours.ToString("D2") + ":";
				if (years > 0 || days > 0 || hours > 0 || minutes > 0) result += minutes.ToString("D2") + ":";
				result += seconds.ToString("D2");

				return result;
			}
		}

		public static string HumanReadableCountdown(double duration, bool compact = false)
		{
			return BuildString("T-", HumanReadableDuration(duration, !compact));
		}

		///<summary> Pretty-print a range (range is in meters) </summary>
		public static string HumanReadableDistance(double distance)
		{
            if (distance == 0.0) return Local.Generic_NONE;//"none"
            if (distance < 0.0) return Lib.BuildString("-", HumanReadableDistance(-distance));
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " m");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " Km");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " Mm");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " Gm");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " Tm");
			distance /= 1000.0;
			if (distance < 1000.0) return BuildString(distance.ToString("F1"), " Pm");
			distance /= 1000.0;
			return BuildString(distance.ToString("F1"), " Em");
		}

		///<summary> Pretty-print a speed (in meters/sec) </summary>
		public static string HumanReadableSpeed(double speed)
		{
			return Lib.BuildString(HumanReadableDistance(speed), "/s");
		}

		///<summary> Pretty-print temperature </summary>
		public static string HumanReadableTemp(double temp)
		{
			return BuildString(temp.ToString("F1"), " K");
		}

		///<summary> Pretty-print angle </summary>
		public static string HumanReadableAngle(double angle)
		{
			return BuildString(angle >= 0.0001 ? angle.ToString("F1") : "0", " °");
		}

		///<summary> Pretty-print flux </summary>
		public static string HumanReadableFlux(double flux)
		{
			return BuildString(flux >= 0.0001 ? flux.ToString("F1") : flux.ToString(), " W/m²");
		}

		///<summary> Pretty-print percentage </summary>
		public static string HumanReadablePerc(double v, string format = "F0")
		{
			return BuildString((v * 100.0).ToString(format), "%");
		}

		///<summary> Pretty-print pressure (value is in kPa) </summary>
		public static string HumanReadablePressure(double v)
		{
			return Lib.BuildString(v.ToString("F1"), " kPa");
		}

		///<summary> Pretty-print volume (value is in m^3) </summary>
		public static string HumanReadableVolume(double v)
		{
			return Lib.BuildString(v.ToString("F2"), " m³");
		}

		///<summary> Pretty-print surface (value is in m^2) </summary>
		public static string HumanReadableSurface(double v)
		{
			return Lib.BuildString(v.ToString("F2"), " m²");
		}

		///<summary> Pretty-print mass </summary>
		public static string HumanReadableMass(double v)
		{
			if (v <= double.Epsilon) return "0 kg";
			if (v > 1) return Lib.BuildString(v.ToString("F3"), " t");
			v *= 1000;
			if (v > 1) return Lib.BuildString(v.ToString("F2"), " kg");
			v *= 1000;
			return Lib.BuildString(v.ToString("F2"), " g");
		}

		///<summary> Pretty-print cost </summary>
		public static string HumanReadableCost(double v)
		{
			return Lib.BuildString(v.ToString("F0"), " $");
		}

		///<summary> Format a value to 2 decimal places, or return 'none' </summary>
		public static string HumanReadableAmount(double value, string append = "")
		{
			return (Math.Abs(value) <= double.Epsilon ? Local.Generic_NONE : BuildString(value.ToString("F2"), append));//"none"
		}

		///<summary> Format an integer value, or return 'none' </summary>
		public static string HumanReadableInteger(uint value, string append = "")
		{
			return (Math.Abs(value) <= 0 ? Local.Generic_NONE : BuildString(value.ToString("F0"), append));//"none"
		}
		// Note : config / code base unit for data rate / size is in megabyte (1000^2 bytes)
		// For UI purposes we use the decimal units (B/kB/MB...), not the binary (1024^2 bytes) units
		public const double BPerMB = 1000.0 * 1000.0;
		public const double kBPerMB = 1000.0;
		public const double GBPerMB = 1.0 / 1000.0;
		public const double TBPerMB = 1.0 / (1000.0 * 1000.0);

		public const double MBPerBTenth = 1.0 / (1000.0 * 1000.0 * 10.0);
		public const double MBPerkB = 1.0 / 1000.0;
		public const double MBPerGB = 1000.0;
		public const double MBPerTB = 1000.0 * 1000.0;

		///<summary> Format data size, the size parameter is in MB (megabytes) </summary>
		public static string HumanReadableDataSize(double size)
		{
			if (size < MBPerBTenth)  // min size is 0.1 byte
				return Local.Generic_NONE;//"none"
			if (size < MBPerkB)
				return (size * BPerMB).ToString("0.0 B");
			if (size < 1.0)
				return (size * kBPerMB).ToString("0.00 kB");
			if (size < MBPerGB)
				return size.ToString("0.00 MB");
			if (size < MBPerTB)
				return (size * GBPerMB).ToString("0.00 GB");

			return (size * TBPerMB).ToString("0.00 TB");
		}

		///<summary> Format data rate, the rate parameter is in MB/s </summary>
		public static string HumanReadableDataRate(double rate)
		{
			if (rate < MBPerBTenth)  // min rate is 0.1 byte/s
				return Local.Generic_NONE;//"none"
			if (rate < MBPerkB)
				return (rate * BPerMB).ToString("0.0 B/s");
			if (rate < 1.0)
				return (rate * kBPerMB).ToString("0.00 kB/s");
			if (rate < MBPerGB)
				return rate.ToString("0.00 MB/s");
			if (rate < MBPerTB)
				return (rate * GBPerMB).ToString("0.00 GB/s");

			return (rate * TBPerMB).ToString("0.00 TB/s");
		}

		public static string HumanReadableSampleSize(double size)
		{
			return HumanReadableSampleSize(SampleSizeToSlots(size));
		}

		public static string HumanReadableSampleSize(int slots)
		{
			if (slots <= 0) return Lib.BuildString(Local.Generic_NO, Local.Generic_SLOT);//"no "

			return Lib.BuildString(slots.ToString(), " ", slots > 1 ? Local.Generic_SLOTS : Local.Generic_SLOT);
		}

		public static int SampleSizeToSlots(double size)
		{
			int result = (int)(size / 1024);
			if (result * 1024 < size) ++result;
			return result;
		}

		public static double SlotsToSampleSize(int slots)
		{
			return slots * 1024;
		}

		///<summary> Format science credits </summary>
		public static string HumanReadableScience(double value, bool compact = true)
		{
			if (compact)
				return Lib.Color(value.ToString("F1"), Kolor.Science, true);
			else
				return Lib.Color(Lib.BuildString(value.ToString("F1"), " ", Local.SCIENCEARCHIVE_CREDITS), Kolor.Science);//CREDITS

		}
#endregion

		#region GAME LOGIC
		///<summary>return true if the current scene is flight</summary>
		public static bool IsFlight()
		{
			return HighLogic.LoadedSceneIsFlight;
		}

		///<summary>return true if the current scene is editor</summary>
		public static bool IsEditor()
		{
			return HighLogic.LoadedSceneIsEditor;
		}

		///<summary>return true if the current scene is not the main menu</summary>
		public static bool IsGame()
		{
			return HighLogic.LoadedSceneIsGame;
		}

		///<summary>return true if game is paused</summary>
		public static bool IsPaused()
		{
			return FlightDriver.Pause || Planetarium.Pause;
		}

		///<summary>return true if a tutorial scenario or making history mission is active</summary>
		public static bool IsScenario()
		{
			return HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO
				|| HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO_NON_RESUMABLE
				|| HighLogic.CurrentGame.Mode == Game.Modes.MISSION_BUILDER
				|| HighLogic.CurrentGame.Mode == Game.Modes.MISSION;
		}

		///<summary>disable the module and return true if a tutorial scenario is active</summary>
		public static bool DisableScenario(PartModule m)
		{
			if (IsScenario())
			{
				m.enabled = false;
				m.isEnabled = false;
				return true;
			}
			return false;
		}

		///<summary>if current game is neither science or career, disable the module and return false</summary>
		public static bool ModuleEnableInScienceAndCareer(PartModule m)
		{
			switch (HighLogic.CurrentGame.Mode)
			{
				case Game.Modes.CAREER:
				case Game.Modes.SCIENCE_SANDBOX:
					return true;
				default:
					m.enabled = false;
					m.isEnabled = false;
					return false;
			}
		}
#endregion

		#region BODY

		/// <summary>For a given body, return the last parent body that is not a sun </summary>
		public static CelestialBody GetParentPlanet(CelestialBody body)
		{
			if (Lib.IsSun(body)) return body;
			CelestialBody checkedBody = body;
			while (!Lib.IsSun(checkedBody.referenceBody)) checkedBody = checkedBody.referenceBody;
			return checkedBody;
		}

		/// <summary> optimized method for getting normalized direction and distance between the surface of two bodies</summary>
		/// <param name="direction">normalized vector 'from' body 'to' body</param>
		/// <param name="distance">distance between the body surface</param>
		public static void DirectionAndDistance(CelestialBody from, CelestialBody to, out Vector3d direction, out double distance)
		{
			Lib.DirectionAndDistance(from.position, to.position, out direction, out distance);
			distance -= from.Radius + to.Radius;
		}

		/// <summary> optimized method for getting normalized direction and distance between a world position and the surface of a body</summary>
		/// <param name="direction">normalized vector 'from' position 'to' body</param>
		/// <param name="distance">distance to the body surface</param>
		public static void DirectionAndDistance(Vector3d from, CelestialBody to, out Vector3d direction, out double distance)
		{
			Lib.DirectionAndDistance(from, to.position, out direction, out distance);
			distance -= to.Radius;
		}

		/// <summary> optimized method for getting normalized direction and distance between two world positions</summary>
		/// <param name="direction">normalized vector 'from' position 'to' position</param>
		/// <param name="distance">distance between the body surface</param>
		public static void DirectionAndDistance(Vector3d from, Vector3d to, out Vector3d direction, out double distance)
		{
			direction = to - from;
			distance = direction.magnitude;
			direction /= distance;
		}

		/// <summary> Is this body a sun ? </summary>
		public static bool IsSun(CelestialBody body)
		{
			return Sim.suns.Exists(p => p.bodyIndex == body.flightGlobalsIndex);
		}

		/// <summary> return the first found parent sun for a given body </summary>
		public static CelestialBody GetParentSun(CelestialBody body)
		{
			if (IsSun(body)) return body;

			CelestialBody refBody = body.referenceBody;
			do
			{
				if (IsSun(refBody)) return refBody;
				refBody = refBody.referenceBody;
			}
			while (refBody != null);

			return FlightGlobals.Bodies[0];
		}

		///<summary
		/// return selected body in tracking-view/map-view
		/// >if a vessel is selected, return its main body
		///</summary>
		public static CelestialBody MapViewSelectedBody()
		{
			var target = PlanetariumCamera.fetch.target;
			return
				target == null ? null : target.celestialBody ?? target.vessel?.mainBody;
		}

		/* this appears to be broken / working unreliably, use a raycast instead
		/// <summary
		/// return terrain height at point specified
		///- body terrain must be loaded for this to work: use it only for loaded vessels
		/// </summary>
		public static double TerrainHeight(CelestialBody body, Vector3d pos)
		{
			PQS pqs = body.pqsController;
			if (pqs == null) return 0.0;
			Vector2d latlong = body.GetLatitudeAndLongitude(pos);
			Vector3d radial = QuaternionD.AngleAxis(latlong.y, Vector3d.down) * QuaternionD.AngleAxis(latlong.x, Vector3d.forward) * Vector3d.right;
			return (pos - body.position).magnitude - pqs.GetSurfaceHeight(radial);
		}
		*/
		#endregion

		#region VESSEL
		///<summary>return true if landed somewhere</summary>
		public static bool Landed(Vessel v)
		{
			if (v.loaded) return v.Landed || v.Splashed;
			else return v.protoVessel.landed || v.protoVessel.splashed;
		}

		///<summary>return vessel position</summary>
		public static Vector3d VesselPosition(Vessel v)
		{
			// the issue
			//   - GetWorldPos3D() return mainBody position for a few ticks after scene changes
			//   - we can detect that, and fall back to evaluating position from the orbit
			//   - orbit is not valid if the vessel is landed, and for a tick on prelaunch/staging/decoupling
			//   - evaluating position from latitude/longitude work in all cases, but is probably the slowest method

			// get vessel position
			Vector3d pos = v.GetWorldPos3D();

			// during scene changes, it will return mainBody position
			if (Vector3d.SqrMagnitude(pos - v.mainBody.position) < 1.0)
			{
				// try to get it from orbit
				pos = v.orbit.getPositionAtUT(Planetarium.GetUniversalTime());

				// if the orbit is invalid (landed, or 1 tick after prelaunch/staging/decoupling)
				if (double.IsNaN(pos.x))
				{
					// get it from lat/long (work even if it isn't landed)
					pos = v.mainBody.GetWorldSurfacePosition(v.latitude, v.longitude, v.altitude);
				}
			}

			// victory
			return pos;
		}


		///<summary>return set of crew on a vessel. Works on loaded and unloaded vessels</summary>
		public static List<ProtoCrewMember> CrewList(Vessel v)
		{
			return v.loaded ? v.GetVesselCrew() : v.protoVessel.GetVesselCrew();
		}

		///<summary>return crew count of a vessel. Works on loaded and unloaded vessels</summary>
		public static int CrewCount(Vessel v)
		{
			return v.isEVA ? 1 : CrewList(v).Count;
		}

		///<summary>return crew count of a protovessel</summary>
		public static int CrewCount(ProtoVessel pv)
		{
			return pv.vesselType == VesselType.EVA ? 1 : pv.GetVesselCrew().Count();
		}

		///<summary>return crew capacity of a vessel</summary>
		public static int CrewCapacity(Vessel v)
		{
			if (v.isEVA) return 1;
			if (v.loaded)
			{
				return v.GetCrewCapacity();
			}
			else
			{
				int capacity = 0;
				foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
				{
					capacity += p.partInfo.partPrefab.CrewCapacity;
				}
				return capacity;
			}
		}


		///<summary>return true if this is a 'vessel'</summary>
		public static bool IsVessel(Vessel v)
		{
			// something weird is going on
			if (v == null) return false;

			// if the vessel is in DEAD status, we consider it invalid
			if (v.state == Vessel.State.DEAD) return false;

			// if the vessel is a debris, a flag or an asteroid, ignore it
			// - the user can change vessel type, in that case he is actually disabling this mod for the vessel
			//   the alternative is to scan the vessel for ModuleCommand, but that is slower, and rescue vessels have no module command
			// - flags have type set to 'station' for a single update, can still be detected as they have vesselID == 0
			switch(v.vesselType)
			{
				case VesselType.Debris:
				case VesselType.Flag:
				case VesselType.SpaceObject:
				case VesselType.Unknown:
#if !KSP15_16
				case VesselType.DeployedSciencePart:
#endif
					return false;
			}

			// [disabled] when going to eva (and possibly other occasions), for a single update the vessel is not properly set
			// this can be detected by vessel.distanceToSun being 0 (an impossibility otherwise)
			// in this case, just wait a tick for the data being set by the game engine
			// if (v.loaded && v.distanceToSun <= double.Epsilon)
			//	return false;

			//
			//if (!v.loaded && v.protoVessel == null)
			//	continue;

			// the vessel is valid
			return true;
		}



		public static bool IsControlUnit(Vessel v)
		{
#if !KSP15_16
			return Serenity.GetScienceCluster(v) != null;
#else
			return false;
#endif
		}

		public static bool IsPowered(Vessel v)
		{
#if !KSP15_16
			var cluster = Serenity.GetScienceCluster(v);
			if (cluster != null)
				return cluster.IsPowered;
#endif
			return ResourceCache.GetResource(v, "ElectricCharge").Amount > double.Epsilon;
		}

		public static Guid VesselID(Vessel v)
		{
			// Lesson learned: v.persistendId is not unique. Far from it, in fact.

			// neither is this ----vvv (see https://github.com/steamp0rt/Kerbalism/issues/370)
			//byte[] b = v.id.ToByteArray();
			//UInt64 result = BitConverter.ToUInt64(b, 0);
			//result ^= BitConverter.ToUInt64(b, 8);
			//return result;
			// --------------------^^^

			// maybe this?
			// return RootID(v); // <-- nope. not unique.
			return v.id;
		}

		public static Guid VesselID(ProtoVessel pv)
		{
			// nope
			//byte[] b = pv.vesselID.ToByteArray();
			//UInt64 result = BitConverter.ToUInt64(b, 0);
			//result ^= BitConverter.ToUInt64(b, 8);
			//return result;
			//return pv.protoPartSnapshots[pv.rootIndex].flightID;
			return pv.vesselID;
		}

		public static Vessel CommNodeToVessel(CommNode node)
		{
			// Iterating over all vessels will work for recovering the vessel from a CommNode.However,
			// since CommNodes are created when Vessels are, you can almost certainly cache this in a
			// reasonable manner.
			// (Vessel creates a CommNetVessel which creates the CommNode.They're established no
			// later than OnStart())
			// We would either need something to monitor new Vessel creation (ie after staging events)
			// OR you want a fallback for cache misses.

			// Is is home return null
			if (node.isHome) return null;

			foreach (Vessel v in FlightGlobals.Vessels)
			{
				if (!IsVessel(v)) continue;

				if (AreSame(node, v.connection.Comm))
				{
					return v;
				}
			}
			return null;
		}

		public static bool AreSame(CommNode a, CommNode b)
		{
			if (a == null || b == null)
			{
				return false;
			}

			return a.precisePosition == b.precisePosition;
		}
		#endregion

		#region PART
		///<summary>get list of parts recursively, useful from the editors</summary>
		public static List<Part> GetPartsRecursively(Part root)
		{
			List<Part> ret = new List<Part>
			{
				root
			};
			foreach (Part p in root.children)
			{
				ret.AddRange(GetPartsRecursively(p));
			}
			return ret;
		}

		///<summary>return the name (not the title) of a part</summary>
		public static string PartName(Part p)
		{
			return p.partInfo.name;
		}

		public static int CrewCount(Part part)
		{
			// outside of the editors, it is easy
			if (!Lib.IsEditor())
			{
				return part.protoModuleCrew.Count;
			}

			// in the editor we need something more involved
			Int64 part_id = 4294967296L + part.GetInstanceID();
			var manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
			var part_manifest = manifest.GetCrewableParts().Find(k => k.PartID == part_id);
			if (part_manifest != null)
			{
				int result = 0;
				foreach (var s in part_manifest.partCrew)
				{
					if (!string.IsNullOrEmpty(s)) result++;
				}
				return result;
			}

			return 0;
		}

		///<summary>return true if a part is manned, even in the editor</summary>
		public static bool IsCrewed(Part p)
		{
			return CrewCount(p) > 0;
		}

		/// <summary>
		/// In the editor, remove the symmetry constraint for this part and its symmetric counterparts. 
		/// This method is available in stock (Part.RemoveFromSymmetry()) since 1.7.2, copied here for 1.4-1.6 compatibility
		/// </summary>
		public static void EditorClearSymmetry(Part part)
		{
			part.CleanSymmetryReferences();
			if (part.stackIcon != null)
			{
				part.stackIcon.RemoveIcon();
				part.stackIcon.CreateIcon();
				if (StageManager.Instance != null) StageManager.Instance.SortIcons(true);
			}
			EditorLogic.fetch.SetBackup();
		}

		#endregion

		#region PART VOLUME/SURFACE

		/// <summary>
		/// return the volume of a part bounding box, in m^3
		/// note: this can only be called when part has not been rotated
		/// </summary>
		public static double PartBoundsVolume(Part p, bool applyCylinderFactor = false)
		{
			return applyCylinderFactor ? BoundsVolume(GetPartBounds(p)) * 0.785398 : BoundsVolume(GetPartBounds(p));
		}

		/// <summary>
		/// return the surface of a part bounding box, in m^2
		/// note: this can only be called when part has not been rotated
		/// </summary>
		public static double PartBoundsSurface(Part p, bool applyCylinderFactor = false)
		{
			return applyCylinderFactor ? BoundsSurface(GetPartBounds(p)) * 0.95493 : BoundsSurface(GetPartBounds(p));
		}

		public static double BoundsVolume(Bounds bb)
		{
			Vector3 size = bb.size;
			return size.x * size.y * size.z;
		}

		public static double BoundsSurface(Bounds bb)
		{
			Vector3 size = bb.size;
			double a = size.x;
			double b = size.y;
			double c = size.z;
			return 2.0 * (a * b + a * c + b * c);
		}

		public static double BoundsIntersectionVolume(Bounds a, Bounds b)
		{
			Vector3 aMin = a.min;
			Vector3 aMax = a.max;
			Vector3 bMin = b.min;
			Vector3 bMax = b.max;

			Vector3 intersectionSize = default;
			intersectionSize.x = Math.Max(Math.Min(aMax.x, bMax.x) - Math.Max(aMin.x, bMin.x), 0f);
			intersectionSize.y = Math.Max(Math.Min(aMax.y, bMax.y) - Math.Max(aMin.y, bMin.y), 0f);
			intersectionSize.z = Math.Max(Math.Min(aMax.z, bMax.z) - Math.Max(aMin.z, bMin.z), 0f);

			return intersectionSize.x * intersectionSize.y * intersectionSize.z;
		}

		/// <summary>
		/// Get the part currently active geometry bounds. Similar to the Part.GetPartRendererBound() method but don't account for inactive renderers.
		/// Note : bounds are world axis aligned, meaning they will change if the part is rotated.
		/// </summary>
		public static Bounds GetPartBounds(Part part) => GetTransformRootAndChildrensBounds(part.transform);

		private static Bounds GetTransformRootAndChildrensBounds(Transform transform)
		{
			Bounds bounds = default;
			Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(false);

			bool firstRenderer = true;
			foreach (Renderer renderer in renderers)
			{
				if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
					continue;

				if (firstRenderer)
				{
					bounds = renderer.bounds;
					firstRenderer = false;
					continue;
				}
				bounds.Encapsulate(renderer.bounds);
			}

			return bounds;
		}

		public class PartVolumeAndSurfaceInfo
		{
			public VolumeAndSurfaceMethod bestMethod = VolumeAndSurfaceMethod.Best;

			public double boundsVolume = 0.0;
			public double boundsSurface = 0.0;

			public double colliderVolume = 0.0;
			public double colliderSurface = 0.0;

			public double meshVolume = 0.0;
			public double meshSurface = 0.0;

			public double attachNodesSurface = 0.0;

			public PartVolumeAndSurfaceInfo() { }

			public PartVolumeAndSurfaceInfo(ConfigNode node)
			{
				bestMethod = Lib.ConfigEnum(node, "bestMethod", VolumeAndSurfaceMethod.Best);
				boundsVolume = Lib.ConfigValue(node, "boundsVolume", 0.0);
				boundsSurface = Lib.ConfigValue(node, "boundsSurface", 0.0);
				colliderVolume = Lib.ConfigValue(node, "colliderVolume", 0.0);
				colliderSurface = Lib.ConfigValue(node, "colliderSurface", 0.0);
				meshVolume = Lib.ConfigValue(node, "meshVolume", 0.0);
				meshSurface = Lib.ConfigValue(node, "meshSurface", 0.0);
				attachNodesSurface = Lib.ConfigValue(node, "attachNodesSurface", 0.0);
			}

			public void Save(ConfigNode node)
			{
				node.AddValue("bestMethod", bestMethod.ToString());
				node.AddValue("boundsVolume", boundsVolume.ToString("G17"));
				node.AddValue("boundsSurface", boundsSurface.ToString("G17"));
				node.AddValue("colliderVolume", colliderVolume.ToString("G17"));
				node.AddValue("colliderSurface", colliderSurface.ToString("G17"));
				node.AddValue("meshVolume", meshVolume.ToString("G17"));
				node.AddValue("meshSurface", meshSurface.ToString("G17"));
				node.AddValue("attachNodesSurface", attachNodesSurface.ToString("G17"));
			}

			public void GetUsingBestMethod(out double volume, out double surface, bool substractAttachNodesSurface = true)
			{
				GetUsingMethod(bestMethod, out volume, out surface, substractAttachNodesSurface);
			}

			public void GetUsingMethod(VolumeAndSurfaceMethod method, out double volume, out double surface, bool substractAttachNodesSurface = true)
			{
				switch (method)
				{
					case VolumeAndSurfaceMethod.Bounds:
						volume = boundsVolume;
						surface = substractAttachNodesSurface ? SubstractNodesSurface(boundsSurface, attachNodesSurface) : boundsSurface;
						return;
					case VolumeAndSurfaceMethod.Collider:
						volume = colliderVolume;
						surface = substractAttachNodesSurface ? SubstractNodesSurface(colliderSurface, attachNodesSurface) : colliderSurface;
						return;
					case VolumeAndSurfaceMethod.Mesh:
						volume = meshVolume;
						surface = substractAttachNodesSurface ? SubstractNodesSurface(meshSurface, attachNodesSurface) : meshSurface;
						return;
					default:
						volume = 0.0;
						surface = 0.0;
						return;
				}
			}

			private double SubstractNodesSurface(double surface, double nodesSurface)
			{
				return Math.Max(surface * 0.5, surface - nodesSurface);
			}
		}

		public enum VolumeAndSurfaceMethod
		{
			Best = 0,
			Bounds,
			Collider,
			Mesh
		}

		private struct MeshInfo : IEquatable<MeshInfo>
		{
			public string name;
			public double volume;
			public double surface;
			public Bounds bounds;
			public double boundsVolume;

			public MeshInfo(string name, double volume, double surface, Bounds bounds)
			{
				this.name = name;
				this.volume = volume;
				this.surface = surface;
				this.bounds = bounds;
				boundsVolume = bounds.size.x * bounds.size.y * bounds.size.z;
			}

			public override string ToString()
			{
				return $"\"{name}\" : VOLUME={volume.ToString("0.00m3")} - SURFACE={surface.ToString("0.00m2")} - BOUNDS VOLUME={boundsVolume.ToString("0.00m3")}";
			}

			public bool Equals(MeshInfo other)
			{
				return volume == other.volume && surface == other.surface && bounds == other.bounds;
			}

			public override bool Equals(object obj) => Equals((MeshInfo)obj);

			public static bool operator ==(MeshInfo lhs, MeshInfo rhs) => lhs.Equals(rhs);

			public static bool operator !=(MeshInfo lhs, MeshInfo rhs) => !lhs.Equals(rhs);

			public override int GetHashCode() => volume.GetHashCode() ^ surface.GetHashCode() ^ bounds.GetHashCode();
		}

		// As a general rule, at least one of the two mesh based methods will return very accurate results.
		// This is very dependent on how the model is done. Specifically, results will be inaccurate in the following cases : 
		// - non closed meshes, larger holes = higher error
		// - overlapping meshes. Obviously any intersection will cause the volume/surface to be higher
		// - surface area will only be accurate in the case of a single mesh per part. A large number of meshes will result in very inaccurate surface evaluation.
		// - results may not be representative of the habitable volume if there are a lot of large structural or "technical" shapes like fuel tanks, shrouds, interstages, integrated engines, etc...

		// Note on surface : surface in kerbalism is meant as the surface of the habitat outer hull exposed to the environment,
		// that's why it make sense to substract the attach nodes area, as that surface will usually by covered by connnected parts.

		/// <summary>
		/// Estimate the part volume and surface by using 3 possible methods : 3D meshes, 3D collider meshes or axis aligned bounding box.
		/// Uses the currently enabled meshes/colliders, and will work with skinned meshes (inflatables).
		/// VERY SLOW, 20-100 ms per call, use it only once and cache the results
		/// </summary>
		/// <param name="part">An axis aligned part, with its geometry in the desired state (mesh switching / animations).</param>
		/// <param name="logAll">If true, the result of all 3 methods will be logged</param>
		/// <param name="ignoreSkinnedMeshes">If true, the volume/surface of deformable meshes (ex : inflatables) will be ignored</param>
		/// <param name="rootTransform">if specified, only bounds/meshes/colliders on this transform and its children will be used</param>
		/// <returns>surface/volume results for the 3 methods, and the best method to use</returns>
		public static PartVolumeAndSurfaceInfo GetPartVolumeAndSurface(
			Part part,
			bool logAll = false,
			bool ignoreSkinnedMeshes = false,
			Transform rootTransform = null)
		{

			if (rootTransform == null) rootTransform = part.transform;

			PartVolumeAndSurfaceInfo results = new PartVolumeAndSurfaceInfo();

			List<MeshInfo> meshInfos = GetPartMeshesVolumeAndSurface(rootTransform, ignoreSkinnedMeshes);
			int usedMeshCount = GetMeshesTotalVolumeAndSurface(meshInfos, out results.meshVolume, out results.meshSurface, logAll);


			// Note that we only account for mesh colliders and ignore any box/sphere/capsule collider because :
			// - they usually are used as an array of overlapping box colliders, giving very unreliable results
			// - they are often used for hollow geometry like trusses
			// - they are systematically used for a variety of non shape related things like ladders/handrails/hatches hitboxes (note that it is be possible to filter those by checking for the "Airlock" or "Ladder" tag on the gameobject)
			List<MeshInfo> colliderMeshInfos = GetPartMeshCollidersVolumeAndSurface(rootTransform);
			int usedCollidersCount = GetMeshesTotalVolumeAndSurface(colliderMeshInfos, out results.colliderVolume, out results.colliderSurface, logAll);

			Bounds partBounds = GetTransformRootAndChildrensBounds(rootTransform);
			results.boundsVolume = BoundsVolume(partBounds);
			results.boundsSurface = BoundsSurface(partBounds);

			// If volume is greater than 90% the bounds volume or less than 0.25 m3 it's obviously wrong
			double validityFactor = 0.9;
			bool colliderIsValid = results.colliderVolume < results.boundsVolume * validityFactor && results.colliderVolume > 0.25;
			bool meshIsValid = results.meshVolume < results.boundsVolume * validityFactor && results.meshVolume > 0.25;


				if (!colliderIsValid && !meshIsValid)
					results.bestMethod = VolumeAndSurfaceMethod.Bounds;
				else if (!colliderIsValid)
					results.bestMethod = VolumeAndSurfaceMethod.Mesh;
				else if (!meshIsValid)
					results.bestMethod = VolumeAndSurfaceMethod.Collider;
				else
				{
				// we consider that both methods are accurate if the volume difference is less than 10%
					double volumeDifference = Math.Abs(results.colliderVolume - results.meshVolume) / Math.Max(results.colliderVolume, results.meshVolume);

				// in case the returned volumes are similar, the method that use the less collider / mesh count will be more accurate for surface
				if (volumeDifference < 0.2 && (usedCollidersCount != usedMeshCount))
					results.bestMethod = usedCollidersCount < usedMeshCount ? VolumeAndSurfaceMethod.Collider : VolumeAndSurfaceMethod.Mesh;
				// in case the returned volumes are still not completely off from one another, favor the result that used only one mesh
				else if (volumeDifference < 0.75 && usedCollidersCount == 1 && usedMeshCount != 1)
					results.bestMethod = VolumeAndSurfaceMethod.Collider;
				else if (volumeDifference < 0.75 && usedMeshCount == 1 && usedCollidersCount != 1)
					results.bestMethod = VolumeAndSurfaceMethod.Mesh;
				// in other cases, the method that return the largest volume is usually right
				else
					results.bestMethod = results.colliderVolume > results.meshVolume ? VolumeAndSurfaceMethod.Collider : VolumeAndSurfaceMethod.Mesh;
				}

			foreach (AttachNode attachNode in part.attachNodes)
			{
				// its seems the standard way of disabling a node involve
				// reducing the rendered radius to 0.001f
				if (attachNode.radius < 0.1f)
					continue;

				switch (attachNode.size)
				{
					case 0: results.attachNodesSurface += 0.3068; break;// 0.625 m disc
					case 1: results.attachNodesSurface += 1.2272; break;// 1.25 m disc
					case 2: results.attachNodesSurface += 4.9090; break;// 2.5 m disc
					case 3: results.attachNodesSurface += 11.045; break;// 3.75 m disc
					case 4: results.attachNodesSurface += 19.635; break;// 5 m disc
				}
			}

			return results;
		}

		private static int GetMeshesTotalVolumeAndSurface(List<MeshInfo> meshInfos, out double volume, out double surface, bool logAll = false)
		{
			volume = 0.0;
			surface = 0.0;
			int usedMeshesCount = 0;

			if (meshInfos == null || meshInfos.Count() == 0)
				return usedMeshesCount;

			// sort the meshes by their volume, largest first
			meshInfos.Sort((x, y) => y.volume.CompareTo(x.volume));

			// only account for meshes that are have at least 25% the volume of the biggest mesh, or are at least 0.5 m3, whatever is smaller
			double minMeshVolume = Math.Min(meshInfos[0].volume * 0.25, 0.5);

			for (int i = 0; i < meshInfos.Count; i++)
			{
				MeshInfo meshInfo = meshInfos[i];

				// for each mesh bounding box, get the volume of all other meshes bounding boxes intersections
				double intersectedVolume = 0.0;
				foreach (MeshInfo otherMeshInfo in meshInfos)
				{
					if (meshInfo == otherMeshInfo)
						continue;

					// Don't account large meshes whose bounding box volume is greater than 3 times their mesh volume because
					// their bounding box contains too much empty space that may enclose anpther mesh.
					// Typical case : the torus mesh of a gravity ring will enclose the central core mesh
					if (otherMeshInfo.volume > 10.0 && otherMeshInfo.boundsVolume > otherMeshInfo.volume * 3.0)
						continue;

					intersectedVolume += BoundsIntersectionVolume(meshInfo.bounds, otherMeshInfo.bounds);
				}

				if (meshInfo.volume < minMeshVolume)
				{
					continue;
				}

				// exclude meshes whose intersected volume is greater than 75% their bounding box volume
				// always accept the first mesh (since it's the largest, we can assume it's other meshes that intersect it)
				if (i > 0 && intersectedVolume / meshInfo.boundsVolume > 0.75)
				{
					continue;
				}

				usedMeshesCount++;
				volume += meshInfo.volume;

				// account for the full surface of the biggest mesh, then only half for the others
				if (i == 0)
					surface += meshInfo.surface;
				else
					surface += meshInfo.surface * 0.5;
			}

			return usedMeshesCount;
		}

		private static List<MeshInfo> GetPartMeshesVolumeAndSurface(Transform partRootTransform, bool ignoreSkinnedMeshes)
		{
			List<MeshInfo> meshInfos = new List<MeshInfo>();

			if (!ignoreSkinnedMeshes)
			{
				SkinnedMeshRenderer[] skinnedMeshRenderers = partRootTransform.GetComponentsInChildren<SkinnedMeshRenderer>(false);
				for (int i = 0; i < skinnedMeshRenderers.Length; i++)
				{
					SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
					Mesh animMesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(animMesh);

					MeshInfo meshInfo = new MeshInfo(
						skinnedMeshRenderer.transform.name,
						MeshVolume(animMesh.vertices, animMesh.triangles),
						MeshSurface(animMesh.vertices, animMesh.triangles),
						skinnedMeshRenderer.bounds);

					meshInfos.Add(meshInfo);
				}
			}

			MeshFilter[] meshFilters = partRootTransform.GetComponentsInChildren<MeshFilter>(false);
			int count = meshFilters.Length;

			if (count == 0)
				return meshInfos;

			foreach (MeshFilter meshFilter in meshFilters)
			{
				// Ignore colliders
				if (meshFilter.gameObject.GetComponent<MeshCollider>() != null)
					continue;

				// Ignore non rendered meshes
				MeshRenderer renderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
				if (renderer == null || !renderer.enabled)
					continue;

				Mesh mesh = meshFilter.sharedMesh;
				Vector3 scaleVector = meshFilter.transform.lossyScale;
				float scale = scaleVector.x * scaleVector.y * scaleVector.z;

				Vector3[] vertices;
				if (scale != 1f)
					vertices = ScaleMeshVertices(mesh.vertices, scaleVector);
				else
					vertices = mesh.vertices;

				MeshInfo meshInfo = new MeshInfo(
					meshFilter.transform.name,
					MeshVolume(vertices, mesh.triangles),
					MeshSurface(vertices, mesh.triangles),
					renderer.bounds);

				meshInfos.Add(meshInfo);
			}

			return meshInfos;
		}

		private static List<MeshInfo> GetPartMeshCollidersVolumeAndSurface(Transform partRootTransform)
		{
			MeshCollider[] meshColliders = partRootTransform.GetComponentsInChildren<MeshCollider>(false);
			int count = meshColliders.Length;

			List<MeshInfo> meshInfos = new List<MeshInfo>(count);

			if (count == 0)
				return meshInfos;

			foreach (MeshCollider meshCollider in meshColliders)
			{
				Mesh mesh = meshCollider.sharedMesh;
				Vector3 scaleVector = meshCollider.transform.lossyScale;
				float scale = scaleVector.x * scaleVector.y * scaleVector.z;

				Vector3[] vertices;
				if (scale != 1f)
					vertices = ScaleMeshVertices(mesh.vertices, scaleVector);
				else
					vertices = mesh.vertices;

				MeshInfo meshInfo = new MeshInfo(
					meshCollider.transform.name,
					MeshVolume(vertices, mesh.triangles),
					MeshSurface(vertices, mesh.triangles),
					meshCollider.bounds);

				meshInfos.Add(meshInfo);
			}

			return meshInfos;
		}

		/// <summary>
		/// Scale a vertice array (note : this isn't enough to produce a valid unity mesh, would need to recalculate normals and UVs)
		/// </summary>
		private static Vector3[] ScaleMeshVertices(Vector3[] sourceVertices, Vector3 scale)
		{
			Vector3[] scaledVertices = new Vector3[sourceVertices.Length];
			for (int i = 0; i < sourceVertices.Length; i++)
			{
				scaledVertices[i] = new Vector3(
					sourceVertices[i].x * scale.x,
					sourceVertices[i].y * scale.y,
					sourceVertices[i].z * scale.z);
			}
			return scaledVertices;
		}

		/// <summary>
		/// Calculate a mesh surface in m^2. WARNING : slow
		/// Very accurate as long as the mesh is fully closed
		/// </summary>
		private static double MeshSurface(Vector3[] vertices, int[] triangles)
		{
			if (triangles.Length == 0)
				return 0.0;

			double sum = 0.0;

			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 corner = vertices[triangles[i]];
				Vector3 a = vertices[triangles[i + 1]] - corner;
				Vector3 b = vertices[triangles[i + 2]] - corner;

				sum += Vector3.Cross(a, b).magnitude;
			}

			return sum / 2.0;
		}

		/// <summary>
		/// Calculate a mesh volume in m^3. WARNING : slow
		/// Very accurate as long as the mesh is fully closed
		/// </summary>
		private static double MeshVolume(Vector3[] vertices, int[] triangles)
		{
			double volume = 0f;
			if (triangles.Length == 0)
				return volume;

			Vector3 o = new Vector3(0f, 0f, 0f);
			// Computing the center mass of the polyhedron as the fourth element of each mesh
			for (int i = 0; i < triangles.Length; i++)
			{
				o += vertices[triangles[i]];
			}
			o = o / triangles.Length;

			// Computing the sum of the volumes of all the sub-polyhedrons
			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 p1 = vertices[triangles[i + 0]];
				Vector3 p2 = vertices[triangles[i + 1]];
				Vector3 p3 = vertices[triangles[i + 2]];
				volume += SignedVolumeOfTriangle(p1, p2, p3, o);
			}
			return Math.Abs(volume);
		}

		private static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 o)
		{
			Vector3 v1 = p1 - o;
			Vector3 v2 = p2 - o;
			Vector3 v3 = p3 - o;

			return Vector3.Dot(Vector3.Cross(v1, v2), v3) / 6f; ;
		}
		#endregion

		#region MODULE
		///<summary>
		/// return all modules implementing a specific type in a vessel
		/// note: disabled modules are not returned
		/// </summary>
		public static List<T> FindModules<T>(Vessel v) where T : class
		{
			List<T> ret = new List<T>();
			for (int i = 0; i < v.parts.Count; ++i)
			{
				Part p = v.parts[i];
				for (int j = 0; j < p.Modules.Count; ++j)
				{
					PartModule m = p.Modules[j];
					if (m.isEnabled)
					{
						if (m is T t)
							ret.Add(t);
					}
				}
			}
			return ret;
		}

		public static bool HasPart(Vessel v, string part_name)
		{
			if (Cache.HasVesselObjectsCache(v, "has_part:" + part_name))
				return Cache.VesselObjectsCache<bool>(v, "has_part:" + part_name);

			bool ret = false;
			foreach(string name in Tokenize(part_name, ','))
			{
				if (v.loaded)
					ret = v.parts.Find(k => k.name.StartsWith(part_name, StringComparison.Ordinal)) != null;
				else
					ret = v.protoVessel.protoPartSnapshots.Find(k => k.partName.StartsWith(part_name, StringComparison.Ordinal)) != null;
				if (ret) break;
			}

			Cache.SetVesselObjectsCache(v, "has_part:" + part_name, ret);
			return ret;
		}

		/// <summary>
		/// return all proto modules with a specified name in a vessel.
		/// note: disabled modules are not returned
		/// </summary>
		public static List<ProtoPartModuleSnapshot> FindModules(ProtoVessel v, string module_name)
		{
			var ret = Cache.VesselObjectsCache<List<ProtoPartModuleSnapshot>>(v, "mod:" + module_name);
			if (ret != null)
				return ret;

			ret = new List<ProtoPartModuleSnapshot>(8);
			for (int i = 0; i < v.protoPartSnapshots.Count; ++i)
			{
				ProtoPartSnapshot p = v.protoPartSnapshots[i];
				ret.AddRange(FindModules(p, module_name));
			}

			Cache.SetVesselObjectsCache(v, "mod:" + module_name, ret);
			return ret;
		}

		///<summary>
		/// return all proto modules with a specified name in a part
		/// note: disabled modules are not returned
		/// </summary>
		public static List<ProtoPartModuleSnapshot> FindModules(ProtoPartSnapshot p, string module_name)
		{
			List<ProtoPartModuleSnapshot> ret = new List<ProtoPartModuleSnapshot>(8);
			for (int j = 0; j < p.modules.Count; ++j)
			{
				ProtoPartModuleSnapshot m = p.modules[j];
				if (m.moduleName == module_name && Proto.GetBool(m, "isEnabled"))
				{
					ret.Add(m);
				}
			}
			return ret;
		}

		///<summary>
		/// return true if a module implementing a specific type and satisfying the predicate specified exist in a vessel
		/// note: disabled modules are ignored
		///</summary>
		public static bool HasModule<T>(Vessel v, Predicate<T> filter) where T : class
		{
			for (int i = 0; i < v.parts.Count; ++i)
			{
				Part p = v.parts[i];
				for (int j = 0; j < p.Modules.Count; ++j)
				{
					PartModule m = p.Modules[j];
					if (m.isEnabled)
					{
						if (m is T t && filter(t))
							return true;
					}
				}
			}
			return false;
		}

		///<summary>
		/// return true if a proto module with the specified name and satisfying the predicate specified exist in a vessel
		///note: disabled modules are not returned
		///</summary>
		public static bool HasModule(ProtoVessel v, string module_name, Predicate<ProtoPartModuleSnapshot> filter)
		{
			for (int i = 0; i < v.protoPartSnapshots.Count; ++i)
			{
				ProtoPartSnapshot p = v.protoPartSnapshots[i];
				for (int j = 0; j < p.modules.Count; ++j)
				{
					ProtoPartModuleSnapshot m = p.modules[j];
					if (m.moduleName == module_name && Proto.GetBool(m, "isEnabled") && filter(m))
					{
						return true;
					}
				}
			}
			return false;
		}

		///<summary>used by ModulePrefab function, to support multiple modules of the same type in a part</summary>
		public sealed class Module_prefab_data
		{
			public int index;                         // index of current module of this type
			public List<PartModule> prefabs;          // set of module prefabs of this type
		}

		///<summary>
		/// get module prefab
		///  This function is used to solve the problem of obtaining a specific module prefab,
		/// and support the case where there are multiple modules of the same type in the part.
		/// </summary>
		public static PartModule ModulePrefab(List<PartModule> module_prefabs, string module_name, Dictionary<string, Module_prefab_data> PD)
		{
			// get data related to this module type, or create it
			Module_prefab_data data;
			if (!PD.TryGetValue(module_name, out data))
			{
				data = new Module_prefab_data
				{
					prefabs = module_prefabs.FindAll(k => k.moduleName == module_name)
				};
				PD.Add(module_name, data);
			}

			// return the module prefab, and increment module-specific index
			// note: if something messed up the prefab, or module were added dynamically,
			// then we have no chances of finding the module prefab so we return null
			return data.index < data.prefabs.Count ? data.prefabs[data.index++] : null;
		}
		#endregion

		#region RESOURCE
		/// <summary> Returns the amount of a resource in a part </summary>
		public static double Amount(Part part, string resource_name, bool ignore_flow = false)
		{
			foreach (PartResource res in part.Resources)
			{
				if ((res.flowState || ignore_flow) && res.resourceName == resource_name) return res.amount;
			}
			return 0.0;
		}

		/// <summary> Returns the capacity of a resource in a part </summary>
		public static double Capacity(Part part, string resource_name, bool ignore_flow = false)
		{
			foreach (PartResource res in part.Resources)
			{
				if ((res.flowState || ignore_flow) && res.resourceName == resource_name) return res.maxAmount;
			}
			return 0.0;
		}

		/// <summary> Returns the level of a resource in a part </summary>
		public static double Level(Part part, string resource_name, bool ignore_flow = false)
		{
			foreach (PartResource res in part.Resources)
			{
				if ((res.flowState || ignore_flow) && res.resourceName == resource_name)
				{
					return res.maxAmount > double.Epsilon ? res.amount / res.maxAmount : 0.0;
				}
			}
			return 0.0;
		}

		/// <summary> Adds the specified resource amount and capacity to a part,
		/// the resource is created if it doesn't already exist </summary>
		///<summary>poached from https://github.com/blowfishpro/B9PartSwitch/blob/master/B9PartSwitch/Extensions/PartExtensions.cs
		public static PartResource AddResource(Part p, string res_name, double amount, double capacity)
		{
			var reslib = PartResourceLibrary.Instance.resourceDefinitions;
			// if the resource is not known, log a warning and do nothing
			if (!reslib.Contains(res_name))
			{
				return null;
			}
			var resourceDefinition = reslib[res_name];

			amount = Math.Min(amount, capacity);
			amount = Math.Max(amount, 0);
			PartResource resource = p.Resources[resourceDefinition.name];

			if (resource == null)
			{
				resource = new PartResource(p);
				resource.SetInfo(resourceDefinition);
				resource.maxAmount = capacity;
				resource.amount = amount;
				resource.flowState = true;
				resource.isTweakable = resourceDefinition.isTweakable;
				resource.isVisible = resourceDefinition.isVisible;
				resource.hideFlow = false;
				p.Resources.dict.Add(resourceDefinition.name.GetHashCode(), resource);

				PartResource simulationResource = new PartResource(resource);
				simulationResource.simulationResource = true;
				p.SimulationResources?.dict.Add(resourceDefinition.name.GetHashCode(), simulationResource);

				// flow mode is a property that call some code using SimulationResource in its setter.
				// consequently it must be set after simulationResource is registered to avoid the following log error spam :
				// [PartSet]: Failed to add Resource XXXXX to Simulation PartSet:XX as corresponding Part XXXX SimulationResource was not found.
				resource.flowMode = PartResource.FlowMode.Both;

				GameEvents.onPartResourceListChange.Fire(p);
			}
			else
			{
				resource.maxAmount = capacity;

				PartResource simulationResource = p.SimulationResources?[resourceDefinition.name];
				if (simulationResource != null) simulationResource.maxAmount = capacity;

				resource.amount = amount;
			}

			return resource;
		}

		/// <summary> Removes the specified resource amount and capacity from a part,
		/// the resource is removed completely if the capacity reaches zero </summary>
		public static void RemoveResource(Part p, string res_name, double amount, double capacity)
		{
			// if the resource is not in the part, do nothing
			if (!p.Resources.Contains(res_name))
				return;

			// get the resource
			var res = p.Resources[res_name];

			// reduce amount and capacity
			res.amount -= amount;
			res.maxAmount -= capacity;

			// clamp amount to capacity just in case
			res.amount = Math.Min(res.amount, res.maxAmount);

			// if the resource is empty
			if (res.maxAmount <= 0.005) //< deal with precision issues
			{
				var reslib = PartResourceLibrary.Instance.resourceDefinitions;
				var resourceDefinition = reslib[res_name];

				p.Resources.dict.Remove(resourceDefinition.name.GetHashCode());
				p.SimulationResources?.dict.Remove(resourceDefinition.name.GetHashCode());

				GameEvents.onPartResourceListChange.Fire(p);
			}
		}

		///<summary>note: the resource must exist</summary>
		public static void SetResourceCapacity( Part p, string res_name, double capacity )
		{
			// if the resource is not in the part, log a warning and do nothing
			if (!p.Resources.Contains( res_name ))
			{
				return;
			}

			// set capacity and clamp amount
			var res = p.Resources[res_name];
			res.maxAmount = capacity;
			res.amount = Math.Min( res.amount, capacity );
		}

		///<summary>note: the resource must exist</summary>
		public static void SetResource( Part p, string res_name, double amount, double capacity )
		{
			// if the resource is not in the part, log a warning and do nothing
			if (!p.Resources.Contains( res_name ))
			{
				return;
			}

			// set capacity and clamp amount
			var res = p.Resources[res_name];
			res.maxAmount = capacity;
			res.amount = Math.Min( amount, capacity );
		}

		/// <summary> Set flow of a resource in the specified part. Does nothing if the resource does not exist in the part </summary>
		public static void SetResourceFlow(Part p, string res_name, bool enable)
		{
			// if the resource is not in the part, do nothing
			if (p.Resources.Contains(res_name))
			{
				// set flow state
				var res = p.Resources[res_name];
				res.flowState = enable;
			}
		}

		/// <summary> Fills a resource in the specified part to its capacity </summary>
		public static void FillResource(Part p, string res_name)
		{
			// if the resource is not in the part, do nothing
			if (p.Resources.Contains(res_name))
			{
				PartResource res = p.Resources[res_name];
				res.amount = res.maxAmount;
			}
		}

		/// <summary> Sets the amount of a resource in the specified part to zero </summary>
		public static void EmptyResource(Part p, string res_name)
		{
			// if the resource is not in the part, do nothing
			if (p.Resources.Contains(res_name))
				p.Resources[res_name].amount = 0.0;
		}

		/// <summary> Set the enabled/disabled state of a process
		/// <para> Use the process_capacity parameter to set the pseudo resource amount for the process,
		/// an amount of 0.0 disables the process, any non-zero value is a multiplier of the process.
		/// </para> </summary>
		public static void SetProcessEnabledDisabled(Part p, string res_name, bool enable, double process_capacity)
		{
			if (!p.Resources.Contains(res_name))
			{
				Lib.AddResource(p, res_name, 0.0, process_capacity);
			}

			if (enable)
			{
				SetResource(p, res_name, process_capacity, process_capacity);
			}
			else
			{
				// Never remove the resource capacity, otherwise checks against
				// the pseudo resource might fail
				SetResource(p, res_name, 0.0, process_capacity);
			}
		}

		/// <summary> Returns the definition of a resource, or null if it doesn't exist </summary>
		public static PartResourceDefinition GetDefinition( string name )
		{
			// shortcut to the resource library
			var reslib = PartResourceLibrary.Instance.resourceDefinitions;

			// return the resource definition, or null if it doesn't exist
			return reslib.Contains( name ) ? reslib[name] : null;
		}

		/// <summary> Returns name of propellant used on eva </summary>
		public static string EvaPropellantName()
		{
			// first, get the kerbal eva part prefab
			Part p = PartLoader.getPartInfoByName( "kerbalEVA" ).partPrefab;

			// then get the KerbalEVA module prefab
			KerbalEVA m = p.FindModuleImplementing<KerbalEVA>();

			// finally, return the propellant name
			return m.propellantResourceName;
		}


		/// <summary> Returns capacity of propellant on eva </summary>
		public static double EvaPropellantCapacity()
		{
			// first, get the kerbal eva part prefab
			Part p = PartLoader.getPartInfoByName( "kerbalEVA" ).partPrefab;

			// then get the first resource and return capacity
			return p.Resources.Count == 0 ? 0.0 : p.Resources[0].maxAmount;
		}
		#endregion


		// -- TECH ------------------------------------------------------------------

		///<summary>return true if the tech has been researched</summary>
		public static bool HasTech( string tech_id )
		{
			// if science is disabled, all technologies are considered available
			if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;

			// if RnD is not initialized
			if (ResearchAndDevelopment.Instance == null)
			{
				// this should not happen, throw exception
				throw new Exception( "querying tech '" + tech_id + "' while TechTree is not ready" );
			}

			// get the tech
			return ResearchAndDevelopment.GetTechnologyState( tech_id ) == RDTech.State.Available;
		}

		///<summary>return number of techs researched among the list specified</summary>
		public static int CountTech( string[] techs )
		{
			int n = 0;
			foreach (string tech_id in techs) n += HasTech( tech_id ) ? 1 : 0;
			return n;
		}

		#region ASSETS
		///<summary> Returns the path of the directory containing the DLL </summary>
		public static string Directory()
		{
			string dll_path = Assembly.GetExecutingAssembly().Location;
			return dll_path.Substring( 0, dll_path.LastIndexOf( Path.DirectorySeparatorChar ) );
		}

		///<summary> Returns a scaled copy of the source texture </summary>
		public static Texture2D ScaledTexture( Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear )
		{
			ScaleWithGPU( src, width, height, mode );

			Texture2D texture = new Texture2D( width, height, TextureFormat.ARGB32, false );
			texture.Resize( width, height );
			texture.ReadPixels( new Rect( 0, 0, width, height ), 0, 0, true );
			return texture;
		}

		///<summary> Scales the texture data of the source texture </summary>
		public static void ScaleTexture( Texture2D texture, int width, int height, FilterMode mode = FilterMode.Trilinear )
		{
			ScaleWithGPU( texture, width, height, mode );

			texture.Resize( width, height );
			texture.ReadPixels( new Rect( 0, 0, width, height ), 0, 0, true );
			texture.Apply( true );
		}

		///<summary>Renders the source texture into the RTT - used by the scaling methods ScaledTexture() and ScaleTexture() </summary>
		private static void ScaleWithGPU( Texture2D src, int width, int height, FilterMode fmode )
		{
			src.filterMode = fmode;
			src.Apply( true );

			RenderTexture rtt = new RenderTexture( width, height, 32 );
			Graphics.SetRenderTarget( rtt );
			GL.LoadPixelMatrix( 0, 1, 1, 0 );
			GL.Clear( true, true, new Color( 0, 0, 0, 0 ) );
			Graphics.DrawTexture( new Rect( 0, 0, 1, 1 ), src );
		}

		/// <summary>
		/// This makes Kerbalism possibly future-proof for new KSP versions, without requiring user
		/// intervention. With this, chances are that older Kerbalism versions will continue to work
		/// with newer KSP versions (until now the shader folder had to be copied).
		/// <para>
		/// Since KSP 1.5 (and possibly before), it has not been necessary to recompile the shaders.
		/// Kerbalism contained the same set of shader files for 1.5, 1.6, 1.7, 1.8 and 1.9. Chances
		/// are that future versions of KSP will still continue to work with the old shaders. To avoid
		/// the need to keep multiple copies of the same files, or manually rename the shader folder
		/// after a KSP update, use the default shader folder for all versions. If needed, this can be
		/// changed for future versions if they ever should require a new set of shaders.
		/// </para>
		/// </summary>
		private static string GetShaderPath()
		{
			string platform = "windows";
			if (Application.platform == RuntimePlatform.LinuxPlayer) platform = "linux";
			else if (Application.platform == RuntimePlatform.OSXPlayer) platform = "osx";

			int version = Versioning.version_major * 100 + Versioning.version_minor;

			string shadersFolder;
			switch (version)
			{
				// should it ever be necessary...
				//case 105: // 1.5
				//case 106: // 1.6
				//case 107: // 1.7
				//case 108: // 1.8
				//case 109: // 1.9
				//	shadersFolder = "15";
				//	break;
				//case 110: // 1.10
				//	shadersFolder = "110";
				//	break;
				default:
					shadersFolder = "15";
					break;
			}

			return KSPUtil.ApplicationRootPath + "GameData/Kerbalism/Shaders/" + shadersFolder + "/_" + platform;
		}

		public static Dictionary<string, Material> shaders;
		///<summary> Returns a material from the specified shader </summary>
		public static Material GetShader( string name )
		{
			if (shaders == null)
			{
				shaders = new Dictionary<string, Material>();
#pragma warning disable CS0618 // WWW is obsolete
				using (WWW www = new WWW("file://" + GetShaderPath()))
#pragma warning restore CS0618
				{
					AssetBundle bundle = www.assetBundle;
					Shader[] pre_shaders = bundle.LoadAllAssets<Shader>();
					foreach (Shader shader in pre_shaders)
					{
						string key = shader.name.Replace("Custom/", string.Empty);
						if (shaders.ContainsKey(key))
							shaders.Remove(key);
						shaders.Add(key, new Material(shader));
					}
					bundle.Unload(false);
					www.Dispose();
				}
			}

			Material mat;
			if (!shaders.TryGetValue( name, out mat ))
			{
				throw new Exception( "shader " + name + " not found" );
			}
			return mat;
		}
		#endregion

		#region CONFIG
		///<summary>get a config node from the config system</summary>
		public static ConfigNode ParseConfig( string path )
		{
			return GameDatabase.Instance.GetConfigNode( path ) ?? new ConfigNode();
		}

		///<summary>get a set of config nodes from the config system</summary>
		public static ConfigNode[] ParseConfigs( string path )
		{
			return GameDatabase.Instance.GetConfigNodes( path );
		}

		///<summary>get a value from config</summary>
		public static T ConfigValue<T>( ConfigNode cfg, string key, T def_value )
		{
			try
			{
				return cfg.HasValue( key ) ? (T) Convert.ChangeType( cfg.GetValue( key ), typeof( T ) ) : def_value;
			}
			catch (Exception e)
			{
				return def_value;
			}
		}

		///<summary>get an enum from config</summary>
		public static T ConfigEnum<T>( ConfigNode cfg, string key, T def_value )
		{
			try
			{
				return cfg.HasValue( key ) ? (T) Enum.Parse( typeof( T ), cfg.GetValue( key ) ) : def_value;
			}
			catch (Exception e)
			{
				return def_value;
			}
		}
		#endregion

		public static string Greek()
		{
			string[] letters = {
				"Alpha",
				"Beta",
				"Gamma",
				"Delta",
				"Epsilon",
				"Zeta",
				"Eta",
				"Theta",
				"Iota",
				"Kappa",
				"Lambda",
				"Mu",
				"Nu",
				"Xi",
				"Omicron",
				"Pi",
				"Sigma",
				"Tau",
				"Upsilon",
				"Phi",
				"Chi",
				"Psi",
				"Omega"
			};
			System.Random rand = new System.Random();
			int index = rand.Next(letters.Length);
			return (string)letters[index];
		}
		#region PROTO
		public static class Proto
		{
			public static bool GetBool( ProtoPartModuleSnapshot m, string name, bool def_value = false )
			{
				bool v;
				string s = m.moduleValues.GetValue( name );
				return s != null && bool.TryParse( s, out v ) ? v : def_value;
			}

			public static uint GetUInt( ProtoPartModuleSnapshot m, string name, uint def_value = 0 )
			{
				uint v;
				string s = m.moduleValues.GetValue( name );
				return s != null && uint.TryParse( s, out v ) ? v : def_value;
			}

			public static int GetInt(ProtoPartModuleSnapshot m, string name, int def_value = 0)
			{
				int v;
				string s = m.moduleValues.GetValue(name);
				return s != null && int.TryParse(s, out v) ? v : def_value;
			}

			public static float GetFloat( ProtoPartModuleSnapshot m, string name, float def_value = 0.0f )
			{
				// note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
				float v;
				string s = m.moduleValues.GetValue( name );
				return s != null && float.TryParse( s, out v ) && !float.IsNaN( v ) && !float.IsInfinity( v ) ? v : def_value;
			}

			public static double GetDouble( ProtoPartModuleSnapshot m, string name, double def_value = 0.0 )
			{
				// note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
				double v;
				string s = m.moduleValues.GetValue( name );
				return s != null && double.TryParse( s, out v ) && !double.IsNaN( v ) && !double.IsInfinity( v ) ? v : def_value;
			}

			public static string GetString( ProtoPartModuleSnapshot m, string name, string def_value = "" )
			{
				string s = m.moduleValues.GetValue( name );
				return s ?? def_value;
			}

			public static T GetEnum<T>(ProtoPartModuleSnapshot m, string name, T def_value)
			{
				UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.Lib.Proto.GetEnum");
				string s = m.moduleValues.GetValue(name);
				if (s != null && Enum.IsDefined(typeof(T), s))
				{
					T forprofiling = (T)Enum.Parse(typeof(T), s);
					UnityEngine.Profiling.Profiler.EndSample();
					return forprofiling;
				}
				UnityEngine.Profiling.Profiler.EndSample();
				return def_value;
			}

			public static T GetEnum<T>(ProtoPartModuleSnapshot m, string name)
			{
				string s = m.moduleValues.GetValue(name);
				if (s != null && Enum.IsDefined(typeof(T), s))
					return (T)Enum.Parse(typeof(T), s);
				return (T)Enum.GetValues(typeof(T)).GetValue(0);
			}

			///<summary>set a value in a proto module</summary>
			public static void Set<T>( ProtoPartModuleSnapshot module, string value_name, T value )
			{
				module.moduleValues.SetValue( value_name, value.ToString(), true );
			}
		}
		#endregion

		#region STRING PARSING
		public static class Parse
		{
			public static bool ToBool( string s, bool def_value = false )
			{
				bool v;
				return s != null && bool.TryParse( s, out v ) ? v : def_value;
			}

			public static uint ToUInt( string s, uint def_value = 0 )
			{
				uint v;
				return s != null && uint.TryParse( s, out v ) ? v : def_value;
			}

			public static Guid ToGuid (string s)
			{
				return new Guid(s);
			}

			public static float ToFloat( string s, float def_value = 0.0f )
			{
				float v;
				return s != null && float.TryParse( s, out v ) ? v : def_value;
			}

			public static double ToDouble( string s, double def_value = 0.0 )
			{
				double v;
				return s != null && double.TryParse( s, out v ) ? v : def_value;
			}

			private static bool TryParseColor( string s, out UnityEngine.Color c )
			{
				string[] split = s.Replace( " ", String.Empty ).Split( ',' );
				if (split.Length < 3)
				{
					c = new UnityEngine.Color( 0, 0, 0 );
					return false;
				}
				if (split.Length == 4)
				{
					c = new UnityEngine.Color( ToFloat( split[0], 0f ), ToFloat( split[1], 0f ), ToFloat( split[2], 0f ), ToFloat( split[3], 1f ) );
					return true;
				}
				c = new UnityEngine.Color( ToFloat( split[0], 0f ), ToFloat( split[1], 0f ), ToFloat( split[2], 0f ) );
				return true;
			}

			public static UnityEngine.Color ToColor( string s, UnityEngine.Color def_value )
			{
				UnityEngine.Color v;
				return s != null && TryParseColor( s, out v ) ? v : def_value;
			}
		}
#endregion
	}

	#region UTILITY CLASSES

	public class ObjectPair<T, U>
	{
		public T Key;
		public U Value;

		public ObjectPair(T key, U Value)
		{
			this.Key = key;
			this.Value = Value;
		}
	}

#endregion


} // KERBALISMLITE