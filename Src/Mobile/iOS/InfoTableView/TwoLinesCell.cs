using System;
using System.Drawing;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using apcurium.MK.Common.Extensions;
using MonoTouch.CoreGraphics;
using apcurium.MK.Booking.Mobile.ListViewStructure;
using Cirrious.MvvmCross.Binding.Touch.Views;

namespace apcurium.MK.Booking.Mobile.Client.InfoTableView
{
	public class TwoLinesCell : MvxBindableTableViewCell
	{
		private UIImageView _arrowImage;
		private UIImageView _plusSignImage;
		private bool _showPlusSign;
		private bool _showArrow;
		private float _rowHeight = 44f;
		public TwoLinesCell (IntPtr handle, string bindingText) : base(bindingText, handle)
		{		
		}

		public TwoLinesCell (string cellIdentifier, string bindingText) : base( bindingText, UITableViewCellStyle.Subtitle, new NSString(cellIdentifier), UITableViewCellAccessory.None   )
		{					
			SelectionStyle = UITableViewCellSelectionStyle.None;
			Accessory = UITableViewCellAccessory.None;
			Initialize ();
		}

		public string FirstLine { get { return TextLabel.Text; }
			set { TextLabel.Text = value; }
		}

		public string SecondLine { get { return DetailTextLabel.Text; }
			set { DetailTextLabel.Text = value; }
		}

		public string test{get;set;}

		public bool ShowPlusSign { 
			get { return _showPlusSign; }
			set { 
				_showPlusSign = value;
				_plusSignImage.Hidden = !_showPlusSign;
			}
		}

		public bool ShowRightArrow { 
			get { return _showArrow; }
			set { 
				_showArrow = value;
				_arrowImage.Hidden = !_showArrow;
			}
		}

		private bool _isFirst, _isLast;
		public bool IsFirst { get { return _isFirst;} 
			set{ 
				_isFirst = value;
				((CustomCellBackgroundView2)BackgroundView).IsTop = _isFirst;
			}
		}
		public bool IsLast { get{ return _isLast;} 
			set{ 
				_isLast = value;
				((CustomCellBackgroundView2)BackgroundView).IsBottom = _isLast;
			}
		}

		private void Initialize ()
		{
			BackgroundView = new CustomCellBackgroundView2( IsFirst, IsLast, Frame, ShowPlusSign );
			TextLabel.TextColor = AppStyle.CellFirstLineTextColor;
			TextLabel.BackgroundColor = UIColor.Clear;
			TextLabel.Font = AppStyle.CellFont;

			DetailTextLabel.TextColor = AppStyle.CellSecondLineTextColor;
			DetailTextLabel.BackgroundColor = UIColor.Clear;
			DetailTextLabel.Font = AppStyle.NormalTextFont;

			_arrowImage = new UIImageView (new RectangleF (290, _rowHeight/2 - 15/2, 14, 15 ) ); 
			_arrowImage.BackgroundColor = UIColor.Clear;
			_arrowImage.ContentMode = UIViewContentMode.ScaleAspectFit;
			_arrowImage.Image = UIImage.FromFile("Assets/Cells/rightArrow.png");
			_arrowImage.Hidden = true;
			AddSubview ( _arrowImage );	
		
			_plusSignImage = new UIImageView (new RectangleF (290, _rowHeight/2 - 15/2, 14, 15 ) ); 
			_plusSignImage.BackgroundColor = UIColor.Clear;
			_plusSignImage.ContentMode = UIViewContentMode.ScaleAspectFit;
			_plusSignImage.Image = UIImage.FromFile("Assets/Cells/plusSign.png");
			_plusSignImage.Hidden = true;
			AddSubview ( _plusSignImage );	
		}
	
