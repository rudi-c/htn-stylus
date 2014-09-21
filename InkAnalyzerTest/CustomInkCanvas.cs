using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InkAnalyzerTest
{
    public class CustomInkCanvas : InkCanvas
    {
        InkCanvas invoker;

        public CustomInkCanvas() { }

        public void hookup(InkCanvas invoker)
        {
            this.invoker = invoker;
            invoker.StylusButtonDown += invoker_StylusButtonDown;
            invoker.StylusButtonUp += invoker_StylusButtonUp;
            invoker.StylusDown += invoker_StylusDown;
            invoker.StylusEnter += invoker_StylusEnter;
            invoker.StylusInAirMove += invoker_StylusInAirMove;
            invoker.StylusInRange += invoker_StylusInRange;
            invoker.StylusLeave += invoker_StylusLeave;
            invoker.StylusMove += invoker_StylusMove;
            invoker.StylusOutOfRange += invoker_StylusOutOfRange;
            invoker.StylusSystemGesture += invoker_StylusSystemGesture;
            invoker.StylusUp += invoker_StylusUp;
        }

        void invoker_StylusUp(object sender, StylusEventArgs e)
        {
            this.OnStylusUp(e);
        }

        void invoker_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
        {
            this.OnStylusSystemGesture(e);
        }

        void invoker_StylusOutOfRange(object sender, StylusEventArgs e)
        {
            this.OnStylusOutOfRange(e);
        }

        void invoker_StylusMove(object sender, StylusEventArgs e)
        {
            this.OnStylusMove(e);
        }

        void invoker_StylusLeave(object sender, StylusEventArgs e)
        {
            this.OnStylusLeave(e);
        }

        void invoker_StylusInAirMove(object sender, StylusEventArgs e)
        {
            this.OnStylusInAirMove(e);
        }

        void invoker_StylusInRange(object sender, StylusEventArgs e)
        {
            this.OnStylusInRange(e);
        }

        void invoker_StylusEnter(object sender, StylusEventArgs e)
        {
            this.OnStylusEnter(e);
        }

        void invoker_StylusDown(object sender, StylusDownEventArgs e)
        {
            this.OnStylusDown(e);
        }

        void invoker_StylusButtonUp(object sender, StylusButtonEventArgs e)
        {
            this.OnStylusButtonUp(e);
        }

        void invoker_StylusButtonDown(object sender, StylusButtonEventArgs e)
        {
            this.OnStylusButtonDown(e);
        }

        protected override void OnStylusButtonDown(StylusButtonEventArgs e)
        {
            base.OnStylusButtonDown(e);
        }
        protected override void OnStylusButtonUp(StylusButtonEventArgs e)
        {
            base.OnStylusButtonUp(e);
        }
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            base.OnStylusDown(e);
        }
        protected override void OnStylusEnter(StylusEventArgs e)
        {
            base.OnStylusEnter(e);
        }
        protected override void OnStylusInAirMove(StylusEventArgs e)
        {
            base.OnStylusInAirMove(e);
        }
        protected override void OnStylusInRange(StylusEventArgs e)
        {
            base.OnStylusInRange(e);
        }
        protected override void OnStylusLeave(StylusEventArgs e)
        {
            base.OnStylusLeave(e);
        }
        protected override void OnStylusMove(StylusEventArgs e)
        {
            base.OnStylusMove(e);
        }
        protected override void OnStylusOutOfRange(StylusEventArgs e)
        {
            base.OnStylusOutOfRange(e);
        }
        protected override void OnStylusSystemGesture(StylusSystemGestureEventArgs e)
        {
            base.OnStylusSystemGesture(e);
        }
        protected override void OnStylusUp(StylusEventArgs e)
        {
            base.OnStylusUp(e);
        }
    }
}
