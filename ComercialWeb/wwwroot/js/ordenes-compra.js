// ================================================================
// Órdenes de Compra — lógica cliente
// Archivo externo para evitar que el parser HTML cierre bloques
// <script> al encontrar '</script' en template literals.
// ================================================================

// Variables inicializadas desde la vista (window.OC_*)
const CANT_STEP  = window.OC_CANT_STEP  || 1;
const TIPO_BUSQ  = window.OC_TIPO_BUSQ  || 1;

// ================================================================
// ESTADO GLOBAL
// ================================================================
let productoActual       = null;
let productosProveedor   = [];   // descripciones para autocomplete
let proveedorConfirmado  = false;

// ================================================================
// CONFIRMAR PROVEEDOR
// ================================================================
function confirmarProveedor() {
    const proveedorId = document.getElementById('ProveedorId').value;
    if (!proveedorId) {
        mostrarToast('Seleccioná un proveedor primero.', false);
        return;
    }

    // Habilitar controles
    document.querySelectorAll('fieldset, input, select').forEach(el => {
        el.disabled = false;
    });
    document.getElementById('btnGrabar').disabled = false;
    proveedorConfirmado = true;

    // Limpiar estado anterior
    document.getElementById('txtFiltro').value  = '';
    document.getElementById('txtDesc').value    = '';
    document.getElementById('lbDesc').innerHTML = '';
    document.getElementById('lbDesc').style.display = 'none';
    document.querySelector('input[name="Descuento"]').value = 0;
    document.querySelector('input[name="Recargo"]').value   = 0;
    const tbody = document.querySelector('#tablaDetalles tbody');
    if (tbody) tbody.innerHTML = '';
    actualizarTotal();

    // Ocultar panel éxito previo
    const panelExito = document.getElementById('panelExito');
    if (panelExito) panelExito.style.display = 'none';

    // Cargar descripciones del proveedor
    fetch('?handler=Productos&proveedorId=' + proveedorId)
        .then(r => r.json())
        .then(data => {
            productosProveedor = data;
            document.getElementById('txtFiltro').focus();
        });
}

// ================================================================
// BÚSQUEDA POR CÓDIGO / INTERNO
// ================================================================
function buscarProductoEnter(e) {
    if (e.key !== 'Enter' || !e.target.value.trim()) return;
    e.preventDefault();

    const filtro      = e.target.value.trim();
    const tipoBusq    = document.getElementById('TipoBusqueda').value;
    const proveedorId = document.getElementById('ProveedorId').value;

    fetch('?handler=BuscarProducto&filtro=' + encodeURIComponent(filtro) +
          '&tipoBusqueda=' + tipoBusq +
          '&proveedorId=' + proveedorId)
        .then(r => r.json())
        .then(data => {
            const lblRes = document.getElementById('lblresultadobusqueda');
            if (!data.encontrado) {
                lblRes.textContent = data.mensaje;
                lblRes.className   = 'oc-msg oc-msg-err';
                productoActual     = null;
                document.getElementById('txtFiltro').focus();
            } else {
                lblRes.textContent = data.descripcion;
                lblRes.className   = 'oc-msg oc-msg-ok';
                productoActual     = data;
                const cantInput    = document.getElementById('txtCantidad');
                cantInput.focus();
                cantInput.select();
            }
        });

    e.target.value = '';
}

// ================================================================
// AGREGAR POR CANTIDAD (Enter en txtCantidad)
// ================================================================
document.addEventListener('DOMContentLoaded', function () {
    const cantInput = document.getElementById('txtCantidad');
    if (!cantInput) return;

    cantInput.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') return;
        e.preventDefault();

        const cantidad = parseFloat(e.target.value);
        if (!productoActual || isNaN(cantidad) || cantidad <= 0) return;

        agregarOActualizarFila(productoActual, cantidad);

        document.getElementById('txtFiltro').value = '';
        document.getElementById('lblresultadobusqueda').textContent = '';
        document.getElementById('txtCantidad').value = '';
        productoActual = null;
        document.getElementById('txtFiltro').focus();
    });
});

