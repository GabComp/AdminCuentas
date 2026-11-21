let dataRecibos = [];
let DatosValidacion = {
    alias: "VALIDA_CUENTA",
    filtros: [
        {
            id_cuenta: 0,
            nombre: "",
            total: 0
        }
    ]
};
let ConsultaPDFModel = {
    alias: "PRINT_RECIBO",
    filtros: [
        {
            id_cuenta: 0,
            id_recibo: ''
        }
    ]
};

let option = 0;

const RECIBOS_POR_PAGINA = 6;
let paginaActual = 1;

$(document).ready(function () {
    AsegurarCuentasUsuario();

    $('#btnRecibosAnterior').on('click', function () {
        if (paginaActual > 1) {
            paginaActual--;
            renderizarDocumentos();
        }
    });

    $('#btnRecibosSiguiente').on('click', function () {
        const totalPaginas = Math.ceil(dataRecibos.length / RECIBOS_POR_PAGINA);
        if (paginaActual < totalPaginas) {
            paginaActual++;
            renderizarDocumentos();
        }
    });
});

function AsegurarCuentasUsuario() {
    // La lista de cuentas vive en la sesión del servidor y es la que valida
    // que el usuario solo consulte sus propios recibos.
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/AdministradorDeCuentas/ConsultarCuentasUsuario',
        type: "POST",
        data: JSON.stringify({ ID_USUARIO_WEB: 0 }),
        cache: false,
        complete: function () {
            ObtenerCuentasRecibosUsuario();
        }
    });
}

