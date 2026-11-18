// ================================================================
// Impresión de Etiquetas — lógica cliente
// Archivo externo para evitar que el parser HTML cierre el <script>
// al encontrar '</script' dentro de los template literals.
// ================================================================
const DEC            = window.IMP_DEC || 2;
const NOMBRE_EMPRESA = window.IMP_NOMBRE_EMPRESA || '';

// Constantes para construir tags <script> sin romper nada
const _SC = '<' + '/' + 'script>';
const _SO = '<' + 'script';

// ================================================================
// ESTADO
// ================================================================
const grilla  = new Map();
let tipoImp   = 'etiqueta';

// Cantidades: entero 1..CANT_MAX por producto, tope total por impresion
const CANT_MAX  = 500;
const TOTAL_MAX = 5000;
let cantidadGlobal = null;      // null = sin cantidad global fijada
let globalInvalida = false;

// ================================================================
// MULTI-SELECT
// ================================================================
function toggleMs(id) {
    const input = document.getElementById('msInput'+id);
    const panel = document.getElementById('msPanel'+id);
    const isOpen = panel.classList.contains('open');
    document.querySelectorAll('.ms-panel.open').forEach(p=>p.classList.remove('open'));
    document.querySelectorAll('.ms-input.open').forEach(i=>i.classList.remove('open'));
    if (!isOpen) {
        panel.classList.add('open'); input.classList.add('open');
        const s = panel.querySelector('.ms-search-box input');
        if (s) { s.value=''; filterMs(id,''); s.focus(); }
    }
}
function filterMs(id,val) {
    const q = val.trim().toUpperCase();
    document.querySelectorAll('#msOptions'+id+' .ms-option').forEach(opt=>
        opt.classList.toggle('hidden', q!=='' && !(opt.dataset.label||'').includes(q)));
}
function updateMsLabel(id) {
    const checked=[...document.querySelectorAll('.ms-chk-'+id.toLowerCase()+':checked')];
    const lbl=document.getElementById('msLabel'+id);
    if (!checked.length){ lbl.textContent=id==='Prov'?'Todos los proveedores':'Todos los rubros'; lbl.classList.add('placeholder'); }
    else if (checked.length===1){ lbl.textContent=checked[0].closest('label').textContent.trim(); lbl.classList.remove('placeholder'); }
    else { lbl.textContent=checked.length+' seleccionado(s)'; lbl.classList.remove('placeholder'); }
}
function selTodos(id) { document.querySelectorAll('#msOptions'+id+' .ms-option:not(.hidden) input').forEach(c=>c.checked=true);  updateMsLabel(id); }
function selNinguno(id){ document.querySelectorAll('.ms-chk-'+id.toLowerCase()).forEach(c=>c.checked=false); updateMsLabel(id); }
document.addEventListener('click',e=>{
    if (!e.target.closest('.ms-wrap')) {
        document.querySelectorAll('.ms-panel.open').forEach(p=>p.classList.remove('open'));
        document.querySelectorAll('.ms-input.open').forEach(i=>i.classList.remove('open'));
    }
});

// ================================================================
// TIPO DE IMPRESIÓN
// ================================================================
function setTipo(t) {
    tipoImp = t;
    document.getElementById('cardTipoEtiqueta').classList.toggle('active', t==='etiqueta');
    document.getElementById('cardTipoCodigo').classList.toggle('active', t==='codigo');
}

// ================================================================
// BUSCAR
// ================================================================
async function buscar() {
    const btn = document.getElementById('btnBuscar');
    const prev = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-sm"></span> Buscando…';
    try {
        const provIds=[...document.querySelectorAll('.ms-chk-prov:checked')].map(c=>c.value);
        const rubIds =[...document.querySelectorAll('.ms-chk-rub:checked')].map(c=>c.value);
        const texto  = document.getElementById('txtBusqueda').value.trim();
        if (!provIds.length && !rubIds.length && !texto) {
            mostrarToast('Ingresá al menos un filtro.', false); return;
        }
        const qs = new URLSearchParams();
        provIds.forEach(v=>qs.append('proveedores',v));
        rubIds.forEach(v =>qs.append('rubros',v));
        if (texto) qs.set('texto',texto);

        const resp = await fetch('/Productos/ImpresionEtiquetas?handler=Buscar&'+qs.toString());
        if (!resp.ok) throw new Error('HTTP '+resp.status);
        const items = await resp.json();
        if (!items.length) { mostrarToast('No se encontraron productos.', false); return; }

        let agr=0, om=0;
        items.forEach(item=>{
            if (!grilla.has(item.productoId)){
                item.cant = cantidadGlobal != null ? cantidadGlobal : 1;   // cantidad global vigente o 1
                grilla.set(item.productoId,item); agregarFila(item); agr++;
            }
            else om++;
        });
        actualizarEstado();
        document.getElementById('cardGrilla').style.removeProperty('display');
        document.getElementById('cardImprimir').style.removeProperty('display');
        let msg=agr+' producto(s) agregado(s).';
        if (om) msg+=' ('+om+' ya estaban)';
        mostrarToast(msg, true);
    } catch(err) {
        mostrarToast('Error: '+err.message, false);
    } finally {
        btn.disabled=false; btn.innerHTML=prev;
    }
}

