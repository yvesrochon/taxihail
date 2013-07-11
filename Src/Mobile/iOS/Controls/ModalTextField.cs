using System;
using MonoTouch.Foundation;
using System.Drawing;
using MonoTouch.UIKit;
using Cirrious.MvvmCross.Dialog.Touch.Dialog.Elements;
using Cirrious.MvvmCross.Dialog.Touch.Dialog;
using apcurium.MK.Common.Entity;
using System.Collections.Generic;
using apcurium.MK.Booking.Mobile.Infrastructure;
using TinyIoC;
using System.Globalization;

namespace apcurium.MK.Booking.Mobile.Client.Controls
{
    [Register("ModalTextField")]
    public class ModalTextField : TextFieldWithArrow
    {
        RootElement _rootElement;

        public ModalTextField(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        public ModalTextField(RectangleF rect) : base ( rect ) {
            Initialize();
        }

        private void Initialize() {
        }

        public override void WillMoveToSuperview (UIView newsuper)
        {
            base.WillMoveToSuperview (newsuper);

            base.Button.TouchUpInside -= HandleTouchUpInside;
            base.Button.TouchUpInside += HandleTouchUpInside;
        }

        void HandleTouchUpInside (object sender, EventArgs e)
        {
            var controller = this.FindViewController();
            if(controller == null) return;
            controller.View.EndEditing(true);
            if (_rootElement != null) {
                var newDvc = new DialogViewController (_rootElement, true) {
                    Autorotate = true

                };
                newDvc.View.BackgroundColor  = UIColor.FromRGB (230,230,230);
                newDvc.TableView.BackgroundColor = UIColor.FromRGB (230,230,230);
                newDvc.TableView.BackgroundView = new UIView{ BackgroundColor =  UIColor.FromPatternImage (UIImage.FromFile ("Assets/background.png")) }; 
                controller.NavigationItem.BackBarButtonItem = new UIBarButtonItem(Resources.GetValue("BackButton"), UIBarButtonItemStyle.Bordered, null, null);
                controller.NavigationController.PushViewController(newDvc, true);
                newDvc.Title = new CultureInfo("en-US",false).TextInfo.ToTitleCase ( _rootElement[0].Caption ); 
            }
        }

        public void Configure<T>(string title, ListItem<T>[] values, Nullable<T> selectedId, Action<ListItem<T>> onItemSelected) where T: struct
        {
            int selected = 0;
            var section = new SectionWithBackground(title);
            var resources = TinyIoCContainer.Current.Resolve<IAppResource>();
            foreach (var v in values)
            {
                // Keep a reference to value in order for callbacks to work correctly
                var value = v;
                var display = value.Display;
                if(!value.Id.HasValue)
                {
                    display = resources.GetString("NoPreference");
                }
                var item = new RadioElementWithId<T>(value.Id, display, value.Image);
                item.Tapped += ()=> {
                    onItemSelected(value);
                    var controller = this.FindViewController();
                    if(controller != null) controller.NavigationController.PopViewControllerAnimated(true);
                };
                section.Add(item);
                if (selectedId.Equals(value.Id))
                {
                    selected = Array.IndexOf(values, value);
                }
            }
            
            _rootElement = new CustomRootElement(title, new RadioGroup(selected));
            _rootElement.Add(section);
        }
    }   
}

