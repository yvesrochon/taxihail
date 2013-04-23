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

namespace apcurium.MK.Booking.Mobile.ViewModels
{
    public class PaymentViewModel : BaseSubViewModel<object>
    {

        public PaymentViewModel (string order, string orderStatus, string messageId) : base(messageId)
        {

			Order = JsonSerializer.DeserializeFromString<Order>(order); 
			OrderStatus = orderStatus.FromJson<OrderStatusDetail>();  

            var account = AccountService.CurrentAccount;
			var paymentInformation = new PaymentInformation 
			{
                CreditCardId = account.DefaultCreditCard,
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
			
			try
			{
				return VehicleClient.SendMessageToDriver(OrderStatus.VehicleNumber,Str.GetPaymentConfirmationMessageToDriver(Amount));
			}
			catch(Exception){
			}
			
			return false;
		}

		public void ShowConfirmation()
		{
			MessageService.ShowMessage (Str.CmtTransactionSuccessTitle, Str.CmtTransactionSuccessMessage,
			                            Str.CmtTransactionResendConfirmationButtonText, ()=>
			{				
				ConfirmPaymentForDriver();
				ShowConfirmation();
			},
			Str.OkButtonText, ()=>{
                ReturnResult("");
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
						MessageService.ShowProgress(false);
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.NoCreditCardSelectedMessage);
						return;
					}
					if(AmountDouble <= 0)
					{
						MessageService.ShowProgress(false);
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.NoAmountSelectedMessage);
						return;
					}

                    MessageService.ShowProgress (true);

					if(!Order.IBSOrderId.HasValue)
					{
						MessageService.ShowProgress(false);
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.NoOrderId);
					}

					var transactionId = PaymentClient.PreAuthorize(PaymentPreferences.SelectedCreditCard.Token,AmountDouble, Order.IBSOrderId.Value +"");

					if(transactionId <= 0)
					{
						MessageService.ShowProgress(false);
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.CmtTransactionErrorMessage);
					}
					else if(!ConfirmPaymentForDriver())
					{
						MessageService.ShowProgress(false);
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.TaxiServerDownMessage);
					}
					else if(!PaymentClient.CommitPreAuthorized(transactionId, Order.IBSOrderId.Value+""))
					{
						MessageService.ShowProgress(false);
						MessageService.ShowMessage (Str.ErrorCreatingOrderTitle, Str.SendMessageErrorMessage);
					}
					else
					{
						MessageService.ShowProgress(false);
						ShowConfirmation();					          
					}

                }); 
                
            }
        }

    }
}

