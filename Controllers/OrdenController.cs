using barApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            }

            return View();
        }

        [HttpPost]
        public ActionResult IndexDetalles(string Id)
        {
            using (var entity = new barbdEntities())
            {
                int idVenta = Convert.ToInt32(Id);
                var ListaDetalleVenta = entity.DetalleVenta.Include("Venta").Include("Producto").Where(x => x.idVenta == idVenta).ToList();

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
                        numPago = 1,
                        idCuadre = Context.Cuadre.AsEnumerable().SingleOrDefault(c => !c.cerrado.GetValueOrDefault(false)).idCuadre
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
            Dictionary<string, string> list = new Dictionary<string, string>();
            list.Add("Vendedor/a", waiter);
            list.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
            list.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

            printer.AddTitle("Despachar");
            printer.AddSpace(3);
            printer.AddDescriptionList(list);
            printer.AddSpace(2);
            printer.AddTable(new string[2] { "Descripción", "Cantidad" }, data.ToArray());

            printer.Print();

            return 1;
        }

        [HttpPost]
        public int Prefacturar(int id)
        {
            int result = 0;
            string cliente;
            string vendedor;
            decimal subtotal;
            decimal itbis;
            string[][] data;

            using (barbdEntities context = new barbdEntities())
            {
                Venta venta = context.Venta.Find(id);
                venta.ordenCerrada = true;

                venta.Cliente.idMesa = null;

                result = context.SaveChanges();
                cliente = context.Cliente.Find(venta.idCliente).nombre;
                vendedor = context.Usuario.Find(venta.idUsuario).nombre;
                subtotal = (decimal)context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.subTotal);
                itbis = context.DetalleVenta.Where(vd => vd.idVenta == id).Sum(vd => vd.precioVenta).GetValueOrDefault(0) * 0.18m;
                data =
                    context.DetalleVenta
                    .Where(vd => vd.idVenta == id)
                    .AsEnumerable()
                    .GroupBy(
                        vd => vd.Producto,
                        vd => new {
                            vd.cantidad,
                            vd.precioVenta,
                            vd.subTotal
                        },
                        (producto, grupo) => new {
                            producto.nombre,
                            producto.idProducto,
                            cantidad = grupo.Sum(g => g.cantidad),
                            precioVenta = grupo.Sum(g => g.precioVenta.GetValueOrDefault(0)),
                            subTotal = grupo.Sum(g => g.subTotal)
                        }
                    )
                    .Select(vd => new string[5] {
                        vd.nombre.ToUpper(),
                        vd.idProducto,
                        Math.Round((decimal)vd.cantidad, 2).ToString("#,0"),
                        Math.Round(vd.precioVenta, 2).ToString("$#,0.00"),
                        Math.Round(vd.subTotal, 2).ToString("$#,0.00")
                    })
                    .ToArray();
            }

            Printer printer = new Printer();

            IDictionary<string, string> list1 = new Dictionary<string, string>();
            list1.Add("Cliente", cliente.ToUpper());
            list1.Add("Orden", id.ToString());
            list1.Add("Vendedor/a", vendedor.ToUpper());
            list1.Add(printer.EmptyListElement);
            list1.Add("Fecha", DateTime.Now.ToString("dd MMM yyyy"));
            list1.Add("Hora", DateTime.Now.ToString("hh:mm:ss tt"));

            Dictionary<string, string> tableDetails = new Dictionary<string, string>();
            tableDetails.Add("Subtotal", (subtotal - itbis).ToString("$#,0.00"));
            tableDetails.Add("ITBIS", itbis.ToString("$#,0.00"));
            Dictionary<string, string> tableTotal = new Dictionary<string, string>();
            tableTotal.Add("TOTAL", subtotal.ToString("$#,0.00"));

            printer.AddTitle("Prefactura");
            printer.AddSpace(3);
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

            printer.Print();

            return result;
        }
    }
}