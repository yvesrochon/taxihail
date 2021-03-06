﻿(function () {

    TaxiHail.TemplatedView = Backbone.View.extend({

        // template and resourceSet are set by TaxiHail.Loader
        template: null,
        resourceSet: null,

        renderTemplate: function (context) {
            context = _.extend(context || {}, {
                resourceSet: this.resourceSet
            });

            return this.template(context);
        },

        localize: function (resourceName) {
            return TaxiHail.localize(resourceName, this.resourceSet);
        }

    });

}());