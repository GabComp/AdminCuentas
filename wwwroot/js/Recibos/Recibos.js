let dataRecibos = [];
let objResibos = {
    username: "alexis",
    password: "1234"
};
let DatosValidacion = {
    alias: "VALIDA_CUENTA",
    filtros: [
        {
            id_cuenta: 18,
            nombre: "Abrego Consuelo",
            total: 1000
        }
    ]
};
let ConsultaPDFModel = {
    alias: "PRINT_RECIBO",
    filtros: [
        {
            id_recibo: '',
        }
    ]
};

let option = 0;

$(document).ready(function () {
    LoginRecibos();
    ObtenerCuentasRecibosUsuario();

});

//eventos
document.addEventListener('DOMContentLoaded', function () {
    const listViewBtn = document.getElementById('listViewBtn');
    const cardViewBtn = document.getElementById('cardViewBtn');
    const listView = document.getElementById('listView');
    const cardView = document.getElementById('cardView');

    listViewBtn.addEventListener('click', function () {
        listView.style.display = 'block';
        cardView.style.display = 'none';
        listViewBtn.classList.add('active');
        cardViewBtn.classList.remove('active');
    });

    cardViewBtn.addEventListener('click', function () {
        listView.style.display = 'none';
        cardView.style.display = 'block';
        cardViewBtn.classList.add('active');
        listViewBtn.classList.remove('active');
    });

    // Funcionalidad para los botones de Ver y Descargar
    document.querySelectorAll('.btn-view').forEach(button => {
        button.addEventListener('click', function () {
            // Aquí puedes agregar la lógica para ver el documento
            const docName = this.closest('.document-item, .document-card')
                .querySelector('h3').textContent;
            alert(`Viendo documento: ${docName}`);
            // En producción, redirigirías a la vista del documento
            //ConsultaPDFModel.filtros[0].id_recibo = dataRecibos.id_recibo; 
        });
    });

    document.querySelectorAll('.btn-download').forEach(button => {
        button.addEventListener('click', function () {
            ConsultaReciboPDF();
            // Aquí puedes agregar la lógica para descargar
            const docName = this.closest('.document-item, .document-card')
                .querySelector('h3').textContent;
            alert(`Descargando documento: ${docName}`);
            // En producción, iniciarías la descarga del archivo
        });
    });
});

//$('#SelectAnioRecibos').on('change', function () {
//    let valor = this.value
//    if (valor != 0) {
//        let filtrados = dataRecibos.filter(doc => doc.ciclo_facturado.includes(valor));
//        renderizarDocumentos(filtrados);
//    } else {
//        renderizarDocumentos(dataRecibos);
//    }
//});
$('#SelectCuentaRecibos').on('change', function () {
    let valor = this.value
    if (valor != 0) {
        DatosValidacion.filtros[0].id_cuenta = valor;
        ConsultaRecibos();
    } else {
        renderizarDocumentos(dataRecibos);
    }
});

//Funciones
//function CargarYears(idSelect) {
//    const yearActual = new Date().getFullYear();
//    const yearAnterior = yearActual - 1;

//    const select = $(`${idSelect}`);
//    //select.empty();

//    select.append(
//        $('<option>', {
//            value: yearActual,
//            text: yearActual
//        })
//    );

//    select.append(
//        $('<option>', {
//            value: yearAnterior,
//            text: yearAnterior
//        })
//    );
//}

function CargarCuentasSelect(idSelect, data) {
    const select = $(`${idSelect}`);
    select.empty();

    //select.append(
    //        $('<option>', {
    //            value: '0',
    //            text: 'Selecciona una Cuenta'
    //        })
    //    );

    data.forEach(cuenta => {

        select.append(
            $('<option>', {
                value: cuenta.CUENTA_COMAPA,
                text: `${cuenta.CUENTA_COMAPA} - ${cuenta.ALIAS_CUENTA}`
            })
        );

    });

    let valor = $('#SelectCuentaRecibos').val();
    if (valor != 0) {
        DatosValidacion.filtros[0].id_cuenta = valor;
        ConsultaRecibos();
    } else {
        renderizarDocumentos(dataRecibos);
    }
};

function crearDocumentoItem(doc) {
    const li = document.createElement('li');
    // Se agrega mb-3 a la clase para separar cada fila en la vista de lista
    li.className = 'document-item mb-3';

    li.innerHTML = `
        <div class="document-info">
            <div class="document-icon">
                <i class="far fa-file-pdf"></i>
            </div>
            <div class="document-details">
                <h3 class="mb-1">${doc.ciclo_facturado}</h3>
                <p class="mb-0 text-muted">Consumo: ${doc.consumo} • Periodo: ${doc.periodo} </p>
            </div>
        </div>
        <!-- Se aplica d-flex, gap-2 y flex-wrap para acomodar los botones. mt-3 separa los botones del texto en móviles -->
        <div class="document-actions d-flex flex-wrap flex-sm-nowrap gap-2 mt-3 mt-sm-0">
            <button class="btn btn-view flex-grow-1 mb-2 mb-sm-0">
                <i class="fas fa-eye"></i> Ver
            </button>
            <button class="btn btn-download flex-grow-1 mb-2 mb-sm-0">
                <i class="fas fa-download"></i> Descargar
            </button>
        </div>
    `;

    asignarEventos(li, doc);
    return li;
}

