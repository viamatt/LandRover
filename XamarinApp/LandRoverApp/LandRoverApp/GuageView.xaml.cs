using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LandRoverApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GuageView : ContentView
    {
        Boolean AddedGauge = false;

        //// Structure for storing information about the needle.
        struct HandParams
        {
            public HandParams(double width, double height, double offset) : this()
            {
                Width = width;
                Height = height;
                Offset = offset;
            }

            public double Width { private set; get; }   // fraction of radius
            public double Height { private set; get; }  // ditto
            public double Offset { private set; get; }  // relative to center pivot
        }

        readonly HandParams secondParams = new HandParams(0.02, 1.1, 0.85);

        BoxView[] tickMarks = new BoxView[30];
        BoxView secondHand;

        public GuageView()
        {
            InitializeComponent();
            AddedGauge = false;
            // Sets modal view shadow
            //Modal.BackgroundColor = new Color(0, 0, 0, 0.5);

            //BtnClose.Clicked += (object sender, EventArgs e) =>
            //{
            //    NavigationService.PopCustomModalOnTab();
            //};

            Device.StartTimer(TimeSpan.FromMilliseconds(100), AddGauge);
        }

        bool AddGauge()
        {
            //trick for Android Nexus 5, where the timer is firing twice
            if (AddedGauge)
            {
                return false;
            }
            AbsoluteLayout absoluteLayout = new AbsoluteLayout();

            // Create the tick marks (to be sized and positioned later)
            for (int i = 0; i < tickMarks.Length; i++)
            {
                if (i < 8)
                {
                    tickMarks[i] = new BoxView
                    {
                        Color = Color.Red
                    };
                }
                else if (i >= 8 && i < 16)
                {
                    tickMarks[i] = new BoxView
                    {
                        Color = Color.Maroon
                    };
                }
                else if (i >= 16 && i < 22)
                {
                    tickMarks[i] = new BoxView
                    {
                        Color = Color.Green
                    };
                }
                else
                {
                    tickMarks[i] = new BoxView
                    {
                        Color = Color.Yellow
                    };
                }
                absoluteLayout.Children.Add(tickMarks[i]);
            }


            absoluteLayout.Children.Add(secondHand =
                new BoxView
                {
                    Color = Color.Black
                });

            GaugeHolder.Children.Add(absoluteLayout);

            // Size and position the tick marks.
            Xamarin.Forms.Point center = new Xamarin.Forms.Point(GaugeHolder.Width / 2, GaugeHolder.Height / 2);
            double radius = 0.45 * Math.Min(GaugeHolder.Width, GaugeHolder.Height);

            for (int i = 0; i < tickMarks.Length; i++)
            {
                double size = radius / 8;//(i % 5 == 0 ? 15 : 30);
                double radians = 0.0;
                if (i <= 15)
                {
                    radians = i * 2 * Math.PI / 60;
                }
                else
                {
                    radians = (i + 30) * 2 * Math.PI / 60;
                }
                double x = center.X + radius * Math.Sin(radians) - size / 2;
                double y = center.Y - radius * Math.Cos(radians) - size / 2;

                AbsoluteLayout.SetLayoutBounds(tickMarks[i], new Rectangle(x, y, size, size));


                tickMarks[i].AnchorX = 0.51;        // Anchor settings necessary for Android
                tickMarks[i].AnchorY = 0.51;
                tickMarks[i].Rotation = 180 * radians / Math.PI;
            }

            // Function for positioning needle.
            Action<BoxView, HandParams> Layout = (boxView, handParams) =>
            {
                double width = handParams.Width * radius;
                double height = handParams.Height * radius;
                double offset = handParams.Offset;

                AbsoluteLayout.SetLayoutBounds(boxView,
                    new Rectangle(center.X - 0.5 * width,
                        center.Y - offset * height,
                        width, height));

                boxView.AnchorX = 0.51;
                boxView.AnchorY = handParams.Offset;
            };

            Layout(secondHand, secondParams);
            AddedGauge = true;
            Device.StartTimer(TimeSpan.FromMilliseconds(200), OnTimerTick);

            return false;
        }

        bool OnTimerTick()
        {
            // Set rotation angles for needle
            DateTime dateTime = DateTime.Now;

            // Do an animation for the needle
            double t = dateTime.Millisecond / 1000.0;
            if (t < 0.5)
            {
                t = 0.5 * Easing.SpringIn.Ease(t / 0.5);
            }
            else
            {
                t = 0.5 * (1 + Easing.SpringOut.Ease((t - 0.5) / 0.5));
            }
            int second = dateTime.Second;
            if (second % 2 == 0)
            {
                second = 357;
            }
            else
            {
                second = 2;
            }

            secondHand.Rotation = 6 * (second + t);

            return true;
        }
    }
}



