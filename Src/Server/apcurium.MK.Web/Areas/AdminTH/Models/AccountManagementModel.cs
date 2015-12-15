﻿using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace apcurium.MK.Web.Areas.AdminTH.Models
{
	public class AccountManagementModel
	{
		public AccountManagementModel()
		{
		}

		[Display(Name = "Id")]
		public Guid Id { get; set; }

		[Display(Name = "Name")]
		public string Name { get; set; }

		[Display(Name = "Email")]
		public string Email { get; set; }

		[Display(Name = "Custumer Number")]
		public string CustomerNumber { get; set; }

		[Display(Name = "Creation Date")]
		public DateTime CreationDate { get; set; }

		[Display(Name = "Facebook Account")]
		public string FacebookAccount { get; set; }

		[Display(Name = "IBS Account ID")]
		public int? IBSAccountId { get; set; }

		[Display(Name = "Confirmed")]
		public bool IsConfirmed { get; set; }

		[Display(Name = "Enabled")]
		public bool IsEnabled { get; set; }

		[Display(Name = "Country Code")]
		public CountryISOCode CountryCode { get; set; }

		[Display(Name = "Phone Number")]
		public string PhoneNumber { get; set; }

		[Display(Name = "Charge Type")]
		public string ChargeType { get; set; }

		[Display(Name = "Default Tip Percent")]
		public int? DefaultTipPercent { get; set; }

		[Display(Name = "PayPal Account Linked")]
		public bool IsPayPalAccountLinked { get; set; }

		[Display(Name = "Credit card last 4 digits")]
		public string CreditCardLast4Digits { get; set; }

		public List<SelectListItem> CountryCodesList { get; set; }

		public List<Order> OrderDetailList { get; set; }
	}

	public static class OrderExtensions
	{
		public static double? TotalAmount(this Order order)
		{
			return order.Fare + order.Tax + order.Toll + order.Tip + order.Surcharge;
		}

		public static string CreatedDateText(this Order order)
		{
			string txt;
			CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			txt = order.CreatedDate.ToShortDateString();
			Thread.CurrentThread.CurrentCulture = originalCulture;
			return txt;
		}

		public static string CreatedTimeText(this Order order)
		{
			string txt;
			CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			txt = order.CreatedDate.ToShortTimeString();
			Thread.CurrentThread.CurrentCulture = originalCulture;
			return txt;
		}

		public static string PickupDateText(this Order order)
		{
			string txt;
			CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			txt = order.PickupDate.ToShortDateString();
			Thread.CurrentThread.CurrentCulture = originalCulture;
			return txt;
		}

		public static string PickupTimeText(this Order order)
		{
			string txt;
			CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			txt = order.PickupDate.ToShortTimeString();
			Thread.CurrentThread.CurrentCulture = originalCulture;
			return txt;
		}

		public static string StatusText(this Order order)
		{
			return order.Status.ToString();
		}
	}
}