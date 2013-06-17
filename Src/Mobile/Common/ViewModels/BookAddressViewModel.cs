using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using apcurium.MK.Booking.Api.Contract.Resources;
using Cirrious.MvvmCross.Commands;
using Cirrious.MvvmCross.Interfaces.Commands;
using System.Threading.Tasks;
using TinyIoC;
using apcurium.MK.Booking.Mobile.Messages;
using apcurium.MK.Common.Extensions;
using System.Threading;
using apcurium.MK.Booking.Mobile.AppServices;
using apcurium.MK.Common.Entity;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Common.Diagnostic;
using Cirrious.MvvmCross.Interfaces.ServiceProvider;
using Cirrious.MvvmCross.ExtensionMethods;
using apcurium.MK.Booking.Mobile.Extensions;
using ServiceStack.Text;
using apcurium.MK.Common;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
    public class BookAddressViewModel : BaseViewModel
       {
        private CancellationTokenSource _cancellationToken;
        private bool _isExecuting;
        private Func<Address> _getAddress;
        private Action<Address> _setAddress;
        private string _id;
        private string _searchingTitle;

        public event EventHandler AddressChanged;

        public event EventHandler AddressCleared;

        public BookAddressViewModel(Func<Address> getAddress, Action<Address> setAddress)
        {
            _getAddress = getAddress;
            _setAddress = setAddress;
            _id = Guid.NewGuid().ToString();
            _searchingTitle = Resources.GetString("AddressSearchingText");
            MessengerHub.Subscribe<AddressSelected>(OnAddressSelected, selected => selected.OwnerId == _id);
        }

        public string AddressLine2
        {
            get
            {
                if (IsExecuting)
                {
                    return "";
                }
                else
                {
                    var addressDisplay = "";
                    var adr = _getAddress();
                    if (adr != null)
                    {
                        if ((adr.AddressType == "place") || (Params.Get(adr.City, adr.State, adr.ZipCode).Count(s => s.HasValue()) == 0))
                        {
                            addressDisplay =  adr.FullAddress;
                        }
                        else
                        {
                            addressDisplay =  Params.Get(adr.City, adr.State, adr.ZipCode).Where(s => s.HasValue()).JoinBy(", ");
                        }
                    }
                    return addressDisplay;


                }
            }
        }

        public string EmptyAddressPlaceholder { get; set; }

        public string AddressLine1
        {
            get
            {
                FirePropertyChanged(() => IsPlaceHolder);
                if (IsExecuting)
                {
                    return _searchingTitle;
                }
                if (GetAddress().HasValue())
                {
                    return GetAddress();
                }
                else
                {
                    return EmptyAddressPlaceholder;
                }
            }
        }

        private string GetAddress()
        {
            var adr = _getAddress();
            if (adr == null)
            {
                return "";
            }
            if (adr.BuildingName.HasValue())
            {
                return Params.Get(adr.BuildingName, adr.Street).Where(s => s.HasValue() && s.Trim().HasValue()).JoinBy(", ");
            }
            if (Params.Get(adr.StreetNumber, adr.Street).Any(s => s.HasValue() && s.Trim().HasValue()))
            {
                return Params.Get(adr.StreetNumber, adr.Street).Where(s => s.HasValue() && s.Trim().HasValue()).JoinBy(" ");
            }
            return adr.FullAddress;
        }



        public bool IsPlaceHolder
        {
            get
            {
                return Model.FullAddress.IsNullOrEmpty();
            }
        }

        public Address Model { get { return _getAddress(); } set { _setAddress(value); } }

        public IMvxCommand SearchCommand
        {
            get
            {
                return GetCommand<Address>(coordinate =>
                {
                    CancelCurrentLocation();

                    _cancellationToken = new CancellationTokenSource();

                    var token = _cancellationToken.Token;
                    var task = Task.Factory.SafeStartNew(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            IsExecuting = true;
                            var accountAddress = AccountService.FindInAccountAddresses(coordinate.Latitude, coordinate.Longitude);
                            if (accountAddress != null)
                            {
                                return new Address[] { accountAddress};
                            }
                            else
                            {

                                return GeolocService.SearchAddress(coordinate.Latitude, coordinate.Longitude).ToArray();
                            }
                        }
                        return null;

                    }, token);

                    task.ContinueWith(t =>
                    { 
                        InvokeOnMainThread(() => {
                            if (t.Result != null && t.Result.Any())
                            {
                                var address = t.Result[0];                                
                                // Replace result coordinates  by search coordinates (= user position)
                                address.Latitude = coordinate.Latitude;
                                address.Longitude = coordinate.Longitude;
                                SetAddress(address, true);
                            }
                            else
                            {
                                Logger.LogMessage("No address found for coordinate : La : {0} , Lg: {1} ", coordinate.Latitude, coordinate.Longitude);
                                ClearAddress();
                            }
                        });
                        
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

                });
            }
        }

        public IMvxCommand PickAddress
        {
            get
            {
                return GetCommand(() =>
                {
                    CancelCurrentLocation();
                    if (Settings.StreetNumberScreenEnabled 
                        && Model.BookAddress.HasValue())
                    {
                        RequestNavigate<BookStreetNumberViewModel>(new { address = JsonSerializer.SerializeToString<Address>(Model), ownerId = _id });
                    }
                    else
                    {
                        RequestNavigate<AddressSearchViewModel>(new { search = Model.FullAddress, ownerId = _id, places = "false" });
                    }

                });
            }
        }

        private void OnAddressSelected(AddressSelected selected)
        {
            if ( selected.Content != null )
            {
                SetAddress(selected.Content, true);
            }
            else
            {
                ClearAddress ();
            }

        }

        public bool IsExecuting
        {
            get { return _isExecuting; }
            set
            {
                _isExecuting = value;
                FirePropertyChanged(() => IsExecuting);
                FirePropertyChanged(() => AddressLine1);
                FirePropertyChanged(() => AddressLine2);
            }
        }

        public IMvxCommand CancelCurrentLocationCommand
        {
            get
            {
                return new MvxRelayCommand<string>(_notUsed => {
                    CancelCurrentLocation();
                });
            }
        }

        private void CancelCurrentLocation()
        {
            if ((_cancellationToken != null) && (_cancellationToken.Token.CanBeCanceled))
            {
                _cancellationToken.Cancel();
                _cancellationToken = null;
            }
            IsExecuting = false;
        }

        public void SetAddress(Address address, bool userInitiated)
        {
            InvokeOnMainThread(() =>
            {
                IsExecuting = true;
                try
                {

                    if (IsExecuting)
                    {
                        CancelCurrentLocation();
                    }

                    if ( ( address.Street.IsNullOrEmpty() ) && (address.ZipCode.IsNullOrEmpty () ) && (address.AddressType != "place")  && (address.AddressType != "popular")) // This should only be true when using an address from a version smaller than 1.3                    
                    {
                        var a = GeolocService.SearchAddress(address.FullAddress, null , null );
                        if ( a.Count() > 0 )
                        {
                            address = a.First();
                        }
                    }

                    address.CopyTo(Model);

                    FirePropertyChanged(() => AddressLine1);
                    FirePropertyChanged(() => AddressLine2);
                    FirePropertyChanged(() => Model);


                    if (AddressChanged != null)
                    {
                        AddressChanged(userInitiated, EventArgs.Empty);
                    }

                }
                finally
                {


                    IsExecuting = false;
                }

            });
        }

        public void ClearAddress()
        {
            InvokeOnMainThread(() =>
            {
                var clearAddress = new Address();
                clearAddress.CopyTo(Model);
                FirePropertyChanged(() => AddressLine1);
                FirePropertyChanged(() => AddressLine2);
                FirePropertyChanged(() => Model);
                IsExecuting = false;
                if (AddressChanged != null)
                {
                    AddressChanged(true, EventArgs.Empty);
                }

                if (AddressCleared != null)
                {
                    AddressCleared(this, EventArgs.Empty);
                }
            });
        }

        public IMvxCommand ClearPositionCommand
        {
            get
            {

                return GetCommand(ClearAddress);
            }
        }

        public IMvxCommand RequestCurrentLocationCommand {
            get {
                return GetCommand(() =>
                {

                    CancelCurrentLocationCommand.Execute ();

                    if ( !LocationService.IsLocationServicesEnabled )
                    {
                        TinyIoCContainer.Current.Resolve<IMessageService>().ShowMessage ( TinyIoCContainer.Current.Resolve<IAppResource>().GetString ("LocationServiceErrorTitle"),TinyIoCContainer.Current.Resolve<IAppResource>().GetString ("LocationServiceErrorMessage") );
                        return ;
                    }

                    IsExecuting = true;
                    bool positionSet = false;

                    if(LocationService.BestPosition != null)
                    {                        
                        InvokeOnMainThread(()=>SearchAddressForCoordinate(LocationService.BestPosition ));
                        return;
                    }

                    LocationService.GetNextPosition(TimeSpan.FromSeconds(6), 50).Subscribe(
                    pos=>
                    {
                        positionSet =true;
                        InvokeOnMainThread(()=>SearchAddressForCoordinate(pos));
                    },
                    ()=>
                    {  
                        if(!positionSet)
                        {
                            InvokeOnMainThread(()=>SearchAddressForCoordinate(LocationService.BestPosition ?? new Position(){Latitude = 60,Longitude = 60}));
                        }
                    });
                });
            }

        }

        private void SearchAddressForCoordinate(Position p)
        {
            IsExecuting = true;
            Logger.LogMessage("Start Call SearchAddress : " + p.Latitude.ToString() + ", " + p.Longitude.ToString());

            var accountAddress = AccountService.FindInAccountAddresses(p.Latitude, p.Longitude);
            if (accountAddress != null)
            {
                Logger.LogMessage("Address found in account");
                SetAddress(accountAddress, false);
            }
            else
            {
                var address = GeolocService.SearchAddress(p.Latitude, p.Longitude,false);
                Logger.LogMessage("Call SearchAddress finsihed, found {0} addresses", address.Count());
                if (address.Count() > 0)
                {
                    Logger.LogMessage(" found {0} addresses", address.Count());
                    SetAddress(address[0], false);
                }
                else
                {
                    Logger.LogMessage(" clear addresses");
                    ClearAddress();
                }
                Logger.LogMessage("Exiting SearchAddress thread");
            }

            IsExecuting = false;
        }

    }
}