// ================================================================
// AUTOCOMPLETE DESCRIPCIÓN
// ================================================================
function filtrarDescripcion(texto) {
    const lb = document.getElementById('lbDesc');
    lb.innerHTML = '';
    if (!texto.trim()) { lb.style.display = 'none'; return; }

    const q = texto.trim().toUpperCase();
    const res = productosProveedor.filter(d => d.toUpperCase().includes(q));

    if (!res.length) { lb.style.display = 'none'; return; }

    res.forEach(desc => {
        const item = document.createElement('div');
        item.className = 'oc-autocomplete-item';
        item.textContent = desc;
        item.tabIndex = 0;
        item.onclick = () => buscarPorDescripcion(desc);
        item.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { buscarPorDescripcion(desc); }
            if (e.key === 'ArrowDown' && this.nextElementSibling) { e.preventDefault(); this.nextElementSibling.focus(); }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                this.previousElementSibling ? this.previousElementSibling.focus() : document.getElementById('txtDesc').focus();
            }
        });
        lb.appendChild(item);
    });
    lb.style.display = 'block';
}

document.addEventListener('DOMContentLoaded', function () {
    const txtDesc = document.getElementById('txtDesc');
    if (!txtDesc) return;
    txtDesc.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown') {
            const lb = document.getElementById('lbDesc');
            if (lb.children.length) { e.preventDefault(); lb.children[0].focus(); }
        }
    });
    document.addEventListener('click', function (e) {
        if (!e.target.closest('#txtDesc') && !e.target.closest('#lbDesc'))
            document.getElementById('lbDesc').style.display = 'none';
    });
});

function buscarPorDescripcion(descripcion) {
    const proveedorId = document.getElementById('ProveedorId').value;
    fetch('?handler=BuscarPorDescripcion&descripcion=' + encodeURIComponent(descripcion) +
          '&proveedorId=' + proveedorId)
        .then(r => r.json())
        .then(data => {
            const lblRes = document.getElementById('lblresultadobusqueda');
            if (!data.encontrado) {
                lblRes.textContent = data.mensaje;
                lblRes.className   = 'oc-msg oc-msg-err';
                productoActual     = null;
            } else {
                lblRes.textContent = data.descripcion;
                lblRes.className   = 'oc-msg oc-msg-ok';
                productoActual     = data;
                const cantInput    = document.getElementById('txtCantidad');
                cantInput.focus();
                cantInput.select();
            }
        });
    document.getElementById('lbDesc').style.display = 'none';
    document.getElementById('txtDesc').value = '';
}

// ================================================================
// GRILLA — AGREGAR / ACTUALIZAR FILA
// ================================================================
function agregarOActualizarFila(prod, cantidad) {
    const tbody   = document.querySelector('#tablaDetalles tbody');
    const valorIVA = parseFloat(document.getElementById('IvaId').value) || 0;
    const desc    = parseFloat(document.querySelector('input[name="Descuento"]').value) || 0;
    const rec     = parseFloat(document.querySelector('input[name="Recargo"]').value) || 0;

    const precioBase = parseFloat(prod.precio);
    let precioSIVA = precioBase;
    if (desc > 0) precioSIVA = +(precioBase * (1 - desc / 100)).toFixed(2);
    else if (rec > 0) precioSIVA = +(precioBase * (1 + rec / 100)).toFixed(2);

    const precioCIVA = +(precioSIVA * (1 + valorIVA / 100)).toFixed(2);

    // Buscar si ya existe
    let existente = null;
    tbody.querySelectorAll('tr').forEach(tr => {
        if ((tr.querySelector('.cod-prov-cell')?.textContent === prod.codProveedor) ||
            (tr.querySelector('.cod-barras-cell')?.textContent === prod.codBarras)) {
            existente = tr;
        }
    });

    if (existente) {
        const pedidoInput = existente.querySelector('.pedido-input');
        const nuevo = (parseFloat(pedidoInput.value) || 0) + cantidad;
        pedidoInput.value = nuevo;
        existente.querySelector('.subtotal-cell').textContent = formatN(nuevo * precioCIVA);
    } else {
        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td class="cod-barras-cell"><code class="oc-code">' + esc(prod.codBarras) + '</code></td>' +
            '<td class="cod-prov-cell"><code class="oc-code">' + esc(prod.codProveedor) + '</code></td>' +
            '<td class="desc-cell">' + esc(prod.descripcion) + '</td>' +
            '<td class="text-end stock-cell">' + formatN(prod.cantidad) + '</td>' +
            '<td class="text-end cmin-cell">' + formatN(prod.cantidadMinima) + '</td>' +
            '<td class="text-end precio-siniva-cell">' + formatN(precioSIVA) + '</td>' +
            '<td class="text-end precio-coniva-cell">' + formatN(precioCIVA) + '</td>' +
            '<td class="text-end" style="width:90px;">' +
            '<input type="number" value="' + cantidad + '" step="' + CANT_STEP + '" ' +
            'class="oc-pedido-input pedido-input" />' +
            '</td>' +
            '<td class="text-end subtotal-cell">' + formatN(cantidad * precioCIVA) + '</td>' +
            '<td><button class="oc-btn-del eliminar-btn" title="Quitar fila">' +
            '<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="currentColor" viewBox="0 0 16 16">' +
            '<path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/>' +
            '<path fill-rule="evenodd" d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z"/>' +
            '</svg></button></td>' +
            '<td style="display:none" class="producto-id-cell">' + prod.id + '</td>' +
            '<td style="display:none" class="precio-original-cell">' + precioBase + '</td>';
        tbody.appendChild(tr);
    }

    actualizarTotal();
    actualizarCntFilas();
}

