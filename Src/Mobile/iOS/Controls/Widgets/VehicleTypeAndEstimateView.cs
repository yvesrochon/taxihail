using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using apcurium.MK.Booking.Mobile.Client.Style;
using System.Linq;
using apcurium.MK.Common.Extensions;
using System.Drawing;
using apcurium.MK.Booking.Mobile.Client.Helper;
using apcurium.MK.Booking.Mobile.Client.Extensions;
using apcurium.MK.Booking.Mobile.Client.Extensions.Helpers;
using MonoTouch.CoreGraphics;
using apcurium.MK.Booking.Mobile.Client.Localization;
using TinyIoC;
using apcurium.MK.Booking.Mobile.Infrastructure;

namespace apcurium.MK.Booking.Mobile.Client.Controls.Widgets
{
    [Register("VehicleTypeAndEstimateView")]
    public class VehicleTypeAndEstimateView : UIView
    {
        private UIView HorizontalDividerTop { get; set; }
        private UIImageView SelectedVehicleType { get; set; }
        private UILabel SelectedVehicleTypeLabel { get; set; }
        private UILabel EstimatedFareLabel { get; set; }

        public VehicleTypeAndEstimateView(IntPtr h):base(h)
        {
            Initialize();
        }
        public VehicleTypeAndEstimateView ( )
        {
            Initialize();
        }

        private void Initialize()
        {
            HorizontalDividerTop = new UIView(new RectangleF(0, 0, Frame.Width, UIHelper.OnePixel)) 
            { 
                BackgroundColor = Theme.LabelTextColor 
            };

            SelectedVehicleType = new UIImageView(new RectangleF(7f, 4f, 34f, 34f));

            SelectedVehicleTypeLabel = new UILabel
            {
                BackgroundColor = UIColor.Clear,
                Font = UIFont.FromName(FontName.HelveticaNeueBold, 18 / 2),
                TextColor = Theme.LabelTextColor,
                ShadowColor = UIColor.Clear
            };

            EstimatedFareLabel = new UILabel
            {
                BackgroundColor = UIColor.Clear,
                Lines = 0,
                Font = UIFont.FromName(FontName.HelveticaNeueRegular, 28 / 2),
                TextColor = Theme.LabelTextColor,
                ShadowColor = UIColor.Clear
            };

            this.SetRoundedCorners(UIRectCorner.BottomLeft | UIRectCorner.BottomRight, 3f);

            AddSubviews(HorizontalDividerTop, SelectedVehicleType, SelectedVehicleTypeLabel, EstimatedFareLabel);
        }

        private bool _showEstimate;
        public bool ShowEstimate
        {
            get { return _showEstimate; }
            set
            {
                if (_showEstimate != value)
                {
                    _showEstimate = value;
                    Redraw();
                }
            }
        }

        private string _vehicleType;
        public string VehicleType
        {
            get { return _vehicleType; }
            set
            {
                if (_vehicleType != value)
                {
                    SelectedVehicleType.Image = ImageHelper.ApplyColorToImage(string.Format("{0}_badge_selected.png", value.ToLower()), 
                        Theme.LabelTextColor, 
                        CGBlendMode.Normal);


					SelectedVehicleTypeLabel.Text = Localize.GetValue (value.ToUpper());
                    SelectedVehicleTypeLabel.SizeToFit();
                    SelectedVehicleTypeLabel.SetHorizontalCenter(SelectedVehicleType.Center.X);
                    SelectedVehicleTypeLabel.SetY(SelectedVehicleType.Frame.Bottom);
                }
            }
        }

        public string EstimatedFare
        {
            get { return EstimatedFareLabel.Text; }
            set
            {
                if (EstimatedFareLabel.Text != value)
                {
                    EstimatedFareLabel.Text = value;
                    var sizeThatFits = EstimatedFareLabel.SizeThatFits(new SizeF(Frame.Width - 56f - 5f, Frame.Height - 10f));
                    EstimatedFareLabel.SetWidth(sizeThatFits.Width);
                    EstimatedFareLabel.SetHeight(sizeThatFits.Height);
                    EstimatedFareLabel.SetX(56f);
                    EstimatedFareLabel.SetVerticalCenter(Frame.Height / 2);
                }
            }
        }

        private void Redraw()
        {
            if (ShowEstimate)
            {
				// this is the state where we show the logo and text of the selected vehicle type and the estimate

                BackgroundColor = Theme.CompanyColor;
                HorizontalDividerTop.BackgroundColor = Theme.LabelTextColor;
                SelectedVehicleType.Hidden = false;
                SelectedVehicleTypeLabel.Hidden = false;
                EstimatedFareLabel.Hidden = false;
            }
            else
            {
				// this is the state where we show the buttons to select vehicle type

                BackgroundColor = UIColor.Clear;
                HorizontalDividerTop.BackgroundColor = UIColor.FromRGB(177, 177, 177);
                SelectedVehicleType.Hidden = true;
                SelectedVehicleTypeLabel.Hidden = true;
                EstimatedFareLabel.Hidden = true;
            }
        }

        private NSLayoutConstraint[] _hiddenContraints { get; set; }
        public override bool Hidden
        {
            get
            {
                return base.Hidden;
            }
            set
            {
                if (base.Hidden != value)
                {
                    base.Hidden = value;
                    new [] { HorizontalDividerTop }.Where(c => c != null).ForEach(c => c.Hidden = value);   
                    if (value)
                    {
                        _hiddenContraints = this.Superview.Constraints != null 
                                            ? this.Superview.Constraints.Where(x => x.FirstItem == this || x.SecondItem == this).ToArray()
                                            : null;
                        if (_hiddenContraints != null)
                        {
                            this.Superview.RemoveConstraints(_hiddenContraints);
                        }
                    }
                    else
                    {
                        if (_hiddenContraints != null)
                        {
                            this.Superview.AddConstraints(_hiddenContraints);
                            _hiddenContraints = null;
                        }
                    }

                    if (Superview != null)
                    {
                        ((OrderOptionsControl)Superview.Superview).Resize();
                    }
                }
            }
        }
    }
}

