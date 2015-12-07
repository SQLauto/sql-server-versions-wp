using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServerVersionsWP.ViewModels
{
    public class RecentVersionInfoViewModel
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
        public int Revision { get; set; }
        public string FriendlyNameShort { get; set; }
        public string FriendlyNameLong { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ReleaseDateFormatted {
            get
            {
                return ReleaseDate.ToString("M/d/yyyy");
            }
            private set { }
        }
        public bool IsSupported { get; set; }
        public bool IsChecked { get; set; }
        public string TitleFontWeight 
        {
            get
            {
                if (IsChecked)
                    return "Normal";
                else
                    return "ExtraBold";
            }
        }
    }
}