// ================================================================
// ELIMINAR FILA
// ================================================================
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.eliminar-btn');
    if (!btn) return;
    btn.closest('tr').remove();
    actualizarTotal();
    actualizarCntFilas();
});

// ================================================================
// CAMBIO EN PEDIDO (edición manual en la grilla)
// ================================================================
document.addEventListener('input', function (e) {
    if (!e.target.classList.contains('pedido-input')) return;
    const tr = e.target.closest('tr');
    const pCIVA = parseFloat(tr.querySelector('.precio-coniva-cell').textContent.replace(',', '.').replace(/\./g, function(m, o, s){ return o < s.lastIndexOf('.') ? '' : m; })) || 0;
    const qty   = parseFloat(e.target.value) || 0;
    tr.querySelector('.subtotal-cell').textContent = formatN(qty * pCIVA);
    actualizarTotal();
});

// ================================================================
// RECALCULAR TOTALES
// ================================================================
function recalcularTotales() {
    const valorIVA = parseFloat(document.getElementById('IvaId').value) || 0;
    const desc     = parseFloat(document.querySelector('input[name="Descuento"]').value) || 0;
    const rec      = parseFloat(document.querySelector('input[name="Recargo"]').value)   || 0;

    document.querySelectorAll('#tablaDetalles tbody tr').forEach(tr => {
        const precioBase = parseFloat(tr.querySelector('.precio-original-cell').textContent) || 0;
        let precioSIVA = precioBase;
        if (desc > 0) precioSIVA = +(precioBase * (1 - desc / 100)).toFixed(2);
        else if (rec > 0) precioSIVA = +(precioBase * (1 + rec / 100)).toFixed(2);
        const pCIVA = +(precioSIVA * (1 + valorIVA / 100)).toFixed(2);
        const qty   = parseFloat(tr.querySelector('.pedido-input').value) || 0;
        tr.querySelector('.precio-siniva-cell').textContent  = formatN(precioSIVA);
        tr.querySelector('.precio-coniva-cell').textContent  = formatN(pCIVA);
        tr.querySelector('.subtotal-cell').textContent       = formatN(qty * pCIVA);
    });

    actualizarTotal();
}