// ================================================================
// GRILLA
// ================================================================
function agregarFila(item) {
    const tbody = document.getElementById('tbodyGrilla');
    const tr = document.createElement('tr');
    tr.id = 'row-'+item.productoId;
    tr.innerHTML =
        '<td><code style="font-size:.77rem;">'+esc(item.codProveedor)+'</code></td>'+
        '<td><code style="font-size:.77rem;">'+esc(item.codBarras)+'</code></td>'+
        '<td style="max-width:260px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;" title="'+esc(item.descripcion)+'">'+esc(item.descripcion)+'</td>'+
        '<td style="font-size:.78rem;">'+esc(item.proveedorNombre)+'</td>'+
        '<td style="font-size:.78rem;">'+esc(item.rubroNombre)+'</td>'+
        '<td style="text-align:center;"><input type="number" class="form-control form-control-sm qty-input" id="qty-'+item.productoId+'"'+
        ' min="1" max="'+CANT_MAX+'" step="1" value="'+item.cant+'" style="width:84px;margin:0 auto;"'+
        ' title="Cantidad de etiquetas de este producto (1 a '+CANT_MAX+')" oninput="cambiarCantidad('+item.productoId+',this)"/></td>'+
        '<td><button class="btn-del-row" title="Quitar" onclick="quitarFila('+item.productoId+')">'+
        '<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="currentColor" viewBox="0 0 16 16">'+
        '<path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/>'+
        '<path fill-rule="evenodd" d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z"/>'+
        '</svg></button></td>';
    tbody.appendChild(tr);
}

function quitarFila(id) {
    grilla.delete(id);
    const tr = document.getElementById('row-'+id);
    if (tr) tr.remove();
    actualizarEstado();
    if (!grilla.size) {
        document.getElementById('cardGrilla').style.display   = 'none';
        document.getElementById('cardImprimir').style.display = 'none';
    }
}

function limpiarGrilla() {
    grilla.clear();
    document.getElementById('tbodyGrilla').innerHTML = '';
    actualizarEstado();
    document.getElementById('cardGrilla').style.display   = 'none';
    document.getElementById('cardImprimir').style.display = 'none';
}

function actualizarEstado() {
    const n = grilla.size;
    document.getElementById('cntFilas').textContent = n+' producto(s)';
    document.getElementById('emptyGrilla').style.display = n===0?'block':'none';
    actualizarCantidades();
}

// ================================================================
// CANTIDADES
// ================================================================
// Devuelve el entero valido (1..CANT_MAX) o null si el texto no lo es.
function parseCant(inp) {
    const t = String(inp.value).trim();
    if (!/^\d+$/.test(t)) return null;          // vacio, decimales, negativos, texto
    const v = parseInt(t, 10);
    return (v >= 1 && v <= CANT_MAX) ? v : null;
}

function cambiarCantidad(id, inp) {
    const it = grilla.get(id);
    if (!it) return;
    it.cant = parseCant(inp);                    // null = invalida
    actualizarCantidades();
}

// La cantidad global reemplaza las cantidades individuales de TODOS los productos.
function cambiarCantidadGlobal(inp) {
    const vacio = String(inp.value).trim() === '' && !inp.validity.badInput;
    if (vacio) { cantidadGlobal = null; globalInvalida = false; actualizarCantidades(); return; }
    const v = parseCant(inp);
    if (v == null) { globalInvalida = true; actualizarCantidades(); return; }
    globalInvalida = false;
    cantidadGlobal = v;
    grilla.forEach((it, id) => {
        it.cant = v;
        const q = document.getElementById('qty-'+id);
        if (q) q.value = v;
    });
    actualizarCantidades();
}

