(function () {
    const boton = document.getElementById('btnPagarConstancia');

    if (!boton) {
        return;
    }

    const csrf = document.querySelector('meta[name="csrf-token"]')?.content || '';

    function mostrar(mensaje, tipo) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                title: tipo === 'error' ? 'Error' : 'Aviso',
                text: mensaje,
                icon: tipo === 'error' ? 'error' : 'warning'
            });
            return;
        }

        window.alert(mensaje);
    }

    function limpiarAlmacenamientoEvo() {
        try {
            Object.keys(sessionStorage)
                .filter(clave => /evo|checkout/i.test(clave))
                .forEach(clave => sessionStorage.removeItem(clave));

            Object.keys(localStorage)
                .filter(clave => /evo|checkout/i.test(clave))
                .forEach(clave => localStorage.removeItem(clave));
        } catch {
            // Algunos navegadores bloquean el almacenamiento.
        }
    }

    function cargarCheckoutEvo(scriptUrl) {
        return new Promise((resolve, reject) => {
            if (typeof Checkout !== 'undefined') {
                resolve();
                return;
            }

            const existente = document.getElementById('evo-checkout-script');
            if (existente) {
                existente.addEventListener('load', () => resolve());
                existente.addEventListener('error', () => reject(new Error('No se pudo cargar la pasarela.')));
                return;
            }

            const script = document.createElement('script');
            script.id = 'evo-checkout-script';
            script.src = scriptUrl;
            script.type = 'text/javascript';
            script.setAttribute('data-error', 'errorCallback');
            script.setAttribute('data-cancel', 'cancelCallback');
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('No se pudo cargar la pasarela de pago.'));
            document.body.appendChild(script);
        });
    }

    function abrirModalEvo(sessionId) {
        const modalElement = document.getElementById('modalPagoEvo');

        if (!modalElement || typeof bootstrap === 'undefined') {
            throw new Error('No se pudo abrir la ventana de pago.');
        }

        const contenedor = document.getElementById('evo-checkout-container');
        contenedor.innerHTML = '';

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
            keyboard: false,
            backdrop: 'static'
        });

        modal.show();

        setTimeout(function () {
            try {
                const configuracion = Checkout.configure({
                    session: { id: sessionId }
                });

                const mostrarCheckout = () => Checkout.showEmbeddedPage('#evo-checkout-container');

                if (configuracion && typeof configuracion.then === 'function') {
                    configuracion.then(mostrarCheckout).catch(function () {
                        mostrar('No se pudo inicializar la pasarela de pago.', 'error');
                        modal.hide();
                        boton.disabled = false;
                    });
                } else {
                    mostrarCheckout();
                }
            } catch (e) {
                console.error('EVO configure:', e);
                mostrar('No se pudo inicializar la pasarela de pago.', 'error');
                modal.hide();
                boton.disabled = false;
            }
        }, 250);
    }

    boton.addEventListener('click', async function () {
        if (!window.constanciaPago || !window.constanciaPago.idConstancia) {
            mostrar('No se encontró la constancia para pagar.', 'error');
            return;
        }

        boton.disabled = true;
        limpiarAlmacenamientoEvo();

        try {
            const respuesta = await fetch('/api/pago/constancia/iniciar', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json; charset=utf-8',
                    'RequestVerificationToken': csrf
                },
                body: JSON.stringify({
                    idConstancia: window.constanciaPago.idConstancia,
                    idCuenta: window.constanciaPago.idCuenta
                })
            });

            const data = await respuesta.json();

            if (!respuesta.ok || !data || data.exito === false) {
                throw new Error((data && data.mensaje) || 'No fue posible iniciar el pago.');
            }

            await cargarCheckoutEvo(data.scriptUrl);

            if (typeof Checkout === 'undefined') {
                throw new Error('La pasarela de pago no está disponible.');
            }

            abrirModalEvo(data.sessionId);
        } catch (e) {
            boton.disabled = false;
            mostrar(e.message || 'No fue posible iniciar el pago.', 'error');
        }
    });

    window.errorCallback = function () {
        boton.disabled = false;
        mostrar('El pago no pudo completarse. Intenta de nuevo.', 'error');
    };

    window.cancelCallback = function () {
        boton.disabled = false;
        mostrar('Pago cancelado.', 'warning');
    };
})();