function actualizarTotal() {
    let total = 0;
    document.querySelectorAll('#tablaDetalles tbody tr').forEach(tr => {
        total += parseFloat(tr.querySelector('.subtotal-cell')?.textContent?.replace(/\./g, '').replace(',', '.') || 0);
    });
    const el = document.getElementById('txtTotal');
    if (el) el.value = total.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function actualizarCntFilas() {
    const n = document.querySelectorAll('#tablaDetalles tbody tr').length;
    const el = document.getElementById('cntFilas');
    if (el) el.textContent = n + ' producto(s)';
    document.getElementById('emptyGrilla').style.display = n === 0 ? 'block' : 'none';
}

// ================================================================
// DESCUENTO / RECARGO
// ================================================================
document.addEventListener('DOMContentLoaded', function () {
    const descInput = document.querySelector('input[name="Descuento"]');
    const recInput  = document.querySelector('input[name="Recargo"]');
    if (!descInput || !recInput) return;
    descInput.addEventListener('input', function () {
        if (parseFloat(this.value) > 0) recInput.value = 0;
        recalcularTotales();
    });
    recInput.addEventListener('input', function () {
        if (parseFloat(this.value) > 0) descInput.value = 0;
        recalcularTotales();
    });
});

// ================================================================
// GRABAR ORDEN — con barra de progreso multi-paso
// ================================================================
async function grabarOrden() {
    const detalles = obtenerDetalles();
    if (!detalles.length) {
        mostrarToast('Debe ingresar al menos un producto con cantidad mayor a cero.', false);
        return;
    }

    const btnGrabar = document.getElementById('btnGrabar');
    btnGrabar.disabled = true;

    // Mostrar panel de progreso
    const panelProg = document.getElementById('panelProgreso');
    panelProg.style.removeProperty('display');
    const panelExito = document.getElementById('panelExito');
    if (panelExito) panelExito.style.display = 'none';

    setStep('Validando datos…', 10);
    await sleep(200);

    setStep('Enviando orden al servidor…', 30);

    // Iniciar animación continua mientras espera
    let progVal = 30;
    const animInterval = setInterval(function () {
        progVal = Math.min(progVal + 2, 80);
        setProgress(progVal, 'Procesando…');
    }, 120);

    try {
        const total = parseFloat(
            (document.getElementById('txtTotal').value || '0')
                .replace(/\./g, '').replace(',', '.'));

        const payload = {
            proveedorId: parseInt(document.getElementById('ProveedorId').value),
            total:       total,
            iva:         parseFloat(document.getElementById('IvaId').value) || 0,
            recargo:     parseFloat(document.querySelector('input[name="Recargo"]').value) || 0,
            descuento:   parseFloat(document.querySelector('input[name="Descuento"]').value) || 0,
            detalles:    detalles
        };

        const response = await fetch('/Proveedores/OrdenesCompra?handler=Grabar', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload)
        });

        clearInterval(animInterval);

        if (!response.ok) {
            const msg = await response.text();
            throw new Error(msg || 'Error HTTP ' + response.status);
        }

        const data = await response.json();
        if (!data.success) throw new Error('El servidor indicó error al grabar.');

        setStep('Guardando en base de datos…', 90);
        await sleep(300);
        setStep('✔ Orden grabada correctamente', 100, true);

        // Mostrar panel de éxito con botones de reporte
        window.ultimaOrdenId = data.id;
        mostrarPanelExito(data.id);

    } catch (err) {
        clearInterval(animInterval);
        setStep('✘ Error: ' + err.message, 100, false, true);
        mostrarToast('Error al grabar: ' + err.message, false);
        btnGrabar.disabled = false;
    }
}

function obtenerDetalles() {
    const detalles = [];
    document.querySelectorAll('#tablaDetalles tbody tr').forEach(tr => {
        const pedido = parseFloat(tr.querySelector('.pedido-input').value) || 0;
        if (pedido > 0) {
            detalles.push({
                FkProducto:    parseInt(tr.querySelector('.producto-id-cell').textContent),
                CodBarras:     tr.querySelector('.cod-barras-cell').textContent.trim(),
                CodProveedor:  tr.querySelector('.cod-prov-cell').textContent.trim(),
                Descripcion:   tr.querySelector('.desc-cell').textContent.trim(),
                PrecioProveedor: parseFloat(tr.querySelector('.precio-siniva-cell').textContent.replace(',', '.').replace(/\./g, '')) || 0,
                Cantidad:      pedido,
                Subtotal:      parseFloat(tr.querySelector('.subtotal-cell').textContent.replace(',', '.').replace(/\./g, '')) || 0,
                FkColor:       0
            });
        }
    });
    return detalles;
}

