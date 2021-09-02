using barApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace barApp.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
            ViewData["Alert"] = null;
        }

        #region Dashboard
        //inicio
        public ActionResult Index()
        {
            using (var entity = new barbdEntities())
            {
                ViewBag.VendedorOrigen = entity.Usuario.Where(x => x.idRol == 2).Select(x => new { x.idUsuario, x.nombre }).ToList();
                ViewData["ModoPago"] = entity.ModoPago.Where(m => m.numPago > 0).ToList();
                ViewData["ClienteOrigen"] = new List<Cliente>();

                int? cuadreActual = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false))?.idCuadre;
                IEnumerable<Venta> cuentas = entity.Venta.Where(v => v.idCuadre == cuadreActual).AsEnumerable().Select(v => UntrackedVenta(v));
                ViewBag.Cuentas = cuentas.ToArray();
                ViewBag.Detalles = entity.DetalleVenta.AsEnumerable().Where(dv => cuentas.Select(c => c.idVenta).Contains(dv.idVenta)).Select(dv => UntrackedDetalle(dv)).ToArray();
                ViewBag.Vendedores = entity.Usuario.Where(u => u.idRol == 2).AsEnumerable().Select(u => UntrackedUsuario(u)).ToArray();
                ViewBag.Usuarios = entity.Usuario.AsEnumerable().Select(u => UntrackedUsuario(u)).ToArray();
                ViewBag.GastosTotal = JsonConvert.SerializeObject(entity.Gastos.Where(g => g.idCuadre == cuadreActual).Sum(g => g.cantidad));
            }

            return View();
        }
        private Venta UntrackedVenta(Venta venta)
        {
            return new Venta
            {
                fecha = venta.fecha,
                idCliente = venta.idCliente,
                idUsuario = venta.idUsuario,
                idVenta = venta.idVenta,
                IVA = venta.IVA,
                ordenCerrada = venta.ordenCerrada,
                ordenFacturada = venta.ordenFacturada,
                total = venta.total
            };
        }
        private DetalleVenta UntrackedDetalle(DetalleVenta detalleVenta)
        {
            return new DetalleVenta
            {
                cantidad = detalleVenta.cantidad,
                despachada = detalleVenta.despachada,
                espcial = detalleVenta.espcial,
                idDetalle = detalleVenta.idDetalle,
                idProducto = detalleVenta.idProducto,
                idVenta = detalleVenta.idVenta,
                precioEntrada = detalleVenta.precioEntrada,
                precioVenta = detalleVenta.precioVenta,
                subTotal = detalleVenta.subTotal
            };
        }
        private Usuario UntrackedUsuario(Usuario usuario)
        {
            return new Usuario
            {
                activo = usuario.activo,
                contrasena = usuario.contrasena,
                Correo = usuario.Correo,
                EnvioCorreo = usuario.EnvioCorreo,
                idRol = usuario.idRol,
                idTarjeta = usuario.idTarjeta,
                idUsuario = usuario.idUsuario,
                nombre = usuario.nombre,
                resetContrasena = usuario.resetContrasena
            };
        }
        #endregion

        #region Home
        //cuadrar
        [HttpPost]
        public ActionResult Cuadrar()
        {
            try
            {
                using (barbdEntities context = new barbdEntities())
                {
                    if (context.Venta.AsEnumerable().Any(v => !v.ordenFacturada.GetValueOrDefault(false)))
                    {
                        InfoMensaje _Info = new InfoMensaje
                        {
                            Tipo = "Notificacion",
                            Mensaje = "No se puede cuadrar con cuentas abiertas.\nFacture las cuentas abiertas he intente nuevamente."
                        };

                        return Json(_Info, JsonRequestBehavior.DenyGet);
                    }

                    IDictionary<string, string> header = new Dictionary<string, string>();
                    header.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                    header.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                    string[][] resumen =
                        context.Database.SqlQuery<CuadreResumen>("exec sp_cuadre_resumen")
                        .Select(r =>
                            new string[2]
                            {
                                r.Descripcion,
                                r.Valor.ToString("$#,0.00")
                            }
                        ).ToArray();

                    int? idCuadre = context.Cuadre.AsEnumerable().SingleOrDefault(c => c.cerrado.GetValueOrDefault(false) == false).idCuadre;
                    Gastos[] gastos = context.Gastos.Where(g => g.idCuadre == idCuadre).ToArray();
                    Dictionary<string, string> gastosResumen = new Dictionary<string, string>();
                    gastosResumen.Add("Total de gastos", gastos.Sum(g => g.cantidad).GetValueOrDefault(0).ToString("$#,0.00"));

                    IEnumerable<CuadreTable> cuadreBar = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_bar");
                    Dictionary<string, string> barResumen = new Dictionary<string, string>();
                    barResumen.Add("Productos vendidos", cuadreBar.Sum(g => g.Cantidad).ToString());
                    barResumen.Add("Subtotal", cuadreBar.Sum(g => g.Total).ToString("$#,0.00"));
                    barResumen.Add("Total costos", cuadreBar.Sum(g => g.Costo).ToString("$#,0.00"));
                    barResumen.Add("Total beneficios", cuadreBar.Sum(g => g.Beneficio).ToString("$#,0.00"));

                    IEnumerable<CuadreTable> cuadreRest = context.Database.SqlQuery<CuadreTable>("exec sp_cuadre_restaurante");
                    Dictionary<string, string> restResumen = new Dictionary<string, string>();
                    restResumen.Add("Productos vendidos", cuadreRest.Sum(g => g.Cantidad).ToString());
                    restResumen.Add("Subtotal", cuadreRest.Sum(g => g.Total).ToString("$#,0.00"));
                    restResumen.Add("Total costos", cuadreRest.Sum(g => g.Costo).ToString("$#,0.00"));
                    restResumen.Add("Total beneficios", cuadreRest.Sum(g => g.Beneficio).ToString("$#,0.00"));

                    Printer printer = new Printer();

                    printer.AddTitle("Cuadre");
                    printer.AddSpace(3);
                    printer.AddDescriptionList(header, 2);
                    printer.AddSpace(5);
                    printer.AddSubtitle("Resumen");
                    printer.AddSpace(2);
                    printer.AddTable(new string[2] { "Descripción", "Valor" }, resumen);
                    printer.AddSpace(5);
                    printer.AddSubtitle("Gastos");
                    printer.AddSpace(2);
                    printer.AddTable(new string[2] { "Nombre", "Precio" }, gastos.Select(g => new string[2] { g.descripcion, g.cantidad.GetValueOrDefault(0).ToString("$#,0.00") }).ToArray());
                    printer.AddSpace();
                    printer.AddTableDetails(gastosResumen, 2);
                    printer.AddSpace(5);
                    printer.AddSubtitle("Bar");
                    printer.AddSpace(2);
                    printer.AddTable(
                        new string[6] { "Producto", "Cantidad", "P/UND", "Subtotal", "Costo" ,"Beneficio" },
                        cuadreBar.Select(b => new string[7] { b.Nombre.ToUpper(), b.CodigoProducto.PadLeft(5, '0'), b.Cantidad.ToString(), b.PrecioVenta.ToString("$#,0.00"), b.Total.ToString("$#,0.00"), b.Costo.ToString("$#,0.00"), b.Beneficio.ToString("$#,0.00") }).ToArray(),
                        true);
                    printer.AddSpace();
                    printer.AddTableDetails(barResumen, 6);
                    printer.AddSpace(5);
                    printer.AddSubtitle("Restaurante");
                    printer.AddSpace(2);
                    printer.AddTable(
                        new string[6] { "Producto", "Cantidad", "P/UND", "Subtotal", "Costo", "Beneficio" },
                        cuadreRest.Select(r => new string[7] { r.Nombre.ToUpper(), r.CodigoProducto.PadLeft(5, '0'), r.Cantidad.ToString(), r.PrecioVenta.ToString("$#,0.00"), r.Total.ToString("$#,0.00"), r.Costo.ToString("$#,0.00"), r.Beneficio.ToString("$#,0.00") }).ToArray(),
                        true);
                    printer.AddSpace();
                    printer.AddTableDetails(restResumen, 6);

                    printer.Print();

                    Cuadre cuadre = context.Cuadre.Find(idCuadre);
                    cuadre.cerrado = true;
                    context.SaveChanges();

                    InfoMensaje Info = new InfoMensaje
                    {
                        Tipo = "Ready",
                        Mensaje = "Cuadre realizada con exito"
                    };

                    return Json(Info, JsonRequestBehavior.DenyGet);
                }
            }
            catch (Exception ex)
            {
                InfoMensaje Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.DenyGet);
            }
        }

        //facturar cuentas
        public ActionResult FacturarCuenta(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ListCliente = new List<Cliente>();
                var Clientes = entity.Venta.AsEnumerable().Where(x => x.idUsuario == usuario.idUsuario && x.ordenCerrada == true && !x.ordenFacturada.GetValueOrDefault(false)).ToList();

                foreach (var item in Clientes)
                {
                    var ObjCliente = new Cliente()
                    {
                        idCliente = item.idVenta,
                        nombre = "Orden No." + item.idVenta + " -- " + item.Cliente.nombre.ToUpper() + " -- " + item.total.ToString("$#,0.00")
                    };

                    ListCliente.Add(ObjCliente);
                }

                return Json(ListCliente, JsonRequestBehavior.AllowGet);
            }
        }

        //facturar cuentas
        [HttpPost]
        public ActionResult FacturarReady(RequestVenta venta)
        {
            try
            {
                string empresa;
                string rnc;
                string telefono;
                string saludo;
                string cliente;
                string vendedor;
                decimal subtotal;
                decimal descuentos;
                decimal itbis;
                string[][] data;

                using (barbdEntities context = new barbdEntities())
                {
                    empresa = context.Configuraciones.Find("Empresa").Value;
                    rnc = context.Configuraciones.Find("RNC").Value;
                    telefono = context.Configuraciones.Find("Telefono").Value;
                    saludo = context.Configuraciones.Find("Saludo").Value;

                    Venta _venta = context.Venta.Find(venta.idVenta);
                    _venta.ordenFacturada = true;

                    for (int index = 0; index < venta.Factura.Length; index++)
                    {
                        Factura factura = new Factura()
                        {
                            descuento = venta.Factura[index].descuento,
                            fecha = DateTime.Now,
                            IVA = 18,
                            idVenta = venta.Factura[index].idVenta,
                            numFactura = venta.Factura[index].numFactura,
                            numPago = venta.Factura[index].numPago,
                            TieneCedito = false,
                            total = venta.Factura[index].total
                        };

                        if (venta.Factura[index].idUsuario != 0)
                        {
                            factura.TieneCedito = true;

                            Creditos credito = new Creditos()
                            {
                                idUsuario = venta.Factura[index].idUsuario,
                                MontoRestante = (decimal)factura.total,
                                numFactura = factura.numFactura
                            };

                            context.Creditos.Add(credito);
                        }

                        context.Factura.Add(factura);
                    }

                    context.SaveChanges();

                    cliente = context.Cliente.Find(_venta.idCliente).nombre;
                    vendedor = context.Usuario.Find(_venta.idUsuario).nombre;
                    subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == venta.idVenta).Sum(vd => vd.subTotal);
                    descuentos = subtotal - (decimal)venta.Factura.Sum(f => f.total);
                    itbis = context.DetalleVenta.Where(vd => vd.idVenta == venta.idVenta).Sum(vd => vd.precioVenta).GetValueOrDefault(0) * 0.18m;
                    data =
                        context.DetalleVenta
                        .Where(vd => vd.idVenta == venta.idVenta)
                        .AsEnumerable()
                        .GroupBy(
                            vd => vd.Producto,
                            vd => new
                            {
                                vd.cantidad,
                                vd.precioVenta,
                                vd.subTotal
                            },
                            (producto, grupo) => new
                            {
                                producto.nombre,
                                producto.idProducto,
                                cantidad = grupo.Sum(g => g.cantidad),
                                precioVenta = grupo.Sum(g => g.precioVenta.GetValueOrDefault(0)),
                                subTotal = grupo.Sum(g => g.subTotal)
                            }
                        )
                        .Select(vd => new string[5] {
                        vd.nombre.ToUpper(),
                        vd.idProducto.PadLeft(5, '0'),
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round(vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round(vd.subTotal, 2).ToString("$#,0.00")
                        })
                        .ToArray();

                    Printer printer = new Printer();

                    IDictionary<string, string> list1 = new Dictionary<string, string>();
                    list1.Add("Cliente", cliente.ToUpper());
                    list1.Add("Orden", venta.idVenta.ToString());
                    list1.Add("Vendedor/a", vendedor.ToUpper());
                    list1.Add("RNC", rnc);
                    list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
                    list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

                    Dictionary<string, string> tableDetails = new Dictionary<string, string>();
                    tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
                    tableDetails.Add("ITBIS", itbis.ToString("$#,0.00"));
                    tableDetails.Add("Descuento", descuentos.ToString("$#,0.00") + " (" + venta.Factura[0].descuento.GetValueOrDefault(0).ToString() + "%)");
                    Dictionary<string, string> tableTotal = new Dictionary<string, string>();
                    tableTotal.Add("TOTAL", (subtotal - descuentos).ToString("$#,0.00"));

                    printer.AddTitle("Factura de consumidor final");
                    printer.AddSpace(2);
                    printer.AddString(empresa, true, System.Drawing.StringAlignment.Center);
                    printer.AddString(telefono, alignment: System.Drawing.StringAlignment.Center);
                    printer.AddSpace(2);
                    printer.AddSubtitle("Información general");
                    printer.AddSpace();
                    printer.AddDescriptionList(list1, 2);
                    printer.AddSpace(2);
                    printer.AddSubtitle("Productos");
                    printer.AddSpace();
                    printer.AddTable(new string[4] { "Código", "Cantidad", "Precio", "Subtotal" }, data, true);
                    printer.AddSpace();
                    printer.AddTableDetails(tableDetails, 4);
                    printer.AddSpace();
                    printer.AddTableDetails(tableTotal, 4);
                    printer.AddSpace(2);
                    printer.AddBarCode(venta.idVenta.ToString());
                    printer.AddString(saludo, alignment: System.Drawing.StringAlignment.Center);
                    printer.AddSpace(2);

                    printer.Print();

                    InfoMensaje Info = new InfoMensaje
                    {
                        Tipo = "Ready",
                        Mensaje = "Facturación realizada con exito"
                    };

                    return Json(Info, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                InfoMensaje Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.AllowGet);
            }
        }

        //transferir cuentas
        public ActionResult TransferirCuenta(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ListCliente = new List<Cliente>();
                var Clientes = entity.Venta.Include("Cliente").Where(x => x.idUsuario == usuario.idUsuario && x.ordenCerrada == null).Select(x => new { x.Cliente.idCliente, x.Cliente.nombre, x.idVenta, x.total }).ToList();

                foreach (var item in Clientes)
                {
                    var ObjCliente = new Cliente()
                    {
                        idCliente = item.idVenta,
                        nombre = "Orden No." + item.idVenta + " -- " + item.nombre.ToUpper() + " -- " + item.total.ToString("$#,0.00")
                    };

                    ListCliente.Add(ObjCliente);
                }

                return Json(ListCliente, JsonRequestBehavior.AllowGet);
            }
        }

        //transferir cuentas
        [HttpPost]
        public ActionResult TransferirReady(Venta venta)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (venta.idUsuario > 0)
                    {
                        var ObjVenta = entity.Venta.Find(venta.idVenta);
                        ObjVenta.idUsuario = venta.idUsuario;
                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Ready",
                            Mensaje = "Tranferencia realizada con exito"

                        };

                        return Json(Info, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Notificacion",
                            Mensaje = "Seleccione camarer@ destino"

                        };

                        return Json(Info, JsonRequestBehavior.AllowGet);
                    }
                }
                catch (Exception ex)
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };

                    return Json(Info, JsonRequestBehavior.AllowGet);
                }
            }
        }

        //Enlazar cuentas
        [HttpPost]
        public JsonResult EnlazarReady(List<string> ListadoEnlazar)
        {
            try
            {
                int CuentaOrigen = int.Parse(ListadoEnlazar[0]);

                using (var entity = new barbdEntities())
                {
                    for (int i = 1; i < ListadoEnlazar.Count; i++)
                    {
                        object[] xparams = {
                        new SqlParameter("@IdventaOrigen",  CuentaOrigen),
                        new SqlParameter("@IdVentaEnlazar",  int.Parse(ListadoEnlazar[i]))};

                        entity.Database.ExecuteSqlCommand("exec sp_EnlazarCuenta @IdventaOrigen, @IdVentaEnlazar", xparams);
                    }

                    var Info = new InfoMensaje
                    {
                        Tipo = "Ready",
                        Mensaje = "Cuenta Enlazada con exito"
                    };

                    return Json(Info, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                var Info = new InfoMensaje
                {
                    Tipo = "Error",
                    Mensaje = ex.Message
                };

                return Json(Info, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Categoria
        //Inicio
        public ActionResult Categoria()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                ViewData["ListaCategoria"] = entity.Categoria.ToList();
            }
            return PartialView();
        }

        //Crear Categoria
        public ActionResult CrearCategoria(Categoria Objcategoria)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    entity.Categoria.Add(Objcategoria);
                    entity.SaveChanges();
                    ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity.Categoria.ToList();
                    var Info = new InfoMensaje
                    {
                        Tipo = "Success",
                        Mensaje = "Categoria Agregada exitosamente"

                    };
                    ViewData["Alert"] = Info;

                    return PartialView("ListaCategoria");
                }
            }
            catch (Exception ex)
            {
                using (var entity1 = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity1.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity1.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
        }

        //Eliminar Categoria
        public ActionResult EliminarCategoria(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.Producto.Count(x => x.idCategoria == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjCategoria = entity.Categoria.Find(Id);
                        entity.Categoria.Remove(ObjCategoria);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                        ViewData["ListaCategoria"] = entity.Categoria.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Categoria Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaCategoria");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Categoria no puede ser eliminada, existen productos asignados"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                        ViewData["ListaCategoria"] = entity.Categoria.ToList();

                        return PartialView("ListaCategoria");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity1 = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity1.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity1.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
        }

        //buscar para editar Categoria
        [HttpPost]
        public ActionResult BuscarEditarCategoria(Categoria categoria)
        {
            using (var entity = new barbdEntities())
            {
                var ObjCategoria = entity.Categoria.Find(categoria.idCategoria);

                var Ca = new Categoria
                {
                    idCategoria = ObjCategoria.idCategoria,
                    idTipoCategoria = ObjCategoria.idTipoCategoria,
                    nombre = ObjCategoria.nombre

                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        //editar categoria
        [HttpPost]
        public ActionResult EditarCategoria(Categoria categoria)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ObjCategoria = entity.Categoria.Find(categoria.idCategoria);
                    ObjCategoria.idTipoCategoria = categoria.idTipoCategoria;
                    ObjCategoria.nombre = categoria.nombre;
                    entity.SaveChanges();

                    var Info = new InfoMensaje
                    {
                        Tipo = "Success",
                        Mensaje = "Categoria Editada exitosamente"

                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
            catch (Exception ex)
            {
                using (var entity1 = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity1.TipoCategoria.ToList();
                    ViewData["ListaCategoria"] = entity1.Categoria.ToList();

                    return PartialView("ListaCategoria");
                }
            }
        }
        #endregion

        #region Producto
        //Producto Inicio
        public ActionResult Producto()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TipoCategoria"] = entity.Categoria.ToList();
                ViewData["ListaProducto"] = entity.Producto.ToList();
            }

            return PartialView();
        }

        public ActionResult CrearProducto(Producto ObjProducto)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        entity.Producto.Add(ObjProducto);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.Categoria.ToList();
                        ViewData["ListaProducto"] = entity.Producto.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Agregado exitosamente"
                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaProducto");
                    }
                    else
                    {
                        ViewData["TipoCategoria"] = entity.Categoria.ToList();
                        ViewData["ListaProducto"] = entity.Producto.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaProducto");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity1.Categoria.ToList();
                        ViewData["ListaProducto"] = entity1.Producto.ToList();

                        return PartialView("ListaProducto");
                    }
                }
            }
        }

        //Eliminar Producto
        [HttpPost]
        public ActionResult EliminarProducto(string Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.DetalleVenta.Count(x => x.idProducto == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjProducto = entity.Producto.Find(Id);
                        entity.Producto.Remove(ObjProducto);
                        entity.SaveChanges();
                        ViewData["TipoCategoria"] = entity.Categoria.ToList();
                        ViewData["ListaProducto"] = entity.Producto.ToList();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaProducto");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto no puede ser eliminada, existen ventas con producto"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.ToList();
                        ViewData["ListaProducto"] = entity.Producto.ToList();

                        return PartialView("ListaProducto");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    ViewData["Alert"] = Info;
                    ViewData["TipoCategoria"] = entity.Categoria.ToList();
                    ViewData["ListaProducto"] = entity.Producto.ToList();

                    return PartialView("ListaProducto");
                }
            }
        }

        //buscar para editar Producto
        [HttpPost]
        public ActionResult BuscarEditarProducto(Producto producto)
        {
            using (var entity = new barbdEntities())
            {
                var ObjProducto = entity.Producto.Find(producto.idProducto);

                var Ca = new Producto
                {
                    idProducto = ObjProducto.idProducto,
                    nombre = ObjProducto.nombre,
                    precioVenta = ObjProducto.precioVenta,
                    precioAlmacen = ObjProducto.precioAlmacen,
                    idCategoria = ObjProducto.idCategoria,
                    activo = ObjProducto.activo
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        //eliminar producto
        [HttpPost]
        public ActionResult EditarProducto(Producto producto)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    string Activo = Request["activoE"].ToString();

                    if (Activo == "false")
                    {
                        producto.activo = false;
                    }
                    else
                    {
                        producto.activo = true;
                    }

                    if (!string.IsNullOrEmpty(producto.precioAlmacen.ToString()) && !string.IsNullOrEmpty(producto.precioVenta.ToString()) && !string.IsNullOrEmpty(producto.nombre.ToString()))
                    {
                        var ObjProducto = entity.Producto.Find(producto.idProducto);
                        ObjProducto.idCategoria = producto.idCategoria;
                        ObjProducto.nombre = producto.nombre;
                        ObjProducto.precioVenta = producto.precioVenta;
                        ObjProducto.precioAlmacen = producto.precioAlmacen;
                        ObjProducto.activo = producto.activo;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.ToList();
                        ViewData["ListaProducto"] = entity.Producto.ToList();

                        return PartialView("ListaProducto");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto Tiene campos Invalidos"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity.Categoria.ToList();
                        ViewData["ListaProducto"] = entity.Producto.ToList();

                        return PartialView("ListaProducto");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message
                        };
                        ViewData["Alert"] = Info;
                        ViewData["TipoCategoria"] = entity1.Categoria.ToList();
                        ViewData["ListaProducto"] = entity1.Producto.ToList();

                        return PartialView("ListaProducto");
                    }
                }
            }
        }
        #endregion

        #region ModoPago
        //Inicio
        public ActionResult ModoPago()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["ListaModoPago"] = entity.ModoPago.Where(m => m.numPago > 0).ToList();
            }

            return PartialView();
        }

        //Crear Modo de pago
        public ActionResult CrearModoPago(ModoPago ObjModoPago)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        entity.ModoPago.Add(ObjModoPago);
                        entity.SaveChanges();
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Modo de Pago Agregado exitosamente"
                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaModoPago");
                    }
                    else
                    {
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Modo de Pago Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaModoPago");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
            }
        }

        //Eliminar Modo de pago
        [HttpPost]
        public ActionResult EliminarModoPago(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarProducto = entity.Factura.Count(x => x.numPago == Id);

                    if (ValidarProducto == 0)
                    {
                        var ObjModoPago = entity.ModoPago.Find(Id);
                        entity.ModoPago.Remove(ObjModoPago);
                        entity.SaveChanges();
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Modo de Pago Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaModoPago");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Modo de Pago no puede ser eliminada, existen ventas"
                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                    return PartialView("ListaModoPago");
                }
            }
        }

        //buscar para editar Modo de pago
        [HttpPost]
        public ActionResult BuscarEditarModoPago(ModoPago modoPago)
        {
            using (var entity = new barbdEntities())
            {
                var ObjModoPago = entity.ModoPago.Find(modoPago.numPago);

                var Ca = new ModoPago
                {
                    numPago = ObjModoPago.numPago,
                    nombre = ObjModoPago.nombre,
                    otroDetalles = ObjModoPago.otroDetalles
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        //eliminar producto

        [HttpPost]
        public ActionResult EditarModoPago(ModoPago modoPago)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var ObjModoPago = entity.ModoPago.Find(modoPago.numPago);
                        ObjModoPago.nombre = modoPago.nombre;
                        ObjModoPago.otroDetalles = modoPago.otroDetalles;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Modo de Pago Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Modo de Pago Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaModoPago"] = entity.ModoPago.ToList();

                        return PartialView("ListaModoPago");
                    }
                }
            }
        }
        #endregion

        #region Suplidor
        //Inicio
        public ActionResult Suplidor()
        {
            using (var entity = new barbdEntities())
            {

                ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
            }

            return PartialView();
        }

        //Crear suplidor
        public ActionResult CrearSuplidor(Suplidor ObjSuplidor)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        entity.Suplidor.Add(ObjSuplidor);
                        entity.SaveChanges();
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Suplidor Agregado exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaSuplidor");
                    }
                    else
                    {
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Suplidor Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaSuplidor");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
            }
        }

        //Eliminar suplidor
        [HttpPost]
        public ActionResult EliminarSuplidor(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ValidarSuplidor = entity.Inventario.Count(x => x.idSuplidor == Id);

                    if (ValidarSuplidor == 0)
                    {
                        var ObjSuplidor = entity.Suplidor.Find(Id);
                        entity.Suplidor.Remove(ObjSuplidor);
                        entity.SaveChanges();
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Suplidor Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaSuplidor");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Suplidor no puede ser eliminada, existen compras"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                    return PartialView("ListaSuplidor");
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarSuplidor(Suplidor suplidor)
        {
            using (var entity = new barbdEntities())
            {
                var ObjSuplidor = entity.Suplidor.Find(suplidor.idSuplidor);

                var Ca = new Suplidor
                {
                    idSuplidor = ObjSuplidor.idSuplidor,
                    nombre = ObjSuplidor.nombre,
                    rnc = ObjSuplidor.rnc,
                    telefono = ObjSuplidor.telefono,
                    correo = ObjSuplidor.correo,
                    direccion = ObjSuplidor.direccion
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarSuplidor(Suplidor suplidor)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var ObjSuplidor = entity.Suplidor.Find(suplidor.idSuplidor);
                        ObjSuplidor.idSuplidor = suplidor.idSuplidor;
                        ObjSuplidor.nombre = suplidor.nombre;
                        ObjSuplidor.rnc = suplidor.rnc;
                        ObjSuplidor.telefono = suplidor.telefono;
                        ObjSuplidor.correo = suplidor.correo;
                        ObjSuplidor.direccion = suplidor.direccion;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Suplidor Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Suplidor Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaSuplidor"] = entity.Suplidor.ToList();

                        return PartialView("ListaSuplidor");
                    }
                }
            }
        }
        #endregion

        #region Gastos
        public ActionResult Gastos()
        {
            using (var entity = new barbdEntities())
            {
                int idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;
                ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == idCuadre).ToList();
            }

            return PartialView();
        }

        public ActionResult CrearGastos(Gastos ObjGastos)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    ObjGastos.idCuadre = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre;

                    if (ModelState.IsValid)
                    {
                        entity.Gastos.Add(ObjGastos);
                        entity.SaveChanges();
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Gasto Agregado exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaGastos");
                    }
                    else
                    {
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Gasto Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaGastos");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();

                        return PartialView("ListaGastos");
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult EliminarGastos(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var ObjGastos = entity.Gastos.Find(Id);
                    entity.Gastos.Remove(ObjGastos);
                    entity.SaveChanges();
                    ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();
                    var Info = new InfoMensaje
                    {
                        Tipo = "Success",
                        Mensaje = "Gasto Eliminada exitosamente"

                    };
                    ViewData["Alert"] = Info;

                    return PartialView("ListaGastos");
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();

                    return PartialView("ListaGastos");
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarGastos(Gastos gastos)
        {
            using (var entity = new barbdEntities())
            {
                var ObjGatos = entity.Gastos.Find(gastos.IdGastos);

                var Ca = new Gastos
                {
                    IdGastos = ObjGatos.IdGastos,
                    descripcion = ObjGatos.descripcion,
                    cantidad = ObjGatos.cantidad,
                    idCuadre = ObjGatos.idCuadre
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarGastos(Gastos gastos)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var ObjGastos = entity.Gastos.Find(gastos.IdGastos);
                        ObjGastos.descripcion = gastos.descripcion;
                        ObjGastos.cantidad = gastos.cantidad;

                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Gasto Editada exitosamente"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();

                        return PartialView("ListaGastos");
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Gasto Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();

                        return PartialView("ListaGastos");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();

                        return PartialView("ListaGastos");
                    }
                }
            }
        }
        #endregion

        #region Usuario
        public ActionResult Usuario()
        {
            using (var entity = new barbdEntities())
            {

                ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                ViewData["TipoRolUsuario"] = entity.Roles.ToList();
            }

            return PartialView();
        }

        public ActionResult CrearUsuario(Usuario ObjUsuario)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        if (ObjUsuario.contrasena.Length == 4)
                        {
                            var ValidarContrasena = entity.Usuario.Where(x => x.contrasena == ObjUsuario.contrasena).Count();

                            if (ValidarContrasena == 0)
                            {
                                var ValidarNombre = entity.Usuario.Where(x => x.nombre == ObjUsuario.nombre).Count();

                                if (ValidarNombre == 0)
                                {
                                    entity.Usuario.Add(ObjUsuario);
                                    entity.SaveChanges();
                                    ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                                    ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                                    var Info = new InfoMensaje
                                    {
                                        Tipo = "Success",
                                        Mensaje = "Usuario Agregado exitosamente"
                                    };
                                    ViewData["Alert"] = Info;

                                    return PartialView("ListaUsuario");
                                }
                                else
                                {
                                    var Info2 = new InfoMensaje
                                    {
                                        Tipo = "js",
                                        Mensaje = "Nombre ya existe por otro usuario"

                                    };

                                    return Json(Info2, JsonRequestBehavior.AllowGet);
                                }
                            }
                            else
                            {
                                var Info2 = new InfoMensaje
                                {
                                    Tipo = "js",
                                    Mensaje = "Contrasena ya existe por otro usuario"
                                };

                                return Json(Info2, JsonRequestBehavior.AllowGet);
                            }
                        }
                        else
                        {
                            var Info1 = new InfoMensaje
                            {
                                Tipo = "js",
                                Mensaje = "Contrasena debe ser 4 Digitos"
                            };

                            return Json(Info1, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Usuario Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaUsuario");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                        return PartialView("ListaUsuario");
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult EliminarUsuario(int Id)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    var Validar = entity.Venta.Count(x => x.idUsuario == Id);

                    if (Validar == 0)
                    {
                        var ObjUsuario = entity.Usuario.Find(Id);
                        entity.Usuario.Remove(ObjUsuario);
                        entity.SaveChanges();
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Usuario Eliminada exitosamente"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaUsuario");
                    }
                    else
                    {
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Usuario no se puede eliminar, tiene ventas realizadas"

                        };
                        ViewData["Alert"] = Info;

                        return PartialView("ListaUsuario");
                    }
                }
            }
            catch (Exception ex)
            {
                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };
                    ViewData["Alert"] = Info;
                    ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                    ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                    return PartialView("ListaUsuario");
                }
            }
        }

        [HttpPost]
        public ActionResult BuscarEditarUsuario(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ObjUsuario = entity.Usuario.Find(usuario.idUsuario);

                var Ca = new Usuario
                {
                    idUsuario = ObjUsuario.idUsuario,
                    nombre = ObjUsuario.nombre,
                    contrasena = ObjUsuario.contrasena,
                    idTarjeta = ObjUsuario.idTarjeta,
                    Correo = ObjUsuario.Correo,
                    idRol = ObjUsuario.idRol,
                    activo = ObjUsuario.activo,
                    resetContrasena = ObjUsuario.resetContrasena,
                    EnvioCorreo = ObjUsuario.EnvioCorreo
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditarUsuario(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        if (usuario.contrasena.Length == 4)
                        {
                            string Activo = Request["activoUsuarioE"].ToString();
                            string resetContrasena = Request["resetContrasenaUsuarioE"].ToString();
                            string EnvioCorreo = Request["EnvioCorreoUsuarioE"].ToString();
                            var ObjUsuario = entity.Usuario.Find(usuario.idUsuario);

                            ObjUsuario.nombre = usuario.nombre;
                            ObjUsuario.contrasena = usuario.contrasena;
                            ObjUsuario.idTarjeta = usuario.idTarjeta;
                            ObjUsuario.Correo = usuario.Correo;
                            ObjUsuario.idRol = usuario.idRol;

                            if (Activo == "false")
                            {
                                ObjUsuario.activo = false;
                            }
                            else
                            {
                                ObjUsuario.activo = true;
                            }

                            if (resetContrasena == "false")
                            {
                                ObjUsuario.resetContrasena = false;
                            }
                            else
                            {
                                ObjUsuario.resetContrasena = true;
                            }

                            if (EnvioCorreo == "false")
                            {
                                ObjUsuario.EnvioCorreo = false;
                            }
                            else
                            {
                                ObjUsuario.EnvioCorreo = true;
                            }

                            entity.SaveChanges();

                            var Info = new InfoMensaje
                            {
                                Tipo = "Success",
                                Mensaje = "Usuario Editada exitosamente"

                            };
                            ViewData["Alert"] = Info;
                            ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                            ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                            return PartialView("ListaUsuario");
                        }
                        else
                        {
                            var Info1 = new InfoMensaje
                            {
                                Tipo = "js",
                                Mensaje = "Contrasena debe ser 4 Digitos"

                            };
                            ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                            ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                            return Json(Info1, JsonRequestBehavior.AllowGet);
                        }
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Usuario Tiene campos Invalidos"

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                        return PartialView("ListaUsuario");
                    }
                }
                catch (Exception ex)
                {
                    using (var entity1 = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Error",
                            Mensaje = ex.Message

                        };
                        ViewData["Alert"] = Info;
                        ViewData["ListaUsuario"] = entity.Usuario.Include("Roles").ToList();
                        ViewData["TipoRolUsuario"] = entity.Roles.ToList();

                        return PartialView("ListaUsuario");
                    }
                }
            }
        }
        #endregion

        #region Inventario
        public ActionResult Inventario()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TransferirVista"] = "Si";
                var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                var queryProducto = entity.Producto.ToList().Select(x => new { x.idProducto, x.nombre });
                var querySuplidor = entity.Suplidor.ToList().Select(x => new { x.idSuplidor, x.nombre });

                ViewData["ListaInventario"] = queryInventario.ToList();
                ViewData["Producto"] = queryProducto.ToList();
                ViewData["Suplidor"] = querySuplidor.ToList();

            }

            return PartialView();
        }

        public ActionResult CrearEntrdaInventario(Inventario inventario)
        {
            try
            {
                using (var entity = new barbdEntities())
                {
                    if (ModelState.IsValid)
                    {
                        ViewData["TransferirVista"] = "Si";
                        inventario.fecha = DateTime.Now;
                        entity.Inventario.Add(inventario);
                        entity.SaveChanges();

                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Producto Agregada en almacen"

                        };
                        ViewData["Alert"] = Info;
                    }
                    else
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Producto tiene campo Invalido"

                        };
                        ViewData["Alert"] = Info;
                    }

                    var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");

                    ViewData["ListaInventario"] = queryInventario.ToList();
                }

                return PartialView("ListaInventario");
            }
            catch (Exception ex)
            {
                var Info = new InfoMensaje
                {
                    Tipo = "Warning",
                    Mensaje = ex.Message

                };
                ViewData["Alert"] = Info;
                using (var entity = new barbdEntities())
                {
                    var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");

                    ViewData["ListaInventario"] = queryInventario.ToList();
                }

                return PartialView("ListaInventario");
            }
        }

        public ActionResult InventarioAlmacen()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TransferirVista"] = "Si";
                var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");

                ViewData["ListaInventario"] = queryInventario.ToList();
            }

            return PartialView("ListaInventario", ViewData["ListaInventario"]);
        }

        public ActionResult InventarioBar()
        {
            using (var entity = new barbdEntities())
            {
                ViewData["TransferirVista"] = null;
                var queryInventarioBAR = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleBAR");

                ViewData["ListaInventario"] = queryInventarioBAR.ToList();
            }

            return PartialView("ListaInventario", ViewData["ListaInventario"]);
        }

        [HttpPost]
        public ActionResult BuscarTransferirInventario(Inventario inventario)
        {
            using (var entity = new barbdEntities())
            {
                var ObjInventarioProducto = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                var QueryProducto = ObjInventarioProducto.Single(x => x.IdProducto == inventario.idProducto);
                var Ca = new Inventario
                {
                    idProducto = QueryProducto.IdProducto,
                    cantidad = QueryProducto.Cantidad
                };

                return Json(Ca, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult TransferirProductoABar(InventarioBar inventarioBar)
        {
            try
            {
                if (inventarioBar.cantidad > 0)
                {
                    using (var entity = new barbdEntities())
                    {
                        ViewData["TransferirVista"] = "Si";
                        inventarioBar.fecha = DateTime.Now;
                        entity.InventarioBar.Add(inventarioBar);
                        entity.SaveChanges();
                        var Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Cantidad de producto transferito exitosamente"
                        };

                        ViewData["Alert"] = Info;
                        var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                        ViewData["ListaInventario"] = queryInventario.ToList();

                        return PartialView("ListaInventario", ViewData["ListaInventario"]);
                    }
                }
                else
                {
                    ViewData["TransferirVista"] = "Si";
                    using (var entity = new barbdEntities())
                    {
                        var Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "Transferencia de producto tiene campos invalidos"
                        };

                        ViewData["Alert"] = Info;
                        var queryInventario = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                        ViewData["ListaInventario"] = queryInventario.ToList();

                        return PartialView("ListaInventario", ViewData["ListaInventario"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["TransferirVista"] = "Si";

                using (var entity = new barbdEntities())
                {
                    var Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message
                    };
                    var queryInventarioBAR = entity.Database.SqlQuery<Models.Inventario>("exec sp_inventarioDisponibleInventario");
                    ViewData["ListaInventario"] = queryInventarioBAR.ToList();

                    return PartialView("ListaInventario", ViewData["ListaInventario"]);
                }
            }
        }
        #endregion

        #region Cobrar
        public ActionResult Cobrar()
        {
            using (barbdEntities entity = new barbdEntities())
            {
                ViewData["AllCreditos"] = entity.Creditos.Include("Pagos").AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();
            }

            return PartialView();
        }

        [HttpPost]
        public ActionResult PagarCredito(int id, decimal monto)
        {
            try
            {
                using (barbdEntities entity = new barbdEntities())
                {
                    InfoMensaje Info;
                    Creditos credito = entity.Creditos.Find(id);

                    if (monto <= credito.MontoRestante)
                    {
                        Pagos pago = new Pagos()
                        {
                            idCredito = id,
                            Monto = monto,
                            Fecha = DateTime.Now
                        };

                        entity.Pagos.Add(pago);
                        credito.MontoRestante -= monto;

                        entity.SaveChanges();

                        Info = new InfoMensaje
                        {
                            Tipo = "Success",
                            Mensaje = "Pago realizado exitosamente"

                        };
                    }
                    else
                    {
                        Info = new InfoMensaje
                        {
                            Tipo = "Warning",
                            Mensaje = "El monto a pagar debe ser menor que el monto en deuda"

                        };
                    }

                    ViewData["Alert"] = Info;
                    ViewData["AllCreditos"] = entity.Creditos.AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                    ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                    ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();

                    return PartialView("ListaCreditos");
                }
            }
            catch (Exception ex)
            {
                using (barbdEntities entity = new barbdEntities())
                {
                    InfoMensaje Info = new InfoMensaje
                    {
                        Tipo = "Error",
                        Mensaje = ex.Message

                    };

                    ViewData["Alert"] = Info;
                    ViewData["AllCreditos"] = entity.Creditos.AsEnumerable().Select(c => UntrackedCredito(c)).ToArray();
                    ViewData["ListaCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante > 0).ToArray();
                    ViewData["ListaHistoricoCreditos"] = entity.Creditos.Include("Usuario").Include("Pagos").Where(c => c.MontoRestante == 0).ToArray();

                    return PartialView("ListaCreditos");
                }
            }
        }

        private Creditos UntrackedCredito(Creditos credito)
        {
            List<Pagos> pagos = new List<Pagos>();

            foreach(Pagos pago in credito.Pagos)
            {
                pagos.Add(new Pagos()
                {
                    id = pago.id,
                    Monto = pago.Monto,
                    Fecha = pago.Fecha
                });
            }

            return new Creditos()
            {
                id = credito.id,
                idUsuario = credito.idUsuario,
                MontoRestante = credito.MontoRestante,
                numFactura = credito.numFactura,
                Pagos = pagos.ToArray()
            };
        }
        #endregion
    }
}