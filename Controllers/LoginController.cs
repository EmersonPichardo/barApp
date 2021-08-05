using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace barApp.Controllers
{
    public class LoginController : Controller
    {
        //INCIO LOGIN 
        public ActionResult Index()
        {
            Session["Usuario"] = null;
            return View();
        }

        //INCIO LOGIN AUTENTIFICARSE
        [HttpPost]
        public ActionResult Index(Usuario usuario)
        {
            //Login


            using (var entity = new barbdEntities())
            {

                if (usuario.contrasena != null)
                {



                    var ValidarUsuario = entity.Usuario.Include("Roles").SingleOrDefault(x => x.contrasena == usuario.contrasena || x.idTarjeta == usuario.contrasena);

                    if (ValidarUsuario != null)
                    {
                        if (ValidarUsuario.activo == true)
                        {

                            if (ValidarUsuario.resetContrasena == false)
                            {

                                var varlidarAbrirSistema = entity.Cuadre.Where(x => x.cerrado == false).Count();

                                if (varlidarAbrirSistema == 0)
                                {
                                    if (ValidarUsuario.idRol == 1 || ValidarUsuario.idRol == 4)
                                    {
                                        using (var context = new barbdEntities())
                                        {
                                            var objCuadre = new Cuadre
                                            {
                                                fecha = DateTime.Now,
                                                cerrado = false,
                                            };

                                            context.Cuadre.Add(objCuadre);
                                            context.SaveChanges();
                                        }

                                    }
                                    else
                                    {
                                        var objinfoSistema = new InfoContrasena
                                        {
                                            Mensaje = "Favor comunicarse con el Administrador para abrir el sistema",
                                            // Usuario = ValidarUsuario.nombre
                                        };
                                        return Json(objinfoSistema, JsonRequestBehavior.AllowGet);
                                    }
                                }


                                if (ValidarUsuario.idRol == 2)
                                {
                                    var objinfoContrasena = new InfoContrasena
                                    {
                                        Mensaje = "AutentificadoVendedor",
                                        // Usuario = ValidarUsuario.nombre
                                    };
                                    Session["Usuario"] = ValidarUsuario.nombre.ToUpper();
                                    Session["Rol"] = ValidarUsuario.Roles.idRol;
                                    Session["IdUsuario"] = ValidarUsuario.idUsuario;
                                    return Json(objinfoContrasena, JsonRequestBehavior.AllowGet);
                                }
                                else
                                {
                                    var objinfoContrasena = new InfoContrasena
                                    {
                                        Mensaje = ValidarUsuario.Roles.idRol == 5 ? "Usuario no tiene rol para entrar al sistema" : "Autentificado",
                                        Usuario = ValidarUsuario.Roles.idRol == 5 ? "Usuario no tiene rol para entrar al sistema" : ValidarUsuario.nombre
                                    };
                                    Session["Usuario"] = ValidarUsuario.nombre.ToUpper();
                                    Session["Rol"] = ValidarUsuario.Roles.idRol;
                                    return Json(objinfoContrasena, JsonRequestBehavior.AllowGet);
                                }




                            }
                            else
                            {
                                var objinfoContrasena = new InfoContrasena
                                {
                                    Mensaje = "ResetContrasena",
                                    Usuario = ValidarUsuario.nombre
                                };

                                return Json(objinfoContrasena, JsonRequestBehavior.AllowGet);
                            }


                        }
                        else
                        {
                            var objinfoContrasena = new InfoContrasena
                            {
                                Mensaje = "Usuario Deshabilitado",
                                // Usuario = ValidarUsuario.nombre
                            };
                            return Json(objinfoContrasena, JsonRequestBehavior.AllowGet);
                        }

                    }
                    else
                    {
                        var objinfoContrasena = new InfoContrasena
                        {
                            Mensaje = "Contrasena Incorrecta",
                            // Usuario = ValidarUsuario.nombre
                        };

                        return Json(objinfoContrasena, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    var objinfoContrasena = new InfoContrasena
                    {
                        Mensaje = "Ingrese Contrasena",
                        // Usuario = ValidarUsuario.nombre
                    };

                    return Json(objinfoContrasena, JsonRequestBehavior.AllowGet);
                }


            }

        }

        //Cambiar CONTRASENA

        public ActionResult CambiarContrasena(Usuario usuario)
        {

            using (var context = new barbdEntities())
            {
                var ObjUsuarioV = context.Usuario.Where(x => x.contrasena == usuario.contrasena).Count();
                if (ObjUsuarioV== 0)
                {
                    var ObjUsuario = context.Usuario.SingleOrDefault(x=> x.nombre == usuario.nombre);
                    ObjUsuario.resetContrasena = false;
                    ObjUsuario.contrasena = usuario.contrasena;
                    context.SaveChanges();

                    return Json($"Contrasena Cambiada Exitosamente {usuario.nombre} ", JsonRequestBehavior.AllowGet);

                }
                else
                {
                    return Json($"NO", JsonRequestBehavior.AllowGet);
                }


            }

        }
        public ActionResult Salir()
        {
            System.Web.HttpContext.Current.Session["Usuario"] = null;
            System.Web.HttpContext.Current.Session["Rol"] = null;           
            Session["idVenta"] = null;
            return RedirectToAction("Index");

        }


    }

    class InfoContrasena
    {
        public string Mensaje { get; set;}
        public string Usuario { get; set; }
    }
}