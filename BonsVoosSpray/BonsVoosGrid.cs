using com.drew.imaging.jpg;
using com.drew.metadata;
using com.drew.metadata.exif;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using log4net;
using MissionPlanner.ArduPilot;
using MissionPlanner.GCSViews;
using MissionPlanner.Maps;
using MissionPlanner.Utilities;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;

namespace MissionPlanner.SprayingMission
{
    public partial class BonsVoosGrid : Form
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Variables
        const double rad2deg = (180 / Math.PI);
        const double deg2rad = (1.0 / rad2deg);

        private SprayingMissionPlugin plugin;
        static public Object thisLock = new Object();

        GMapOverlay routesOverlay;
        GMapOverlay kmlpolygonsoverlay;
        List<PointLatLngAlt> list = new List<PointLatLngAlt>();
        List<PointLatLngAlt> grid;
        bool loadedfromfile = false;
        bool loading = false;

        Dictionary<string, sprayerinfo> sprayer = new Dictionary<string, sprayerinfo>();
        Dictionary<string, camerainfo> cameras = new Dictionary<string, camerainfo>();

        public string DistUnits = "";
        public string inchpixel = "";
        public string feet_fovH = "";
        public string feet_fovV = "";

        internal PointLatLng MouseDownStart = new PointLatLng();
        internal PointLatLng MouseDownEnd;
        internal PointLatLngAlt CurrentGMapMarkerStartPos;
        PointLatLng currentMousePosition;
        GMapMarker marker;
        GMapMarker CurrentGMapMarker = null;
        GMapMarkerOverlapCount GMapMarkerOverlap = new GMapMarkerOverlapCount(PointLatLng.Empty);
        int CurrentGMapMarkerIndex = 0;
        bool isMouseDown = false;
        bool isMouseDraging = false;
        public BonsVoosGrid()
        {
            InitializeComponent();
        }
        private void BonsVoosGrid_Load_1(object sender, EventArgs e)
        {
            domainUpDown1_ValueChanged(this, null);
            TRK_zoom.Value = (float)map.Zoom;
        }


        // Do Work
        private async void domainUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (loading)
                return;

            if (CMB_camera.Text != "")
            {
                doCalc();
            }

            // new grid system test

            if (chk_Corridor.Checked)
            {
                grid = await Utilities.Grid.CreateCorridorAsync(list, CurrentState.fromDistDisplayUnit((double)NUM_altitude.Value),
                    (double)NUM_Distance.Value, (double)NUM_spacing.Value, (double)NUM_angle.Value,
                    (double)NUM_overshoot.Value, (double)NUM_overshoot2.Value,
                    (Utilities.Grid.StartPosition)Enum.Parse(typeof(Utilities.Grid.StartPosition), CMB_startfrom.Text), false,
                    (float)NUM_Lane_Dist.Value, (float)num_corridorwidth.Value, (float)NUM_leadin.Value).ConfigureAwait(true);
            }
            else if (chk_spiral.Checked)
            {
                grid = await Utilities.Grid.CreateRotaryAsync(list, CurrentState.fromDistDisplayUnit((double)NUM_altitude.Value),
                    (double)NUM_Distance.Value, (double)NUM_spacing.Value, (double)NUM_angle.Value,
                    (double)NUM_overshoot.Value, (double)NUM_overshoot2.Value,
                    (Utilities.Grid.StartPosition)Enum.Parse(typeof(Utilities.Grid.StartPosition), CMB_startfrom.Text), false,
                    (float)NUM_Lane_Dist.Value, (float)NUM_leadin.Value, MainV2.comPort.MAV.cs.PlannedHomeLocation).ConfigureAwait(true);
            }
            else
            {
                grid = await Utilities.Grid.CreateGridAsync(list, CurrentState.fromDistDisplayUnit((double)NUM_altitude.Value),
                    (double)NUM_Distance.Value, (double)NUM_spacing.Value, (double)NUM_angle.Value,
                    (double)NUM_overshoot.Value, (double)NUM_overshoot2.Value,
                    (Utilities.Grid.StartPosition)Enum.Parse(typeof(Utilities.Grid.StartPosition), CMB_startfrom.Text), false,
                    (float)NUM_Lane_Dist.Value, (float)NUM_leadin.Value, MainV2.comPort.MAV.cs.PlannedHomeLocation).ConfigureAwait(true);
            }

