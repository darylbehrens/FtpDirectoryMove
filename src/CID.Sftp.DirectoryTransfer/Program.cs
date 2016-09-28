using System;
using System.IO;
using System.Linq;
using WinSCP;
using Serilog;
using System.Configuration;

namespace CID.Sftp.DirectoryTransfer
{
    class Program
    {
        /// <summary>
        /// Program will through directories of a base directory
        /// and copy all files from base directory to a root FTP directory
        /// Then move subdivrectory to a sent location
        /// </summary>
        /// <param></param>
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var options = new Options()
            {
                BaseDirectory = appSettings["BaseDirectory"],
                SentDirectory = appSettings["SentDirectory"],
                FtpAddress = appSettings["FtpAddress"],
                FtpDirectory = appSettings["FtpDirectory"],
                FtpLogin = appSettings["FtpLogin"]
            };

            Program program = new Program();
            program.Upload(options);
        }

        private void Upload(Options options)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, o) => LogError(s, o);

            CreateLogger();
            Log.Information("{@options}", options);

            string date = DateTime.Now.ToString("yy-MM-dd");

            // Make Sure Directory Exists
            if (!Directory.Exists(@"..\logs"))
            {
                Directory.CreateDirectory(@"..\logs");
            }

            // Setup Options for FTP Session
            SessionOptions sessionOptions = GetSessionOptions(options);

            // Setup Transfer Options for FTP Session
            TransferOptions transferOp = GetTransferOptions();

            // Link to Base Directory, and Create Enumerable of Strings With Full Directory and Individual Directroy Name
            DirectoryInfo baseDir = new DirectoryInfo(options.BaseDirectory);
            var subFolders = baseDir.GetDirectories().Select(x => new { x.FullName, x.Name });

            // Transfer Files and Move Directories
            using (Session session = new Session())
            {
                // Open Session
                session.SessionLogPath = $@"..\logs\winscp.{DateTime.Now.ToString("yy-MM-dd")}.log";
                session.Open(sessionOptions);

                // Loop Through Directroy Full names and Individual Names
                foreach (var dir in subFolders)
                {
                    // Upload File
                    TransferOperationResult transferResult;
                    transferResult = session.PutFiles(dir.FullName + @"\*", options.FtpDirectory, false, transferOp);
                    transferResult.Check();

                    // For Logging
                    foreach (TransferEventArgs ev in transferResult.Transfers)
                    {
                        FileInfo file = new FileInfo(ev.FileName);
                        Log.Information($"{dir.Name}/{file.Name} transfered");
                    }

                    // Move Directroy To Sent Directory
                    string sentDir = Path.Combine(options.SentDirectory, dir.Name);
                    Directory.Move(dir.FullName, sentDir);
                }
            }
        }

        // Setup SFTP Session
        private SessionOptions GetSessionOptions(Options options)
        {
            var appSettings = ConfigurationManager.AppSettings;

            SessionOptions sessionOptions = new SessionOptions()
            {
                Protocol = Protocol.Ftp,
                HostName = options.FtpAddress,
                UserName = options.FtpLogin,
                Password = appSettings["FtpSecret"]
            };
            return sessionOptions;
        }

        // Setup SFTP Transfer Options
        private TransferOptions GetTransferOptions()
        {
            var transferOptions = new TransferOptions()
            {
                TransferMode = TransferMode.Binary
            };
            return transferOptions;
        }

        // Create Logger
        private void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.LiterateConsole()
               .WriteTo.RollingFile(@"..\logs\{Date}.log")
               .CreateLogger();
        }

        private void LogError(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            Log.Error(exception, "Process has terminated");
        }
    }
}