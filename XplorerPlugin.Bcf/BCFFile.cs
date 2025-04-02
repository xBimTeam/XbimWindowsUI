using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using XplorerPlugin.Bcf;

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
				// TODO: improve Guid Regex?
				Regex r = new Regex(@"(?<guid>.*?)\\(?<fname>.*)");

				var entries = z.Entries;
				foreach (var zipentry in entries)
				{
					string tFName = System.IO.Path.GetTempFileName();
					var res = r.Match(zipentry.FullName);
					if (res.Success)
					{
						zipentry.ExtractToFile(tFName, overwrite: true);

						string guid = res.Groups["guid"].Value;
						string fname = res.Groups["fname"].Value;
						if (!Instances.Where(x => x.Markup.Topic.Guid == guid).Any())
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

			using (var zip = ZipFile.Open(filename, ZipArchiveMode.Update))
			{
				foreach (var instance in Instances)
				{
					string dir = GetTemporaryDirectory(instance.Guid);
					instance.Markup.SaveToFile(Path.Combine(dir, MarkupFileName));
					instance.SnapShotSaveToFile(Path.Combine(dir, SnapshotFileName));
					instance.VisualizationInfo.SaveToFile(Path.Combine(dir, ViewpointFileName));
					zip.CreateEntryFromAny(dir);
				}

				foreach (var instance in Instances)
				{
					Directory.Delete(GetTemporaryDirectory(instance.Guid), true);
				}
			}
		}
	}
}