// Valida todo, actualiza total, resaltado y habilita/deshabilita la impresion.
// Devuelve { total, error } (error = texto o null).
function actualizarCantidades() {
    let total = 0, invalidos = 0;
    grilla.forEach((it, id) => {
        const q = document.getElementById('qty-'+id);
        if (it.cant == null) invalidos++; else total += it.cant;
        if (q) {
            q.classList.toggle('qty-invalid', it.cant == null);
            q.classList.toggle('qty-multi', it.cant != null && it.cant > 1);
        }
    });
    const g = document.getElementById('txtCantGlobal');
    if (g) {
        g.classList.toggle('qty-invalid', globalInvalida);
        g.classList.toggle('qty-multi', !globalInvalida && cantidadGlobal != null && cantidadGlobal > 1);
    }

    let error = null;
    if (globalInvalida)         error = 'Cantidad de etiquetas inválida: ingresá un número entero entre 1 y '+CANT_MAX+'.';
    else if (invalidos > 0)     error = invalidos+' producto(s) con cantidad inválida: usá enteros entre 1 y '+CANT_MAX+' (sin decimales, ceros ni negativos).';
    else if (total > TOTAL_MAX) error = 'El total ('+total.toLocaleString('es-AR')+') supera el máximo de '+TOTAL_MAX.toLocaleString('es-AR')+' etiquetas por impresión.';

    const txt = 'Total: '+total.toLocaleString('es-AR')+' etiqueta'+(total===1?'':'s');
    ['lblTotalEtiquetas','lblTotalEtiquetas2'].forEach(i => {
        const e = document.getElementById(i);
        if (e) { e.textContent = txt; e.classList.toggle('badge-error', !!error); }
    });
    const m = document.getElementById('msgCantidad');
    if (m) { m.textContent = error || ''; m.style.display = error ? 'block' : 'none'; }
    const b = document.getElementById('btnGenerar');
    if (b) b.disabled = !!error || grilla.size === 0;
    return { total, error };
}

