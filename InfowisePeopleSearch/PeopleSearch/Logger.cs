
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Web;
using System.Security.Principal;

namespace Infowise.Sharepoint
{
    /// <summary>
    /// This class is used for logging method messages in debug mode and
    /// </summary>
    static class Logger
    {
        #region Constants
        static readonly string DirectoryKeyName = "SOFTWARE\\PeopleSearchPro";
        static readonly string FileNameFormat = "D({0}) P({1}) T({2})";
        #endregion

        #region Private Members
        static string logFilePath = string.Empty;
        static List<string> loggedModules = new List<string>();
        static Nullable<bool> m_Debug = null;
        static Nullable<bool> m_EventViewer = null;
        #endregion

        #region Public Properties
        /// <summary>
        /// Returns a value indicating if the program is in debug mode
        /// </summary>
        static public bool Debug
        {
            get
            {
                if (m_Debug != null)
                    return m_Debug.Value;

                try
                {
                    RegistryKey masterKey = Registry.LocalMachine.OpenSubKey(DirectoryKeyName);

                    if (masterKey != null)
                    {
                        try { m_Debug = (Int32)masterKey.GetValue("Debug") != 0; }
                        catch (Exception) { m_Debug = false; }

                        try { m_EventViewer = (Int32)masterKey.GetValue("LogToEventViewer") != 0; }
                        catch (Exception) { m_EventViewer = true; }

                        try { logFilePath = (string)masterKey.GetValue("LogFilePath"); }
                        catch (Exception) { }

                        try { loggedModules = new List<string>(((string)masterKey.GetValue("ModulesToLog")).Split(';')); }
                        catch (Exception) { }
                    }
                }
                catch (Exception)
                {
                    m_Debug = false;
                    m_EventViewer = false;
                }

                if (m_Debug == null)
                    m_Debug = false;

                return m_Debug.Value;
            }
        }
        #endregion

        #region Private Methods
        static bool logAuditException = true;
        static private void LogAuditException(Exception ex)
        {
            WindowsImpersonationContext winContext = null;
            try
            {
                winContext = WindowsIdentity.Impersonate(IntPtr.Zero);
                if (Debug && logAuditException)
                {
                    System.Diagnostics.EventLog.WriteEntry("Audit", ex.ToString(), EventLogEntryType.Error);
                    logAuditException = false;
                }
            }
            finally
            {
                if (winContext != null)
                    winContext.Undo();
            }
        }
        private static MethodBase GetTopMethod(int topErrorLevel, out string spaces)
        {
            StackTrace st = new StackTrace();
            MethodBase method = null;

            bool methodFound = false;
            int errorLevel = topErrorLevel + 2;
            while (!methodFound)
            {
                try
                {
                    StackFrame sf = st.GetFrame(errorLevel);
                    method = sf.GetMethod();

                    if (method.ReflectedType != typeof(Logger))
                    {
                        methodFound = true;
                        break;
                    }
                    else
                        errorLevel++;
                }
                catch
                {
                    errorLevel--;
                }
            }
            bool countIdent = true;
            int nspaces = 0;
            while (countIdent)
            {
                try
                {
                    StackFrame sf = st.GetFrame(errorLevel + nspaces);

                    if (loggedModules.Contains(sf.GetMethod().Module.Name))
                    {
                        nspaces++;
                    }
                    else
                        break;

                }
                catch
                {
                    countIdent = false;
                }
            }

            spaces = string.Empty;
            for (int i = 0; i < nspaces; i++) spaces += " ";
            return method;
        }
        private static void LogToFile(string msg)
        {
            lock (LogFile)
            {
                if (LogFile != null)
                    LogFile.WriteLine(DateTime.Now.ToString("hh:mm:ss :: ") + msg);
            }
        }
        private static string FormatMessage(params object[] args)
        {
            // Getting calling method
            string spaces;
            MethodBase method = GetTopMethod(1, out spaces);
            StringBuilder paramsInfo = new StringBuilder();
            int varIndex = 0;
            int paramIndex = 0;

            if (method == null)
                if (Debug)
                    LogToFile("Problem getting method level from stack.");

            paramsInfo.AppendFormat("{0}{1}.{2}(", spaces, method.ReflectedType.Name, method.Name.Substring(method.Name.LastIndexOf('.') + 1));

            // Adding paramets info
            if ((args != null) && (args.Length > 0))
            {
                ParameterInfo[] parameters = method.GetParameters();
                for (; varIndex < args.Length && paramIndex < parameters.Length; )
                {
                    paramsInfo.Append(parameters[paramIndex++].Name + "[");

                    if (args[varIndex] == null)
                        paramsInfo.Append("null");
                    else
                        paramsInfo.Append(args[varIndex].ToString());

                    paramsInfo.Append("]");
                    ++varIndex;
                    if ((varIndex < args.Length) && (paramIndex < parameters.Length))
                        paramsInfo.Append(",");
                }
            }
            paramsInfo.Append(")");
            return paramsInfo.ToString();
        }
        #endregion

