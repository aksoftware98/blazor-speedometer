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

namespace SpeedometerWebAssembly.Shared
{
    public partial class LapCounter
    {

        [Inject] HttpClient? HttpClient { get; set; }

        private List<Lap>? _laps = null;
        private bool _isStarted = false;
        private bool _isFinished = false;
        private string? _time = "Warm Up";
        private int _position = 1;
        private int _lapNumber = 0;
        private bool _isPreview;
        private LapImprovementStatus _lapStatus;
        private Lap? _fastestLap = null;



        const string PurpleGradient = "#C100E7";
        const string GreenGradient = "#2ED800";
        const string OrangeGradient = "#FDAD00";
        override protected async Task OnInitializedAsync()
        {
            await FetchLapsAsync();
            StateHasChanged();
            await Task.Delay(30000);
            await StartAsync();
        }

        private string _timingClasses => new CssBuilder()
                                            .AddClass("flickering", _isPreview)
                                            .Build();



        private string _timingStyles => new StyleBuilder()
                                    .AddStyle("color", PurpleGradient, _isPreview && _lapStatus == LapImprovementStatus.FastestLap)
                                    .AddStyle("color", GreenGradient, _isPreview && _lapStatus == LapImprovementStatus.Fast)
                                    .AddStyle("color", OrangeGradient, _isPreview && _lapStatus == LapImprovementStatus.Slow)
                                    .AddStyle("margin-left", "10px", !_isPreview)
                                    .Build();

        private string _positionStyles => new StyleBuilder()
                                   .AddStyle("background-color", PurpleGradient, _isPreview && _lapStatus == LapImprovementStatus.FastestLap)
                                   .AddStyle("background-color", GreenGradient, _isPreview && _lapStatus == LapImprovementStatus.Fast)
                                   .AddStyle("background-color", OrangeGradient, _isPreview && _lapStatus == LapImprovementStatus.Slow)
                                   .Build();


        private string _borderStyles => new StyleBuilder()
                                            .AddStyle("border-color", PurpleGradient, _isPreview && _lapStatus == LapImprovementStatus.FastestLap)
                                            .AddStyle("border-color", GreenGradient, _isPreview && _lapStatus == LapImprovementStatus.Fast)
                                            .AddStyle("border-color", OrangeGradient, _isPreview && _lapStatus == LapImprovementStatus.Slow)
                                            .Build();


        private async Task FetchLapsAsync()
        {
            _laps = await HttpClient.GetFromJsonAsync<List<Lap>>("sessions/session_01_timings.json");
            _fastestLap = _laps.MinBy(m => m.LapTime);
        }

        private async Task StartAsync()
        {
            if (_laps == null || _isStarted)
                return;

            _isStarted = true;

            var lapCounter = TimeSpan.Zero;
            Lap currentLap = null;
            var timer = new System.Timers.Timer();
            timer.Interval = 10;
            timer.AutoReset = true;

            timer.Elapsed += (s, e) =>
            {
                lapCounter = lapCounter.Add(TimeSpan.FromMilliseconds(10));
                if (!_isPreview)
                    _time = lapCounter.ToString("mm\\:ss\\.ff");
                StateHasChanged();
            };

            for (int i = 0; i < _laps.Count; i++)
            {
                lapCounter = TimeSpan.Zero;
                _lapNumber = i + 1;
                currentLap = _laps[i];
                _position = currentLap.Position;
                timer.Start();
                await Task.Delay(Convert.ToInt32(currentLap.LapTime.TotalMilliseconds));
                timer.Stop();
                if (i == 0)
                    Preview(null, currentLap);
                else
                    Preview(_laps[i - 1], currentLap);
            }
            await Task.Delay(7500);
            _time = "Finish";
            _isFinished = true;
        }

        private void Preview(Lap previousLap, Lap currentLap)
        {
            _isPreview = true;
            var fullTime = currentLap.LapTime.ToString("mm\\:ss\\.fff");
            var secondTime = string.Empty;
            if (previousLap == null)
            {
                _lapStatus = LapImprovementStatus.Fast;
            }
            else
            {
                var difference = currentLap.LapTime - previousLap?.LapTime;
                if (_fastestLap.LapTime >= currentLap.LapTime)
                {
                    secondTime = FormatLapTimeFromDifference(difference);
                    _lapStatus = LapImprovementStatus.FastestLap;
                }
                else if (previousLap?.LapTime > currentLap.LapTime)
                {
                    secondTime = FormatLapTimeFromDifference(difference);
                    _lapStatus = LapImprovementStatus.Fast;
                }
                else /*if (previousLap?.LapTime < currentLap.LapTime)*/
                {
                    secondTime = FormatLapTimeFromDifference(difference);
                    _lapStatus = LapImprovementStatus.Slow;
                }
            }
            bool firstRender = previousLap != null;
            _time = fullTime;
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 3500;
            timer.AutoReset = true;
            timer.Start();
            timer.Elapsed += (s, e) =>
            {
                if (firstRender)
                {
                    _time = secondTime;
                    StateHasChanged();
                    firstRender = false;
                }
                else
                {
                    _isPreview = false;
                    StateHasChanged();
                    timer.Stop();
                    timer.Dispose();
                }
            };
        }

        private string FormatLapTimeFromDifference(TimeSpan? difference)
        {
            if (difference == null)
                return string.Empty;
            if (Math.Abs(difference.Value.TotalMilliseconds) < 60000)
            {
                return CustomFormatTimeSpan(difference, "ss\\.fff");
            }
            else
            {
                return CustomFormatTimeSpan(difference, "mm\\:ss\\.fff");
            }

        }

        private static string CustomFormatTimeSpan(TimeSpan? difference, string format)
        {
            if (difference < TimeSpan.Zero)
                return $"-{difference.Value.ToString(format)}";
            else
                return $"+{difference.Value.ToString(format)}";
        }
    }

    public enum LapImprovementStatus
    {
        Fast,
        Slow,
        FastestLap,
    }

    class Lap
    {
        [JsonPropertyName("lap")]
        public int LapNumber { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("time")]
        public string? TimeAsString { get; set; }

        public TimeSpan LapTime
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TimeAsString))
                    return TimeSpan.FromMilliseconds(0);

                return TimeSpan.ParseExact(TimeAsString, "mm\\:ss\\.fff", null);
            }
        }

    }
}