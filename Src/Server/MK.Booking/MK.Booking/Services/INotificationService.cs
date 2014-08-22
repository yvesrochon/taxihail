using System;
using apcurium.MK.Booking.Commands;
using apcurium.MK.Common.Entity;

namespace apcurium.MK.Booking.Services
{
    public interface INotificationService
    {
        void SendStatusChangedNotification(OrderStatusDetail orderStatusDetail);
        void SendPaymentCaptureNotification(Guid orderId, decimal amount);
        void SendTaxiNearbyNotification(Guid orderId, string ibsStatus, double? newLatitude, double? newLongitude);

        void SendAccountConfirmationEmail(Uri confirmationUrl, string clientEmailAddress, string clientLanguageCode);
        void SendAssignedConfirmationEmail(int ibsOrderId, double fare, string vehicleNumber,
            Address pickupAddress, Address dropOffAddress, DateTime pickupDate, DateTime transactionDate,
            SendBookingConfirmationEmail.BookingSettings settings, string clientEmailAddress, string clientLanguageCode);
        void SendBookingConfirmationEmail(int ibsOrderId, string note, Address pickupAddress, Address dropOffAddress,
            DateTime pickupDate, SendBookingConfirmationEmail.BookingSettings settings, string clientEmailAddress, string clientLanguageCode);
        void SendPasswordResetEmail(string password, string clientEmailAddress, string clientLanguageCode);
        void SendReceiptEmail(int ibsOrderId, string vehicleNumber, string driverName, double fare, double toll, double tip,
            double tax, double totalFare, SendReceipt.CardOnFile cardOnFileInfo, Address pickupAddress, Address dropOffAddress, 
            DateTime transactionDate, string clientEmailAddress, string clientLanguageCode);
    }
}