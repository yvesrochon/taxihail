using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cirrious.MvvmCross.ExtensionMethods;
using Cirrious.MvvmCross.Interfaces.Commands;
using Cirrious.MvvmCross.Interfaces.ServiceProvider;
using TinyIoC;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
    public class CreditCardsListViewModel : BaseViewModel,
        IMvxServiceConsumer<IAccountService>
    {

        private ObservableCollection<CreditCardViewModel> _creditCards;

        public ObservableCollection<CreditCardViewModel> CreditCards
        {
            get { return _creditCards; }
            set { this._creditCards = value; FirePropertyChanged("CreditCards"); }
        }

        private bool _hasCards;
        public bool HasCards
        {
            get
            {
                return _hasCards;
            }
            set
            {
                if (value != _hasCards)
                {
                    _hasCards = value;
                    FirePropertyChanged("HasCards");
                }
            }
        }

        public CreditCardsListViewModel ()
        {
            var accountService = this.GetService<IAccountService>();
            LoadCreditCards();
           // PaymentList = new ObservableCollection<CreditCardDetails>(accountService.GetMyPaymentList());
        }

        public Task LoadCreditCards()
        {
            return Task.Factory.StartNew(() =>
            {
                var creditCards = TinyIoCContainer.Current.Resolve<IAccountService>().GetMyPaymentList().ToList();
                creditCards.Add(new CreditCardDetails
                {
                    FriendlyName = Resources.GetString("LocationAddFavoriteTitle"),
                });
                CreditCards = new ObservableCollection<CreditCardViewModel>(creditCards.Select(x => new CreditCardViewModel()
                {
                    FriendlyName = x.FriendlyName,
                    AccountId = x.AccountId,
                    CreditCardCompany = x.CreditCardCompany,
                    CreditCardId = x.CreditCardId,
                    Last4Digits = x.Last4Digits,
                    Token = x.Token,
                    IsAddNew = x.CreditCardId.IsNullOrEmpty(),
                    ShowPlusSign = x.CreditCardId.IsNullOrEmpty(),
                    IsFirst = x.Equals(creditCards.First()),
                    IsLast = x.Equals(creditCards.Last())
                }));
                HasCards = CreditCards.Any();
            });
        }

        public IMvxCommand NavigateToAddOrSelect
        {
            get
            {
                return GetCommand<CreditCardViewModel>(creditCard =>
                                                           {
                                                               if (creditCard.IsAddNew)
                                                               {
                                                                   this.NavigateToAddCreditCard.Execute();
                                                               }
                                                               else
                                                               {
                                                                   this.SelectCreditCardAndBackToSettings.Execute(creditCard.CreditCardId);
                                                               }
                                                           });
            }
        }

        public IMvxCommand NavigateToAddCreditCard
        {
            get
            {
                return GetCommand(() => RequestNavigate<CreditCardAddViewModel>());
            }
        }

        public IMvxCommand SelectCreditCardAndBackToSettings
        {
            get
            {
                return GetCommand<Guid>(creditCardId => RequestNavigate<RideSettingsViewModel>());
            }
        }
    }
}

