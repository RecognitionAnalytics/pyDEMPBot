namespace SimpleControls.MRU.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    public class MRUEntry
    {
        #region constructor
        /// <summary>
        /// Standard Constructor
        /// </summary>
        public MRUEntry()
        {
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        public MRUEntry(MRUEntry copyFrom)
        {
            if (copyFrom == null) return;
            this.FileName = copyFrom.FileName;
            this.PathFileName = copyFrom.PathFileName;
            this.IsPinned = copyFrom.IsPinned;
        }

        /// <summary>
        /// Convinience constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fullTime"></param>
        public MRUEntry(string name, bool fullTime)
        {
            this.PathFileName = name;
            this.FileName = System.IO.Path.GetFileNameWithoutExtension(name);
            this.IsPinned = fullTime;
        }
        #endregion constructor

        #region properties
        public string FileName { get; set; }

        private string _PathFileName = null;
        public string PathFileName {
            get
            {
                return _PathFileName;
            }
            set
            {
                _PathFileName = value;
                this.FileName = System.IO.Path.GetFileNameWithoutExtension(value);
            }
        }

        public bool IsPinned { get; set; }
        #endregion properties

        #region methods
        public override string ToString()
        {
            return string.Format("Path {0}, IsPinned:{1}", (this.PathFileName == null ? "(null)" : this.PathFileName),
                                                           this.IsPinned);
        }
        #endregion methods
    }
}
