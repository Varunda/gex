using gex.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.ExtensionMethods {

    public static class HttpClientExtensionMethod {

        /// <summary>
        ///     download a json blob from a url
        /// </summary>
        /// <param name="http"></param>
        /// <param name="url"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async static Task<Result<JsonElement, string>> GetJsonAsync(this HttpClient http, string url, CancellationToken cancel) {
            HttpResponseMessage res = await http.GetAsync(url, cancel);
            if (res.IsSuccessStatusCode == false) {
                return $"got status code {res.StatusCode}";
            }

            byte[] body = await res.Content.ReadAsByteArrayAsync(cancel);
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);

            return json;
        }

    }
}