            map.HoldInvalidation = true;

            routesOverlay.Routes.Clear();
            routesOverlay.Polygons.Clear();
            routesOverlay.Markers.Clear();

            GMapMarkerOverlap.Clear();

            if (grid.Count == 0)
            {
                map.ZoomAndCenterMarkers("routes");
                return;
            }

            if (chk_crossgrid.Checked)
            {
                // add crossover
                Utilities.Grid.StartPointLatLngAlt = grid[grid.Count - 1];

                grid.AddRange(await Utilities.Grid.CreateGridAsync(list, CurrentState.fromDistDisplayUnit((double)NUM_altitude.Value),
                    (double)NUM_Distance.Value, (double)NUM_spacing.Value, (double)NUM_angle.Value + 90.0,
                    (double)NUM_overshoot.Value, (double)NUM_overshoot2.Value,
                    Utilities.Grid.StartPosition.Point, false,
                    (float)NUM_Lane_Dist.Value, (float)NUM_leadin.Value, MainV2.comPort.MAV.cs.PlannedHomeLocation).ConfigureAwait(true));
            }

            if (CHK_boundary.Checked)
                AddDrawPolygon();

            if (grid.Count == 0)
            {
                map.ZoomAndCenterMarkers("routes");
                return;
            }

            int strips = 0;
            int images = 0;
            int a = 1;
            PointLatLngAlt prevprevpoint = grid[0];
            PointLatLngAlt prevpoint = grid[0];
            // distance to/from home
            double routetotal = grid.First().GetDistance(MainV2.comPort.MAV.cs.PlannedHomeLocation) / 1000.0 +
                               grid.Last().GetDistance(MainV2.comPort.MAV.cs.PlannedHomeLocation) / 1000.0;
            List<PointLatLng> segment = new List<PointLatLng>();
            double maxgroundelevation = double.MinValue;
            double mingroundelevation = double.MaxValue;
            double startalt = plugin.Host.cs.HomeAlt;

