using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Runtime.Remoting.Contexts;
using PlanMetricExplorer;

[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }
        PlanningItem pi;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context, System.Windows.Window window, ScriptEnvironment environment)
        {            
            // TODO : Add here the code that is called when the script is launched from Eclipse.
            PlanSetup plan = context.PlanSetup;
            var planDVHDetails = new PlanMetricExplorer.PlanDVHDetails();
            planDVHDetails.MyPlan = plan;
            planDVHDetails.MyPI = pi;
            planDVHDetails.Visibility = Visibility.Visible;
            planDVHDetails.zwindow = window;
            window.Content = planDVHDetails;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
