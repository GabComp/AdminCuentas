
let Usuario_ID = 0

let objUser = {
    ACCION: '',
    USUARIO: '',
    CONTRASENIA: '',
    CORREO_ELECTRONICO: '',
    TELEFONO_CEL: '',
    MOTIVO_BAJA: '',
    ID_USUARIO: '',
    CODIGO: ''
};

function LimpiarObjetoUsuario() {
    objUser.ACCION = '';
    objUser.USUARIO = '';
    objUser.CONTRASENIA = '';
    objUser.CORREO_ELECTRONICO = '';
    objUser.TELEFONO_CEL = '';
    objUser.MOTIVO_BAJA = '';
    objUser.ID_USUARIO = '';
    objUser.CODIGO = '';
};

$(document).ready(function () {
    CargarDatosName();
});

function CargarDatosName() {
    $.get('/Login/CargarDatosUsuario', function (resp) {

        const nombre = resp?.usuario?.NOMBRE;

        if (!nombre) {
            $("#perfiLink").hide();
            return;
        }
        $("#perfiLink").show().text(nombre.toUpperCase());
    });
};