using System;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using ColorWheelControl = PaintDotNet.ColorBgra;
using AngleControl = System.Double;
using PanSliderControl = PaintDotNet.Pair<double, double>;
using TextboxControl = System.String;
using DoubleSliderControl = System.Double;
using ListBoxControl = System.Byte;
using RadioButtonControl = System.Byte;
using ReseedButtonControl = System.Byte;
using MultiLineTextboxControl = System.String;
using RollControl = System.Tuple<double, double, double>;

//[assembly: AssemblyTitle("FilmicTonemapping plugin for paint.net")]
[assembly: AssemblyDescription("Filmic Tonemapping Operators selected pixels")]
//[assembly: AssemblyConfiguration("filmic tonemapping operators")]
//[assembly: AssemblyCompany("Jace Regenbrecht")]
//[assembly: AssemblyProduct("FilmicTonemapping")]
[assembly: AssemblyCopyright("Copyright ©2018 by Jace Regenbrecht")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: AssemblyVersion("1.1.*")]

namespace CLUT
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return ((AssemblyCopyrightAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
            }
        }
        public string Copyright
        {
            get
            {
                return ((AssemblyDescriptionAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description;
            }
        }

        public string DisplayName
        {
            get
            {
                return ((AssemblyProductAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("https://github.com/Zackin5/Filmic-Tonemapping-Plugin");
            }
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "CLUT")]
    public class CLUT : PropertyBasedEffect
    {
        public static string StaticName
        {
            get
            {
                return "CLUT";
            }
        }

        public static Image StaticIcon
        {
            get
            {
                return null;
            }
        }

        public static string SubmenuName
        {
            get
            {
                return null;
            }
        }

        public CLUT() : base(StaticName, StaticIcon, SubmenuName, EffectFlags.Configurable)
        {
        }

        public enum PropertyNames
        {
            DirSetting,
            DirFiles
        }

        public enum Amount2Options
        {
            CLUTFilename
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new StringProperty(PropertyNames.DirSetting, "", 255));
            Amount2Options Amount2Default = (Enum.IsDefined(typeof(Amount2Options), 0)) ? (Amount2Options)0 : 0;
            props.Add(StaticListChoiceProperty.CreateForEnum<Amount2Options>(PropertyNames.DirFiles, Amount2Default, false));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.DirSetting, ControlInfoPropertyNames.DisplayName, "Directory");
            configUI.SetPropertyControlValue(PropertyNames.DirFiles, ControlInfoPropertyNames.DisplayName, "File");
            PropertyControlInfo Amount2Control = configUI.FindControlForPropertyName(PropertyNames.DirFiles);
            Amount2Control.SetValueDisplayName(Amount2Options.CLUTFilename, "");

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "CLUT";
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "CLUT v1.0";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            DirSetting = newToken.GetProperty<StringProperty>(PropertyNames.DirSetting).Value;
            Filename = (byte)((int)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.DirFiles).Value);

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface, SrcArgs.Surface, rois[i]);
            }
        }

        #region User Entered Code
        #region UICode
        TextboxControl DirSetting = ""; // [0,255] Directory
        ListBoxControl Filename = 0; // File|
        #endregion

        // Source: http://www.quelsolaar.com/technology/clut.html

        ColorBgra correctPixel(ColorBgra pixelColour, Bitmap clut, int level)
        {
            int cubeSize = level * level;
            byte alpha = pixelColour.A;

            // Get R and G values
            int x = ((pixelColour.R) / (level / 2));
            int y = ((pixelColour.G) / (cubeSize / 2));

            // Correct R value offset
            if (x > 0) x--;

            // Get B value from depth
            x += (pixelColour.B % clut.Width);
            y += (pixelColour.B / clut.Height);

            pixelColour = clut.GetPixel(x, y);
            pixelColour.A = alpha;

            return pixelColour;
        }

        // Main render function
        void Render(Surface dst, Surface src, Rectangle rect)
        {
            ColorBgra currentPixel;
            int level;
            
            // Get CLUT image
            Bitmap clutBitmap = new Bitmap(Image.FromFile("D:\\zachi\\Documents\\My Game DLLs\\Graphic Injectors\\Shaders\\HaldCLUT\\CLUTgallery\\Identity_level_8.HCLUT.png"));

            // Calculate image's cubic root
            for (level = 1; level * level * level < clutBitmap.Height; level++) ;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    currentPixel = src[x, y];

                    currentPixel = correctPixel(currentPixel, clutBitmap, level);

                    // Render to image pixels
                    dst[x, y] = currentPixel;
                }
            }
        }

        #endregion
    }
}
