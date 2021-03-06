using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using apcurium.MK.Booking.Api.Contract.Requests;
using apcurium.MK.Booking.Api.Contract.Resources;
using apcurium.MK.Booking.Mobile.AppServices.Impl;
using apcurium.MK.Booking.Mobile.Data;
using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Booking.Mobile.Infrastructure;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Diagnostic;
using apcurium.MK.Common.Entity;
using apcurium.MK.Common.Enumeration;
using apcurium.MK.Common.Extensions;
using apcurium.MK.Common.Configuration.Impl;
using apcurium.MK.Common.Provider;
using MK.Common.Exceptions;
using ServiceStack.ServiceInterface.ServiceModel;
using apcurium.MK.Booking.Mobile.Models;

namespace apcurium.MK.Booking.Mobile.AppServices.Orders
{
	public class OrderWorkflowService: BaseService, IOrderWorkflowService
    {
		readonly ILocationService _locationService;
		readonly IAccountService _accountService;
		readonly IVehicleTypeService _vehicleTypeService;
		readonly IGeolocService _geolocService;
		readonly IAppSettings _appSettings;
		readonly ILocalization _localize;
		readonly IBookingService _bookingService;
		readonly ICacheService _cacheService;
		readonly IAccountPaymentService _accountPaymentService;
	    readonly INetworkRoamingService _networkRoamingService;
		readonly IPaymentService _paymentService;
        readonly IPOIProvider _poiProvider;
	    private readonly ILogger _logger;
        private readonly IDeviceCollectorService _deviceCollectorService;

	    readonly ISubject<Address> _pickupAddressSubject = new BehaviorSubject<Address>(new Address());
		readonly ISubject<Address> _destinationAddressSubject = new BehaviorSubject<Address>(new Address());
		readonly ISubject<AddressSelectionMode> _addressSelectionModeSubject = new BehaviorSubject<AddressSelectionMode>(AddressSelectionMode.PickupSelection);
		readonly ISubject<DateTime?> _pickupDateSubject = new BehaviorSubject<DateTime?>(null);
		readonly ISubject<int?> _vehicleTypeSubject;
        readonly ISubject<BookingSettings> _bookingSettingsSubject;
		readonly ISubject<string> _estimatedFareDisplaySubject;
        readonly ISubject<OrderValidationResult> _orderValidationResultSubject = new BehaviorSubject<OrderValidationResult>(new OrderValidationResult());
		readonly ISubject<DirectionInfo> _estimatedFareDetailSubject = new BehaviorSubject<DirectionInfo>( new DirectionInfo() );
		readonly ISubject<string> _noteToDriverSubject = new BehaviorSubject<string>(string.Empty);
		readonly ISubject<string> _promoCodeSubject = new BehaviorSubject<string>(string.Empty);
		readonly ISubject<bool> _loadingAddressSubject = new BehaviorSubject<bool>(false);
		readonly ISubject<bool> _dropOffSelectionModeSubject = new BehaviorSubject<bool>(false);
		readonly ISubject<AccountChargeQuestion[]> _accountPaymentQuestions = new BehaviorSubject<AccountChargeQuestion[]> (null);
		readonly ISubject<bool> _orderCanBeConfirmed = new BehaviorSubject<bool>(false);
		readonly ISubject<bool> _isDestinationModeOpenedSubject = new BehaviorSubject<bool>(false);
		readonly ISubject<string> _cvvSubject = new BehaviorSubject<string>(string.Empty);
		readonly ISubject<PickupPoint[]> _poiRefPickupListSubject = new BehaviorSubject<PickupPoint[]>(new PickupPoint[0]);
        readonly ISubject<Airline[]> _poiRefAirlineListSubject = new BehaviorSubject<Airline[]>(new Airline[0]);
        readonly ISubject<double?> _tipIncentiveSubject = new BehaviorSubject<double?>(null);
		readonly ISubject<bool> _canExecuteBookingOperation = new BehaviorSubject<bool>(true);

        private bool _isOrderRebooked;
        private string _kountSessionId;
		private MarketSettings _marketSettings = new MarketSettings { HashedMarket = null };

