using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Coven.Code;
using gex.Coven.Services.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace gex.Coven.Controls {

    public partial class MapNameImage : TemplatedControl {

        #region properties

        public static readonly StyledProperty<string> MapProperty = AvaloniaProperty.Register<MapNameImage, string>(
            name: nameof(Map),
            defaultValue: ""
        );

        public string Map {
            get => GetValue(MapProperty);
            set => SetValue(MapProperty, value);
        }

        public static readonly StyledProperty<string> SizeProperty = AvaloniaProperty.Register<MapNameImage, string>(
            name: nameof(Size),
            defaultValue: "texture-thumb"
        );

        public string Size {
            get => GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly StyledProperty<Task<Bitmap?>> ImageSourceProperty = AvaloniaProperty.Register<MapNameImage, Task<Bitmap?>>(
            name: nameof(ImageSource),
            defaultValue: default!
        );

        public Task<Bitmap?> ImageSource {
            get => GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        #endregion

        private readonly ILogger<MapNameImage>? _Logger;

        public MapNameImage() {
            _Logger = App.Current?.Services?.GetService<ILogger<MapNameImage>>();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);

            if (change.Property == MapProperty || change.Property == SizeProperty) {
                _UpdateImageSource();
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
            base.OnApplyTemplate(e);

            _UpdateImageSource();
        }

        private void _UpdateImageSource() {
            //_Logger?.LogInformation($"drawing map image [map={Map}] [size={Size}]");
            if (string.IsNullOrWhiteSpace(Map) == true) {
                return;
            }

            string dir = $"{ShellUtil.GetWorkingDirectory()}/cache/image/MapNameBackground/{Size}/";
            string path = Path.Join(dir, $"{Map}.jpg");
            try {
                Directory.CreateDirectory(dir);

                if (File.Exists(path)) {
                    ImageSource = Task.Run(async () => {
                        try {
                            byte[] bytes = await File.ReadAllBytesAsync(path);
                            return new Bitmap(new MemoryStream(bytes));
                        } catch (Exception ex) {
                            _Logger?.LogError(ex, $"failed to load image from cache [path={path}]");
                            return null;
                        }
                    });

                    return;
                }
            } catch (Exception ex) {
                _Logger?.LogError(ex, $"failed to check cache [dir={dir}] [path={path}]");
            }

            string url = $"https://gex.honu.pw/image-proxy/MapNameBackground?map={HttpUtility.UrlEncode(Map)}&size={Size}";
            ImageSource = ImageHelper.LoadUrl(new Uri(url));

            string map = Map;
            string size = Size;
            ImageSource.ContinueWith((Task<Bitmap?> task) => {
                Bitmap? bitmap = task.Result;

                try {
                    bitmap?.Save(path, new JpegBitmapEncoderOptions() {
                        Quality = 100
                    });
                    _Logger?.LogDebug($"cached image being loaded [map={map}] [size={size}] [path={path}]");
                } catch (Exception ex) {
                    _Logger?.LogError(ex, $"failed to save loaded bitmap [path={path}]");
                }
            });
        }

    }
}