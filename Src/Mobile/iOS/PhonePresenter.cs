using System.Collections.Generic;
using apcurium.MK.Booking.Mobile.AppServices;
using Cirrious.CrossCore;
using Cirrious.MvvmCross.Touch.Views.Presenters;
using Cirrious.MvvmCross.ViewModels;
using UIKit;
using apcurium.MK.Booking.Mobile.PresentationHints;
using apcurium.MK.Booking.Mobile.ViewModels;

namespace apcurium.MK.Booking.Mobile.Client
{
    public class PhonePresenter: MvxModalSupportTouchViewPresenter
    {
		public PhonePresenter(UIApplicationDelegate applicationDelegate, UIWindow window)
            : base(applicationDelegate, window)
        {
        }

        protected override UINavigationController CreateNavigationController(UIViewController viewController)
        {
            var navController = new UINavigationController(viewController);
            Mvx.RegisterSingleton<UINavigationController>(navController);
            return navController;
        }

        public override void Show(MvxViewModelRequest request)
        {
			if(request.ParameterValues != null
                && request.ParameterValues.ContainsKey("clearHistoryExceptFirstElement"))
			{
				ClearNavigationStackExceptFirstElement();
				return;
			}

			if (request.ParameterValues != null
			   && request.ParameterValues.ContainsKey("clearNavigationStack"))
			{
                ClearNavigationStack();
			}

            base.Show(request);
			if(request.ParameterValues != null 
				&& request.ParameterValues.ContainsKey("removeFromHistory"))
			{
				RemovePreviousViewFromHistory();
			}
        }

        public override void ChangePresentation(MvxPresentationHint hint)
        {
            if (hint is ChangePresentationHint)
            {
                TryChangeViewPresentation((ChangePresentationHint)hint);
            }
            else
            {
                base.ChangePresentation(hint);
            }
        }

	    public override void Close(IMvxViewModel viewModel)
	    {
			base.Close(viewModel);

			if (viewModel is TutorialViewModel)
			{
				var tutorialService = Mvx.Resolve<ITutorialService>();

				tutorialService.NotifyTutorialEnded();
			}
	    }

	    private void ClearNavigationStack()
        {
            var navController = Mvx.Resolve<UINavigationController>();

            navController.ViewControllers = new UIViewController[0];
        }

		private void ClearNavigationStackExceptFirstElement()
		{
			var navController = Mvx.Resolve<UINavigationController>();

			var controllers = navController.ViewControllers;
			if (controllers.Length > 1)
			{
				navController.ViewControllers = new UIViewController[] {controllers[0]};
			}
		}

        private void RemovePreviousViewFromHistory()
        { 
            var navController = Mvx.Resolve<UINavigationController>();

            var controllers = navController.ViewControllers;
            if (controllers.Length > 1)
            {
                var listOfControllers = new List<UIViewController>(controllers);
                listOfControllers.RemoveAt(listOfControllers.Count - 2);
                navController.ViewControllers = listOfControllers.ToArray();
            }
            else
            {
                Mvx.Warning("Can't remove previous view, not enough UIViewControllers in the stack");
            }
        }

        private void TryChangeViewPresentation(ChangePresentationHint hint)
        {
            var view = CurrentTopViewController as IChangePresentation;
            if (view != null)
            {
                view.ChangePresentation(hint);
                foreach (var subview in CurrentTopViewController.View.FindSubviewsOfType<IChangePresentation>())
                {
                    subview.ChangePresentation(hint);
                }
            }
            else
            {
                Mvx.Warning("Can't change presentation, view controller doesn't support IChangePresentation");
            }
        }
    }
}


