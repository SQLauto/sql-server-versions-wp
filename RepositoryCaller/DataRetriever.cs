using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace RepositoryCaller
{
    public static class DataRetriever
    {
        private static int _topNewReleasesCount = 10;

        private enum HttpVerb
        {
            GET,
            PUT,
            POST,
            DELETE
        }

        private static string _webApiUri = "http://sqlserverversions.azurewebsites.net/api";

        public static async Task<VersionInfoConsumer> GetVersionInfoAsync(int major, int minor, int build, int revision)
        {
            string CompleteRequestUri = string.Format(
                "{0}/version/{1}/{2}/{3}/{4}",
                _webApiUri,
                major,
                minor,
                build,
                revision);
            string ResponseString = "";

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(CompleteRequestUri);
            Request.Method = HttpVerb.GET.ToString();
            //Request.ContentType = "application/json";

            //HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
            //    Request.BeginGetResponse(null, null),
            //    ar => (HttpWebResponse)Request.EndGetResponse(ar));
            //WebResponse Response = await Task<WebResponse>.Factory.FromAsync(Request.BeginGetResponse, Request.EndGetResponse, Request);
            HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
                Request.BeginGetResponse, 
                ar => (HttpWebResponse)Request.EndGetResponse(ar), 
                Request);


            if (Response.StatusCode != HttpStatusCode.OK)
                return null;
            
            using (Stream ResponseStream = Response.GetResponseStream())
                if (ResponseStream != null)
                    using (StreamReader ResponseStreamReader = new StreamReader(ResponseStream))
                        ResponseString = ResponseStreamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(ResponseString))
                return null;

            return JsonConvert.DeserializeObject<VersionInfoConsumer>(ResponseString);
        }

        public static async Task<IEnumerable<VersionInfoConsumer>> GetRecentReleaseVersionInfoAsync(int topCount)
        {
            string CompleteRequestUri = string.Format(
                "{0}/recent/{1}",
                _webApiUri,
                topCount);
            string ResponseString = "";

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(CompleteRequestUri);
            Request.Method = HttpVerb.GET.ToString();
            //Request.ContentType = "application/json";

            //HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
            //    Request.BeginGetResponse(null, null),
            //    ar => (HttpWebResponse)Request.EndGetResponse(ar));
            //WebResponse Response = await Task<WebResponse>.Factory.FromAsync(Request.BeginGetResponse, Request.EndGetResponse, Request);
            HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
                Request.BeginGetResponse,
                ar => (HttpWebResponse)Request.EndGetResponse(ar),
                Request);


            if (Response.StatusCode != HttpStatusCode.OK)
                return null;

            using (Stream ResponseStream = Response.GetResponseStream())
                if (ResponseStream != null)
                    using (StreamReader ResponseStreamReader = new StreamReader(ResponseStream))
                        ResponseString = ResponseStreamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(ResponseString))
                return null;

            return JsonConvert.DeserializeObject<IEnumerable<VersionInfoConsumer>>(ResponseString);
        }

        public static async Task<IEnumerable<VersionInfoConsumer>> GetNewReleasesAsync(int pastDaysCount)
        {
            //IEnumerable<VersionInfoConsumer> output = await DataRetriever.GetRecentReleaseVersionInfo(_topNewReleasesCount);
            //return output.Where<VersionInfoConsumer>(m => m.ReleaseDate >= DateTime.Now.AddDays(-1 * _newReleasesPastDaysCount));
            return (await DataRetriever.GetRecentReleaseVersionInfoAsync(_topNewReleasesCount))
                .Where<VersionInfoConsumer>(m => m.ReleaseDate >= DateTime.Now.AddDays(-1 * pastDaysCount));
        }

        public static async Task<IEnumerable<VersionInfoConsumer>> GetMajorReleases()
        {
            string CompleteRequestUri = string.Format(
                "{0}/allmajorreleases",
                _webApiUri);
            string ResponseString = "";

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(CompleteRequestUri);
            Request.Method = HttpVerb.GET.ToString();
            //Request.ContentType = "application/json";

            //HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
            //    Request.BeginGetResponse(null, null),
            //    ar => (HttpWebResponse)Request.EndGetResponse(ar));
            //WebResponse Response = await Task<WebResponse>.Factory.FromAsync(Request.BeginGetResponse, Request.EndGetResponse, Request);
            HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
                Request.BeginGetResponse,
                ar => (HttpWebResponse)Request.EndGetResponse(ar),
                Request);


            if (Response.StatusCode != HttpStatusCode.OK)
                return null;

            using (Stream ResponseStream = Response.GetResponseStream())
                if (ResponseStream != null)
                    using (StreamReader ResponseStreamReader = new StreamReader(ResponseStream))
                        ResponseString = ResponseStreamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(ResponseString))
                return null;

            return JsonConvert.DeserializeObject<IEnumerable<VersionInfoConsumer>>(ResponseString);
        }

        public static async Task<IEnumerable<VersionInfoConsumer>> GetRecentAndOldestSupported()
        {
            string CompleteRequestUri = string.Format(
                "{0}/supportboundaries",
                _webApiUri);
            string ResponseString = "";

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(CompleteRequestUri);
            Request.Method = HttpVerb.GET.ToString();
            //Request.ContentType = "application/json";

            //HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
            //    Request.BeginGetResponse(null, null),
            //    ar => (HttpWebResponse)Request.EndGetResponse(ar));
            //WebResponse Response = await Task<WebResponse>.Factory.FromAsync(Request.BeginGetResponse, Request.EndGetResponse, Request);
            HttpWebResponse Response = await Task.Factory.FromAsync<HttpWebResponse>(
                Request.BeginGetResponse,
                ar => (HttpWebResponse)Request.EndGetResponse(ar),
                Request);


            if (Response.StatusCode != HttpStatusCode.OK)
                return null;

            using (Stream ResponseStream = Response.GetResponseStream())
                if (ResponseStream != null)
                    using (StreamReader ResponseStreamReader = new StreamReader(ResponseStream))
                        ResponseString = ResponseStreamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(ResponseString))
                return null;

            return JsonConvert.DeserializeObject<IEnumerable<VersionInfoConsumer>>(ResponseString);
        }
    }
}
