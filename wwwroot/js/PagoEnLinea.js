$(function () {
    const apiConsulta = '/api/pago/obtener-adeudo';
    const apiIniciar = '/api/pago/iniciar';

    const $cuenta = $('#txtCuenta');
    const $nombre = $('#txtNombre');
    const $msg = $('#msgConsulta');
    const $tarjeta = $('#divSaldoPendiente');
    const $btnConsulta = $('#btnConsulta');
    const $spinner = $('#spinnerConsulta');
    const $btnTexto = $('#txtBtnConsulta');
    const $icono = $('#iconoConsulta');
    const $titular = $('#txtTitular');
    const $adeudo = $('#txtAdeudo');
    const $estado = $('#txtEstadoAdeudo');
    const $bloquePago = $('#bloquePago');

    const $btnPagar = $('#btnPagar');
    const $spinnerPagar = $('#spinnerPagar');
    const $iconoPagar = $('#iconoPagar');
    const $textoPagar = $('#textoPagar');

    const $divMisCuentas = $('#divMisCuentas');
    const $selectMisCuentas = $('#selectMisCuentas');

    const pagoHabilitado = window.comapaPagoHabilitado === true;
    const pagoMensaje = window.comapaPagoMensaje
        || 'El pago en línea estará disponible próximamente.';

    let consultando = false;
    let pagoDisponible = false;
    let pagoCargando = false;
    let montoActual = 0;
    let cuentaActual = 0;

    function mostrarMensaje(texto, tipo) {
        $msg
            .removeClass('d-none alert-success alert-warning alert-danger alert-info')
            .addClass('alert-' + tipo)
            .text(texto);
    }

    function limpiarMensaje() {
        $msg
            .addClass('d-none')
            .removeClass('alert-success alert-warning alert-danger alert-info')
            .text('');
    }

    function formatearMoneda(valor) {
        return new Intl.NumberFormat('es-MX', {
            style: 'currency',
            currency: 'MXN'
        }).format(Number(valor) || 0);
    }

    function setCargando(activo) {
        consultando = activo;
        $btnConsulta.prop('disabled', activo);
        $cuenta.prop('readonly', activo);
        $spinner.toggleClass('d-none', !activo);
        $icono.toggleClass('d-none', activo);
        $btnTexto.text(activo ? 'CONSULTANDO...' : 'CONSULTA');
    }

    function actualizarBotonPagar() {
        $btnPagar.prop('disabled', !pagoDisponible || pagoCargando);
        $spinnerPagar.toggleClass('d-none', !pagoCargando);
        $iconoPagar.toggleClass('d-none', pagoCargando);
        $textoPagar.text(pagoCargando ? 'CONECTANDO...' : 'Pagar');
    }

    function setPagoDisponible(disponible) {
        pagoDisponible = disponible && pagoHabilitado;

        $btnPagar
            .toggleClass('btn-success', pagoDisponible)
            .toggleClass('btn-secondary', !pagoDisponible);

        $iconoPagar
            .toggleClass('fa-credit-card', pagoDisponible)
            .toggleClass('fa-lock', !pagoDisponible);

        $btnPagar.attr(
            'title',
            !pagoHabilitado
                ? pagoMensaje
                : (pagoDisponible ? 'Pagar con tarjeta' : 'Consulta primero una cuenta con adeudo'));

        actualizarBotonPagar();
    }

    function setPagoCargando(activo) {
        pagoCargando = activo;
        actualizarBotonPagar();
    }

    function ocultarTarjeta() {
        $tarjeta.addClass('d-none').removeClass('fade-in fade-out');
    }

    function mostrarTarjeta() {
        $tarjeta.removeClass('d-none fade-out fade-in');
        void $tarjeta[0].offsetWidth;
        $tarjeta.addClass('fade-in');
    }

    function desplazarATarjeta() {
        if ($(window).width() <= 768) {
            $('html, body').animate({
                scrollTop: $tarjeta.offset().top - 120
            }, 600);
        }
    }

    function consultarAdeudo() {
        if (consultando) {
            return;
        }

        limpiarMensaje();
        ocultarTarjeta();
        setPagoDisponible(false);
        montoActual = 0;
        cuentaActual = 0;
        $nombre.val('');

        const valor = ($cuenta.val() || '').toString().trim();

        if (!/^\d{1,6}$/.test(valor) || Number(valor) <= 0) {
            mostrarMensaje('Ingresa un número de cuenta válido (solo números, máximo 6 dígitos).', 'danger');
            $cuenta.trigger('focus');
            return;
        }

        setCargando(true);

        $.ajax({
            url: apiConsulta,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify({ cuenta: Number(valor) }),
            cache: false
        })
            .done(function (data) {
                if (!data || data.exito === false) {
                    mostrarMensaje(
                        (data && data.mensaje) || 'No fue posible consultar el adeudo.',
                        'danger');
                    return;
                }

                if (!data.cuentaValida) {
                    mostrarMensaje(
                        data.mensaje || 'La cuenta indicada no existe.',
                        'warning');
                    return;
                }

                $nombre.val(data.nombre || '');
                $titular.text(data.nombre || '—');

                const monto = Number(data.adeudo) || 0;
                $adeudo.text(formatearMoneda(monto));

                if (data.tieneNegociacion) {
                    $estado.text(data.mensaje || 'La cuenta tiene una negociación activa.');
                    $bloquePago
                        .removeClass('alert-info alert-success')
                        .addClass('alert-warning')
                        .html('<i class="fas fa-circle-info me-1"></i> Acude a la sucursal para regularizar tu cuenta.');
                    mostrarMensaje('La cuenta tiene una negociación activa.', 'warning');
                } else if (monto > 0) {
                    if (pagoHabilitado) {
                        $estado.text('Puedes realizar el pago con tarjeta de crédito o débito.');
                        $bloquePago
                            .removeClass('alert-warning alert-success')
                            .addClass('alert-info')
                            .html('<i class="fas fa-lock me-1"></i> El cobro con tarjeta se realiza en la pasarela segura de EVO Payments.');
                        mostrarMensaje('Adeudo consultado correctamente.', 'success');
                        montoActual = monto;
                        cuentaActual = Number(valor);
                    } else {
                        $estado.text(pagoMensaje);
                        $bloquePago
                            .removeClass('alert-warning alert-success')
                            .addClass('alert-info')
                            .html('<i class="fas fa-clock me-1"></i> ' + $('<div>').text(pagoMensaje).html());
                        mostrarMensaje('Adeudo consultado correctamente.', 'success');
                    }

                    setPagoDisponible(true);
                } else {
                    $estado.text('No tienes adeudo pendiente. ¡Gracias por tu pago!');
                    $bloquePago
                        .removeClass('alert-warning alert-info')
                        .addClass('alert-success')
                        .html('<i class="fas fa-circle-check me-1"></i> No hay adeudo pendiente en esta cuenta.');
                    mostrarMensaje('La cuenta no tiene adeudo pendiente.', 'success');
                }

                mostrarTarjeta();
                desplazarATarjeta();
            })
            .fail(function (xhr) {
                let mensaje = 'No fue posible consultar el adeudo. Intenta de nuevo.';

                if (xhr.responseJSON && xhr.responseJSON.mensaje) {
                    mensaje = xhr.responseJSON.mensaje;
                } else if (xhr.status === 429) {
                    mensaje = 'Demasiadas consultas. Espera un momento e intenta de nuevo.';
                }

                mostrarMensaje(mensaje, 'danger');
            })
            .always(function () {
                setCargando(false);
            });
    }

    function cargarCuentasVinculadas() {
        // Solo para usuarios autenticados con cuentas vinculadas: se muestra un
        // selector para consultar el adeudo de una cuenta ya agregada, sin teclearla.
        $.get('/Login/CargarDatosUsuario')
            .done(function (resp) {
                const usuario = resp && resp.usuario;
                if (!usuario) {
                    return;
                }

                $.ajax({
                    contentType: 'application/json; charset=utf-8',
                    url: '/AdministradorDeCuentas/ConsultarCuentasUsuario',
                    type: 'POST',
                    data: JSON.stringify({ ID_USUARIO_WEB: 0 }),
                    cache: false,
                    complete: function () {
                        $.get('/Recibos/ObtenerCuentasRecibosUsuario')
                            .done(function (cuentas) {
                                if (cuentas && cuentas.length > 0) {
                                    poblarMisCuentas(cuentas);
                                }
                            });
                    }
                });
            });
    }

    function poblarMisCuentas(cuentas) {
        $selectMisCuentas.empty();
        $selectMisCuentas.append($('<option>', { value: '', text: 'Selecciona una cuenta' }));

        cuentas.forEach(function (c) {
            $selectMisCuentas.append(
                $('<option>', {
                    value: c.CUENTA_COMAPA,
                    text: c.CUENTA_COMAPA + ' - ' + (c.ALIAS_CUENTA || 'Cuenta')
                })
            );
        });

        $divMisCuentas.removeClass('d-none');

        // Con una sola cuenta, se prepara el adeudo y el botón de pago automáticamente.
        if (cuentas.length === 1) {
            $selectMisCuentas.val(cuentas[0].CUENTA_COMAPA);
            $cuenta.val(cuentas[0].CUENTA_COMAPA);
            consultarAdeudo();
        }
    }

    function limpiarAlmacenamientoEvo() {
        // Solo se limpian claves relacionadas con la pasarela; no se borra
        // todo el almacenamiento del origen.
        try {
            Object.keys(sessionStorage)
                .filter(function (clave) { return /evo|checkout/i.test(clave); })
                .forEach(function (clave) { sessionStorage.removeItem(clave); });

            Object.keys(localStorage)
                .filter(function (clave) { return /evo|checkout/i.test(clave); })
                .forEach(function (clave) { localStorage.removeItem(clave); });
        } catch (e) {
            // Algunos navegadores pueden bloquear el almacenamiento.
        }
    }

    function cargarCheckoutEvo(scriptUrl) {
        return new Promise(function (resolve, reject) {
            const anterior = document.getElementById('evo-checkout-script');
            if (anterior) {
                anterior.remove();
            }

            try {
                delete window.Checkout;
            } catch (e) {
                window.Checkout = undefined;
            }

            const script = document.createElement('script');
            script.id = 'evo-checkout-script';
            script.src = scriptUrl;
            script.type = 'text/javascript';
            script.setAttribute('data-error', 'errorCallback');
            script.setAttribute('data-cancel', 'cancelCallback');
            script.onload = function () { resolve(); };
            script.onerror = function () {
                reject(new Error('No se pudo cargar la pasarela de pago.'));
            };

            document.body.appendChild(script);
        });
    }

    function abrirModalEvo(sessionId) {
        const modalElement = document.getElementById('modalPagoEvo');

        if (!modalElement || typeof bootstrap === 'undefined') {
            throw new Error('No se pudo abrir la ventana de pago.');
        }

        $('#evo-checkout-container').empty();

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
            keyboard: false,
            backdrop: 'static'
        });

        modal.show();

        setTimeout(function () {
            try {
                const configuracion = Checkout.configure({
                    session: {
                        id: sessionId
                    }
                });

                const mostrarCheckout = function () {
                    Checkout.showEmbeddedPage('#evo-checkout-container');
                };

                if (configuracion && typeof configuracion.then === 'function') {
                    configuracion
                        .then(mostrarCheckout)
                        .catch(function () {
                            mostrarMensaje('No se pudo inicializar la pasarela de pago.', 'danger');
                            cerrarModalEvo();
                        });
                } else {
                    mostrarCheckout();
                }
            } catch (e) {
                console.error('EVO configure:', e);
                mostrarMensaje('No se pudo inicializar la pasarela de pago.', 'danger');
                cerrarModalEvo();
            }
        }, 250);
    }

    function iniciarPago() {
        if (!pagoDisponible || pagoCargando || montoActual <= 0 || cuentaActual <= 0) {
            return;
        }

        limpiarMensaje();
        setPagoCargando(true);

        // EVO conserva la última sesión en el almacenamiento del navegador.
        // Se limpian solo sus claves para no borrar datos de otros módulos.
        limpiarAlmacenamientoEvo();

        $.ajax({
            url: apiIniciar,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify({ cuenta: cuentaActual }),
            cache: false
        })
            .done(function (data) {
                if (!data || data.exito === false) {
                    mostrarMensaje(
                        (data && data.mensaje) || 'No fue posible iniciar el pago.',
                        'danger');
                    return;
                }

                cargarCheckoutEvo(data.scriptUrl)
                    .then(function () {
                        if (typeof Checkout === 'undefined') {
                            mostrarMensaje('La pasarela de pago no está disponible. Intenta de nuevo.', 'danger');
                            return;
                        }

                        abrirModalEvo(data.sessionId);
                    })
                    .catch(function () {
                        mostrarMensaje('No se pudo cargar la pasarela de pago. Intenta de nuevo.', 'danger');
                    });
            })
            .fail(function (xhr) {
                let mensaje = 'No fue posible iniciar el pago. Intenta de nuevo.';

                if (xhr.responseJSON && xhr.responseJSON.mensaje) {
                    mensaje = xhr.responseJSON.mensaje;
                } else if (xhr.status === 429) {
                    mensaje = 'Demasiados intentos de pago. Espera un momento e intenta de nuevo.';
                }

                mostrarMensaje(mensaje, 'danger');
            })
            .always(function () {
                setPagoCargando(false);
            });
    }

    function cerrarModalEvo() {
        const modalElement = document.getElementById('modalPagoEvo');

        if (!modalElement || typeof bootstrap === 'undefined') {
            return;
        }

        const modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) {
            modal.hide();
        }
    }

    window.errorCallback = function (error) {
        console.error('EVO Payments:', error);
        mostrarMensaje('La pasarela no pudo procesar el pago. Intenta de nuevo.', 'danger');
        cerrarModalEvo();
    };

    window.cancelCallback = function () {
        mostrarMensaje('Pago cancelado.', 'warning');
        cerrarModalEvo();
    };

    $('#modalPagoEvo').on('hidden.bs.modal', function () {
        $('#evo-checkout-container').empty();

        const script = document.getElementById('evo-checkout-script');
        if (script) {
            script.remove();
        }

        try {
            delete window.Checkout;
        } catch (e) {
            window.Checkout = undefined;
        }

        limpiarAlmacenamientoEvo();
    });

    $('#btnConsulta').on('click', consultarAdeudo);

    $cuenta.on('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            consultarAdeudo();
        }
    });

    $cuenta.on('input', function () {
        const limpio = this.value.replace(/\D/g, '').slice(0, 6);
        if (this.value !== limpio) {
            this.value = limpio;
        }
    });

    $('#btnCancelar').on('click', function () {
        $tarjeta.removeClass('fade-in').addClass('fade-out');

        setTimeout(function () {
            $tarjeta.addClass('d-none').removeClass('fade-out');
        }, 800);

        $cuenta.val('').prop('readonly', false).trigger('focus');
        $nombre.val('');
        montoActual = 0;
        cuentaActual = 0;
        setPagoDisponible(false);
        limpiarMensaje();
    });

    $('#btnPagar').on('click', iniciarPago);

    $selectMisCuentas.on('change', function () {
        const valor = this.value;
        if (!valor) {
            return;
        }

        $cuenta.val(valor);
        consultarAdeudo();
    });

    cargarCuentasVinculadas();
});
