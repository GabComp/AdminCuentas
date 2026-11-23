
(function () {
    // Token antiforgery generado por _AntiforgeryMeta.cshtml.
    // Se envía en todas las peticiones AJAX de jQuery.
    var meta = document.querySelector('meta[name="csrf-token"]');
    if (meta && meta.content) {
        $.ajaxSetup({
            headers: {
                "RequestVerificationToken": meta.content
            }
        });
    }
})();

// Filtros de captura sin manejadores inline (compatibles con CSP por nonce).
$(document).on('input', '[data-solo-enteros]', function () {
    this.value = this.value.replace(/[^0-9]/g, '');
});

$(document).on('input', '[data-solo-decimales]', function () {
    this.value = this.value.replace(/[^0-9.]/g, '');
});

$(document).on('click', '.js-imprimir', function () {
    window.print();
});

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

function CargarDatosName() {
    $.get('/Login/CargarDatosUsuario', function (resp) {

        const nombre = resp?.usuario?.NOMBRE;
        const $badge = $('#nombreUsuarioBadge');

        if (!nombre) {
            $badge.text('Mi Perfil');
            return;
        }

        $badge.text(nombre.toUpperCase());
    });
};

$(function () {
    CargarDatosName();

    const $navList = $('#navList');
    const $mobileToggle = $('#mobileToggle');

    function cerrarMenuMovil() {
        $navList.removeClass('active');
        $mobileToggle.removeClass('active').attr('aria-expanded', 'false');
    }

    $mobileToggle.on('click', function () {
        const abierto = $navList.toggleClass('active').hasClass('active');
        $(this).toggleClass('active', abierto).attr('aria-expanded', abierto ? 'true' : 'false');
    });

    // Submenú de perfil: hover en escritorio y clic/toque en cualquier tamaño.
    $('.nav-item.has-dropdown > .nav-link').on('click', function (e) {
        e.preventDefault();
        const $item = $(this).parent();
        const abierto = $item.toggleClass('open').hasClass('open');
        $(this).attr('aria-expanded', abierto ? 'true' : 'false');
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.nav-item.has-dropdown').length) {
            $('.nav-item.has-dropdown')
                .removeClass('open')
                .children('.nav-link')
                .attr('aria-expanded', 'false');
        }
    });

    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') {
            $('.nav-item.has-dropdown')
                .removeClass('open')
                .children('.nav-link')
                .attr('aria-expanded', 'false');
            cerrarMenuMovil();
        }
    });

    $navList.find('a').on('click', function () {
        if ($(this).closest('.nav-item.has-dropdown').length) {
            return;
        }
        if ($(window).width() <= 992) {
            cerrarMenuMovil();
        }
    });

    $(window).on('resize', function () {
        if ($(window).width() > 992) {
            cerrarMenuMovil();
        }
    });

    $(document).on('click', '.js-logout', function () {
        $.post('/Login/CerrarSesion')
            .always(function () {
                window.location.href = '/Home/Index';
            });
    });

    // Botón "Ir arriba".
    const $btnSubir = $('#btnSubir');

    if ($btnSubir.length) {
        $(window).on('scroll', function () {
            $btnSubir.toggleClass('mostrar', $(this).scrollTop() > 320);
        });

        $btnSubir.on('click', function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    // Banner de cookies (esenciales). Se recuerda la aceptación en el navegador.
    const $cookieBanner = $('#cookieBanner');

    if ($cookieBanner.length) {
        try {
            if (localStorage.getItem('comapa-cookies') === '1') {
                $cookieBanner.prop('hidden', true);
            } else {
                $cookieBanner.prop('hidden', false);
            }
        } catch (e) {
            $cookieBanner.prop('hidden', false);
        }

        $('#btnAceptarCookies').on('click', function () {
            try {
                localStorage.setItem('comapa-cookies', '1');
            } catch (e) {
                // Sin almacenamiento disponible; se oculta igualmente.
            }
            $cookieBanner.prop('hidden', true);
        });
    }
});

// Sesión expirada en una llamada AJAX: volver al login con aviso.
// La API del portal responde 401 con un JSON cuando la sesión ya no es válida.
$(document).ajaxError(function (event, xhr) {
    if (!xhr || xhr.status !== 401) {
        return;
    }

    if (window.location.pathname.toLowerCase() !== '/home/index') {
        window.location.href = '/Home/Index?sesion=expirada';
    }
});
