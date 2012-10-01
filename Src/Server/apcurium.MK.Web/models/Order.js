﻿(function () {

    TaxiHail.Order = Backbone.Model.extend({

        idAttribute: 'orderId',
        urlRoot: 'api/account/orders',

        save: function(key, value, options) {

            if (_.isObject(key) || key == null) {
                attrs = key;
                options = value;
            } else {
                attrs = {};
                attrs[key] = value;
            }
            options = options ? _.clone(options) : {};

            var success = options.success;
            options.success = function(model, resp) {
                TaxiHail.orderService.setCurrentOrder(model);
                if(success) {
                    success(model, resp);
                }
            };

            if (_.isObject(key) || key == null) {
                Backbone.Model.prototype.save.call(this, key, options);
            } else {
                Backbone.Model.prototype.save.call(this, key, value, options);
            }
        },

        saveLocal: function() {

            TaxiHail.orderService.setCurrentOrder(this);
        },

        cancel: function() {

            return $.post(this.url() + '/cancel', {
                orderId: this.id
            }, function(){}, 'json');

        },
        getStatus: function() {
            return this._status || (this._status = new TaxiHail.OrderStatus({
                id: this.id
            }));
        },

        isValidAddress: function(attr) {
            var value = this.get(attr);
            return !!(value && value.fullAddress && value.latitude && value.longitude);
        }
    });

}());