using barApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Printing;
using System.Web;
using System.Web.Mvc;

namespace barApp.Controllers
{
    public class OrdenController : Controller
    {
        // GET: Orden
        public ActionResult Index()
        {


            using (var entity = new barbdEntities())
            {
                Session["idVenta"] = null;
                int idrol = Convert.ToInt32(Session["IdUsuario"]);
                ViewBag.ListadoClienteOrdenar = entity.Venta.Include("Cliente").Where(x => x.idUsuario == idrol && x.ordenCerrada == null).ToList();
                ViewData["Ordenar"] = null;
                ViewBag.Categoria = entity.Categoria.ToList();
                ViewBag.Producto = entity.Producto.Where(x => x.activo == true).ToList();
                ViewData["Mesas"] = entity.Mesa.ToList();
                //ViewBag.Especial = orden_.ListaEspeciales();
            }


            return View();
        }

        [HttpPost]
        public ActionResult IndexDetalles(string Id)
        {
            // int idrol = Convert.ToInt32(System.Web.HttpContext.Current.Session["idVendedor"]);
            using (var entity = new barbdEntities())
            {
                //int idrol = Convert.ToInt32(Session["IdUsuario"]);
                //ViewData["ListadoClienteOrdenar"] = orden_.ListaClienteOrdenar(idrol);
                //double Total = orden_.totalVenta(Convert.ToInt32(Session["idVenta"]));
                //ViewBag.TotalPagar = Total;
                int idVenta = Convert.ToInt32(Id);
                var ListaDetalleVenta = entity.DetalleVenta.Include("Venta").Include("Producto").Where(x => x.idVenta == idVenta).ToList();   //orden_.ListaOrdenar(Convert.ToInt32(Session["idVenta"]));
                                                                                                                                              //Session["idVenta"] = btnCliente;

                return PartialView("ListadoDeOrdenes", ListaDetalleVenta);



            }



        }


        public ActionResult Total(int Id)
        {
            using (var entity = new barbdEntities())
            {

                var ObjTotal = entity.Venta.Find(Id);

                return Json(ObjTotal.total, JsonRequestBehavior.AllowGet);

            }
        }
        [HttpPost]
        public ActionResult AgregarProductoCarrito(DetalleVenta detalleVenta)
        {
            using (var Context = new barbdEntities())
            {
                detalleVenta.numFactura = Context.DetalleVenta.FirstOrDefault(dv => dv.idVenta == detalleVenta.idVenta)?.numFactura ?? 0;

                if (detalleVenta.numFactura == 0)
                {
                    Factura factura = new Factura()
                    {
                        fecha = DateTime.Now,
                        IVA = 18,
                        total = 0,
                        numPago = 1
                    };

                    Context.Factura.Add(factura);
                    Context.SaveChanges();

                    detalleVenta.numFactura = factura.numFactura;
                }

                Producto producto = Context.Producto.Find(detalleVenta.idProducto);

                detalleVenta.subTotal = (float)(detalleVenta.cantidad * producto.precioVenta);
                detalleVenta.despachada = false;
                detalleVenta.precioVenta = producto.precioVenta;
                detalleVenta.precioEntrada = producto.precioAlmacen;

                Context.DetalleVenta.Add(detalleVenta);
                Context.SaveChanges();

                Venta venta = Context.Venta.Find(detalleVenta.idVenta);
                venta.total += detalleVenta.subTotal;
                Context.Entry(venta).State = System.Data.Entity.EntityState.Modified;
                Context.SaveChanges();

                return Json(new
                {
                    producto = new Producto()
                    {
                        idProducto = producto.idProducto,
                        nombre = producto.nombre,
                        precioVenta = producto.precioVenta
                    },
                    idDetalle = detalleVenta.idDetalle
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public int EliminarProductoCarrito(int idDetalle)
        {
            using (barbdEntities context = new barbdEntities())
            {
                DetalleVenta detalle = context.DetalleVenta.Find(idDetalle);
                context.Entry(detalle).State = System.Data.Entity.EntityState.Deleted;

                Venta venta = context.Venta.Find(detalle.idVenta);
                venta.total -= detalle.subTotal;

                return context.SaveChanges();
            }
        }

        [HttpPost]
        public ActionResult obtenerMesas()
        {
            using (var context = new barbdEntities())
            {
                List<Mesa> ListMesa = new List<Mesa>();

                var M = context.Mesa.ToList();

                foreach (var item in M)
                {

                    var Validar = context.Cliente.Count(x => x.idMesa == item.idMesa);


                    if (Validar == 0)
                    {
                        Mesa ObjMesa = new Mesa()
                        {
                            idMesa = item.idMesa,
                            descripcion = item.descripcion

                        };

                        ListMesa.Add(ObjMesa);



                    }


                }

                return PartialView("ListaMesas", ListMesa);

            }



        }

        [HttpPost]
        public ActionResult NuevaOrden(Cliente cliente)
        {

            using (var Context = new barbdEntities())
            {
                var ObjCliente = Context.Cliente.Add(cliente);
                Context.SaveChanges();

                var ObjVenta = new Venta
                {
                    total = 0,
                    idCliente = ObjCliente.idCliente,
                    fecha = DateTime.Now,
                    IVA = Context.Impuesto.Single().Itbis.Value,
                    idUsuario = Convert.ToInt32(Session["IdUsuario"])
                };

                Context.Venta.Add(ObjVenta);
                Context.SaveChanges();

                ViewData["ListadoClienteOrdenar"] = Context.Venta.Include("Cliente").Where(x => x.idUsuario == ObjVenta.idUsuario && x.ordenCerrada == null).ToList();
                return PartialView("ListadoClientes", ViewData["ListadoClienteOrdenar"]);
            }
        }

        [HttpPost]
        public int Despachar(string waiter, int[] ids)
        {
            List<string[]> data = new List<string[]>();

            using (barbdEntities context = new barbdEntities())
            {
                for (int index = 0; index < (ids?.Length ?? 0); index++)
                {
                    DetalleVenta detalleVenta = context.DetalleVenta.Find(ids[index]);
                    detalleVenta.despachada = true;
                    context.Entry(detalleVenta).State = System.Data.Entity.EntityState.Modified;

                    data.Add(new string[2] { detalleVenta.Producto.nombre.ToUpper(), detalleVenta.cantidad.ToString() });
                }

                context.SaveChanges();
            }

            Printer printer = new Printer();

            printer.AddTitle("Despachar");

            Dictionary<string, string> list = new Dictionary<string, string>();
            list.Add("Mesero/a", waiter);
            list.Add("Fecha/hora", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));
            printer.AddDescriptionList(list);

            printer.AddTable(new string[2] { "Descripción", "Cantidad" }, data.ToArray());

            printer.Print();

            return 1;
        }
    }
}