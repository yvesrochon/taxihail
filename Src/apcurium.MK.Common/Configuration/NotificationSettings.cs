using System;
using System.ComponentModel.DataAnnotations;

namespace apcurium.MK.Common.Configuration
{
    public class NotificationSettings
    {
        [Key]
        public Guid Id { get; set; }

        [Display(Name="AllowNotifications")]
        public bool Enabled { get; set; }

        public bool? BookingConfirmationEmail { get; set; }
        public bool? ReceiptEmail { get; set; }
        public bool? PromotionUnlockedEmail { get; set; }
        public bool? DriverAssignedPush { get; set; }
        public bool? ConfirmPairingPush { get; set; }
        public bool? NearbyTaxiPush { get; set; }
        public bool? UnpairingReminderPush { get; set; }
        public bool? VehicleAtPickupPush { get; set; }
        public bool? PaymentConfirmationPush { get; set; }
        public bool? PromotionUnlockedPush { get; set; }
        public bool? DriverBailedPush { get; set; }
        public bool? NoShowPush { get; set; }
        public bool? OrderCancellationConfirmationPush { get; set; }
    }
}