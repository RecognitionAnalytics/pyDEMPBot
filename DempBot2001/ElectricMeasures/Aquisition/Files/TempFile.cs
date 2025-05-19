using System;
using System.Collections.Generic;
using System.IO;

namespace DataControllers.Aquisition.Files
{
    public class TempFile
    {
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public string TempFileName { get; set; }
        public string Filename { get; set; }

        public void AddProp(string name, object value)
        {
            if (value == null)
                return;
            Properties.Add(name, value.ToString());
        }

        public string TimeStamp { get; private set; }

        public TempFile(string filename)
        {
            TimeStamp = DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
            TempFileName = filename;
        }

        public void Save(TempFile file)
        {
            try
            {
                var path = Path.GetDirectoryName(file.Filename);
                var superPath = Path.GetDirectoryName(path);
                if (System.IO.Directory.Exists(superPath) == false)
                    Directory.CreateDirectory(superPath);
                if (System.IO.Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);

                File.Move(file.TempFileName, file.Filename + ".tdms");
                if (File.Exists(file.TempFileName + "_index"))
                    File.Move(file.TempFileName + "_index", file.Filename + ".tdms_index");
                string propsFile = "root:\n";
                foreach (KeyValuePair<string, string> kvp in file.Properties)
                {
                    propsFile += "    " + kvp.Key + ": '" + kvp.Value.Replace("\"", "'").Replace("\n", "//n").Replace("\r", "").Replace("\\", "/") + "'\n";
                }

                File.WriteAllText(file.Filename + "_props.yaml", propsFile);
            }
            catch { }

        }
    }
}