		public override void TouchesBegan ( NSSet touches, UIEvent evt )
		{	
			BackgroundView.SetNeedsDisplay();
			base.TouchesBegan ( touches, evt );	
		}
		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			SetHighlighted( false, true );
			BackgroundView.SetNeedsDisplay();
			base.TouchesEnded (touches, evt);
		}
		public override void TouchesCancelled (NSSet touches, UIEvent evt)
		{
			SetHighlighted( false, true );
			BackgroundView.SetNeedsDisplay();
			base.TouchesCancelled (touches, evt);
		}


	}

	public class CustomCellBackgroundView2 : UIView
	{
		private bool _isTop;
		private bool _isBottom;
		private float _cornerRadius = 3f;
		private float _strokeSize = 1f;
		private float _innerShadowTopBottomBlurRadius = 3f;
		private float _innerShadowSidesBlurRadius = 2f;
		private UIColor _strokeColor = UIColor.FromRGB( 105, 105, 105 );
		private UIColor _selectedBackgroundColor = UIColor.FromRGBA( 233, 217, 219, 0.1f );
		private UIColor _backgroundColor = AppStyle.CellBackgroundColor;

		public CustomCellBackgroundView2(bool isTop, bool isBottom, RectangleF rect, bool isAddNewCell ) : base( rect )
		{
			_isTop = isTop;
			_isBottom = isBottom;
			BackgroundColor = UIColor.Clear;
			IsAddNewCell = isAddNewCell;

		}

		public bool IsAddNewCell { get; set; }

		public bool IsTop { 
			get { return _isTop; } 
			set { _isTop = value; }
		}

		public bool IsBottom { 
			get { return _isBottom; } 
			set { _isBottom = value; }
		}

		public override void Draw (RectangleF rect)
		{
			base.Draw (rect);

			var context = UIGraphics.GetCurrentContext();

			context.SetLineWidth( _strokeSize );

			UIBezierPath fillRectPath;
			UIBezierPath strokePath = new UIBezierPath();
			if( _isTop && _isBottom )
			{
				fillRectPath = UIBezierPath.FromRoundedRect( rect, _cornerRadius );
				strokePath = fillRectPath;
			}
			else if( _isTop )
			{
				fillRectPath = UIBezierPath.FromRoundedRect(rect, UIRectCorner.TopLeft | UIRectCorner.TopRight, new SizeF(_cornerRadius,_cornerRadius) );
				strokePath.MoveTo( new PointF( rect.Left, rect.Bottom ));
				strokePath.AddLineTo( new PointF( rect.Left, rect.Top + _cornerRadius ));
				strokePath.AddArc( new PointF( rect.Left + _cornerRadius, rect.Top + _cornerRadius), _cornerRadius, (float)Math.PI, (float)(3*Math.PI/2), true );
				strokePath.AddLineTo( new PointF( rect.Right - _cornerRadius, rect.Top ));
				strokePath.AddArc( new PointF( rect.Right - _cornerRadius, rect.Top + _cornerRadius), _cornerRadius, (float)(3*Math.PI/2), 0, true );
				strokePath.AddLineTo( new PointF( rect.Right, rect.Bottom ));
				strokePath.AddLineTo( new PointF( rect.Left, rect.Bottom ));
			}
			else if( _isBottom )
			{
				fillRectPath = UIBezierPath.FromRoundedRect(rect, UIRectCorner.BottomLeft | UIRectCorner.BottomRight, new SizeF(_cornerRadius,_cornerRadius) );
				strokePath.MoveTo( new PointF( rect.Right, rect.Top ));
				strokePath.AddLineTo( new PointF( rect.Right, rect.Bottom - _cornerRadius ));
				strokePath.AddArc( new PointF( rect.Right - _cornerRadius, rect.Bottom - _cornerRadius), _cornerRadius, 0, (float)(Math.PI/2), true );
				strokePath.AddLineTo( new PointF( rect.Left + _cornerRadius, rect.Bottom ));
				strokePath.AddArc( new PointF( rect.Left + _cornerRadius, rect.Bottom - _cornerRadius), _cornerRadius, (float)(Math.PI/2), (float)Math.PI, true );
				strokePath.AddLineTo( new PointF( rect.Left, rect.Top) );
			}
			else
			{
				fillRectPath = UIBezierPath.FromRect( rect );
				strokePath.MoveTo( new PointF( rect.Left, rect.Top) );
				strokePath.AddLineTo( new PointF( rect.Left, rect.Bottom ));
				strokePath.AddLineTo( new PointF( rect.Right, rect.Bottom) );
				strokePath.AddLineTo( new PointF( rect.Right, rect.Top) );
			}

			//Fill
			if( IsAddNewCell && !((UITableViewCell)Superview).Highlighted )
			{
				UIImage.FromFile( "Assets/Cells/addNewBackground.png" ).Draw( rect, CGBlendMode.Normal, 1f );
			}
			else if( IsAddNewCell && ((UITableViewCell)Superview).Highlighted )
			{
				UIImage.FromFile( "Assets/Cells/addNewBackgroundSelected.png" ).Draw( rect, CGBlendMode.Normal, 1f );
			}
			else
			{
	  			var backgroundColor = ((UITableViewCell)Superview).Highlighted ? _selectedBackgroundColor : _backgroundColor;
				backgroundColor.SetFill();
				fillRectPath.LineWidth = _strokeSize;
				fillRectPath.Fill();
			}


			//Inner Shadow
		    var roundedRectangleBorderRect = fillRectPath.Bounds;
			roundedRectangleBorderRect.X -= _innerShadowSidesBlurRadius;
			roundedRectangleBorderRect.Width += 2*_innerShadowSidesBlurRadius;
			roundedRectangleBorderRect.Y -= _isTop ? _innerShadowTopBottomBlurRadius : 0;
			roundedRectangleBorderRect.Height += _isBottom ? 2*_innerShadowTopBottomBlurRadius : 0;

            roundedRectangleBorderRect = RectangleF.Union(roundedRectangleBorderRect, fillRectPath.Bounds);
			roundedRectangleBorderRect.X -= 1;
			roundedRectangleBorderRect.Width += 2;
			roundedRectangleBorderRect.Y -= 0;
			roundedRectangleBorderRect.Height += 0;

            var roundedRectangleNegativePath = UIBezierPath.FromRect(roundedRectangleBorderRect);
            roundedRectangleNegativePath.AppendPath(fillRectPath);
            roundedRectangleNegativePath.UsesEvenOddFillRule = true;

            context.SaveState();
            {
                var xOffset = (float)Math.Round(roundedRectangleBorderRect.Width);
				var yOffset = 0;
                context.SetShadowWithColor(
                    new SizeF(xOffset + (xOffset >= 0 ? 0.1f : -0.1f), yOffset + (yOffset >= 0 ? 0.1f : -0.1f)),
                    _innerShadowTopBottomBlurRadius,
					_strokeColor.CGColor);

                fillRectPath.AddClip();
                var transform = CGAffineTransform.MakeTranslation(-(float)Math.Round(roundedRectangleBorderRect.Width), 0);
                roundedRectangleNegativePath.ApplyTransform(transform);
                UIColor.Gray.SetFill();
                roundedRectangleNegativePath.Fill();
            }
            context.RestoreState();




			//Stroke
			context.SaveState();
            strokePath.LineWidth = _strokeSize;
            _strokeColor.SetStroke();
            strokePath.AddClip();
            context.AddPath(strokePath.CGPath);
            context.StrokePath();
            context.RestoreState();     

		}
	}
}
