﻿using System;
using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using System.Drawing;
using Color = Android.Graphics.Color;
using Point = System.Drawing.Point;
using SizeF = System.Drawing.SizeF;
using Android.Support.V4.Content;

namespace apcurium.MK.Booking.Mobile.Client.Helpers
{
    public static class DrawHelper
    {
        public static int ToPixels(this float dip)
        {
            return GetPixels(dip);
        }

        public static int ToPixels(this int dip)
        {
            return GetPixels(dip);
        }

        public static int GetPixels(float dipValue)
        {
            return (int)Math.Round(TypedValue.ApplyDimension(ComplexUnitType.Dip, dipValue, Application.Context.Resources.DisplayMetrics), MidpointRounding.AwayFromZero);
        }
 
        private static Bitmap DrawableToBitmap (Drawable drawable, Color? colorFilter = null) 
        {
            if (colorFilter != null)
            {
                drawable.SetColorFilter ((Color)colorFilter, PorterDuff.Mode.SrcIn);
            }

            var bitmap = Bitmap.CreateBitmap(drawable.IntrinsicWidth, drawable.IntrinsicHeight, Bitmap.Config.Argb8888);
            var canvas = new Canvas(bitmap); 

            drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
            drawable.Draw(canvas);

            return bitmap;
        }

        public static Bitmap ApplyColorToMapIcon(int foregroundResource, Color color, bool isBigIcon)
        {
            var foreground = ContextCompat.GetDrawable(Application.Context, foregroundResource);

            var originalImageSize = isBigIcon 
                ? new SizeF(52, 58)
                : new SizeF(34, 39);

            if (ImageWasOverridden(foreground, originalImageSize, Color.Transparent, isBigIcon ? new Point(26, 29) : new Point(18, 16)))
            {
                return DrawableToBitmap(foreground);
            }

            var backgroundToColorize = isBigIcon
                ? ContextCompat.GetDrawable(Application.Context, Resource.Drawable.map_bigicon_background)
                : ContextCompat.GetDrawable(Application.Context, Resource.Drawable.map_smallicon_background);

            var bitmapOverlay = Bitmap.CreateBitmap(backgroundToColorize.IntrinsicWidth, backgroundToColorize.IntrinsicHeight, Bitmap.Config.Argb8888);
            var canvas = new Canvas(bitmapOverlay);

            canvas.DrawBitmap (DrawableToBitmap (backgroundToColorize, color), new Matrix (), null);
            canvas.DrawBitmap (DrawableToBitmap (foreground), 0, 0, null);

            return bitmapOverlay;
        }

        public static Color GetTextColorForBackground(Color backgroundColor)
        {
            if (IsThisColorLight(backgroundColor))
            {
                return Color.Black;
            }
            
            return Color.White;
        }

        public static void SetEditTextBackgroundForBackground(EditText editText, Color backgroundColor)
        {
            var backgroundDrawable = Application.Context.Resources.GetDrawable(Resource.Drawable.edit_text_flat);

            if (IsThisColorLight(backgroundColor))
            {
                backgroundDrawable = Application.Context.Resources.GetDrawable(Resource.Drawable.edit_text_black_border);
            }

            editText.Background = backgroundDrawable;
        }

        public static bool IsThisColorLight(Color color)
        {
            var darknessScore = (((color.R) * 299) + ((color.G) * 587) + ((color.B) * 114)) / 1000;

            return darknessScore >= 125;
        }

        public static Bitmap ApplyThemeColorToImage(int drawableResource, bool skipApplyIfCustomImage = false, SizeF originalImageSize = new SizeF(), Color? expectedColor = null, Point? expectedColorCoordinate = null)
        {
            var drawable = ContextCompat.GetDrawable(Application.Context, drawableResource);
            if (skipApplyIfCustomImage)
            {
                if (ImageWasOverridden(drawable, originalImageSize, expectedColor, expectedColorCoordinate))
                {
                    return DrawableToBitmap(drawable);
                }
            }

            return DrawableToBitmap(drawable, Application.Context.Resources.GetColor (Resource.Color.company_color));
        }

        public static void SupportLoginTextColor(TextView textView)
        {
            int[][] states = new int[1][];
            states[0] = new int[0];
            var colors = new[]{(int)GetTextColorForBackground(textView.Resources.GetColor(Resource.Color.login_color))};
            var colorList = new ColorStateList (states, colors);
            textView.SetTextColor(colorList);
        }

        private static bool ImageWasOverridden(Drawable image, SizeF originalImageSize, Color? expectedColor, Point? expectedColorCoordinate)
        {
            var densityAdjustedWidth = originalImageSize.Width.ToPixels();
            var differentSize = image.IntrinsicWidth != densityAdjustedWidth;
            if (differentSize)
            {
                return true;
            }

            if (expectedColor == null || expectedColorCoordinate == null)
            {
                return false;
            }

            var bitmap = DrawHelper.DrawableToBitmap (image);

            var defaultDensity = 160;
            var factor = (double)bitmap.Density / (double)defaultDensity;
            var correctedX = expectedColorCoordinate.Value.X * factor;
            var correctedY = expectedColorCoordinate.Value.Y * factor;

            var detectedColor = bitmap.GetPixel((int)correctedX, (int)correctedY);
            var differentColorThanExpected = !detectedColor.Equals(expectedColor.Value.ToArgb());

            return differentColorThanExpected;
        }

		public static Bitmap RotateImageByDegrees(int imageResource, double degrees)
		{
			var image = DrawHelper.DrawableToBitmap (Application.Context.Resources.GetDrawable(imageResource));

			Matrix matrix = new Matrix();
			matrix.PostRotate((float)degrees);

			return Bitmap.CreateBitmap(image, 0, 0, image.Width, image.Height, matrix, true);
		}

		public static Bitmap RotateImageByDegreesWithСenterCrop(int imageResource, double degrees)
		{
			var originalImage = Application.Context.Resources.GetDrawable(imageResource);

			var rotatedImage = RotateImageByDegrees(imageResource, degrees);
			var croppedImage = Bitmap.CreateBitmap(originalImage.IntrinsicWidth, originalImage.IntrinsicHeight, Bitmap.Config.Argb8888);

			var rectDestination = new Rect()
			{
				Left = 0,
				Right = originalImage.IntrinsicWidth,
				Top = 0,
				Bottom = originalImage.IntrinsicHeight
			};

			var rectSource = new Rect()
			{
				Left = (rotatedImage.Width / 2) - (originalImage.IntrinsicWidth / 2),
				Right = (rotatedImage.Width / 2) + (originalImage.IntrinsicWidth / 2),
				Top = (rotatedImage.Height / 2) - (originalImage.IntrinsicHeight / 2),
				Bottom = (rotatedImage.Height / 2) + (originalImage.IntrinsicHeight / 2)
			};

			var croppedImageCanvas = new Canvas(croppedImage);

			croppedImageCanvas.DrawBitmap(rotatedImage, rectSource, rectDestination, new Paint());

			return croppedImage;
		}
    }
}