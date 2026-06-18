using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace slnRhonline.Models
{
    public static class InspeccionPdfCreator
    {
        /// <summary>
        /// Genera un PDF con datos y fotos de la inspección:
        /// - Datos en tabla
        /// - Conductores en una lista
        /// - Fotos de a 2 por página (frontal, posterior y luego adicionales)
        /// Retorna la ruta del PDF.
        /// </summary>
        public static string CreatePdf(InspeccionViewModel model, List<EmpleadoInspeccion> lstEmpleado)
        {
            // 1. Crear ruta en carpeta temporal
            string pdfPath = Path.Combine(Path.GetTempPath(), $"Actualizacion_{model.IdVehiculo}_{Guid.NewGuid()}.pdf");

            using (FileStream fs = new FileStream(pdfPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // 2. Configurar documento A4 con márgenes
                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // 3. Definir las fuentes principales
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(211, 47, 47)); // rojo intenso
                var subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA, 13, new BaseColor(60, 60, 60));    // gris oscuro
                var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);
                var footerFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10, new BaseColor(120, 120, 120));

                // 4. Línea separadora
                var lineSeparator = new iTextSharp.text.pdf.draw.LineSeparator(1.0f, 100.0f, new BaseColor(180, 180, 180), Element.ALIGN_CENTER, 1);

                // ===================== Encabezado =====================
                // Título principal
                Paragraph header = new Paragraph("SUBGERENCIA DE RECURSOS HUMANOS", headerFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 5f
                };
                doc.Add(header);

                // Subtítulo con el vehículo
                Paragraph subHeader = new Paragraph($"ACTUALIZACIÓN DE DATOS - VEHÍCULO: {model.IdVehiculo}", subHeaderFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 5f
                };
                doc.Add(subHeader);

                // Fecha (alineada a la derecha)
                Paragraph fecha = new Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}", footerFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 10f
                };
                doc.Add(fecha);

                // Separador y espacio
                doc.Add(new Chunk(lineSeparator));
                doc.Add(new Paragraph(" "));

                // ===================== Tabla de datos =====================
                PdfPTable mainTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10,
                    SpacingAfter = 10
                };
                mainTable.SetWidths(new float[] { 0.3f, 0.7f });

                // Función local para crear filas
                void AddRow(string label, string value)
                {
                    PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont))
                    {
                        BackgroundColor = new BaseColor(240, 240, 240), // gris suave
                        Padding = 5,
                        BorderWidth = 0.5f,
                        BorderColor = new BaseColor(180, 180, 180)
                    };
                    mainTable.AddCell(labelCell);

                    PdfPCell valueCell = new PdfPCell(new Phrase(value ?? "", valueFont))
                    {
                        Padding = 5,
                        BorderWidth = 0.5f,
                        BorderColor = new BaseColor(180, 180, 180)
                    };
                    mainTable.AddCell(valueCell);
                }

                // Llenar la tabla con tus datos principales
                AddRow("Asignada", model.AsignadaA);
                AddRow("Departamento", model.Departamento);
                AddRow("Edificio", model.Edificio);
                AddRow("Lugar de estacionamiento", model.Domicilio);
                AddRow("Teléfono", model.Telefono);
                AddRow("Kilometraje", model.Kilometraje);
                AddRow("Realizó Prueba de Manejo", model.PruebaManejo ? "Sí" : "No");

                doc.Add(mainTable);

                // ===================== Conductores =====================
                doc.Add(new Paragraph("Conductores:", sectionFont));
                doc.Add(new Paragraph(" "));

                if (model.Conductores != null && model.Conductores.Any())
                {
                    iTextSharp.text.List conductorList = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED, 10f);
                    conductorList.SetListSymbol("• ");

                    foreach (var cedula in model.Conductores)
                    {
                        var emp = lstEmpleado.FirstOrDefault(x => x.CEDULA == cedula);
                        string texto = emp != null ? $"{emp.NOMBRE_COMPLETO} - {emp.CARNET}" : cedula;
                        conductorList.Add(new ListItem(texto, valueFont));
                    }
                    doc.Add(conductorList);
                }
                else
                {
                    doc.Add(new Paragraph("(Sin conductores)", valueFont));
                }

                doc.Add(new Paragraph(" "));
                doc.Add(new Chunk(lineSeparator));
                doc.Add(new Paragraph(" "));

                // ===================== Fotos =====================
                // 1) Lista KeyValuePair de fotos
                List<KeyValuePair<string, string>> photos = new List<KeyValuePair<string, string>>();
                // Licencia frontal y posterior primero
                photos.Add(new KeyValuePair<string, string>("Foto Licencia Frontal", model.FotoLicenciaFrontalBase64));
                photos.Add(new KeyValuePair<string, string>("Foto Licencia Posterior", model.FotoLicenciaPosteriorBase64));

                // Luego las adicionales
                if (model.Imagenes != null)
                {
                    foreach (var kvp in model.Imagenes)
                        photos.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                }

                // Función local para crear celdas con imagen
                PdfPCell MakePhotoCell(string title, string base64)
                {
                    PdfPCell cell = new PdfPCell { Border = 0, Padding = 5 };
                    if (!string.IsNullOrEmpty(base64))
                    {
                        cell.AddElement(new Paragraph(title, labelFont));
                        cell.AddElement(new Paragraph(" ", valueFont));

                        byte[] bytes = ConvertirImagenWebP(base64);
                        if (bytes != null && bytes.Length > 0)
                        {
                            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(bytes);
                            img.ScaleToFit(200f, 200f);
                            img.Alignment = Element.ALIGN_CENTER;
                            cell.AddElement(img);
                        }
                        else
                        {
                            cell.AddElement(new Paragraph("(Base64 inválido)", valueFont));
                        }
                    }
                    else
                    {
                        cell.AddElement(new Paragraph(title, labelFont));
                        cell.AddElement(new Paragraph("(Sin imagen)", valueFont));
                    }
                    return cell;
                }

                // Recorremos de 4 en 4 para hacer tablas 2x2 en cada página
                for (int i = 0; i < photos.Count; i += 4)
                {
                    if (i > 0) doc.NewPage(); // Cada grupo de 4 fotos, página nueva

                    PdfPTable photoTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100
                    };
                    photoTable.SetWidths(new float[] { 0.5f, 0.5f });

                    // Foto 1
                    var pA = photos[i];
                    photoTable.AddCell(MakePhotoCell(pA.Key, pA.Value));

                    // Foto 2
                    if (i + 1 < photos.Count)
                    {
                        var pB = photos[i + 1];
                        photoTable.AddCell(MakePhotoCell(pB.Key, pB.Value));
                    }
                    else
                    {
                        photoTable.AddCell(new PdfPCell { Border = 0 });
                    }

                    // Foto 3
                    if (i + 2 < photos.Count)
                    {
                        var pC = photos[i + 2];
                        photoTable.AddCell(MakePhotoCell(pC.Key, pC.Value));
                    }
                    else
                    {
                        photoTable.AddCell(new PdfPCell { Border = 0 });
                    }

                    // Foto 4
                    if (i + 3 < photos.Count)
                    {
                        var pD = photos[i + 3];
                        photoTable.AddCell(MakePhotoCell(pD.Key, pD.Value));
                    }
                    else
                    {
                        photoTable.AddCell(new PdfPCell { Border = 0 });
                    }

                    doc.Add(photoTable);
                }

                // ===================== Pie de página =====================
                doc.Add(new Paragraph(" "));
                doc.Add(new Chunk(lineSeparator));

                Paragraph footer = new Paragraph("Documento generado automáticamente por RHOnline.", footerFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                doc.Add(footer);

                // 5. Cerrar doc
                doc.Close();
            }

            return pdfPath;
        }

        /// <summary>
        /// Convierte la cadena Base64 (con prefijo data:image/...) en un arreglo de bytes.
        /// Si el formato es WebP, lo convierte a JPEG (formato soportado por iTextSharp).
        /// Si la cadena no es válida, retorna null.
        /// </summary>
        private static byte[] ConvertirImagenWebP(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return null;

            var partes = base64Data.Split(new char[] { ',' }, 2);
            if (partes.Length < 2)
                return null;

            try
            {
                byte[] bytes = Convert.FromBase64String(partes[1]);

                // Si la imagen es WebP, convertirla a JPEG
                if (base64Data.StartsWith("data:image/webp"))
                {
                    using (var msInput = new MemoryStream(bytes))
                    {
                        using (var image = System.Drawing.Image.FromStream(msInput))
                        {
                            using (var msJpeg = new MemoryStream())
                            {
                                // Guarda en formato JPEG (puedes ajustar la calidad si lo deseas)
                                image.Save(msJpeg, System.Drawing.Imaging.ImageFormat.Jpeg);
                                return msJpeg.ToArray();
                            }
                        }
                    }
                }
                else
                {
                    // Para otros formatos (jpeg, png, etc.), se retorna el arreglo de bytes
                    return bytes;
                }
            }
            catch
            {
                return null;
            }
        }

    }
}