        #region Logging Methods

        #region private logging of simple text message
        static private void Log(string msg)
        {
            try
            {
                if (Debug)
                {
                    if (m_EventViewer.Value)
                        EventLog.WriteEntry("PeopleSearch.Audit", msg);
                    msg = FormatMessage() + " : " + msg;
                    LogToFile(msg);
                }
            }
            catch (Exception iex)
            {
                LogAuditException(iex);
            }
        }
        #endregion

        #region public logging of method entering with parameters
        /// <summary>
        /// Logs a method enter
        /// Insert this method anywhere inside the desired method and it will print the method
        /// name and its parameters names and values, ie:
        /// calling to Audit.MethodEnter(1, 2, 3) from void A(int a, b, c)
        /// will result in log:
        /// ModuleName.A(a[1], b[2], c[3]);
        /// </summary>
        /// <param name="args">parameters sent to the function one after another</param>
        static public void EnterMethod(params object[] args)
        {
            try
            {
                if (Debug)
                {
                    string msg = string.Empty;

                    msg = FormatMessage(args);
                    LogToFile(msg);
                }
            }
            catch (Exception iex)
            {
                LogAuditException(iex);
            }
        }
        #endregion

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="ex">Exception thrown</param>
        static public void Log(Exception ex)
        {
            try
            {
                if (Debug)
                    Log("Exception! \n " + ex.ToString());
            }
            catch (Exception iex)
            {
                LogAuditException(iex);
            }
        }
        /// <summary>
        /// Logs a message while giving a string format and its paraneters attached
        /// </summary>
        /// <param name="msg">String format</param>
        /// <param name="args">String format arguments</param>
        static public void Log(string msg, params object[] args)
        {
            try
            {
                if (Debug)
                {
                    if (args != null && args.Length > 0)
                        msg = string.Format(msg, args);

                    Log(msg);
                }
            }
            catch (Exception iex)
            {
                LogAuditException(iex);
            }
        }
        static public void LogUser()
        {
            Logger.Log("User Name: <{0}>.", HttpContext.Current.User.Identity.Name);
        }
        #endregion

        #region File
        static private StreamWriter m_LogFile = null;
        static private StreamWriter LogFile
        {
            get
            {
                if ((m_LogFile == null) && (logFilePath != string.Empty))
                {
                    string procID = Process.GetCurrentProcess().Id.ToString();
                    string threadID = Thread.CurrentThread.ManagedThreadId.ToString();
                    string newFileName = string.Format(FileNameFormat,
                        DateTime.Now.ToString().Replace('/', '-').Replace(':', ';'),
                        procID, threadID);
                    newFileName = string.Format(logFilePath, newFileName);
                    m_LogFile = File.AppendText(newFileName);
                    m_LogFile.AutoFlush = true;

                    lock (m_LogFile)
                    {
                        m_LogFile.WriteLine();
                        m_LogFile.WriteLine();
                        m_LogFile.WriteLine("Starting Logging at : {0}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss"));
                        m_LogFile.WriteLine("=============================================");
                    }
                }

                return m_LogFile;
            }
        }
        #endregion
    }
}
