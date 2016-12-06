﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dropbox.Api;
using ServiceStack;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;

namespace WP6Service2
{
    [Route("/sbfiles/dropbox/{path*}")]
    public class DropBoxSBFile : SBFile
    {
    }

    public partial class SBFileService : Service
    {
        /** returns list of files and directories under specified path of the configured root
         * directory.
         */

        public object Get(DropBoxSBFile request)
        {
            Console.WriteLine("Get( " + request.path + " )");
            String path = (request.path != null) ? request.path : "";
            if ((request.path != null) && request.path.Contains(".."))
                path = ""; //prevents directory listing outside
            //MAIN splitter for strategies of listing files
            return DropBoxFS.ListOfFiles(path);
        }
    }

    public static class DropBoxFS
    {
        private static DropboxClient dbx;
        private static Boolean initialized = false;
        public static String accesstoken = "";

        public static async void Initialize(){
            //TODO change access token to user specific
            Console.WriteLine("Dropbox Init()");
            //setting proxy
            WebProxy proxy = new WebProxy("http://wwwcache.dl.ac.uk:8080/", false);
            var drconfig = new DropboxClientConfig()
            {
                HttpClient = null
            };
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = proxy
            };
            drconfig.HttpClient = new HttpClient(httpClientHandler);
            //setting dropboxclient
            dbx = new DropboxClient(accesstoken, drconfig);
            Console.WriteLine("Dropbox Init() dbx created:"+dbx.Dump());
            var full = await dbx.Users.GetCurrentAccountAsync();
            Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
            Console.WriteLine("Dropbox initialized");
            initialized = true;
        }

        public static object ListOfFiles(String path)
        {

            Console.WriteLine("ListOfFiles( "+path+" )");
            var mytask = ListOfFilesAsync(path);
            mytask.Wait();
            return mytask.Result;
        }

        public static async Task<object> ListOfFilesAsync(String path)
        {
            if (!initialized) Initialize();
            Console.WriteLine("ListOfFilesAsync( "+path+" )");
            var dropboxpath = path.Length > 0 ? "/" + path : String.Empty; //leading slash otherwise empty
            Console.WriteLine("ListOfFilesAsync( "+dropboxpath+" )");
            try
            {
                var list = await dbx.Files.ListFolderAsync(dropboxpath);
                bool hasmoreresults = false;
                List<SBFile> listOfFiles = new List<SBFile>();
                do
                {
                    Console.WriteLine("ListOfFilesAsync(), result.Count: " + list.Entries.Count);

                    //mapping FileSystemInfos into list structure returned to client
                    foreach (var fi in list.Entries.Where(i => i.IsFolder))
                    {
                        Console.WriteLine("adding path:[" + path + "] name:[" + fi.Name + "]");
                        listOfFiles.Add(new SBFile()
                        {
                            path = path,
                            name = fi.Name,
                            attributes = FileAttributes.Directory, //.ToString(),
                            size = 0,
                            date = DateTime.Now,
                            filetype = FileType.Directory & FileType.Read & FileType.Write,
                            //TODO introduce GET on file - which will download the file and redirects to webdav uri
                            webdavuri = path + "/" + fi.Name
                        });
                    }

                    foreach (var fi in list.Entries.Where(i => i.IsFile))
                    {
                        Console.WriteLine("adding path:[" + path + "] name:[" + fi.Name + "]");
                        listOfFiles.Add(new SBFile()
                        {
                            path = path,
                            name = fi.Name,
                            attributes = FileAttributes.Normal, //.ToString(),
                            size = fi.AsFile.Size,
                            date = fi.AsFile.ServerModified,
                            filetype = FileType.Read & FileType.Write,
                            //TODO introduce GET on file - which will download the file and redirects to webdav uri
                            webdavuri = path + "/" + fi.Name
                        });
                    }

                    if (list.HasMore)
                    {
                        hasmoreresults = false; //true
                        //list = await dbx.Files.ListFolderContinueAsync(ListFolderContinueArg);
                    }
                    else
                        hasmoreresults = false;
                } while (hasmoreresults);

                Console.WriteLine("ListOfFilesAsync done");
                return listOfFiles; //returns all
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message+" "+e.StackTrace);
                throw e;
            }
        }

    }
}