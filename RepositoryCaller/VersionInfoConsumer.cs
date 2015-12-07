using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryCaller
{
    public class VersionInfoConsumer
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
        public int Revision { get; set; }
        public string FriendlyNameShort { get; set; }
        public string FriendlyNameLong { get; set; }
        public DateTime ReleaseDate { get; set; }
        public IEnumerable<string> ReferenceLinks { get; set; }
        public bool IsSupported { get; set; }
    }
}