// ================================================================
// PANEL ÉXITO POST-GUARDADO
// ================================================================
function mostrarPanelExito(ordenId) {
    const panel = document.getElementById('panelExito');
    if (!panel) return;
    document.getElementById('lblOrdenId').textContent = String(ordenId).padStart(8, '0');
    panel.style.removeProperty('display');
    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function imprimirOrden(formato) {
    const id = window.ultimaOrdenId;
    if (!id) { mostrarToast('No se encontró el ID de la orden.', false); return; }
    const url = '/Proveedores/OrdenesCompra?handler=ReporteOrden&id=' + id + '&formato=' + formato;
    window.open(url, '_blank');
    if (formato !== 'excel') setTimeout(function () { window.location.reload(); }, 1200);
    else window.location.reload();
}

// ================================================================
// BARRA DE PROGRESO HELPERS
// ================================================================
function setStep(texto, pct, ok, error) {
    setProgress(pct, texto);
    const lbl = document.getElementById('lblEstadoGuardar');
    if (!lbl) return;
    if (ok)    { lbl.textContent = '✔ Completado'; lbl.style.color = '#1cc88a'; }
    else if (error) { lbl.textContent = '✘ Error'; lbl.style.color = '#e74a3b'; }
    else       { lbl.textContent = ''; }
}
function setProgress(pct, texto) {
    const bar  = document.getElementById('progressBarGuardar');
    const lbl  = document.getElementById('lblProgresoGuardar');
    const pctL = document.getElementById('lblPctGuardar');
    if (!bar) return;
    bar.style.width   = pct + '%';
    bar.textContent   = pct + '%';
    if (lbl) lbl.textContent = texto || '';
    if (pctL) pctL.textContent = pct + '%';
}

// ================================================================
// MODAL STOCK PRODUCTOS
// ================================================================
document.addEventListener('DOMContentLoaded', function () {
    const modalEl = document.getElementById('modalStockProductos');
    if (!modalEl) return;

    modalEl.addEventListener('show.bs.modal', function () {
        const hoy   = new Date();
        const desde = new Date();
        desde.setDate(hoy.getDate() - 90);
        document.getElementById('dtpHasta').value = hoy.toISOString().split('T')[0];
        document.getElementById('dtpDesde').value = desde.toISOString().split('T')[0];
        cargarStockProductos();
    });

    document.getElementById('btnBuscarStock').addEventListener('click', cargarStockProductos);

    async function cargarStockProductos() {
        const proveedorId = document.getElementById('ProveedorId').value;
        const desde       = document.getElementById('dtpDesde').value;
        const hasta       = document.getElementById('dtpHasta').value;
        const tbody       = document.querySelector('#tablaStockProductos tbody');
        tbody.innerHTML   = '<tr><td colspan="7" style="text-align:center;color:#aaa;padding:1rem;">Cargando…</td></tr>';

        try {
            const r = await fetch('?handler=ProductosAPedir&proveedorId=' + proveedorId +
                                  '&desde=' + desde + '&hasta=' + hasta);
            const productos = await r.json();

            if (!productos || !productos.length) {
                tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;color:#e74a3b;padding:1rem;">No se encontraron productos.</td></tr>';
                return;
            }

            tbody.innerHTML = '';
            productos.forEach(p => {
                const tr = document.createElement('tr');
                tr.innerHTML =
                    '<td><code class="oc-code">' + esc(p.codProveedor) + '</code></td>' +
                    '<td>' + esc(p.descripcion) + '</td>' +
                    '<td class="text-end">' + formatN(p.stock) + '</td>' +
                    '<td class="text-end">' + formatN(p.cantidadMinima) + '</td>' +
                    '<td class="text-end">' + formatN(p.ingreso) + '</td>' +
                    '<td class="text-end">' + formatN(p.ventas) + '</td>' +
                    '<td style="width:90px;">' +
                    '<input type="number" class="oc-pedido-input cantidad-stock-input" ' +
                    'value="' + p.cantidad + '" ' +
                    'data-id="' + p.productoId + '" ' +
                    'data-prov="' + p.precioProveedor + '" ' +
                    'data-lista="' + p.precioLista + '" ' +
                    'data-costo="' + p.costo + '" ' +
                    'data-barras="' + esc(p.codBarras) + '" ' +
                    'data-codprov="' + esc(p.codProveedor) + '" ' +
                    'data-desc="' + esc(p.descripcion) + '" />' +
                    '</td>';
                tbody.appendChild(tr);
            });

            recalcularTotalesModal();
            tbody.querySelectorAll('.cantidad-stock-input').forEach(inp =>
                inp.addEventListener('input', recalcularTotalesModal));

        } catch (err) {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;color:#e74a3b;">Error al cargar.</td></tr>';
        }
    }

    function recalcularTotalesModal() {
        let cantProd = 0, totProv = 0, totLista = 0, totCosto = 0;
        document.querySelectorAll('.cantidad-stock-input').forEach(inp => {
            const qty   = parseFloat(inp.value) || 0;
            const prov  = parseFloat(inp.dataset.prov)  || 0;
            const lista = parseFloat(inp.dataset.lista) || 0;
            const costo = parseFloat(inp.dataset.costo) || 0;
            if (qty > 0) cantProd++;
            totProv  += prov  * qty;
            totLista += lista * qty;
            totCosto += costo * qty;
        });
        document.getElementById('txtCantProd').value  = cantProd;
        document.getElementById('txtTotProv').value   = formatN(totProv);
        document.getElementById('txtTotLista').value  = formatN(totLista);
        document.getElementById('txtTotCosto').value  = formatN(totCosto);
    }

    // Aceptar stock → pasar a grilla principal
    document.getElementById('btnAceptarStock').addEventListener('click', function () {
        document.querySelectorAll('#tablaStockProductos tbody .cantidad-stock-input').forEach(inp => {
            const qty = parseFloat(inp.value) || 0;
            if (qty <= 0) return;
            agregarOActualizarFila({
                id:            parseInt(inp.dataset.id),
                codProveedor:  inp.dataset.codprov,
                codBarras:     inp.dataset.barras,
                descripcion:   inp.dataset.desc,
                cantidad:      0,
                cantidadMinima:0,
                precio:        parseFloat(inp.dataset.prov) || 0
            }, qty);
        });
        bootstrap.Modal.getInstance(modalEl).hide();
    });

    // Imprimir reporte stock
    document.getElementById('btnImprimirStockPdf').addEventListener('click', function () {
        imprimirStock('pdf');
    });
    document.getElementById('btnImprimirStockExcel').addEventListener('click', function () {
        imprimirStock('excel');
    });
});

function imprimirStock(formato) {
    const desde = document.getElementById('dtpDesde').value;
    const hasta = document.getElementById('dtpHasta').value;
    const proveedorId = document.getElementById('ProveedorId').value;
    const url = '?handler=ReporteStock&formato=' + formato +
        '&desde=' + encodeURIComponent(desde) +
        '&hasta=' + encodeURIComponent(hasta) +
        '&proveedorId=' + encodeURIComponent(proveedorId);
    window.open(url, '_blank');
}

// ================================================================
// CANT. MÍNIMA → grilla
// ================================================================
document.addEventListener('DOMContentLoaded', function () {
    const btnMin = document.getElementById('btnCantMinima');
    if (!btnMin) return;
    btnMin.addEventListener('click', function () {
        const proveedorId = document.getElementById('ProveedorId').value;
        fetch('?handler=CantMinima&proveedorId=' + proveedorId)
            .then(r => r.json())
            .then(productos => {
                const tbody = document.querySelector('#tablaDetalles tbody');
                tbody.innerHTML = '';
                productos.forEach(p => {
                    agregarOActualizarFila({
                        id:            p.id,
                        codProveedor:  p.codProveedor,
                        codBarras:     p.codBarras,
                        descripcion:   p.descripcion,
                        cantidad:      p.cantidad,
                        cantidadMinima:p.cantidadMinima,
                        precio:        p.precio
                    }, p.pedido || 1);
                });
            });
    });
});

// ================================================================
// NUEVA ORDEN (limpiar todo)
// ================================================================
function nuevaOrden() {
    window.ultimaOrdenId = null;
    document.getElementById('panelExito').style.display = 'none';
    document.getElementById('panelProgreso').style.display = 'none';
    document.querySelector('#tablaDetalles tbody').innerHTML = '';
    document.getElementById('txtTotal').value = '0';
    document.getElementById('btnGrabar').disabled = false;
    actualizarCntFilas();
    document.getElementById('ProveedorId').focus();
}

// ================================================================
// HELPERS
// ================================================================
function formatN(n) {
    return parseFloat(n || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function esc(s) {
    return String(s == null ? '' : s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function mostrarToast(msg, ok) {
    const a = document.createElement('div');
    a.className = 'alert alert-' + (ok ? 'success' : 'danger') +
                  ' alert-dismissible fade show position-fixed';
    a.style.cssText = 'bottom:1.2rem;right:1.2rem;z-index:9999;min-width:290px;font-size:.85rem;max-width:430px;';
    a.innerHTML = msg + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>';
    document.body.appendChild(a);
    setTimeout(function () { try { a.remove(); } catch (e) { } }, 5000);
}
