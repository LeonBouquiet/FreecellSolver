using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreecellSolver
{
	public static class ConfigSettings
	{
		private static int? _levelWeight;

		public static int LevelWeight
		{
			get
			{
				if (_levelWeight == null)
					_levelWeight = GetAppSetting<int>("LevelWeight");

				return _levelWeight.Value;
			}
		}

		private static int? _consecutivenessWeight;

		public static int ConsecutivenessWeight
		{
			get
			{
				if (_consecutivenessWeight == null)
					_consecutivenessWeight = GetAppSetting<int>("ConsecutivenessWeight");

				return _consecutivenessWeight.Value;
			}
		}

		private static int? _completenessWeight;

		public static int CompletenessWeight
		{
			get
			{
				if (_completenessWeight == null)
					_completenessWeight = GetAppSetting<int>("CompletenessWeight");

				return _completenessWeight.Value;
			}
		}

		private static int? _availabilityWeight;

		public static int AvailabilityWeight
		{
			get
			{
				if (_availabilityWeight == null)
					_availabilityWeight = GetAppSetting<int>("AvailabilityWeight");

				return _availabilityWeight.Value;
			}
		}

		private static int? _relaxation;

		public static int Relaxation
		{
			get
			{
				if (_relaxation == null)
					_relaxation = GetAppSetting<int>("Relaxation");

				return _relaxation.Value;
			}
		}

		/// <summary>
		/// Gets the appSetting with the given name, and attempts to convert it to <typeparamref name="T"/>.
		/// </summary>
		/// <exception cref="ConfigurationErrorsException">When the key doesn't exist, or when the
		/// value couldn't be converted to the requested type.</exception>
		private static T GetAppSetting<T>(string keyName)
		{
			//Extract the key's value.
			string stringValue = ConfigurationManager.AppSettings[keyName];
			if (stringValue == null)
			{
				string errMsg = string.Format(CultureInfo.InvariantCulture, "Config file is missing required appSettings key \"{0}\".", keyName);
				throw new ConfigurationErrorsException(errMsg);
			}

			try
			{
				//Dynamically convert it to the requested type.
				TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
				object result = converter.ConvertFromInvariantString(stringValue);

				return (T)result;
			}
			catch (Exception ex)
			{
				string errMsg = string.Format(CultureInfo.InvariantCulture, "The appSettings key \"{0}\" has value \"{1}\" which cannot be converted to the requested type ({2}).", keyName, stringValue, typeof(T));
				throw new ConfigurationErrorsException(errMsg, ex);
			}
		}
	}
}
