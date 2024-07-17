using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
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

            var structDetails = new List<structList>();
            foreach (Structure s in plan.StructureSet.Structures.Where
                (st => st.DicomType.Contains("TV") ||
                st.DicomType.Contains("ORGAN") ||
                st.DicomType.Contains("AVOIDANCE")))
            {
                if (!s.IsEmpty)//check for empty structures
                {


                    structDetails.Add(new structList()
                    {
                        structureId = s.Id,
                        structureType = s.DicomType,
                        structureVolume = (s.Volume.ToString("F1") + "cc"),
                        HI = CalculateHI(s, myPi).ToString(),
                        gEUD = CalculateGEUD(s, myPi).ToString(),
                        ptvD98 = Calculated98(s, myPi).ToString() + "Gy",
                    });
                }
            }
            for (int i = 0; i < structDetails.Count; i++)
            {
                this.dataGrid.Items.Add(new structList() { 
                    structureId = structDetails[i].structureId.ToString(), 
                    structureType = structDetails[i].structureType.ToString(), 
                    structureVolume = structDetails[i].structureVolume.ToString(), 
                    HI = structDetails[i].HI.ToString(), 
                    gEUD = structDetails[i].gEUD.ToString(), 
                    ptvD98 = structDetails[i].ptvD98.ToString() });
            }
        }
       
        public class structList
        {
            public string structureId { get; set; }
            public string structureType { get; set; }
            public string structureVolume { get; set; }
            public string HI { get; set; }
            public string gEUD { get; set; }
            public string ptvD98 { get; set; }
        }

        private double Calculated98(Structure s, PlanningItem pi)
        {
            double d98 = (pi as PlanSetup).GetDoseAtVolume(s, 20, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;

            d98 = Math.Round(d98, 2);
            return d98;
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
