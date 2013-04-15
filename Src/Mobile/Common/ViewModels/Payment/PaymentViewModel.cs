using System;
using apcurium.MK.Booking.Mobile.AppServices;
using ServiceStack.Text;
using Cirrious.MvvmCross.ExtensionMethods;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Mobile.ViewModels.Payment;
using Cirrious.MvvmCross.Interfaces.ServiceProvider;
using Cirrious.MvvmCross.Interfaces.Commands;
using apcurium.MK.Booking.Mobile.Messages;
using apcurium.MK.Booking.Api.Contract.Resources;
using TinyIoC;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Common.Extensions;
using Cirrious.MvvmCross.Android.Views;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
    public class PaymentViewModel : BaseViewModel
    {

		public PaymentViewModel (string order, string orderStatus)
        {

			Order = JsonSerializer.DeserializeFromString<Order>(order); 
			OrderStatus = orderStatus.FromJson<OrderStatusDetail>();  

            var account = AccountService.CurrentAccount;
			var paymentInformation = new PaymentInformation 
			{
                CreditCardId = account.DefaultCreditCard,
                TipAmount = account.DefaultTipAmount,
                TipPercent = account.DefaultTipPercent,
            };
            PaymentPreferences = new PaymentDetailsViewModel(Guid.NewGuid().ToString(), paymentInformation);

        }

		Order Order { get; set; }
		OrderStatusDetail OrderStatus {get; set;}

		private double AmountDouble { get{ return Amount.FromDollars(); }}
		public string Amount { get; set;}

        public PaymentDetailsViewModel PaymentPreferences {
            get;
            private set;
        }

		public bool ConfirmPaymentForDriver(){
			
			return VehicleClient.SendMessageToDriver(OrderStatus.VehicleNumber,Str.GetPaymentConfirmationMessageToDriver(Amount));
		}

		public void ShowConfirmation()
		{
			MessageService.ShowMessage (Str.CmtTransactionSuccessTitle, Str.CmtTransactionSuccessMessage,
			                            Str.CmtTransactionResendConfirmationButtonText, ()=>
			{						
				ShowConfirmation();
			},
			Str.OkButtonText, ()=>{
				RequestClose(this);         
			});
		}

        public IMvxCommand ConfirmOrderCommand
        {
            get
            {
                
                return GetCommand(() => 
                {                    
					if(PaymentPreferences.SelectedCreditCard == null)
					{
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.NoCreditCardSelectedMessage);
						MessageService.ShowProgress(false);
						return;
					}
					if(AmountDouble <= 0)
					{
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.NoAmountSelectedMessage);
						MessageService.ShowProgress(false);
						return;
					}

                    MessageService.ShowProgress (true);

					var transactionId = PaymentClient.PreAuthorize(PaymentPreferences.SelectedCreditCard.Token,AmountDouble);

					if(!ConfirmPaymentForDriver())
					{
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.TaxiServerDownMessage);
						MessageService.ShowProgress(false);
						return;
					}
					
					if(transactionId <= 0 || !PaymentClient.CommitPreAuthorized(transactionId))
					{
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.CmtTransactionErrorMessage);
						MessageService.ShowProgress(false);
						return;
					}

					ConfirmPaymentForDriver();

					MessageService.ShowProgress(false);
					ShowConfirmation();
					          

                }); 
                
            }
        }

    }
}

