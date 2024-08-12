using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanMetricExplorer
{
    /// <summary>
    /// Interaction logic for PlanDVHDetails.xaml
    /// </summary>
    public partial class PlanDVHDetails : UserControl
    {
        public PlanDVHDetails()
        {
            InitializeComponent();
        } 
        public PlanSetup MyPlan { get; set; }
        public PlanningItem MyPI { get; set; }
        public Window zwindow;

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            PlanSetup plan = MyPlan;
            var myPi = MyPI;
            myPi = plan as PlanningItem;
            var referenceDose = plan.TotalDose;
            var structDetails = new List<structList>();
            var bodyDetails = new List<structList>();
            if (plan == null)
            {
                MessageBox.Show("No valid plan selected");
                return;
            }
            if (!plan.IsDoseValid)
            {
                MessageBox.Show("The plan selected has no valid dose.");
                return;
            }
            Structure target = plan.StructureSet.Structures.FirstOrDefault(st => st.Id.Contains("PTV"));

            if (target == null)
            {
                MessageBox.Show("Plan contains no target volume");
                return;
            }
            //to work out the volume of 100% ref dose
            foreach (Structure s in plan.StructureSet.Structures.Where(st => st.DicomType.Contains("EXTERNAL")))
            {
                bodyDetails.Add(new structList()
                {
                    bodyId = s,
                    vol100 = Calculated100(s, myPi, referenceDose),
                    vol50 = Calculated50(s, myPi, referenceDose)
                });
            }

            //PTV Volume list
            var tvDetails = new List<structList>();
            foreach (Structure s in plan.StructureSet.Structures.Where(st => st.DicomType.Contains("PTV")))
            {
                if (!s.IsEmpty)//check for empty structures
                {
                    string str = s.Id.ToString();
                    string pattern = @"\d+(\.\d+)?";
                    MatchCollection matches = Regex.Matches(str, pattern);
                    foreach (Match match in matches)
                    {
                        var d100 = double.Parse(match.Value) * 100;
                        var d50 = double.Parse(match.Value) * 50;
                        tvDetails.Add(new structList()
                        {
                            structureId = s.Id,
                            tvVol = s.Volume,
                            tvDose = double.Parse(match.Value),
                            tvVol100 = CalculatedPTV100Vol( bodyDetails[0].bodyId, myPi, new DoseValue(value: d100, unit: DoseValue.DoseUnit.cGy)),
                            tvVol50 = CalculatedPTV100Vol(bodyDetails[0].bodyId, myPi, new DoseValue(value: d50, unit: DoseValue.DoseUnit.cGy)),
                            HI = CalculateHI(s, myPi).ToString(),
                            gEUD = CalculateGEUD(s, myPi).ToString(),
                            ptvD95 = Calculated95(s, myPi).ToString() + "Gy",
                        });
                    }
                }
            }

          //add OAR/Body to List
            foreach (Structure s in plan.StructureSet.Structures.Where
                (st => st.DicomType.Contains("ORGAN") ||
                st.DicomType.Contains("EXTERNAL") ||
                st.DicomType.Contains("AVOIDANCE")))
            {
                if (!s.IsEmpty)//check for empty structures
                {
                    structDetails.Add(new structList()
                    {
                        structureId = s.Id,
                        structureType = s.DicomType,
                        structureVolume = (s.Volume.ToString("F1") + "cc"),
                        structDMax = CalculateddMax(s, myPi),
                        structDMean = CalculatedMeanDose(s, myPi),
                    });
                }
            }

            //Add PTV vols to datagrid
            for (int i = 0; i < tvDetails.Count; i++)
            {
                double CI = tvDetails[i].tvVol100 / tvDetails[i].tvVol;
                double eqSphereRadiusD100 = Math.Pow((tvDetails[i].tvVol100 * 3) / (4 * Math.PI), (1.0 / 3.0));
                double eqSphereRadiusD50 = Math.Pow((tvDetails[i].tvVol50 * 3) / (4 * Math.PI), (1.0 / 3.0));
                this.dataGrid2.Items.Add(new structList()
                {
                    tvStructureId = tvDetails[i].structureId.ToString(),
                    calcCI = Math.Round(CI, 2).ToString(),
                    calcGI = Math.Round((eqSphereRadiusD50 - eqSphereRadiusD100), 2).ToString(),
                    calcDose = tvDetails[i].tvDose.ToString(),
                    calcVol = tvDetails[i].tvVol100.ToString(),
                    calcVol50 = tvDetails[i].tvVol50.ToString(),
                    HI = tvDetails[i].HI.ToString(),
                    gEUD = tvDetails[i].gEUD.ToString(),
                    ptvD95 = tvDetails[i].ptvD95.ToString(),
                });
            }
            //Add OR to datagrid
            for (int i = 0; i < structDetails.Count; i++)
            {
                this.dataGrid.Items.Add(new structList()
                {
                    structureId = structDetails[i].structureId.ToString(),
                    structureType = structDetails[i].structureType.ToString(),
                    structureVolume = structDetails[i].structureVolume.ToString(),
                    structureDMax = structDetails[i].structDMax.ToString(),
                    structureMean = structDetails[i].structDMean.ToString(),
                });
            }
        }

        public class structList
        {
            public string structureId { get; set; }
            public string structureType { get; set; }
            public string structureVolume { get; set; }
            public string HI { get; set; }
            public string gEUD { get; set; }
            public string ptvD95 { get; set; }
            public double vol100 { get; set; }
            public double vol50 { get; set; }
            public double tvVol { get; set; }  
            public string calcCI { get; set; }
            public string calcGI { get; set; }
            public string calcDose { get; set; }
            public string calcVol { get; set; }
            public string calcVol50 { get; set; }
            public string tvStructureId { get; set;}
            public double tvDose { get; set;}
            public double tvVol100 { get; set;}
            public double tvVol50 { get; set; }
            public Structure bodyId { get; set;}
            public double structDMax { get; set;}
            public string structureDMax { get; set;}
            public double structDMean { get; set;}
            public string structureMean { get; set;}
        }

        private double CalculateddMax(Structure s, PlanningItem pi)
        {
            var dMax = (pi as PlanSetup).GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1).MaxDose.Dose;

            dMax = Math.Round(dMax, 2);
            return dMax;
        }
        private double CalculatedMeanDose(Structure s, PlanningItem pi)
        {
            var dMean = (pi as PlanSetup).GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1).MeanDose.Dose;

            dMean = Math.Round(dMean, 2);
            return dMean;
        }
        private double Calculated95(Structure s, PlanningItem pi)
        {
            double d95 = (pi as PlanSetup).GetDoseAtVolume(s, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;

            d95 = Math.Round(d95, 2);
            return d95;
        }
        private double Calculated100(Structure s, PlanningItem pi, DoseValue dv)
        {double vol100 = (pi as PlanSetup).GetVolumeAtDose(s, dv, VolumePresentation.AbsoluteCm3); //new DoseValue(dv, DoseValue.DoseUnit.cGy)
            vol100 = Math.Round(vol100, 2);
            return vol100;
        }
        private double CalculatedPTV100Vol(Structure s, PlanningItem pi, DoseValue dv)
        {
            double ptvVol100 = (pi as PlanSetup).GetVolumeAtDose(s, dv, VolumePresentation.AbsoluteCm3); //new DoseValue(dv, DoseValue.DoseUnit.cGy)
            ptvVol100 = Math.Round(ptvVol100, 2);
            return ptvVol100;
        }
        private double Calculated50(Structure s, PlanningItem pi, DoseValue dv)
        {
            double ptvVol50 = (pi as PlanSetup).GetVolumeAtDose(s, dv * .5, VolumePresentation.AbsoluteCm3); //new DoseValue(dv, DoseValue.DoseUnit.cGy)
            ptvVol50 = Math.Round(ptvVol50, 2);
            return ptvVol50;
        }
        private double CalculateGEUD(Structure s, PlanningItem pi)
        {
            //collect the DVH
            //if volume is not relative, make sure to normalize over the total volume during geud calculation.
            //double volume = s.Volume;
            //remember plansums must be absolute dose.

            List<geudValues> structgeud = new List<geudValues>();
            structgeud.Add(new geudValues { struc = "Heart", value = 0.5 });
            structgeud.Add(new geudValues { struc = "Cord", value = 20 });
            structgeud.Add(new geudValues { struc = "Parotid", value = 0.5 });
            structgeud.Add(new geudValues { struc = "Bladder", value = 10 });
            structgeud.Add(new geudValues { struc = "Rectum", value = 10 });
            structgeud.Add(new geudValues { struc = "TV", value = 0.5 });
            structgeud.Add(new geudValues { struc = "Stem", value = 20 });
            structgeud.Add(new geudValues { struc = "Brain", value = 20 });
            structgeud.Add(new geudValues { struc = "Eye", value = 0.5 });
            structgeud.Add(new geudValues { struc = "Rectum", value = 10 });

            Structure geudStruct = s;
            double a_value = 1;
            for (int i = 0; i < structgeud.Count; i++) { if (structgeud[i].struc.Contains(s.Id)) { a_value = structgeud[i].value; } }


            DVHData dvh = pi.GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
            if (dvh == null)
            {
                MessageBox.Show("Could not calculate DVH");
                return Double.NaN;
            }
            //we need to get the differential volume from the definition. Loop through Volumes and take the difference with the previous dvhpoint
            double running_sum = 0;
            int counter = 0;
            foreach (DVHPoint dvhp in dvh.CurveData.Skip(1))
            {
                //volume units are in % (divide by 100)
                double vol_diff = Math.Abs(dvhp.Volume - dvh.CurveData[counter].Volume) / 100;
                double dose = dvhp.DoseValue.Dose;
                running_sum += vol_diff * Math.Pow(dose, a_value);
                counter++;
            }
            double geud = Math.Pow(running_sum, 1 / a_value);
            geud = Math.Round(geud, 2);
            return geud;
        }
        public class geudValues
        {
            public string struc { get; set; }
            public double value { get; set; }
        }
        private double CalculateHI(Structure s, PlanningItem pi)
        {
            //hi only needs to be calculated for ptv, so filter those out here.
            if (!s.DicomType.ToUpper().Contains("PTV"))
            {
                return Double.NaN;
            }
            //now check if the planning item is a plansetup or sum.
            if (pi is PlanSetup)
            {
                //plansetups have a method called GetDoseAtVolume.
                if (pi.Dose == null)
                {
                    MessageBox.Show("Plan has no dose");
                    return Double.NaN;
                }
                double d2 = (pi as PlanSetup).GetDoseAtVolume(s, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                double d98 = (pi as PlanSetup).GetDoseAtVolume(s, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                double hi = ((d2 - d98) / (pi as PlanSetup).TotalDose.Dose) * 100;
                hi = Math.Round(hi, 2);
                return hi;
            }
            else if (pi is PlanSum)
            {
                //must manually calculate value from DVH
                DVHData dvh = pi.GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
                if (dvh == null)
                {
                    MessageBox.Show("Could not collect DVH");
                    return Double.NaN;
                }
                double d98 = dvh.CurveData.FirstOrDefault(x => x.Volume <= 98).DoseValue.Dose;
                double d2 = dvh.CurveData.FirstOrDefault(x => x.Volume <= 2).DoseValue.Dose;
                List<double> rx_doses = new List<double>();
                foreach (PlanSetup ps in (pi as PlanSum).PlanSetups)
                {
                    try
                    {
                        rx_doses.Add(ps.TotalDose.Dose);
                    }
                    catch
                    {
                        MessageBox.Show("One of the prescriptions for the plansum is not defined");
                        return Double.NaN;
                    }
                }
                double rx = rx_doses.Sum();
                double hi = ((d2 - d98) / rx) * 100;
                hi = Math.Round(hi, 2);
                return hi;
            }

            else
            {
                MessageBox.Show("Plan not handled correctly");
                return Double.NaN;
            }
        }
    }
}