// ================================================================
// GENERAR PDF
// ================================================================
async function generarPdf() {
    if (!grilla.size) return;

    // No se imprime con cantidades invalidas ni por encima del tope.
    const chk = actualizarCantidades();
    if (chk.error) { mostrarToast(chk.error, false); return; }

    const btn = document.getElementById('btnGenerar');
    btn.disabled = true;
    const panelProg = document.getElementById('panelProgreso');
    panelProg.style.removeProperty('display');

    // Una entrada por etiqueta a imprimir: cada producto se repite 'cant' veces.
    const items  = [];
    grilla.forEach(it => { for (let k = 0; k < it.cant; k++) items.push(it); });
    const total  = items.length;      // == suma de las cantidades
    setProgreso(0, total, 'Iniciando generación…');
    const isEtiq = tipoImp === 'etiqueta';
    const fecha  = new Date().toLocaleDateString('es-AR');

    let labelsCss, labelsHtml = '';

    if (isEtiq) {
        labelsCss =
            '.lbl-grid { display:flex; flex-wrap:wrap; gap:2mm; padding:3mm; }'+
            '.lbl { width:50mm; height:30mm; border:0.4mm solid #555; border-radius:1mm;'+
            '  padding:1mm 1.5mm 0.8mm; box-sizing:border-box; overflow:hidden;'+
            '  page-break-inside:avoid; display:flex; flex-direction:column; justify-content:space-between;'+
            '  font-family:Arial,Helvetica,sans-serif; }'+
            '.lbl-desc { font-size:5.5pt; font-weight:700; color:#111; line-height:1.2;'+
            '            max-height:11pt; overflow:hidden; text-transform:uppercase; }'+
            '.lbl-cod  { font-size:5pt; color:#555; margin-top:0.5mm; }'+
            '.lbl-precio { font-size:10pt; font-weight:700; color:#1a2a4a;'+
            '              text-align:center; margin:0.5mm 0; line-height:1; }'+
            '.lbl-barcode { flex:1; display:flex; align-items:center; justify-content:center; overflow:hidden; }'+
            '.lbl-barcode svg { max-width:100%; max-height:11mm; }'+
            '.lbl-fecha { font-size:4.5pt; color:#888; text-align:left; margin-top:0.5mm; }';
    } else {
        labelsCss =
            '.lbl-grid { display:flex; flex-wrap:wrap; gap:2mm; padding:3mm; }'+
            '.lbl { width:30mm; height:20mm; border:0.4mm solid #555; border-radius:1mm;'+
            '  padding:1mm; box-sizing:border-box; overflow:hidden;'+
            '  page-break-inside:avoid; display:flex; flex-direction:column;'+
            '  align-items:center; justify-content:center;'+
            '  font-family:Arial,Helvetica,sans-serif; }'+
            '.lbl-barcode { flex:1; display:flex; align-items:center; justify-content:center; overflow:hidden; width:100%; }'+
            '.lbl-barcode svg { max-width:100%; max-height:12mm; }'+
            '.lbl-barnum { font-size:5pt; color:#222; text-align:center; margin-top:0.5mm; letter-spacing:0.5pt; }';
    }

    const LOTE = 20;
    for (let i = 0; i < total; i++) {
        const item = items[i];
        const cod5 = String(item.productoId).padStart(5, '0');
        const precio = Number(item.precioLista).toLocaleString('es-AR',
            { minimumFractionDigits: DEC, maximumFractionDigits: DEC });
        const barcode = (item.codBarras || '').trim();
        const tieneBarcode = barcode.length >= 1;

        if (isEtiq) {
            labelsHtml +=
                '<div class="lbl">'+
                '<div class="lbl-desc">'+escHtml(item.descripcion)+'</div>'+
                '<div class="lbl-cod">Cód: '+escHtml(cod5)+'</div>'+
                '<div class="lbl-precio">$ '+precio+'</div>'+
                '<div class="lbl-barcode">'+
                (tieneBarcode ? '<svg class="bcs" data-code="'+esc(barcode)+'"></svg>' : '<span style="font-size:5.5pt;color:#aaa;">Sin código de barras</span>')+
                '</div>'+
                '<div class="lbl-fecha">'+fecha+'</div>'+
                '</div>';
        } else {
            labelsHtml +=
                '<div class="lbl">'+
                '<div class="lbl-barcode">'+
                (tieneBarcode ? '<svg class="bcs" data-code="'+esc(barcode)+'"></svg>' : '<span style="font-size:5pt;color:#aaa;">Sin cód.</span>')+
                '</div>'+
                '<div class="lbl-barnum">'+escHtml(barcode)+'</div>'+
                '</div>';
        }

        if ((i + 1) % LOTE === 0 || i === total - 1) {
            setProgreso(i + 1, total, 'Generando etiqueta '+(i+1)+' de '+total+'…');
            await sleep(0);
        }
    }

    setProgreso(total, total, 'Abriendo ventana de impresión…');
    await sleep(80);

    // Una sola rutina dibuja todos los svg (uno por etiqueta, sin ids repetidos).
    const barcodeInits =
        'document.querySelectorAll("svg.bcs").forEach(function(s){'+
        'try { JsBarcode(s, s.getAttribute("data-code"), {format:"CODE128",width:1.2,height:'+(isEtiq ? 28 : 22)+',displayValue:false,margin:0}); } catch(e){}'+
        '});';

    const htmlCompleto =
        '<!DOCTYPE html><html lang="es"><head><meta charset="UTF-8">'+
        '<title>'+(isEtiq ? 'Etiquetas de Góndola' : 'Códigos de Barras')+'</title>'+
        _SO+' src="https://cdn.jsdelivr.net/npm/jsbarcode@3.11.6/dist/JsBarcode.all.min.js">'+_SC+
        '<style>'+
        '* { box-sizing:border-box; margin:0; padding:0; }'+
        'body { background:#fff; }'+
        '@page { size:A4; margin:5mm; }'+
        '@media print { body { -webkit-print-color-adjust:exact; print-color-adjust:exact; } .no-print { display:none !important; } }'+
        labelsCss+
        '</style></head><body>'+
        '<div class="lbl-grid">'+labelsHtml+'</div>'+
        _SO+'>window.onload=function(){'+barcodeInits+';setTimeout(function(){window.print();},300);};'+_SC+
        '</body></html>';

    const win = window.open('', '_blank');
    if (win) {
        win.document.write(htmlCompleto);
        win.document.close();
        setProgreso(total, total, '✔ Listo — se abrió la ventana de impresión');
        document.getElementById('lblEstado').textContent = '✔ Completado';
        document.getElementById('lblEstado').style.color = '#1cc88a';
    } else {
        mostrarToast('El navegador bloqueó la ventana emergente. Permitila e intentá de nuevo.', false);
        setProgreso(0, total, '');
    }

    btn.disabled = false;
}

// ================================================================
// HELPERS
// ================================================================
function setProgreso(actual, total, texto) {
    const pct = total > 0 ? Math.round((actual/total)*100) : 0;
    document.getElementById('progressBar').style.width  = pct + '%';
    document.getElementById('progressBar').textContent  = pct + '%';
    document.getElementById('lblPct').textContent       = pct + '%';
    document.getElementById('lblProgresoTexto').textContent = texto || '';
    const tipo = tipoImp === 'etiqueta' ? 'etiquetas' : 'códigos';
    document.getElementById('lblDetalle').textContent   = actual+' / '+total+' '+tipo+' generados';
}

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }
function esc(s)    { return String(s==null?'':s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }
function escHtml(s){ return String(s==null?'':s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

function mostrarToast(msg, ok) {
    const a = document.createElement('div');
    a.className = 'alert alert-'+(ok?'success':'danger')+' alert-dismissible fade show position-fixed';
    a.style.cssText = 'bottom:1.2rem;right:1.2rem;z-index:9999;min-width:290px;font-size:.85rem;max-width:430px;';
    a.innerHTML = msg+'<button type="button" class="btn-close" data-bs-dismiss="alert"></button>';
    document.body.appendChild(a);
    setTimeout(function(){ try{a.remove();}catch(e){} }, 4500);
}
