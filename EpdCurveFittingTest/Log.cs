using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.ApplicationBlocks.ExceptionManagement;
using System.Collections.Specialized;

namespace EpdCurveFittingTest
{
    public class Log
    {
        #region private

        private static readonly BlockingCollection<Tuple<DateTime, string>> _buffer = new BlockingCollection<Tuple<DateTime, string>>(10000);
        private static readonly TimeSpan CullLimit = TimeSpan.FromDays(14);
        private static readonly CancellationTokenSource _source = new CancellationTokenSource();
        private static readonly CancellationToken _cancellation = _source.Token;

        private static DateTime _lastLog;
        private static StreamWriter _writer;
        private static bool _opened;
        private static readonly string _title;
        private static readonly string _version;
        private static readonly string _exeName;
        private static string _path;

        #endregion

        #region properties
        /// <summary>
        /// Level of logging
        /// </summary>
        public static int Level { get; set; }

        /// <summary>
        /// Base path for logging
        /// </summary>
        private static string _basePath = @"C:\Log";
        public static string BasePath
        {
            get => _basePath;
            set
            {
                _basePath = value;
                _path = $@"{_basePath}\LogFiles";

                // Ensure that LOG directory exists
                Directory.CreateDirectory(_path);

                CloseLog();
            }
        }

        private static string _suffix = "Main";
        public static string Suffix
        {
            get => _suffix;
            set
            {
                _suffix = value;
                CloseLog();
            }
        }

        #endregion

        #region public

        /// <summary>
        /// Gets or sets a value indicating whether to disable events.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disable events; otherwise, <c>false</c>.
        /// </value>
        public static bool DisableEvents { get; set; }

        /// <summary>
        /// Line Written Handler event
        /// </summary>	
        public static event EventHandler<WriteLineEventArgs> LineWritten;

        /// <summary>
        /// Indent Log file
        /// </summary>
        public static void Indent() { }

        /// <summary>
        /// Unindent Log File
        /// </summary>
        public static void Unindent() { }

        /// <summary>
        /// Get Log Directory
        /// </summary>
        /// <returns></returns>
        public static string GetLogDirectory()
        {
            return _path;
        }

