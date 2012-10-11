﻿// Authentication Service
// Methods: login, logout
// Events: loggedIn, loggedOut
(function () {

    var isLoggedIn = false;


    TaxiHail.auth = _.extend(Backbone.Events, {
        account: null,
        login: function (email, password, url) {
            return $.post('api/auth/credentials', {
                userName: email,
                password: password
            },_.bind(function () {
                isLoggedIn = true;
                
                this.trigger('change', isLoggedIn, url);
            }, this), 'json');
        },

        logout: function () {
            isLogged = false;
            window.localStorage.removeItem('fbId');
            return $.post('api/auth/logout', _.bind(function () {
                isLoggedIn = false;
                this.trigger('change', isLoggedIn);
            }, this), 'json');
        },

        resetPassword: function(email) {
            return $.post('api/account/resetpassword/' + email,{}, function () {}, 'json');
        },
        
        fblogin: function (url) {
          
                            FB.api('/me', _.bind(function (me) {
                                if (me.name) {

                                    $.post('api/auth/credentialsfb', {
                                        userName: me.id,
                                        password: me.id
                                    }, 'json')
                                        .success(_.bind(function () {
                                            isLoggedIn = true;
                                            this.trigger('change', isLoggedIn,url);
                                        }, this))
                                        .error(function (e) {
                                            if (e.status == 401) {
                                                window.localStorage.setItem('fbinfos', JSON.stringify(me));
                                                window.localStorage.setItem('fbId', me.id);
                                                if (url) {
                                                    TaxiHail.app.navigate('signup/'+url, { trigger: true });
                                                } else {
                                                    TaxiHail.app.navigate('signup', { trigger: true });
                                                }
                                            }
                                        });
                                }
                            }, this));
        },
      
        
        isLoggedIn : function() {
            return isLoggedIn;
        },

        initialize: function(account) {
            if(account == null) {
                isLoggedIn = false;
                this.account = new TaxiHail.UserAccount();
            }
            else {
                isLoggedIn = true;
                this.account = new TaxiHail.UserAccount(account.toJSON());
            }

            this.on('change', function(isLoggedIn){
                if(!isLoggedIn) this.account.clear();
            }, this);
        }
    
    });

    $(document).ajaxError(function (e, jqxhr, settings, exception) {
        if (jqxhr.status === 401  /*Unauthorized*/ ) {
             if(isLoggedIn) {
                isLoggedIn = false;
                TaxiHail.auth.trigger('change', isLoggedIn);
             }
         }
    });

} ());