﻿using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack;
using ServiceStack.OrmLite;

namespace WP6Service2
{
	[Route("/sbfiles")]
	[Route("/sbfiles/{path*}")]
	/*** DTO of file infos
	 */
	public class SBFile
	{
		public String name { get; set;}
		public FileAttributes attributes { get; set; }
		public long size { get; set; }
		public String date { get; set; }
		public String path { get; set; }

		public Boolean directory { get; set; }
		public Boolean read { get; set; }
		public Boolean write { get; set; }
		public String webdavuri { get; set; }
	}

	public class SBFileService : Service
	{
		public String root = "/home/vagrant/work/b2drop/";
		public String webdavroot = "/webdav/b2drop/";

		/*** returns list of files and directories under specified path of the configured root
		 * directory. 
		 */
		public object Get(SBFile request) 
		{
			//sets default subpath
			var path = (request.path!=null)?request.path:"";

			if ((request.path!=null) && request.path.Contains (".."))
				path = "";//prevents directory listing outside 
			var di = new DirectoryInfo (root+path);
			var fis = di.GetFileSystemInfos();
			List<SBFile> listOfFiles = new List<SBFile> ();
			//mapping FileSystemInfos into list structure returned to client
			foreach (var fi in fis) {
				Boolean isdirectory = ! (fi.GetType ().Equals (typeof(FileInfo)));

				long mysize = isdirectory ? 0 : ((FileInfo)fi).Length;
				listOfFiles.Add(new SBFile(){
					path=path,
					name=fi.Name,
					attributes=fi.Attributes,//.ToString(),
					size=mysize,
					date=fi.LastWriteTime.ToLongDateString(),
					directory=isdirectory,
					read=true,
					write = (fi.Attributes & FileAttributes.ReadOnly)>0? false:true,
					webdavuri = webdavroot+path+"/"+fi.Name 
				});
			};
			return listOfFiles; //returns all
		}
	}
}

