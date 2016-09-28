using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace CID.Sftp.DirectoryTransfer
{
    class Options
    {
        [Option('b', "base", Required = true, HelpText = "Base Directroy To Scan")]
        public string BaseDirectory { get; set; }

        [Option('s', "sent", Required = true, HelpText = "Directory To Send Scanned Directories To")]
        public string SentDirectory { get; set; }

        [Option('d', "ftpDir", Required = true, HelpText = "FTP Directory To Send Files To")]
        public string FtpDirectory { get; set; }


        [Option('a', "ftpAddress", Required = true, HelpText = "FTP Address To Send Files To")]
        public string FtpAddress { get; set; }


        [Option('l', "ftpLogin", Required = true, HelpText = "FTP User Name")]
        public string FtpLogin { get; set; }
    }
}