        /// <summary>
        /// Get Attribute
        /// </summary>
        /// <typeparam name="TAttributeType"></typeparam>
        /// <param name="exe"></param>
        /// <returns></returns>
        private static TAttributeType GetAttribute<TAttributeType>(Assembly exe) where TAttributeType : Attribute
        {
            return (TAttributeType)Attribute.GetCustomAttribute(exe, typeof(TAttributeType));
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        ///-------------------------------------------------------------------------------------
        static Log()
        {
            Assembly exe = Assembly.GetEntryAssembly();
            _title = GetAttribute<AssemblyTitleAttribute>(exe).Title;

            FileVersionInfo f = FileVersionInfo.GetVersionInfo(exe.Location);
            _version = f.ProductVersion;

            try
            {
                _exeName = Path.GetFileNameWithoutExtension(exe.Location);
            }
            catch
            {
                _exeName = "<UNKNOWN>";
            }

            BasePath = Path.GetDirectoryName(exe.Location);

            // Get lifespan of debug files.
            using (RegistryKey configRootKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\JollyGiant\Configuration"))
            {
                try
                {
                    if (double.TryParse(configRootKey?.GetValue("Debug File Lifespan")?.ToString(), out var val))
                        CullLimit = TimeSpan.FromDays(val);
                }
                catch
                {
                    CullLimit = TimeSpan.FromDays(30);
                }
            }

            Task.Run(() => BackgroundTask(), _cancellation);
        }

        /// <summary>
        ///  Open Log File
        /// </summary>	  
        private static void OpenLog()
        {
            // Close existing file
            _writer?.Dispose();
            _writer = null;

            Task.Run(
              () =>
              {
                  try
                  {
                      if (Directory.Exists(_path))
                          foreach (var file in new DirectoryInfo(_path).GetFiles().Where(file => (DateTime.Today - file.LastWriteTime) > CullLimit))
                              file.Delete();
                  }
                  catch (Exception e)
                  {
                      ExceptionManager.Publish(e);
                  }
              }
            );

            // Get current date & time
            _lastLog = DateTime.Now;

            // open log file ensuring that it is flushed after every write
            var fs = File.Open($@"{_path}\{_lastLog:yyyyMMdd}_{_suffix}.{"log"}", FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(fs) { AutoFlush = true };

            // Write Header to file
            DoWrite(_lastLog, $"{_title} v{_version} - debug log started");
            DoWrite(_lastLog, string.Empty);

            _opened = true;
        }
        
        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Close Log file
        /// </summary>
        ///-------------------------------------------------------------------------------------
        private static void CloseLog()
        {
            _opened = false;
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        ///-------------------------------------------------------------------------------------
        private static void BackgroundTask()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        // block until at least one item is queued or we are shutdown
                        var entry = _buffer.Take(_cancellation);

                        // Get current time & date
                        DateTime now = DateTime.Now;

                        // Open new log file if we have crossed midnight since last log write
                        if (!_opened || now.DayOfYear != _lastLog.DayOfYear || now.Year != _lastLog.Year)
                        {
                            // Open todays log file
                            OpenLog();
                        }

                        // Update time of last log
                        _lastLog = now;

                        // Write this item to log
                        DoWrite(entry.Item1, entry.Item2);

                        DrainBuffer();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                DrainBuffer();
                _writer = null;
            }
        }

        private static void DrainBuffer()
        {
            // try to drain rest of entries
            while (_buffer.TryTake(out var entry))
                DoWrite(entry.Item1, entry.Item2);
        }

        private static void DoWrite(DateTime when, string what)
        {
            var s = $"{when:HH:mm:ss.fff} | {what}";
            if (!DisableEvents)
                LineWritten?.Invoke(null, new WriteLineEventArgs(s));
            _writer.WriteLine(s);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write a line to the log file
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="message"></param>
        ///-------------------------------------------------------------------------------------

        public static void AddEntry(string moduleName, string message)
        {
            WriteLine($"[{moduleName,30}] {message}");
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write a line to the log file
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        ///-------------------------------------------------------------------------------------

        public static void AddEntry(string moduleName, string format, params object[] args)
        {
            AddEntry(moduleName, string.Format(format, args));
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write line to log file
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="log"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        ///-------------------------------------------------------------------------------------

        public static void AddEntry(string moduleName, bool log, string format, params object[] args)
        {
            if (log)
                AddEntry(moduleName, format, args);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write a line to the log file
        /// </summary>
        /// <param name="message"></param>
        ///-------------------------------------------------------------------------------------
        public static void WriteLine(string message)
        {
            _buffer.TryAdd(Tuple.Create(DateTime.Now, message), 100);
        }

        /// <summary>
        /// Write Line with Header
        /// </summary>
        /// <param name="header"></param>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public static void WriteLineHeader(string header, string message, int level)
        {
            if (level <= Level)
                WriteLine($"[{header,30}] {message}");
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write a line to the log file
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        ///-------------------------------------------------------------------------------------
        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write line to log file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="loggingAllowed"></param>
        ///-------------------------------------------------------------------------------------
        public static void WriteLine(bool loggingAllowed, string message)
        {
            if (loggingAllowed)
                WriteLine(message);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write line to log file
        /// </summary>
        /// <param name="loggingAllowed"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        ///-------------------------------------------------------------------------------------
        public static void WriteLine(bool loggingAllowed, string format, params object[] args)
        {
            if (loggingAllowed)
                WriteLine(format, args);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write line to log file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        ///-------------------------------------------------------------------------------------
        public static void WriteLine(int logLevel, string message)
        {
            if (logLevel <= Level)
                WriteLine(message);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Write line to log file
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        ///-------------------------------------------------------------------------------------
        public static void WriteLine(int logLevel, string format, params object[] args)
        {
            if (logLevel <= Level)
                WriteLine(format, args);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        ///-------------------------------------------------------------------------------------
        public static void AddException(Exception exception)
        {
            AddException(exception, null, "Exception Caught");
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        ///-------------------------------------------------------------------------------------
        public static void AddException(Exception exception, string message)
        {
            AddException(exception, null, message);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        ///-------------------------------------------------------------------------------------
        public static void AddException(Exception exception, NameValueCollection additionalInfo)
        {
            AddException(exception, additionalInfo, "Exception Caught");
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        /// <param name="message"></param>
        ///-------------------------------------------------------------------------------------
        public static void AddException(Exception exception, NameValueCollection additionalInfo, string message)
        {
            // Record General information.
            AddEntry("Exception", message);
            //Indent();

            // Record the contents of the additionalInfo collection.
            if (additionalInfo != null)
            {
                AddEntry("Exception", "Additonal Info:");
                //Indent();
                foreach (string i in additionalInfo)
                    AddEntry("Exception", "{0}- {1}", i, additionalInfo.Get(i));
                //Unindent();
            }

            // Append the exception text
            if (exception != null)
            {
                AddEntry("Exception", "Exception Information - {0}", exception.Message);
                //Indent();

                if (exception.InnerException != null)
                {
                    Exception inner = exception.InnerException;

                    AddEntry("Exception", "Inner Exceptions");
                    //Indent();
                    while (inner != null)
                    {
                        AddEntry("Exception", "{0} - {1}", inner.Source, inner.Message);
                        DumpStack(inner.StackTrace);
                        AddEntry("Exception", string.Empty);
                        inner = inner.InnerException;
                    }
                    //Unindent();
                }
                //Unindent();

                DumpStack(exception.StackTrace);
            }
            else
                AddEntry("Exception", "No Exception.");

            //Unindent();
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        /// Writes formatted version of stack trace to log file
        /// </summary>
        /// <param name="stackTrace">raw stack trace</param>
        ///-------------------------------------------------------------------------------------
        private static void DumpStack(string stackTrace)
        {
            if (stackTrace != null)
            {
                AddEntry("Exception", "Stack Trace : ");
                var stack = stackTrace.Split(Environment.NewLine.ToCharArray());
                foreach (var line in stack)
                    AddEntry("Exception", line);
            }
        }

        #endregion
    }

    /// <summary>
    /// Write Line Event Args class
    /// </summary>
    public class WriteLineEventArgs : EventArgs
    {
        #region public
        /// <summary>
        /// Message member
        /// </summary>
        public string Message { get; private set; }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public WriteLineEventArgs(string message)
        {
            Message = message;
        }
        #endregion
    }
}
