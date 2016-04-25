using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace Task
{
    public static class Tasks
    {
        /// <summary>
        /// Returns the content of required uri's.
        /// Method has to use the synchronous way and can be used to compare the
        ///  performace of sync/async approaches. 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContent(this IEnumerable<Uri> uris)
        {
            using (var client = new HttpClient())
            {
                foreach (var uri in uris)
                    yield return client.GetStringAsync(uri).Result;
            }
        }

        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the asynchronous way and can be used to compare the performace 
        /// of sync \ async approaches. 
        /// maxConcurrentStreams parameter should control the maximum of concurrent streams 
        /// that are running at the same time (throttling). 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <param name="maxConcurrentStreams">Max count of concurrent request streams</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContentAsync(this IEnumerable<Uri> uris, int maxConcurrentStreams)
        {
            List<Uri> list = uris.ToList();
            string[] results = new string[list.Count];

            using (var client = new HttpClient())
            {
                ParallelOptions options = new ParallelOptions();
                options.MaxDegreeOfParallelism = maxConcurrentStreams;
                Parallel.For(0, list.Count, options,
                    (i) =>
                    {
                        results[i] = client.GetStringAsync(list[i]).Result;
                    }
                );
                return results;
            }
        }

        /// <summary>
        /// Calculates MD5 hash of required resource.
        /// 
        /// Method has to run asynchronous. 
        /// Resource can be any of type: http page, ftp file or local file.
        /// </summary>
        /// <param name="resource">Uri of resource</param>
        /// <returns>MD5 hash</returns>
        public static async Task<string> GetMD5Async(this Uri resource)
        {
            Stream data = null;
            switch (resource.Scheme)
            {
                case "http":
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
                        data = await client.GetStreamAsync(resource);
                    }
                    break;
                case "ftp":
                    var request = (FtpWebRequest)WebRequest.Create(resource);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    var response = (FtpWebResponse)await request.GetResponseAsync();
                    data = response.GetResponseStream();
                    break;
                case "file":
                    data = new FileStream(resource.LocalPath, FileMode.Open);
                    break;
                default:
                    throw new NotImplementedException();
            }

            MD5 md5Hash = MD5.Create();
            byte[] hashed = md5Hash.ComputeHash(data);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < hashed.Length; i++)
                sBuilder.Append(hashed[i].ToString("x2"));
            data.Close();
            return sBuilder.ToString();
        }
    }
}
