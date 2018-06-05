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
            DirFiles,
            checkk
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
            props.Add(new BooleanProperty(PropertyNames.checkk, false));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.DirSetting, ControlInfoPropertyNames.DisplayName, "Directory");
            configUI.SetPropertyControlValue(PropertyNames.DirFiles, ControlInfoPropertyNames.DisplayName, "File");
            configUI.SetPropertyControlValue(PropertyNames.checkk, ControlInfoPropertyNames.DisplayName, "Checkk");
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
            Checkk = newToken.GetProperty<BooleanProperty>(PropertyNames.checkk).Value;

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
        CheckboxControl Checkk = false; // [0,1] Display Clipping
        #endregion

        // Source: http://www.quelsolaar.com/technology/clut.html

        class ClutData
        {
            public float[] data;
            public int x;
            public int y;

            public ClutData(string filepath)
            {
                Bitmap clutBitmap = new Bitmap(Image.FromFile(filepath));
                
                if(clutBitmap.Width != clutBitmap.Height)
                {
                    data = null;
                    return;
                }

                x = clutBitmap.Width;
                y = clutBitmap.Height;
                
                data = new float[x * y * 3];

                // 32bit
                for( int j = 0; j < y; j++)
                {
                    for(int i = 0; i < x; i++)
                    {
                        data[((y - j - 1) * x + i) * 3 + 2] = clutBitmap.GetPixel(i, j).R / 255.0f;
                        data[((y - j - 1) * x + i) * 3 + 1] = clutBitmap.GetPixel(i, j).G / 255.0f;
                        data[((y - j - 1) * x + i) * 3 + 0] = clutBitmap.GetPixel(i, j).B / 255.0f;
                    }
                }
            }

            public Bitmap Bitmap()
            {
                Bitmap returnBitmap = new Bitmap(x, y);

                for (int j = 0; j < y; j++)
                {
                    for (int i = 0; i < x; i++)
                    {
                        float r, g, b;

                        r = data[((y - j - 1) * x + i) * 3 + 2] * 255.0f;
                        g = data[((y - j - 1) * x + i) * 3 + 1] * 255.0f;
                        b = data[((y - j - 1) * x + i) * 3 + 0] * 255.0f;

                        returnBitmap.SetPixel(i, j, ColorBgra.FromBgr((byte)b, (byte)g, (byte)r));
                    }
                }

                return returnBitmap;
            }
        }

        ColorBgra correctPixel(ColorBgra pixelColor, ClutData clut, int level)
        {
            int color, red, green, blue, i, j;
            float r, g, b;
            float[] tmp = new float[6];
            float[] output = new float[3];
            float[] input = new float[]{
                pixelColor.R / 255.0f,
                pixelColor.G / 255.0f,
                pixelColor.B / 255.0f,
            };
            
            level *= level;
                        
            red = (int)(input[0] * (level - 1));
            if (red > level - 2)
                red = level - 2;
            if (red < 0)
                red = 0;

            green = (int)(input[1] * (level - 1));
            if (green > level - 2)
                green = level - 2;
            if (green < 0)
                green = 0;

            blue = (int)(input[2] * (level - 1));
            if (blue > level - 2)
                blue = level - 2;
            if (blue < 0)
                blue = 0;

            r = input[0] * (level - 1) - red;
            g = input[1] * (level - 1) - green;
            b = input[2] * (level - 1) - blue;

            color = red + green * level + blue * level * level;

            i = color * 3;
            j = (color + 1) * 3;

            tmp[0] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[1] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[2] = clut.data[i] * (1 - r) + clut.data[j] * r;

            i = (color + level) * 3;
            j = (color + level + 1) * 3;

            tmp[3] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[4] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[5] = clut.data[i] * (1 - r) + clut.data[j] * r;

            output[0] = tmp[0] * (1 - g) + tmp[3] * g;
            output[1] = tmp[1] * (1 - g) + tmp[4] * g;
            output[2] = tmp[2] * (1 - g) + tmp[5] * g;

            i = (color + level * level) * 3;
            j = (color + level * level + 1) * 3;

            tmp[0] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[1] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[2] = clut.data[i] * (1 - r) + clut.data[j] * r;

            i = (color + level + level * level) * 3;
            j = (color + level + level * level + 1) * 3;

            tmp[3] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[4] = clut.data[i++] * (1 - r) + clut.data[j++] * r;
            tmp[5] = clut.data[i] * (1 - r) + clut.data[j] * r;

            tmp[0] = tmp[0] * (1 - g) + tmp[3] * g;
            tmp[1] = tmp[1] * (1 - g) + tmp[4] * g;
            tmp[2] = tmp[2] * (1 - g) + tmp[5] * g;

            output[0] = output[0] * (1 - b) + tmp[0] * b;
            output[1] = output[1] * (1 - b) + tmp[1] * b;
            output[2] = output[2] * (1 - b) + tmp[2] * b;
            
            return ColorBgra.FromBgra((byte)(output[2] * 255.0f), (byte)(output[1] * 255.0f), (byte)(output[0] * 255.0f), pixelColor.A);
        }

        ColorBgra correctPixelCheap(ColorBgra pixelColour, Bitmap clut, int level)
        {
            int cubeSize = level * level;
            byte alpha = pixelColour.A;

            // Get R and G values
            int x = (int)((pixelColour.R / 256.0) * cubeSize);
            int y = (int)((pixelColour.G / 256.0) * level);

            // Get B value from depth
            int blue = (int)((pixelColour.B / 256.0) * clut.Width);
            int xp = blue % level;
            int yp = (blue / level);

            // Blue offset 
            x += cubeSize * xp;
            y += level * yp;

            // Correct R value offset
            if (x > 0) x--;

            pixelColour = clut.GetPixel(x, y);
            //pixelColour = ColorBgra.FromBgra((byte)(xp), (byte)(yp), 0, alpha);
            pixelColour.A = alpha;

            return pixelColour;
        }

        // Main render function
        void Render(Surface dst, Surface src, Rectangle rect)
        {
            ColorBgra currentPixel;
            int level;
            
            // Get CLUT image
            //Bitmap clutBitmap = new Bitmap(Image.FromFile("D:\\zachi\\Documents\\My Game DLLs\\Graphic Injectors\\Shaders\\HaldCLUT\\CLUTgallery\\Identity_level_8.HCLUT.png"));
            ClutData clut = new ClutData("D:\\zachi\\Documents\\My Game DLLs\\Graphic Injectors\\Shaders\\HaldCLUT\\CLUTgallery\\Identity_level_8.HCLUT.png");
            
            // Calculate image's cubic root
            for (level = 1; level * level * level < clut.x; level++);

            if (level * level * level > clut.x)
                return;

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;

                for (int x = rect.Left; x < rect.Right; x++)
                {
                    currentPixel = src[x, y];

                    var prevPix = currentPixel;

                    if (Checkk)
                    {
                        var nucl = correctPixel(currentPixel, clut, level);

                        currentPixel = ColorBgra.FromBgra((byte)(nucl.B - prevPix.B), (byte)(nucl.G - prevPix.G), (byte)(nucl.R - prevPix.R), currentPixel.A);
                    }
                    else
                        currentPixel = correctPixel(currentPixel, clut, level);

                    // Render to image pixels
                    dst[x, y] = currentPixel;
                }
            }
        }

        #endregion
    }
}