		public OrderWorkflowService(ILocationService locationService,
			IAccountService accountService,
			IGeolocService geolocService,
			IAppSettings configurationManager,
			ILocalization localize,
			IBookingService bookingService,
			ICacheService cacheService,
			IAccountPaymentService accountPaymentService,
            INetworkRoamingService networkRoamingService,
			IPaymentService paymentService,
            ILogger logger,
            IPOIProvider poiProvider,
			IVehicleTypeService vehicleTypeService,
            IDeviceCollectorService deviceCollectorService)
		{
			_cacheService = cacheService;
			_appSettings = configurationManager;
			_geolocService = geolocService;
			_accountService = accountService;
			_vehicleTypeService = vehicleTypeService;
			_locationService = locationService;
            _deviceCollectorService = deviceCollectorService;

			_bookingSettingsSubject = new BehaviorSubject<BookingSettings>(accountService.CurrentAccount.Settings);

            _vehicleTypeSubject = new BehaviorSubject<int?>(
                _appSettings.Data.VehicleTypeSelectionEnabled
                ? accountService.CurrentAccount.Settings.VehicleTypeId
                : null);

			_localize = localize;
			_bookingService = bookingService;
			_accountPaymentService = accountPaymentService;
		    _networkRoamingService = networkRoamingService;
			_paymentService = paymentService;
		    _logger = logger;
            _poiProvider = poiProvider;

		    _estimatedFareDisplaySubject = new BehaviorSubject<string>(_localize[_appSettings.Data.DestinationIsRequired ? "NoFareTextIfDestinationIsRequired" : "NoFareText"]);
		
			Observe (_networkRoamingService.GetAndObserveMarketSettings(), marketSettings => MarketChanged(marketSettings).FireAndForget());
			Observe (_vehicleTypeService.GetAndObserveVehiclesList(), vehiclesList => PreselectDefaultVehicleType(vehiclesList));
		}

		private async Task MarketChanged(MarketSettings marketSettings)
		{
			if (_marketSettings.HashedMarket != marketSettings.HashedMarket)
			{
				await SetBookingSettings(_accountService.CurrentAccount.Settings);
			}

			_marketSettings = marketSettings;
		}

		public async Task SetAddress(Address address)
		{
			await SetAddressToCurrentSelection(address);
		}

		public async Task<Address> SetAddressToUserLocation(CancellationToken cancellationToken = default(CancellationToken))
		{
			_loadingAddressSubject.OnNext(true);

		    var position = await _locationService.GetUserPosition();
		    if (position == null)
		    {
                _loadingAddressSubject.OnNext(false);
                return new Address();
		    }

            var address = await SearchAddressForCoordinate(position);
			cancellationToken.ThrowIfCancellationRequested();
			await SetAddressToCurrentSelection(address, cancellationToken);
			return address;
		}

		public void SetAddresses(Address pickupAddress, Address destinationAddress)
		{
			_pickupAddressSubject.OnNext(pickupAddress);
			_destinationAddressSubject.OnNext(destinationAddress);
		}

		public void SetDropOffSelectionMode(bool isDropOffSelectionMode)
		{
			_dropOffSelectionModeSubject.OnNext(isDropOffSelectionMode);
		}

        public async Task SetAddressToCoordinate(Position coordinate, CancellationToken cancellationToken)
		{
			var address = await SearchAddressForCoordinate(coordinate);
			address.Latitude = coordinate.Latitude;
			address.Longitude = coordinate.Longitude;
			cancellationToken.ThrowIfCancellationRequested();
			await SetAddressToCurrentSelection(address, cancellationToken);
		}

		public async Task ClearDestinationAddress()
		{
			_destinationAddressSubject.OnNext(new Address());

			await CalculateEstimatedFare();
		}

		public async Task SetPickupDate(DateTime? date)
		{
			_pickupDateSubject.OnNext(date);

			await CalculateEstimatedFare();
		}

		public async Task ToggleBetweenPickupAndDestinationSelectionMode()
		{
			var currentSelectionMode = await _addressSelectionModeSubject.Take(1).ToTask();

			_addressSelectionModeSubject.OnNext(currentSelectionMode == AddressSelectionMode.PickupSelection
				? AddressSelectionMode.DropoffSelection
				: AddressSelectionMode.PickupSelection);
		}

		public void SetAddressSelectionMode(AddressSelectionMode mode = AddressSelectionMode.None)
		{
			_addressSelectionModeSubject.OnNext(mode);
		}

		public async Task ValidatePickupTime()
		{
			var pickupDate = await _pickupDateSubject.Take(1).ToTask();
			var pickupDateIsValid = !pickupDate.HasValue || (pickupDate.Value.ToUniversalTime() >= DateTime.UtcNow);

			if (!pickupDateIsValid)
			{
				throw new OrderValidationException("Invalid pickup date", OrderValidationError.InvalidPickupDate);
			}
		}

