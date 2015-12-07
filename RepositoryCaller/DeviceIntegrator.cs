using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RepositoryCaller
{
    public class DeviceIntegrator
    {
        private class VersionComparer : IEqualityComparer<VersionInfoConsumer>
        {
            public bool Equals(VersionInfoConsumer x, VersionInfoConsumer y)
            {
                return
                    x.Major == y.Major &&
                    x.Minor == y.Minor &&
                    x.Build == y.Build &&
                    x.Revision == y.Revision;
            }

            public int GetHashCode(VersionInfoConsumer obj)
            {
                if (obj == null)
                    return 0;

                return
                    obj.Major.GetHashCode() ^
                    obj.Minor.GetHashCode() ^
                    obj.Build.GetHashCode() ^
                    obj.Revision.GetHashCode();
            }
        }

        private int _newReleasesPastDaysCount = 7;
        private string _isolatedStorageFileName = "checkedversions.dat";

        public async Task<IEnumerable<VersionInfoConsumer>> GetUncheckedNewReleasesAsync()
        {
            IEnumerable<VersionInfoConsumer> NewReleases = await DataRetriever.GetNewReleasesAsync(_newReleasesPastDaysCount);
            if (NewReleases == null)
                return null;

            IEnumerable<VersionInfoConsumer> CheckedReleases = GetLocallyCheckedVersions();

            if (CheckedReleases == null)
                return NewReleases;

            return NewReleases.Except(CheckedReleases, new VersionComparer());
        }

        private IEnumerable<VersionInfoConsumer> GetLocallyCheckedVersions()
        {
            using (IsolatedStorageFile StorFile = IsolatedStorageFile.GetUserStoreForApplication())
            using (IsolatedStorageFileStream StorFs = new IsolatedStorageFileStream(_isolatedStorageFileName, FileMode.OpenOrCreate, StorFile))
            using (StreamReader sr = new StreamReader(StorFs))
            {
                string JsonData = sr.ReadToEnd();
                return JsonConvert.DeserializeObject<IEnumerable<VersionInfoConsumer>>(JsonData);
            }            
        }
        public bool IsCheckedNewVersion(VersionInfoConsumer versionInfoConsumer)
        {
            IEnumerable<VersionInfoConsumer> LocallyCheckVersions = GetLocallyCheckedVersions();
            //if (LocallyCheckVersions == null &&
            //    versionInfoConsumer.ReleaseDate >= DateTime.Now.AddDays(-1 * _newReleasesPastDaysCount))
            //    return false;
            if (LocallyCheckVersions == null)
            {
                if (versionInfoConsumer.ReleaseDate >= DateTime.Now.AddDays(-1 * _newReleasesPastDaysCount))
                    return false;
                else
                    return true;
            }
            else
            { 
                return 
                    LocallyCheckVersions.Contains(versionInfoConsumer, new VersionComparer()) ||
                    versionInfoConsumer.ReleaseDate < DateTime.Now.AddDays(-1 * _newReleasesPastDaysCount);
            }
        }
        public void AddLocallyCheckedVersion(VersionInfoConsumer checkedVersionInfo)
        {
            IEnumerable<VersionInfoConsumer> LocallyCheckedVersions = GetLocallyCheckedVersions();
            List<VersionInfoConsumer> LocallyCheckedVersionsList;

            LocallyCheckedVersionsList =
                LocallyCheckedVersions == null ?
                new List<VersionInfoConsumer>() : LocallyCheckedVersions.ToList();

            if (!LocallyCheckedVersionsList.Contains(checkedVersionInfo, new VersionComparer()))
            {
                LocallyCheckedVersionsList.Add(checkedVersionInfo);

                using (IsolatedStorageFile StorFile = IsolatedStorageFile.GetUserStoreForApplication())
                using (IsolatedStorageFileStream StorFs = new IsolatedStorageFileStream(_isolatedStorageFileName, FileMode.Create, StorFile))
                using (StreamWriter sr = new StreamWriter(StorFs))
                {
                    string JsonData = JsonConvert.SerializeObject(LocallyCheckedVersionsList);
                    sr.Write(JsonData);
                }
            }
        }
    }
}
