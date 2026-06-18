using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using Entities;

namespace slnRhonline.Models
{
    public class ReceiptProcessor
    {
       
    public ReceiptData ProcessText(string jsonString, List<UnidadControl> listaControl)
        {
            var data = new ReceiptData();

            // Paso 1: Extraer el contenido dentro de [ ]
            int startIndex = jsonString.IndexOf('[');
            int endIndex = jsonString.LastIndexOf(']');

            if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
            {
                Console.WriteLine("Formato JSON inválido: no se encontraron corchetes [ ].");
                return data;
            }

            string textContent = jsonString.Substring(startIndex + 1, endIndex - startIndex - 1);

            // Paso 2: Dividir por '","'
            // Primero, reemplazar \" con un marcador temporal para evitar conflictos durante el split
            string tempMarker = "__TEMP_MARKER__";
            textContent = textContent.Replace("\\\"", tempMarker);

            // Ahora, dividir por '","'
            string[] items = textContent.Split(new string[] { "\",\"" }, StringSplitOptions.None);

            // Paso 3: Limpiar cada cadena
            for (int i = 0; i < items.Length; i++)
            {
                // Reemplazar el marcador temporal con comillas normales si es necesario
                items[i] = items[i].Replace(tempMarker, "\"").Trim();

                // Eliminar cualquier \ remanente
                items[i] = items[i].Replace("\\", "").Trim();

                // Eliminar comillas iniciales y finales si existen
                if (items[i].StartsWith("\""))
                {
                    items[i] = items[i].Substring(1);
                }
                if (items[i].EndsWith("\""))
                {
                    items[i] = items[i].Substring(0, items[i].Length - 1);
                }

                // Convertir a mayúsculas para simplificar las comparaciones
                items[i] = items[i].ToUpper();
            }

            // Paso 4: Recorrer el array y extraer los datos
            for (int i = 0; i < items.Length; i++)
            {
                string line = items[i];
                if (data.Unidad == null || data.Unidad == "")
                {
                    var matchedControl = listaControl.Where(control => line.Contains(control.Codigo.ToUpper())|| line.Contains(control.unidad.ToUpper()));
                    if (matchedControl!=null&& matchedControl.Count()>0)
                    {
                        string codigo = matchedControl.FirstOrDefault().unidad;
                        if (!string.IsNullOrEmpty(codigo))
                        {
                            data.NumeroCompra = codigo;
                            data.Unidad = codigo;

                        }
                    }
            
                }
                if (line.StartsWith("REFERENCIA:"))
                {
                    data.Referencia = ExtractValue(line);
                    continue;
                }
                if (line.Contains("REFERENCIA") == true)
                {
                    data.Referencia = ExtractValue(line);
                    continue;
                }

                if (line.Contains("TELEFONO")==true || i==26)
                {
                    continue;
                }
                if (i==0 || data.Estacion==null || data.Estacion.Length<=2)
                {
                    data.Estacion = line;
                    continue;

                }   // Cliente
                if (line.StartsWith("CLIENTE:"))
                {
                    data.Cliente = ExtractValue(line);
                    continue;
                }
                 // Odómetro
                if (line.StartsWith("ODOMETRO:") || line.StartsWith("ODONETRO:") || line.StartsWith("DDOMETRO:") )
                {
                    data.Odometro = ExtractValue(line);
                    continue;
                }

                // Producto (PETROSUPER o DIESEL)
                if (line.Contains("PETROSUPER") || line.Contains("PETRUSUPE") || line.Contains("DIESEL") || line.Contains("PETRODIESEL"))
                {
                    data.Producto = line.Trim();

                    // Después del producto, buscar "VOLUMEN: LTS" y luego el valor
                    // Inicializar contador para números encontrados
                    int numbersFound = 0;

                    // Escanear hacia adelante para encontrar los dos primeros números: volumen y precio
                    for (int j = i + 1; j < items.Length && numbersFound < 2; j++)
                    {
                        string currentLine = items[j].Trim();

                        // Intentar extraer un número de la línea actual
                        double number;
                        if (TryExtractDouble(currentLine, out number))
                        {
                            if (numbersFound == 0)
                            {
                                data.VolumenLitros = number;
                                numbersFound++;
                            }
                            else if (numbersFound == 1)
                            {
                                data.PrecioUnitario = number;
                                numbersFound++;
                            }
                        }
                    }

                     continue;
                }

                
                // Referencia
                // Cédula
                if (line.StartsWith("CEDULA:"))
                {
                    data.Cedula = ExtractValue(line);
                    continue;
                }

                // Fecha y Hora
                if (Regex.IsMatch(line, @"\d{2}/\d{2}/\d{2}\s*\d{2}:\d{2}:\d{2}"))
                {
                    data.FechaHora = ParseDateTime(line);
                    continue;
                }
                
                     if (line.Contains("CONTROL"))
                {
                     
                         
                        data.Control = line;
                   
                    continue;
                }
             
                   
                // Número de Compra
                if (line == "COMPRA")
                {
                    if (i + 1 < items.Length)
                    {
                        string compraLine = items[i + 1];
                        if (string.IsNullOrEmpty(data.NumeroCompra) || data.NumeroCompra == null)
                        {
                            var matchedControl = listaControl.Where(control => line.Contains(control.Codigo.ToUpper()) || line.Contains(control.unidad.ToUpper()));
                            if (matchedControl != null && matchedControl.Count() > 0)
                            {
                                string codigo = matchedControl.FirstOrDefault().unidad;


                                if (!string.IsNullOrEmpty(codigo))
                                {
                                    data.NumeroCompra = codigo;
                                    data.Unidad = codigo;

                                }
                            }
                        }

                         data.NumeroCompra = compraLine;
                        data.Unidad = compraLine;
                    }
                    continue;
                }
            }

            return data;
        }

