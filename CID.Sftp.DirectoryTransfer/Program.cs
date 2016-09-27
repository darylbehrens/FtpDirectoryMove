using System;
using System.IO;
using System.Linq;
using WinSCP;
using Serilog;
using System.Configuration;
using System.Collections.Specialized;
using Serilog.Core;

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
            string baseDirString = appSettings["BaseDirectory"];
            string sentDirString = appSettings["SentDirectory"];
            string date = DateTime.Now.ToString("yy-MM-dd");
            string tab = "    ";
            int directoryCount = 0;
            int fileCount = 0;

            if (!Directory.Exists(@"..\logs"))
            {
                Directory.CreateDirectory(@"..\logs");
            }

            // Setup Logger
            CreateLogger();

            // Log Start Of Program
            Log.Information($"Starting CID.Sftp.DirectoryTransfer");

            // Setup Options for FTP Session
            SessionOptions sessionOptions = GetSessionOptions(appSettings);

            // Setup Transfer Options for FTP Session
            TransferOptions transferOp = GetTransferOptions();

            // Link to Base Directory, and Create Enumerable of Strings With Full Directory and Individual Directroy Name
            DirectoryInfo baseDir = new DirectoryInfo(baseDirString);
            var subFolders = baseDir.GetDirectories().Select(x => new { x.FullName, x.Name });

            try
            {
                // Transfer Files and Move Directories
                using (Session session = new Session())
                {
                    // Open Session
                    session.SessionLogPath = $@"..\logs\winscp.{DateTime.Now.ToString("yy-MM-dd")}.log";
                    session.Open(sessionOptions);

                    // Loop Through Directroy Full names and Individual Names
                    foreach (var dir in subFolders)
                    {
                        directoryCount++;
                        Log.Information($"{tab}{tab}Tranfering files from {dir.Name} directory");

                        // Upload File
                        TransferOperationResult transferResult;
                        transferResult = session.PutFiles(dir.FullName + @"\*", appSettings["FtpDirectory"], false, transferOp);
                        transferResult.Check();

                        // For Logging
                        foreach (TransferEventArgs ev in transferResult.Transfers)
                        {
                            fileCount++;
                            Log.Information($"{tab}{tab}{tab}File {ev.FileName} has been transfered to FTP site");
                        }

                        // Move Directroy To Sent Directory
                        string sentDir = Path.Combine(sentDirString, dir.Name);
                        Directory.Move(dir.FullName, sentDir);
                        Log.Information($"{tab}{tab}Directory {dir.Name} successfully processed and moved");
                    }
                }
            }
            catch (IOException ioEx)
            {
                Log.Error(ioEx, "IO Error");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WinScp Error");
            }

            // Final Logging
            if (directoryCount == 0)
            {
                Log.Information($"{tab}No Directories Or Files To Process");
            }
            else
            {
                Log.Information($"{tab}Succesfully Processed {directoryCount} {(directoryCount == 1 ? "directory" : "directories")}.");
                Log.Information($"{tab}Succesfully Processed {fileCount} {(fileCount == 1 ? "file" : "files")}.");
            }
            Log.Information($"Ending CID.Sftp.DirectoryTransfer");
        }

        private static SessionOptions GetSessionOptions(NameValueCollection appSettings)
        {
            SessionOptions sessionOptions = new SessionOptions()
            {
                Protocol = Protocol.Ftp,
                HostName = appSettings["FtpAddress"],
                UserName = appSettings["FtpLogin"],
                Password = appSettings["FtpSecret"]
            };

            return sessionOptions;
        }

        private static TransferOptions GetTransferOptions()
        {
            var transferOptions = new TransferOptions()
            {
                TransferMode = TransferMode.Binary
            };

            return transferOptions;
        }

        private static void CreateLogger()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.LiterateConsole()
               .WriteTo.RollingFile(@"..\logs\{Date}.log")
               .CreateLogger();
        }
    }
}

