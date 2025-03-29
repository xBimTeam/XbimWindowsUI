using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Xbim.BCF
{
    public class BcfFile 
    {
        public ObservableCollection<BcfInstance> Instances = new ObservableCollection<BcfInstance>();
        private const string MarkupFileName = "markup.bcf";
        private const string ViewpointFileName = "viewpoint.bcfv";
        private const string SnapshotFileName = "snapshot.png";

        public void LoadFile(string fileName)
        {
            // BCFFile retFile = new BCFFile();
            using (var z = ZipFile.OpenRead(fileName))
            {
                Regex r = new Regex(@"(?<guid>.*?)/(?<fname>.*)");
                foreach (var zipentry in z.Entries)
                {
                    string tFName = System.IO.Path.GetTempFileName();
                    var res = r.Match(zipentry.FullName);
                    if (res.Success)
                    {
						zipentry.ExtractToFile(tFName);

                        string guid = res.Groups["guid"].Value;
                        string fname = res.Groups["fname"].Value;
                        if (!Instances.Where(x=>x.Markup.Topic.Guid == guid).Any())
                            Instances.Add(new BcfInstance(guid));

                        BcfInstance inst = Instances.Where(x => x.Markup.Topic.Guid == guid).FirstOrDefault();
                        switch (fname.ToLowerInvariant())
                        {
                            case MarkupFileName:
                                inst.Markup = Markup.LoadFromFile(tFName);
                                break;
                            case ViewpointFileName:
                                inst.VisualizationInfo = VisualizationInfo.LoadFromFile(tFName);
                                break;
                            case SnapshotFileName:
                                var bi = new System.Windows.Media.Imaging.BitmapImage();
                                bi.BeginInit();
                                bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bi.UriSource = new Uri(tFName);
                                bi.EndInit();
                                inst.SnapShot = bi;
                                break;
                            default:
                                break;
                        }
                        File.Delete(tFName);
                    }
                }    
            }
        }

        private string GetTemporaryDirectory(string guid)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), guid);
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        internal void SaveFile(string filename)
        {
			using (FileStream streamToOpen = new FileStream(filename, FileMode.CreateNew))
			{
				using ZipArchive archive = new ZipArchive(streamToOpen, ZipArchiveMode.Update);

				foreach (var instance in Instances)
				{
					string dir = GetTemporaryDirectory(instance.Guid);
					instance.Markup.SaveToFile(Path.Combine(dir, MarkupFileName));
					instance.SnapShotSaveToFile(Path.Combine(dir, SnapshotFileName));
					instance.VisualizationInfo.SaveToFile(Path.Combine(dir, ViewpointFileName));
					AddDirectory(archive, dir, instance.Guid);
				}
				
				foreach (var instance in Instances)
				{
					Directory.Delete(GetTemporaryDirectory(instance.Guid), true);
				}
			}
        }

		private void AddDirectory(ZipArchive archive, string sourceDirectory, string entryName)
		{
			var directoryInfo = new DirectoryInfo(sourceDirectory);

			// Add all files in the directory
			foreach (var file in directoryInfo.GetFiles())
			{
				string entryPath = Path.Combine(entryName, file.Name);
				archive.CreateEntryFromFile(file.FullName, entryPath);
			}

			// Recursively add subdirectories
			foreach (var subDirectory in directoryInfo.GetDirectories())
			{
				string subDirectoryEntryName = Path.Combine(entryName, subDirectory.Name);
				AddDirectory(archive, subDirectory.FullName, subDirectoryEntryName);
			}
		}
	}
}
