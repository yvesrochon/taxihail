using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Data;
using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Common.Entity;
using ServiceStack.Common.Utils;

namespace apcurium.MK.Booking.Mobile.ViewModels.Payment
{
	public class PaymentDetailsViewModel : BaseViewModel
	{
		private readonly IAccountService _accountService;
        private int _defaultTipPercentage;

		public PaymentDetailsViewModel(IAccountService accountService)
		{
			_accountService = accountService;
		}

		public async Task Start(PaymentInformation paymentDetails = null)
		{
			_defaultTipPercentage = Settings.DefaultTipPercentage;

			Tips = new[]
			{ 
				new ListItem { Id = 0,  Display = "0%" }, 
				new ListItem { Id = 5,  Display = "5%" }, 
				new ListItem { Id = 10, Display = "10%" }, 
				new ListItem { Id = 15, Display = "15%" }, 
				new ListItem { Id = 18, Display = "18%" }, 
				new ListItem { Id = 20, Display = "20%" },
				new ListItem { Id = 25, Display = "25%" }
			};

			if (paymentDetails == null)
			{
				paymentDetails = new PaymentInformation();
			}

		    try
		    {
                SelectedCreditCard = await _accountService.GetCreditCard();
		    }
		    catch (Exception ex)
		    {
                Logger.LogMessage(ex.Message, ex.ToString());
                this.Services().Message.ShowMessage(this.Services().Localize["Error"], this.Services().Localize["PaymentLoadError"]);
		    }
			
			if (SelectedCreditCard != null)
			{
				paymentDetails.CreditCardId = SelectedCreditCard.CreditCardId;
			}

			var currentAccount = _accountService.CurrentAccount;
			if (!paymentDetails.TipPercent.HasValue)
			{
				if (currentAccount.DefaultTipPercent.HasValue)
				{
					paymentDetails.TipPercent = currentAccount.DefaultTipPercent;
				}
				else
				{
					paymentDetails.TipPercent = _defaultTipPercentage;
				}
			}

			Tip = paymentDetails.TipPercent.Value;
		}
    
		private CreditCardDetails _selectedCreditCard;
		public CreditCardDetails SelectedCreditCard 
		{
			get { return _selectedCreditCard; }
			set
			{
				_selectedCreditCard = value;
				RaisePropertyChanged ();
				RaisePropertyChanged (() => SelectedCreditCardId);
				RaisePropertyChanged (() => HasCreditCard);
			}
		}

		public Guid SelectedCreditCardId 
		{
			get 
			{ 
				return SelectedCreditCard != null 
					? SelectedCreditCard.CreditCardId 
					: Guid.Empty; 
			}
		}

		public ListItem[] Tips { get; set; }

        public string CurrencySymbol 
		{
            get 
			{
				var culture = new CultureInfo(Settings.PriceFormat);
                return culture.NumberFormat.CurrencySymbol;
            }
        }

        public bool HasCreditCard
		{
            get 
			{
				return SelectedCreditCard != null;
            }
        }

	    public bool IsPayPalAccountLinked
	    {
	        get { return _accountService.CurrentAccount.IsPayPalAccountLinked; }
	    }

		private int _tip;
        public int Tip 
        { 
            get
            {
                return _tip;
            }
            set
			{
                _tip = value;
				RaisePropertyChanged();
				RaisePropertyChanged(() => TipAmount);
				RaisePropertyChanged(() => TipAmountDisplay);
            }
        }

		public string TipAmount
		{
			get
			{
				return $"{Tip}%";
			}
		}

        public bool TipListDisabled = false;

        public string TipAmountDisplay
        {
            get
            {
				return TipListDisabled ? "" : $"{Tip}%";
            }
        }
    }
}
