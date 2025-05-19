using System.IO;

namespace MeasureCommons.Data.Experiments
{
    public class Experiment : IExperiment
    {
        public string _Wafer = "";
        public string Wafer {
            get {  return _Wafer; }
            set
            {
                _Wafer = value;
            }
        }

        public string ActiveChannels { get; set; }
        public string CleanedActiveChannels
        {
            get
            {
                return ActiveChannels.Trim();
            }
        }
        public string Chip { get; set; }
        public string Tags { get; set; }

        public string DataFolder { get; set; }
        public string ProgramName { get; set; }
        public string ResistorChannel { get; set; }

        public string Machine { get; set; }

        public string Notes { get; set; }

        //  public FluidicProgram Program { get; set; }
        public void Save()
        {
            string wafer = Wafer == null ? "" : Wafer.Replace("'", "\"").Replace("\n", "\\n");
            string chip = Chip == null ? "" : Chip.Replace("'", "\"").Replace("\n", "\\n");
            string tags = Tags == null ? "" : Tags.Replace("'", "\"").Replace("\n", "\\n");

            string dataFolder = DataFolder == null ? "" : DataFolder.Replace("'", "\"").Replace("\n", "\\n");
            string programName = ProgramName == null ? "" : ProgramName.Replace("'", "\"").Replace("\n", "\\n");

            string machine = Machine == null ? "" : Machine.Replace("'", "\"").Replace("\n", "\\n");
            string notes = Notes == null ? "" : Notes.Replace("'", "\"").Replace("\n", "\\n");

            string propsFile = $@"root:
  Wafer: '{wafer}'
  Chip: '{chip}'
  Tags: '{tags}'
  ResistorChannel: '{ResistorChannel}'
  ProgramName: '{programName}'
  Machine: '{machine}'
  Notes: '{notes}'
";

            var dirName = DataFolder + "\\" + Wafer + "\\" + Chip + "\\";


            if (Directory.Exists(DataFolder + "\\" + Wafer) == false)
                Directory.CreateDirectory(DataFolder + "\\" + Wafer);
            if (Directory.Exists(dirName) == false)
                Directory.CreateDirectory(dirName);
            System.IO.File.WriteAllText(dirName + "Experiment_props.yaml", propsFile);

        }

        public override string ToString()
        {
            return $"{_Wafer}:{Chip}".ToString();
        }
    }

    public interface IExperiment
    {
        string Wafer { get; set; }
        string Chip { get; set; }
        string Tags { get; set; }
        string DataFolder { get; set; }
        string ProgramName { get; set; }



        //  public FluidicProgram Program { get; set; }
    }

 
}