function crearDocumentoItemCard(doc) {
    const div = document.createElement('div');
    const container = document.querySelector('#cardView .document-cards');
    container.classList.add('row'); // Añade la clase row
    // Se agrega p-3 para reducir el relleno interno (padding) y hacer la tarjeta más compacta
    div.className = 'document-card d-flex flex-column h-100 mb-3 p-3';

    div.innerHTML = `
        <div class="card-icon mb-2">
            <!-- fs-4 hace el icono ligeramente más pequeño si estabas usando el tamaño por defecto -->
            <i class="far fa-file-pdf fs-4"></i>
        </div>
        
        <div class="card-content flex-grow-1">
            <!-- Se cambia <h3> a <h5> para reducir el tamaño del título -->
            <h5 class="mb-1">${doc.ciclo_facturado}</h5>
            
            <!-- Se agrega la clase 'small' para que el texto descriptivo ocupe menos espacio -->
            <p class="mb-3 text-muted small">
                Consumo: ${doc.consumo} • Periodo: ${doc.periodo} 
            </p>
        </div>
        
        <!-- Cambiamos a flex-column para forzar un botón debajo del otro. 
             El gap-2 se encarga del espacio entre ellos sin necesidad de poner mb-2 en cada botón -->
        <div class="card-actions d-flex flex-column gap-2 mt-auto">
            <!-- w-100 asegura que los botones tomen todo el ancho de la tarjeta -->
            <button class="btn btn-view w-100">
                <i class="fas fa-eye"></i> Ver
            </button>
            <button class="btn btn-download w-100">
                <i class="fas fa-download"></i> Descargar
            </button>
        </div>
    `;

    asignarEventos(div, doc);
    return div;
}

function renderizarDocumentos(lista) {
    const contenedor = document.querySelector('.document-list');
    contenedor.innerHTML = '';
    const contenedorCard = document.querySelector('.document-cards');
    contenedorCard.innerHTML = '';

    lista.forEach(doc => {
        const item = crearDocumentoItem(doc);
        contenedor.appendChild(item);
        const itemCard = crearDocumentoItemCard(doc);
        contenedorCard.appendChild(itemCard);
    });
};

function asignarEventos(elemento, doc) {
    elemento.querySelector('.btn-view').addEventListener('click', () => {
        const ventanaPDF = window.open('', '_blank');
        ConsultaPDFModel.filtros[0].id_recibo = doc.id_recibo;
        option = 1;
        GenerarTokenPDF(ventanaPDF);
    });

    elemento.querySelector('.btn-download').addEventListener('click', () => {
        //const ventanaPDF = window.open('', '_blank');
        option = 2;
        ConsultaPDFModel.filtros[0].id_recibo = doc.id_recibo;
        GenerarTokenPDF();
    });
};

function base64ToPdfBlob(base64) {
    base64 = base64.replace(/\s/g, '');

    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);

    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }

    const byteArray = new Uint8Array(byteNumbers);
    return new Blob([byteArray], { type: 'application/pdf' });
}

function descargarPDF(base64, nombreArchivo) {
    const blob = base64ToPdfBlob(base64);
    const url = window.URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = nombreArchivo;
    a.click();

    window.URL.revokeObjectURL(url);
}

function getTimestamp() {
    const now = new Date();

    const yyyy = now.getFullYear();
    const MM = String(now.getMonth() + 1).padStart(2, '0');
    const dd = String(now.getDate()).padStart(2, '0');
    const HH = String(now.getHours()).padStart(2, '0');
    const mm = String(now.getMinutes()).padStart(2, '0');
    const ss = String(now.getSeconds()).padStart(2, '0');

    return `${yyyy}${MM}${dd}${HH}${mm}${ss}`;
}

//AJAX
function LoginRecibos() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/LoginRecibos',
        type: "POST",
        data: JSON.stringify(objResibos),
        cache: false,
        success: function (data) {
            if (data.ok) { ValidarLoginRecibos(); }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};


function ValidarLoginRecibos() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/ValidarLoginRecibos',
        type: "POST",
        data: JSON.stringify(DatosValidacion),
        cache: false,
        success: function (data) {
            if (data.body[0].usrValido) {
                /*ConsultaRecibos();*/
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};

function ConsultaRecibos() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/ConsultaRecibos',
        type: "POST",
        data: JSON.stringify(DatosValidacion),
        cache: false,
        success: function (data) {
            if (data.ok) {
                dataRecibos = data.body.slice(0, 12);
                renderizarDocumentos(dataRecibos);
            }

        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};
function GenerarTokenPDF(ventanaPDF) {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/LoginRecibos',
        type: "POST",
        data: JSON.stringify(objResibos),
        cache: false,
        success: function (data) {
            if (data.ok) { ConsultaReciboPDF(ventanaPDF); }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};
function ConsultaReciboPDF(ventanaPDF) {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/ConsultaReciboPDF',
        type: "POST",
        data: JSON.stringify(ConsultaPDFModel),
        cache: false,
        success: function (data) {
            if (data.ok) {
                console.log(data);
                if (option == 1) {
                    const blob = base64ToPdfBlob(data.body);
                    const url = URL.createObjectURL(blob);

                    ventanaPDF.location.href = url;
                } else {
                    descargarPDF(data.body, `Recibo_${getTimestamp()}`);
                }
            } else {
                let obj = JSON.parse(data.detail);
                Swal.fire({ title: "ERROR!!", text: `${obj.message}`, icon: "error" });
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};
//
function ObtenerCuentasRecibosUsuario() {

    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/ObtenerCuentasRecibosUsuario',
        type: "POST",
        data: JSON.stringify(),
        cache: false,
        success: function (data) {
            if (data.length > 0) {
                CargarCuentasSelect('#SelectCuentaRecibos', data);
            }
        },
        error: function () {
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL INICIAE SESION!`, icon: "error" });
        }
    });
};