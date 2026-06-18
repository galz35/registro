using System;

namespace Entities.ViewModels
{
    public class ExpenseDetailReportxxx
    {
        public int ExpenseId { get; set; }
        public string tipo { get; set; }
        public DateTime fecha_pago { get; set; }
        public string periodo { get; set; }
        public string empresa { get; set; }
        public string ubicacion { get; set; }
        public string gerencia { get; set; }
        public string subgerencia { get; set; }
        public string areaName { get; set; }
        public string numeroEmpleado { get; set; } // Tu carnet
        public string colaborador { get; set; }    // Tu empleado
        public DateTime fecha_pagado { get; set; } // Tu fecha de viático
        public string ORG_PAYMENT_METHOD_NAME { get; set; }
        public string cuentacontable { get; set; }
        public string cc { get; set; }
        public string act { get; set; }
        public string negocio { get; set; }
        public string justificacion { get; set; }
        public string orden_servicio { get; set; }
        public string Clasification { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public decimal monto { get; set; }
        public decimal monto_rendir { get; set; }
        public decimal monto_retornar { get; set; }
        public string razon { get; set; }
        public string puesto { get; set; }
        public string edificio { get; set; }
        public string tn { get; set; }
        public string cuenta { get; set; }
        public string nombre { get; set; }
        public string estado { get; set; }
        public string Month { get; set; }
        public string Banco { get; set; }
        public string Ruta { get; set; }
        public string Departamentotraslado { get; set; }
        public string Departamento { get; set; }

        // === OTRAS PROPIEDADES INTERNAS QUE YA TENÍAS ===
        public int EXPENSE_DETAIL_ID { get; set; }
        public long PersonId { get; set; }
        public int PeriodId { get; set; }
        public long ManagementId { get; set; }
        public int AreaId { get; set; }
        public string Accounting { get; set; }
        public string EconomicActivity { get; set; }
        public int ClasificationId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int StatusId { get; set; }
        public string localidad { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
    }
}