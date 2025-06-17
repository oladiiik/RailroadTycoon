import { PanZoom } from './panZoom.js';
import { CityInteraction } from './cityInteraction.js';
import { ManualConnectionManager } from './manualConnectionManager.js';

let connectionManager, bestRoute, costPerKm, rewardPerCity;

const elements = {
    get viewport() { return document.getElementById('viewport'); },
    get layer() { return document.getElementById('layer'); },
    get hintToggle() { return document.getElementById('hintToggleBtn'); },
    get gridToggle() { return document.getElementById('gridToggleBtn'); },
    get gridSvgImage() { return document.getElementById('gridSvg'); },
    get budgetVal() { return document.getElementById('budgetVal'); },
    get connectedVal() { return document.getElementById('connectedVal'); },
    get costVal() { return document.getElementById('costVal'); },
    get rewardVal() { return document.getElementById('rewardVal'); },
    get bestCount() { return document.getElementById('bestCount'); },
    get controls() { return document.getElementById('controls'); },
    get citiesSvgWrap() { return document.getElementById('cities-svg-wrap'); },
    get selectedCity() { return document.getElementById('selected-city'); }
};

const LOADER_CONFIG = {
    animationInterval: 500,
    zIndex: 10000
};

const STYLES = {
    overlay: {
        position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
        background: 'rgba(0, 0, 0, 0.8)', display: 'flex',
        alignItems: 'center', justifyContent: 'center'
    },
    loaderText: {
        color: '#fff', fontSize: '1.5rem', fontFamily: 'sans-serif'
    },
    endMenuContainer: {
        position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
        background: 'rgba(0,0,0,0.7)', display: 'flex',
        alignItems: 'center', justifyContent: 'center', zIndex: 9999
    },
    endMenu: {
        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '1rem',
        padding: '2rem', background: '#333', borderRadius: '8px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.5)', color: '#fff'
    },
    button: {
        padding: '.8rem 2rem', fontSize: '1.1rem',
        border: 'none', borderRadius: '4px', cursor: 'pointer',
        background: '#4caf50', color: '#fff'
    },
    buttonRow: {
        display: 'flex', width: '100%', justifyContent: 'space-between', gap: '1rem'
    }
};

class LoaderManager {
    constructor() {
        this.loader = null;
        this.interval = null;
        this.dots = 0;
    }

    show() {
        this.loader = document.createElement('div');
        this.loader.id = 'loaderOverlay';
        Object.assign(this.loader.style, { ...STYLES.overlay, zIndex: LOADER_CONFIG.zIndex });

        const text = document.createElement('div');
        Object.assign(text.style, STYLES.loaderText);
        this.loader.appendChild(text);
        document.body.appendChild(this.loader);

        this.interval = setInterval(() => {
            this.dots = (this.dots % 3) + 1;
            text.textContent = 'Завантаження' + '.'.repeat(this.dots);
        }, LOADER_CONFIG.animationInterval);
    }

    hide() {
        if (this.interval) {
            clearInterval(this.interval);
            this.interval = null;
        }
        if (this.loader) {
            this.loader.style.display = 'none';
        }
    }

    showExisting() {
        const existing = document.getElementById('loaderOverlay');
        if (existing) existing.style.display = 'flex';
    }
}

const UIUpdater = {
    updateBudget(current, cost = null) {
        if (!elements.budgetVal) return;
        elements.budgetVal.textContent = cost != null
            ? `${Math.round(current)} (−${Math.round(cost)})`
            : Math.round(current);
    },

    updateConnected(count) {
        if (!elements.connectedVal) return;
        elements.connectedVal.textContent = count;
    },

    updateCost(cost) {
        if (elements.costVal) elements.costVal.textContent = Math.round(cost);
    },

    updateReward(reward) {
        if (elements.rewardVal) elements.rewardVal.textContent = Math.round(reward);
    },

    updateBestCount(count) {
        if (!elements.bestCount) return;
        elements.bestCount.textContent = count;
    }
};

class EndGameMenu {
    show(won) {
        elements.viewport.style.pointerEvents = 'none';

        const overlay = this.createOverlay();
        const menu = this.createMenu(won);

        overlay.appendChild(menu);
        document.body.appendChild(overlay);
    }

    createOverlay() {
        const overlay = document.createElement('div');
        Object.assign(overlay.style, STYLES.endMenuContainer);
        return overlay;
    }

    createMenu(won) {
        const menu = document.createElement('div');
        Object.assign(menu.style, STYLES.endMenu);

        const title = document.createElement('h2');
        title.textContent = won ? 'Вітаємо! Ви перемогли!' : 'Гру завершено';
        menu.appendChild(title);

        const buttonRow = this.createButtonRow(won);
        menu.appendChild(buttonRow);

        return menu;
    }

