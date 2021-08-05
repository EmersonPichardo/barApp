/* teclado-auxiliar-integrado.js
 * (c) 2013, Andrés de la Paz
 * www.wextensible.com
 */


//::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
//  Funciones de uso común del original general.js
//::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

//Funciones de general.js que se usan en el script
var wxG = {
 
    nodoPadre: function(nodo) {
        try {
            if (nodo == null) {
                return null;
            } else if (nodo.parentElement) {//IE
                return nodo.parentElement;
            } else {//FF
                return nodo.parentNode;
            }
        } catch(e){
            return null;
        }
    },   
    
    
    arrayClassName: function(nombreClase, elemento){
        try {
            function recorreDom(el, arr){
                if (el.className == nombreClase) arr.push(el);
                var hijos = el.childNodes;
                for (var i=0; i<hijos.length; i++){
                    recorreDom(hijos[i], arr);
                }
                return arr;
            }
            var nodo = document.body;
            if (elemento != null) nodo = elemento;
            if (document.getElementsByClassName){ //FF
                return nodo.getElementsByClassName(nombreClase);
            } else { //IE
                var arr = new Array;
                return recorreDom(nodo, arr);
            }
        } catch (e) {
            return null;
        }        
    },

    /* AGREGAR ADDEVENTLISTENER (para IE)
     * Para agregar addEventListener a IE8 y navegadores que no lo 
     * soporten. Hay dos opciones para agregar esto que pueden usarse
     * al mismo tiempo.
     * El primer argumento es un array de tags como ["div", "span",...]
     * que lo agrega al PROTOTIPO DE todos esos tag del documento.
     * Mientras que el segundo es un array de elementos de tal forma que se 
     * agrega solo a ese elemento y, opcionalmente a todos sus hijos. Es de la forma
     * [[ref1, "outer"], [ref2, "inner"], [ref3, "all"]...]
     * - refN es una referencia a un elemento HTML
     * - con "outer" se agrega al elemento
     * - con "inner" se agrega a los hijos del elemento
     * - con "all" se agrega al elemento y sus hijos
     * Podemos pasar agregarEventListener(tages, elementos) para ambas opciones o
     * agregarEventListener(tages) para usar sólo la primera opción o
     * agregarEventListener(null, elementos) si sólo usamos la segunda.
     */

    conEventListener: false,

    agregarEventListener: function(tages, elementos){
        try {
            //Si el navegador soporta addEventListener no entrará aquí
            if (wxG.conEventListener || !document.body.addEventListener){
                //Para IE8 que no soporta addEventListener, la primera vez que se adjudique estará
                //conEventListener falso. Pero si adjudicamos al body primero y posteriormente 
                //adjudicamos a otro elemento, no basta con consultar document.body.addEventListener
                //pues será cierto y no entrará aquí. Por eso con la primera adjudicación anotamos
                //que este navegador necesita agregar addEventListener.
                wxG.conEventListener = true;
                //Agregamos al prototipo del tag
                if (tages) {
                    for (var i=0; i<tages.length; i++){
                        var tagesi = tages[i].toLowerCase();
                        //Elementos con nombre extendido
                        switch (tagesi) {
                            case "p":
                                tagesi = "Paragraph";
                                break;
                            case "textarea":
                                tagesi = "TextArea";
                                break;
                            default:
                                tagesi = tagesi[0].toUpperCase() + tagesi.substr(1, tagesi.length);
                        }
                        var objTag = "HTML" + tagesi + "Element";
                        if (window[objTag]) {
                            window[objTag].prototype.addEventListener = function(evento, accion, arg){
                                this["on" + evento] = accion;   
                            };
                        }
                    }
                }
                //Agregamos al elemento.
                if (elementos) {
                    for (var i=0, max=elementos.length; i<max; i++){
                        var arr = elementos[i];
                        if (arr.length==2) {
                            //El elemento no debe ser nulo y no debe haber sido agregado antes
                            if (arr[0] && !arr[0].addEventListener) {
                                if ((arr[1]=="outer")||(arr[1]=="all")){
                                    arr[0].addEventListener = function(evento, accion, arg){
                                        this["on" + evento] = accion;   
                                    };
                                }
                                if ((arr[1]=="inner")||(arr[1]=="all")) {
                                    var todos = arr[0].getElementsByTagName("*");
                                    for (var j=0, maxj=todos.length; j<maxj; j++) {
                                        todos[j].addEventListener = function(evento, accion, arg){
                                            this["on" + evento] = accion;   
                                        };
                                    }                            
                                }
                            }
                        }
                    } 
                }
            }
        } catch(e) {}
    },
    
    /* ENCIENDE UN EVENTO.
     * Con Explorer se enciende un evento con fireEvent de tal forma que el 
     * objeto evento viene con 4 propiedades con los siguientes valores 
     * predeterminados:
     * - cancelBubble = false (impide que el evento se propage por el DOM)
     * - returnValue = true (en los casos en que el evento retorne algún valor)
     * - srcElement = elemento donde se encendió el evento
     * - type = el nombre del evento (blur, click, etc)
     * El cancelBubble viene false, pero lo ponemos a true para impedir posibles
     * burbujeos. Mientras que con Mozilla el encendido del evento se define así:
     * event.initEvent(type, bubbles, cancelable). El argumento bubbles es true por
     * defecto, lo que hará que el evento "burbujee" por todo el DOM (para aquellos
     * eventos que burbujean como, por ejemplo "onmousedown"), es decir, sigue 
     * aplicándose a los nodos hijos, por ejemplo. Cómo no queremos este efecto, 
     * lo pasamos false. El argumento cancelable si lo pasamos true por si se 
     * necesita cancelarlo.
     * Argumento evento como "onchange", "onclick", etc.
     */
    enciendeEvento: function(elemento, evento, touch) {
        try {
            if ((elemento != null)&&(evento != "")){

                if (elemento.fireEvent) {//para Explorer
                    //Impide burbujeo (esto da error con un textarea???)
                    try{window.event.cancelBubble = true;}catch(e){};
                    elemento.fireEvent(evento);
                } else if (document.createEvent) {//para Firefox
                    var evt = document.createEvent("HTMLEvents");
                    evento = evento.substring(2, evento.length);
                    //Se cancela el burbujeo
                    evt.initEvent(evento, false, true);
                    elemento.dispatchEvent(evt);
                } else {
                    alert("Error, no se pudo encender un evento en 'general.js'");
                }
                
            }
        } catch (e) {
            alert("Error en enciendeEvento de 'general.js'");
        }
    },    
    

    /* Obtiene el estilo actual de una propiedad de un elemento consultando
     * directamente al objeto CSSStyleDeclaration. La propiedad debe venir con
     * guiones como "font-size". Para IE se le quitan los guiones con
     * cambiaGuiones().
     */
    estiloActual: function(elemento, propiedad){
        try {
            if (window.getComputedStyle){//FF, Opera (con guiones)
                //también puede ser window.getComputedStyle...            
                return document.defaultView.getComputedStyle(elemento,null).getPropertyValue(propiedad); 
            } else if (elemento.currentStyle){//IE (sin guiones)
                var propSinGuiones = wxG.cambiaGuiones(propiedad);
                if (elemento.currentStyle.getAttribute) {//IE
                    return elemento.currentStyle.getAttribute(propSinGuiones);
                } else {//
                    return elemento.currentStyle[propSinGuiones];
                }           
            } else {
                return "";
            }
        } catch(e) {
            return "";
        }
        
    }
  
};


