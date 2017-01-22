using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Phone.Maps.Services;

namespace LocationTracker
{
    public partial class TrackerPivotPage : PhoneApplicationPage
    {
        Geolocator geolocator = null;
        bool tracking = false;
        ProgressIndicator pi;
        MapLayer PushpinMapLayer;
        List<GeoCoordinate> coordinates;


        public TrackerPivotPage()
        {
            InitializeComponent();

            pi = new ProgressIndicator();
            pi.IsIndeterminate = true;
            pi.IsVisible = false;
            coordinates = new List<GeoCoordinate>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //Creating a MapLayer and adding the MapOverlay to it
            PushpinMapLayer = new MapLayer();
            MyMap.Layers.Add(PushpinMapLayer);

            base.OnNavigatedTo(e);
        }

        private void TrackLocation_Click(object sender, RoutedEventArgs e)
        {
            if (!tracking)
            {
                geolocator = new Geolocator();
                geolocator.DesiredAccuracy = PositionAccuracy.High;
                geolocator.MovementThreshold = 50; // The units are meters.

                geolocator.StatusChanged += geolocator_StatusChanged;
                geolocator.PositionChanged += geolocator_PositionChanged;

                tracking = true;
                TrackLocationButton.Content = "stop tracking";

                this.MyMap.ResolveCompleted += MapResolveCompleted;
            }
            else
            {
                geolocator.PositionChanged -= geolocator_PositionChanged;
                geolocator.StatusChanged -= geolocator_StatusChanged;
                geolocator = null;

                tracking = false;
                TrackLocationButton.Content = "track location";
                StatusTextBlock.Text = "stopped";

                this.MyMap.ResolveCompleted -= MapResolveCompleted;
            }
        }


        void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            string status = "";

            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    // the application does not have the right capability or the location master switch is off
                    status = "location is disabled in phone settings";
                    break;
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    status = "initializing";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    status = "no data";
                    break;
                case PositionStatus.Ready:
                    // the location service is generating geopositions as specified by the tracking parameters
                    status = "ready";
                    break;
                case PositionStatus.NotAvailable:
                    status = "not available";
                    // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
                    break;
                case PositionStatus.NotInitialized:
                    // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state

                    break;
            }

            Dispatcher.BeginInvoke(() =>
            {
                StatusTextBlock.Text = status;
            });
        }

        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Geocoordinate coord = args.Position.Coordinate;
            coordinates.Add(coord.ToGeoCoordinate());
            Dispatcher.BeginInvoke(() =>
            {
                LatitudeTextBlock.Text = args.Position.Coordinate.Latitude.ToString("0.00");
                LongitudeTextBlock.Text = args.Position.Coordinate.Longitude.ToString("0.00");

                // Unfortunately, the Location API works with Windows.Devices.Geolocation.Geocoordinate objeccts
                // and the Maps controls use System.Device.Location.GeoCoordinate objects so we have to do a
                // conversion before we do anything with it on the map
                GeoCoordinate positionCoord = new GeoCoordinate()
                {
                    Altitude = args.Position.Coordinate.Altitude.HasValue ? args.Position.Coordinate.Altitude.Value : 0.0,
                    Course = args.Position.Coordinate.Heading.HasValue ? args.Position.Coordinate.Heading.Value : 0.0,
                    HorizontalAccuracy = args.Position.Coordinate.Accuracy,
                    Latitude = args.Position.Coordinate.Latitude,
                    Longitude = args.Position.Coordinate.Longitude,
                    Speed = args.Position.Coordinate.Speed.HasValue ? args.Position.Coordinate.Speed.Value : 0.0,
                    VerticalAccuracy = args.Position.Coordinate.AltitudeAccuracy.HasValue ? args.Position.Coordinate.AltitudeAccuracy.Value : 0.0
                };

                // Center the map on the new location
                this.MyMap.Center = positionCoord;

                this.MyMap.ZoomLevel = 17;

                //// Shorthand way of doing the above
                //GeoCoordinateEx gex = new GeoCoordinateEx();
                //gex = args.Position.Coordinate;
                //this.MyMap.Center = gex.GeoCoordinate;
                //// ... or using the Map extension method:
                //this.MyMap.SetCenter(args.Position.Coordinate);    


                // Draw a pushpin
                DrawPushpin(positionCoord);

                // Show progress indicator while map resolves to new position
                pi.IsVisible = true;
                pi.IsIndeterminate = true;
                pi.Text = "Resolving...";
                SystemTray.SetProgressIndicator(this, pi);
            });
        }

        private void MapResolveCompleted(object sender, MapResolveCompletedEventArgs e)
        {
            // Hide progress indicator
            pi.IsVisible = false;
            pi.IsIndeterminate = false;
            SystemTray.SetProgressIndicator(this, null);
        }

        private void DrawPushpin(GeoCoordinate coord)
        {
            var aPushpin = CreatePushpinObject();

            //Creating a MapOverlay and adding the Pushpin to it.
            MapOverlay MyOverlay = new MapOverlay();
            MyOverlay.Content = aPushpin;
            MyOverlay.GeoCoordinate = coord;
            MyOverlay.PositionOrigin = new Point(0, 0.5);

            // Add the MapOverlay containing the pushpin to the MapLayer
            this.PushpinMapLayer.Add(MyOverlay);
        }

        private void pace_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void distance_Click(object sender, RoutedEventArgs e)
        {

            double sum = 0;

            for (int i = 1; i < coordinates.Count; i++)
            {
                sum += coordinates[i].GetDistanceTo(coordinates[i - 1]);
            }
            distance1.Text = sum.ToString();


        }




        private Grid CreatePushpinObject()
        {
            //Creating a Grid element.
            Grid MyGrid = new Grid();
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.Background = new SolidColorBrush(Colors.Transparent);

            //Creating a Rectangle
            Rectangle MyRectangle = new Rectangle();
            MyRectangle.Fill = new SolidColorBrush(Colors.DarkGray);
            MyRectangle.Height = 40;
            MyRectangle.Width = 5;
            MyRectangle.SetValue(Grid.RowProperty, 1);
            MyRectangle.SetValue(Grid.ColumnProperty, 0);

            //Adding the Rectangle to the Grid
            MyGrid.Children.Add(MyRectangle);

            //Creating a circle
            Ellipse MyEllipse = new Ellipse();
            MyEllipse.Fill = new SolidColorBrush(Colors.Red);
            MyEllipse.Height = 30;
            MyEllipse.Width = 30;

            MyEllipse.SetValue(Grid.RowProperty, 0);
            MyEllipse.SetValue(Grid.ColumnProperty, 0);

            //Adding the Polygon to the Grid
            MyGrid.Children.Add(MyEllipse);
            return MyGrid;
        }

        private void heightTxt_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

    }
}