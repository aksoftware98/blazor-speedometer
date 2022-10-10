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
        private bool _isFinalLap = false;
        private string? _time = "Formation Lap";
        private int _position = 1;
        private int _lapNumber = 0;
        private readonly Stack<Lap>? _finishedLaps = new();
        private bool _isPreview;
        private LapImprovementStatus _lapStatus;
        private Lap? _fastestLap = null;



        const string PurpleGradient = "border-image: linear-gradient(#C300EA, #76008E) 30;";
        const string GreenGradient = "border-image: linear-gradient(#13CF00, #0D8E00) 30;";
        const string OrangeGradient = "border-image: linear-gradient(#E7BF0A, #FDAD00) 30;";
        override protected async Task OnInitializedAsync()
        {
            await FetchLapsAsync();
        }

        private string _timingClasses => new CssBuilder()
                                            .AddClass("flickering", !_isStarted)
                                            .Build();



        private string _timingStyles => new StyleBuilder()
                                    .AddStyle("color", PurpleGradient, _isPreview && _lapStatus == LapImprovementStatus.FastestLap)
                                    .AddStyle("color", GreenGradient, _isPreview && _lapStatus == LapImprovementStatus.Fast)
                                    .AddStyle("color", OrangeGradient, _isPreview && _lapStatus == LapImprovementStatus.Slow)
                                    .Build();
        private string _borderStyles => new StyleBuilder()
                                            .AddStyle("border-image", PurpleGradient, _isPreview && _lapStatus == LapImprovementStatus.FastestLap)
                                            .AddStyle("border-image", GreenGradient, _isPreview && _lapStatus == LapImprovementStatus.Fast)
                                            .AddStyle("border-image", OrangeGradient, _isPreview && _lapStatus == LapImprovementStatus.Slow)
                                            .Build();
        

        private async Task FetchLapsAsync()
        {
            _laps = await HttpClient.GetFromJsonAsync<List<Lap>>("sessions/session_01_timings.json");
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
        }

        private void Preview(Lap previousLap, Lap currentLap)
        {
            _isPreview = true;
            if (previousLap == null)
            {
                _time = currentLap.TimeAsString;
                _lapStatus = LapImprovementStatus.Fast;
            }
            else
            {
                var difference = currentLap.LapTime - previousLap?.LapTime;
                if (previousLap?.LapTime > currentLap.LapTime)
                {
                    _fastestLap = currentLap;
                    _time = FormatLapTimeFromDifference(difference);
                    _lapStatus = LapImprovementStatus.Fast;
                }
                else /*if (previousLap?.LapTime < currentLap.LapTime)*/
                {
                    _time = FormatLapTimeFromDifference(difference);
                    _lapStatus = LapImprovementStatus.Slow;
                }
            }
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.AutoReset = false;
            timer.Start();
            timer.Elapsed += (s, e) =>
            {
                _isPreview = false;
                StateHasChanged();
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