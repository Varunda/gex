using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace gex.TestUpload {

    public class Uploader {

        private readonly ILogger<Uploader> _Logger;
        private readonly IOptions<Env> _Env;
        private readonly IOptions<Secret> _Secret;

        private static readonly HttpClient _Http = new HttpClient();
        static Uploader() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1");
        }

        public Uploader(ILogger<Uploader> logger,
            IOptions<Env> env, IOptions<Secret> secret) {
            _Logger = logger;

            _Env = env;
            _Secret = secret;
        }

        public async Task UploadThirdParty(byte[] data, string? description, CancellationToken cancel) {
            using MultipartFormDataContent form = new() {
                { new ByteArrayContent(data), "demofile.sdfz", "demofile.sdfz" },
            };

            _Logger.LogDebug($"sending match files [host={_Env.Value.Host}]");

            HttpRequestMessage req = new();
            req.RequestUri = new Uri(_Env.Value.Host + "/api/match-upload/upload-third-party?matchPoolID=1" + (description != null ? $"&description={description}" : ""));
            req.Content = form;
            req.Method = HttpMethod.Post;
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _Secret.Value.JWT);

            HttpResponseMessage response = await _Http.SendAsync(req);
            string body = await response.Content.ReadAsStringAsync(cancel);
            if (response.IsSuccessStatusCode == false) {
                _Logger.LogWarning($"failed to upload game [status={response.StatusCode}] [body={body}]");
            } else {
                _Logger.LogInformation($"successfully uploaded game! [status={response.StatusCode}] [body={body}]");
            }

        }

    }

}
