using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyMessenger;

namespace apcurium.MK.Booking.Mobile.Messages
{
	public class LogOutRequested : TinyMessageBase
	{
		public LogOutRequested(object sender)
			: base(sender)
		{
		}
		
	}
}