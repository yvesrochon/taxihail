﻿using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using apcurium.MK.Common.Entity;
using Cirrious.CrossCore.Converters;
using System.Linq;

namespace apcurium.MK.Booking.Mobile.Client.Converters
{
    public class CreditCardNumberToTypeImageConverter : MvxValueConverter
    {
        private const string Visa = "Visa";
        private const string MasterCard = "MasterCard";
        private const string Amex = "Amex";
        private const string CreditCardGeneric = "Credit Card Generic";
        private const string VisaElectron = "Visa Electron";
        private List<ListItem> _creditCardCompanies;

        public CreditCardNumberToTypeImageConverter()
        {
            _creditCardCompanies = new List<ListItem>
                {
                    new ListItem {Display = Visa, Image = "visa.png"},
                    new ListItem {Display = MasterCard, Image = "mastercard.png"},
                    new ListItem {Display = Amex, Image = "amex.png"},
                    new ListItem {Display = VisaElectron, Image = "visa_electron.png"},
                    new ListItem {Display = CreditCardGeneric, Image =  "credit_card_generic.png"}
                };
        }

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var type = _creditCardCompanies.FirstOrDefault(x=>x.Display == value.ToString());
            return type.Image;
        }
    }
}
