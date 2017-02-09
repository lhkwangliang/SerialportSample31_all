using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialportSample
{
    static class Program
    {
        public static string globalPortName = "";
        public static int globalBaudrate = 0;
        public static Dictionary<string, ParamItem> paramsMap = new Dictionary<string, ParamItem>();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            installMap();
            Application.Run(new MainForm());
        }

        private static void installMap()
        {
            configParam("F001", "pvModuleType", 20, "", 0, "string", "");
            configParam("F002", "pvModuleAddr", 2, "10", 0, "uint2", "");
            configParam("F003", "pvProtocolVersion", 2, "", 100, "uint2", "");
            configParam("F018", "pvSoftwareVersion", 2, "", 0, "uint2", "");
            configParam("F019", "pvProductSerial", 20, "", 0, "string", "");

            configParam("8028", "pvPowerModuleAlarm", 1, "", 0, "uint1", "");//1:告警, 0：正常
            configParam("801C", "pvOverPowerAlarm", 1, "", 0, "uint1", "");//1:告警, 0：正常
            configParam("80C8", "pvVSWRAlarm", 1, "", 0, "uint1", "");//1:告警, 0：正常
            configParam("81F0", "pvOverTemperatureAlarm", 1, "", 0, "uint1", "");//1:告警, 0：正常

            configParam("8045", "pvSystemThermometer", 1, "", 0, "sint1", "℃");
            configParam("80BF", "pvInputPower", 2, "", 10, "sint2", "dBm");
            configParam("8004", "pvOutputPower", 2, "", 10, "sint2", "dBm");
            configParam("800C", "pvIncidentPower", 2, "", 10, "sint2", "dBm");
            configParam("8120", "pvVSWR", 1, "", 10, "uint1", "");
            configParam("80C3", "pvInputPowerVoltage", 2, "", 1000, "uint2", "V");
            configParam("8086", "pvOutputPowerVoltage", 2, "", 1000, "uint2", "V");
            configParam("809E", "pvIncidentPowerVoltage", 2, "", 1000, "uint2", "V");
            configParam("0024", "pvDigitalAtt", 1, "", 2, "uint1", "dB");
            configParam("0018", "pvAlcControl", 2, "", 10, "sint2", "dBm");
            configParam("0008", "pvPowerSwitch", 1, "", 0, "uint1", "");


            configParam("D122", "pvVgEnabled", 1, "", 0, "uint1", "");//AUTO:0, MANU:1
            configParam("D100", "pvVgSwitch1", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D101", "pvVgSwitch2", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D102", "pvVgSwitch3", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D103", "pvVgSwitch4", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D104", "pvVgSwitch5", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D105", "pvVgSwitch6", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D106", "pvVgSwitch7", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D107", "pvVgSwitch8", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D108", "pvVgSwitch9", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D109", "pvVgSwitch10", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D10A", "pvVgSwitch11", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D10B", "pvVgSwitch12", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D10C", "pvVgSwitch13", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D10D", "pvVgSwitch14", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D10E", "pvVgSwitch15", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D10F", "pvVgSwitch16", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("DE10", "pvVgVoltage1", 2, "", 0, "uint2", "mV");
            configParam("DE11", "pvVgVoltage2", 2, "", 0, "uint2", "mV");
            configParam("DE12", "pvVgVoltage3", 2, "", 0, "uint2", "mV");
            configParam("DE13", "pvVgVoltage4", 2, "", 0, "uint2", "mV");
            configParam("DE14", "pvVgVoltage5", 2, "", 0, "uint2", "mV");
            configParam("DE15", "pvVgVoltage6", 2, "", 0, "uint2", "mV");
            configParam("DE16", "pvVgVoltage7", 2, "", 0, "uint2", "mV");
            configParam("DE17", "pvVgVoltage8", 2, "", 0, "uint2", "mV");
            configParam("DE18", "pvVgVoltage9", 2, "", 0, "uint2", "mV");
            configParam("DE19", "pvVgVoltage10", 2, "", 0, "uint2", "mV");
            configParam("DE1A", "pvVgVoltage11", 2, "", 0, "uint2", "mV");
            configParam("DE1B", "pvVgVoltage12", 2, "", 0, "uint2", "mV");
            configParam("DE1C", "pvVgVoltage13", 2, "", 0, "uint2", "mV");
            configParam("DE1D", "pvVgVoltage14", 2, "", 0, "uint2", "mV");
            configParam("DE1E", "pvVgVoltage15", 2, "", 0, "uint2", "mV");
            configParam("DE1F", "pvVgVoltage16", 2, "", 0, "uint2", "mV");
            configParam("DF10", "pvVgVoltageTemp1", 2, "", 0, "uint2", "mV");
            configParam("DF11", "pvVgVoltageTemp2", 2, "", 0, "uint2", "mV");
            configParam("DF12", "pvVgVoltageTemp3", 2, "", 0, "uint2", "mV");
            configParam("DF13", "pvVgVoltageTemp4", 2, "", 0, "uint2", "mV");
            configParam("DF14", "pvVgVoltageTemp5", 2, "", 0, "uint2", "mV");
            configParam("DF15", "pvVgVoltageTemp6", 2, "", 0, "uint2", "mV");
            configParam("DF16", "pvVgVoltageTemp7", 2, "", 0, "uint2", "mV");
            configParam("DF17", "pvVgVoltageTemp8", 2, "", 0, "uint2", "mV");
            configParam("DF18", "pvVgVoltageTemp9", 2, "", 0, "uint2", "mV");
            configParam("DF19", "pvVgVoltageTemp10", 2, "", 0, "uint2", "mV");
            configParam("DF1A", "pvVgVoltageTemp11", 2, "", 0, "uint2", "mV");
            configParam("DF1B", "pvVgVoltageTemp12", 2, "", 0, "uint2", "mV");
            configParam("DF1C", "pvVgVoltageTemp13", 2, "", 0, "uint2", "mV");
            configParam("DF1D", "pvVgVoltageTemp14", 2, "", 0, "uint2", "mV");
            configParam("DF1E", "pvVgVoltageTemp15", 2, "", 0, "uint2", "mV");
            configParam("DF1F", "pvVgVoltageTemp16", 2, "", 0, "uint2", "mV");
            configParam("D130", "pvVgRealVoltage1", 2, "", 0, "uint2", "");
            configParam("D131", "pvVgRealVoltage2", 2, "", 0, "uint2", "");
            configParam("D132", "pvVgRealVoltage3", 2, "", 0, "uint2", "");
            configParam("D133", "pvVgRealVoltage4", 2, "", 0, "uint2", "");
            configParam("D134", "pvVgRealVoltage5", 2, "", 0, "uint2", "");
            configParam("D135", "pvVgRealVoltage6", 2, "", 0, "uint2", "");
            configParam("D136", "pvVgRealVoltage7", 2, "", 0, "uint2", "");
            configParam("D137", "pvVgRealVoltage8", 2, "", 0, "uint2", "");
            configParam("D138", "pvVgRealVoltage9", 2, "", 0, "uint2", "");
            configParam("D139", "pvVgRealVoltage10", 2, "", 0, "uint2", "");
            configParam("D13A", "pvVgRealVoltage11", 2, "", 0, "uint2", "");
            configParam("D13B", "pvVgRealVoltage12", 2, "", 0, "uint2", "");
            configParam("D13C", "pvVgRealVoltage13", 2, "", 0, "uint2", "");
            configParam("D13D", "pvVgRealVoltage14", 2, "", 0, "uint2", "");
            configParam("D13E", "pvVgRealVoltage15", 2, "", 0, "uint2", "");
            configParam("D13F", "pvVgRealVoltage16", 2, "", 0, "uint2", "");


            configParam("D123", "pvAlcGainEnabled", 1, "", 0, "uint1", "");//AUTO:0, MANU:1
            configParam("D120", "pvAlcVoltageSwitch", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("D121", "pvGainVoltageSwitch", 1, "", 0, "uint1", "");//OFF:0, ON:1
            configParam("DE20", "pvAlcVoltage", 2, "", 0, "uint2", "mV");
            configParam("DE30", "pvGainVoltage", 2, "", 0, "uint2", "mV");
            configParam("DF20", "pvAlcVoltageTemp", 2, "", 0, "uint2", "mV");
            configParam("DF30", "pvGainVoltageTemp", 2, "", 0, "uint2", "mV");
            configParam("D140", "pvAlcRealVoltage", 2, "", 0, "uint2", "");
            configParam("D141", "pvGainRealVoltage", 2, "", 0, "uint2", "");

            configParam("DE41", "pvAttCompensation", 9, "", 0, "table type:uint1+uint1+sint1*3;scale:0,0,2", "");
            configParam("D004", "pvOutputPowerAd", 40, "", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            configParam("D00C", "pvInputPowerAd", 40, "", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            configParam("D010", "pvIncidentPowerAd", 40, "", 0, "table type:sint2+uint2*10;scale:10,0", "dBm");
            configParam("D030", "pvAlcPowerVoltage", 40, "", 0, "table type:sint2+uint2*10;scale:10,0", "dBm,mV");
            configParam("D018", "pvOutputPowerAdValue", 2, "", 0, "uint2", "");
            configParam("D020", "pvInputPowerAdValue", 2, "", 0, "uint2", "");
            configParam("D024", "pvIncidentPowerAdValue", 2, "", 0, "uint2", "");
            configParam("D040", "pvAlcPowerVoltageValue", 2, "", 0, "uint2", "");
            
        }

        private static void configParam(string paramId, string paramName, int length, string paramValue, int scale, string type, string unit)
        {
            paramsMap.Add(paramId, new ParamItem(paramId, paramName, length, paramValue, scale, type, unit));
        }
    }
}
