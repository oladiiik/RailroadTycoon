export class CityInteraction {
    constructor(container, displayEl,
                onTryConnect = () => {},      
                onQueryAffordable = null) {  
        this.container        = container;
        this.displayEl        = displayEl;
        this.displayEl.draggable = false;
        this.displayEl.style.userSelect = 'none';

        this.onTryConnect     = onTryConnect;
        this.onQueryAffordable= onQueryAffordable;

        this.startCity   = null;
        this.overlayTexts = [];

        this.attach();
    }
    

    attach() {
        this.container.querySelectorAll('.city').forEach(g => {
            g.addEventListener('mousedown', e => this.onMouseDown(g, e));
            g.addEventListener('mouseup',   e => this.onMouseUp(g, e));
        });

        this.container.addEventListener('click', e => {
            if (e.target === this.container) this.clear();
        });
    }

    onMouseDown(g, e) {
        this.clear();
        this.startCity = g;

        const index = +g.dataset.index;

        if (this.onQueryAffordable) {
            const options = this.onQueryAffordable(index);
            this.highlightAffordableCities(options);
        }

        const circle = g.querySelector('circle');
        if (circle) circle.setAttribute('fill', '#FFD700');

        this.displayEl.textContent = 'Початок: ' + g.dataset.name;
        e.stopPropagation();
    }
    
    async onMouseUp(g, e) {
        if (!this.startCity || this.startCity === g) return;

        const from = +this.startCity.dataset.index;
        const to   = +g.dataset.index;
        
        if (this.onQueryAffordable) {
            const affordable = this.onQueryAffordable(from)
                .filter(o => !o.reachable);
            const allowed    = affordable.some(o => o.index === to);
            if (!allowed) {
                this.displayEl.textContent =
                    'Місто недоступне для з’єднання або вже приєднане';
                this.startCity = null;
                e.stopPropagation();
                return;
            }
        }
        
        const result = await this.onTryConnect(from, to);

        if (result?.success) {
            this.drawLine(this.startCity, g);
            this.displayEl.textContent =
                `З’єднано ${this.startCity.dataset.name} ↔ ${g.dataset.name}`;
        } else {
            const msg = this.reasonText(result?.reason);
            this.displayEl.textContent = msg;
        }

        this.startCity = null;
        e.stopPropagation();
    }
    

    clear() {
        this.container.querySelectorAll('.city circle')
            .forEach(c => c.setAttribute('fill', '#B82731FF'));
        this.overlayTexts.forEach(t => t.remove());
        this.overlayTexts = [];
        this.displayEl.textContent = '';
        this.startCity = null;
    }

    highlightAffordableCities(arr) {
        const svg = this.container.querySelector('svg');
        if (!svg) return;

        arr.forEach(({ index, cost, reachable }) => {
            if (reachable) return;                   

            const g = this.container.querySelector(`.city[data-index='${index}']`);
            if (!g) return;

            const circle = g.querySelector('circle');
            if (circle) circle.setAttribute('fill', 'green');

            const cx = +circle.getAttribute('cx');
            const cy = +circle.getAttribute('cy');

            const text = document.createElementNS(
                'http://www.w3.org/2000/svg', 'text');
            text.setAttribute('x', cx);
            text.setAttribute('y', cy - 5);
            text.setAttribute('text-anchor', 'middle');
            text.setAttribute('font-family', 'sans-serif');
            text.setAttribute('font-size', '6');
            text.setAttribute('fill', 'black');
            text.setAttribute('pointer-events', 'none');
            text.textContent = Math.round(cost);

            const firstCity = svg.querySelector('.city');
            firstCity ? svg.insertBefore(text, firstCity) : svg.appendChild(text);
            this.overlayTexts.push(text);
        });
    }

    drawLine(g1, g2) {
        const svg = this.container.querySelector('svg');
        if (!svg) return;

        const c1 = g1.querySelector('circle');
        const c2 = g2.querySelector('circle');

        const x1 = +c1.getAttribute('cx');
        const y1 = +c1.getAttribute('cy');
        const x2 = +c2.getAttribute('cx');
        const y2 = +c2.getAttribute('cy');

        const line = document.createElementNS(
            'http://www.w3.org/2000/svg', 'line');
        line.setAttribute('x1', x1);
        line.setAttribute('y1', y1);
        line.setAttribute('x2', x2);
        line.setAttribute('y2', y2);
        line.setAttribute('stroke', 'yellow');
        line.setAttribute('stroke-width', '2');

        svg.insertBefore(line, svg.querySelector('.city'));
    }

    reasonText(code) {
        switch (code) {
            case 'already-connected':   return 'Вузли вже з’єднані';
            case 'unreachable':         return 'Місто не приєднане до графу';
            case 'blocked-by-relief':   return 'Маршрут перекритий рельєфом';
            case 'no-budget':           return 'Недостатньо бюджету';
            case 'network':             return 'Помилка мережі';
            default:                    return `Помилка (${code ?? 'невідома'})`;
        }
    }
}
