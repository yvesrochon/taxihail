using System;
using System.Linq;

using apcurium.MK.Booking.Mobile.ViewModels;
using apcurium.MK.Common.Entity;
using ServiceStack.Text;
using apcurium.Framework.Extensions;
using Cirrious.MvvmCross.Interfaces.Commands;
using Cirrious.MvvmCross.Commands;
using apcurium.MK.Booking.Mobile.Messages;
using apcurium.MK.Common;
using TinyMessenger;

namespace apcurium.MK.Booking.Mobile
{
    public class BookStreetNumberViewModel : BaseViewModel
    {
        private string _ownerId;
        private TinyMessageSubscriptionToken _token;
        public BookStreetNumberViewModel (string ownerId, string address)
        {
            _ownerId = ownerId;
            if (address != null) {
                Model = JsonSerializer.DeserializeFromString<Address>(address);
                if(Model.StreetNumber.HasValue())
                {
                    _streetNumberOrBuildingName = Model.StreetNumber;
                }

                if(Model.BuildingName.HasValue())
                {
                    _streetNumberOrBuildingName = Model.BuildingName;
                }
                FirePropertyChanged(() => StreetNumberOrBuildingName);
            }
            _token = MessengerHub.Subscribe<AddressSelected> (OnAddressSelected, selected => selected.OwnerId == _ownerId);
        }

        string _streetNumberOrBuildingName;
        public string StreetNumberOrBuildingName {
            get {
                return _streetNumberOrBuildingName;
            }
            set {
                _streetNumberOrBuildingName = value;
                FirePropertyChanged (() => StreetNumberOrBuildingName);
            }
        }

        Address _model;
        public Address Model {
            get {
                return _model;
            }
            set {
                _model = value;
            }
        }

        public string StreetCity
        {
            get{
                return Params.Get(  _model.Street, _model.City ).Where( s=>s.HasValue() ).JoinBy( ", " );
            }
        }

        public IMvxCommand NavigateToSearch {
            get {
                return GetCommand(() =>
                 {
                    MessengerHub.Unsubscribe<AddressSelected> ( _token );
                    _token.Dispose ();
                    _token = null;

                    RequestNavigate<AddressSearchViewModel> (new { search = "", ownerId = _ownerId });                    
                });
            }
        }

        void OnAddressSelected (AddressSelected obj)
        {
            RequestClose(this);
        }

        public IMvxCommand SaveCommand
        {
            get
            {
                return GetCommand(() => 
                {
                    Model.UpdateStreetOrNumberBuildingName(StreetNumberOrBuildingName);
                    MessengerHub.Publish(new AddressSelected(this, Model,_ownerId));                    
                });
            }
        }
    }
}

