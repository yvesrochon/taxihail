using Cirrious.MvvmCross.Binding.Bindings.Target.Construction;
using apcurium.MK.Booking.Mobile.Client.Binding;

namespace apcurium.MK.Booking.Mobile.Client.Controls.Binding
{
    public static class CustomBindingsLoader
    {
        public static void Load(IMvxTargetBindingFactoryRegistry registry)
        {
            FlatCheckBoxBinding.Register(registry);
            MvxUIViewHiddenExTargetBinding.Register(registry);
        }
    }
}

