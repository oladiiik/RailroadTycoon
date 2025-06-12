export class PanZoom {
    constructor(viewport, layer) {
        this.vp    = viewport;
        this.layer = layer;
        this.scale = 0;
        this.tx    = 0;
        this.ty    = 0;
        this.initEvents();
        this.render();
    }
    
    initEvents() {
        this.vp.addEventListener('wheel',        this.onWheel.bind(this), { passive: false });
        this.vp.addEventListener('pointerdown',  this.onDown.bind(this));
        this.vp.addEventListener('pointermove',  this.onMove.bind(this));
        this.vp.addEventListener('pointerup',    this.onUp.bind(this));
        this.vp.addEventListener('dblclick', e => e.preventDefault());
    }
    
    clamp() {
        const vpRect = this.vp.getBoundingClientRect();
        const img    = document.getElementById('hmap');
        const w0     = img.naturalWidth;
        const h0     = img.naturalHeight;
        if (!w0 || !h0) return;

        const minScale = Math.max(vpRect.width / w0, vpRect.height / h0);
        const maxScale = 25;
        this.scale     = Math.max(minScale, Math.min(maxScale, this.scale));

        const mapW = w0 * this.scale;
        const mapH = h0 * this.scale;
        this.tx    = Math.min(0, Math.max(vpRect.width  - mapW, this.tx));
        this.ty    = Math.min(0, Math.max(vpRect.height - mapH, this.ty));
    }
    render() {
        this.clamp();
        this.layer.style.transform =
            `translate(${this.tx}px, ${this.ty}px) scale(${this.scale})`;
    }
    
    onWheel(e) {
        e.preventDefault();

        const rect = this.vp.getBoundingClientRect();
        const ox   = e.clientX - rect.left;
        const oy   = e.clientY - rect.top;

        const img  = document.getElementById('hmap');
        const w0   = img.naturalWidth;
        const h0   = img.naturalHeight;
        if (!w0 || !h0) return;

        const minScale = Math.max(rect.width / w0, rect.height / h0);
        const k        = e.deltaY > 0 ? 0.9 : 1.1;
        const newScale = Math.max(minScale, Math.min(25, this.scale * k));

        const worldX = (ox - this.tx) / this.scale;
        const worldY = (oy - this.ty) / this.scale;

        this.tx    = ox - worldX * newScale;
        this.ty    = oy - worldY * newScale;
        this.scale = newScale;

        this.render();
    }
    
    onDown(e) {
        if (e.button !== 0) return;
        
        if (e.target.closest('#controls')) return;
        
        if (e.target.closest('.city')) return;

        this.vp.setPointerCapture(e.pointerId);
        this.vp.style.cursor = 'grabbing';
        this.startX = e.clientX;
        this.startY = e.clientY;
    }

    onMove(e) {
        if (!this.vp.hasPointerCapture(e.pointerId)) return;

        this.tx += e.clientX - this.startX;
        this.ty += e.clientY - this.startY;
        this.startX = e.clientX;
        this.startY = e.clientY;
        this.render();
    }

    onUp(e) {
        if (this.vp.hasPointerCapture(e.pointerId))
            this.vp.releasePointerCapture(e.pointerId);
        this.vp.style.cursor = 'grab';
    }
}
