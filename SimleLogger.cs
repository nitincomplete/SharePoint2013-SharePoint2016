using System;
using System.IO;

namespace ConsoleApp1
{
    internal class Logger
    {
        static string logDirectory = @".\Logs";
        static bool isLoggingEnabled = true;
        static string filePrefix = "Flexi_Logs_";
        static string fileExtension = ".log";
        static string format = "dMMMyyyy";

        static string logFile = (string.IsNullOrWhiteSpace(logDirectory) ? string.Empty : logDirectory) + filePrefix + System.DateTime.Now.ToString(format) + fileExtension;

        public static void LogInfo(string msg)
        {
            if (!string.IsNullOrWhiteSpace(msg))
                InsertIntoFile(msg);
        }

        public static void LogInfo(string feedname, string msg)
        {
            if (!string.IsNullOrWhiteSpace(feedname) && !string.IsNullOrWhiteSpace(msg))
            {
                msg = feedname + "\t" + msg;
                InsertIntoFile(msg);
            }
        }

        public static void LogException(string feedname, string msg)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                msg = feedname + "\t" + "Exception: " + msg;
                InsertIntoFile(msg);
            }
        }

        public static void LogException(Exception ex)
        {
            if (ex != null)
            {
                string msg = "\t" + "Exception: " + ex.ToString();
                InsertIntoFile(msg);
            }
        }

        private static void InsertIntoFile(string msg)
        {
            FileInfo objFileInfo = new FileInfo(logFile);
            try
            {
                if (isLoggingEnabled && !string.IsNullOrWhiteSpace(msg))
                {
                    msg = DateTime.Now + "\t" + msg;
                    if (!string.IsNullOrWhiteSpace(logDirectory) && !Directory.Exists(logDirectory))
                        Directory.CreateDirectory(logDirectory);

                    if (objFileInfo.Exists)
                    {
                        if (objFileInfo.Length > 20971520)    //20971520 = 20MB
                        {
                            logFile = GetNewLogFile();
                        }
                        using (StreamWriter file = new StreamWriter(logFile, true))
                        {
                            file.WriteLine(msg);
                        }
                    }
                    else
                    {
                        using (StreamWriter file = new StreamWriter(logFile, true))
                        {
                            file.WriteLine(msg);
                        }
                    }
                }
            }
            catch (FileNotFoundException fnfex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(fnfex);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static string GetNewLogFile()
        {
            string fileName = string.Empty;
            fileName = logDirectory + filePrefix + System.DateTime.Now.ToString(format) + "-" + System.DateTime.Now.ToFileTimeUtc() + fileExtension;
            return fileName;
        }
    }
}