		public async Task ValidateNumberOfPassengers(int? numberOfPassengers)
		{
			var vehicleTypeId = await _vehicleTypeSubject.Take(1).ToTask();
			var vehicleTypes = await _vehicleTypeService.GetAndObserveVehiclesList().Take(1).ToTask();
			var data = await _accountService.GetReferenceData();
			var settings = await _bookingSettingsSubject.Take(1).ToTask();
			var defaultVehicleType = data.VehiclesList.FirstOrDefault (x => x.IsDefault.HasValue && x.IsDefault.Value);

			if (vehicleTypeId == null
				&& defaultVehicleType != null)
			{
				vehicleTypeId = defaultVehicleType.Id;
			}

			var vehicleType = vehicleTypes.FirstOrDefault(v => v.ReferenceDataVehicleId == vehicleTypeId);
			numberOfPassengers = numberOfPassengers ?? settings.Passengers;

			if (vehicleType != null
				&& vehicleType.MaxNumberPassengers > 0
				&& numberOfPassengers > vehicleType.MaxNumberPassengers)
			{
				throw new OrderValidationException("Number of passengers is too large", OrderValidationError.InvalidPassengersNumber);
			}
		}

	    public async Task<bool> ValidateChargeType()
	    {
			var chargeTypes = await _accountService.GetAndObservePaymentsList().Take(1).ToTask();

	        if (!chargeTypes.Any())
	        {
	            return false;
	        }

	        return true;
	    }

		public async Task ValidatePickupAndDestination()
		{
			var pickupAddress = await _pickupAddressSubject.Take(1).ToTask();
			var pickupIsValid = pickupAddress.FullAddress.HasValue()
				&& pickupAddress.HasValidCoordinate();

			if (!pickupIsValid)
			{
				throw new OrderValidationException("Pickup address required", OrderValidationError.PickupAddressRequired);
			}

			var destinationIsRequired = _appSettings.Data.DestinationIsRequired;

			if (destinationIsRequired)
			{
				var currentSelectionMode = await _addressSelectionModeSubject.Take(1).ToTask();

				var destinationAddress = await _destinationAddressSubject.Take(1).ToTask();
				var destinationIsValid = destinationAddress.FullAddress.HasValue()
					&& destinationAddress.HasValidCoordinate();

				if (currentSelectionMode == AddressSelectionMode.PickupSelection && !destinationIsValid)
				{
					await ToggleBetweenPickupAndDestinationSelectionMode();
					await ToggleIsDestinationModeOpened();
					throw new OrderValidationException("Open the destination selection", OrderValidationError.OpenDestinationSelection);
				}

				if (!destinationIsValid)
				{
					throw new OrderValidationException("Destination address required", OrderValidationError.DestinationAddressRequired);
				}
			}
		}

		public async Task<OrderRepresentation> ConfirmOrder()
		{
		    _isOrderRebooked = false;

			var order = await GetOrder();

			try
			{
				await ValidateTokenizedCardIfNecessary(false, order.Settings.ChargeTypeId, order.KountSessionId);

				var orderStatus = await _bookingService.CreateOrder(order);

			    var currentDate = DateTime.Now;

				var orderCreated = new Order
				{
                    CreatedDate = currentDate, 
					DropOffAddress = order.DropOffAddress, 
					IBSOrderId = orderStatus.IBSOrderId, 
					Id = order.Id,
                    PickupAddress = order.PickupAddress,
					Note = order.Note,
                    TipIncentive = order.TipIncentive,
                    PickupDate = order.PickupDate ?? currentDate,
					Settings = order.Settings,
					PromoCode = order.PromoCode
				};

				Logger.LogMessage("Order created: ID [" + orderCreated.Id + "], IBS ID [" + orderStatus.IBSOrderId + "]");
                Logger.LogMessage("MarketSettings for order {0}: {1}", orderCreated.Id, _marketSettings.ToJson());

                _deviceCollectorService.GenerateNewSessionIdAndCollect();

				return new OrderRepresentation(orderCreated, orderStatus);
			}
			catch(InvalidCreditCardException ex)
			{
				// will be handled by calling method
				throw ex;
			}
			catch(WebServiceException e)
			{
				// prevents an exception in the deserialization of the response body
				if (e.StatusCode != (int)HttpStatusCode.BadRequest)
				{
					throw new OrderCreationException(GetUnhandledErrorMessageForOrderCreation());
				}

			    var error = e.ResponseBody.FromJson<ErrorResponse>();

			    if (e.StatusCode == (int)HttpStatusCode.BadRequest && error.ResponseStatus != null)
			    {
					var localizedMessageKey = error.ResponseStatus.ErrorCode == "CreateOrder_PendingOrder" 
                        ? error.ResponseStatus.ErrorCode 
                        : error.ResponseStatus.Message;

                    throw new OrderCreationException(localizedMessageKey, error.ResponseStatus.Message);
			    }

			    // Unhandled errors
				// if ibs3000, there's a problem with the account, use a different one

				throw new OrderCreationException(GetUnhandledErrorMessageForOrderCreation());		
			}			
			catch(Exception)
			{
				throw new OrderCreationException(GetUnhandledErrorMessageForOrderCreation());
			}
		}