        private string ExtractValue(string line)
        {
            int index = line.IndexOf(':');
            if (index >= 0 && index < line.Length - 1)
            {
                return line.Substring(index + 1).Trim();
            }
            return line.Trim();
        }



        private DateTime ParseDateTime(string line)
        {
            try
            {
                var match = Regex.Match(line, @"(\d{2}/\d{2}/\d{2})\s*(\d{2}:\d{2}:\d{2})");
                if (match.Success)
                {
                    string datePart = match.Groups[1].Value;
                    string timePart = match.Groups[2].Value;
                    return DateTime.ParseExact(datePart + " " + timePart, "dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
                }
            }
            catch (FormatException)
            {
                // Manejar errores de formato
            }
            return DateTime.MinValue;
        }
        private bool TryExtractDouble(string input, out double result)
        {
            // Usar regex para encontrar el primer número en la cadena
            var match = Regex.Match(input, @"\d+([.,]\d+)?");
            if (match.Success)
            {
                string numberString = match.Value.Replace(',', '.');
                return double.TryParse(numberString, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }
            result = 0;
            return false;
        }

        // Función para extraer solo la parte numérica de una cadena
        private string ExtractNumericPart(string input)
        {
            var match = Regex.Match(input, @"\d+");
            if (match.Success)
            {
                return match.Value;
            }
            return null;
        }

        // Función para extraer el valor después de ':'
       

       

        private double ExtractNumericValueFromLine(string line)
        {
            // Extraer el primer número que aparezca en la línea
            var match = Regex.Match(line, @"\d+([.,]\d+)?");
            if (match.Success)
            {
                string number = match.Value.Replace(',', '.');
                if (double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }
            return 0;
        }
        static List<Vehiculo> ProcesarDatos(List<string> controlList)
        {
            // Inicializar lista de vehículos
            List<Vehiculo> vehiculos = new List<Vehiculo>();

            // Iterar sobre la lista en bloques de 3 (IdUnidad, Modelo, Tipo)
            for (int i = 0; i < controlList.Count; i += 2)
            {
                // Verificar que el bloque de 3 datos exista para evitar errores
                if (i + 2 < controlList.Count)
                {
                    string idUnidad = controlList[i];
                    string modelo = controlList[i + 1];
 
                    // Crear objeto Vehiculo y agregarlo a la lista
                    Vehiculo vehiculo = new Vehiculo(idUnidad, modelo );
                    vehiculos.Add(vehiculo);
                }
            }

            return vehiculos;
        }
        public ReceiptData ProcessBACReceipt(string jsonString,List<UnidadControl> vehiculos)
        {
            var data = new ReceiptData();
 
            //List<string> ControlList = new List<string> { "BOMBA1", "SR440", "COMEC. 315", "NS11293", "COMER. 344", "CH41867", "COMERC. 121", "MY15414", "COMERC. 136", "M105923", "COMERC. 139", "MT22174", "COMERC. 142", "MY9675", "COMERC. 155", "M166545", "COMERC. 157", "CT10685", "COMERC. 158", "CH26409", "COMERC. 160", "M241696", "COMERC. 161", "M251726", "COMERC. 179", "M274570", "COMERC. 181", "CT13372", "COMERC. 191", "M208134", "COMERC. 196", "NS1625", "COMERC. 208", "M272109", "COMERC. 214", "RN6052", "COMERC. 215", "M038904", "COMERC. 217", "M290784", "COMERC. 218", "M074677", "COMERC. 219", "LE25448", "COMERC. 221", "ES25283", "COMERC. 224", "LE31256", "COMERC. 227", "M257182", "COMERC. 229", "RS1591", "COMERC. 230", "CH33701", "COMERC. 231", "M052809", "COMERC. 233", "M269175", "COMERC. 235", "RN3292", "COMERC. 236", "RN3292", "COMERC. 237", "MY13195", "COMERC. 238", "CZ16219", "COMERC. 239", "CH33134", "COMERC. 253", "ES27981", "COMERC. 254", "RS13491", "COMERC. 256", "M255576", "COMERC. 258", "MT35006", "COMERC. 259", "CZ17156", "COMERC. 260", "M321041", "COMERC. 261", "M188239", "COMERC. 262", "M276854", "COMERC. 263", "RN3937", "COMERC. 264", "BO106323", "COMERC. 265", "M339712", "COMERC. 267", "RS14451", "COMERC. 268", "RS1940", "COMERC. 271", "CT16512", "COMERC. 273", "M276610", "COMERC. 274", "RN17832", "COMERC. 275", "MT36644", "COMERC. 276", "M346168", "COMERC. 277", "M270518", "COMERC. 278", "BO11152", "COMERC. 279", "M102133", "COMERC. 281", "RS5462", "COMERC. 282", "CT15187", "COMERC. 283", "MY14980", "COMERC. 285", "M353302", "COMERC. 287", "RN6052", "COMERC. 288", "LE35314", "COMERC. 289", "M351220", "COMERC. 290", "M239897", "COMERC. 291", "M180894", "COMERC. 292", "MT26407", "COMERC. 293", "RN13639", "COMERC. 294", "M208646", "COMERC. 295", "M154231", "COMERC. 296", "M174413", "COMERC. 297", "M240816", "COMERC. 298", "NS5646", "COMERC. 299", "RS3495", "COMERC. 300", "M331179", "COMERC. 301", "RI13853", "COMERC. 304", "CZ9383", "COMERC. 305", "LE24694", "COMERC. 306", "M355094", "COMERC. 307", "M234363", "COMERC. 309", "LE36374", "COMERC. 311", "NS6697", "COMERC. 312", "LE36912", "COMERC. 313", "M305436", "COMERC. 314", "RN241", "COMERC. 316", "CH49633", "COMERC. 317", "CH25722", "COMERC. 318", "CZ18611", "COMERC. 321", "RI14093", "COMERC. 322", "MT52563", "COMERC. 324", "MT41153", "COMERC. 325", "M378301", "COMERC. 326", "ES31975", "COMERC. 327", "CT18387", "COMERC. 328", "NS33524", "COMERC. 329", "M031081", "COMERC. 330", "MT09244", "COMERC. 331", "M378177", "COMERC. 332", "MT33113", "COMERC. 336", "M173499", "COMERC. 338", "M136031", "COMERC. 339", "M305259", "COMERC. 340", "M188248", "COMERC. 342", "JI25035", "COMERC. 343", "CH41440", "COMERC. 348", "LE38226", "COMERC. 352", "M333182", "COMERC. 353", "M395957", "COMERC. 354", "CT20640", "COMERC. 355", "ES33010", "COMERC. 362", "JI31746", "COMERC. 374", "BO9065", "COMERC.106", "RI7852", "COMERC.108", "CH22191", "COMERC.169", "M280463", "COMERC.170", "M209245", "COMERC.182", "M277580", "COMERC.183", "NR2900", "COMERC.184", "M143113", "COMERC.186", "CZ14350", "COMERC.189", "ES24372", "COMERC.190", "MT29115", "Comerc.204", "CZ14824", "Comerc.212", "CT14219", "COMERC.213", "M267701", "COMERC.216", "M309414", "COMERC.234", "RN5978", "COMERC.241", "M274649", "COMERC.242", "RS5197", "COMERC.244", "BO9941", "COMERC.245", "M146536", "COMERC.246", "M233949", "COMERC.247", "RS5534", "COMERC.248", "ES27693", "COMERC.249", "ES27808", "COMERC.252", "MT34426", "COMERC.65", "MT29222", "COMERC.68", "M255601", "CORMERC. 284", "LE35093", "CORP. 119", "M064453", "CORP. 155", "M286536", "CORP. 165", "M2932217", "CORP. 166", "M255554", "CORP. 168", "M298469", "CORP. 169", "M298219", "CORP. 181", "M303851", "CORP. 183", "M273229", "CORP. 184", "M240141", "CORP. 186", "M258520", "CORP. 189", "M136031", "CORP. 190", "M280070", "CORP. 195", "GR12411", "CORP. 196", "M001530", "CORP. 198", "GR12971", "CORP. 199", "MT23670", "CORP. 201", "M321046", "CORP. 202", "MY17000", "CORP. 207", "M293396", "CORP. 212", "LE32221", "CORP. 215", "208936", "CORP. 216", "M306404", "CORP. 217", "M294183", "CORP. 218", "M013322", "CORP. 219", "M123025", "CORP. 220", "M278727", "CORP. 221", "M161231", "CORP. 222", "M333404", "CORP. 223", "M063329", "CORP. 225", "M214097", "CORP. 226", "ES28520", "CORP. 229", "M336703", "CORP. 230", "GR15816", "CORP. 231", "M335213", "CORP. 233", "M205730", "CORP. 234", "M340090", "CORP. 235", "M336250", "CORP. 236", "M219269", "CORP. 237", "M303745", "CORP. 238", "M344602", "CORP. 239", "M173614", "CORP. 241", "MY16839", "CORP. 242", "M333905", "CORP. 243", "M197660", "CORP. 244", "LE26946", "CORP. 245", "ES20177", "CORP. 246", "M226125", "CORP. 247", "M311502", "CORP. 250", "M267770", "CORP. 253", "M065737", "CORP. 254", "M235807", "CORP. 255", "M236021", "CORP. 256", "M357850", "CORP. 259", "M299474", "CORP. 260", "M222835", "CORP. 261", "M171135", "CORP. 262", "MY17067", "CORP. 264", "M350297", "CORP. 265", "M219483", "CORP. 266", "M006868", "CORP. 267", "CZ19296", "CORP. 268", "JI9517", "CORP. 269", "M337795", "CORP. 270", "M168293", "CORP. 272", "M339092", "CORP. 277", "M288843", "CORP. 278", "M296498", "CORP. 279", "M369271", "CORP. 282", "M290784", "CORP. 284", "M146131", "CORP. 285", "M371060", "CORP. 286", "BO10981", "CORP. 287", "ES31941", "CORP. 288", "M171791", "CORP. 289", "LE36208", "CORP. 290", "MY2328", "CORP. 291", "M375456", "CORP. 292", "M376033", "CORP. 293", "M373052", "CORP. 299", "M375699", "CORP. 300", "M061638", "CORP. 301", "M097616", "CORP. 302", "M278320", "CORP. 303", "M3527450", "CORP. 304", "M389085", "CORP. 306", "M397129", "CORP. 309", "M290784", "CORP. 314", "M037273", "CORP. 316", "M249527", "CORP. 317", "M400730", "CORP. 324", "M402567", "CORP. 326", "M393710", "CORP. 327", "M406948", "CORP. 331", "CH44320", "CORP.101", "M081964", "CORP.115", "ES24560", "CORP.138", "LE27160", "CORP.142", "M283122", "CORP.152", "M099133", "CORP.153", "M286028", "CORP.154", "N291710", "CORP.157", "M265008", "CORP.160", "M229190", "CORP.174", "M216055", "Corp.176", "M287798", "Corp.177", "M184270", "Corp.185", "M273700", "CORP.203", "M308946", "CORP.204", "MY15096", "CORP.205", "M221094", "CORP.208", "CH21870", "CORP.210", "M299400", "CORP.211", "M207655", "ELECTROGEN", "1325", "EMERGENCIA 14", "0821", "MERCADEO 04", "M185711", "MERCADEO 11", "M346915", "N1C-1346", "M159582", "N1C-1347", "M150923", "N1C-1352", "M150392", "N1C-1660", "M295950", "N1C-1799", "M321387", "N1C-1800", "M321382", "N1C-1801", "M321384", "N1C-1968", "M399501", "N1C-839", "M034745", "N1C-840", "M042100", "N1C-842", "M033996", "N1C-843", "M037305", "N1J-1193", "M088284", "N1J-1194", "M088285", "N1J-1252", "M116670", "N1J-1288", "M128757", "N1J-1363", "M168707", "N1J-1394", "M184380", "N1J-1440", "M227399", "N1J-1441", "M227401", "N1J-1442", "M227398", "N1J-1443", "M227400", "N1J-1444", "M227404", "N1J-1445", "M320553", "N1J-1446", "M227406", "N1J-1542", "M312536", "N1J-1543", "M238627", "N1J-1544", "M287283", "N1J-1545", "M238640", "N1J-1546", "M256174", "N1J-1547", "M238637", "N1J-1548", "M285768", "N1J-1626", "M291348", "N1J-1627", "M291408", "N1M-8277", "M07258", "N1M-8283", "M26130", "N1M-8284", "M26131", "N1M-8285", "M107448", "N1M-8286", "M107446", "N1M-8287", "M107439", "N1M-8288", "M37397", "N1M-8289", "M37414", "N1M-8290", "M37392", "N1M-8291", "M172542", "N1M-8292", "M172535", "N1M-8293", "M172537", "N1M-8294", "M172534", "N1M-8295", "M172539", "N1MT-1178", "S/P", "N1MT-1218", "S/P", "N1MT-1272", "S/P", "N1MT-1343", "S/P", "N1MT-1395", "S/P", "N1MT-1573", "S/P", "N1MT-1716", "S/P", "N1MT-1865", "S/P", "N1MT-1880", "S/P", "N1PA-1067", "M007578", "N1PA-1069", "M007575", "N1PA-1070", "M007588", "N1PA-1084", "M007605", "N1PA-1085", "M007608", "N1PA-1097", "M013730", "N1PA-1101", "M013747", "N1PA-1104", "M013739", "N1PA-1112", "M031181", "N1PA-1121", "M031196", "N1PA-1141", "M050013", "N1PA-1143", "M050014", "N1PA-1144", "M050008", "N1PA-1147", "M050019", "N1PA-1233", "M111916", "N1PA-1235", "M116673", "N1PA-1236", "M116658", "N1PA-1238", "M116676", "N1PA-1268", "M120362", "N1PA-1275", "M125721", "N1PA-1304", "M129000", "N1PA-1305", "M128997", "N1PA-1318", "M153735", "N1PA-1319", "M153734", "N1PA-1320", "M153733", "N1PA-1321", "M153915", "N1PA-1322", "M153913", "N1PA-1323", "M153914", "N1PA-1324", "M154870", "N1PA-1325", "M154872", "N1PA-1326", "M153438", "N1PA-1329", "M156151", "N1PA-1338", "M158746", "N1PA-1339", "M158749", "N1PA-1349", "M150283", "N1PA-1362", "M169480", "N1PA-1405", "M198467", "N1PA-1406", "M198485", "N1PA-1407", "M198486", "N1PA-1408", "M198487", "N1PA-1409", "M198484", "N1PA-1410", "M198464", "N1PA-1411", "M198466", "N1PA-1412", "M198468", "N1PA-1413", "M198470", "N1PA-1414", "M198473", "N1PA-1415", "M198478", "N1PA-1416", "M198461", "N1PA-1417", "M198479", "N1PA-1418", "M198481", "N1PA-1419", "M198476", "N1PA-1420", "M198474", "N1PA-1421", "M198471", "N1PA-1422", "M198463", "N1PA-1447", "M227813", "N1PA-1448", "M227362", "N1PA-1449", "M226734", "N1PA-1450", "M226737", "N1PA-1451", "M226735", "N1PA-1452", "M227814", "N1PA-1453", "M229497", "N1PA-1454", "M229505", "N1PA-1538", "M238607", "N1PA-1539", "M238608", "N1PA-1540", "M238610", "N1PA-1541", "M238611", "N1PA-1549", "M240138", "N1PA-1550", "M240137", "N1PA-1562", "M273430", "N1PA-1563", "M273434", "N1PA-1564", "M273441", "N1PA-1565", "M273432", "N1PA-1566", "M273427", "N1PA-1567", "M273436", "N1PA-1568", "M273429", "N1PA-1569", "M273428", "N1PA-1570", "M273431", "N1PA-1571", "M273435", "N1PA-1572", "M273437", "N1PA-1606", "M291887", "N1PA-1607", "M291892", "N1PA-1608", "M291891", "N1PA-1609", "M291893", "N1PA-1610", "M291890", "N1PA-1611", "M291899", "N1PA-1612", "M291896", "N1PA-1613", "M291897", "N1PA-1614", "M291901", "N1PA-1615", "M291904", "N1PA-1616", "M291895", "N1PA-1617", "M291894", "N1PA-1618", "M291889", "N1PA-1619", "M291830", "N1PA-1620", "M291832", "N1PA-1621", "M291825", "N1PA-1622", "M291829", "N1PA-1623", "M291833", "N1PA-1624", "M291827", "N1PA-1625", "M291836", "N1PA-1694", "M310060", "N1PA-1695", "M310053", "N1PA-1696", "M310061", "N1PA-1697", "M310051", "N1PA-1698", "M310074", "N1PA-1699", "M310054", "N1PA-1700", "M310055", "N1PA-1701", "M310056", "N1PA-1702", "M310057", "N1PA-1703", "M310058", "N1PA-1758", "M321242", "N1PA-1759", "M321243", "N1PA-1760", "M321261", "N1PA-1761", "M320707", "N1PA-1762", "M321965", "N1PA-1763", "M323192", "N1PA-1764", "M323193", "N1PA-1765", "M324018", "N1PA-1766", "M324058", "N1PA-1767", "M324057", "N1PA-1768", "M321392", "N1PA-1769", "M321395", "N1PA-1770", "M321400", "N1PA-1771", "M321403", "N1PA-1772", "M321389", "N1PA-1773", "M321402", "N1PA-1774", "M320705", "N1PA-1775", "M321532", "N1PA-1776", "M321548", "N1PA-1777", "M321530", "N1PA-1778", "M321549", "N1PA-1779", "M321535", "N1PA-1780", "M321538", "N1PA-1781", "M321555", "N1PA-1782", "M321561", "N1PA-1783", "M321557", "N1PA-1784", "M321551", "N1PA-1785", "M321543", "N1PA-1786", "M321546", "N1PA-1787", "M321553", "N1PA-1788", "M321539", "N1PA-1789", "M321559", "N1PA-1790", "M321540", "N1PA-1791", "M321541", "N1PA-1792", "M321560", "N1PA-1793", "M321542", "N1PA-1794", "M321558", "N1PA-1795", "M321533", "N1PA-1796", "M321544", "N1PA-1797", "M321536", "N1PA-1826", "M350681", "N1PA-1861", "M354494", "N1PA-1877", "M365370", "N1PA-1899", "M395626", "N1PA-1900", "M395626", "N1PA-1901", "M401102", "N1PA-1902", "M401100", "N1PA-1903", "M401101", "N1PU-1029", "M055111", "N1PU-1064", "M233631", "N1PU-1093", "M235076", "N1PU-1107", "M013718", "N1PU-1116", "M031197", "N1PU-1117", "M031190", "N1PU-1139", "M048756", "N1PU-1155", "M048762", "N1PU-1163", "M203243", "N1PU-1166", "M059043", "N1PU-1176", "M063248", "N1PU-1182", "M066925", "N1PU-1185", "M069560", "N1PU-1186", "M069562", "N1PU-1196", "M064006", "N1PU-1211", "M089535", "N1PU-1232", "M110556", "N1PU-1240", "M116675", "N1PU-1261", "M119953", "N1PU-1265", "M264796", "N1PU-1266", "M120370", "N1PU-1269", "M120358", "N1PU-1276", "M127047", "N1PU-1279", "M127038", "N1PU-1285", "M128777", "N1PU-1290", "M128767", "N1PU-1293", "M128775", "N1PU-1295", "M128758", "N1PU-1297", "M128776", "N1PU-1309", "M130114", "N1PU-1310", "M130120", "N1PU-1311", "M130123", "N1PU-1313", "M152326", "N1PU-1314", "M152324", "N1PU-1315", "M152325", "N1PU-1316", "M152446", "N1PU-1327", "M203244", "N1PU-1328", "M156152", "N1PU-1332", "M190624", "N1PU-1335", "M157847", "N1PU-1340", "M159758", "N1PU-1342", "M159754", "N1PU-1344", "M159757", "N1PU-1353", "M203018", "N1PU-1355", "M203020", "N1PU-1356", "M203019", "N1PU-1357", "M203003", "N1PU-1364", "M170838", "N1PU-1365", "M170829", "N1PU-1366", "M170837", "N1PU-1367", "M172594", "N1PU-1368", "M172595", "N1PU-1370", "M172607", "N1PU-1371", "M172614", "N1PU-1372", "M172612", "N1PU-1373", "M172602", "N1PU-1374", "M172606", "N1PU-1375", "M172599", "N1PU-1376", "M175972", "N1PU-1378", "M172598", "N1PU-1381", "M172611", "N1PU-1383", "M172609", "N1PU-1384", "M387207", "N1PU-1385", "M172600", "N1PU-1386", "M172592", "N1PU-1387", "M185373", "N1PU-1388", "M185376", "N1PU-1390", "M185377", "N1PU-1392", "M185552", "N1PU-1396", "M198488", "N1PU-1397", "M198460", "N1PU-1398", "M198462", "N1PU-1399", "M198469", "N1PU-1400", "M198465", "N1PU-1402", "M198472", "N1PU-1403", "M198480", "N1PU-1423", "M198482", "N1PU-1424", "M198483", "N1PU-1426", "M299699", "N1PU-1427", "M203010", "N1PU-1428", "M203017", "N1PU-1429", "M203021", "N1PU-1430", "M203015", "N1PU-1432", "M203008", "N1PU-1434", "M207931", "N1PU-1435", "M207927", "N1PU-1436", "M207925", "N1PU-1437", "M207924", "N1PU-1438", "M207930", "N1PU-1439", "M207923", "N1PU-1455", "M227791", "N1PU-1456", "M226907", "N1PU-1457", "M227360", "N1PU-1458", "M227795", "N1PU-1461", "M227410", "N1PU-1462", "M227363", "N1PU-1463", "M227808", "N1PU-1464", "M226910", "N1PU-1465", "M226913", "N1PU-1466", "M227414", "N1PU-1467", "M226914", "N1PU-1468", "M226912", "N1PU-1469", "M227412", "N1PU-1470", "M226903", "N1PU-1471", "M227793", "N1PU-1472", "M226908", "N1PU-1473", "M227797", "N1PU-1474", "M226905", "N1PU-1475", "M227799", "N1PU-1476", "M227792", "N1PU-1477", "M227794", "N1PU-1478", "M227800", "N1PU-1479", "M227355", "N1PU-1480", "M227356", "N1PU-1481", "M227365", "N1PU-1482", "M227359", "N1PU-1484", "M227796", "N1PU-1485", "M229507", "N1PU-1486", "M229526", "N1PU-1487", "M229512", "N1PU-1488", "M229525", "N1PU-1489", "M229494", "N1PU-1490", "M229515", "N1PU-1491", "M229522", "N1PU-1492", "M229488", "N1PU-1493", "M229517", "N1PU-1494", "M229513", "N1PU-1495", "M229520", "N1PU-1497", "M229498", "N1PU-1498", "M229504", "N1PU-1499", "M229490", "N1PU-1500", "M229495", "N1PU-1501", "M229500", "N1PU-1502", "M229492", "N1PU-1503", "M229491", "N1PU-1504", "M229519", "N1PU-1505", "M229508", "N1PU-1506", "M229487", "N1PU-1507", "M229523", "N1PU-1508", "M229510", "N1PU-1509", "M229501", "N1PU-1510", "M238668", "N1PU-1511", "M238649", "N1PU-1512", "M238644", "N1PU-1513", "M238642", "N1PU-1514", "M238665", "N1PU-1515", "M238650", "N1PU-1516", "M238615", "N1PU-1517", "M238645", "N1PU-1518", "M238662", "N1PU-1519", "M238647", "N1PU-1520", "M238651", "N1PU-1521", "M238661", "N1PU-1522", "M238622", "N1PU-1523", "M238620", "N1PU-1524", "M238659", "N1PU-1525", "M276723", "N1PU-1526", "M238657", "N1PU-1527", "M238664", "N1PU-1528", "M238654", "N1PU-1529", "M238606", "N1PU-1530", "M238605", "N1PU-1531", "M238599", "N1PU-1532", "M238625", "N1PU-1533", "M238619", "N1PU-1534", "M238617", "N1PU-1535", "M238613", "N1PU-1536", "M238624", "N1PU-1537", "M238614", "N1PU-1551", "M250226", "N1PU-1552", "M250229", "N1PU-1553", "M269454", "N1PU-1554", "M269451", "N1PU-1555", "M269453", "N1PU-1556", "M273596", "N1PU-1557", "M273600", "N1PU-1558", "M273592", "N1PU-1559", "M273599", "N1PU-1560", "M273598", "N1PU-1561", "M273593", "N1PU-1574", "M291798", "N1PU-1575", "M291814", "N1PU-1576", "M291812", "N1PU-1577", "M291799", "N1PU-1578", "M291792", "N1PU-1579", "M291796", "N1PU-1580", "M291811", "N1PU-1581", "M291810", "N1PU-1582", "M291813", "N1PU-1583", "M291807", "N1PU-1584", "M291791", "N1PU-1585", "M291793", "N1PU-1586", "M291817", "N1PU-1587", "M291819", "N1PU-1588", "M291795", "N1PU-1589", "M291808", "N1PU-1590", "M291805", "N1PU-1591", "M291801", "N1PU-1592", "M291802", "N1PU-1593", "M291804", "N1PU-1594", "M291794", "N1PU-1595", "M291790", "N1PU-1596", "M291783", "N1PU-1597", "M291789", "N1PU-1598", "M291809", "N1PU-1599", "M291816", "N1PU-1600", "M291782", "N1PU-1601", "M291785", "N1PU-1602", "M291788", "N1PU-1603", "M291803", "N1PU-1604", "M291786", "N1PU-1605", "M291806", "N1PU-1628", "M295512", "N1PU-1629", "M295478", "N1PU-1630", "M295473", "N1PU-1631", "M295470", "N1PU-1632", "M295503", "N1PU-1633", "M295510", "N1PU-1634", "M295495", "N1PU-1635", "M295509", "N1PU-1636", "M295487", "N1PU-1637", "M295472", "N1PU-1638", "M295498", "N1PU-1639", "M295490", "N1PU-1640", "M295491", "N1PU-1641", "M295469", "N1PU-1642", "M295497", "N1PU-1643", "M295480", "N1PU-1644", "M295482", "N1PU-1645", "M295484", "N1PU-1646", "M295483", "N1PU-1647", "M295504", "N1PU-1648", "M295492", "N1PU-1649", "M295486", "N1PU-1650", "M295505", "N1PU-1651", "M295507", "N1PU-1652", "M295508", "N1PU-1653", "M295494", "N1PU-1654", "M295475", "N1PU-1655", "M295479", "N1PU-1656", "M295501", "N1PU-1657", "M295511", "N1PU-1658", "M295513", "N1PU-1659", "M295493", "N1PU-1661", "M295956", "N1PU-1662", "M295958", "N1PU-1663", "M295959", "N1PU-1664", "M295955", "N1PU-1665", "M295951", "N1PU-1666", "M295954", "N1PU-1667", "M295957", "N1PU-1668", "M295960", "N1PU-1704", "M309840", "N1PU-1705", "M309861", "N1PU-1706", "M309864", "N1PU-1707", "M309870", "N1PU-1708", "M309865", "N1PU-1709", "M309866", "N1PU-1710", "M309867", "N1PU-1711", "M309868", "N1PU-1712", "M309869", "N1PU-1713", "M310069", "N1PU-1714", "M310067", "N1PU-1715", "M310068", "N1PU-1717", "M321245", "N1PU-1718", "M321247", "N1PU-1719", "M320706", "N1PU-1720", "M323194", "N1PU-1721", "M324627", "N1PU-1722", "M324628", "N1PU-1723", "M324617", "N1PU-1724", "M324629", "N1PU-1725", "M324631", "N1PU-1726", "M324618", "N1PU-1727", "M324615", "N1PU-1728", "M326391", "N1PU-1729", "M326375", "N1PU-1730", "M326400", "N1PU-1731", "M326399", "N1PU-1732", "M320691", "N1PU-1733", "M320674", "N1PU-1734", "M320678", "N1PU-1735", "M320681", "N1PU-1736", "M320680", "N1PU-1737", "M320688", "N1PU-1738", "M320690", "N1PU-1739", "N320687", "N1PU-1740", "M320686", "N1PU-1741", "M320683", "N1PU-1742", "M320679", "N1PU-1743", "M382486", "N1PU-1744", "M320693", "N1PU-1745", "M320677", "N1PU-1746", "M320673", "N1PU-1747", "M320718", "N1PU-1748", "M320717", "N1PU-1749", "M320694", "N1PU-1750", "M320720", "N1PU-1751", "M320719", "N1PU-1752", "M320696", "N1PU-1753", "M320698", "N1PU-1754", "M320697", "N1PU-1755", "M323319", "N1PU-1756", "M323320", "N1PU-1757", "M323321", "N1PU-1811", "M332482", "N1PU-1812", "M332476", "N1PU-1813", "M332481", "N1PU-1814", "M332722", "N1PU-1815", "M332718", "N1PU-1816", "M332721", "N1PU-1817", "M332720", "N1PU-1818", "M332717", "N1PU-1828", "M351817", "N1PU-1829", "M351793", "N1PU-1876", "M364232", "N1PU-1913", "M392283", "N1PU-1914", "M392286", "N1PU-1915", "M392279", "N1PU-1916", "M392276", "N1PU-1917", "M392285", "N1PU-1918", "M392281", "N1PU-1920", "M393598", "N1PU-1921", "M393593", "N1PU-1926", "M401098", "N1PU-1928", "M401104", "N1PU-1932", "M401111", "N1PU-1933", "M402904", "N1PU-1934", "M398588", "N1PU-1935", "M398590", "N1PU-1936", "M398593", "N1PU-1937", "M398594", "N1PU-1938", "M398586", "N1PU-1939", "M402908", "N1PU-1940", "M402906", "N1PU-1941", "M402907", "N1PU-1942", "M402910", "N1PU-1943", "M402912", "N1PU-1944", "M402915", "N1PU-1945", "M402919", "N1PU-1946", "M402923", "N1PU-1947", "M404151", "N1PU-1948", "M404153", "N1PU-1949", "M404995", "N1PU-1951", "M405001", "N1PU-1955", "M399313", "N1PU-1956", "M399315", "N1PU-1957", "M399316", "N1PU-1958", "M399317", "N1PU-1959", "M399318", "N1PU-1960", "M399319", "N1PU-1961", "M399326", "N1PU-1962", "M399327", "N1SE-1669", "M310077", "N1SE-1670", "M310079", "N1SE-1671", "M310078", "N1SE-1672", "M310071", "N1SE-1673", "M310075", "N1SE-1674", "M310065", "N1SE-1675", "M310091", "N1SE-1676", "M310066", "N1SE-1677", "M310084", "N1SE-1678", "M310082", "N1SE-1679", "M310080", "N1SE-1680", "M310097", "N1SE-1681", "M310098", "N1SE-1682", "M310086", "N1SE-1683", "M310089", "N1SE-1684", "M310088", "N1SE-1685", "M310073", "N1SE-1686", "M310090", "N1SE-1687", "M310062", "N1SE-1688", "M310085", "N1SE-1689", "M310095", "N1SE-1690", "M310092", "N1SE-1691", "M310052", "N1SE-1692", "M310094", "N1SE-1693", "M310096", "N1SE-1802", "M321587", "N1SE-1803", "M321590", "N1SE-1804", "M321589", "N1SE-1805", "M321591", "N1SE-1806", "M321593", "N1SE-1807", "M321583", "N1SE-1808", "M321585", "N1SE-1809", "M321588", "N1SE-1810", "M321582", "N1SE-1824", "M334791", "N1SU-1798", "M321398", "N3A-0130", "M103846", "N3A-0132", "M108179", "N3A-0135", "M113572", "N3C-0103", "M063956", "N3G-0105", "M113886", "N3G-0106", "M114603", "N3M-5118", "M30100", "N3M-5133", "M30116", "N3M-5135", "M34048", "N3MC-0068", "M058140", "N3MC-0069", "M063950", "N3MC-0090", "M056241", "N3MC-0095", "M056237", "N3MC-0125", "M066620", "N3MC-0127", "M091792", "N3MC-0128", "M091794", "N3MC-0160", "M187253", "N3MC-0161", "M188132", "N3MC-0163", "M187251", "N3MC-0164", "M187254", "N3PU-0014", "M034495", "N3PU-0029", "M219412", "N3PU-0032", "M038638", "N3PU-0044", "M078653", "N3PU-0050", "M008445", "N3PU-0063", "M002196", "N3PU-0071", "M056250", "N3PU-0072", "M056265", "N3PU-0076", "M056256", "N3PU-0077", "M056248", "N3PU-0081", "M056253", "N3PU-0083", "M056260", "N3PU-0084", "M056252", "N3PU-0085", "M056259", "N3PU-0104", "M203272", "N3PU-0109", "M113574", "N3PU-0111", "M113577", "N3PU-0112", "M113576", "N3PU-0114", "M116523", "N3PU-0116", "M119791", "N3PU-0146", "M176053", "N3PU-0147", "M176055", "N3PU-0148", "M176059", "N3PU-0149", "M175991", "N3PU-0150", "M175990", "N3PU-0151", "M175957", "N3PU-0152", "M175947", "N3PU-0154", "M178489", "N3PU-0155", "M178490", "N3PU-0156", "M178488", "N3PU-0157", "M178501", "N3PU-0158", "M178494", "N3PU-0159", "M184382", "N3PU-0165", "M192993", "NI-PU1950", "M404998", "OBRAS CIVILES G", "2010", "ZONA OCCIDENTE", "1845" };
 
            // Limpiar y dividir los datos
            //List<Vehiculo> vehiculos = ProcesarDatos(ControlList);
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            var items = jsonData.text.ToObject<List<string>>();

            // Convert all items to uppercase for consistent matching
            for (int i = 0; i < items.Count; i++)
            {
                items[i] = items[i].Trim().ToUpper();
            }

            // Regular expressions
            Regex plateRegex = new Regex(@"^M\d+$");
            Regex kilometerRegex = new Regex(@"^\d{5,6}$");
            Regex litersRegex = new Regex(@"^\d+\.\d{3}$"); // Para litros con tres decimales, e.g., "57.324"
            Regex dateRegex = new Regex(@"(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)", RegexOptions.IgnoreCase);

            Regex autoRegex = new Regex(@"^AUT[O0]\.:", RegexOptions.IgnoreCase); // Captura AUTO.: y AUT0.:
            Regex placaIdRegex = new Regex(@"^PLACA\s*ID$", RegexOptions.IgnoreCase); // Para capturar "PLACAID" o "PLACA ID"
            Regex precioTotalRegex = new Regex(@"^C\d{1,3}(?:[.,]\d{3})*(?:\.\d{2})?$"); // Para "C2,112.02"
            foreach (var item in items)
            {
                if (item.Contains("PUMA") || item.Contains("ESTACION"))
                {
                    data.Estacion = item;
                }
                if (dateRegex.IsMatch(item))
                {
                    // Attempt to parse date
                    DateTime date;
                    if (TryParseDate(item, out date))
                    {
                        data.FechaHora = date;
                    }
                }
                else if (plateRegex.IsMatch(item))
                {
                    // Item matches plate pattern
                    data.Control = item;
                    data.Unidad = item;


                    if (vehiculos.Count(x=>x.MatriculaLimpia==item)>0)
                    {
                        data.NumeroCompra = vehiculos.FirstOrDefault(x => x.MatriculaLimpia == item).unidad;
                         data.Unidad = data.NumeroCompra;
                    }
                }
                else if (kilometerRegex.IsMatch(item))
                {
                    // Item matches kilometraje pattern
                    data.Odometro = item;
                }
                else if (litersRegex.IsMatch(item))
                {
                    // Coincide con patrón de litros
                    double litros;
                    if (double.TryParse(item.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out litros))
                    {
                        data.VolumenLitros = litros;
                    }
                }
                else if (precioTotalRegex.IsMatch(item))
                {
                    // Coincide con patrón de PrecioTotal
                    double precioTotal;
                    string precioStr = item.Substring(1).Replace(",", "").Replace(".", "."); // Eliminar la 'C' y normalizar
                    if (double.TryParse(precioStr, NumberStyles.Any, CultureInfo.InvariantCulture, out precioTotal))
                    {
                        data.PrecioTotal = precioTotal;
                    }
                }
             
                else if (autoRegex.IsMatch(item))
                {
                    // Extraer el valor después de "AUTO.:" o "AUT0.:"
                    data.Referencia = item.Substring(item.IndexOf(':') + 1).Trim();
                }
                if (string.IsNullOrEmpty(data.Control))
                {
                    

                    var matchedControl = vehiculos.Where(control => item.Contains(control.Codigo.ToUpper()) || item.Contains(control.unidad.ToUpper()));
                    if (matchedControl != null && matchedControl.Count() > 0)
                    {
                        string codigo = matchedControl.FirstOrDefault().unidad;


                        if (!string.IsNullOrEmpty(codigo))
                        {
                            data.NumeroCompra = codigo;
                            data.Unidad = codigo;

                        }
                    }
                }
            }

            return data;
        }

        private bool TryParseDate(string dateString, out DateTime date)
        {
            date = DateTime.MinValue;

            // Expresión regular para extraer componentes de la fecha
            // Maneja formatos como "OCT01,24-09:44", "SEP 18,24-16:03" y "OCT 15.24-17:08"
                 var regex = new Regex(@"^(?<Month>[A-Z]{3})\s?(?<Day>\d{2})[.,](?<Year>\d{2})[·\-](?<Hour>\d{2}):(?<Minute>\d{2})$", RegexOptions.IgnoreCase);

            var match = regex.Match(dateString);

            if (match.Success)
            {
                string monthAbbr = match.Groups["Month"].Value;
                string dayStr = match.Groups["Day"].Value;
                string yearStr = match.Groups["Year"].Value;
                string hourStr = match.Groups["Hour"].Value;
                string minuteStr = match.Groups["Minute"].Value;

                // Mapeo de abreviaturas de meses a números
                var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    {"JAN",1}, {"FEB",2}, {"MAR",3}, {"APR",4},
                    {"MAY",5}, {"JUN",6}, {"JUL",7}, {"AUG",8},
                    {"SEP",9}, {"OCT",10}, {"NOV",11}, {"DEC",12}
                };

                if (monthMap.TryGetValue(monthAbbr, out int month))
                {
                    if (int.TryParse(dayStr, out int day) &&
                        int.TryParse(yearStr, out int year) &&
                        int.TryParse(hourStr, out int hour) &&
                        int.TryParse(minuteStr, out int minute))
                    {
                        // Asumiendo que el año es 2000 + yy
                        year += 2000;

                        try
                        {
                            date = new DateTime(year, month, day, hour, minute, 0);
                            return true;
                        }
                        catch
                        {
                            // Fecha inválida
                        }
                    }
                }
            }

            return false;
        }

    }
}