    createButtonRow(won) {
        const btnRow = document.createElement('div');
        Object.assign(btnRow.style, STYLES.buttonRow);

        const homeBtn = this.createButton('Додому', () => {
            window.location.href = '/';
        });

        const actionBtn = this.createButton(
            won ? 'Наступний рівень' : 'Спробувати знову',
            () => this.handleActionClick(won)
        );

        btnRow.appendChild(homeBtn);
        btnRow.appendChild(actionBtn);
        return btnRow;
    }

    createButton(text, onClick) {
        const button = document.createElement('button');
        button.textContent = text;
        Object.assign(button.style, STYLES.button);
        button.addEventListener('click', onClick);
        return button;
    }

    handleActionClick(won) {
        const loader = new LoaderManager();
        loader.showExisting();

        setTimeout(() => {
            if (won) {
                this.goToNextLevel();
            } else {
                window.location.reload();
            }
        }, 50);
    }

    goToNextLevel() {
        const params = new URLSearchParams(window.location.search);
        const newSeed = Math.floor(Math.random() * 1e9);
        params.set('seed', newSeed);
        params.set('costPerKm', costPerKm - 5000);
        params.set('rewardPerCity', rewardPerCity + 5000);
        window.location.href = `${location.pathname}?${params.toString()}`;
    }
}

class GameManager {
    constructor() {
        this.endGameMenu = new EndGameMenu();
    }

    checkGameEnd() {
        const connectedCount = connectionManager.reachable.size;
        const totalBest = bestRoute.connections.length + 1;

        if (connectedCount === totalBest) {
            this.endGameMenu.show(true);
            return;
        }

        if (!this.canPlayerMove()) {
            this.endGameMenu.show(false);
        }
    }

    canPlayerMove() {
        for (const idx of connectionManager.reachable) {
            const targets = connectionManager
                .getAffordableTargets(idx)
                .filter(t => !t.reachable);
            if (targets.length > 0) {
                return true;
            }
        }
        return false;
    }

    async fetchGameData() {
        const urlParams = new URLSearchParams(window.location.search);
        const params = new URLSearchParams({
            seed: urlParams.get('seed') || '42',
            freq: urlParams.get('freq') || '0.008',
            octaves: urlParams.get('octaves') || '10',
            lacunarity: urlParams.get('lacunarity') || '1.4',
            gain: urlParams.get('gain') || '0.55',
            cityCount: urlParams.get('cityCount') || '450',
            initialBudget: urlParams.get('initialBudget') || '3000000',
            costPerKm: urlParams.get('costPerKm') || '50000',
            rewardPerCity: urlParams.get('rewardPerCity') || '10000'
        });

        const response = await fetch(`/cities?${params}`);
        return await response.json();
    }

    async initializeConnectionManager(data) {
        const { cities }     = data;               
        bestRoute            = data.bestRoute;     
        costPerKm            = data.costPerKm;
        rewardPerCity        = data.rewardPerCity;
        
        const urlParams  = new URLSearchParams(window.location.search);
        const seedParams = '?' + new URLSearchParams({
            seed:          urlParams.get('seed')          ?? '42',
            freq:          urlParams.get('freq')          ?? '0.008',
            octaves:       urlParams.get('octaves')       ?? '4',
            lacunarity:    urlParams.get('lacunarity')    ?? '1.4',
            gain:          urlParams.get('gain')          ?? '0.55',
            cityCount:     urlParams.get('cityCount')     ?? '450',
            initialBudget: urlParams.get('initialBudget') ?? data.budget,
            costPerKm:     costPerKm,
            rewardPerCity: rewardPerCity
        }).toString();
        
        connectionManager = new ManualConnectionManager(cities, {
            costPerKm,
            initialBudget: data.budget,
            rewardPerCity,
            apiBase:   '',          
            seedParams
        });
    }
    updateInitialUI() {
        UIUpdater.updateCost(costPerKm);
        UIUpdater.updateReward(rewardPerCity);
        UIUpdater.updateBudget(connectionManager.remainingBudget());
        UIUpdater.updateConnected(connectionManager.reachable.size);

        const totalBestCities = bestRoute.connections.length + 1;
        UIUpdater.updateBestCount(totalBestCities);
    }

    setupBestCountDisplay() {
        if (elements.controls && !elements.bestCount) {
            const bestDiv = document.createElement('div');
            bestDiv.id = 'bestCountContainer';
            bestDiv.innerHTML = `Найкраща кількість міст: <span id="bestCount"></span>`;
            elements.controls.appendChild(bestDiv);
        }
    }
}

class SVGRenderer {
    constructor() {
        this.svgNS = 'http://www.w3.org/2000/svg';
        this.pixelSize = window.CELL_SIZE;
    }

    createCitiesSVG(cities) {
        const wrap = elements.citiesSvgWrap;
        const svg = document.createElementNS(this.svgNS, 'svg');

        svg.setAttribute('width', wrap.clientWidth);
        svg.setAttribute('height', wrap.clientHeight);
        Object.assign(svg.style, {
            position: 'absolute',
            top: '0',
            left: '0',
            pointerEvents: 'all'
        });

        this.renderCities(svg, cities);
        this.renderHintPaths(svg, cities);

        wrap.appendChild(svg);
        return svg;
    }

