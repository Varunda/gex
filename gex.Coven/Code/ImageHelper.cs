using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code {

    public class ImageHelper {

        private static HttpClient _Http = new HttpClient();

        static ImageHelper() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex.Coven/0.1");
            _Http.Timeout = TimeSpan.FromSeconds(30);
        }

        public static async Task<Bitmap?> LoadUrl(Uri uri) {
            try {
                HttpResponseMessage res = await _Http.GetAsync(uri);
                res.EnsureSuccessStatusCode();

                byte[] data = await res.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data));
            } catch (HttpRequestException ex) {
                ILogger<ImageHelper>? logger = App.Current?.Services?.GetService<ILogger<ImageHelper>>();
                logger?.LogError(ex, $"failed to load bitmap [uri={uri}]");
                return null;
            }
        }

    }
}
