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
using System.Reflection.Emit;
using BlazorComponentUtilities;

namespace SpeedometerWebAssembly.Shared
{
    public partial class Speedometer
    {
        [Parameter]
        public int Speed { get; set; }

        private int _itemsCount = 40;

        private Color _slowSpeedColor = new(0, 194, 129);
        private Color _medSpeedColor = new(36, 199, 0);
        private Color _highSpeedColor = new(199, 40, 0);

        private string GetItemStyle(int index, out bool isLighting)
        {
            // Define the height 
            var height = 40;
            if (index <= 9)
                height = index + 30;
            if (index > 30)
                height = 40 + (index - 30);

            var styleBuilder = new StyleBuilder()
                                .AddStyle("height", $"{height}px");

            // Define the color based on the speed 
            if (Speed / 2 > index)
            {
                var speedLevel = GetSpeedLevel();
                if (speedLevel == SpeedLevel.Low)
                    styleBuilder.AddStyle("background-color", $"rgb({_slowSpeedColor.R}, {_slowSpeedColor.G}, {_slowSpeedColor.B}, 0.7);box-shadow: 2px 2px 7px 1px rgba(0, 194, 129)");
                else if (speedLevel == SpeedLevel.Medium)
                    styleBuilder.AddStyle("background-color", $"rgb({_medSpeedColor.R}, {_medSpeedColor.G}, {_medSpeedColor.B}, 0.7);box-shadow: 2px 2px 7px 1px rgba(36, 199, 0)");
                else if (speedLevel == SpeedLevel.High)
                    styleBuilder.AddStyle("background-color", $"rgb({_highSpeedColor.R}, {_highSpeedColor.G}, {_highSpeedColor.B}, 0.7);box-shadow: 2px 2px 7px 1px rgba(199, 40, 0)");

                if (index > 30)
                    isLighting = true;
                else
                    isLighting = false;
            }
            else
                isLighting = false;

            var style = styleBuilder.Build();
            return style;
        }

        private SpeedLevel GetSpeedLevel()
        {
            return Speed switch
            {
                < 25 => SpeedLevel.Low,
                < 65 => SpeedLevel.Medium,
                _ => SpeedLevel.High,

            };
        }



    }

    struct Color
    {
        public Color(int r, int g, int b)
        {
            ValidateAndSet(ref R, r);
            ValidateAndSet(ref G, g);
            ValidateAndSet(ref B, b);
        }

        private void ValidateAndSet(ref int value, int newValue)
        {
            if (newValue >= 255)
                value = 255;
            else
                value = newValue;
        }

        public int R = 0;
        public int G = 0;
        public int B = 0;
    }

    enum SpeedLevel
    {
        Low,
        Medium,
        High,
    }
}