		public async Task SetVehicleType(int? vehicleTypeId)
		{
			if (_appSettings.Data.VehicleTypeSelectionEnabled)
			{
				_vehicleTypeSubject.OnNext(vehicleTypeId);
			}

			var bookingSettings = await _bookingSettingsSubject.Take (1).ToTask ();
			bookingSettings.VehicleTypeId = vehicleTypeId;

			await SetBookingSettings (bookingSettings);
		}

		public async Task SetBookingSettings(BookingSettings bookingSettings)
		{
			// Get the vehicle type selected on the home screen and put them in the 
			// bookingsettings to prevent the default vehicle type to override the selected value
			var vehicleTypeId = await _vehicleTypeSubject.Take (1).ToTask ();
			bookingSettings.VehicleTypeId = vehicleTypeId;

			var paymentList = await _accountService.GetAndObservePaymentsList().Take(1).ToTask();

            // if there's a market and payment preference of the user is set to CardOnFile, change it to PaymentInCar
		    if (bookingSettings.ChargeTypeId == ChargeTypes.CardOnFile.Id)
		    {
				if (!_marketSettings.IsLocalMarket)
		        {
					var paymentSettings = await _paymentService.GetPaymentSettings();

					if (paymentSettings.PaymentMode != PaymentMethod.Cmt
						&& paymentSettings.PaymentMode != PaymentMethod.RideLinqCmt)
					{
						bookingSettings.ChargeTypeId = ChargeTypes.PaymentInCar.Id;
					}
		        }
		    }

            // If no booking settings matches the available payment types, take PayInCar
            // or the first one by default if PayInCar was deactivated. It will attempt to avoid ChargeAccount if possible.
            if (paymentList.None(x => x.Id == bookingSettings.ChargeTypeId))
            {
                var matchingPaymentType = paymentList.FirstOrDefault(p => p.Id == ChargeTypes.PaymentInCar.Id)
                                          ?? paymentList.FirstOrDefault(p => p.Id != ChargeTypes.Account.Id)
                                          ?? paymentList.FirstOrDefault();

                bookingSettings.ChargeTypeId = matchingPaymentType != null ? matchingPaymentType.Id : null;
            }
            
			_bookingSettingsSubject.OnNext(bookingSettings);
		}

		public async Task SetPickupAptAndRingCode(string apt, string ringCode)
		{
			var address = await _pickupAddressSubject.Take(1).ToTask();
			address.Apartment = apt;
			address.RingCode = ringCode;
			_pickupAddressSubject.OnNext(address);
		}

