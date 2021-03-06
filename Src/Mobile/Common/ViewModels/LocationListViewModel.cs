using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
	public class LocationListViewModel: PageViewModel
    {
		private readonly IAccountService _accountService;

		public LocationListViewModel(IAccountService accountService)
		{
			_accountService = accountService;
		}

        public override void OnViewStarted(bool firstStart = false)
        {
            base.OnViewStarted (firstStart);
            LoadAllAddresses();
        }

        private ObservableCollection<AddressViewModel> _allAddresses = new ObservableCollection<AddressViewModel>();
        public ObservableCollection<AddressViewModel> AllAddresses 
		{ 
			get { return _allAddresses; }
            set 
			{
                if(value != _allAddresses) 
				{
                    _allAddresses = value;
					RaisePropertyChanged();
                }
            }
        }

		public ICommand NavigateToLocationDetailPage
        {
            get
			{
                return this.GetCommand<AddressViewModel>(a =>
                {
                    if(a.Address.Id == Guid.Empty)
                    {
                        // New address
                        ShowViewModel<LocationDetailViewModel>();
                    } 
					else 
					{
                        ShowViewModel<LocationDetailViewModel>(new Dictionary<string,string>{
                            { "address", a.Address.ToJson() }
                        });
                    }
                });
            }
        }

        public Task LoadAllAddresses ()
        {
			using (this.Services().Message.ShowProgress())
			{
				var tasks = new [] 
				{
					LoadFavoriteAddresses(),
					LoadHistoryAddresses()
				};

				return Task.Factory.ContinueWhenAll(tasks, t => 
				{
					AllAddresses.Clear ();
					AllAddresses.Add (new AddressViewModel{ Address =  new Address
						{
							FriendlyName = this.Services().Localize["LocationAddFavoriteTitle"],
							FullAddress = this.Services().Localize["LocationAddFavoriteSubtitle"],
						}, IsAddNew = true, ShowPlusSign=true});

					if(t[0].Status == TaskStatus.RanToCompletion)
					{
						AllAddresses.AddRange(t[0].Result);
					}

					if(t[1].Status == TaskStatus.RanToCompletion)
					{
						AllAddresses.AddRange(t[1].Result);
					}

					AllAddresses.ForEach ( a=> 
					{
						a.IsFirst = a.Equals(AllAddresses.First());
						a.IsLast = a.Equals(AllAddresses.Last());                    
					});

					RaisePropertyChanged( () =>AllAddresses );

				}, new CancellationTokenSource().Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
        }
        
		private async Task<AddressViewModel[]> LoadFavoriteAddresses()
        {
			try
			{
				var adrs = await _accountService.GetFavoriteAddresses();
                return adrs.Select(a => new AddressViewModel
                { 
                    Address = a,
                    IsAddNew =  a.Id.IsNullOrEmpty(),
                    ShowPlusSign = a.Id.IsNullOrEmpty(),
                    ShowRightArrow = !a.Id.IsNullOrEmpty(),
					Type = AddressType.Favorites
                }).ToArray();
			}
			catch(Exception e)
			{
				Logger.LogError(e);
				return new AddressViewModel[0];
			}

        }

		private async Task<AddressViewModel[]> LoadHistoryAddresses()
        {
			try
			{
				var adrs = await _accountService.GetHistoryAddresses();
                return adrs.Select(a => new AddressViewModel
                { 
                    Address = a,
                    ShowPlusSign = a.Id.IsNullOrEmpty(),
                    ShowRightArrow = !a.Id.IsNullOrEmpty(),
					Type = AddressType.History
                }).ToArray();
			}
			catch(Exception e)
			{
				Logger.LogError(e);
				return new AddressViewModel[0];
			}
        }
    }
}

