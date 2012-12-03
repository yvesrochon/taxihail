using System;
using MonoTouch.Foundation;
using System.Drawing;
using MonoTouch.UIKit;
using Cirrious.MvvmCross.Dialog.Touch.Dialog.Elements;
using Cirrious.MvvmCross.Dialog.Touch.Dialog;
using apcurium.MK.Common.Entity;
using System.Collections.Generic;

namespace apcurium.MK.Booking.Mobile.Client
{
    [Register("ModalTextField")]
    public class ModalTextField : TextField
    {
        RootElement _rootElement;
        UIImageView _rightArrow;  

        public ModalTextField(IntPtr handle) : base(handle)
        {
            Initialize();
        }
        
        public ModalTextField(RectangleF rect) : base( rect )
        {
            Initialize();
        }

        private void Initialize() {
            EditingDidBegin += HandleEditingDidBegin;
        }

        public override void WillMoveToSuperview (UIView newsuper)
        {
            base.WillMoveToSuperview (newsuper);
            if (_rightArrow == null) {
                _rightArrow = new UIImageView(new RectangleF(this.Frame.Width - 25, this.Frame.Height/2 - 7,9, 13));
                _rightArrow.Image = UIImage.FromFile("Assets/Cells/rightArrow.png");
                this.AddSubview(_rightArrow);
            }
        }

        void HandleEditingDidBegin (object sender, EventArgs e)
        {
            Controller.View.EndEditing(true);
            if (_rootElement != null) {
                var newDvc = new DialogViewController (_rootElement, true) {
                    Autorotate = true
                };
                Controller.PresentViewController (newDvc, true, delegate { });
            }
        }

        public void Configure (string title, ListItem[] values, int selectedId, Action<ListItem> onItemSelected)
        {
            int selected = 0;
            var section = new SectionWithBackground(title);
            foreach (ListItem v in values)
            {
                // Keep a reference to value in order for callbacks to work correctly
                var value = v;
                var item = new RadioElementWithId(value.Id, value.Display);
                item.Tapped += ()=> {
                    onItemSelected(value);
                    Controller.DismissViewController(true, delegate {});
                };
                section.Add(item);
                if (selectedId == value.Id)
                {
                    selected = Array.IndexOf(values, value);
                }
            }
            
            _rootElement = new CustomRootElement(Resources.RideSettingsVehiculeType, new RadioGroup(selected));
            _rootElement.Add(section);
        }

        void HandleTouchDown (object sender, EventArgs e)
        {

        }

        protected UIViewController Controller {
            get {
                return (UIViewController)FindViewController(this);
            }
        }

        private UIResponder FindViewController (UIResponder responder)
        {
            if (responder is UIViewController) {
                return responder;
            }
            if (responder is UIView) {
                return FindViewController(responder.NextResponder);
            }
            return null;
        }
    }
}

