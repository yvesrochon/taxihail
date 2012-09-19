using System;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;
using MonoTouch.CoreFoundation;
using MonoTouch.Foundation;
using System.Drawing;
using Cirrious.MvvmCross.Interfaces.Commands;

namespace apcurium.MK.Booking.Mobile.Client
{
    [Register("SearchTextField")]
	public class SearchTextField : UITextField
    {

        private UIColor _strokeColor = UIColor.FromRGBA(82, 82, 82, 255);
        private float _paddingRight = 0;
        private float _paddingLeft = 0;

		public SearchTextField(IntPtr handle) : base(handle)
        {
            Initialize();
        }

		public SearchTextField(RectangleF rect) : base( rect )
        {
            Initialize();
        }

        private void Initialize()
        {
            BorderStyle = UITextBorderStyle.Line;
            BackgroundColor = UIColor.Clear;
        }

        public float PaddingLeft
        {
            get{ return _paddingLeft; }
            set
            {
                _paddingLeft = value;
                if (_paddingLeft != 0)
                {
                    LeftView = new UIView{ Frame = new RectangleF( 0,0,_paddingLeft,30) };
                    LeftViewMode = UITextFieldViewMode.Always;
                }
                else
                {
                    LeftViewMode = UITextFieldViewMode.Never;
                }
            }
        }

        public float PaddingRight
        {
            get{ return _paddingRight; }
            set
            {
                _paddingRight = value;
                if (_paddingRight != 0)
                {
                    RightView = new UIView{ Frame = new RectangleF( 0,0,_paddingRight,30) };
                    RightViewMode = UITextFieldViewMode.Always;
                }
                else
                {
                    RightViewMode = UITextFieldViewMode.Never;
                }
            }
                
        }

		public void SetImage( string image )
		{
			var img = UIImage.FromFile( image );
			var imgView = new UIImageView( new RectangleF( 5, Frame.Height/2 - img.Size.Height/2, img.Size.Width, img.Size.Height ) );
			imgView.BackgroundColor = UIColor.Clear;
			imgView.Image = img;

			AddSubview( imgView );

			PaddingLeft += img.Size.Width + 5;
		}


        public UIColor StrokeColor
        {
            get { return _strokeColor;}
            set { _strokeColor = value; }
        }

		public override void DidChange (NSKeyValueChange changeKind, NSIndexSet indexes, NSString forKey)
		{
			base.DidChange (changeKind, indexes, forKey);
			if ((TextChangedCommand != null) && (TextChangedCommand.CanExecute()))
			{
				TextChangedCommand.Execute();
			}
		} 

		public override void DidChange (NSString forKey, NSKeyValueSetMutationKind mutationKind, NSSet objects)
		{
			base.DidChange (forKey, mutationKind, objects);
		}

		public override void DidChangeValue (string forKey)
		{
			base.DidChangeValue (forKey);
		}
	


		public IMvxCommand TextChangedCommand { get; set; }

        public override void Draw(System.Drawing.RectangleF frame)
        {

            // General Declarations
            var context = UIGraphics.GetCurrentContext();

            // Color Declarations
            UIColor color = UIColor.FromRGBA(0.00f, 0.00f, 0.00f, 1.00f);
            UIColor color3 = color.ColorWithAlpha(0.6f);
            UIColor wColor = UIColor.FromRGBA(1.00f, 1.00f, 1.00f, 1.00f);
            UIColor color2 = wColor.ColorWithAlpha(0.2f);


            // Shadow Declarations
            var inner = color3.CGColor;
            var innerOffset = new SizeF(0, 1);
            var innerBlurRadius = 2;
            var outer = color2.CGColor;
            var outerOffset = new SizeF(0, 1);
            var outerBlurRadius = 1;

            // Rectangle Drawing
            context.SetShadowWithColor(outerOffset, outerBlurRadius, outer);

            var rectanglePath = UIBezierPath.FromRoundedRect(new RectangleF(frame.GetMinX() + 1f, frame.GetMinY() + 1f, frame.Width - 2, frame.Height - 2), 3);
            UIColor.White.SetFill();
            rectanglePath.Fill();

            // Rectangle Inner Shadow
            var rectangleBorderRect = rectanglePath.Bounds;
            rectangleBorderRect.Inflate(innerBlurRadius, innerBlurRadius);
            rectangleBorderRect.Offset(-innerOffset.Width, -innerOffset.Height);
            rectangleBorderRect = RectangleF.Union(rectangleBorderRect, rectanglePath.Bounds);
            rectangleBorderRect.Inflate(1, 1);

            var rectangleNegativePath = UIBezierPath.FromRect(rectangleBorderRect);
            rectangleNegativePath.AppendPath(rectanglePath);
            rectangleNegativePath.UsesEvenOddFillRule = true;

            context.SaveState();
            {
                var xOffset = innerOffset.Width + (float)Math.Round(rectangleBorderRect.Width);
                var yOffset = innerOffset.Height;
                context.SetShadowWithColor(new SizeF(xOffset + (xOffset >= 0 ? 0.1f : -0.1f), yOffset + (yOffset >= 0 ? 0.1f : -0.1f)), innerBlurRadius, inner);

                rectanglePath.AddClip();
                var transform = CGAffineTransform.MakeTranslation(-(float)Math.Round(rectangleBorderRect.Width), 0);
                rectangleNegativePath.ApplyTransform(transform);
                UIColor.Gray.SetFill();
                rectangleNegativePath.Fill();
            }
            context.RestoreState();


            context.SaveState();
            rectanglePath.LineWidth = 1f;
            _strokeColor.SetStroke();
            rectanglePath.AddClip();
            context.AddPath(rectanglePath.CGPath);
            context.StrokePath();
            context.RestoreState();           

        }
    }
}