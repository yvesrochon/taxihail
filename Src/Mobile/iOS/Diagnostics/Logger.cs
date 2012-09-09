
using System;
using System.IO;

using System.Diagnostics;
using apcurium.MK.Common.Diagnostic;
using TinyIoC;
using apcurium.MK.Booking.Mobile.Infrastructure;

namespace apcurium.MK.Booking.Mobile.Client
{
    public class LoggerWrapper :  ILogger
    {
        public void LogError(Exception ex)
        {
            Logger.LogError(ex);
        }
        
        public void LogMessage(string message)
        {
            Logger.LogMessage(message);
        }
        
        public void StartStopwatch(string message)
        {           
            Logger.StartStopwatch(message);
        }

        public void StopStopwatch(string message)
        {
            Logger.StopStopwatch(message);
        }

        public void LogStack()
        {
            Logger.LogStack();
        }

    }
    
    public class Logger
    {
        private static Stopwatch _stopWatch;

        public Logger()
        {
        }

        public static void LogError(Exception ex)
        {
            LogError(ex, 0);
        }

        public static void LogError(Exception ex, int indent)
        {
            string indentStr = "";
            for (int i = 0; i < indent; i++)
            {
                indentStr += "   ";
            }
            if (indent == 0)
            {
                Write(indentStr + "Error on " + DateTime.Now.ToString());
            }
            
            
            Write(indentStr + "Message : " + ex.Message);
            Write(indentStr + "Stack : " + ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                LogError(ex.InnerException, indent++);
            }
        }

        public static void LogMessage(string message)
        {
            
            
            Write("Message on " + DateTime.Now.ToString() + " : " + message);
            
        }

        public static void StartStopwatch(string message)
        {
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            
            Write("Start timer : " + message);
        }

        public static void StopStopwatch(string message)
        {
            if (_stopWatch != null)
            {
                _stopWatch.Stop();
                Write("Stop timer : " + message + " in " + _stopWatch.ElapsedMilliseconds + " ms");
            }
        }

        public static void LogStack()
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

            // write call stack method names
            foreach (StackFrame stackFrame in stackFrames)
            {
                if (stackFrame.GetMethod().Name != "LogStack")
                {
                    Write("Stack : " + stackFrame.GetMethod().Name);   // write method name
                }
            }
        
        }

        private static void Write(string message)
        {
            try
            {
                string user = @" N\A with version " + TinyIoCContainer.Current.Resolve<IPackageInfo>().Version;
                if (AppContext.Current.LoggedUser != null)
                {
                    user = AppContext.Current.LoggedUser.Email;                             
                }
                
                Console.WriteLine(message + " by :" + user + " with version " + TinyIoCContainer.Current.Resolve<IPackageInfo>().Version);            
            
                if (TinyIoCContainer.Current.Resolve<IAppSettings>().ErrorLogEnabled)
                {
                    try
                    {
                        using (var fs = new FileStream (TinyIoCContainer.Current.Resolve<IAppSettings>().ErrorLog, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            using (var w = new StreamWriter (fs))
                            {
                                w.BaseStream.Seek(0, SeekOrigin.End);
                                w.WriteLine(message + " by :" + user + " with version " + TinyIoCContainer.Current.Resolve<IPackageInfo>().Version);
                                w.Flush();
                                w.Close();
                            }
                            fs.Close();
                        }
                    }
                    catch
                    {
                    
                    }
                }
            }
            catch
            {
            }
            
        }
    }
}

