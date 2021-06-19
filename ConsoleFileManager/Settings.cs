using System;
using System.Configuration;

namespace ConsoleFileManager
{
	public static class Settings
	{
		public static string Get(string key)
		{
			try
			{
				if (ConfigurationManager.AppSettings[key] == null)
					return null;

				return ConfigurationManager.AppSettings[key];
			}
			catch (ConfigurationErrorsException ex)
			{
				Console.WriteLine(ex.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			
			return null;
		}

		public static void Update(string key, string value)
		{
			try
			{
				var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				var settings = configFile.AppSettings.Settings;

				if (settings[key] != null)
					settings[key].Value = value;
				else
					settings.Add(key, value);

				configFile.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
			}
			catch (ConfigurationErrorsException ex)
			{
				Console.WriteLine(ex.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}
