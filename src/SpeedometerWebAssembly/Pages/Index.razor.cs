using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpeedometerWebAssembly;
using SpeedometerWebAssembly.Shared;
using System.Text.Json.Serialization;
using BlazorComponentUtilities;

namespace SpeedometerWebAssembly.Pages
{
    public partial class Index
    {

        [Inject]
        public HttpClient? HttpClient { get; set; }
        
        private Random random = new();

        private string _currentSpeedCssClasses => new CssBuilder()
                                                        .AddClass("fast-speed", _currentSpeed > 69)
                                                        .AddClass("normal-speed", _currentSpeed <= 69)
                                                        .Build();


        private async Task StartSpeedAsync()
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 100;
            timer.Elapsed += async (s, e) =>
            {
                _currentTime.Add(TimeSpan.FromMilliseconds(100));
                if (_index >= _records?.Length)
                {
                    timer.Stop();
                    return;
                }

                _currentSpeed = Convert.ToInt32(_records[_index].SpeedInKmph);
                StateHasChanged();
                _index += 1;
            };
            timer.Start();
            timer.AutoReset = true;
        }

        protected async override Task OnInitializedAsync()
        {
            await FetchAsync();
            StateHasChanged();
            await Task.Delay(5000);
            await StartSpeedAsync();
        }

        private GpsRecord[]? _records = null;
        private bool _isBusy = true;
        private TimeSpan _currentTime = new TimeSpan(0, 0, 0, 0, 0);
        private int _currentSpeed = 0;
        private int _index = 0;
        private async Task FetchAsync()
        {
            _isBusy = true;
            var model = await HttpClient!.GetFromJsonAsync<GpsModel>("Session_GPS.json");
            _records = model?.Values;
            _isBusy = false;

        }


    }

    public class GpsModel
    {
        [JsonPropertyName("values")]
        public GpsRecord[]? Values { get; set; }
    }

    public class GpsRecord
    {
        [JsonPropertyName("value")]
        public double[]? Values { get; set; }


        public double? SpeedInKmph => Values?[3] / 1000 * 3600;

        [JsonPropertyName("date")]
        public DateTime? Timestamp { get; set; }
    }
}