using System;
using Cirrious.MvvmCross.Commands;
using Cirrious.MvvmCross.Interfaces.Commands;
using apcurium.MK.Booking.Mobile.ViewModels;
using apcurium.MK.Common.Extensions;

namespace apcurium.MK.Booking.Mobile.Models
{
    public class RatingModel : BaseViewModel
    {
        public Guid RatingTypeId { get; set; }
        public string RatingTypeName { get; set; }
        public int Score { get; set; }

        private bool _madSelected;
        public bool MadSelected
        {
            get { return _madSelected; }
            set
            {
                _madSelected = value;
                FirePropertyChanged(() => MadSelected);
            }
        }

        private bool _unhappySelected;
        public bool UnhappySelected
        {
            get { return _unhappySelected; }
            set
            {
                _unhappySelected = value;
                FirePropertyChanged(() => UnhappySelected);
            }
        }

        private bool _neutralSelected;
        public bool NeutralSelected
        {
            get { return _neutralSelected; }
            set
            {
                _neutralSelected = value;
                FirePropertyChanged(() => NeutralSelected);
            }
        }

        private bool _happySelected;
        public bool HappySelected
        {
            get { return _happySelected; }
            set
            {
                _happySelected = value;
                FirePropertyChanged(() => HappySelected);
            }
        }

        private bool _ecstaticSelected;
        public bool EcstaticSelected
        {
            get { return _ecstaticSelected; }
            set
            {
                _ecstaticSelected = value;
                FirePropertyChanged(() => EcstaticSelected);
            }
        }

        private enum RatingState { Mad = 1, Unhappy = 2, Neutral = 3, Happy = 4, Ecstatic = 5 }

        private void DeselectAllState()
        {
            EcstaticSelected = false;
            HappySelected = false;
            NeutralSelected = false;
            UnhappySelected = false;
            MadSelected = false;
        }

        public IMvxCommand SetRateCommand
        {
            get
            {
                return new MvxRelayCommand<object>(param => param.Maybe(tag =>
                                                                            {
                                                                                RatingState state;
                                                         if (Enum.TryParse(tag.ToString(), true, out state))
                                                         {
                                                             DeselectAllState();
                                                             Score = (int)state;
                                                             switch (state)
                                                             {
                                                                 case RatingState.Mad:
                                                                     MadSelected = true;
                                                                     break;
                                                                 case RatingState.Unhappy:
                                                                     UnhappySelected = true;
                                                                     break;
                                                                 case RatingState.Neutral:
                                                                     NeutralSelected = true;
                                                                     break;
                                                                 case RatingState.Happy:
                                                                     HappySelected = true;
                                                                     break;
                                                                 case RatingState.Ecstatic:
                                                                     EcstaticSelected = true;
                                                                     break;
                                                             }
                                                         }
                                                                            }));
            }
        }
    }
}