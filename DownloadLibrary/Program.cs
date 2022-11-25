using Microsoft.SharePoint.Client;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DownloadLibrary
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Insert SharePoint site address");
            string siteUrl = Console.ReadLine();
            Console.WriteLine("Insert folder path");
            string libraryName = Console.ReadLine();
            Console.WriteLine("Insert domain\\username");
            string userName = Console.ReadLine();
            Console.WriteLine("Insert Password");
            var password = GetConsolePassword();
            DownloadAllDocumentsfromLibrary(siteUrl,libraryName,userName,password);
            Console.WriteLine("Folder download completed, keypress to close");
            Console.ReadKey();
        }
        public static void DownloadAllDocumentsfromLibrary(string siteUrl,string libraryName,string userName, string password)
        {
            ClientContext ctxSite = GetSPContext(siteUrl,userName,password);
            string libraryname = libraryName;
            var folder = ctxSite.Web.GetFolderByServerRelativeUrl(libraryname);
            //Modify this part fo change download location
            string pathString = string.Format(@"{0}{1}\", @"C:\", libraryname);
            if (!Directory.Exists(pathString))
                System.IO.Directory.CreateDirectory(pathString);
            GetFoldersAndFiles(folder, ctxSite, pathString);
        }

        private static void GetFoldersAndFiles(Folder mainFolder, ClientContext clientContext, string pathString)
        {
            try
            {
                clientContext.Load(mainFolder, k => k.Name, k => k.Files, k => k.Folders);
                System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
                clientContext.ExecuteQuery();
                foreach (var folder in mainFolder.Folders)
                {
                    string subfolderPath = string.Format(@"{0}{1}\", pathString, folder.Name);
                    if (!Directory.Exists(subfolderPath))
                        System.IO.Directory.CreateDirectory(subfolderPath);

                    GetFoldersAndFiles(folder, clientContext, subfolderPath);
                }

                foreach (var file in mainFolder.Files)
                {
                    var fileName = Path.Combine(pathString, file.Name);
                    if (!System.IO.File.Exists(fileName))
                    {
                        var fileRef = file.ServerRelativeUrl;
                        var fileInfo = Microsoft.SharePoint.Client.File.OpenBinaryDirect(clientContext, fileRef);
                        using (var fileStream = System.IO.File.Create(fileName))
                        {
                            fileInfo.Stream.CopyTo(fileStream);
                        }
                        Console.WriteLine( fileName + " downloaded");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error "+ex.Message);
            }
        }

        private static string GetConsolePassword()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        Console.Write("\b\0\b");
                        sb.Length--;
                    }

                    continue;
                }

                Console.Write('*');
                sb.Append(cki.KeyChar);
            }

            return sb.ToString();
        }

        private static ClientContext GetSPContext(string siteUrl,string userName, string password)
        {
            ClientContext spContext = new ClientContext(siteUrl);
            //This for SharePoint OnPrem Authenitication           
            NetworkCredential networkCredential = new NetworkCredential(userName, password);    
            spContext.Credentials = networkCredential;
            return spContext;
        }




    }


}

