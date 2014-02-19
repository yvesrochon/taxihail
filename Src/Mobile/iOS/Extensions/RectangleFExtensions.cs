using System.Drawing;

namespace apcurium.MK.Booking.Mobile.Client.Extensions
{
    public static class RectangleFExtensions
    {
        public static RectangleF IncrementX(this RectangleF thisRectangleF, float delta)
        {
            thisRectangleF.X += delta;
            return thisRectangleF;
        }

        public static RectangleF IncrementY(this RectangleF thisRectangleF, float delta)
        {
            thisRectangleF.Y += delta;
            return thisRectangleF;
        }

        public static RectangleF SetX(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.X = value;
            return thisRectangleF;
        }

        public static RectangleF SetY(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.Y = value;
            return thisRectangleF;
        }

        public static RectangleF SetBottom(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.Height = value - thisRectangleF.Y;
            return thisRectangleF;
        }

        public static RectangleF SetRight(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.X = value - thisRectangleF.Width;
            return thisRectangleF;
        }

        public static RectangleF SetHorizontalCenter(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.X = value - (thisRectangleF.Width/2);
            return thisRectangleF;
        }

        public static RectangleF SetVerticalCenter(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.Y = value - (thisRectangleF.Height/2);
            return thisRectangleF;
        }

        public static RectangleF Grow(this RectangleF rect, float numberOfPixels)
        {
            return Shrink(rect, -numberOfPixels);
        }

        public static RectangleF Shrink(this RectangleF rect, float numberOfPixels)
        {
            rect.Y += numberOfPixels;
            rect.X += numberOfPixels;
            rect.Width -= (numberOfPixels * 2);
            rect.Height -= (numberOfPixels * 2);
            return rect;
        }

        public static RectangleF Copy(this RectangleF thisRectangleF)
        {
            return new RectangleF(thisRectangleF.X, thisRectangleF.Y, thisRectangleF.Width, thisRectangleF.Height);
        }

        public static RectangleF IncrementHeight(this RectangleF thisRectangleF, float delta)
        {
            thisRectangleF.Height += delta;
            return thisRectangleF;
        }

        public static RectangleF IncrementWidth(this RectangleF thisRectangleF, float delta)
        {
            thisRectangleF.Width += delta;
            return thisRectangleF;
        }

        public static RectangleF SetWidth(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.Width = value;
            return thisRectangleF;
        }       

        public static RectangleF SetHeight(this RectangleF thisRectangleF, float value)
        {
            thisRectangleF.Height = value;
            return thisRectangleF;
        }
    }
}

