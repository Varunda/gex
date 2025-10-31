using gex.Familiar.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace gex.Familiar.Services {

    public class MatchUploader {

        private readonly ILogger<MatchUploader> _Logger;
        private readonly IOptions<FamiliarInstanceOptions> _Options;
        private readonly IOptions<JwtFamiliarOptions> _JwtOptions;

        private static readonly HttpClient _Http = new HttpClient();

        static MatchUploader() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex familiar/0.1 (discord: varunda)");
        }

        public MatchUploader(ILogger<MatchUploader> logger,
            IOptions<FamiliarInstanceOptions> options, IOptions<JwtFamiliarOptions> jwtOptions) {

            _Logger = logger;
            _Options = options;
            _JwtOptions = jwtOptions;
        }

        public async Task Upload(byte[] demofile, byte[] actions, byte[] stdout, byte[] stderr) {

            using MultipartFormDataContent form = new() {
                { new ByteArrayContent(demofile), "demofile.sdfz", "demofile.sdfz" },
                { new ByteArrayContent(actions), "actions.json", "actions.json" },
                { new ByteArrayContent(stdout), "stdout.txt", "stdout.txt" },
                { new ByteArrayContent(stderr), "stderr.txt", "stderr.txt" }
            };

            _Logger.LogDebug($"sending match files [host={_Options.Value.Host}]");

            HttpRequestMessage req = new();
            req.RequestUri = new Uri(_Options.Value.Host + "/api/match-upload/upload-familiar");
            req.Content = form;
            req.Method = HttpMethod.Post;
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _JwtOptions.Value.Token);

            HttpResponseMessage response = await _Http.SendAsync(req);
            string body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode == false) {
                _Logger.LogWarning($"failed to upload game [status={response.StatusCode}] [body={body}]");
            } else {
                _Logger.LogInformation($"successfully uploaded game! [status={response.StatusCode}] [body={body}]");
            }
        }

    }
}
