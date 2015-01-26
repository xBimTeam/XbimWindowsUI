using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Xbim.BCF
{
    public class BCFFile 
    {
        public ObservableCollection<BCFInstance> Instances = new ObservableCollection<BCFInstance>();

        private const string MarkupFileName = "markup.bcf";
        private const string ViewpointFileName = "viewpoint.bcfv";
        private const string SnapshotFileName = "snapshot.png";


        public void LoadFile(string FileName)
        {
            // BCFFile retFile = new BCFFile();
            using (ZipFile z = ZipFile.Read(FileName))
            {
                Regex r = new Regex(@"(?<guid>.*?)/(?<fname>.*)");
                foreach (var zipentry in z)
                {
                    string tFName = System.IO.Path.GetTempFileName();
                    var res = r.Match(zipentry.FileName);
                    if (res.Success)
                    {
                        using (BinaryWriter writer = new BinaryWriter(File.Open(tFName, FileMode.Create)))
                        {
                            zipentry.Extract(writer.BaseStream);
                        }

                        string guid = res.Groups["guid"].Value;
                        string fname = res.Groups["fname"].Value;
                        if (!Instances.Where(x=>x.Markup.Topic.Guid == guid).Any())
                            Instances.Add(new BCFInstance(guid));

                        BCFInstance inst = Instances.Where(x => x.Markup.Topic.Guid == guid).FirstOrDefault();
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
            using (ZipFile zip = new ZipFile())
            {
                foreach (var Instance in Instances)
                {
                    string dir = GetTemporaryDirectory(Instance.guid);
                    Instance.Markup.SaveToFile(Path.Combine(dir, MarkupFileName));
                    Instance.SnapShotSaveToFile(Path.Combine(dir, SnapshotFileName));
                    Instance.VisualizationInfo.SaveToFile(Path.Combine(dir, ViewpointFileName));
                    zip.AddDirectory(dir, Instance.guid);
                }
                zip.Save(filename);
                foreach (var Instance in Instances)
                {
                    Directory.Delete(GetTemporaryDirectory(Instance.guid), true);
                }
            }
        }
    }
}