            foreach (var item in grid)
            {
                double currentalt = srtm.getAltitude(item.Lat, item.Lng).alt;
                mingroundelevation = Math.Min(mingroundelevation, currentalt);
                maxgroundelevation = Math.Max(maxgroundelevation, currentalt);

                prevprevpoint = prevpoint;

                if (item.Tag == "M")
                {
                    images++;

                    if (CHK_internals.Checked)
                    {
                        routesOverlay.Markers.Add(new GMarkerGoogle(item, GMarkerGoogleType.green) { ToolTipText = a.ToString(), ToolTipMode = MarkerTooltipMode.OnMouseOver });
                        a++;

                        segment.Add(prevpoint);
                        segment.Add(item);
                        prevpoint = item;
                    }
                    try
                    {
                        if (TXT_fovH.Text != "")
                        {
                            if (CHK_footprints.Checked)
                            {
                                double fovh = double.Parse(TXT_fovH.Text);
                                double fovv = double.Parse(TXT_fovV.Text);

                                getFOV(item.Alt + startalt - currentalt, ref fovh, ref fovv);

                                double startangle = 0;

                                if (!CHK_camdirection.Checked)
                                {
                                    startangle += 90;
                                }

                                double angle1 = startangle - (Math.Sin((fovh / 2.0) / (fovv / 2.0)) * rad2deg);
                                double dist1 = Math.Sqrt(Math.Pow(fovh / 2.0, 2) + Math.Pow(fovv / 2.0, 2));

                                double bearing = (double)NUM_angle.Value;

                                if (CHK_copter_headinghold.Checked)
                                {
                                    bearing = Convert.ToInt32(TXT_headinghold.Text);
                                }

                                if (chk_Corridor.Checked)
                                    bearing = prevprevpoint.GetBearing(item);

                                double fovha = 0;
                                double fovva = 0;
                                getFOVangle(ref fovha, ref fovva);
                                var itemcopy = new PointLatLngAlt(item);
                                itemcopy.Alt += startalt;
                                var temp = ImageProjection.calc(itemcopy, 0, 0, bearing + startangle, fovha, fovva);

                                List<PointLatLng> footprint = new List<PointLatLng>();
                                footprint.Add(temp[0]);
                                footprint.Add(temp[1]);
                                footprint.Add(temp[2]);
                                footprint.Add(temp[3]);

                                GMapPolygon poly = new GMapPolygon(footprint, a.ToString());
                                poly.Stroke =
                                    new Pen(Color.FromArgb(250 - ((a * 5) % 240), 250 - ((a * 3) % 240), 250 - ((a * 9) % 240)), 1);
                                poly.Fill = new SolidBrush(Color.Transparent);

                                GMapMarkerOverlap.Add(poly);

                                routesOverlay.Polygons.Add(poly);
                                a++;
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    if (item.Tag != "SM" && item.Tag != "ME")
                        strips++;

                    if (CHK_markers.Checked)
                    {
                        var marker = new GMapMarkerWP(item, a.ToString()) { ToolTipText = a.ToString(), ToolTipMode = MarkerTooltipMode.OnMouseOver };
                        routesOverlay.Markers.Add(marker);
                    }

                    segment.Add(prevpoint);
                    segment.Add(item);
                    prevpoint = item;
                    a++;
                }
                GMapRoute seg = new GMapRoute(segment, "segment" + a.ToString());
                seg.Stroke = new Pen(Color.Yellow, 4);
                seg.Stroke.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                seg.IsHitTestVisible = true;
                routetotal = routetotal + (float)seg.Distance;
                if (CHK_grid.Checked)
                {
                    routesOverlay.Routes.Add(seg);
                }
                else
                {
                    seg.Dispose();
                }

                segment.Clear();
            }

            if (CHK_footprints.Checked)
                routesOverlay.Markers.Add(GMapMarkerOverlap);
            /*      Old way of drawing route, incase something breaks using segments
            GMapRoute wproute = new GMapRoute(list2, "GridRoute");
            wproute.Stroke = new Pen(Color.Yellow, 4);
            if (chk_grid.Checked)
                routesOverlay.Routes.Add(wproute);
            */

            // turn radrad = tas^2 / (tan(angle) * G)
            float v_sq = (float)(((float)NUM_UpDownFlySpeed.Value / CurrentState.multiplierspeed) * ((float)NUM_UpDownFlySpeed.Value / CurrentState.multiplierspeed));
            float turnrad = (float)(v_sq / (float)(9.808f * Math.Tan(35 * deg2rad)));

            // Update Stats 
            if (DistUnits == "Feet")
            {
                // Area
                float area = (float)calcpolygonarea(list) * 10.7639f; // Calculate the area in square feet
                lbl_area.Text = area.ToString("#") + " ft^2";
                if (area < 21780f)
                {
                    lbl_area.Text = area.ToString("#") + " ft^2";
                }
                else
                {
                    area = area / 43560f;
                    if (area < 640f)
                    {
                        lbl_area.Text = area.ToString("0.##") + " acres";
                    }
                    else
                    {
                        area = area / 640f;
                        lbl_area.Text = area.ToString("0.##") + " miles^2";
                    }
                }

                // Distance
                double distance = routetotal * 3280.8399; // Calculate the distance in feet
                if (distance < 5280f)
                {
                    lbl_distance.Text = distance.ToString("#") + " ft";
                }
                else
                {
                    distance = distance / 5280f;
                    lbl_distance.Text = distance.ToString("0.##") + " miles";
                }

                lbl_spacing.Text = (NUM_spacing.Value * 3.2808399m).ToString("#.#") + " ft";
                lbl_grndres.Text = inchpixel;
                lbl_distbetweenlines.Text = (NUM_Distance.Value * 3.2808399m).ToString("0.##") + " ft";
                lbl_footprint.Text = feet_fovH + " x " + feet_fovV + " ft";
                lbl_turnrad.Text = (turnrad * 2 * 3.2808399).ToString("0") + " ft";
                lbl_gndelev.Text = (mingroundelevation * 3.2808399).ToString("0") + "-" + (maxgroundelevation * 3.2808399).ToString("0") + " ft";
            }
            else
            {
                // Meters
                lbl_area.Text = calcpolygonarea(list).ToString("#") + " m^2";
                lbl_distance.Text = routetotal.ToString("0.##") + " km";
                lbl_spacing.Text = NUM_spacing.Value.ToString("0.#") + " m";
                lbl_grndres.Text = TXT_cmpixel.Text;
                lbl_distbetweenlines.Text = NUM_Distance.Value.ToString("0.##") + " m";
                lbl_footprint.Text = TXT_fovH.Text + " x " + TXT_fovV.Text + " m";
                lbl_turnrad.Text = (turnrad * 2).ToString("0") + " m";
                lbl_gndelev.Text = mingroundelevation.ToString("0") + "-" + maxgroundelevation.ToString("0") + " m";

            }

            try
            {
                if (TXT_cmpixel.Text != "")
                {
                    // speed m/s
                    var speed = ((float)NUM_UpDownFlySpeed.Value / CurrentState.multiplierspeed);
                    // cmpix cm/pixel
                    var cmpix = float.Parse(TXT_cmpixel.Text.TrimEnd(new[] { 'c', 'm', ' ' }));
                    // m pix = m/pixel
                    var mpix = cmpix * 0.01;
                    // gsd / 2.0
                    var minmpix = mpix / 2.0;
                    // min sutter speed
                    var minshutter = speed / minmpix;
                    lbl_minshutter.Text = "1/" + (minshutter - minshutter % 1).ToString();
                }
            }
            catch { }

            double flyspeedms = CurrentState.fromSpeedDisplayUnit((double)NUM_UpDownFlySpeed.Value);

            lbl_pictures.Text = images.ToString();
            lbl_strips.Text = ((int)(strips / 2)).ToString();
            double seconds = ((routetotal * 1000.0) / ((flyspeedms) * 0.8));
            // reduce flying speed by 20 %
            lbl_flighttime.Text = secondsToNice(seconds);
            seconds = ((routetotal * 1000.0) / (flyspeedms));
            lbl_photoevery.Text = secondsToNice(((double)NUM_spacing.Value / flyspeedms));
            map.HoldInvalidation = false;
            if (!isMouseDown && sender != NUM_angle)
                map.ZoomAndCenterMarkers("routes");

            CalcHeadingHold();

            map.Invalidate();
        }


        //FERNANDO - REVISÃO
        private void AddWP(double Lng, double Lat, double Alt, string tag, object gridobject = null)
        {

            // fernando 14-10-2020 - criar um novo checked para forçar a seguir a linha no azimute para o drone fazer as curvas antes de seguir.
            //  if (CHK_copter_headinghold.Checked)
            //  {

            //      plugin.Host.AddWPtoList(MAVLink.MAV_CMD.CONDITION_YAW, Convert.ToInt32(TXT_headinghold.Text), 0, 0, 0, 0, 0, 0, gridobject);
            //  }

            if (CHK_copter_headingline.Checked)
            {

                plugin.Host.AddWPtoList(MAVLink.MAV_CMD.CONDITION_YAW, Convert.ToInt32(TXT_headinghold.Text), 0, 0, 0, 0, 0, 0, gridobject);
            }
            //  fernando

            if (CHK_copter_headinghold.Checked)
            {

                plugin.Host.AddWPtoList(MAVLink.MAV_CMD.CONDITION_YAW, Convert.ToInt32(TXT_headinghold.Text), 0, 0, 0, 0, 0, 0, gridobject);
            }

            if (NUM_copter_delay.Value > 0)
            {
                plugin.Host.AddWPtoList(MAVLink.MAV_CMD.WAYPOINT, (double)NUM_copter_delay.Value, 0, 0, 0, Lng, Lat, Alt * CurrentState.multiplierdist, gridobject);
            }
            else
            {
                if ((tag == "S" || tag == "SM") && chk_spline.Checked)
                {
                    plugin.Host.AddWPtoList(MAVLink.MAV_CMD.SPLINE_WAYPOINT, 0, 0, 0, 0, Lng, Lat, (int)(Alt * CurrentState.multiplierdist), gridobject);
                }
                else
                {
                    plugin.Host.AddWPtoList(MAVLink.MAV_CMD.WAYPOINT, 0, 0, 0, 0, Lng, Lat, (int)(Alt * CurrentState.multiplierdist), gridobject);
                }
            }
        }


    }
}
