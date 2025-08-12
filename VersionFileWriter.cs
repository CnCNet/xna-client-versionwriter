/*
 * Copyright © 2025 Starkku
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading;
using Starkku.Utilities;
using Starkku.Utilities.FileTypes;

namespace VersionWriter
{
    /// <summary>
    /// Version file writer.
    /// </summary>
    class VersionFileWriter
    {
        private string _basePath;

        /// <summary>
        /// Directory where to run the version file writer.
        /// </summary>
        public string BasePath
        {
            get { return _basePath; }
            set
            {
                _basePath = value;
                ParseConfigFile();
            }
        }

        /// <summary>
        /// Directory where to copy updated files.
        /// </summary>
        public string CopyDirectory { get; set; } = AppDomain.CurrentDomain.FriendlyName.Substring(0, AppDomain.CurrentDomain.FriendlyName.IndexOf('.')) + "-CopiedFiles";

        /// <summary>
        /// Whether or not version file writer is ready to run (configuration files have been successfully parsed etc).
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Updater version.
        /// </summary>
        public string UpdaterVersion { get; set; }

        /// <summary>
        /// Manual download URL.
        /// </summary>
        public string ManualDownloadURL { get; set; }

        /// <summary>
        /// Whether or not extended updater features are enabled.
        /// </summary>
        public bool EnableExtendedUpdaterFeatures { get; set; }

        /// <summary>
        /// Whether or not recursive directory search is enabled.
        /// </summary>
        public bool RecursiveDirectorySearch { get; set; }

        /// <summary>
        /// Whether or not only files that have been changed will be included in version file.
        /// </summary>
        public bool IncludeOnlyChangedFiles { get; set; }

        /// <summary>
        /// If set to true no files will be copied at all, only version file is generated. This will also disable use of archived files.
        /// </summary>
        public bool NoCopyMode { get; set; }

        /// <summary>
        /// If set to true, version name is parsed as <see cref="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">timestamp/datetime string</see> with current timestamp applied on it.
        /// </summary>
        public bool ApplyTimestampOnVersion { get; set; }

        /// <summary>
        /// Whether or not non-archived files of archived files will also be copied to copied files directory.
        /// </summary>
        public bool CopyArchivedOriginalFiles { get; set; }

        /// <summary>
        /// Whether or not system and hidden files are excluded unless explicitly included by filename.
        /// </summary>
        public bool ExcludeHiddenAndSystemFiles { get; set; }

        private INIFile config;

        private readonly List<FileItem> filesInclude = new List<FileItem>();
        private readonly List<CustomComponentItem> customComponents = new List<CustomComponentItem>();
        private List<FileItem> filesPrevious = new List<FileItem>();

        private const string ARCHIVE_EXTENSION = ".lzma";

        /// <summary>
        /// Create new instance of version file writer.
        /// </summary>
        /// <param name="basePath">Directory where to run version file writer.</param>
        public VersionFileWriter(string basePath)
        {
            BasePath = basePath;
        }

        /// <summary>
        /// Parse version file writer configuration file.
        /// </summary>
        private void ParseConfigFile()
        {
            Initialized = false;

            string configFilename = Path.Combine(BasePath, "versionconfig.ini");

            if (!File.Exists(configFilename))
            {
                Logger.Error("Configuration file '" + configFilename + "' does not exist.");
                return;
            }

            config = new INIFile(configFilename);

            Logger.Info("Parsing configuration file '" + configFilename + "'.");

            var keys = config.GetKeys("Version");
            if (keys != null && keys.Length > 0)
                Version = keys[0];

            if (string.IsNullOrEmpty(Version))
            {
                Logger.Error("Configuration file does not declare [Version].");
                return;
            }

            ParseOptions();

            keys = config.GetKeys("Include");
            if (keys == null || keys.Length < 1)
            {
                Logger.Error("Configuration file '" + configFilename + "' does not list any files or directories in [Include].");
                return;
            }

            Logger.Info("Checking directories & files in [Include].");

            filesInclude.Clear();
            filesInclude.AddRange(GetFilesFromConfig(config, out bool success));

            if (!success)
                return;

            Logger.Info("Checking custom components.");

            customComponents.Clear();
            customComponents.AddRange(GetCustomComponentsFromConfig(config, out success));

            if (!success)
                return;

            Initialized = true;
        }

        /// <summary>
        /// Generate version file.
        /// </summary>
        /// <param name="supressInputs">If set, does not ask confirmation on overriding copied files.</param>
        /// <returns>True if successful, otherwise false.</returns>
        public bool GenerateVersionFile(bool supressInputs)
        {
            if (!Initialized)
            {
                Logger.Error("Cannot generate version file because version file writer is not initialized.");
                return false;
            }

            Logger.Info("Checking for updated files.");

            var filesUpdated = GetUpdatedFiles();

            Logger.Info("Checking for updated custom components.");

            var customComponentsUpdated = GetUpdatedCustomComponents();
            string directory = string.Empty;

            if (filesUpdated.Count > 0 || customComponentsUpdated.Count > 0)
            {
                if (!NoCopyMode)
                {
                    Logger.Info("Copying updated files to subdirectory " + CopyDirectory + ".");

                    directory = Path.Combine(BasePath, CopyDirectory);

                    if (!HandleOverwriting(directory, supressInputs))
                        return false;

                    if (!CopyFiles(directory, new List<IFileItem>(filesUpdated)))
                        return false;

                    if (!CopyFiles(directory, new List<IFileItem>(customComponentsUpdated), true))
                        return false;
                }

                Logger.Info("Writing version file.");
                
                if (IncludeOnlyChangedFiles)
                {
                    if (!WriteVersionFile("version", filesUpdated, filesPrevious))
                        return false;

                    if (!WriteVersionFile("version_base", filesInclude))
                        return false;
                }
                else
                {
                    if (!WriteVersionFile("version", filesInclude))
                        return false;
                }

                if (!NoCopyMode && Directory.Exists(directory))
                    File.Copy(Path.Combine(BasePath, "version"), Path.Combine(directory, "version"));

                Logger.Info("Finished generating new version file.");
            }
            else
            {
                Logger.Warn("No updated files or custom components found. No files will be copied & no version files will be written.");
            }

            return true;
        }

        /// <summary>
        /// Writes version file.
        /// </summary>
        /// <param name="filename">Filename of version file.</param>
        /// <param name="files">List of files to include in version file.</param>
        /// <param name="previousFiles">List of files from previous version file.</param>
        /// <returns>True if successful, otherwise false.</returns>
        private bool WriteVersionFile(string filename, List<FileItem> files, List<FileItem> previousFiles = null)
        {
            string path = Path.Combine(BasePath, filename);
            INIFile versionOld = new INIFile(path);
            File.Delete(path);
            INIFile version = new INIFile(path);

            if (!ApplyTimestampOnVersion)
                version.SetKey("DTA", "Version", Version);
            else
                version.SetKey("DTA", "Version", DateTime.Now.ToString(Version));

            if (EnableExtendedUpdaterFeatures)
            {
                if (!string.IsNullOrEmpty(UpdaterVersion))
                    version.SetKey("DTA", "UpdaterVersion", UpdaterVersion);

                if (!string.IsNullOrEmpty(ManualDownloadURL))
                    version.SetKey("DTA", "ManualDownloadURL", ManualDownloadURL);
            }

            version.AddSection("FileVersions");
            version.AddSection("AddOns");

            if (!NoCopyMode && EnableExtendedUpdaterFeatures)
                version.AddSection("ArchivedFiles");

            foreach (FileItem file in files)
            {
                version.SetKey("FileVersions", file.Filename, file.ID + "," + file.Size);

                if (!NoCopyMode && EnableExtendedUpdaterFeatures && file.Archived)
                {
                    GetArchiveIDAndSize(versionOld.GetKey("ArchivedFiles", file.Filename, ""), out string archiveID, out int archiveSize);

                    if ((string.IsNullOrEmpty(file.ArchiveID) || file.ArchiveSize < 1) && !string.IsNullOrEmpty(archiveID) && archiveSize > -1)
                        version.SetKey("ArchivedFiles", file.Filename, archiveID + "," + archiveSize);
                    else
                        version.SetKey("ArchivedFiles", file.Filename, file.ArchiveID == null ? "0" : file.ArchiveID + "," + file.ArchiveSize);
                }
            }

            if (previousFiles != null)
            {
                foreach (FileItem file in previousFiles)
                {
                    if (files.Find(x => x.Filename == file.Filename) == null && filesInclude.Find(x => x.Filename == file.Filename) != null)
                    {
                        version.SetKey("FileVersions", file.Filename, file.ID + "," + file.Size);

                        if (!NoCopyMode && EnableExtendedUpdaterFeatures && file.Archived)
                            version.SetKey("ArchivedFiles", file.Filename, file.ArchiveID + "," + file.ArchiveSize);
                    }
                }
            }

            foreach (CustomComponentItem component in customComponents)
            {
                version.SetKey("AddOns", component.ComponentID, component.ID + "," + component.Size);

                if (!NoCopyMode && EnableExtendedUpdaterFeatures && component.Archived)
                {
                    GetArchiveIDAndSize(versionOld.GetKey("ArchivedFiles", component.Filename, ""), out string archiveID, out int archiveSize);

                    if ((string.IsNullOrEmpty(component.ArchiveID) || component.ArchiveSize < 1) && !string.IsNullOrEmpty(archiveID) && archiveSize > -1)
                        version.SetKey("ArchivedFiles", component.Filename, archiveID + "," + archiveSize);
                    else
                        version.SetKey("ArchivedFiles", component.Filename, component.ArchiveID == null ? "0" : component.ArchiveID + "," + component.ArchiveSize);
                }
            }

            string errorMessage = version.Save();

            if (errorMessage != null)
            {
                Logger.Error("Could not save version file '" + filename + "'. Error message: " + errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copy specific sets of files.
        /// </summary>
        /// <param name="directory">Directory to copy to.</param>
        /// <param name="files">Files to copy.</param>
        /// <param name="isCustomComponents">If set, treat as custom components.</param>
        /// <returns>True if successful, otherwise false.</returns>
        private bool CopyFiles(string directory, List<IFileItem> files, bool isCustomComponents = false)
        {
            try
            {
                foreach (IFileItem file in files)
                {
                    string filename = Path.Combine(BasePath, file.Filename);

                    if (CopyArchivedOriginalFiles || !file.Archived)
                    {
                        string copyFilename = Path.Combine(directory, file.Filename);
                        Directory.CreateDirectory(Path.GetDirectoryName(copyFilename));
                        File.Copy(filename, copyFilename, true);
                        Logger.Info((isCustomComponents ? "Custom component: " : "") + file.Filename, ConsoleColor.Magenta);
                    }

                    if (EnableExtendedUpdaterFeatures && file.Archived)
                    {
                        string archivename = file.Filename + ARCHIVE_EXTENSION;
                        string archiveFilename = Path.Combine(directory, archivename);
                        Logger.Info((isCustomComponents ? "Custom component: " : "") + "Compressing archive: " + archivename + "...", ConsoleColor.Magenta);
                        Directory.CreateDirectory(Path.GetDirectoryName(archiveFilename));
                        CompressionHelper.CompressFile(filename, archiveFilename);
                        file.ArchiveSize = GetSize(archiveFilename);
                        file.ArchiveID = GetUniqueID(archiveFilename);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error when copying " + (isCustomComponents ? "custom components" : "files") + ". Message: " + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles overwriting existing copied files directory & files.
        /// </summary>
        /// <param name="directory">Directory to copy to.</param>
        /// <param name="doNotAskOverwrite">If set, attempts to overwrite existing files without asking user.</param>
        /// <returns>True if successful, otherwise false.</returns>
        private bool HandleOverwriting(string directory, bool doNotAskOverwrite)
        {
            if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            {
                bool response = true;

                if (!doNotAskOverwrite)
                {
                    ConsoleColor defaultColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("");
                    Console.WriteLine("Directory '" + CopyDirectory + "' already exists and is not empty.");
                    Console.WriteLine("Do you want to remove it and ALL files & subdirectories within before copying files?");
                    Console.ForegroundColor = defaultColor;

                    response = GetConsoleYNResponse();
                }

                if (response)
                {
                    Logger.Info("Attempting to delete directory '" + CopyDirectory + "' and all subdirectories & files.");
                    int counter = 2;
                    while (Directory.Exists(directory))
                    {
                        if (counter == 0) break;
                        try
                        {
                            Directory.Delete(directory, true);
                            Thread.Sleep(1000);
                        }
                        catch (Exception e)
                        {
                            Logger.Warn("Error deleting directory '" + CopyDirectory + "'. Message: " + e.Message);

                            if (!doNotAskOverwrite)
                            {
                                Console.WriteLine("");
                                Console.WriteLine("Press any key to retry. (" + counter-- + " attempts left)");
                                Console.ReadKey();
                            }
                        }
                    }
                    if (counter > 0)
                    {
                        Logger.Info("Directory " + CopyDirectory + " and all files in it were successfully removed.", ConsoleColor.Green);
                        Directory.CreateDirectory(directory);
                    }
                    else
                    {
                        Logger.Error("Directory " + CopyDirectory + " and/or files in it could not be removed. Aborting.");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets list of changed files based on options.
        /// </summary>
        /// <returns>List of changed files.</returns>
        private List<FileItem> GetUpdatedFiles()
        {
            List<FileItem> files = filesInclude;
            string versionFilename = IncludeOnlyChangedFiles ? "version_base" : "version";
            string path = Path.Combine(BasePath, versionFilename);

            if (File.Exists(path))
            {
                INIFile versionFile = new INIFile(path);
                filesPrevious = GetFilesFromVersionFile(versionFile);
                files = GetChangedFiles(filesInclude, filesPrevious);
            }
            else
            {
                Logger.Warn("Could not parse files from version file '" + path + "'.");
            }

            return files;
        }

        /// <summary>
        /// Gets list of changed custom components based on options.
        /// </summary>
        /// <returns>List of changed custom components.</returns>
        private List<CustomComponentItem> GetUpdatedCustomComponents()
        {
            string versionFilename = IncludeOnlyChangedFiles ? "version_base" : "version";
            string path = Path.Combine(BasePath, versionFilename);

            if (!File.Exists(path))
            {
                Logger.Warn("Could not parse custom components from version file '" + path + "'.");
                return customComponents;
            }

            INIFile versionFile = new INIFile(path);
            var customComponentsOld = GetCustomComponentsFromVersionFile(versionFile);

            List<CustomComponentItem> newComponents = new List<CustomComponentItem>();

            foreach (CustomComponentItem component in customComponents)
            {
                var tmp = customComponentsOld.Find(x => x.ComponentID == component.ComponentID);
                if (tmp == null || tmp.ID != component.ID)
                {
                    newComponents.Add(component);
                    continue;
                }
            }

            return newComponents;
        }

        /// <summary>
        /// Parse options from configuration file.
        /// </summary>
        private void ParseOptions()
        {
            EnableExtendedUpdaterFeatures = config.GetKeyAsBool("Options", "EnableExtendedUpdaterFeatures", false);

            if (EnableExtendedUpdaterFeatures)
                Logger.Info("Option enabled: EnableExtendedUpdaterFeatures - Extended features enabled for versionconfig.ini.", ConsoleColor.Green);

            RecursiveDirectorySearch = config.GetKeyAsBool("Options", "RecursiveDirectorySearch", false);

            if (RecursiveDirectorySearch)
                Logger.Info("Option enabled: RecursiveDirectorySearch - Search from directories recursively.", ConsoleColor.Green);

            IncludeOnlyChangedFiles = config.GetKeyAsBool("Options", "IncludeOnlyChangedFiles", false);

            if (IncludeOnlyChangedFiles)
                Logger.Info("Option enabled: IncludeOnlyChangedFiles - Only include info for changed files.", ConsoleColor.Green);

            ExcludeHiddenAndSystemFiles = config.GetKeyAsBool("Options", "ExcludeHiddenAndSystemFiles", true);

            if (!ExcludeHiddenAndSystemFiles)
                Logger.Info("NOTE: Exclusion of hidden and system files has been disabled in configuration file.", ConsoleColor.Yellow);

            NoCopyMode = config.GetKeyAsBool("Options", "NoCopyMode", false);

            if (NoCopyMode)
                Logger.Info("Option enabled: NoCopyMode - No files will be copied, archived files are also disabled.", ConsoleColor.Green);

            ApplyTimestampOnVersion = config.GetKeyAsBool("Options", "ApplyTimestampOnVersion", false);

            if (ApplyTimestampOnVersion)
                Logger.Info("Option enabled: ApplyTimestampOnVersion - Version name is treated as standard .NET date and time format string.", ConsoleColor.Green);

            if (!NoCopyMode && EnableExtendedUpdaterFeatures)
            {
                CopyArchivedOriginalFiles = config.GetKeyAsBool("Options", "CopyArchivedOriginalFiles", false);

                if (CopyArchivedOriginalFiles)
                    Logger.Info("Option enabled: CopyArchivedOriginalFiles - Original versions of archived files will also be copied to copied files directory.", ConsoleColor.Green);
            }

            if (EnableExtendedUpdaterFeatures)
            {
                var keys = config.GetKeys("UpdaterVersion");
                if (keys != null && keys.Length > 0)
                    UpdaterVersion = keys[0];

                if (string.IsNullOrEmpty(UpdaterVersion))
                    Logger.Warn("Configuration file does not declare [UpdateVersion] It will not be written to version file.");

                keys = config.GetKeys("ManualDownloadURL");
                if (keys != null && keys.Length > 0)
                    ManualDownloadURL = keys[0];

                if (string.IsNullOrEmpty(ManualDownloadURL))
                    Logger.Warn("Configuration file does not declare [ManualDownloadURL] It will not be written to version file.");
            }
        }

        /// <summary>
        /// Returns list of changed files.
        /// </summary>
        /// <param name="filesNew">New files.</param>
        /// <param name="filesOld">Old files.</param>
        /// <returns>List of changed files.</returns>
        private List<FileItem> GetChangedFiles(List<FileItem> filesNew, List<FileItem> filesOld)
        {
            List<FileItem> files = new List<FileItem>();
            foreach (FileItem file in filesNew)
            {
                FileItem newFile = null;

                if (filesOld != null)
                    newFile = filesOld.Find(x => x.Filename == file.Filename);

                if (newFile != null && file.ID == newFile.ID)
                {
                    if (!(newFile.Archived && string.IsNullOrEmpty(newFile.ArchiveID)))
                        continue;
                }

                Logger.Info(file.Filename, ConsoleColor.Green);
                files.Add(file);
            }

            return files;
        }

        /// <summary>
        /// Gets list of files from version file config.
        /// </summary>
        /// <param name="config">Version file config.</param>
        /// <param name="success">Set to true if successful, otherwise false.</param>
        /// <returns>List of files from version file config.</returns>
        private List<FileItem> GetFilesFromConfig(INIFile config, out bool success)
        {
            List<FileItem> files = new List<FileItem>();
            success = true;
            string[] paths = config.GetKeys("Include");
            string[] excludedFiles = config.GetKeys("ExcludeFiles");
            string[] excludedDirectories = config.GetKeys("ExcludeDirectories");
            string[] archiveFiles = config.GetKeys("ArchiveFiles");

            if (paths == null)
                return files;

            foreach (string path in paths)
            {
                ProcessPath(path, files, excludedFiles, excludedDirectories, archiveFiles, out bool successOnPath);

                if (!successOnPath)
                {
                    success = false;
                    return files;
                }
            }

            return files;
        }

        private void ProcessPath(string path, List<FileItem> files, string[] excludedFiles, string[] excludedDirectories, string[] archiveFiles, out bool success)
        {
            success = true;
            string fullPath = Path.Combine(BasePath + path);
            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(fullPath));

            if (dirInfo.Exists)
            {
                if (ExcludeHiddenAndSystemFiles)
                {
                    var attributes = dirInfo.Attributes;

                    if (attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System))
                    {
                        Logger.Warn("Included directory '" + path + "' is hidden or a system directory. Skipping.");
                        return;
                    }
                }
                
                Logger.Info("Included path '" + path + "' is a directory. Including all files in it.", ConsoleColor.Green);
                FileInfo[] fileInfos = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                foreach (var file in fileInfos)
                {
                    string filename = file.FullName.Replace(BasePath, "");

                    if (ExcludeHiddenAndSystemFiles)
                    {
                        var attributes = file.Attributes;

                        if (attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System))
                        {
                            Logger.Warn("Included file '" + filename + "' is hidden or a system file. Skipping.");
                            continue;
                        }
                    }

                    var includedFile = GetFileFromConfig(filename, excludedFiles, excludedDirectories, archiveFiles, out bool successOnFile);

                    if (!successOnFile)
                    {
                        success = false;
                        return;
                    }

                    if (includedFile == null)
                        continue;
                    else
                        files.Add(includedFile);
                }

                if (RecursiveDirectorySearch)
                {
                    DirectoryInfo[] directoryInfos = dirInfo.GetDirectories("*.*", SearchOption.TopDirectoryOnly);

                    foreach (var directory in directoryInfos)
                    {
                        ProcessPath(directory.FullName.Replace(BasePath, ""), files, excludedFiles, excludedDirectories, archiveFiles, out bool successOnDir);

                        if (!successOnDir)
                        {
                            success = false;
                            return;
                        }
                    }
                }
            }
            else
            {
                var includedFile = GetFileFromConfig(path, excludedFiles, excludedDirectories, archiveFiles, out bool successOnFile);

                if (!successOnFile)
                {
                    success = false;
                    return;
                }

                if (includedFile == null)
                    return;
                else
                    files.Add(includedFile);
            }
        }

        /// <summary>
        /// Gets individual file from version file config.
        /// </summary>
        /// <param name="filename">Filename of files.</param>
        /// <param name="excludedFiles">List of excluded files.</param>
        /// <param name="excludedFiles">List of excluded directories.</param>
        /// <param name="archiveFiles">List of files to be archived.</param>
        /// <param name="success">Set to true if successful, otherwise false.</param>
        /// <returns>The file, or null if it is excluded.</returns>
        private FileItem GetFileFromConfig(string filename, IEnumerable<string> excludedFiles, IEnumerable<string> excludedDirectories, IEnumerable<string> archiveFiles, out bool success)
        {
            success = true;
            string path = Path.Combine(BasePath, filename);

            if (!File.Exists(path))
            {
                Logger.Error("Included file '" + filename + "' does not exist. Aborting.");
                success = false;
                return null;
            }
            else if (excludedDirectories != null && IsDirectoryExcluded(filename, excludedDirectories))
            {
                Logger.Warn("Included file '" + filename + "' is in an excluded directory. Skipping.");
                return null;
            }
            else if (excludedFiles != null && excludedFiles.Contains(filename))
            {
                Logger.Warn("Included file '" + filename + "' is on exclude list. Skipping.");
                return null;
            }

            Logger.Info(filename, ConsoleColor.Magenta);

            FileItem file = new FileItem
            {
                Filename = filename,
                ID = GetUniqueID(path),
                Size = GetSize(path)
            };

            if (archiveFiles != null && archiveFiles.Contains(filename))
                file.Archived = true;

            return file;
        }

        /// <summary>
        /// Gets list of files from version file.
        /// </summary>
        /// <param name="versionFile">Version file.</param>
        /// <returns>List of files from version file.</returns>
        private List<FileItem> GetFilesFromVersionFile(INIFile versionFile)
        {
            List<FileItem> files = new List<FileItem>();
            var kvps = versionFile.GetKeyValuePairs("FileVersions");

            if (kvps == null)
            {
                Logger.Warn("Could not parse files from version file '" + versionFile.Filename + "'.");
                return files;
            }

            foreach (KeyValuePair<string, string> kvp in kvps)
            {
                FileItem file = new FileItem
                {
                    Filename = kvp.Key
                };

                GetArchiveIDAndSize(config.GetKey("ArchivedFiles", file.Filename, ""), out string archiveID, out int archiveSize);
                file.Archived = archiveSize > -1 || !string.IsNullOrEmpty(archiveID);

                if (file.Archived)
                {
                    file.ArchiveID = archiveID;
                    file.ArchiveSize = archiveSize;
                }

                string[] tmp = kvp.Value.Split(',');

                if (tmp.Length < 2 || tmp.Length > 3)
                    continue;

                file.ID = tmp[0];

                if (int.TryParse(tmp[1], out int sizeResult))
                    file.Size = sizeResult;

                files.Add(file);
            }

            return files;
        }

        /// <summary>
        /// Gets list of custom components from version file config.
        /// </summary>
        /// <param name="config">Version file config.</param>
        /// <returns>List of custom components from version file config.</returns>
        private List<CustomComponentItem> GetCustomComponentsFromConfig(INIFile config, out bool success)
        {
            List<CustomComponentItem> components = new List<CustomComponentItem>();
            success = true;

            if (!config.SectionExists("AddOns"))
                return components;

            foreach (KeyValuePair<string, string> kvp in config.GetKeyValuePairs("AddOns"))
            {
                string path = Path.Combine(BasePath, kvp.Value);

                if (!File.Exists(path))
                {
                    Logger.Error("Custom component file '" + path + "' does not exist. Aborting.");
                    success = false;
                    return null;
                }

                Logger.Info(kvp.Key + "=" + kvp.Value, ConsoleColor.Magenta);

                CustomComponentItem component = new CustomComponentItem
                {
                    ComponentID = kvp.Key,
                    ID = GetUniqueID(path),
                    Size = GetSize(path),
                    Filename = kvp.Value,
                    Archived = IsArchived(config, kvp.Value)
                };

                components.Add(component);
            }

            return components;
        }

        /// <summary>
        /// Gets list of custom components from version file.
        /// </summary>
        /// <param name="versionFile">Version file.</param>
        /// <returns>List of custom components from version file.</returns>
        private List<CustomComponentItem> GetCustomComponentsFromVersionFile(INIFile versionFile)
        {
            List<CustomComponentItem> components = new List<CustomComponentItem>();
            if (config.SectionExists("AddOns"))
            {
                foreach (KeyValuePair<string, string> kvp in versionFile.GetKeyValuePairs("AddOns"))
                {
                    string[] val = kvp.Value.Split(',');
                    if (val.Length < 2)
                        continue;

                    CustomComponentItem add = new CustomComponentItem
                    {
                        ComponentID = kvp.Key,
                        ID = val[0]
                    };

                    if (int.TryParse(val[1], out int sizeResult))
                        add.Size = sizeResult;
                    else
                        continue;

                    GetArchiveIDAndSize(config.GetKey("ArchivedFiles", add.Filename, ""), out string archiveID, out int archiveSize);

                    add.Archived = archiveSize > -1 || !string.IsNullOrEmpty(archiveID);
                    if (add.Archived)
                    {
                        add.ArchiveID = archiveID;
                        add.ArchiveSize = archiveSize;
                    }

                    components.Add(add);
                }
            }

            return components;
        }

        /// <summary>
        /// Is file set to be archived.
        /// </summary>
        /// <param name="config">Version file config.</param>
        /// <param name="filename">Filename of file.</param>
        /// <returns>Whether or not file is set to be archived.</returns>
        private bool IsArchived(INIFile config, string filename)
        {
            if (!config.SectionExists("ArchiveFiles"))
                return false;

            foreach (KeyValuePair<string, string> kvp in config.GetKeyValuePairs("ArchiveFiles"))
            {
                if (kvp.Value == filename)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get archive ID and size from version file entry string.
        /// </summary>
        /// <param name="entry">Entry string.</param>
        /// <param name="archiveID">Set to archive ID.</param>
        /// <param name="archiveSize">Set to archive size.</param>
        private void GetArchiveIDAndSize(string entry, out string archiveID, out int archiveSize)
        {
            archiveSize = -1;
            archiveID = null;

            if (string.IsNullOrEmpty(entry))
                return;

            string[] values = entry.Split(',');

            if (values.Length < 1)
                return;

            string archSize = string.Empty;

            if (values.Length == 1)
                archSize = values[0];

            else if (values.Length >= 2)
            {
                archiveID = values[0];
                archSize = values[1];
            }

            if (int.TryParse(archSize, out int archSizeResult))
                archiveSize = archSizeResult;
        }

        /// <summary>
        /// Get size of file in kilobytes.
        /// </summary>
        /// <param name="filename">Filename of file.</param>
        /// <returns>Size of file in kilobytes.</returns>
        private static int GetSize(string filename)
        {
            return Convert.ToInt32((new FileInfo(filename).Length / 1024));
        }

        /// <summary>
        /// Gets MD5 hash for file.
        /// </summary>
        /// <param name="filename">Filename of file.</param>
        /// <returns>MD5 hash for file.</returns>
        private static string GetUniqueID(string filename)
        {
            MD5 md5 = MD5.Create();
            md5.Initialize();
            md5.ComputeHash(new FileStream(filename, FileMode.Open, FileAccess.Read));
            StringBuilder sb = new StringBuilder();

            foreach (byte b in md5.Hash)
            {
                sb.Append(b.ToString());
            }

            md5.Dispose();
            return sb.ToString();
        }

        /// <summary>
        /// Checks if filename is in an excluded directory.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <param name="excludedDirectories">List of excluded directories.</param>
        /// <returns>True if filename is in excluded directory, otherwise false.</returns>
        private bool IsDirectoryExcluded(string filename, IEnumerable<string> excludedDirectories)
        {
            foreach (string directory in excludedDirectories)
            {
                if (filename.StartsWith(directory))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get yes / no input from user.
        /// </summary>
        /// <returns>True if yes, false if no.</returns>
        private static bool GetConsoleYNResponse()
        {
            ConsoleKey response;
            do
            {
                while (Console.KeyAvailable)
                    Console.ReadKey();

                Console.WriteLine("[Y / N]");
                response = Console.ReadKey().Key;
                Console.WriteLine();
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            Console.WriteLine();
            return response == ConsoleKey.Y;
        }
    }
}