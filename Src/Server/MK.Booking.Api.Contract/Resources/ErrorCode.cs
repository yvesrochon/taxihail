﻿namespace apcurium.MK.Booking.Api.Contract.Resources
{
    public enum ErrorCode
    {
// ReSharper disable InconsistentNaming
        CreateAccount_AccountAlreadyExist,
        CreateAccount_CannotSendSMS,
        CreateAccount_InvalidConfirmationToken,
        CreateOrder_CannotCreateInIbs,
        CreateOrder_SettingsRequired,
        CreateOrder_InvalidProvider,
        CreateOrder_RuleDisable,
        CreateOrder_NoFareEstimateAvailable,
        CreateOrder_PendingOrder,
        CreateOrder_CardOnFileButNoCreditCard,
        CreateOrder_CardOnFileDeactivated,
        CreateOrder_NoChargeType,
        NearbyPlaces_LocationRequired,
        Search_Locations_NameRequired,
        UpdatePassword_NotSame,
        OrderNotInIbs,
        OrderNotCompleted,
        Tariff_DuplicateName,
        Rule_DuplicateName,
        Rule_InvalidPriority,
        ResetPassword_AccountNotFound,
        ResetPassword_FacebookAccount,
        ResetPassword_TwitterAccount,
        AccountCharge_AccountAlreadyExisting,
        AccountCharge_InvalidAccountNumber,
        AccountCharge_InvalidAnswer,
        RatingType_DuplicateName,
        IBSAccountNotFound,
        ManualRideLinq_NoCardOnFile,
        ManualRideLinq_CardOnFileDeactivated,
		EmailAlreadyUsed,
		Rule_TwoTypeZoneVerificationSelected
 // ReSharper restore InconsistentNaming
    }
}