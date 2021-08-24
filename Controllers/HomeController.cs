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
                var cliente = new List<Cliente>();

                ViewBag.VendedorOrigen = entity.Usuario.Where(x => x.idRol == 2).Select(x => new { x.idUsuario, x.nombre }).ToList();
                ViewData["ModoPago"] = entity.ModoPago.ToList();
                ViewData["ClienteOrigen"] = cliente.ToList();

                int? cuadreActual = entity.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false))?.idCuadre;
                decimal[] facturas = entity.Factura.AsEnumerable().Where(f => f.idCuadre == cuadreActual.GetValueOrDefault(0)).Select(f => f.numFactura).ToArray();
                decimal[] detalles = entity.DetalleVenta.Where(dv => facturas.Contains(dv.numFactura)).Select(dv => dv.idVenta).Distinct().ToArray();
                ViewBag.Usuarios = entity.Usuario.AsEnumerable().Select(u => UntrackedUsuario(u)).ToArray();
                ViewBag.Detalles = entity.DetalleVenta.Where(dv => facturas.Contains(dv.numFactura)).AsEnumerable().Select(dv => UntrackedDetalle(dv)).ToArray();
                ViewBag.Cuentas = entity.Venta.Where(v => detalles.Contains(v.idVenta)).AsEnumerable().Select(v => UntrackedVenta(v)).ToArray();
                ViewBag.CuentasJson = JsonConvert.SerializeObject(ViewBag.Cuentas);
                ViewBag.DetallesJson = JsonConvert.SerializeObject(ViewBag.Detalles);
                ViewBag.UsuariosJson = JsonConvert.SerializeObject(ViewBag.Usuarios);
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
                numFactura = detalleVenta.numFactura,
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

        //transferir cuentas
        public ActionResult TransferirCuenta(Usuario usuario)
        {
            using (var entity = new barbdEntities())
            {
                var ListCliente = new List<Cliente>();
                var Clientes = entity.Venta.Include("Cliente").Where(x => x.idUsuario == usuario.idUsuario && x.ordenCerrada == null).Select(x => new { x.Cliente.idCliente, x.Cliente.nombre, x.idVenta }).ToList();

                foreach (var item in Clientes)
                {
                    var ObjCliente = new Cliente()
                    {
                        idCliente = item.idVenta,
                        nombre = "Orden No." + item.idVenta + "\t\t" + item.nombre
                    };

                    ListCliente.Add(ObjCliente);
                }

                return Json(ListCliente, JsonRequestBehavior.AllowGet);
            }
        }

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
                ViewData["ListaModoPago"] = entity.ModoPago.ToList();
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

                ViewData["ListaGastos"] = entity.Gastos.Where(x => x.idCuadre == null).ToList();
            }

            return PartialView();
        }

        public ActionResult CrearGastos(Gastos ObjGastos)
        {
            using (var entity = new barbdEntities())
            {
                try
                {
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
    }
}