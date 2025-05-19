using System.IO;

namespace MeasureCommons.Data.Experiments
{
    public class ExperimentPath
    {
        public string Wafer { get; set; }
        public string Chip { get; set; }

        private string _FolderPath;
        public string FolderPath {
            get
            {
                return _FolderPath;
            }
            set {
                _FolderPath = value;
                var dir =Path.GetDirectoryName(_FolderPath);
                Chip = Path.GetFileName(_FolderPath);
                Wafer = Path.GetFileName( dir);

            } }

        public override string ToString()
        {
            return Wafer + ":" + Chip;
        }
    }
}
