// ================================================================
// Promociones por cantidad (NxM) y porcentaje — lado navegador
// ----------------------------------------------------------------
// IMPORTANTE: este archivo NO contiene reglas de cálculo. El servidor decide qué promoción
// corresponde (POST ?handler=EvaluarPromos) y vuelve a validarla al grabar; acá solo se envían las
// líneas de la grilla y se muestra el resultado (descuento de la fila, etiqueta y aviso).
//
// Contrato con las pantallas (Ventas, Venta minorista, Pedidos):
//   PromoDesc.init({ habilitado, url, getRows, baseline, recalcFila, refrescar, avisar })
//   PromoDesc.recalcular([inmediato]) -> Promise   (debounce; true = ahora, para esperar antes de grabar)
//   PromoDesc.badge(row)  -> html con el nombre de la promoción aplicada ('' si no hay)
//   PromoDesc.reset()     -> olvida el último aviso (al limpiar el formulario)
// Campos que usa de cada fila (_rows[i]): fkProducto, cantidad, precioSinIva, descRec, fraccionado,
//   promo (resultado aplicado por este módulo) y promoLocked (fila que no participa: viene de un pedido).
// Regla de convivencia: un Desc/Rec distinto de cero SIN promo es manual y gana sobre la promoción.
// ================================================================
(function () {
    'use strict';

    var cfg = null, seq = 0, timer = null, ultimaFirma = '', proxVistas = {};

    function esc(s) {
        return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
    function fmtMoneda(n) {
        return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(n || 0);
    }

    // ¿La fila puede recibir promoción? (solo decide qué se envía; el servidor vuelve a filtrar)
    function elegible(row) {
        if (row.promoLocked || row.fraccionado) return false;
        if (!(+row.cantidad > 0) || !(+row.precioSinIva > 0)) return false;
        if (!row.promo && (+row.descRec || 0) !== 0) return false;   // desc/rec manual: gana el manual
        return true;
    }

    function limpiarPromo(row) {
        row.promo = null;
        row.descRec = cfg.baseline();
    }

    // Redibuja conservando el input que el usuario estaba editando (la grilla se regenera completa).
    function conFoco(fn) {
        var a = document.activeElement, tbId = null, idx = -1;
        if (a && a.tagName === 'INPUT') {
            var tb = a.closest('tbody');
            if (tb) { tbId = tb.id; idx = Array.prototype.indexOf.call(tb.querySelectorAll('input'), a); }
        }
        fn();
        if (tbId && idx >= 0) {
            var el = (document.getElementById(tbId) || document).querySelectorAll('input')[idx];
            if (el) { try { el.focus(); } catch (e) { } }
        }
    }

    async function ejecutar() {
        var mi = ++seq;
        var rows = cfg.getRows();

        var lineas = [];
        rows.forEach(function (r, i) {
            if (elegible(r)) lineas.push({
                clave: i, fkProducto: r.fkProducto, cantidad: +r.cantidad, precioSinIva: +r.precioSinIva
            });
        });

        var data = { lineas: [], resumen: [], proximas: [] };
        if (lineas.length) {
            try {
                var resp = await fetch(cfg.url, {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(lineas)
                });
                if (!resp.ok) { console.error('EvaluarPromos HTTP', resp.status); return; }
                data = await resp.json();
            } catch (e) { console.error('EvaluarPromos', e); return; }
        }
        if (mi !== seq) return;                       // llegó una evaluación más nueva: esta ya no vale
        if (cfg.getRows() !== rows) return;           // la grilla fue reemplazada (limpiar / cargar pedido)

        var porClave = {}, proxPorClave = {};
        (data.lineas || []).forEach(function (l) { porClave[l.clave] = l; });
        (data.proximas || []).forEach(function (x) { proxPorClave[x.clave] = x; });

        // Se vuelve a dibujar la grilla solo si cambió el resultado de alguna fila (evita parpadeos y perder el foco).
        var huboCambio = false;
        function huella(r) {
            var prox = r.proxima ? 'prox' + r.proxima.fkRegla + '_' + r.proxima.cantidadActual : '';
            return (r.promo ? [r.promo.fkRegla, r.promo.cantBonificada, r.promo.ahorro, r.descRec].join('|') : 'sin|' + r.descRec) + '#' + prox;
        }
        rows.forEach(function (r, i) {
            var res = porClave[i];
            var antes = huella(r);
            r.proxima = proxPorClave[i] || null;       // aviso de proximidad: solo informativo, no toca importes
            if (res) {
                r.promo = {
                    fkRegla: res.fkRegla, nombre: res.nombre, tipo: res.tipo, porcentaje: res.porcentaje,
                    lleva: res.lleva, paga: res.paga, cantBonificada: res.cantBonificada, ahorro: res.ahorro
                };
                r.descRec = -res.descuentoPct;
            } else if (r.promo) {                      // tenía promoción y ya no corresponde
                limpiarPromo(r);
            } else {
                if (huella(r) !== antes) huboCambio = true;   // solo cambió el aviso de proximidad
                return;
            }
            cfg.recalcFila(r);
            if (huella(r) !== antes) huboCambio = true;
        });

        if (huboCambio) conFoco(cfg.refrescar);

        // Aviso: solo promociones realmente aplicadas, y solo cuando cambia el conjunto aplicado.
        var resumen = data.resumen || [];
        var firma = JSON.stringify(resumen.map(function (x) { return [x.fkProducto, x.texto]; }));
        if (firma !== ultimaFirma) {
            ultimaFirma = firma;
            if (resumen.length) {
                var html = '<strong>Se aplicaron las siguientes promociones:</strong><ul class="mb-0 mt-1">' +
                    resumen.map(function (x) {
                        return '<li><strong>' + esc(x.descripcion) + '</strong>: ' + esc(x.texto) + '.</li>';
                    }).join('') + '</ul>';
                cfg.avisar(html);
            }
        }

        // Aviso de proximidad (¡falta 1 unidad para completar un grupo NxM!): una sola vez por combinación
        // producto + promoción + cantidad. Si el usuario no modifica la cantidad, no se repite.
        var vistasAhora = {}, nuevas = [];
        (data.proximas || []).forEach(function (x) {
            var k = x.fkProducto + '|' + x.fkRegla + '|' + x.cantidadActual;
            vistasAhora[k] = true;
            if (!proxVistas[k]) nuevas.push(x);
        });
        proxVistas = vistasAhora;
        if (nuevas.length) {
            var htmlProx = nuevas.map(function (x) {
                return '<div><strong>' + esc(x.descripcion) + '</strong><br>' + esc(x.texto) + '</div>';
            }).join('<hr class="my-1">');
            if (cfg.avisarProxima) cfg.avisarProxima(htmlProx);
            else toast(htmlProx, { id: 'promo-proxima', clase: 'warning', ms: 7000 });
        }
    }

    // Aviso flotante no bloqueante (esquina inferior derecha). Un mismo 'id' reemplaza al anterior.
    function toast(html, opt) {
        opt = opt || {};
        var cont = document.getElementById('promoToasts');
        if (!cont) {
            cont = document.createElement('div');
            cont.id = 'promoToasts';
            cont.style.cssText = 'position:fixed;right:1.2rem;bottom:1.2rem;z-index:1080;display:flex;' +
                'flex-direction:column;gap:.5rem;max-width:420px;pointer-events:none;';
            document.body.appendChild(cont);
        }
        var id = 'toast-' + (opt.id || 'general');
        var ant = document.getElementById(id);
        if (ant) ant.remove();
        var t = document.createElement('div');
        t.id = id;
        t.className = 'alert alert-' + (opt.clase || 'success') + ' alert-dismissible fade show shadow mb-0';
        t.style.cssText = 'pointer-events:auto;font-size:.84rem;padding-right:2rem;';
        t.setAttribute('role', 'status');
        t.innerHTML = html + '<button type="button" class="btn-close" aria-label="Cerrar"></button>';
        t.querySelector('.btn-close').addEventListener('click', function () { t.remove(); });
        cont.appendChild(t);
        setTimeout(function () { try { t.remove(); } catch (e) { } }, opt.ms || 8000);
    }

    window.PromoDesc = {
        init: function (c) { cfg = c; },
        activo: function () { return !!(cfg && cfg.habilitado); },

        recalcular: function (inmediato) {
            if (!cfg || !cfg.habilitado) return Promise.resolve();
            clearTimeout(timer);
            if (inmediato) return ejecutar();
            return new Promise(function (resolve) {
                timer = setTimeout(function () { ejecutar().then(resolve, resolve); }, 150);
            });
        },

        // Etiqueta con la promoción aplicada a la fila (nombre + importe sin IVA que se deja de cobrar).
        badge: function (row) {
            if (!row) return '';
            return badgePromo(row) + chipProxima(row);
        },

        toast: toast,

        reset: function () { ultimaFirma = ''; proxVistas = {}; clearTimeout(timer); seq++; }
    };

    // Etiqueta de proximidad: informativa, junto a la descripción. No cambia ningún importe.
    function chipProxima(row) {
        if (!row.proxima) return '';
        var x = row.proxima;
        return ' <span title="' + esc(x.texto) + '" style="display:inline-block;background:#f6c23e;color:#4a3500;' +
            'border-radius:8px;padding:0 .45rem;font-size:.68rem;font-weight:700;vertical-align:middle;white-space:nowrap;">' +
            esc(x.textoCorto) + '</span>';
    }

    // Etiqueta con la promoción aplicada a la fila (nombre + importe sin IVA que se deja de cobrar).
    function badgePromo(row) {
        if (!row.promo) return '';
        var p = row.promo;
        var detalle = p.tipo === 'NXM'
            ? 'Promoción ' + p.lleva + 'x' + p.paga + ' — ' + (+p.cantBonificada) + ' unidad(es) bonificada(s)'
            : (+p.porcentaje) + '% de descuento';
        var titulo = detalle + ' — ahorro ' + fmtMoneda(p.ahorro) + ' (sin IVA)';
        return ' <span title="' + esc(titulo) + '" style="display:inline-block;background:#1cc88a;color:#fff;' +
            'border-radius:8px;padding:0 .45rem;font-size:.68rem;font-weight:700;vertical-align:middle;">' +
            esc(p.nombre) + '</span>' +
            ' <span style="font-size:.68rem;color:#1a7a45;white-space:nowrap;">−' + esc(fmtMoneda(p.ahorro)) + '</span>';
    }
})();