function escaparHtml(valor) {
    return String(valor ?? '').replace(/[&<>"']/g, function (caracter) {
        return {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[caracter];
    });
}

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
        listViewBtn.setAttribute('aria-pressed', 'true');
        cardViewBtn.setAttribute('aria-pressed', 'false');
    });

    cardViewBtn.addEventListener('click', function () {
        listView.style.display = 'none';
        cardView.style.display = 'block';
        cardViewBtn.classList.add('active');
        listViewBtn.classList.remove('active');
        cardViewBtn.setAttribute('aria-pressed', 'true');
        listViewBtn.setAttribute('aria-pressed', 'false');
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
        ConsultaPDFModel.filtros[0].id_cuenta = valor;
        ConsultaRecibos();
    } else {
        renderizarDocumentos();
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
        ConsultaPDFModel.filtros[0].id_cuenta = valor;
        ConsultaRecibos();
    } else {
        renderizarDocumentos();
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
                <h2 class="document-title mb-1">${escaparHtml(doc.ciclo_facturado)}</h2>
                <p class="mb-0 text-muted">Consumo: ${escaparHtml(doc.consumo)} • Periodo: ${escaparHtml(doc.periodo)} </p>
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
            <h2 class="document-title mb-1">${escaparHtml(doc.ciclo_facturado)}</h2>
            
            <!-- Se agrega la clase 'small' para que el texto descriptivo ocupe menos espacio -->
            <p class="mb-3 text-muted small">
                Consumo: ${escaparHtml(doc.consumo)} • Periodo: ${escaparHtml(doc.periodo)} 
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

function mostrarEstadoRecibos(mensaje, icono, girando) {
    const estado = document.getElementById('estadoRecibos');
    if (!estado) return;

    estado.classList.remove('d-none');
    estado.innerHTML = `<i class="fas ${icono}${girando ? ' fa-spin' : ''}"></i><p>${escaparHtml(mensaje)}</p>`;
}

function actualizarVistasRecibos(mostrar) {
    const listViewBtn = document.getElementById('listViewBtn');
    const cardViewBtn = document.getElementById('cardViewBtn');
    const listView = document.getElementById('listView');
    const cardView = document.getElementById('cardView');

    listViewBtn.disabled = !mostrar;
    cardViewBtn.disabled = !mostrar;

    if (!mostrar) {
        listView.style.display = 'none';
        cardView.style.display = 'none';
        return;
    }

    const verLista = listViewBtn.classList.contains('active');
    listView.style.display = verLista ? 'block' : 'none';
    cardView.style.display = verLista ? 'none' : 'block';
}

function actualizarPaginacion() {
    const contenedorPaginacion = document.getElementById('recibosPaginacion');
    if (!contenedorPaginacion) return;

    const totalPaginas = Math.max(1, Math.ceil(dataRecibos.length / RECIBOS_POR_PAGINA));

    if (dataRecibos.length <= RECIBOS_POR_PAGINA) {
        contenedorPaginacion.classList.add('d-none');
        return;
    }

    contenedorPaginacion.classList.remove('d-none');
    document.getElementById('btnRecibosAnterior').disabled = paginaActual <= 1;
    document.getElementById('btnRecibosSiguiente').disabled = paginaActual >= totalPaginas;
    document.getElementById('recibosInfo').textContent = `Página ${paginaActual} de ${totalPaginas}`;
}

function renderizarDocumentos() {
    const totalPaginas = Math.max(1, Math.ceil(dataRecibos.length / RECIBOS_POR_PAGINA));
    if (paginaActual > totalPaginas) paginaActual = totalPaginas;

    const inicio = (paginaActual - 1) * RECIBOS_POR_PAGINA;
    const vista = dataRecibos.slice(inicio, inicio + RECIBOS_POR_PAGINA);

    const contenedor = document.querySelector('.document-list');
    contenedor.innerHTML = '';
    const contenedorCard = document.querySelector('.document-cards');
    contenedorCard.innerHTML = '';

    vista.forEach(doc => {
        const item = crearDocumentoItem(doc);
        contenedor.appendChild(item);
        const itemCard = crearDocumentoItemCard(doc);
        contenedorCard.appendChild(itemCard);
    });

    const estado = document.getElementById('estadoRecibos');
    const hayDocumentos = dataRecibos.length > 0;

    actualizarVistasRecibos(hayDocumentos);

    if (estado) {
        if (hayDocumentos) {
            estado.classList.add('d-none');
        } else {
            mostrarEstadoRecibos('No hay recibos disponibles para esta cuenta.', 'fa-file-circle-xmark', false);
        }
    }

    actualizarPaginacion();
};

function asignarEventos(elemento, doc) {
    elemento.querySelector('.btn-view').addEventListener('click', () => {
        const ventanaPDF = window.open('', '_blank');
        ConsultaPDFModel.filtros[0].id_cuenta = DatosValidacion.filtros[0].id_cuenta;
        ConsultaPDFModel.filtros[0].id_recibo = doc.id_recibo;
        option = 1;
        ConsultaReciboPDF(ventanaPDF);
    });

    elemento.querySelector('.btn-download').addEventListener('click', () => {
        ConsultaPDFModel.filtros[0].id_cuenta = DatosValidacion.filtros[0].id_cuenta;
        ConsultaPDFModel.filtros[0].id_recibo = doc.id_recibo;
        option = 2;
        ConsultaReciboPDF();
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
function ConsultaRecibos() {
    $.ajax({
        contentType: "application/json; charset=utf-8",
        url: '/Recibos/ConsultaRecibos',
        type: "POST",
        data: JSON.stringify(DatosValidacion),
        cache: false,
        success: function (data) {
            if (data.ok) {
                dataRecibos = data.body || [];
                paginaActual = 1;
                renderizarDocumentos();
            } else {
                actualizarVistasRecibos(false);
                mostrarEstadoRecibos('No fue posible consultar los recibos de esta cuenta.', 'fa-circle-exclamation', false);
                Swal.fire({
                    title: "ERROR!!",
                    text: data.message || 'No fue posible consultar los recibos.',
                    icon: "error"
                });
            }
        },
        error: function (xhr) {
            const mensaje = xhr.responseJSON && xhr.responseJSON.message
                ? xhr.responseJSON.message
                : 'No fue posible consultar los recibos.';
            actualizarVistasRecibos(false);
            mostrarEstadoRecibos(mensaje, 'fa-circle-exclamation', false);
            Swal.fire({ title: "ERROR!!", text: mensaje, icon: "error" });
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
                if (option == 1) {
                    const blob = base64ToPdfBlob(data.body);
                    const url = URL.createObjectURL(blob);

                    ventanaPDF.location.href = url;
                } else {
                    descargarPDF(data.body, `Recibo_${getTimestamp()}`);
                }
            } else {
                Swal.fire({
                    title: "ERROR!!",
                    text: data.message || 'No fue posible generar el recibo.',
                    icon: "error"
                });
            }
        },
        error: function (xhr) {
            const mensaje = xhr.responseJSON && xhr.responseJSON.message
                ? xhr.responseJSON.message
                : 'No fue posible generar el recibo.';
            Swal.fire({ title: "ERROR!!", text: mensaje, icon: "error" });
        }
    });
};
//
function ObtenerCuentasRecibosUsuario() {

    $.ajax({
        url: '/Recibos/ObtenerCuentasRecibosUsuario',
        type: "GET",
        cache: false,
        success: function (data) {
            if (data && data.length > 0) {
                CargarCuentasSelect('#SelectCuentaRecibos', data);
            } else {
                mostrarEstadoRecibos('No tienes cuentas vinculadas. Agrega una en "Administrar Cuenta".', 'fa-link-slash', false);
            }
        },
        error: function () {
            mostrarEstadoRecibos('No fue posible obtener tus cuentas. Intenta de nuevo.', 'fa-circle-exclamation', false);
            Swal.fire({ title: "ERROR!!", text: `OCURRIO UN ERROR AL OBTENER LAS CUENTAS!`, icon: "error" });
        }
    });
};