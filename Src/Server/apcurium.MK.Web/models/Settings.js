﻿(function () {

    TaxiHail.Settings = Backbone.Model.extend({

        validate: function (attrs) {
            var errors = [];
            if (attrs.name.length==0) errors.push({ errorCode: 'error.NameRequired' });
            if (!attrs.phone) errors.push({ errorCode: 'error.PhoneRequired' });
            if (attrs.phone && !(/^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$/.test(attrs.phone))) errors.push({ errorCode: 'error.PhoneBadFormat' });
            if (!attrs.passengers) errors.push({ errorCode: 'error.PassengersRequired' });
            if (!attrs.vehicleTypeId) errors.push({ errorCode: 'error.VehicleTypeRequired' });
            if (!attrs.chargeTypeId) errors.push({ errorCode: 'error.ChargeTypeRequired' });
            

            if (errors.length) return { errors: errors };
        }
    });

}());