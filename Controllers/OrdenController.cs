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
        public int Despachar(int[] ids)
        {
            using (barbdEntities context = new barbdEntities())
            {
                List<string[]> data = new List<string[]>();

                for (int index = 0; index < (ids?.Length ?? 0); index++)
                {
                    DetalleVenta detalleVenta = context.DetalleVenta.Find(ids[index]);
                    detalleVenta.despachada = true;
                    context.Entry(detalleVenta).State = System.Data.Entity.EntityState.Modified;

                    data.Add(new string[2] { detalleVenta.Producto.nombre.ToUpper(), detalleVenta.cantidad.ToString() });
                }

                context.SaveChanges();

                //Imprimir
                PrintDocument document = new PrintDocument();

                document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
                {
                    //Configuration
                    Font titleFont = new Font("Calibri", 22, FontStyle.Bold);
                    Font bodyTitleFont = new Font("Calibri", 14, FontStyle.Bold);
                    Font bodyFont = new Font("Calibri", 12);
                    Brush brush = new SolidBrush(Color.Black);
                    float width = document.DefaultPageSettings.PrintableArea.Width;
                    float height = document.DefaultPageSettings.PrintableArea.Height;
                    int padding = 30;
                    int gridColumns = 2;
                    float xGridSize = (width - (padding * 2)) / gridColumns;
                    int ySeparation = 10;
                    float yCurrent = padding;

                    //Header
                    string header = "Despachar";
                    _event.Graphics.DrawString(header, titleFont, brush, width / 2, yCurrent, new StringFormat() { Alignment = StringAlignment.Center });
                    yCurrent += _event.Graphics.MeasureString(header, titleFont).Height + (ySeparation * 4);

                    //Body
                    _event.Graphics.DrawLine(new Pen(brush), padding / 2, yCurrent, width - (padding / 2), yCurrent);

                    string[] tableColumns = new string[2] { "Descripción", "Cantidad" };
                    for (int index = 0; index < tableColumns.Length; index++)
                    {
                        _event.Graphics.DrawString(tableColumns[index].ToUpper(), bodyTitleFont, brush, (padding + (xGridSize * index)), yCurrent);
                    }

                    yCurrent += _event.Graphics.MeasureString(tableColumns[0], bodyFont).Height;
                    _event.Graphics.DrawLine(new Pen(brush), padding / 2, yCurrent, width - (padding / 2), yCurrent);
                    yCurrent += ySeparation;

                    for (int row = 0; row < data.Count; row++)
                    {
                        for (int valueIndex = 0; valueIndex < data[row].Length; valueIndex++)
                        {
                            _event.Graphics.DrawString(data[row][valueIndex], bodyFont, brush, (padding + (xGridSize * valueIndex)), yCurrent);
                        }

                        yCurrent += row < data.Count - 1 ? _event.Graphics.MeasureString(tableColumns[0], bodyFont).Height + ySeparation : 0;
                    }

                    yCurrent += _event.Graphics.MeasureString(tableColumns[0], bodyFont).Height + (ySeparation * 4);

                    //Footer
                    _event.Graphics.DrawString(DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"), bodyFont, brush, width / 2, yCurrent, new StringFormat() { Alignment = StringAlignment.Center });
                };

                document.Print();

                return 1;
            }
        }
    }
}