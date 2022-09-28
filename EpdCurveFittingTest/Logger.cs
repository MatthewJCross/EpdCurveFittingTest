using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace EpdCurveFittingTest
{
    class Logger
    {
        static Logger instance;

        static Mutex mut = new Mutex();
        static string mainLogLocation;

        Logger()
        {
            mainLogLocation = "";
        }

        static public void SetLoggingDir(string dir)
        {
            mainLogLocation = System.AppDomain.CurrentDomain.BaseDirectory + @"\" + dir + @"\";
            if (!Directory.Exists(mainLogLocation))
                Directory.CreateDirectory(mainLogLocation);
        }

        static public string GetLoggingDir()
        {
            return mainLogLocation + "Output.Log";
        }

        static public Logger GetInstance(MainWindow parent)
        {
            mut.WaitOne();
            if (instance == null)
            {
                instance = new Logger();
            }

            mut.ReleaseMutex();
            return instance;
        }

        public void Log(string fmt, params Object[] obj)
        {
            string now = GetTimestamp();
            string msg = String.Format(fmt, obj);

            TextWriter tw = new StreamWriter(mainLogLocation + "Output.Log", true);
            tw.WriteLine(now + msg);
            tw.Close();
        }

        string GetTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff: ", CultureInfo.InvariantCulture);
        }
    }
}
