﻿(function () {

    TaxiHail.BookingStatusView = TaxiHail.TemplatedView.extend({
        events: {
            'click [data-action=cancel]': 'cancel'
        },

        initialize: function() {

            var status = this.model.getStatus();
            this.interval = window.setInterval(_.bind(status.fetch, status), 3000);
            status.on('change:iBSStatusId', this.render, this);

        },

        render: function() {

            // Close popover if it is open
            // Otherwise it will stay there forever
            this.$('[data-action=call]').popover('hide');

            this.$el.html(this.renderTemplate(this.model.getStatus().toJSON()));

            this.$('[data-action=call]').popover({
                    title:"Call me maybe",
                    content:"514 692 6813"
                });

            return this;
        },

        remove: function() {

            // Close popover if it is open
            // Otherwise it will stay there forever
            this.$('[data-action=call]').popover('hide');

            this.$el.remove();

            // Stop polling for Order Status updates
            window.clearInterval(this.interval);

        },

        cancel: function(e) {
            e.preventDefault();
            this.model.cancel()
                .done(function(){
                    TaxiHail.app.navigate('', { trigger: true });
                });
        }


    });
}());