﻿(function () {
    TaxiHail.OrderHistoryDetailView = TaxiHail.TemplatedView.extend({
        events: {
            "click [data-action=rebook]": "rebook",
            "click [data-action=cancel]": "cancel",
            "click [data-action=remove]": "remove",
            "click [data-action=send-receipt]": "sendReceipt"
        },

        initialize: function () {

            this.model.on('change', this.render, this);
            
            this.model.id = this.model.get('id');
            this.model.getStatus().fetch({
                success: _.bind(function(model) {
                    var data = model.toJSON();
                    if (!model.get('ibsStatusDescription')) {
                        model.set('ibsStatusDescription', TaxiHail.localize('Processing'));
                    }
                    this.render();
                },this)
            });

            
        },

        render: function () {

            var data = this.model.toJSON(),
                status = this.model.getStatus();

            _.extend(data, {
                orderStatus:     status.toJSON(),
                canCancel:       status.isActive(),
                canDelete:      !status.isActive(),
                canPrintReceipt: status.canSendReceipt()
            });

            this.$el.html(this.renderTemplate(data));
            
            return this;
        },
        
        rebook : function () {
            this.model.saveLocal();
            TaxiHail.app.navigate('confirmationbook', { trigger: true });
        },
        
        cancel : function () {
            if (this.model.getStatus().isActive()) {
                TaxiHail.confirm({
                    title: this.localize('Cancel Order'),
                    message: this.localize('modal.cancelOrder.message'),
                    cancelButton: this.localize('modal.cancelOrder.cancelButton')
                }).on('ok', function () {
                    this.model.cancel();
                    this.model.trigger('cancel', this);
                }, this);
            }
        },
        
        remove: function () {
            if (!this.model.getStatus().isActive()) {
                TaxiHail.confirm({
                    title: this.localize('Remove Order'),
                    message: this.localize('modal.removeOrder.message')
                }).on('ok', function () {
                    this.model.destroy();
                }, this);
            }
        },

        sendReceipt: function() {
            if (this.model.getStatus().isCompleted()) {
                
                var $button = this.$('[data-action=send-receipt]');
                $button.button('loading');
                
                TaxiHail.postpone(function() {
                    this.model.sendReceipt()
                        .done(_.bind(function(){
                            $button.addClass('btn-success').text(this.localize('Receipt Sent'));
                        }, this))
                        .fail(_.bind(function(){
                            $button.text(this.localize('Cannot Send Receipt'));
                        }, this));
                }, this)();
            }
        }
    });

}());