//::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
//Funciones del código original ubicado en general.js
//::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

/* COMPLEMENTO TECLADO AUXILIAR INPUT TYPE NUMBER ------------------------------------------------------
 * Algunos navegadores como Firefox o en los móviles no aceptan el tipo number o bien no
 * incluyen los botones para subir y bajar el valor. Este complemento agrega una especie
 * de teclado que se abrirá cuando el input type number obtenga el foco  y que permitirá
 * subir y bajar el valor numérico así como insertar punto o coma para el valor decimal
 * o un guión para el negativo.
 * 
 * Necesita de algo de CSS que está en formatos.css
 * 
 * Este complemento después de minificado ocupará algo menos de 4KB. Es muy pequeño
 * para ponerlo en un archivo separado, por lo que lo agregamos a este general.js.
 */
 
var teC = {

    //Aquí guardamos la referencia al input type number que tiene el foco
    focoNumeric: null,
    
    
    /* Función que carga un teclado decimal con botones para subir y bajar el valor y otros
     * tres botones con ".", "," y "-" para incluir estos caracteres en el input, pues navegadores
     * en móviles de Samsung incluye un teclado númerico cuando encuentran un type number pero
     * ese teclado no lleva sino los números 0-9 sin puntos, comas o guiones para poner números
     * negativos o decimales.
     * Esta función se incluirá en el window.onload y el contenedor puede ser body o cualquier
     * otro que tenga input type number en su interior a los cuales les dotaremos de esta
     * funcionalidad. 
     * Argumentos
     * - arg:  
     *      Determinamos el contenedor donde debemos buscar los input type number:
     *      - Un Object, referencia directa a un elemento HTML
     *      - Si es string primero mira si existe un contenedor con ese string por ID
     *      - Si no es así mira todos los contenedores con ese string por CLASS
     * - elementoPosicionAbrir:
     *      Opcional, nulo o una referencia a un elemento HTML.
     *      Indica que elemento se toma para abrir el teclado según la posición indicada
     *      en el siguiente argumento. Si es nulo el elemento será el input tipo number.
     * - posicionAbrir:
     *      Opcional, un string para indicar en que posición se abrirá el teclado respecto
     *      al elemento del argumento anterior. Puede tomar los valores 
     *      "[left|right]-[top|bottom]" o nulo tomándose "left-bottom" que es el valor inicial.
     *      
     *    
     */
    cargarTecladoAuxiliar: function(arg, elementoPosicionAbrir, posicionAbrir) {
        var cars = ".-";
        var carsTitle = ["punto", "guion"];
        if (!document.getElementById("keyboard-numeric")){
            var html = '<div class="knum-cerrar" title="Cerrar">&times;</div>' +    
            '<div class="knum" onmousedown="teC.variarTypeNumber(event, 1.0)" title="Subir valor">&uArr;</div>' +     
            '<div class="knum" onmousedown="teC.variarTypeNumber(event, -1.0)" title="Bajar valor">&dArr;</div>';
            for (var i=0, maxi=cars.length; i<maxi; i++){
                html += '<div class="knum" onmousedown="teC.symNumeric(event, &quot;' + cars[i] + '&quot;)" ' +
                'title="Agregar ' + carsTitle[i] + '" style="display: none" ' +
                'id="key-numeric-' + carsTitle[i] + '">' + cars[i] + '</div>';
            }
            var diver = document.createElement("div");
            diver.id = "keyboard-numeric";
            diver.innerHTML = html;
            document.body.appendChild(diver);
        }
        var inputes = [];
        if (typeof arg == "string"){
            var contenedor = document.getElementById(arg);
            if (contenedor) {
                inputes = contenedor.getElementsByTagName("input");
            } else {
                var contenedores = wxG.arrayClassName(arg);
                for (var i=0, maxi=contenedores.length; i<maxi; i++){
                    var inps = contenedores[i].getElementsByTagName("input");
                    for (var j=0, maxj=inps.length; j<maxj; j++) {
                        inputes[inputes.length] = inps[j];
                    }
                    
                }
            }
        } else if (typeof arg == "object"){
            inputes = arg.getElementsByTagName("input");
        }
        for (var i=0, maxi=inputes.length; i<maxi; i++) {
            if (inputes[i].hasAttribute("max") || inputes[i].hasAttribute("min") || 
                inputes[i].hasAttribute("step")){
                teC.ajustarAnchoNumber(inputes[i]);
                //agregamos unos data para luego mostrar u ocultar teclas "." y "-"
                if (parseInt(inputes[i].getAttribute("min")) < 0){
                    inputes[i].setAttribute("data-numero-negativo", "");
                }
                if (inputes[i].value.indexOf(".")>-1) {
                    inputes[i].setAttribute("data-numero-real", "");
                }
                wxG.agregarEventListener(null, [[inputes[i], "outer"]]);  
                inputes[i].addEventListener("focus",
                    function(event){
                        var evt = event || window.event;
                        var elemento = evt.target || evt.srcElement;
                        if (elemento.hasAttribute("max") || elemento.hasAttribute("min") || 
                            elemento.hasAttribute("step")){
                            teC.focoNumeric = elemento;
                            var arr = ["max", "min", "step"];
                            for (var i=0, maxi=arr.length; i<maxi; i++){
                                var val=1.0;
                                if (elemento.hasAttribute(arr[i])){
                                    var atval = elemento.getAttribute(arr[i]);
                                    if ((atval != "")&&(!isNaN(atval))) {
                                        val = parseFloat(atval);
                                    }
                                } else if (arr[i]=="min"){
                                    val = -100000000;
                                } else if (arr[i]=="max"){
                                    val = 100000000;
                                } else {
                                    val = 1;
                                }
                                teC.focoNumeric["at" + arr[i]] = val;  
                            }
                            //muestra y oculta botones ".","," y "-" según sea negativo y reales
                            var keyNumericGuion = document.getElementById("key-numeric-guion");
                            var keyNumericPunto = document.getElementById("key-numeric-punto");
                            if (teC.focoNumeric.hasAttribute("data-numero-negativo")){
                                keyNumericGuion.style.display = "block";
                            } else {
                                keyNumericGuion.style.display = "none";
                            }
                            if (teC.focoNumeric.hasAttribute("data-numero-real")){
                                keyNumericPunto.style.display = "block";
                            } else {
                                keyNumericPunto.style.display = "none";
                            } 
                            //Esto es necesario para ajusta la posición del teclado
                            var sumaTop = 0, sumaLeft = 0;
                            padre = elemento;
                            while (padre.tagName.toLowerCase() != "body"){
                                sumaTop -= padre.scrollTop;
                                sumaLeft -= padre.scrollLeft;
                                var ptn = padre.tagName.toLowerCase();
                                var rel = (wxG.estiloActual(padre, "position")=="absolute");
                                if (rel||(ptn=="table")||(ptn=="td")){
                                    var bw = parseInt(wxG.estiloActual(padre, "border-bottom-width"));
                                    if (isNaN(bw)) bw = 0;
                                    sumaTop += padre.offsetTop + bw;
                                    sumaLeft += padre.offsetLeft + bw;
                                }
                                padre = wxG.nodoPadre(padre);
                            }
                            var knum = document.getElementById("keyboard-numeric");
                            if (isNaN(sumaTop)) sumaTop = 0;
                            var elementoAbrir = elemento;
                            if (elementoPosicionAbrir) elementoAbrir = elementoPosicionAbrir;
                            var posicion = "left-bottom";
                            if (posicionAbrir) posicion = posicionAbrir;
                            var posx = 0, posy = 0, arrPos = posicion.split("-");
                            posx = elementoAbrir.offsetLeft + sumaLeft;
                            if (arrPos[0]=="right") posx += elementoAbrir.offsetWidth;
                            posy = elementoAbrir.offsetTop + sumaTop;
                            if (arrPos[1]=="bottom") posy += elementoAbrir.offsetHeight;
                            knum.style.left = posx + "px";
                            knum.style.top = posy + "px";
                            knum.style.display = "block";
                        }      
                    }, false);
    
                inputes[i].addEventListener("blur",
                    function(event){
                        var evt = event || window.event;
                        var elemento = evt.target || evt.srcElement; 
                        if (elemento.hasAttribute("max") || elemento.hasAttribute("min") || 
                            elemento.hasAttribute("step")){
                            var valor = elemento.value;
                            if (isNaN(valor)) valor = 0;
                            valor = parseFloat(valor); 
                            if (valor > teC.focoNumeric.atmax){
                                valor = elemento.max;
                            } else if (valor < teC.focoNumeric.atmin){
                                valor = elemento.min;
                            }
                            elemento.value = valor;
                            var knum = document.getElementById("keyboard-numeric");
                            knum.style.display = "none";
                        }
                    },false);
            }    
        }
        wxG.agregarEventListener(null, [[document.body, "outer"]]);
        document.body.addEventListener("mousedown",
            function(event){
                var evt = event || window.event;
                var elemento = evt.target || evt.srcElement; 
                if ((elemento!=teC.focoNumeric) && (elemento.className!="knum")) {
                    var knum = document.getElementById("keyboard-numeric");
                    knum.style.display = "none";
                }
            }, false);
    },
    
    
    /* Manejador de los botones de caracteres "." y "-".
     * Los móviles Samsung con Android cuando encuentran un input type number
     * ofrecen un teclado númerico pero que no incluye puntos o guiones
     * con lo que no podemos poner números decimales ni negativos. El teclado
     * decimal acompaña unos botones para insertarlos.
     */
    symNumeric: function(event, car){
      var evt = event || window.event;
      if (evt.preventDefault) evt.preventDefault();
      var valor = teC.focoNumeric.value;
      if (teC.focoNumeric.value === "") {
          if (car == "-"){
              valor = teC.focoNumeric.min;
          } else {
              valor = teC.focoNumeric.min + car;
          }
      } else {
          if (car == "-"){
              //si hay un valor lo cambia de signo
              valor = -teC.focoNumeric.value;
              if (parseFloat(valor) < parseFloat(teC.focoNumeric.min)) {
                valor = teC.focoNumeric.min;    
              }
          } else if (teC.focoNumeric.value.indexOf(".")==-1) {
              //Insertamos punto solo si no lo tiene ya incluido
              valor = teC.focoNumeric.value + car;
          }      
          
      }
      teC.focoNumeric.value = valor; 
      teC.focoNumeric.focus();
    },
    
    
    /* Manejador de los botones para subir y bajar el valor del 
     * número.
    */
    variarTypeNumber: function(event, masMenos) {
      var evt = event || window.event;
      if (evt.preventDefault) evt.preventDefault();
      var valor = teC.focoNumeric.value;
      if (valor=="") valor = 0;
      if (isNaN(valor)) valor = 0;
      valor = parseFloat(valor);
      valor += masMenos * teC.focoNumeric.atstep; 
      valor = parseFloat(valor.toFixed(8));
      if (valor > teC.focoNumeric.atmax){
          valor = teC.focoNumeric.atmax;
      } else if (valor < teC.focoNumeric.atmin){
          valor = teC.focoNumeric.atmin;
      }      
      teC.focoNumeric.value = valor;
      wxG.enciendeEvento(teC.focoNumeric, "onchange");
      teC.focoNumeric.focus();
    },
    
    
    /* Función adicional, no necesaria para el Teclado Decimales pero que también afecta 
     * a los input. En navegadores de sobremesa como Opera y Chrome que aceptan el tipo number
     * e incluyen en el input unos botones para variar el número, no aplican de la 
     * misma forma el atributo size.
     * Con un atributo data-no-ajustar-ancho se omite este ajuste
     */
    ajustarAnchoNumber: function(elemento) {
        if (!elemento.hasAttribute("data-no-ajustar-ancho")) {
            var size = elemento.size;
            var fs = wxG.estiloActual(elemento, "font-size");
            if (fs=="") fs = "16px";
            if (fs.indexOf("em")>-1) fs = parseInt(fs)*16;
            var ancho = ((1 + size) * parseInt(fs)) / 16;
            elemento.style.width = ancho + "em";
        }
    }
};