// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace apcurium.MK.Booking.Mobile.Client
{
	[Register ("AddressSearchView")]
	partial class AddressSearchView
	{
		[Outlet]
		MonoTouch.UIKit.UITableView AddressListView { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton CancelButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField SearchTextField { get; set; }

		[Outlet]
		apcurium.MK.Booking.Mobile.Client.SegmentedButtonBar TopBar { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (AddressListView != null) {
				AddressListView.Dispose ();
				AddressListView = null;
			}

			if (CancelButton != null) {
				CancelButton.Dispose ();
				CancelButton = null;
			}

			if (SearchTextField != null) {
				SearchTextField.Dispose ();
				SearchTextField = null;
			}

			if (TopBar != null) {
				TopBar.Dispose ();
				TopBar = null;
			}
		}
	}
}
