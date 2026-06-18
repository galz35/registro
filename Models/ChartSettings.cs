using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DevExpress.Web.Mvc;
using DevExpress.Data;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Data.Linq;
using DevExpress.Data.Linq.Helpers;
using System.Web.SessionState;
using System.Collections;
using System.Linq.Expressions;
using System.IO;
using System.Data;
using DevExpress.XtraCharts;
using DevExpress.Utils;

namespace slnRhonline.Models
{
    public static class ChartSettings
    {
        
        /// <summary>
        /// Método para la configuración del chart de viaticos Asignación vs Ejecución
        /// </summary>
        /// <returns></returns>
        public static ChartControlSettings GetChartExpensesSettings()
        {
            var settings = new ChartControlSettings();

            settings.Name = "Chart";
            settings.Width = 800;
            settings.Height = 500;
            ChartTitle chartTitle1 = new ChartTitle();
            chartTitle1.Text = "<b>Asignación vs Ejecución de Viáticos</b>"; //"<i>Basic</i> <b>HTML</b> <u>is</u> <color=blue>supported</color>.";
                                                                             // settings.Titles.Add(new ChartTitle()) ="Asignacion vs Ejecución de Viaticos";
            settings.Titles.AddRange(new ChartTitle[] { chartTitle1 });


            settings.SeriesDataMember = "Type";
            settings.SeriesTemplate.ArgumentDataMember = "PeriodId";
            settings.SeriesTemplate.ValueDataMembers[0] = "Amount";
            //settings.SeriesTemplate.ArgumentScaleType = ScaleType.DateTime;
            settings.SeriesTemplate.ChangeView(DevExpress.XtraCharts.ViewType.StackedBar);
            settings.SeriesTemplate.LabelsVisibility = DefaultBoolean.True;

            XYDiagram diagram = (XYDiagram)settings.Diagram;
            diagram.Rotated = true;
            diagram.AxisY.Interlaced = true;
            diagram.AxisY.Title.Text = "Monto en Córdobas";
            diagram.AxisY.GridLines.MinorVisible = true;
            diagram.AxisY.Title.Visibility = DefaultBoolean.True;
            
            //diagram.AxisX.Label.Angle = 270;

            //diagram.AxisY.Range.Auto = true;
            //diagram.AxisY.Range.AlwaysShowZeroLevel = false;

            return settings;
        }
        /// <summary>
        /// Método para la configuración del chart de horas extras Asignación vs Ejecución
        /// </summary>
        /// <returns></returns>
        public static ChartControlSettings GetChartExtratimeSettings()
        {
            var settings = new ChartControlSettings();

            settings.Name = "Chart";
            settings.Width = 800;
            settings.Height = 500;
            ChartTitle chartTitle1 = new ChartTitle();
            chartTitle1.Text = "<b>Asignación vs Ejecución de Horas Extras</b>"; 
                                                                            
            settings.Titles.AddRange(new ChartTitle[] { chartTitle1 });


            settings.SeriesDataMember = "Type";
            settings.SeriesTemplate.ArgumentDataMember = "Period";
            settings.SeriesTemplate.ValueDataMembers[0] = "Hours";
            //settings.SeriesTemplate.ArgumentScaleType = ScaleType.DateTime;
            settings.SeriesTemplate.ChangeView(DevExpress.XtraCharts.ViewType.StackedBar);
            settings.SeriesTemplate.LabelsVisibility = DefaultBoolean.True;

            XYDiagram diagram = (XYDiagram)settings.Diagram;
            diagram.Rotated = true;
            diagram.AxisY.Interlaced = true;
            diagram.AxisY.Title.Text = "Horas Extras";
            diagram.AxisY.GridLines.MinorVisible = true;
            diagram.AxisY.Title.Visibility = DefaultBoolean.True;

           

            return settings;
        }
    }
}