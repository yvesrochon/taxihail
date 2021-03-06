using UIKit;
using apcurium.MK.Booking.Mobile.Client.Extensions;
using apcurium.MK.Booking.Mobile.Client.Helper;
using apcurium.MK.Booking.Mobile.Client.Style;
using CoreGraphics;
using System;

namespace apcurium.MK.Booking.Mobile.Client.Controls.Message
{
    public class LoadingOverlayView: UIView
    {
        private UIView _dialogView;
        private UIImageView _imageView;
        private CircularProgressView _progressView;
        private static nfloat _dialogWidth = UIScreen.MainScreen.Bounds.Width;
        private static nfloat _dialogHeight = 95;
        private static bool _isLoading;
        private UIWindow _modalWindow;

        public LoadingOverlayView()
        {
            BackgroundColor = UIColor.Black.ColorWithAlpha(0.75f);
            Frame = UIScreen.MainScreen.Bounds;

            _dialogView = new UIView();
            _dialogView.BackgroundColor = UIColor.White;

            _imageView = new UIImageView (ImageHelper.ApplyThemeColorToImage("taxi_progress.png", true, new CGSize(52, 20), UIColor.FromRGBA (0, 122, 255, 255), new CGPoint (25, 10)));
                
            _imageView.SizeToFit();
            _imageView.Hidden = true;

            _progressView = new CircularProgressView(new CGRect(0, 0, 67, 67),  Theme.CompanyColor);
            _progressView.OnCompleted = () => Hide();
            _progressView.LineWidth = 1.5f;
            _progressView.Hidden = true;

            _dialogView.Frame  = new CGRect(0, UIScreen.MainScreen.Bounds.Height / 2, UIScreen.MainScreen.Bounds.Width, 0);

            _progressView.SetHorizontalCenter(UIScreen.MainScreen.Bounds.Width / 2);
            _progressView.SetVerticalCenter(UIScreen.MainScreen.Bounds.Height / 2);

            _imageView.SetHorizontalCenter(UIScreen.MainScreen.Bounds.Width / 2);
            _imageView.SetVerticalCenter(UIScreen.MainScreen.Bounds.Height / 2);

            AddSubviews(_dialogView, _progressView, _imageView);
        }

        private void Animate()
        {
            var options = UIViewAnimationOptions.CurveEaseIn;

            UIView.Animate(
                0.2, 0, options, 
                () =>
            {
                _dialogView.Frame = new CGRect(0, (UIScreen.MainScreen.Bounds.Height - _dialogHeight) / 2, _dialogWidth, _dialogHeight);
            },
                () => 
            {
                _progressView.Hidden = false;
                _imageView.Hidden = false;

                if(_isLoading)
                {
                    IncreaseProgress();
                    Animate();
                }
                else
                {
                    _progressView.Progress = 1f;
                }
            });
        }
        private void IncreaseProgress()
        {
            var currentProgress = _progressView.Progress;

            var slowestSpeed = 0.00001f;

            // multistage progress speed
            if (currentProgress <= 0.2f)
            {
                _progressView.Progress += slowestSpeed * 50f;
                return;
            }

            if (currentProgress < 0.8f)
            {
                _progressView.Progress += slowestSpeed * 10f;
                return;
            }

            if (currentProgress > 0.8f)
            {
                var nextProgress = currentProgress + slowestSpeed;
                if (nextProgress >= 0.95f)
                {
                    nextProgress = 0.95f;
                }
                _progressView.Progress = nextProgress;
            }
        }

        public void Show()
        {
            _modalWindow =  _modalWindow ?? new UIWindow(UIScreen.MainScreen.Bounds)
            { 
                WindowLevel = UIWindowLevel.Alert,
                RootViewController = new UIViewController()
            };

            _modalWindow.MakeKeyAndVisible();
            _modalWindow.RootViewController.View.AddSubview(this);

            _isLoading = true;
            Animate();
        }

        public void Dismiss()
        {
            // Will hide this view at the end of next animation cycle
            _isLoading = false;
        }

        private void Hide()
        {
            if (_modalWindow == UIApplication.SharedApplication.KeyWindow)
            {
                UIApplication.SharedApplication.Windows[0].MakeKeyWindow();
            }
            this.RemoveFromSuperview();
            _modalWindow.Hidden = true;
        }
    }
}

