(function(){
    
    var Controller = TaxiHail.SecurityController = TaxiHail.Controller.extend({

        initialize: function() {

            this.ready();
        },

        index: function() {

            return new TaxiHail.GrantAdminAccessView();

        }

    });

}());