    renderCities(svg, cities) {
        cities.forEach(city => {
            const group = this.createCityGroup(city);
            svg.appendChild(group);
        });
    }

    createCityGroup(city) {
        const group = document.createElementNS(this.svgNS, 'g');
        group.classList.add('city');
        group.dataset.index = city.index;
        group.dataset.name = city.name;

        const circle = this.createCityCircle(city);
        group.appendChild(circle);

        return group;
    }

    createCityCircle(city) {
        const circle = document.createElementNS(this.svgNS, 'circle');
        const cx = city.x * this.pixelSize + this.pixelSize / 2;
        const cy = city.y * this.pixelSize + this.pixelSize / 2;

        circle.setAttribute('cx', cx);
        circle.setAttribute('cy', cy);
        circle.setAttribute('r', this.pixelSize / 2);
        circle.setAttribute('fill', city.index === bestRoute.startIdx ? '#FFD700' : 'green');
        circle.style.pointerEvents = 'all';

        return circle;
    }

    renderHintPaths(svg, cities) {
        const hintGroup = document.createElementNS(this.svgNS, 'g');
        hintGroup.setAttribute('class', 'hint-paths');
        Object.assign(hintGroup.style, {
            pointerEvents: 'none',
            display: 'none'
        });

        bestRoute.connections.forEach(({ from, to }) => {
            const line = this.createHintLine(cities[from], cities[to]);
            hintGroup.appendChild(line);
        });

        svg.insertBefore(hintGroup, svg.querySelector('.city'));
        this.setupHintToggle(hintGroup);
    }

    createHintLine(cityA, cityB) {
        const line = document.createElementNS(this.svgNS, 'line');
        const ax = cityA.x * this.pixelSize + this.pixelSize / 2;
        const ay = cityA.y * this.pixelSize + this.pixelSize / 2;
        const bx = cityB.x * this.pixelSize + this.pixelSize / 2;
        const by = cityB.y * this.pixelSize + this.pixelSize / 2;

        line.setAttribute('x1', ax);
        line.setAttribute('y1', ay);
        line.setAttribute('x2', bx);
        line.setAttribute('y2', by);
        line.setAttribute('stroke', '#FFD700');
        line.setAttribute('stroke-width', '3');
        line.setAttribute('stroke-dasharray', '8,4');

        return line;
    }

    setupHintToggle(hintGroup) {
        elements.hintToggle.addEventListener('change', (e) => {
            hintGroup.style.display = e.target.checked ? 'block' : 'none';
        });
    }
    
    setupGridToggle() {
        if (!elements.gridSvgImage) return;

        elements.gridSvgImage.style.display = 'none';
        elements.gridToggle.addEventListener('change', (e) => {
            elements.gridSvgImage.style.display = e.target.checked ? 'block' : 'none';
        });
    }
}

class GameInitializer {
    constructor() {
        this.loader = new LoaderManager();
        this.gameManager = new GameManager();
        this.svgRenderer = new SVGRenderer();
    }

    async initialize () {
        this.loader.show();
        
        const imgPromise   = this.waitForImages();
        const dataPromise  = this.gameManager.fetchGameData();

        this.initializePanZoom();        
        
        const [ , data ] = await Promise.all([imgPromise, dataPromise]);

        await this.gameManager.initializeConnectionManager(data);
        this.gameManager.setupBestCountDisplay();
        this.gameManager.updateInitialUI();
        
        const svgFragment = document.createDocumentFragment();
        this.svgRenderer.createCitiesSVG(data.cities, svgFragment); 
        elements.citiesSvgWrap.appendChild(svgFragment);

        this.svgRenderer.setupGridToggle();
        this.setupCityInteraction();

        this.loader.hide();
    }

    async waitForImages() {
        const hmapImg = document.getElementById('hmap');
        const linesImg = document.getElementById('lines');

        await Promise.all([
            hmapImg.complete ? Promise.resolve() : new Promise(resolve => hmapImg.onload = resolve),
            linesImg.complete ? Promise.resolve() : new Promise(resolve => linesImg.onload = resolve)
        ]);
    }

    initializePanZoom() {
        new PanZoom(elements.viewport, elements.layer);
    }

    setupCityInteraction() {
        new CityInteraction(
            elements.citiesSvgWrap,
            elements.selectedCity,
            (from, to) => this.handleConnection(from, to),
            (from) => connectionManager.getAffordableTargets(from)
        );
    }

    async handleConnection(from, to) {
        const result = await connectionManager.tryConnect(from, to);
        if (result.success) {
            UIUpdater.updateBudget(connectionManager.remainingBudget());
            UIUpdater.updateConnected(connectionManager.reachable.size);
            this.gameManager.checkGameEnd();
        }
        return result;
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    const gameInitializer = new GameInitializer();
    await gameInitializer.initialize();
});