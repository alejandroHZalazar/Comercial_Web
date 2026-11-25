// ================================================================
// Escáner de códigos de barras / QR con la cámara + selector de productos
// ----------------------------------------------------------------
// Componente compartido por Ventas y Venta minorista. NO contiene reglas de negocio: entrega el
// código leído a la página (opts.onCodigo), que usa su circuito habitual de alta por código de barras
// (misma búsqueda, mismo _agregarOSumar, mismas promociones).
//
//   Escaner.abrir({ onCodigo: async function(codigo){ return { ok, titulo, detalle, aviso } } })
//   Escaner.cerrar()
//   ElegirProducto.mostrar(lista, codigo) -> Promise<producto|null>   (código repetido en varios productos)
//
// Detección: API estándar BarcodeDetector cuando el navegador la trae con los formatos necesarios
// (Chrome Android). Si no, polyfill "barcode-detector" (ZXing-C++ en WebAssembly, MIT) alojado en
// /lib/barcode-detector/ (ponyfill.js + zxing_reader.wasm). Todo se procesa en el navegador: no se
// guardan ni se envían imágenes; al servidor solo llega el código.
// La cámara se libera al cerrar, al ocultarse la pestaña y al abandonar la página.
// ================================================================
(function () {
    'use strict';

    var LIB_URL  = '/lib/barcode-detector/ponyfill.js';
    var WASM_URL = '/lib/barcode-detector/zxing_reader.wasm';
    var FORMATOS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'itf', 'qr_code'];
    var INTERVALO_MS     = 120;    // ~8 lecturas por segundo
    var PAUSA_MISMO_MS   = 1500;   // el mismo código no se vuelve a tomar antes de este tiempo
    var PAUSA_OTRO_MS    = 600;    // pausa mínima entre dos códigos distintos

    var st = {
        abierto: false, pausado: false, ocupado: false,
        stream: null, track: null, detector: null, timer: null,
        ultimoCodigo: '', ultimoMs: 0, linterna: false,
        camaras: [], idxCamara: -1, opts: null, audio: null, modal: null, bsModal: null
    };

    function esc(s) {
        return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
    function $(id) { return document.getElementById(id); }

    // ── Biblioteca de detección ─────────────────────────────────────────────
    function cargarScript(src) {
        return new Promise(function (resolve, reject) {
            if (window.BarcodeDetectionAPI) { resolve(); return; }
            var s = document.createElement('script');
            s.src = src; s.async = true;
            s.onload = function () { resolve(); };
            s.onerror = function () { reject(new Error('No se pudo cargar ' + src)); };
            document.head.appendChild(s);
        });
    }

    async function crearDetector() {
        // 1) Detector nativo, si soporta al menos EAN-13 y Code 128 (los que usa el comercio)
        if ('BarcodeDetector' in window) {
            try {
                var soportados = await window.BarcodeDetector.getSupportedFormats();
                var usar = FORMATOS.filter(function (f) { return soportados.indexOf(f) >= 0; });
                if (usar.indexOf('ean_13') >= 0 && usar.indexOf('code_128') >= 0)
                    return new window.BarcodeDetector({ formats: usar });
            } catch (e) { /* se usa el polyfill */ }
        }
        // 2) Polyfill alojado localmente (sin CDN): el .wasm se sirve desde /lib/barcode-detector/
        await cargarScript(LIB_URL);
        var api = window.BarcodeDetectionAPI;
        if (!api || !api.BarcodeDetector)
            throw new Error('Falta la biblioteca de lectura en /lib/barcode-detector/.');
        api.prepareZXingModule({
            overrides: {
                locateFile: function (path, prefix) { return path.slice(-5) === '.wasm' ? WASM_URL : prefix + path; }
            }
        });
        return new api.BarcodeDetector({ formats: FORMATOS });
    }

    // ── Interfaz ────────────────────────────────────────────────────────────
    function construirModal() {
        if (st.modal) return;
        var div = document.createElement('div');
        div.className = 'modal fade';
        div.id = 'modalEscaner';
        div.tabIndex = -1;
        div.style.zIndex = '1070';   // por encima del botón de menú móvil del layout (1060), que tapaba el título
        div.setAttribute('aria-label', 'Escanear producto');
        div.innerHTML =
            '<div class="modal-dialog modal-dialog-centered modal-fullscreen-sm-down">' +
            ' <div class="modal-content" style="background:#111;color:#fff;border:none;">' +
            '  <div class="modal-header py-2" style="border-bottom:1px solid #333;">' +
            '   <h6 class="modal-title mb-0">📷 Escanear producto</h6>' +
            '   <button type="button" class="btn-close btn-close-white" aria-label="Cerrar" data-esc="cerrar"></button>' +
            '  </div>' +
            '  <div class="modal-body p-2">' +
            '   <div style="position:relative;width:100%;aspect-ratio:4/3;background:#000;border-radius:8px;overflow:hidden;">' +
            '    <video id="escVideo" playsinline muted autoplay style="width:100%;height:100%;object-fit:cover;"></video>' +
            '    <div style="position:absolute;left:8%;right:8%;top:30%;bottom:30%;border:2px solid rgba(28,200,138,.9);border-radius:8px;box-shadow:0 0 0 999px rgba(0,0,0,.25);pointer-events:none;"></div>' +
            '    <div id="escEstado" style="position:absolute;left:0;right:0;bottom:0;padding:.35rem .6rem;font-size:.8rem;background:rgba(0,0,0,.55);text-align:center;">Iniciando cámara…</div>' +
            '   </div>' +
            '   <div id="escResultado" class="mt-2" style="min-height:64px;border-radius:8px;padding:.5rem .7rem;background:#1e1e1e;font-size:.85rem;" aria-live="polite">' +
            '    <span style="color:#aaa;">Apuntá al código de barras o QR del producto.</span></div>' +
            '  </div>' +
            '  <div class="modal-footer py-2 d-flex flex-wrap gap-2 justify-content-between" style="border-top:1px solid #333;">' +
            '   <div class="d-flex gap-2">' +
            '    <button type="button" class="btn btn-sm btn-outline-light" data-esc="pausa" id="escBtnPausa">⏸ Pausar</button>' +
            '    <button type="button" class="btn btn-sm btn-outline-light" data-esc="linterna" id="escBtnLinterna" style="display:none;">🔦 Linterna</button>' +
            '    <button type="button" class="btn btn-sm btn-outline-light" data-esc="camara" id="escBtnCamara" style="display:none;">🔄 Cámara</button>' +
            '   </div>' +
            '   <button type="button" class="btn btn-sm btn-success" data-esc="cerrar">Listo</button>' +
            '  </div>' +
            ' </div>' +
            '</div>';
        document.body.appendChild(div);
        st.modal = div;
        st.bsModal = new bootstrap.Modal(div, { backdrop: 'static', keyboard: true });
        div.addEventListener('click', function (e) {
            var b = e.target.closest('[data-esc]');
            if (!b) return;
            var acc = b.getAttribute('data-esc');
            if (acc === 'cerrar') Escaner.cerrar();
            else if (acc === 'pausa') alternarPausa();
            else if (acc === 'linterna') alternarLinterna();
            else if (acc === 'camara') cambiarCamara();
        });
        div.addEventListener('hidden.bs.modal', function () { detenerCamara(); st.abierto = false; });
    }

    function estado(texto) { var e = $('escEstado'); if (e) e.textContent = texto; }

    function mostrarResultado(r) {
        var box = $('escResultado');
        if (!box) return;
        var color = r.ok ? '#1cc88a' : (r.cancelado ? '#858796' : '#e74a3b');
        box.style.background = r.ok ? 'rgba(28,200,138,.15)' : (r.cancelado ? '#1e1e1e' : 'rgba(231,74,59,.18)');
        box.innerHTML =
            '<div style="font-weight:700;color:' + color + ';">' + esc(r.titulo || (r.ok ? '✔ Agregado' : '✖ No se agregó')) + '</div>' +
            (r.detalle ? '<div>' + esc(r.detalle) + '</div>' : '') +
            (r.aviso ? '<div style="color:#f6c23e;font-weight:600;margin-top:.15rem;">' + esc(r.aviso) + '</div>' : '');
    }

    // ── Feedback: sonido (WebAudio, sin archivos) y vibración (si existe) ─────
    function beep(ok) {
        try {
            if (!st.audio) return;
            var o = st.audio.createOscillator(), g = st.audio.createGain();
            o.frequency.value = ok ? 1400 : 300;
            g.gain.value = 0.08;
            o.connect(g); g.connect(st.audio.destination);
            o.start(); o.stop(st.audio.currentTime + (ok ? 0.09 : 0.25));
        } catch (e) { }
        try { if (navigator.vibrate) navigator.vibrate(ok ? 60 : [40, 40, 40]); } catch (e) { }
    }

    // ── Cámara ──────────────────────────────────────────────────────────────
    async function iniciarCamara() {
        if (!window.isSecureContext || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia)
            throw new Error('La cámara solo está disponible con conexión segura (HTTPS).');

        var video = { width: { ideal: 1280 }, height: { ideal: 720 } };
        if (st.idxCamara >= 0 && st.camaras[st.idxCamara]) video.deviceId = { exact: st.camaras[st.idxCamara].deviceId };
        else video.facingMode = { ideal: 'environment' };          // cámara trasera por defecto

        st.stream = await navigator.mediaDevices.getUserMedia({ video: video, audio: false });
        st.track = st.stream.getVideoTracks()[0];
        var v = $('escVideo');
        v.srcObject = st.stream;
        try { await v.play(); } catch (e) { }

        // Linterna y cambio de cámara solo si el equipo los ofrece
        var caps = st.track && st.track.getCapabilities ? st.track.getCapabilities() : {};
        $('escBtnLinterna').style.display = caps && caps.torch ? '' : 'none';
        try {
            var devs = await navigator.mediaDevices.enumerateDevices();
            st.camaras = devs.filter(function (d) { return d.kind === 'videoinput'; });
            if (st.idxCamara < 0 && st.track) {
                var actual = st.track.getSettings ? st.track.getSettings().deviceId : null;
                st.idxCamara = Math.max(0, st.camaras.findIndex(function (c) { return c.deviceId === actual; }));
            }
        } catch (e) { st.camaras = []; }
        $('escBtnCamara').style.display = st.camaras.length > 1 ? '' : 'none';
    }

    function detenerCamara() {
        clearTimeout(st.timer); st.timer = null;
        if (st.stream) { st.stream.getTracks().forEach(function (t) { try { t.stop(); } catch (e) { } }); }
        st.stream = null; st.track = null; st.linterna = false;
        var v = $('escVideo'); if (v) v.srcObject = null;
    }

    async function alternarLinterna() {
        if (!st.track) return;
        try {
            st.linterna = !st.linterna;
            await st.track.applyConstraints({ advanced: [{ torch: st.linterna }] });
        } catch (e) { st.linterna = false; }
    }

    async function cambiarCamara() {
        if (st.camaras.length < 2) return;
        st.idxCamara = (st.idxCamara + 1) % st.camaras.length;
        detenerCamara();
        try { await iniciarCamara(); programar(); } catch (e) { estado(mensajeError(e)); }
    }

    function alternarPausa() {
        st.pausado = !st.pausado;
        $('escBtnPausa').textContent = st.pausado ? '▶ Reanudar' : '⏸ Pausar';
        estado(st.pausado ? 'Pausado' : 'Buscando código…');
        if (!st.pausado) programar();
    }

    function mensajeError(e) {
        var n = e && e.name;
        if (n === 'NotAllowedError' || n === 'SecurityError') return 'Permiso de cámara denegado. Habilitalo en la configuración del navegador.';
        if (n === 'NotFoundError' || n === 'OverconstrainedError') return 'No se encontró una cámara disponible.';
        if (n === 'NotReadableError') return 'La cámara está siendo usada por otra aplicación.';
        return (e && e.message) || 'No se pudo iniciar la cámara.';
    }

    // ── Bucle de lectura ────────────────────────────────────────────────────
    function programar() {
        clearTimeout(st.timer);
        if (!st.abierto || st.pausado || !st.stream) return;
        st.timer = setTimeout(leer, INTERVALO_MS);
    }

    async function leer() {
        if (!st.abierto || st.pausado || !st.stream) return;
        var v = $('escVideo');
        if (!st.ocupado && v && v.readyState >= 2 && st.detector) {
            try {
                var codigos = await st.detector.detect(v);
                if (codigos && codigos.length) await procesar(String(codigos[0].rawValue || '').trim());
            } catch (e) { /* frame sin lectura: se sigue intentando */ }
        }
        programar();
    }

    async function procesar(codigo) {
        if (!codigo || st.ocupado) return;
        var ahora = Date.now();
        // Evita lecturas duplicadas mientras el código sigue frente a la cámara
        if (codigo === st.ultimoCodigo && ahora - st.ultimoMs < PAUSA_MISMO_MS) return;
        if (ahora - st.ultimoMs < PAUSA_OTRO_MS) return;

        st.ocupado = true;
        estado('Buscando ' + codigo + '…');
        var r;
        try { r = await st.opts.onCodigo(codigo); }
        catch (e) { r = { ok: false, titulo: '✖ Error', detalle: (e && e.message) || String(e) }; }
        r = r || { ok: false };
        if (!r.cancelado) beep(!!r.ok);
        mostrarResultado(r);
        st.ultimoCodigo = codigo;
        st.ultimoMs = Date.now();               // la pausa cuenta desde que terminó el alta
        st.ocupado = false;
        if (st.abierto && !st.pausado) estado('Buscando código…');
    }

    // Al ocultar la pestaña se apaga la cámara; al volver se reanuda (si el escáner sigue abierto)
    document.addEventListener('visibilitychange', function () {
        if (!st.abierto) return;
        if (document.hidden) { detenerCamara(); estado('Cámara detenida (pestaña oculta)'); }
        else if (!st.stream) {
            iniciarCamara().then(function () { estado(st.pausado ? 'Pausado' : 'Buscando código…'); programar(); })
                .catch(function (e) { estado(mensajeError(e)); });
        }
    });
    window.addEventListener('pagehide', detenerCamara);

    window.Escaner = {
        disponible: function () { return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia); },

        abrir: async function (opts) {
            st.opts = opts || {};
            construirModal();
            st.abierto = true; st.pausado = false; st.ocupado = false; st.ultimoCodigo = ''; st.ultimoMs = 0;
            $('escBtnPausa').textContent = '⏸ Pausar';
            mostrarResultado({ ok: true, titulo: 'Listo para escanear', detalle: 'Apuntá al código de barras o QR del producto.' });
            $('escResultado').style.background = '#1e1e1e';
            st.bsModal.show();
            // El audio debe crearse en el gesto del usuario (requisito de los navegadores móviles)
            try { st.audio = st.audio || new (window.AudioContext || window.webkitAudioContext)(); st.audio.resume && st.audio.resume(); } catch (e) { }
            try {
                estado('Iniciando cámara…');
                await iniciarCamara();
                estado('Preparando lector…');
                st.detector = st.detector || await crearDetector();
                estado('Buscando código…');
                programar();
            } catch (e) {
                estado(mensajeError(e));
                mostrarResultado({ ok: false, titulo: 'No se puede escanear', detalle: mensajeError(e) });
            }
        },

        cerrar: function () {
            st.abierto = false;
            detenerCamara();
            if (st.bsModal) st.bsModal.hide();
        },

        // Para pruebas automatizadas: procesa un código como si lo hubiera leído la cámara
        _simular: function (codigo) { return procesar(codigo); },
        _estado: function () { return st; }
    };

    // ================================================================
    // Selector de producto cuando un código pertenece a varios productos
    // (capa propia por encima de cualquier modal, usable desde teclado y desde la cámara)
    // ================================================================
    window.ElegirProducto = {
        mostrar: function (lista, codigo) {
            return new Promise(function (resolve) {
                var fmt = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' });
                var capa = document.createElement('div');
                capa.style.cssText = 'position:fixed;inset:0;z-index:2100;background:rgba(0,0,0,.55);display:flex;' +
                    'align-items:center;justify-content:center;padding:1rem;';
                capa.innerHTML =
                    '<div role="dialog" aria-modal="true" style="background:#fff;color:#222;border-radius:10px;width:100%;max-width:560px;max-height:85vh;display:flex;flex-direction:column;box-shadow:0 8px 30px rgba(0,0,0,.3);">' +
                    ' <div style="padding:.7rem 1rem;border-bottom:1px solid #e3e6f0;">' +
                    '  <strong>El código ' + esc(codigo) + ' corresponde a ' + lista.length + ' productos</strong>' +
                    '  <div style="font-size:.8rem;color:#6c757d;">Elegí el producto correcto para agregarlo.</div></div>' +
                    ' <div style="overflow:auto;">' +
                    lista.map(function (p, i) {
                        return '<button type="button" data-i="' + i + '" style="display:block;width:100%;text-align:left;border:none;border-bottom:1px solid #f0f0f0;background:#fff;padding:.6rem 1rem;">' +
                            '<div style="font-weight:600;font-size:.88rem;">' + esc(p.descripcion) + '</div>' +
                            '<div style="font-size:.76rem;color:#6c757d;">Cód. prov. ' + esc(p.codProveedor || '—') +
                            ' · Stock ' + esc(p.stock) + ' · ' + esc(fmt.format(p.precioSinIva || 0)) + ' s/IVA</div></button>';
                    }).join('') +
                    ' </div>' +
                    ' <div style="padding:.6rem 1rem;border-top:1px solid #e3e6f0;text-align:right;">' +
                    '  <button type="button" data-cancelar="1" class="btn btn-sm btn-secondary">Cancelar</button></div>' +
                    '</div>';
                function cerrar(valor) { document.removeEventListener('keydown', teclas, true); capa.remove(); resolve(valor); }
                function teclas(e) { if (e.key === 'Escape') { e.stopPropagation(); cerrar(null); } }
                capa.addEventListener('click', function (e) {
                    var b = e.target.closest('[data-i]');
                    if (b) { cerrar(lista[+b.getAttribute('data-i')]); return; }
                    if (e.target.closest('[data-cancelar]') || e.target === capa) cerrar(null);
                });
                document.addEventListener('keydown', teclas, true);
                document.body.appendChild(capa);
                var primero = capa.querySelector('[data-i]');
                if (primero) primero.focus();
            });
        }
    };
})();
