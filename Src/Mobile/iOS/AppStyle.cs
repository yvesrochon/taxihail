using System.Linq;
using System.Drawing;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using System.Collections.Generic;
using apcurium.MK.Booking.Mobile.Style;

namespace apcurium.MK.Booking.Mobile.Client
{
    public class AppStyle
    {
        public enum ButtonColor
        {
            Black,
            Grey,
            Green,
            Red,
            CorporateColor,
            Silver,
            AlternateCorporateColor }
        ;

    
        public static float ButtonStrokeLineWidth { get { return 2f; } }

        public static float ButtonCornerRadius { get { return 2f; } }
      

        public static UIColor[] GetButtonColors(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());
            return style.Colors.Select(c => UIColor.FromRGBA(c.Red, c.Green, c.Blue, c.Alpha)).ToArray();          
        }

        public static float[] GetButtonColorLocations(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());
            return  style.Colors.Select(c => c.Location).ToArray();
        }

        public static UIColor GetButtonTextShadowColor(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());
            return UIColor.FromRGBA(style.TextShadowColor.Red, style.TextShadowColor.Green, style.TextShadowColor.Blue, style.TextShadowColor .Alpha);
        }
        public static UIColor GetButtonTextColor(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());
            return UIColor.FromRGBA(style.TextColor.Red, style.TextColor.Green, style.TextColor.Blue, style.TextColor .Alpha);
        }

        public static ShadowSetting GetInnerShadow(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());

            if (style.InnerShadow != null)
            {
                return new ShadowSetting{ BlurRadius = style.InnerShadow.BlurRadius, 
                                                                            Offset = new SizeF( style.InnerShadow.OffsetX, style.InnerShadow.OffsetY ), 
                                                                            Color = UIColor.FromRGBA( style.InnerShadow.Color.Red, style.InnerShadow.Color.Green, style.InnerShadow.Color.Blue , style.InnerShadow.Color.Alpha) };
            }
            else
            {
                return null;
            }
           
        }

        public static ShadowSetting GetDropShadow(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());
            if (style.DropShadow != null)
            {
                return new ShadowSetting{ BlurRadius = style.DropShadow.BlurRadius, 
                                                                            Offset = new SizeF( style.DropShadow.OffsetX, style.DropShadow.OffsetY ),   
                                                                            Color = UIColor.FromRGBA( style.DropShadow.Color.Red, style.DropShadow.Color.Green, style.DropShadow.Color.Blue , style.DropShadow.Color.Alpha) };                
            }
            else
            {
                return null;
            }

        }

        public static UIColor GetButtonStrokeColor(ButtonColor color)
        {
            var style = StyleManager.Current.Buttons.Single(c => c.Key == color.ToString());
            return UIColor.FromRGBA(style.StrokeColor.Red, style.StrokeColor.Green, style.StrokeColor.Blue, style.StrokeColor .Alpha);
        }

        public static UIColor GreyText { get { return UIColor.FromRGB(101, 101, 101); } }

        public static UIColor NavigationTitleColor { get { return  UIColor.FromRGBA(StyleManager.Current.NavigationTitleColor.Red, StyleManager.Current.NavigationTitleColor.Green, StyleManager.Current.NavigationTitleColor.Blue, StyleManager.Current.NavigationTitleColor.Alpha); } }

        public static UIColor NavigationBarColor { get { return  UIColor.FromRGBA(StyleManager.Current.NavigationBarColor.Red, StyleManager.Current.NavigationBarColor.Green, StyleManager.Current.NavigationBarColor.Blue, StyleManager.Current.NavigationBarColor.Alpha); } }

        public static UIColor LightCorporateColor { get { return  UIColor.FromRGBA(StyleManager.Current.LightCorporateTextColor.Red, StyleManager.Current.LightCorporateTextColor.Green, StyleManager.Current.LightCorporateTextColor.Blue, StyleManager.Current.LightCorporateTextColor.Alpha); } }

        public static UIColor DarkText { get { return UIColor.FromRGB(57, 44, 11); } }

	    public static UIColor TitleTextColor { get { return UIColor.FromRGB(66, 63, 58); } }

		public static UIFont NormalTextFont { get { return UIFont.FromName( "HelveticaNeue", CellFontSize ); } }

		public static UIFont BoldTextFont { get { return UIFont.FromName( "HelveticaNeue-Bold", CellFontSize ); } }

        public static string ButtonFontName { get { return "HelveticaNeue-Bold"; } }

		public static UIFont CellFont { get { return UIFont.FromName( "HelveticaNeue-Bold", CellFontSize ); } }

		public static float CellFontSize { get { return 14f; } }

        public static float ButtonFontSize { get { return 14f; } }

        public static UIFont GetButtonFont(float size)
        {
            return UIFont.FromName(ButtonFontName, size);
        }
        
        public  static  UIColor AcceptButtonColor{ get { return UIColor.FromRGB(19, 147, 11); } }

        public  static UIColor AcceptButtonHighlightedColor{ get { return UIColor.FromRGB(12, 98, 8); } }
        
        public  static UIColor CancelButtonColor{ get { return UIColor.FromRGB(215, 0, 2); } }

        public  static UIColor CancelButtonHighlightedColor{ get { return UIColor.FromRGB(136, 0, 2); } }
        
        public  static UIColor LightButtonColor{ get { return UIColor.FromRGB(90, 90, 90); } }

        public  static UIColor LightButtonHighlightedColor{ get { return UIColor.FromRGB(50, 50, 50); } }

		public static UIColor CellBackgroundColor{ get{ return UIColor.FromRGB(253, 253, 253); } }
		public static UIColor CellFirstLineTextColor{ get{ return UIColor.FromRGB(77, 77, 77); } }
		public static UIColor CellSecondLineTextColor{ get{ return UIColor.FromRGB(133, 133, 133); } }


    }

    public class ShadowSetting
    {
        public float BlurRadius { get; set; }

        public SizeF Offset { get; set; }

        public UIColor Color { get; set; }

    }
}

