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
            string baseDirString = appSettings["BaseDirectory"];
            string sentDirString = appSettings["SentDirectory"];
            string date = DateTime.Now.ToString("yy-MM-dd");
            string tab = "    ";
            int directoryCount = 0;
            int fileCount = 0;

            // Setup Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile(@"logs\{Date}.log")
                .CreateLogger();

            Log.Information($"Starting CID.Sftp.DirectoryTransfer {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            // Setup Options for FTP Session
            SessionOptions sessionOptions = new SessionOptions()
            {
                Protocol = Protocol.Ftp,
                HostName = appSettings["FtpAddress"],
                UserName = appSettings["FtpLogin"],
                Password = appSettings["FtpSecret"]
            };

            // Setup Transfer Options for FTP Session
            TransferOptions transferOp = new TransferOptions()
            {
                TransferMode = TransferMode.Binary
            };

            // Link to Base Directory, and Create Array of Strings With Full Directory and Individual Directroy Name
            DirectoryInfo baseDir = new DirectoryInfo(baseDirString);
            var subFolders = baseDir.GetDirectories().Select(x => new { x.FullName, x.Name });

            try
            {
                using (Session session = new Session())
                {
                    // Open Session
                    session.SessionLogPath = AppDomain.CurrentDomain.BaseDirectory + @"\logs\winscp." + DateTime.Now.ToString("yy-MM-dd") + ".log";
                    session.Open(sessionOptions);

                    // Loop Through Directroy Full names and Individual Names
                    foreach (var dir in subFolders)
                    {
                        directoryCount++;
                        Log.Information($"{tab}Tranfering files from {dir.Name} directory");

                        // Upload File
                        TransferOperationResult transferResult;
                        transferResult = session.PutFiles(dir.FullName + @"\*", appSettings["FtpDirectory"], false, transferOp);
                        transferResult.Check();

                        // For Logging
                        foreach (TransferEventArgs ev in transferResult.Transfers)
                        {
                            fileCount++;
                            Log.Information($"{tab}{tab}File {ev.FileName} has been transfered to FTP site");
                        }

                        // Move Directroy To Sent Directory
                        string sentDir = Path.Combine(sentDirString, dir.Name);
                        Directory.Move(dir.FullName, sentDir);
                        Log.Information($"{tab}Directory {dir.Name} successfully processed and moved");
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

            Log.Information($"Succesfully Processed {directoryCount} {(directoryCount == 1 ? "directory" : "directories")}.");
            Log.Information($"Succesfully Processed {fileCount} {(fileCount == 1 ? "files" : "file")}.");
            Log.Information($"Ending CID.Sftp.DirectoryTransfer {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\n");
        }
    }
}

