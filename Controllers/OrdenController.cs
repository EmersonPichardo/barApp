using System;
using System.Collections.Generic;
using System.Linq;
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
        public ActionResult AgregarProductoCarrito(Producto producto)
        {
            Producto producto1 = new Producto();

            using (var Context = new barbdEntities())
            {

                var Producto = Context.Producto.Find(producto.idProducto);

                producto1.idProducto = Producto.idProducto;
                producto1.nombre = Producto.nombre;
                producto1.precioVenta = Producto.precioVenta;

                return Json(producto1, JsonRequestBehavior.AllowGet);
            }
                       

        }

     
        [HttpPost]
        public ActionResult obtenerMesas()
        {
            string a;
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


    }
}