		public async Task<OrderRepresentation> GetLastActiveOrder()
		{
			if (_bookingService.HasLastOrder) 
			{
                var status = await _bookingService.GetLastOrderStatus();

				if (status != null && !_bookingService.IsStatusCompleted(status))
                {
                    try
                    {
                        var order = await _accountService.GetHistoryOrderAsync(status.OrderId);

						return new OrderRepresentation(order, status);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogMessage(string.Format("Error trying to get status of order {0}", status.OrderId));
                        _logger.LogError(ex);

                        return null;
                    }
                }

				try
				{
					var order = await _accountService.GetHistoryOrderAsync(status.OrderId);

					// For some reason, the OrderId is not found. This should not normally happen in production.
					if(order == null)
					{
						_logger.LogMessage(string.Format("Order {0} was not found on server.", status.OrderId));

						_bookingService.ClearLastOrder();
						return null;
					}

					if (order.IsRated)
					{
						_bookingService.ClearLastOrder();
					}
					else
					{
						if (!order.IsManualRideLinq)
						{
							// Rating only for "normal" rides
							_bookingService.SetLastUnratedOrderId(status.OrderId);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogMessage(string.Format("Error trying to get status of order {0}", status.OrderId));
					_logger.LogError(ex);

					return null;
				}
			}

			return null;
		}

        public async Task POIRefPickupList(string textMatch, int maxRespSize)
        {
			var pickupPointLists = await _poiProvider.GetPOIRefPickupList(string.Empty, textMatch, maxRespSize);
			_poiRefPickupListSubject.OnNext(pickupPointLists);

        }

        public async Task POIRefAirLineList(string textMatch, int maxRespSize)
        {
			var airlines = await _poiProvider.GetPOIRefAirLineList(string.Empty, textMatch, maxRespSize);
			_poiRefAirlineListSubject.OnNext(airlines);
        }

        public Guid? GetLastUnratedRide()
	    {
            if (_bookingService.HasUnratedLastOrder)
            {
                return _bookingService.GetUnratedLastOrder();
            }
            return null;
	    }

		public IObservable<Address> GetAndObservePickupAddress()
		{
			return _pickupAddressSubject;
		}

		public IObservable<Address> GetAndObserveDestinationAddress()
		{
			return _destinationAddressSubject;
		}

		public IObservable<AddressSelectionMode> GetAndObserveAddressSelectionMode()
		{
			return _addressSelectionModeSubject;
		}

		public IObservable<PickupPoint[]> GetAndObservePOIRefPickupList()
        {
            return _poiRefPickupListSubject;
        }

		public IObservable<Airline[]> GetAndObservePOIRefAirlineList()
        {
            return _poiRefAirlineListSubject;
        }

        public async Task<Address> GetCurrentAddress()
		{
			var currentSelectionMode = await _addressSelectionModeSubject.Take(1).ToTask();
			if (currentSelectionMode == AddressSelectionMode.PickupSelection)
			{
				return await _pickupAddressSubject.Take(1).ToTask();
			}
			else
			{
				return await _destinationAddressSubject.Take(1).ToTask();
			}
		}

		public IObservable<int?> GetAndObserveVehicleType()
		{
			return _vehicleTypeSubject;
		}

		public IObservable<BookingSettings> GetAndObserveBookingSettings()
		{
			return _bookingSettingsSubject;
		}

		public IObservable<string> GetAndObserveEstimatedFare()
		{
			return _estimatedFareDisplaySubject;
		}

        public IObservable<OrderValidationResult> GetAndObserveOrderValidationResult()
        {
            return _orderValidationResultSubject;
        }

		public IObservable<string> GetAndObserveNoteToDriver()
		{
			return _noteToDriverSubject;
        }

        public IObservable<double?> GetAndObserveTipIncentive()
        {
            return _tipIncentiveSubject;
        }

		public IObservable<string> GetAndObservePromoCode()
		{
			return _promoCodeSubject;
		}

		public IObservable<bool> GetAndObserveOrderCanBeConfirmed()
		{
			return _orderCanBeConfirmed;
		}

		public IObservable<DateTime?> GetAndObservePickupDate()
		{
			return _pickupDateSubject;
		}

		public IObservable<bool> GetAndObserveLoadingAddress()
		{
			return _loadingAddressSubject;
		}

		public IObservable<bool> GetAndObserveDropOffSelectionMode()
		{
			return _dropOffSelectionModeSubject;
		}

		public IObservable<bool> GetAndObserveIsUsingGeo()
		{
			return _networkRoamingService.GetAndObserveMarketSettings()
				.Select(marketSettings => !marketSettings.IsLocalMarket
					? _appSettings.Data.ExternalAvailableVehiclesMode == ExternalAvailableVehiclesModes.Geo
					: _appSettings.Data.LocalAvailableVehiclesMode == LocalAvailableVehiclesModes.Geo
				);
		}

		private async Task<Address> SearchAddressForCoordinate(Position p)
		{
			_loadingAddressSubject.OnNext(true);
			var accountAddress = await _accountService
				.FindInAccountAddresses(p.Latitude, p.Longitude)
				.ConfigureAwait(false);

			if (accountAddress != null)
			{
				_loadingAddressSubject.OnNext(false);
				return accountAddress;
			}

			var address = await Task.Run(() => _geolocService.SearchAddress(p.Latitude, p.Longitude));
			if (address.Any())
			{
				_loadingAddressSubject.OnNext(false);
				return address[0];
			}

			_loadingAddressSubject.OnNext(false);
			return new Address { Latitude = p.Latitude, Longitude = p.Longitude };
		}

		private async Task SetAddressToCurrentSelection(Address address, CancellationToken token = default(CancellationToken))
		{
			// Needs to run in a background thread to prevent a potential deadlock issue.
			await Task.Run(async () =>
			{
				var selectionMode = await _addressSelectionModeSubject.Take(1).ToTask(token);
				if (selectionMode == AddressSelectionMode.PickupSelection)
				{
					_pickupAddressSubject.OnNext (address);
					await _networkRoamingService.UpdateMarketSettingsIfNecessary(new Position { Latitude = address.Latitude, Longitude = address.Longitude });
				} 
				else 
				{
					_destinationAddressSubject.OnNext (address);
				}
			}, token);

			// Do NOT await this
			CalculateEstimatedFare(token).FireAndForget();

			_canExecuteBookingOperation.OnNext(true);
		}

		private CancellationTokenSource _calculateFareCancellationTokenSource = new CancellationTokenSource();

		private async Task CalculateEstimatedFare(CancellationToken token = default(CancellationToken))
		{
			if (!_calculateFareCancellationTokenSource.IsCancellationRequested)
			{
				_calculateFareCancellationTokenSource.Cancel ();
			}
			_calculateFareCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

			var newCancelToken = _calculateFareCancellationTokenSource.Token;

			_estimatedFareDisplaySubject.OnNext(_localize["EstimateFareCalculating"]);

			var direction = await GetFareEstimate ();

			var estimatedFareString = _bookingService.GetFareEstimateDisplay(direction);

			if (newCancelToken.IsCancellationRequested) {
				return;
			}

			_estimatedFareDetailSubject.OnNext (direction);
			_estimatedFareDisplaySubject.OnNext(estimatedFareString);
		}

		private async Task<DirectionInfo> GetFareEstimate()
		{
			// Create order for fare estimate
            var order = new CreateOrderRequest
		    {
		        Id = Guid.NewGuid(),
		        PickupDate = await _pickupDateSubject.Take(1).ToTask(),
		        PickupAddress = await _pickupAddressSubject.Take(1).ToTask(),
		        DropOffAddress = await _destinationAddressSubject.Take(1).ToTask(),
		        Settings = await _bookingSettingsSubject.Take(1).ToTask()
		    };

		    var estimate = await _bookingService.GetFareEstimate(order);

			_orderValidationResultSubject.OnNext(estimate.ValidationResult);

		    return estimate;
		}

		public async Task PrepareForNewOrder()
		{          
			var isDestinationModeOpened = await _isDestinationModeOpenedSubject.Take(1).ToTask();
			if (isDestinationModeOpened)
			{
				await ToggleBetweenPickupAndDestinationSelectionMode();
				await ToggleIsDestinationModeOpened(false);
			}

			_noteToDriverSubject.OnNext(string.Empty);
			_promoCodeSubject.OnNext(string.Empty);
			_pickupAddressSubject.OnNext(new Address());
			_destinationAddressSubject.OnNext(new Address());
			_addressSelectionModeSubject.OnNext(AddressSelectionMode.PickupSelection);
			_pickupDateSubject.OnNext(null);
		    if (_accountService.CurrentAccount != null)
		    {
                await SetBookingSettings(_accountService.CurrentAccount.Settings);
            }
            _estimatedFareDisplaySubject.OnNext(_localize[_appSettings.Data.DestinationIsRequired ? "NoFareTextIfDestinationIsRequired" : "NoFareText"]);
			_orderCanBeConfirmed.OnNext (false);
			_cvvSubject.OnNext(string.Empty);
			DisableBooking();
			_loadingAddressSubject.OnNext(false);
			_dropOffSelectionModeSubject.OnNext(false);
			_accountPaymentQuestions.OnNext(null);
            _poiRefPickupListSubject.OnNext(new PickupPoint[0]);
            _poiRefAirlineListSubject.OnNext(new Airline[0]);
            _tipIncentiveSubject.OnNext(null);
        }

		public void BeginCreateOrder()
		{
			_orderCanBeConfirmed.OnNext (false);
		}

		public void EndCreateOrder()
		{
			Observable.Timer( TimeSpan.FromSeconds( 1 )).Subscribe( _ => _orderCanBeConfirmed.OnNext (true));
		}

		public async Task ResetOrderSettings()
		{
            _noteToDriverSubject.OnNext(string.Empty);
            _tipIncentiveSubject.OnNext(null);
			await SetBookingSettings(_accountService.CurrentAccount.Settings);
		}

		public void SetNoteToDriver(string text)
		{
			_noteToDriverSubject.OnNext(text);
        }

        public void SetTipIncentive(double? tipIncentive)
        {
            _tipIncentiveSubject.OnNext(tipIncentive);
        }

		public void SetPromoCode(string code)
		{
			_promoCodeSubject.OnNext(code);
		}

		public async Task<double?> GetTipIncentive()
		{
			return await _tipIncentiveSubject.Take(1).ToTask();
		}

		public async Task<bool> ShouldWarnAboutEstimate()
		{
			var destination = await _destinationAddressSubject.Take(1).ToTask();
			return _appSettings.Data.ShowEstimateWarning
					&& !_cacheService.Get<string>("WarningEstimateDontShow").HasValue()
					&& destination.HasValidCoordinate();
		}

		public async Task<bool> ShouldWarnAboutPromoCode()
		{
			var promoCode = await _promoCodeSubject.Take(1).ToTask();
			return !_cacheService.Get<string>("WarningPromoCodeDontShow").HasValue()
				&& promoCode.HasValue();
		}

	    public bool ShouldPromptUserToRateLastRide()
	    {
            return !_cacheService.Get<string>("RateLastRideDontPrompt").HasValue();
	    }

		public async Task<bool> UpdateDropOff (Guid orderId)
		{
			var address = await GetCurrentAddress(); 

			return await _bookingService.UpdateDropOff(orderId, address);
		}

		public async Task<bool> ShouldGoToAccountNumberFlow()
		{
			var settings = await _bookingSettingsSubject.Take(1).ToTask();
            return settings.ChargeTypeId == ChargeTypes.Account.Id;
		}

        public async Task<bool> ValidateAccountNumberAndPrepareQuestions(string accountNumber = null, string customerNumber = null)
		{
			if (accountNumber == null || customerNumber == null)
			{
				var settings = await _bookingSettingsSubject.Take(1).ToTask();
				accountNumber = settings.AccountNumber;
                customerNumber = settings.CustomerNumber;
			}

			if (!accountNumber.HasValue() && !customerNumber.HasValue())
			{
				return false;
			}

			try
			{
				var questions = await _accountPaymentService.GetQuestions (accountNumber, customerNumber);
				_accountPaymentQuestions.OnNext (questions);

				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool ValidateAndSaveAccountAnswers(AccountChargeQuestion[] questionsAndAnswers)
		{
			if (questionsAndAnswers.Any (x => x.IsEnabled
										&& x.IsRequired
										&& string.IsNullOrEmpty (x.Answer)))
			{
				return false;
			}

			if (questionsAndAnswers.Any(x => x.IsEnabled
				&& x.MaxLength != null
				&& !string.IsNullOrEmpty(x.Answer)
				&& x.Answer.Length > x.MaxLength))
			{
				return false;
			}

			_accountPaymentQuestions.OnNext (questionsAndAnswers);

			return true;
		}

		public async Task SetAccountNumber(string accountNumber, string customerNumber)
		{
            _accountService.UpdateAccountNumber(accountNumber, customerNumber);
            
			var bookingSettings = await _bookingSettingsSubject.Take(1).ToTask();
			bookingSettings.AccountNumber = accountNumber;
            bookingSettings.CustomerNumber = customerNumber;

			await SetBookingSettings (bookingSettings);
		}

		public async Task<AccountChargeQuestion[]> GetAccountPaymentQuestions()
		{
			return await _accountPaymentQuestions.Take (1).ToTask ();
		}

        public async Task<OrderValidationResult> ValidateOrder(CreateOrderRequest order = null)
		{
			_kountSessionId = _deviceCollectorService.GetSessionId();

			var orderToValidate = order ?? await GetOrder();
			var validationResult = await _bookingService.ValidateOrder(orderToValidate);
            _orderValidationResultSubject.OnNext(validationResult);

			return validationResult;
		}

		public async Task ValidateTokenizedCardIfNecessary(bool isManualRideLinq, int? chargeTypeId, string kountSessionId)
		{
			try
			{
				if (isManualRideLinq || chargeTypeId == ChargeTypes.CardOnFile.Id)
				{
					var creditCard = await _accountService.GetDefaultCreditCard();
					if (creditCard == null)
					{
						throw new Exception("No credit card in account");
					}

					if (creditCard.IsExpired())
					{
						throw new Exception("Credit card has expired");
					}

					if (creditCard.IsDeactivated)
					{
						throw new Exception("Credit card is deactivated");
					}

					var cvv = await _cvvSubject.Take(1).ToTask();

					var response = await _paymentService.ValidateTokenizedCard(creditCard, cvv, kountSessionId, _accountService.CurrentAccount);
					if(!response.IsSuccessful)
					{
						throw new Exception("PaymentService.ValidateTokenizedCard() returned an unsuccessful response");
					}
				}
			}
			catch(Exception ex)
			{
				throw new InvalidCreditCardException("Validation of tokenized card failed", ex);
			}
		}

		public async Task<bool> ValidateCardOnFile()
		{
			var orderToValidate = await GetOrder ();	
			if (orderToValidate.Settings.ChargeTypeId == ChargeTypes.CardOnFile.Id 
				&& _accountService.CurrentAccount.DefaultCreditCard == null)
			{
				return false;
			}

			return true;
		}

		public async Task<bool> ValidateCardExpiration()
		{
			var orderToValidate = await GetOrder();	
			if (orderToValidate.Settings.ChargeTypeId == ChargeTypes.CardOnFile.Id)
			{
				var creditCard = await _accountService.GetDefaultCreditCard();

				if (creditCard == null)
                {
					return false;
				}

				return !creditCard.IsExpired();
			}

			return true;
		}

		public async Task<bool> ValidatePromotionUseConditions()
		{
			var orderToValidate = await GetOrder ();	
			if (orderToValidate.PromoCode.HasValue())
			{
				if (orderToValidate.Settings.ChargeTypeId != ChargeTypes.CardOnFile.Id
                    && orderToValidate.Settings.ChargeTypeId != ChargeTypes.PayPal.Id)
				{
					return false;
				}
			}

			return true;
		}

		public void ConfirmValidationOrder()
		{
			_orderCanBeConfirmed.OnNext (true);
		}

        private async Task<CreateOrderRequest> GetOrder()
		{
            var order = new CreateOrderRequest();
			order.Id = Guid.NewGuid();
			order.PickupDate = await _pickupDateSubject.Take(1).ToTask();
			if (order.PickupDate.HasValue) 
			{
				order.PickupDate = DateTime.SpecifyKind (order.PickupDate.Value, DateTimeKind.Unspecified);
			}

            order.PickupAddress = await _pickupAddressSubject.Take(1).ToTask();
			order.DropOffAddress = await _destinationAddressSubject.Take(1).ToTask();
			order.Settings = await _bookingSettingsSubject.Take(1).ToTask();
			order.Note = await _noteToDriverSubject.Take(1).ToTask();
            order.PromoCode = await _promoCodeSubject.Take(1).ToTask();
            order.TipIncentive = await _tipIncentiveSubject.Take(1).ToTask();
			
			var estimatedFare = await _estimatedFareDetailSubject.Take (1).ToTask();
			if (estimatedFare != null) 
			{
				order.Estimate = new RideEstimate
				{ 
					Price = estimatedFare.Price, 
					Distance = estimatedFare.Distance ?? 0
				};
			}

		    order.UserLatitude = _locationService.BestPosition != null
		        ? _locationService.BestPosition.Latitude
		        : (double?) null;
            order.UserLongitude = _locationService.BestPosition != null
                ? _locationService.BestPosition.Longitude
                : (double?)null;

			order.QuestionsAndAnswers = await _accountPaymentQuestions.Take(1).ToTask ();
			order.Cvv = await _cvvSubject.Take(1).ToTask();
			order.KountSessionId = _kountSessionId;

			return order;
		}

		public async Task Rebook(Order previous)
		{
            _isOrderRebooked = true;
			_pickupAddressSubject.OnNext(previous.PickupAddress);
			_destinationAddressSubject.OnNext(previous.DropOffAddress);
			_bookingSettingsSubject.OnNext(previous.Settings);
            _noteToDriverSubject.OnNext(previous.Note);
            _tipIncentiveSubject.OnNext(previous.TipIncentive);
            await CalculateEstimatedFare();
		}

        public bool IsOrderRebooked()
        {
            return _isOrderRebooked;
        }

	    public void CancelRebookOrder()
	    {
	        _isOrderRebooked = false;
	    }

		private async Task PreselectDefaultVehicleType(IList<VehicleType> vehicleList)
		{
			int? selectedVehicleId = null;

			if (vehicleList.Any())
			{
				if (_marketSettings.IsLocalMarket)
				{
					// Try to match with account vehicle type preference if no match, we use the first vehicle
					var matchingVehicle = vehicleList.FirstOrDefault(v => v.ReferenceDataVehicleId == _accountService.CurrentAccount.Settings.VehicleTypeId);
					selectedVehicleId = matchingVehicle != null
						? matchingVehicle.ReferenceDataVehicleId
						: vehicleList.First().ReferenceDataVehicleId;
				}
				else
				{
					selectedVehicleId = vehicleList.First().ReferenceDataVehicleId;
				}
			}

			await SetVehicleType(selectedVehicleId);
		}

		public async Task ToggleIsDestinationModeOpened(bool? forceValue = null)
		{
			var currentValue = await _isDestinationModeOpenedSubject.Take(1).ToTask();
			_isDestinationModeOpenedSubject.OnNext(forceValue ?? !currentValue);
		}
			
		public IObservable<bool> GetAndObserveIsDestinationModeOpened()
		{
			return _isDestinationModeOpenedSubject;
		}

		public async Task<bool> ShouldPromptForCvv()
		{
			var order = await GetOrder ();
			if (order.Settings.ChargeTypeId == ChargeTypes.CardOnFile.Id)
			{
				var paymentSettings = await _paymentService.GetPaymentSettings();
				return paymentSettings.AskForCVVAtBooking;
			}

			return false;
		}

		public async Task<bool> ShouldPromptForCreditCardConfirm()
		{
			var order = await GetOrder();

			return order.Settings.ChargeTypeId == ChargeTypes.CardOnFile.Id &&
				        _appSettings.Data.MaxNumberOfCardsOnFile > 1;
		}

		public bool ValidateAndSetCvv(string cvv)
		{
			if (!cvv.HasValue())
			{
				return false;
			}

			var success = cvv.IsDigit()
				&& cvv.Length >= 3
				&& cvv.Length <= 4;

			if (success)
			{
				_cvvSubject.OnNext(cvv);
			}

			return success;
		}

		public void DisableBooking()
		{
			_canExecuteBookingOperation.OnNext(false);
		}

		public IObservable<bool> GetAndObserveCanExecuteBookingOperation()
		{
			return _canExecuteBookingOperation;
		}

		private string GetUnhandledErrorMessageForOrderCreation()
		{
			return _appSettings.Data.HideCallDispatchButton
				? _localize["ServiceError_ErrorCreatingOrderMessage_NoCall"]
				: string.Format(_localize["ServiceError_ErrorCreatingOrderMessage"], _appSettings.Data.TaxiHail.ApplicationName, _appSettings.Data.DefaultPhoneNumberDisplay);
		}
    }
}

