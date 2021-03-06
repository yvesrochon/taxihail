using System;
using Cirrious.MvvmCross.Binding.BindingContext;
using UIKit;
using apcurium.MK.Booking.Mobile.ViewModels;
using apcurium.MK.Booking.Mobile.Client.Localization;
using Cirrious.MvvmCross.Binding.Touch.Views;
using apcurium.MK.Booking.Mobile.Client.Order;
using System.Windows.Input;

namespace apcurium.MK.Booking.Mobile.Client.Views
{
	public partial class RideSummaryView  : BaseViewController<RideSummaryViewModel>
	{     
        private MvxActionBasedTableViewSource _source;

		public RideSummaryView() : base("RideSummaryView", null)
		{
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

            NavigationController.NavigationBar.Hidden = false;
            NavigationItem.HidesBackButton = true;
            NavigationItem.Title = Localize.GetValue("RideSummaryTitleText");

            ChangeThemeOfBarStyle();
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

            View.BackgroundColor = UIColor.FromRGB(242, 242, 242);

            lblSubTitle.Text = String.Format(Localize.GetValue ("RideSummarySubTitleText"), this.Services().Settings.TaxiHail.ApplicationName);

            PrepareTableView();

            InitializeBindings();
		}

        public override void ViewWillDisappear(bool animated)
        {
            if (IsMovingFromParentViewController)
            {
                // Back button pressed
                ViewModel.PrepareNewOrder.ExecuteIfPossible();
            }

            base.ViewWillDisappear(animated);
        }

        private void PrepareTableView()
        {
            _source = new MvxActionBasedTableViewSource(
                tableRatingList,
                UITableViewCellStyle.Default,
                BookRatingCell.Identifier,
                BookRatingCell.BindingText,
                UITableViewCellAccessory.None);

            _source.CellCreator = (tableView, indexPath, item) =>
            {
                var cell = BookRatingCell.LoadFromNib(tableView);
                cell.RemoveDelay();
                return cell;
            };

            tableRatingList.Source = _source;
        }

        private void InitializeBindings()
        {
            ViewModel.PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == "RatingList")
                {
                    ResizeTableView();
                }
            };

            var set = this.CreateBindingSet<RideSummaryView, RideSummaryViewModel> ();

            NavigationItem.RightBarButtonItem = new UIBarButtonItem(Localize.GetValue("Done"), UIBarButtonItemStyle.Bordered, (o, e) => 
            {  
					ViewModel.RateOrderAndNavigateToHome.ExecuteIfPossible();
            });

            set.Bind(_source)
                .For(v => v.ItemsSource)
                .To(vm => vm.RatingList);

            set.Apply ();
        }

        private void ResizeTableView()
        {
            if (ViewModel.RatingList != null)
            {
                constraintRatingTableHeight.Constant = BookRatingCell.Height * ViewModel.RatingList.Count;
            }
            else
            {
                constraintRatingTableHeight.Constant = 0;
            }
        }
	}
}

