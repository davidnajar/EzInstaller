using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
namespace EzInstaller
{
    public class Packer : IDisposable
    {
        // Source file of standalone exe 
        protected readonly string sourceName = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SelfExtractorCode.cs");

        // Compressed files ready to embed as resource
        protected List<FileEntry> filenames = new List<FileEntry>();

        public void RemoveFile(string filename)
        {
            filenames.RemoveAll(item => item.FullName.EndsWith(filename));
        }
        public void AddFile(string filename)
        {




          AddFile(filename, "");
        }

        public void AddFile(string filename, string originFolder)
        {

            FileEntry fe = new FileEntry();
            fe.FullName = filename;
            if (!string.IsNullOrEmpty(originFolder))
            {
                fe.Entry = filename.Replace(originFolder, "");
            }
            else
            {
                fe.Entry =  Path.GetFileName(filename);
            }

            if (filenames.Any(p => p.FullName.EndsWith(filename)))
                return;

            // Compress input file using System.IO.Compression

            if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(TempDirectory, filename))))
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(TempDirectory, filename)));


            // Store filename so we can embed it on CompileArchive() call
            filenames.Add(fe);
        }
      /*  public void CreateFile(string relativePath, byte[] buffer)
        {
           
            var filename = Path.GetFileName(fullpath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }


            using (FileStream fs = new FileStream(Path.Combine(directory, filename), FileMode.Create))
            {
                fs.Write(buffer, 0, buffer.Length);
            }

            filenames.Add(new FileEntry(){
                FullName=fullpath,
                Entry="./"+
                )};

        }*/


        private string _tempDir = string.Empty;

        string TempDirectory
        {
            get
            {
                if (_tempDir == string.Empty)
                {
                    _tempDir = Guid.NewGuid().ToString();



                    if (!Directory.Exists((_tempDir)))
                    {
                        Directory.CreateDirectory(_tempDir);
                    }

                }
                return _tempDir;
            }
        }

        public void AddFolder(string foldername)
        {
           AddFolder(foldername,false);


        }

        public void AddFolder(string foldername, bool includeSubDirectories)
        {
            AddFiles(foldername, "*.*", includeSubDirectories, foldername);


        }
        public void AddFiles(string foldername, string filemask,  bool includeSubdirectories, string originFolder = "")
        {
            DirectoryInfo di = new DirectoryInfo(foldername);
            foreach (var p in di.GetFiles(filemask))
            {
               
                AddFile(p.FullName,originFolder);
            }

            if (includeSubdirectories)
            {
                foreach (var dir in di.GetDirectories())
                {
                    AddFiles(dir.FullName, filemask, includeSubdirectories,originFolder);
                }
            }

        }
        public void AddFiles(string foldername, string filemask,  string originFolder = "")
        {
           AddFiles(foldername, filemask, false, originFolder);

        }

        string GetRelativePath(string filespec)
        {
            var folder = Environment.CurrentDirectory;
            if (!Path.IsPathRooted(folder)) folder = Path.GetFullPath(folder);
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        public void CompileArchive(string archiveFilename)
        {
            var filename = Path.GetFileNameWithoutExtension(archiveFilename);
            var folder = Path.GetDirectoryName(archiveFilename);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var ext = Path.GetExtension(archiveFilename);
            if (!string.IsNullOrEmpty(ext))
            {
                //     archiveFilename = archiveFilename.Substring(0, (archiveFilename.Length - ext.Length ));
            }
            string path = Path.GetDirectoryName(archiveFilename);
            if (!Directory.Exists((path)))
            {
                Directory.CreateDirectory(path);
            }

            using (Stream zipFileStream = File.Create(Path.Combine(TempDirectory, filename + ".zip")))
            {
                using (var zip = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                {


                    foreach (var c in filenames)
                    {
                        var destinationfilename = c.Entry;
                        if ((destinationfilename.StartsWith("\\"))&&destinationfilename.Length>2)
                        {
                            destinationfilename = destinationfilename.Substring(1, destinationfilename.Length - 1);
                        }
                        if (destinationfilename.Contains(TempDirectory))
                        {
                            String[] pathSeparators = new String[] { "\\" };
                            var patharray = destinationfilename.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries);
                            var destinationArray = new List<string>();
                            patharray.ToList().ForEach(p =>
                            {
                                if (p != TempDirectory)
                                {
                                    destinationArray.Add(p);
                                }
                            });

                            destinationfilename = string.Join(Path.DirectorySeparatorChar.ToString(), destinationArray);
                        }

                        using (Stream fileStream = new FileStream(c.FullName, FileMode.Open, FileAccess.Read))
                        {
                            using (Stream fileStreamInZip = zip.CreateEntry(destinationfilename).Open())
                            {
                                fileStream.CopyTo(fileStreamInZip);
                            }

                        }

                   

                        
                    }
                }
            }


            CompileArchive(archiveFilename, false, true, null);

            //Borro el temp...
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }
            _tempDir = string.Empty;

        }

        public void CompileArchive(string archiveFilename, bool run1stItem, bool createshortcut, string iconFilename)
        {
            CodeDomProvider csc = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();
            var filename = Path.GetFileNameWithoutExtension(archiveFilename);

            cp.GenerateExecutable = true;
            cp.OutputAssembly = archiveFilename;
            cp.CompilerOptions = "/target:winexe";
            // Custom option to run a file after extraction  
            if (run1stItem)
            {
                cp.CompilerOptions += " /define:RUN_1ST_ITEM";
            }
            if (createshortcut)
            {
                cp.CompilerOptions += " /define:CREATE_SHORTCUT_DESKTOP";
            }
            if (!string.IsNullOrEmpty(iconFilename))
            {
                cp.CompilerOptions += " /win32icon:" + iconFilename;
            }
            cp.IncludeDebugInformation = true;
            cp.TempFiles.KeepFiles = true;
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            cp.ReferencedAssemblies.Add("System.IO.Compression.dll");
            cp.ReferencedAssemblies.Add("System.IO.Compression.FileSystem.dll");
            cp.ReferencedAssemblies.Add("System.Runtime.InteropServices.dll");
            string[] ass = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var manifestpath = ass.FirstOrDefault(p => p.EndsWith("app.manifest.txt"));

            //Creo el manifest...
            var manifestStringStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestpath);
            //cambio los valores del manifest..
            string manifestString = string.Empty;
            using (StreamReader reader = new StreamReader(manifestStringStream))
            {
                manifestString = reader.ReadToEnd();

            }
            manifestString = manifestString.Replace("MyApplication.app", Path.ChangeExtension(Path.GetFileName(archiveFilename), "app"));
            var AppManifestPath = Path.Combine(TempDirectory, Path.GetFileName(archiveFilename) + ".manifest");
            using (StreamWriter sw = new StreamWriter(AppManifestPath))
            {
                sw.Write(manifestString);
            }



            cp.CompilerOptions += string.Format(" /win32manifest:\"{0}\"", AppManifestPath);



            // Add compressed files as resource
            cp.EmbeddedResources.Add(Path.Combine(TempDirectory, filename + ".zip"));

            var sourcecodefile = ass.FirstOrDefault(p => p.EndsWith("SelfExtractorCode.cs"));
            if (string.IsNullOrEmpty(sourcecodefile))
                return;
            CompilerResults cr;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(sourcecodefile))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                cr = csc.CompileAssemblyFromSource(cp, result);
            }
            // Compile standalone executable with input files embedded as resource


            // yell if compilation error
            if (cr.Errors.Count > 0)
            {
                string msg = "Errors building " + cr.PathToAssembly;

                foreach (CompilerError ce in cr.Errors)
                {
                    msg += Environment.NewLine + ce.ToString();
                }
                throw new ApplicationException(msg);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
         
            filenames.Clear();

            GC.SuppressFinalize(this);
        }

        #endregion





    }
}

