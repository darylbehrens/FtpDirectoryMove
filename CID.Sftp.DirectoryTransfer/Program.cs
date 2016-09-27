using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;
using Serilog;
using System.Configuration;

namespace CID.Sftp.DirectoryTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string baseDirString = appSettings["BaseDirectory"];
            string sentDirString = appSettings["SentDirectory"];

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
                    session.Open(sessionOptions);

                    // Loop Through Directroy Full names and Individual Names
                    foreach (var dir in subFolders)
                    {
                        // Upload File
                        TransferOperationResult transferResult;
                        transferResult = session.PutFiles(dir.FullName + @"\*", appSettings["FtpDirectory"], false, transferOp);
                        transferResult.Check();

                        // For Logging
                        foreach (TransferEventArgs error in transferResult.Transfers)
                        {
                            Console.WriteLine($"File {error.FileName} has been transfered to FTP site at {error.Destination}");
                            // Need to Find Out What Information needs to be logged
                        }

                        // Move Directroy To Sent Directory
                        string sentDir = Path.Combine(sentDirString, dir.Name);
                        Directory.Move(dir.FullName, sentDir);
                    }
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine(ioEx.Message);
                // Log IO Exception from Directroy Move
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // Log Generic Exception which is what is thrown by WinScp
            }

            Console.ReadLine();
        }
    }
}

