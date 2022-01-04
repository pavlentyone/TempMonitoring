using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace TempMonitoring
{
    public class TemperatureIntervalSettings
    {
        public DataTable settingsDataTable {  get; private set; }
        public List<TemperatureSetting> plusTZakTemperatures { get; private set; }
        public List<TemperatureSetting> minusTZakTemperatures { get; private set; }
        public List<TemperatureSetting> maxTemperatures { get; private set; }
        public List<TemperatureSetting> minTemperatures { get; private set; }
        public TemperatureIntervalSettings()
        {
            UpdateSettingsDataTable();
        }

        public void UpdateSettingsDataTable()
        {
            string selectStr = @"
            select 
                a.hid,
                a.value,
                a.message,
                a.behavior
            from temp.temp_settings a";
            settingsDataTable = InitTableData.FillDataTable(InitTableData.connStr, selectStr, null);

            plusTZakTemperatures = new List<TemperatureSetting>();
            minusTZakTemperatures = new List<TemperatureSetting>();
            maxTemperatures = new List<TemperatureSetting>();
            minTemperatures = new List<TemperatureSetting>();

            foreach(DataRow row in settingsDataTable.Rows){
                double? value = row["value"] as double?;
                string message = row["message"] as string;
                string behavior = row["behavior"] as string;

                if(value == null)
                    continue;

                TemperatureSetting current = new TemperatureSetting((double)value, message);

                switch(behavior){
                    case "plus_t_zak":
                        plusTZakTemperatures.Add(current);
                        break;
                    case "minus_t_zak":
                        minusTZakTemperatures.Add(current);
                        break;
                    case "max":
                        maxTemperatures.Add(current);
                        break;
                    case "min":
                        minTemperatures.Add(current);
                        break;
                    default:
                        if (behavior != null)
                            Console.WriteLine(String.Format(@"'{0}' behavior was found but it doesn't suppors by this application"));
                        break;
                }
            }

            SortTemperatureSetting(plusTZakTemperatures);
            SortTemperatureSetting(minusTZakTemperatures);
            SortTemperatureSetting(minTemperatures);
            SortTemperatureSetting(maxTemperatures);
        }
        public void SortTemperatureSetting(List<TemperatureSetting> settings)
        {
            TemperatureSetting temp;
            for (int i = 0; i < settings.Count; i++)
                for (int j = i; j < settings.Count; j++)
                    if (settings[i].value > settings[j].value)
                    {
                        temp = settings[i];
                        settings[i] = settings[j];
                        settings[j] = temp;
                    }
        }

        public List<string> GetListOfWarningMessages(double temp, double t_zak)
        {
            List<string> result = new List<string>();

            foreach (TemperatureSetting setting in plusTZakTemperatures)
                if (temp > t_zak + setting.value)
                    result.Add(setting.message);
            foreach (TemperatureSetting setting in minusTZakTemperatures)
                if (temp < t_zak + setting.value)
                    result.Add(setting.message);
            foreach (TemperatureSetting setting in maxTemperatures)
                if (temp > setting.value)
                    result.Add(setting.message);
            foreach (TemperatureSetting setting in minTemperatures)
                if (temp < setting.value)
                    result.Add(setting.message);

            return result;
        }

        public bool IsNormal(double temp, double t_zak)
        {
            foreach (TemperatureSetting setting in plusTZakTemperatures)
                if (temp > t_zak + setting.value)
                    return false;
            foreach (TemperatureSetting setting in minusTZakTemperatures)
                if (temp < t_zak + setting.value)
                    return false;
            foreach (TemperatureSetting setting in maxTemperatures)
                if (temp > setting.value)
                    return false;
            foreach (TemperatureSetting setting in minTemperatures)
                if (temp < setting.value)
                    return false;

            return true;
        }

        public bool IsTheSameIntervals(double temp1, double temp2, double t_zak1, double t_zak2)
        {
            foreach (TemperatureSetting setting in plusTZakTemperatures)
                if (!(temp1 > t_zak1 + setting.value == temp2 > t_zak2 + setting.value))
                    return false;
            foreach (TemperatureSetting setting in minusTZakTemperatures)
                if (!(temp1 < t_zak1 + setting.value == temp2 < t_zak2 + setting.value))
                    return false;
            foreach (TemperatureSetting setting in maxTemperatures)
                if (!(temp1 > setting.value == temp2 > setting.value))
                    return false;
            foreach (TemperatureSetting setting in minTemperatures)
                if (!(temp1 < setting.value == temp2 < setting.value))
                    return false;
            return true;
        }
    }

    public class TemperatureSetting
    {
        public double value { get; private set; }
        public string message { get; private set; }
        public TemperatureSetting(double value, string message)
        {
            this.value = value;
            this.message = message;
        }
    }
}
